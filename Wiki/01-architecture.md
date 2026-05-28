# 01 — Architecture

## Request lifecycle

Every request to Approva passes through the same pipeline:

```
Client Request
      │
      ▼
IdempotencyMiddleware
      │
      ├── POST request without Idempotency-Key header → HTTP 400
      │
      ├── POST request with key found in Redis → HTTP 200 (cached response)
      │
      └── POST request with new key → continue pipeline
              │
              ▼
         Controller
              │
              ▼
       Application Service
         │           │
         ▼           ▼
      EF Core      Dapper
      (writes)     (reads)
         │
         ▼
    Azure SQL
         │
         ▼ (on successful write)
   Service Bus Publisher
         │
         ├── Queue (point-to-point tasks)
         └── Topic (broadcast events)
```

---

## Layer responsibilities

**Middleware**
Runs before every request reaches a controller. Handles cross-cutting concerns. Currently: idempotency key validation and response caching.

**Controllers**
Thin. Extract inputs from headers, route params, and request body. Call the service layer. Return HTTP responses. No business logic. No SQL. No Redis calls.

**Application/Services**
Business logic lives here. Orchestrates EF Core for writes, delegates reads to read repositories, publishes messages to Service Bus after successful state changes. No direct Redis calls — cache is accessed via ICacheService interface.

**Application/Interfaces**
Contracts that decouple the application layer from infrastructure. Controllers depend on interfaces, not implementations. This makes the system testable and infrastructure-replaceable.

**Infrastructure/Persistence**
AppDbContext and EF Core configuration. All entity mappings, indexes, relationships, and concurrency tokens. Migrations live here.

**Infrastructure/ReadRepositories**
Dapper-based read repositories. Raw SQL, explicit joins, optimized for the specific read patterns of each endpoint. DTOs returned here are separate from EF entities.

**Infrastructure/Cache**
Redis interactions. RedisCacheService implements ICacheService. IdempotencyService implements IIdempotencyService. No caller above this layer touches Redis directly.

**Infrastructure/Messaging**
Azure Service Bus publishers. Encapsulates message envelope construction and sender management. Services call publishers — they don't construct ServiceBusMessage objects directly.

---

## Data flow — expense submission

```
POST /api/expenses
Headers: Idempotency-Key: {uuid}, X-Employee-Id: {guid}
Body: { title, amount, currencyCode, categoryId, departmentId }

1. IdempotencyMiddleware
   → Check Redis for key "idempotency:expenses:{uuid}"
   → Not found → continue

2. ExpensesController
   → Extract employeeId from X-Employee-Id header
   → Validate request body
   → Call IExpenseService.CreateExpenseAsync

3. ExpenseService.CreateExpenseAsync
   → Build Expense entity (Status = Draft)
   → EF Core: SaveChangesAsync → Azure SQL
   → Build ServiceBusMessage envelope
   → Publish to "expense-submitted-queue"
   → Return ExpenseResponse

4. IdempotencyMiddleware (response capture)
   → Serialize response to JSON
   → Store in Redis: key = "idempotency:expenses:{uuid}", TTL = 24hr
   → Return HTTP 201

Same request with same Idempotency-Key:
1. IdempotencyMiddleware
   → Check Redis → Found
   → Return stored response as HTTP 200
   → Controller never runs, EF never runs, Service Bus never fires
```

---

## Data flow — expense approval

```
POST /api/expenses/{id}/approve
Headers: Idempotency-Key: {uuid}, X-Employee-Id: {managerId}

1. IdempotencyMiddleware → new key → continue

2. ExpensesController → call IExpenseService.ApproveExpenseAsync

3. ExpenseService.ApproveExpenseAsync
   → Fetch manager from Users (EF) → get DepartmentId
   → Fetch expense (EF) → verify DepartmentId matches
   → If mismatch → throw UnauthorizedAccessException → HTTP 403
   → ExecuteUpdateAsync WHERE Status = 'Submitted'
   → If 0 rows affected → HTTP 409 (wrong status)
   → Catch DbUpdateConcurrencyException → HTTP 409 (race condition)
   → Publish to "expense-approved-topic"
   → Return updated ExpenseResponse

4. IdempotencyMiddleware → store response → HTTP 200

Same approval request with same key:
   → Redis hit → HTTP 200, no second DB write, no second message
```

---

## Infrastructure topology

```
┌─────────────────────────────────────────────┐
│                  Developer Mac               │
│                                             │
│  ┌──────────────┐    ┌───────────────────┐  │
│  │  Approva.API │    │  Docker Desktop   │  │
│  │  dotnet run  │    │  ┌─────────────┐  │  │
│  │              │    │  │    Redis    │  │  │
│  └──────┬───────┘    │  │ port 6379  │  │  │
│         │            │  └─────────────┘  │  │
│         │            └───────────────────┘  │
└─────────┼───────────────────────────────────┘
          │
          │ (internet)
          │
┌─────────┴─────────────────────────────────────┐
│                    Azure                        │
│                                                 │
│  ┌──────────────────┐  ┌─────────────────────┐ │
│  │   Azure SQL DB   │  │  Azure Service Bus  │ │
│  │  exp-management  │  │  exp-management-sb  │ │
│  │  free tier       │  │  Standard tier      │ │
│  │  serverless      │  │                     │ │
│  └──────────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────┘
```

---

## Caching strategy

| Data | Cached? | TTL | Reason |
|---|---|---|---|
| Expense categories | Yes | 24 hours | Shared, rarely changes |
| Departments | Yes | 24 hours | Shared, rarely changes |
| Dashboard summary | No (Tier 1) | — | Added in Tier 2 |
| User expenses | No | — | Authenticated, per-user |
| Expense status | No | — | Transactional, must be fresh |
| Idempotency results | Yes | 24 hours | Protects against retries |

Cache-aside pattern throughout. Application checks cache first, falls back to database on miss, stores result for next request.

---

## Messaging topology

| Event | Mechanism | Subscribers |
|---|---|---|
| Expense submitted | Queue | Notification service (manager email) |
| Expense approved | Topic | Notification, Audit, Analytics |
| Expense rejected | Topic | Notification, Audit |
| Reimbursement processed | Queue | Payment processor |
| Policy violation | Topic | Manager, Compliance, HR |

Queues for point-to-point tasks. Topics for broadcast events. Every consumer must be idempotent — Service Bus guarantees at-least-once delivery, not exactly-once.
