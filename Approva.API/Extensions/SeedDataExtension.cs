using Approva.API.Domain.Entities;
using Approva.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Approva.API.Extensions;

public static class SeedDataExtension
{
    private static readonly Guid DeptEngineering = new("00000000-0000-0000-0000-000000000020");
    private static readonly Guid DeptFinance     = new("00000000-0000-0000-0000-000000000021");
    private static readonly Guid DeptOperations  = new("00000000-0000-0000-0000-000000000022");

    private static readonly Guid CatTravel    = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CatMeals     = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid CatSoftware  = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid CatEquipment = new("00000000-0000-0000-0000-000000000004");
    private static readonly Guid CatTraining  = new("00000000-0000-0000-0000-000000000005");

    private static readonly Guid UserEmployee = new("00000000-0000-0000-0000-000000000010");
    private static readonly Guid UserManager  = new("00000000-0000-0000-0000-000000000011");
    private static readonly Guid UserAdmin    = new("00000000-0000-0000-0000-000000000012");

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void ApplySeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new { Id = DeptEngineering, Name = "Engineering", CreatedAt = SeedDate },
            new { Id = DeptFinance,     Name = "Finance",     CreatedAt = SeedDate },
            new { Id = DeptOperations,  Name = "Operations",  CreatedAt = SeedDate }
        );

        modelBuilder.Entity<ExpenseCategory>().HasData(
            new { Id = CatTravel,    Name = "Travel",     Description = "Travel-related expenses",             IsActive = true, CreatedAt = SeedDate },
            new { Id = CatMeals,     Name = "Meals",      Description = "Meal and entertainment expenses",     IsActive = true, CreatedAt = SeedDate },
            new { Id = CatSoftware,  Name = "Software",   Description = "Software licenses and subscriptions", IsActive = true, CreatedAt = SeedDate },
            new { Id = CatEquipment, Name = "Equipment",  Description = "Hardware and equipment purchases",    IsActive = true, CreatedAt = SeedDate },
            new { Id = CatTraining,  Name = "Training",   Description = "Training and certification expenses", IsActive = true, CreatedAt = SeedDate }
        );

        modelBuilder.Entity<User>().HasData(
            new { Id = UserEmployee, FullName = "Test Employee", Email = "employee@test.com", Role = UserRole.Employee,    DepartmentId = (Guid?)DeptEngineering, CreatedAt = SeedDate },
            new { Id = UserManager,  FullName = "Test Manager",  Email = "manager@test.com",  Role = UserRole.Manager,     DepartmentId = (Guid?)DeptEngineering, CreatedAt = SeedDate },
            new { Id = UserAdmin,    FullName = "Test Admin",     Email = "admin@test.com",    Role = UserRole.FinanceAdmin, DepartmentId = (Guid?)DeptFinance,     CreatedAt = SeedDate }
        );
    }
}
