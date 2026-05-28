using System.Data;
using Dapper;
using ExpenseApp.API.Application.DTOs.Reference;
using ExpenseApp.API.Application.Interfaces;

namespace ExpenseApp.API.Infrastructure.ReadRepositories;

public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly IDbConnection _db;

    public ReferenceDataRepository(IDbConnection db) => _db = db;

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var result = await _db.QueryAsync<CategoryDto>(
            "SELECT Id, Name, Description FROM ExpenseCategories WHERE IsActive = 1");
        return result.ToList();
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync()
    {
        var result = await _db.QueryAsync<DepartmentDto>(
            "SELECT Id, Name FROM Departments");
        return result.ToList();
    }
}
