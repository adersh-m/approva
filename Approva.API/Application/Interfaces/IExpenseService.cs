using ExpenseApp.API.Application.DTOs.Expenses;

namespace ExpenseApp.API.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, Guid employeeId);
    Task<ExpenseResponse> ApproveExpenseAsync(Guid expenseId, Guid managerId);
    Task<ExpenseResponse> RejectExpenseAsync(Guid expenseId, Guid managerId, RejectExpenseRequest request);
}
