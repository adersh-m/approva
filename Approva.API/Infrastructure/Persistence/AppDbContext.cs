using ExpenseApp.API.Domain.Entities;
using ExpenseApp.API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseApp.API.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department>      Departments      => Set<Department>();
    public DbSet<User>            Users            => Set<User>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense>         Expenses         => Set<Expense>();
    public DbSet<AuditLog>        AuditLogs        => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.Property(d => d.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.Role).HasConversion<string>()
 .HasMaxLength(50).IsRequired();
            e.Property(u => u.CreatedAt).IsRequired();

            e.HasIndex(u => u.Email).IsUnique();

            e.HasOne(u => u.Department)
             .WithMany(d => d.Users)
             .HasForeignKey(u => u.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExpenseCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.IsActive).IsRequired();
            e.Property(c => c.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Title).HasMaxLength(500).IsRequired();
            e.Property(ex => ex.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(ex => ex.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(ex => ex.ReceiptUrl).HasMaxLength(2048);
            e.Property(ex => ex.Status)
                .HasConversion<string>()
                .HasMaxLength(50).IsRequired();
            e.Property(ex => ex.RejectionReason).HasMaxLength(1000);

            e.HasIndex(ex => ex.EmployeeId);
            e.HasIndex(ex => new { ex.DepartmentId, ex.Status });
            e.HasIndex(ex => ex.Status);

            e.HasOne(ex => ex.Employee)
             .WithMany(u => u.Expenses)
             .HasForeignKey(ex => ex.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.Category)
             .WithMany(c => c.Expenses)
             .HasForeignKey(ex => ex.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.Department)
             .WithMany(d => d.Expenses)
             .HasForeignKey(ex => ex.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.ApprovedBy)
             .WithMany(u => u.ApprovedExpenses)
             .HasForeignKey(ex => ex.ApprovedById)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.RejectedBy)
             .WithMany(u => u.RejectedExpenses)
             .HasForeignKey(ex => ex.RejectedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasMaxLength(200).IsRequired();
            e.Property(a => a.PerformedAt).IsRequired();
            e.Property(a => a.Notes).HasMaxLength(1000);

            e.HasIndex(a => a.ExpenseId);

            e.HasOne(a => a.Expense)
             .WithMany(ex => ex.AuditLogs)
             .HasForeignKey(a => a.ExpenseId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.PerformedBy)
             .WithMany(u => u.AuditLogs)
             .HasForeignKey(a => a.PerformedById)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.ApplySeedData();
    }
}
