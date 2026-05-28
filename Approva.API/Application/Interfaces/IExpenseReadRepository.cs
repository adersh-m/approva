using Approva.API.Application.DTOs.Expenses;

namespace Approva.API.Application.Interfaces;

public interface IExpenseReadRepository
{
    Task<ExpenseListResponse> GetEmployeeExpensesAsync(Guid employeeId, int page, int pageSize);
    Task<ExpenseListResponse> GetDepartmentExpensesAsync(Guid departmentId, int page, int pageSize, string? status);
    Task<ExpenseListResponse> GetAllExpensesAsync(int page, int pageSize, string? status, Guid? departmentId);
    Task<DashboardSummary> GetDashboardSummaryAsync(Guid departmentId, DateTime periodStart, DateTime periodEnd);
    Task<DashboardSummary> GetAdminDashboardSummaryAsync(DateTime periodStart, DateTime periodEnd);
}
