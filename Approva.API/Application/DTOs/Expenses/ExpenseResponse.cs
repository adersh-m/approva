using Approva.API.Domain.Enums;

namespace Approva.API.Application.DTOs.Expenses;

public class ExpenseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ReceiptUrl { get; set; }
    public ExpenseStatus Status { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedById { get; set; }
    public string? RejectionReason { get; set; }
}
