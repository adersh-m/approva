using ExpenseApp.API.Application.DTOs;
using ExpenseApp.API.Application.DTOs.Expenses;
using ExpenseApp.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService) => _expenseService = expenseService;

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed"));

        if (!Request.Headers.TryGetValue("X-Employee-Id", out var employeeIdStr)
            || !Guid.TryParse(employeeIdStr, out var employeeId))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Id header is required and must be a valid GUID"));

        var result = await _expenseService.CreateExpenseAsync(request, employeeId);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ExpenseResponse>.Ok(result));
    }
}
