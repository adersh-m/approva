namespace ExpenseApp.API.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedById { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }

    public Expense Expense { get; set; } = null!;
    public User PerformedBy { get; set; } = null!;
}
