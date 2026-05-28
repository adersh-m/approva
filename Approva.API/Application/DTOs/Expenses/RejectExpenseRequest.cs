using System.ComponentModel.DataAnnotations;

namespace Approva.API.Application.DTOs.Expenses;

public class RejectExpenseRequest
{
    [Required]
    [MaxLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;
}
