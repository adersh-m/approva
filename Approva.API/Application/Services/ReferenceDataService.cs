using ExpenseApp.API.Application.DTOs.Reference;
using ExpenseApp.API.Application.Interfaces;

namespace ExpenseApp.API.Application.Services;

public class ReferenceDataService : IReferenceDataService
{
    private readonly IReferenceDataRepository _repo;
    private readonly ICacheService _cache;

    public ReferenceDataService(IReferenceDataRepository repo, ICacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public Task<List<CategoryDto>> GetCategoriesAsync() =>
        _cache.GetOrSetAsync("cache:categories:all", _repo.GetCategoriesAsync, TimeSpan.FromHours(24))!;

    public Task<List<DepartmentDto>> GetDepartmentsAsync() =>
        _cache.GetOrSetAsync("cache:departments:all", _repo.GetDepartmentsAsync, TimeSpan.FromHours(24))!;
}
