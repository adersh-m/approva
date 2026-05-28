using System.Data;
using Dapper;
using Approva.API.Application.DTOs.Expenses;
using Approva.API.Application.Interfaces;

namespace Approva.API.Infrastructure.ReadRepositories;

public class ExpenseReadRepository : IExpenseReadRepository
{
    private readonly IDbConnection _db;

    public ExpenseReadRepository(IDbConnection db) => _db = db;

    private static string ListSelect =>
        @"SELECT e.Id, e.Title, e.Amount, e.CurrencyCode, e.Status,
                 c.Name AS CategoryName, u.FullName AS EmployeeName,
                 d.Name AS DepartmentName, e.SubmittedAt, e.CreatedAt
          FROM Expenses e
          INNER JOIN ExpenseCategories c ON e.CategoryId = c.Id
          INNER JOIN Users u             ON e.EmployeeId = u.Id
          INNER JOIN Departments d       ON e.DepartmentId = d.Id ";

    private static string Paginate(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        return $"ORDER BY e.CreatedAt DESC OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
    }

    public async Task<ExpenseListResponse> GetEmployeeExpensesAsync(Guid employeeId, int page, int pageSize)
    {
        var sql = ListSelect + "WHERE e.EmployeeId = @employeeId " + Paginate(page, pageSize);
        var items = await _db.QueryAsync<ExpenseListItem>(sql, new { employeeId });

        var total = await _db.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Expenses WHERE EmployeeId = @employeeId",
            new { employeeId });

        return new ExpenseListResponse { Items = items.ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ExpenseListResponse> GetDepartmentExpensesAsync(Guid departmentId, int page, int pageSize, string? status)
    {
        var where = status is not null
            ? "WHERE e.DepartmentId = @departmentId AND e.Status = @status "
            : "WHERE e.DepartmentId = @departmentId ";

        var sql = ListSelect + where + Paginate(page, pageSize);
        var items = await _db.QueryAsync<ExpenseListItem>(sql, new { departmentId, status });

        var countWhere = status is not null
            ? "WHERE DepartmentId = @departmentId AND Status = @status"
            : "WHERE DepartmentId = @departmentId";
        var total = await _db.QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM Expenses {countWhere}",
            new { departmentId, status });

        return new ExpenseListResponse { Items = items.ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ExpenseListResponse> GetAllExpensesAsync(int page, int pageSize, string? status, Guid? departmentId)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (status is not null)
        {
            conditions.Add("e.Status = @status");
            parameters.Add("status", status);
        }

        if (departmentId is not null)
        {
            conditions.Add("e.DepartmentId = @departmentId");
            parameters.Add("departmentId", departmentId);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) + " " : string.Empty;
        var sql = ListSelect + where + Paginate(page, pageSize);
        var items = await _db.QueryAsync<ExpenseListItem>(sql, parameters);

        var countConditions = conditions.Select(c => c.Replace("e.", string.Empty, StringComparison.Ordinal));
        var countWhere = countConditions.Any() ? "WHERE " + string.Join(" AND ", countConditions) : string.Empty;

        var countParams = new DynamicParameters();
        if (status is not null) countParams.Add("status", status);
        if (departmentId is not null) countParams.Add("departmentId", departmentId);

        var total = await _db.QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM Expenses {countWhere}",
            countParams);

        return new ExpenseListResponse { Items = items.ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    private const string DashboardAggregation =
        @"SELECT
            COUNT(*) AS TotalExpenses,
            ISNULL(SUM(Amount), 0) AS TotalAmount,
            SUM(CASE WHEN Status = 'Submitted' THEN 1 ELSE 0 END) AS PendingCount,
            ISNULL(SUM(CASE WHEN Status = 'Submitted' THEN Amount ELSE 0 END), 0) AS PendingAmount,
            SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
            ISNULL(SUM(CASE WHEN Status = 'Approved' THEN Amount ELSE 0 END), 0) AS ApprovedAmount,
            SUM(CASE WHEN Status = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
            ISNULL(SUM(CASE WHEN Status = 'Rejected' THEN Amount ELSE 0 END), 0) AS RejectedAmount,
            SUM(CASE WHEN Status = 'Reimbursed' THEN 1 ELSE 0 END) AS ReimbursedCount,
            ISNULL(SUM(CASE WHEN Status = 'Reimbursed' THEN Amount ELSE 0 END), 0) AS ReimbursedAmount
          FROM Expenses";

    public async Task<DashboardSummary> GetDashboardSummaryAsync(Guid departmentId, DateTime periodStart, DateTime periodEnd)
    {
        var result = await _db.QuerySingleAsync<DashboardSummary>(
            DashboardAggregation + " WHERE DepartmentId = @departmentId AND SubmittedAt BETWEEN @periodStart AND @periodEnd",
            new { departmentId, periodStart, periodEnd });

        result.PeriodStart = periodStart;
        result.PeriodEnd = periodEnd;
        return result;
    }

    public async Task<DashboardSummary> GetAdminDashboardSummaryAsync(DateTime periodStart, DateTime periodEnd)
    {
        var result = await _db.QuerySingleAsync<DashboardSummary>(
            DashboardAggregation + " WHERE SubmittedAt BETWEEN @periodStart AND @periodEnd",
            new { periodStart, periodEnd });

        result.PeriodStart = periodStart;
        result.PeriodEnd = periodEnd;
        return result;
    }
}
