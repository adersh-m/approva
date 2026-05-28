# 05 — Systems Concepts

The five Tier 1 concepts implemented in Approva — explained not as textbook definitions, but as they appear in this codebase and why they were implemented the way they were.

---

## 1. Caching with Redis

**The problem:** Reference data like expense categories and departments is read on almost every request — submitted with expenses, used to populate dropdowns, shown in listings. This data changes rarely if ever. Hitting the database every time wastes resources and adds latency for no benefit.

**What Approva does:** Cache-aside pattern via `ICacheService` backed by Redis.

```
GET /api/reference/categories
  → ReferenceDataService.GetCategoriesAsync
  → ICacheService.GetOrSetAsync("cache:categories:all", factory, 24hr TTL)
    → Cache hit? Return immediately from Redis
    → Cache miss? Query Azure SQL via Dapper → store in Redis → return
```

**What's cached and why:**

| Cached | Reason |
|---|---|
| Categories | Shared, changes require admin action, 24hr staleness acceptable |
| Departments | Same pattern |
| Idempotency responses | Not strictly caching — but uses the same Redis infrastructure |

**What's never cached and why:**

| Not cached | Reason |
|---|---|
| User's own expenses | Per-user data, poor cache hit rate, no shared benefit |
| Expense status | Transactional — must always reflect current DB state |
| Approval/rejection data | Consistency-critical — wrong answer has business consequences |

**The business connection:** Caching reference data reduces database load proportionally to traffic. At 1,000 concurrent users all loading the expense submission form, that's 1,000 category queries that become 1 query and 999 Redis hits. The database cost stays flat while traffic scales.

---

## 2. Idempotency

**The problem:** Networks are unreliable. A user clicks "Submit Expense." The request reaches the server, the expense is created, but the response never arrives — timeout. The user sees an error and clicks submit again. Without idempotency, they now have two identical expense claims.

**What Approva does:** `IdempotencyMiddleware` intercepts every POST request.

```
POST /api/expenses
Headers: Idempotency-Key: 7f3b2a1c-...

Middleware:
  1. Missing key? → HTTP 400 immediately
  2. Check Redis for "idempotency:expenses:{key}"
  3. Found? → Return stored response as HTTP 200. Controller never runs.
  4. Not found? → Allow request through
  5. After controller returns 201 → Store response in Redis with 24hr TTL
```

**Why middleware, not the service layer?** Because idempotency is a cross-cutting concern — every mutating endpoint needs it. Putting it in middleware means it's enforced uniformly without each service implementing its own version. The service layer never sees duplicate requests.

**Why 24 hours TTL?** Long enough to cover reasonable retry windows (hours), short enough that a user can legitimately resubmit the same expense data the next day as a new request.

**The business connection:** In a financial system, duplicate submissions aren't just a UX problem — they're a financial integrity problem. Duplicate expenses mean duplicate reimbursements, incorrect reporting, and manual reconciliation. Idempotency eliminates an entire category of financial data errors at the infrastructure level.

---

## 3. Queues and Topics (Azure Service Bus)

**The problem:** When an expense is submitted, things need to happen: the manager needs notification, the audit log needs an entry, analytics need updating. Doing all of this synchronously in the submission request means:
- The user waits for all of it to complete
- A notification service failure causes the expense submission to fail
- Services are tightly coupled

**What Approva does:** Publish a message to Service Bus after the database write. Each downstream concern processes it independently.

```
ExpenseService.CreateExpenseAsync
  → SaveChangesAsync (EF → Azure SQL)
  → PublishToQueue("expense-submitted-queue", { expenseId, employeeId, ... })
  → Return response to client

Later, independently:
  NotificationConsumer picks up message → sends manager email
  (if this fails, it retries up to 5 times before dead-lettering)
```

**Queue vs Topic decision:**

| Event | Mechanism | Reason |
|---|---|---|
| Expense submitted | Queue | One task: notify one manager. Point-to-point. |
| Expense approved | Topic | Three independent reactions: notify employee, log audit, update analytics. Broadcast. |
| Expense rejected | Topic | Two independent reactions: notify employee, log audit. |

**At-least-once delivery and idempotency:** Service Bus guarantees delivery but not exactly-once. A consumer crash after processing but before acknowledging means the message is redelivered. Every consumer must handle duplicate messages safely — the same patterns that protect the API (state machine guards, key checks) apply to message consumers.

**The business connection:** Decoupling submission from notification means a notification service outage doesn't prevent employees from submitting expenses. The notification happens when the service recovers, processing its backlog. The core transaction is never blocked by peripheral concerns.

---

## 4. Concurrency

**The problem:** Two managers open the same expense simultaneously. Both see Status: Submitted. Both click Approve. Both requests hit the server at the same moment. Without concurrency controls, both succeed — the expense is "approved twice" with two notifications sent.

**What Approva does:** Three layers of protection.

**Layer 1 — Idempotency key (outermost)**
If both managers are the same person (e.g., double-click), the same Idempotency-Key prevents the second request from reaching the database at all.

**Layer 2 — State machine guard (database level)**
```sql
UPDATE Expenses
SET Status = 'Approved', ApprovedById = @managerId, ApprovedAt = @now
WHERE Id = @id AND Status = 'Submitted'
```
If the expense is already Approved (first manager succeeded), zero rows are affected. The service checks `rowsAffected == 0` and returns 409 Conflict.

**Layer 3 — Optimistic concurrency (EF RowVersion)**
`Expense.RowVersion` is a `[Timestamp]` column — SQL Server updates it automatically on every write. EF Core includes it in every UPDATE's WHERE clause. If another transaction modified the row between the read and the write, the RowVersion won't match, and EF throws `DbUpdateConcurrencyException` → 409 Conflict.

**Why state machine guard over pessimistic locking?**
Pessimistic locking (SELECT WITH UPDLOCK) holds a database lock for the duration of the transaction. In a web API with potentially thousands of concurrent requests, this creates contention and degrades performance. The state machine guard achieves the same safety without holding locks — the database resolves conflicts at commit time, not at read time.

**The business connection:** In an expense system, a duplicate approval means duplicate reimbursements and incorrect financial records. The cost isn't just a bug fix — it's financial reconciliation, potential compliance issues, and user trust damage. Concurrency controls protect financial integrity at the infrastructure level.

---

## 5. EF Core vs Dapper

**The problem:** One data access tool cannot optimally serve both transactional writes and complex read queries. EF Core's change tracking and abstraction are valuable for writes. The same abstraction generates inefficient SQL for complex reads and adds overhead where none is needed.

**What Approva does:** EF Core for writes, Dapper for reads. Strictly enforced.

**EF Core — all write operations:**
```csharp
// ExpenseService.CreateExpenseAsync
var expense = new Expense { ... };
_context.Expenses.Add(expense);
await _context.SaveChangesAsync();
```
Change tracking, relationship management, migration-controlled schema, optimistic concurrency via RowVersion — all valuable on write paths.

**Dapper — all read operations:**
```csharp
// ExpenseReadRepository.GetDepartmentExpensesAsync
var sql = @"
    SELECT e.Id, e.Title, e.Amount, e.Status,
           c.Name as CategoryName,
           u.FullName as EmployeeName,
           d.Name as DepartmentName
    FROM Expenses e
    JOIN ExpenseCategories c ON e.CategoryId = c.Id
    JOIN Users u ON e.EmployeeId = u.Id
    JOIN Departments d ON e.DepartmentId = d.Id
    WHERE e.DepartmentId = @departmentId
    ORDER BY e.CreatedAt DESC
    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
```
Explicit SQL, predictable execution plan, returns exactly what's needed — no tracked objects, no lazy loading surprises, no N+1 risk.

**Why strict separation?**
The temptation is to use EF everywhere for simplicity. This works until the dashboard query takes 8 seconds under load and you discover EF generated five round trips where one SQL query would suffice. Establishing the boundary upfront — EF for writes, Dapper for reads — prevents this class of performance problems from appearing in the first place.

**The business connection:** A dashboard that takes 8 seconds to load drives users away. A submission that fails intermittently loses employee trust. Different operations have different performance profiles and different failure consequences. Using the right tool for each operation means both can be optimized independently without compromise.
