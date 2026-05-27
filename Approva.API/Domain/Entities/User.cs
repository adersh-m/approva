using ExpenseApp.API.Domain.Enums;

namespace ExpenseApp.API.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public Guid? DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department? Department { get; set; }
    public ICollection<Expense> Expenses { get; set; } = null!;
    public ICollection<Expense> ApprovedExpenses { get; set; } = null!;
    public ICollection<Expense> RejectedExpenses { get; set; } = null!;
    public ICollection<AuditLog> AuditLogs { get; set; } = null!;
}
