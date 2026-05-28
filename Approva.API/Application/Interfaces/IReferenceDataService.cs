using Approva.API.Application.DTOs.Reference;

namespace Approva.API.Application.Interfaces;

public interface IReferenceDataService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<DepartmentDto>> GetDepartmentsAsync();
}
