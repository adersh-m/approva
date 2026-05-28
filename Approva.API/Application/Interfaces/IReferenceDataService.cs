using ExpenseApp.API.Application.DTOs.Reference;

namespace ExpenseApp.API.Application.Interfaces;

public interface IReferenceDataService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<DepartmentDto>> GetDepartmentsAsync();
}
