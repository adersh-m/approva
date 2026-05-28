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

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveExpense(Guid id)
    {
        if (!Request.Headers.TryGetValue("X-Employee-Id", out var managerIdStr)
            || !Guid.TryParse(managerIdStr, out var managerId))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Id header is required and must be a valid GUID"));

        try
        {
            var result = await _expenseService.ApproveExpenseAsync(id, managerId);
            return Ok(ApiResponse<ExpenseResponse>.Ok(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("Expense not found"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Manager's department does not match expense department"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectExpense(Guid id, [FromBody] RejectExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed"));

        if (!Request.Headers.TryGetValue("X-Employee-Id", out var managerIdStr)
            || !Guid.TryParse(managerIdStr, out var managerId))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Id header is required and must be a valid GUID"));

        try
        {
            var result = await _expenseService.RejectExpenseAsync(id, managerId, request);
            return Ok(ApiResponse<ExpenseResponse>.Ok(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("Expense not found"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Manager's department does not match expense department"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
