# 02 — Data Model

## Entity overview

```
Department ──< User ──< Expense ──< AuditLog
                │            │
                │       ExpenseCategory
                │
         (Manager approves/rejects Expense)
```

---

## Entities

### Department
```
Id             Guid, PK
Name           nvarchar(200), required
CreatedAt      datetime2, required
```

Represents an organizational unit. Expenses belong to departments. Managers approve expenses within their own department only.

---

### User
```
Id             Guid, PK
FullName       nvarchar(200), required
Email          nvarchar(256), required, unique index
Role           nvarchar(50), required, default 'Employee'
DepartmentId   Guid, FK → Department, nullable
CreatedAt      datetime2, required
```

**Why nullable DepartmentId?** FinanceAdmin operates across all departments. Assigning them to one department would artificially constrain their access. Nullable reflects reality.

**Auto-provisioning:** No registration flow. First time a known user hits the API, their User record is created automatically from their identity headers. Role defaults to Employee — elevated roles are assigned manually.

**Roles:** Employee, Manager, FinanceAdmin. No others in v1.

---

### ExpenseCategory
```
Id             Guid, PK
Name           nvarchar(200), required, unique
Description    nvarchar(1000), nullable
IsActive       bit, required, default true
CreatedAt      datetime2, required
```

Shared reference data. Cached in Redis for 24 hours. Only active categories are returned to clients.

---

### Expense
```
Id               Guid, PK
Title            nvarchar(500), required
Amount           decimal(18,2), required
CurrencyCode     nvarchar(3), required, default 'USD'
ReceiptUrl       nvarchar(2048), nullable
Status           nvarchar(50), required, default 'Draft'
EmployeeId       Guid, FK → User, required
CategoryId       Guid, FK → ExpenseCategory, required
DepartmentId     Guid, FK → Department, required
SubmittedAt      datetime2, nullable
ApprovedAt       datetime2, nullable
ApprovedById     Guid, FK → User, nullable
RejectedAt       datetime2, nullable
RejectedById     Guid, FK → User, nullable
RejectionReason  nvarchar(1000), nullable
ReimbursedAt     datetime2, nullable
CreatedAt        datetime2, required
UpdatedAt        datetime2, required
RowVersion       rowversion, required
```

**Why decimal(18,2)?** Default EF decimal precision loses scale. Explicit column type ensures currency amounts are stored correctly to two decimal places.

**Why RowVersion?** Optimistic concurrency token. EF Core checks this on every update — if the row was modified between read and write, `DbUpdateConcurrencyException` is thrown. This protects against concurrent edits without locking rows.

**Why nullable approval/rejection fields?** An expense starts as Draft. It only gets ApprovedAt when approved. Nullable accurately models the domain — an unapproved expense has no approval date.

**v1 deferrals:**
- `CurrencyCode` stores the code only — no conversion logic
- `ReceiptUrl` stores a URL string — no file upload or storage

---

### AuditLog
```
Id              Guid, PK
ExpenseId       Guid, FK → Expense, required
Action          nvarchar(200), required
PerformedById   Guid, FK → User, required
PerformedAt     datetime2, required
Notes           nvarchar(1000), nullable
```

Immutable record of every action taken on an expense. Written by Service Bus topic subscribers — not by the main transaction. This ensures audit logging never blocks or affects the core workflow.

---

## Status state machine

```
              ┌─────────────────────────────┐
              │                             │
           [Draft]                          │
              │                             │
         (submit)                           │
              │                             │
              ▼                             │
         [Submitted] ◀───────────────────── │
              │                             │
       ┌──────┴──────┐                      │
       │             │                      │
   (approve)      (reject)                  │
       │             │                      │
       ▼             ▼                      │
   [Approved]    [Rejected]                 │
       │                                    │
  (reimburse)                               │
       │                                    │
       ▼                                    │
  [Reimbursed] ──────────────────────────── ┘
```

**State transitions are enforced at two levels:**

1. **Application level** — Service layer checks current status before attempting transition
2. **Database level** — `ExecuteUpdateAsync WHERE Status = 'Submitted'` — if the row isn't in the expected state, zero rows are affected, and the operation returns 409 Conflict

This double enforcement protects against race conditions. Two managers attempting to approve the same expense simultaneously will result in one success and one 409 — not two approvals.

---

## Relationships and delete behavior

| Relationship | Delete behavior | Reason |
|---|---|---|
| User → Department | Restrict | Cannot delete a department with users |
| Expense → User (Employee) | Restrict | Cannot delete a user with expenses |
| Expense → User (ApprovedBy) | Restrict | Cannot delete a user who approved expenses |
| Expense → ExpenseCategory | Restrict | Cannot delete a category with expenses |
| Expense → Department | Restrict | Cannot delete a department with expenses |
| AuditLog → Expense | Restrict | Cannot delete an expense with audit history |
| AuditLog → User | Restrict | Cannot delete a user with audit history |

Restrict everywhere — data integrity over convenience. Deletions require manual cleanup or soft delete (not implemented in v1).

---

## Indexes

| Table | Index | Type | Reason |
|---|---|---|---|
| User | Email | Unique | Auto-provisioning lookup on every request |
| Expense | EmployeeId | Standard | Employees query their own expenses constantly |
| Expense | DepartmentId + Status | Composite | Managers filter by department and status |
| Expense | Status | Standard | FinanceAdmin filters all expenses by status |
| AuditLog | ExpenseId | Standard | Always queried by expense |

---

## Design decisions

**Why GUIDs for primary keys?**
Initially specified as int identity columns, but GUIDs were chosen because they're safer for distributed systems — no sequential ID exposure, compatible with client-generated IDs for idempotency, and future-proof for sharding or federation. The tradeoff is slightly larger index size, which is acceptable at this scale.

**Why enums stored as strings?**
`ExpenseStatus` and `UserRole` are stored as `nvarchar` with `HasConversion<string>()`. The alternative — storing as integers — makes the database unreadable without the code. String storage makes the database self-documenting and queryable by humans directly.

**Why separate ApprovedBy and RejectedBy FKs?**
An expense can only be approved or rejected — not both. But both need to be nullable. Two separate FKs with separate timestamps accurately model the domain and make queries straightforward without conditional joins.
