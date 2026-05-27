using System.ComponentModel.DataAnnotations;

namespace ExpenseApp.API.Application.DTOs.Expenses;

public class CreateExpenseRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    [MaxLength(2048)]
    public string? ReceiptUrl { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }
}
