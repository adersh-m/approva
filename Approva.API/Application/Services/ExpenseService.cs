using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Approva.API.Application.DTOs.Expenses;
using Approva.API.Application.Interfaces;
using Approva.API.Domain.Entities;
using Approva.API.Domain.Enums;
using Approva.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Approva.API.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IExpenseReadRepository _readRepo;

    public ExpenseService(AppDbContext db, ServiceBusClient serviceBusClient, IExpenseReadRepository readRepo)
    {
        _db = db;
        _serviceBusClient = serviceBusClient;
        _readRepo = readRepo;
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, Guid employeeId)
    {
        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            ReceiptUrl = request.ReceiptUrl,
            Status = ExpenseStatus.Submitted,
            EmployeeId = employeeId,
            CategoryId = request.CategoryId,
            DepartmentId = request.DepartmentId,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        await PublishExpenseSubmittedAsync(expense);

        return MapToResponse(expense);
    }

    public async Task<ExpenseResponse> ApproveExpenseAsync(Guid expenseId, Guid managerId)
    {
        var manager = await _db.Users.FirstOrDefaultAsync(u => u.Id == managerId);

        var expense = await _db.Expenses
            .Include(e => e.Employee)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense is null)
            throw new KeyNotFoundException($"Expense {expenseId} not found");

        if (manager is null || manager.DepartmentId != expense.DepartmentId)
            throw new UnauthorizedAccessException("Manager's department does not match expense department");

        var now = DateTime.UtcNow;
        var rowsAffected = await _db.Expenses
            .Where(e => e.Id == expenseId && e.Status == ExpenseStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, ExpenseStatus.Approved)
                .SetProperty(e => e.ApprovedAt, now)
                .SetProperty(e => e.ApprovedById, managerId)
                .SetProperty(e => e.UpdatedAt, now));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Expense is not in Submitted status");

        await _db.Entry(expense).ReloadAsync();

        await PublishExpenseApprovedAsync(expense, managerId);

        return MapToResponse(expense);
    }

    public async Task<ExpenseResponse> RejectExpenseAsync(Guid expenseId, Guid managerId, RejectExpenseRequest request)
    {
        var manager = await _db.Users.FirstOrDefaultAsync(u => u.Id == managerId);

        var expense = await _db.Expenses
            .Include(e => e.Employee)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense is null)
            throw new KeyNotFoundException($"Expense {expenseId} not found");

        if (manager is null || manager.DepartmentId != expense.DepartmentId)
            throw new UnauthorizedAccessException("Manager's department does not match expense department");

        var now = DateTime.UtcNow;
        var rowsAffected = await _db.Expenses
            .Where(e => e.Id == expenseId && e.Status == ExpenseStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, ExpenseStatus.Rejected)
                .SetProperty(e => e.RejectedAt, now)
                .SetProperty(e => e.RejectedById, managerId)
                .SetProperty(e => e.RejectionReason, request.RejectionReason)
                .SetProperty(e => e.UpdatedAt, now));

        if (rowsAffected == 0)
            throw new InvalidOperationException("Expense is not in Submitted status");

        await _db.Entry(expense).ReloadAsync();

        await PublishExpenseRejectedAsync(expense, managerId, request.RejectionReason);

        return MapToResponse(expense);
    }

    public Task<ExpenseListResponse> GetEmployeeExpensesAsync(Guid employeeId, int page, int pageSize) =>
        _readRepo.GetEmployeeExpensesAsync(employeeId, page, pageSize);

    public async Task<ExpenseListResponse> GetDepartmentExpensesAsync(Guid managerId, int page, int pageSize, string? status)
    {
        var manager = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == managerId);

        if (manager?.DepartmentId is null)
            throw new KeyNotFoundException($"Manager {managerId} not found or has no department");

        return await _readRepo.GetDepartmentExpensesAsync(manager.DepartmentId.Value, page, pageSize, status);
    }

    public Task<ExpenseListResponse> GetAllExpensesAsync(int page, int pageSize, string? status, Guid? departmentId) =>
        _readRepo.GetAllExpensesAsync(page, pageSize, status, departmentId);

    public async Task<DashboardSummary> GetDashboardAsync(Guid managerId, DateTime periodStart, DateTime periodEnd)
    {
        var manager = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == managerId);

        if (manager?.DepartmentId is null)
            throw new KeyNotFoundException($"Manager {managerId} not found or has no department");

        return await _readRepo.GetDashboardSummaryAsync(manager.DepartmentId.Value, periodStart, periodEnd);
    }

    public Task<DashboardSummary> GetAdminDashboardAsync(DateTime periodStart, DateTime periodEnd) =>
        _readRepo.GetAdminDashboardSummaryAsync(periodStart, periodEnd);

    private static ExpenseResponse MapToResponse(Expense expense) => new()
    {
        Id = expense.Id,
        Title = expense.Title,
        Amount = expense.Amount,
        CurrencyCode = expense.CurrencyCode,
        ReceiptUrl = expense.ReceiptUrl,
        Status = expense.Status,
        EmployeeId = expense.EmployeeId,
        CategoryId = expense.CategoryId,
        DepartmentId = expense.DepartmentId,
        CreatedAt = expense.CreatedAt,
        UpdatedAt = expense.UpdatedAt,
        ApprovedAt = expense.ApprovedAt,
        ApprovedById = expense.ApprovedById,
        RejectedAt = expense.RejectedAt,
        RejectedById = expense.RejectedById,
        RejectionReason = expense.RejectionReason
    };

    private async Task PublishExpenseSubmittedAsync(Expense expense)
    {
        await using var sender = _serviceBusClient.CreateSender("expense-submitted-queue");

        var envelope = new
        {
            messageId = Guid.NewGuid().ToString(),
            eventType = "ExpenseSubmitted",
            occurredAt = DateTime.UtcNow.ToString("O"),
            payload = new
            {
                expenseId = expense.Id,
                employeeId = expense.EmployeeId,
                departmentId = expense.DepartmentId
            }
        };

        var message = new ServiceBusMessage(JsonSerializer.Serialize(envelope))
        {
            ContentType = "application/json",
            MessageId = envelope.messageId
        };

        await sender.SendMessageAsync(message);
    }

    private async Task PublishExpenseApprovedAsync(Expense expense, Guid managerId)
    {
        await using var sender = _serviceBusClient.CreateSender("expense-approved-topic");

        var envelope = new
        {
            messageId = Guid.NewGuid().ToString(),
            eventType = "ExpenseApproved",
            occurredAt = DateTime.UtcNow.ToString("O"),
            payload = new
            {
                expenseId = expense.Id,
                managerId,
                employeeId = expense.EmployeeId,
                departmentId = expense.DepartmentId
            }
        };

        var message = new ServiceBusMessage(JsonSerializer.Serialize(envelope))
        {
            ContentType = "application/json",
            MessageId = envelope.messageId
        };

        await sender.SendMessageAsync(message);
    }

    private async Task PublishExpenseRejectedAsync(Expense expense, Guid managerId, string rejectionReason)
    {
        await using var sender = _serviceBusClient.CreateSender("expense-rejected-topic");

        var envelope = new
        {
            messageId = Guid.NewGuid().ToString(),
            eventType = "ExpenseRejected",
            occurredAt = DateTime.UtcNow.ToString("O"),
            payload = new
            {
                expenseId = expense.Id,
                managerId,
                employeeId = expense.EmployeeId,
                rejectionReason
            }
        };

        var message = new ServiceBusMessage(JsonSerializer.Serialize(envelope))
        {
            ContentType = "application/json",
            MessageId = envelope.messageId
        };

        await sender.SendMessageAsync(message);
    }
}
