using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ExpenseApp.API.Application.DTOs.Expenses;
using ExpenseApp.API.Application.Interfaces;
using ExpenseApp.API.Domain.Entities;
using ExpenseApp.API.Domain.Enums;
using ExpenseApp.API.Infrastructure.Persistence;

namespace ExpenseApp.API.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    private readonly ServiceBusClient _serviceBusClient;

    public ExpenseService(AppDbContext db, ServiceBusClient serviceBusClient)
    {
        _db = db;
        _serviceBusClient = serviceBusClient;
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
            Status = ExpenseStatus.Draft,
            EmployeeId = employeeId,
            CategoryId = request.CategoryId,
            DepartmentId = request.DepartmentId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        await PublishExpenseSubmittedAsync(expense);

        return new ExpenseResponse
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
            UpdatedAt = expense.UpdatedAt
        };
    }

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
}
