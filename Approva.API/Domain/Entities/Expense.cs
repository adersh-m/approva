using System.ComponentModel.DataAnnotations;
using ExpenseApp.API.Domain.Enums;

namespace ExpenseApp.API.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ReceiptUrl { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    public Guid EmployeeId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid DepartmentId { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedById { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReimbursedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public User Employee { get; set; } = null!;
    public ExpenseCategory Category { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public User? ApprovedBy { get; set; }
    public User? RejectedBy { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = null!;
}
