# 06 — Architecture Decision Records

Architecture Decision Records (ADRs) document significant decisions made during development — what was decided, why, what alternatives were considered, and what tradeoffs were accepted.

---

## ADR-001: EF Core for writes, Dapper for reads

**Decision:** Use EF Core 9 for all write operations and Dapper for all read operations. This boundary is enforced strictly — no EF in read repositories, no Dapper in write paths.

**Reasoning:**
EF Core provides change tracking, relationship management, migration-controlled schema, and optimistic concurrency via RowVersion. These features add real value on write paths where object graphs need to be managed and schema changes need to be tracked.

The same features add overhead on read paths. EF generates SQL that isn't always optimal for complex joins and aggregations. Dapper allows hand-written SQL that's predictable, tunable, and readable directly in the database.

**Alternatives considered:**
- EF Core everywhere: simpler, but performance degrades on complex reads. Dashboard queries with multiple joins and aggregations become verbose LINQ and generate inefficient SQL.
- Dapper everywhere: more control, but no change tracking, no migrations, more manual boilerplate on writes.
- Repository pattern abstraction over both: adds indirection without benefit for this project scope.

**Tradeoffs accepted:**
Two data access patterns to maintain instead of one. Developers must know which to use for each operation. Offset by explicit documentation and strict folder separation (Infrastructure/Persistence for EF, Infrastructure/ReadRepositories for Dapper).

---

## ADR-002: GUIDs for primary keys

**Decision:** All entities use `Guid` primary keys instead of `int` identity columns.

**Reasoning:**
GUIDs are safer for systems where IDs may be client-generated, exposed in URLs, or used across service boundaries. Idempotency keys benefit from non-sequential IDs. Sequential integer IDs expose record counts and make enumeration attacks trivial.

**Alternatives considered:**
- Int identity: smaller index size, slightly faster joins, human-readable. Appropriate for internal systems with no external exposure.
- Sequential GUIDs (NEWSEQUENTIALID): combine GUID benefits with better index performance. Considered but adds complexity for minimal gain at this scale.

**Tradeoffs accepted:**
Larger index size. Slightly less human-readable in logs and debugging. Acceptable at this scale.

---

## ADR-003: Idempotency in middleware, not service layer

**Decision:** Idempotency key validation and response caching is implemented in `IdempotencyMiddleware`, not in individual services.

**Reasoning:**
Idempotency is a cross-cutting concern. Every mutating endpoint needs it. Implementing it in the service layer means each service must remember to implement it, with no enforcement mechanism. Middleware ensures uniform application — a new endpoint gets idempotency protection automatically without any additional code.

**Alternatives considered:**
- Service-level idempotency: more granular control per operation, but requires every developer to remember the pattern. Higher risk of inconsistency.
- Action filter attribute: possible, but middleware has earlier access in the pipeline and doesn't require decorating every controller action.

**Tradeoffs accepted:**
Middleware captures the raw HTTP response, which means it's format-agnostic. If the response format ever changes, the middleware captures the new format automatically. Minor complexity in response interception (capturing and storing the response body stream).

---

## ADR-004: State machine guard over pessimistic locking

**Decision:** Status transitions use a `WHERE Status = 'Submitted'` guard in the UPDATE query rather than pessimistic locking (SELECT WITH UPDLOCK).

**Reasoning:**
Pessimistic locking holds a database lock for the duration of the transaction. In a web API with concurrent requests, this creates contention — every approval request for any expense blocks on the same lock table. Under load, this degrades performance significantly.

The state machine guard achieves the same safety without holding locks. The database resolves conflicts at commit time: if two managers approve simultaneously, one succeeds and one sees zero rows affected (conflict detected, 409 returned). No locks held, no contention.

**Alternatives considered:**
- Pessimistic locking: stronger guarantee, simpler reasoning about correctness. Appropriate for long-running transactions or complex multi-step operations where intermediate state matters.
- Application-level locking (SemaphoreSlim): in-process only, doesn't protect against multiple API instances. Not suitable.

**Tradeoffs accepted:**
Optimistic approach means a conflict is detected at commit rather than prevented at read. The losing transaction does real work (fetches the expense, validates the manager's department) before discovering the conflict. At low conflict rates (which is realistic for expense approvals), this is more efficient than holding locks.

---

## ADR-005: Azure Service Bus Standard over Basic

**Decision:** Use Service Bus Standard tier, not Basic.

**Reasoning:**
Basic tier supports queues only. Topics (pub/sub) are required for broadcast events — expense approval needs to notify the employee, update the audit log, and trigger analytics updates simultaneously. These are independent concerns that should not know about each other. Topics make this possible; queues do not.

**Alternatives considered:**
- Multiple queues instead of topics: the expense service would need to know about every downstream consumer and send to each queue explicitly. Adding a new consumer requires changing the publisher. This is tight coupling through the back door.
- In-process events (MediatR): no network hop, simpler setup, but ties consumers to the same process. Doesn't survive service restarts. Not suitable for audit logging where durability matters.

**Tradeoffs accepted:**
Standard tier has a per-operation cost (~$0.10 per million operations). At development and early production volumes, this is negligible. The architectural correctness of topics over multiple queues justifies the cost.

---

## ADR-006: Enums stored as strings

**Decision:** `ExpenseStatus` and `UserRole` enums are stored as `nvarchar` in the database using `HasConversion<string>()`.

**Reasoning:**
Storing enums as integers makes the database unreadable without the application code. A row showing `Status = 2` requires knowing that 2 maps to `Approved`. This complicates debugging, direct SQL queries, reporting tools, and database migrations when enum values change.

Storing as strings (`Status = 'Approved'`) makes the database self-documenting.

**Alternatives considered:**
- Integer storage: smaller column size, faster comparison. Appropriate for high-volume tables where storage and performance are critical.
- Lookup table: normalized approach, enforces referential integrity at DB level. Overkill for a small fixed set of values.

**Tradeoffs accepted:**
Slightly larger column size. String comparison marginally slower than integer comparison. Both are irrelevant at this scale.

---

## ADR-007: Single project over multi-project solution

**Decision:** Approva is a single .NET project with folder-based layer separation, not a multi-project solution (Approva.API, Approva.Application, Approva.Domain, Approva.Infrastructure).

**Reasoning:**
Multi-project solutions enforce layer boundaries at the compiler level — the Application layer genuinely cannot reference Infrastructure types. For a team project or a long-lived production codebase, this enforcement is valuable.

For a learning project built by one developer, the overhead of managing project references, separate builds, and cross-project dependency management costs more than it provides. The folder structure enforces the same conceptual boundaries with no tooling overhead.

**Alternatives considered:**
- Multi-project solution: stronger enforcement, conventional for production .NET systems, better demonstration of enterprise patterns. Would be the right choice for a team project.

**Tradeoffs accepted:**
No compiler-enforced layer separation. A developer could import Infrastructure types in a Controller directly — the folder structure is a convention, not a constraint. Acceptable for a single-developer learning project. Revisit for Tier 3 or when multiple contributors join.

---

## ADR-008: Azure SQL free tier for development database

**Decision:** Use Azure SQL free tier instead of SQL Server in Docker for the development database.

**Reasoning:**
SQL Server in Docker requires ~1.5–2GB RAM on the development machine. On an 8GB Mac running VS Code, Docker Desktop, and a .NET process simultaneously, this creates memory pressure. Azure SQL free tier offloads the database to the cloud with zero local RAM cost.

**Alternatives considered:**
- SQL Server in Docker: no internet dependency, faster queries, full SQL Server feature set. Appropriate for high-performance development environments.
- SQLite: zero setup, zero RAM cost, but different SQL dialect — EF migrations and Dapper queries may behave differently in production.
- PostgreSQL: excellent free cloud options, but different SQL dialect from production SQL Server.

**Tradeoffs accepted:**
Internet connectivity required for development. First request after 1 hour of inactivity triggers a cold start (15–30 second delay). Acceptable for development workloads.
