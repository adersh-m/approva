using ExpenseApp.API.Application.DTOs;
using ExpenseApp.API.Application.DTOs.Reference;
using ExpenseApp.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApp.API.Controllers;

[ApiController]
[Route("api/reference")]
public class ReferenceController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceController(IReferenceDataService referenceDataService) =>
        _referenceDataService = referenceDataService;

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _referenceDataService.GetCategoriesAsync();
        return Ok(ApiResponse<List<CategoryDto>>.Ok(result));
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await _referenceDataService.GetDepartmentsAsync();
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }
}
