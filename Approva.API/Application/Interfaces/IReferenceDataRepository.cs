using Approva.API.Application.DTOs.Reference;

namespace Approva.API.Application.Interfaces;

public interface IReferenceDataRepository
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<DepartmentDto>> GetDepartmentsAsync();
}
