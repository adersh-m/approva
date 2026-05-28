using Approva.API.Application.DTOs;
using Approva.API.Application.DTOs.Expenses;
using Approva.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Approva.API.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? departmentId = null)
    {
        if (!Request.Headers.TryGetValue("X-Employee-Id", out var actorIdStr)
            || !Guid.TryParse(actorIdStr, out var actorId))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Id header is required and must be a valid GUID"));

        if (!Request.Headers.TryGetValue("X-Employee-Role", out var role))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Role header is required"));

        try
        {
            ExpenseListResponse result = role.ToString() switch
            {
                "Employee" => await _expenseService.GetEmployeeExpensesAsync(actorId, page, pageSize),
                "Manager" => await _expenseService.GetDepartmentExpensesAsync(actorId, page, pageSize, status),
                "FinanceAdmin" => await _expenseService.GetAllExpensesAsync(page, pageSize, status, departmentId),
                _ => throw new UnauthorizedAccessException($"Unknown role: {role}")
            };

            return Ok(ApiResponse<ExpenseListResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null)
    {
        if (!Request.Headers.TryGetValue("X-Employee-Id", out var actorIdStr)
            || !Guid.TryParse(actorIdStr, out var actorId))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Id header is required and must be a valid GUID"));

        if (!Request.Headers.TryGetValue("X-Employee-Role", out var role))
            return BadRequest(ApiResponse<object>.Fail("X-Employee-Role header is required"));

        var start = periodStart ?? DateTime.UtcNow.AddDays(-30);
        var end = periodEnd ?? DateTime.UtcNow;

        try
        {
            DashboardSummary result = role.ToString() switch
            {
                "Manager" => await _expenseService.GetDashboardAsync(actorId, start, end),
                "FinanceAdmin" => await _expenseService.GetAdminDashboardAsync(start, end),
                _ => throw new UnauthorizedAccessException("Only Manager and FinanceAdmin can access the dashboard")
            };

            return Ok(ApiResponse<DashboardSummary>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
    }
}
