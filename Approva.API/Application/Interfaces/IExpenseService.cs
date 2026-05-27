using ExpenseApp.API.Application.DTOs.Expenses;

namespace ExpenseApp.API.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, Guid employeeId);
}
