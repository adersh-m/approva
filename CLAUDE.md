# CLAUDE.md

Behavioral guidelines and architecture contract for the Expense Management System.
Merge both sections before implementing anything.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

---

## Part 1 — Behavioral Rules (Karpathy)

### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently.
Weak criteria ("make it work") require constant clarification.

---

## Part 2 — Project Architecture Contract

### Stack

| Layer | Technology | Version |
|---|---|---|
| Backend | .NET Web API | 10 |
| Frontend | React | Latest stable |
| Database | Azure SQL (free tier, serverless) | SQL Server 2022 compat |
| Cache | Redis | Latest stable |
| Messaging | Azure Service Bus | Standard tier |
| ORM | EF Core + Dapper | 9.0.5 stable |
| Auth | JWT Bearer tokens | — |

Never substitute these without explicit instruction.
Never introduce a new library without asking first.

**OpenAPI:** Use `Microsoft.AspNetCore.OpenApi` — native .NET 9+ package.
Never use Swashbuckle — it is not supported in .NET 9+ default templates.

**JSON serialization:** Always configure `JsonStringEnumConverter` globally in Program.cs.
Enums must serialize as strings in all API responses — never as integers.

---

### Infrastructure

**Local development setup:**

| Component | Where | Notes |
|---|---|---|
| Azure SQL | `exp-management.database.windows.net` | Free tier, serverless, pauses after 1hr inactivity — first request after break will be slow |
| Redis | Docker (`localhost:6379`) | `docker compose up redis -d` |
| Azure Service Bus | `exp-management-sb.servicebus.windows.net` | Standard tier, real Azure namespace |

**Docker Compose (Redis only):**

```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    container_name: expense-redis
    ports:
      - "6379:6379"
    restart: unless-stopped
```

Start with: `docker compose up -d`
Stop with: `docker compose down`

**Connection strings (appsettings.Development.json):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=exp-management.database.windows.net;Database=exp-management-db;User Id=sqladmin;Password=YourPassword;TrustServerCertificate=True",
    "Redis": "localhost:6379",
    "ServiceBus": "Endpoint=sb://exp-management-sb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxx"
  }
}
```

**Never commit appsettings.Development.json to source control.**
Add it to `.gitignore` immediately. Use `dotnet user-secrets` or environment variables for CI.

**Required NuGet packages:**

```
Microsoft.EntityFrameworkCore          9.0.5
Microsoft.EntityFrameworkCore.SqlServer 9.0.5
Microsoft.EntityFrameworkCore.Tools    9.0.5
Microsoft.Data.SqlClient               5.2.2
StackExchange.Redis                    latest
Microsoft.Extensions.Caching.StackExchangeRedis latest
Azure.Messaging.ServiceBus             latest
Dapper                                 latest
```

**Azure Service Bus — queues and topics provisioned:**

Queues:
- `expense-submitted-queue` (max delivery: 5)
- `reimbursement-queue` (max delivery: 5)

Topics and subscriptions:
- `expense-approved-topic` → `notification-sub`, `audit-sub`, `analytics-sub`
- `expense-rejected-topic` → `notification-sub`, `audit-sub`
- `policy-violation-topic` → `manager-sub`, `compliance-sub`

Never recreate these — they already exist in the Azure namespace.

---

---

### Data Access Rules — Non-Negotiable

**EF Core is for writes and transactional operations.**
**Dapper is for reads, reports, and dashboard queries.**

This is an architectural decision, not a preference. Do not deviate.

```
Write operations  → EF Core (expense submission, approval, rejection, reimbursement)
Read operations   → Dapper (dashboard, reports, expense lists, search, filters)
```

**EF Core rules:**
- Always use `async/await` — never `.Result` or `.Wait()`
- Always use `AsNoTracking()` on read-only EF queries if EF must be used for reads
- Always use `.Include()` explicitly — never rely on lazy loading
- Lazy loading is disabled globally — do not enable it
- Every Expense entity carries a `RowVersion` concurrency token

**Dapper rules:**
- All Dapper queries live in dedicated repository classes, never in controllers
- Use `QueryAsync` and `QuerySingleAsync` — never synchronous variants
- Parameters always use anonymous objects — never string interpolation
- DTOs for Dapper results are separate from EF entities — never reuse EF models for Dapper reads

---

### Concurrency Rules

**Optimistic concurrency on Expense edits:**
- `RowVersion byte[]` on the Expense entity, decorated with `[Timestamp]`
- Catch `DbUpdateConcurrencyException` and return HTTP 409 Conflict
- Frontend handles 409 by showing a conflict message and prompting refresh

**State machine guard on status transitions:**
- All status updates include a WHERE clause checking current state
- Example: approve only where `Status = 'Submitted'`
- Check affected row count — zero rows means the state transition was invalid
- Return HTTP 409 Conflict with a descriptive message

**Never use `lock` inside async methods.**
Use `SemaphoreSlim` for any in-process locking requirement.

**Valid expense status transitions:**
```
Draft → Submitted → Approved → Reimbursed
                 → Rejected
```
No other transitions are permitted.

---

### Idempotency Rules

**Every POST endpoint that creates or mutates state requires an idempotency key.**

Header name: `Idempotency-Key`
Format: UUID v4, generated by the client before the request is sent

**Server-side behavior:**
1. Extract key from header — return HTTP 400 if missing
2. Check Redis for existing result under that key
3. If found — return stored result with HTTP 200, do not reprocess
4. If not found — process request, store result in Redis with 24-hour TTL, return HTTP 201

**Redis key format:** `idempotency:{endpoint}:{key}`
Example: `idempotency:expenses:7f3b2a1c-94d0-4e6b-8f1a-2c3d4e5f6789`

**Endpoints requiring idempotency key:**
- `POST /api/expenses` (submit expense)
- `POST /api/expenses/{id}/approve`
- `POST /api/expenses/{id}/reject`
- `POST /api/expenses/{id}/reimburse`

---

### Caching Rules

**Cache shared reference data. Never cache authenticated or transactional data.**

| Data | Cache? | TTL | Strategy |
|---|---|---|---|
| Expense categories | Yes | 24 hours | Cache-aside |
| Currencies | Yes | 24 hours | Cache-aside |
| Department list | Yes | 24 hours | Cache-aside |
| Company policies | Yes | 1 hour | Cache-aside |
| Dashboard summary | Yes | 5 minutes | Cache-aside |
| User's own expenses | No | — | Always DB |
| Expense status | No | — | Always DB |
| Account/auth data | No | — | Always DB |

**Redis key format:** `cache:{resource}:{identifier}`
Examples:
- `cache:categories:all`
- `cache:dashboard:dept:{departmentId}`

**Eviction policy:** LRU
**Never cache inside a controller** — caching logic lives in service or repository layer only.

---

### Messaging Rules

**Azure Service Bus — queue vs topic decision per event:**

| Event | Mechanism | Reason |
|---|---|---|
| Expense submitted | Queue | One task, one manager to notify |
| Expense approved | Topic | Multiple subscribers: notification + audit + analytics |
| Expense rejected | Topic | Multiple subscribers: notification + audit |
| Reimbursement processed | Queue | One payment processor |
| Policy violation detected | Topic | Multiple subscribers: manager + compliance |

**Consumer rules:**
- Every message consumer must be idempotent
- Check a processed message log before acting — use Redis with message ID as key
- Dead letter queue monitoring is required for all queues and subscriptions
- Never throw unhandled exceptions in consumers — catch, log, and dead-letter explicitly

**Message envelope format:**
```json
{
  "messageId": "uuid-v4",
  "eventType": "ExpenseSubmitted",
  "occurredAt": "ISO-8601",
  "payload": {}
}
```

---

### Roles and Authorization

Three roles. No others.

| Role | What they can do |
|---|---|
| Employee | Submit, edit (Draft only), view own expenses |
| Manager | View team expenses, approve, reject |
| FinanceAdmin | View all expenses, trigger reimbursement, view all reports |

**Rules:**
- Role checks via `[Authorize(Roles = "...")]` on controllers
- Never do role logic inside service layer — that belongs at the API boundary
- Employees cannot see other employees' expenses — enforce at query level, not just UI

---

### Folder Structure

Single project structure. Separation of concerns enforced by folders, not separate projects.

```
/ExpenseApp.API
  /Domain
    /Entities              — EF Core entity classes
    /Enums                 — ExpenseStatus, UserRole
  /Application
    /Interfaces            — IExpenseService, ICacheService etc.
    /Services              — Business logic implementations
    /DTOs                  — Request and response objects
  /Infrastructure
    /Persistence           — AppDbContext, EF configurations, migrations
    /ReadRepositories      — Dapper read repositories and read DTOs
    /Cache                 — Redis cache service implementation
    /Messaging             — Service Bus publishers and consumers
  /Controllers             — API controllers
  /Middleware              — Idempotency, auth, error handling
  /Extensions              — IServiceCollection extensions, seed data
/client                    — React frontend (added in later tier)
```

Never put business logic in controllers.
Never put SQL in controllers.
Never put caching logic in controllers.
Never put Redis calls in controllers.

---

### API Conventions

- All endpoints return consistent response envelope:
```json
{
  "success": true,
  "data": {},
  "error": null
}
```
- HTTP 200 — success, existing resource returned (idempotent repeat)
- HTTP 201 — success, new resource created
- HTTP 400 — validation failure or missing required header
- HTTP 401 — unauthenticated
- HTTP 403 — authenticated but unauthorized for this role
- HTTP 409 — concurrency conflict or invalid state transition
- HTTP 500 — unhandled server error (never expose stack trace)

---

### Testing Expectations

Every feature delivered must include:

- Unit test for the service layer logic
- Unit test for the happy path and at least one failure path
- Integration test for the API endpoint
- For concurrency: a test that simulates simultaneous requests

Test project uses xUnit. Mocking uses Moq.
No feature is complete without passing tests.

---

### What This System Is Not

To keep scope honest:

- No multi-currency conversion logic in v1 — store currency code only
- No file storage for receipts in v1 — store URL string only
- No email sending in v1 — log the notification intent only
- Real Azure SQL and Azure Service Bus are used — but no deployment pipeline in Tier 1

These are deliberate v1 deferrals. Do not add them speculatively.
