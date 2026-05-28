using Approva.API.Application.DTOs.Expenses;

namespace Approva.API.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, Guid employeeId);
    Task<ExpenseResponse> ApproveExpenseAsync(Guid expenseId, Guid managerId);
    Task<ExpenseResponse> RejectExpenseAsync(Guid expenseId, Guid managerId, RejectExpenseRequest request);
    Task<ExpenseListResponse> GetEmployeeExpensesAsync(Guid employeeId, int page, int pageSize);
    Task<ExpenseListResponse> GetDepartmentExpensesAsync(Guid managerId, int page, int pageSize, string? status);
    Task<ExpenseListResponse> GetAllExpensesAsync(int page, int pageSize, string? status, Guid? departmentId);
    Task<DashboardSummary> GetDashboardAsync(Guid managerId, DateTime periodStart, DateTime periodEnd);
    Task<DashboardSummary> GetAdminDashboardAsync(DateTime periodStart, DateTime periodEnd);
}
