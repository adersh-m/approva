namespace Approva.API.Application.DTOs.Expenses;

public class ExpenseListResponse
{
    public List<ExpenseListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
