using ExpenseApp.API.Application.DTOs.Reference;

namespace ExpenseApp.API.Application.Interfaces;

public interface IReferenceDataRepository
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<DepartmentDto>> GetDepartmentsAsync();
}
