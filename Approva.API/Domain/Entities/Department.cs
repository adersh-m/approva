namespace Approva.API.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = null!;
}
