namespace ExpenseApp.API.Application.DTOs.Expenses;

public class DashboardSummary
{
    public int TotalExpenses { get; set; }
    public decimal TotalAmount { get; set; }
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int RejectedCount { get; set; }
    public decimal RejectedAmount { get; set; }
    public int ReimbursedCount { get; set; }
    public decimal ReimbursedAmount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
