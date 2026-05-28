# Approva

> An expense management and approval system built to demonstrate production-grade systems engineering concepts — not just CRUD.

Approva is a .NET 10 + React application that models a real enterprise expense workflow: employees submit expenses, managers approve or reject them, finance admins oversee everything. The codebase is deliberately designed around systems engineering decisions — caching, idempotency, event-driven messaging, concurrency control, and role-based data access — each implemented the way they would be in a production system.

---

## Why this project exists

Most portfolio projects demonstrate that you can write code. This one demonstrates that you can design systems.

Every architectural decision in Approva has a reason. EF Core for writes, Dapper for reads. Cache-aside on reference data, never on transactional data. Idempotency keys on every mutating endpoint. State machine guards on status transitions. Topics for broadcast events, queues for point-to-point tasks. These aren't arbitrary choices — they're documented, reasoned tradeoffs that reflect how production systems actually behave.

See [Wiki/06-decisions.md](Wiki/06-decisions.md) for the full Architecture Decision Records.

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10 Web API |
| ORM (writes) | Entity Framework Core 9 |
| Data access (reads) | Dapper |
| Cache | Redis |
| Messaging | Azure Service Bus (Standard) |
| Database | Azure SQL (free tier) |
| Frontend | React (Tier 2) |

---

## Architecture overview

```
                         ┌─────────────────────────────────────┐
                         │           Approva.API                │
                         │                                      │
Browser/Client ──────────▶  IdempotencyMiddleware               │
                         │          │                           │
                         │          ▼                           │
                         │     Controllers                      │
                         │          │                           │
                         │          ▼                           │
                         │   Application/Services               │
                         │     │            │                   │
                         │     ▼            ▼                   │
                         │  EF Core      Dapper                 │
                         │  (writes)     (reads)                │
                         │     │            │                   │
                         └─────┼────────────┼───────────────────┘
                               │            │
                    ┌──────────▼──┐    ┌────▼──────────┐
                    │  Azure SQL  │    │     Redis      │
                    │  (free tier)│    │  (Docker)      │
                    └─────────────┘    └────────────────┘
                               │
                    ┌──────────▼──────────────┐
                    │   Azure Service Bus      │
                    │   Queues:                │
                    │   • expense-submitted    │
                    │   • reimbursement        │
                    │   Topics:                │
                    │   • expense-approved     │
                    │   • expense-rejected     │
                    │   • policy-violation     │
                    └─────────────────────────┘
```

---

## Expense status flow

```
Draft ──▶ Submitted ──▶ Approved ──▶ Reimbursed
                   └──▶ Rejected
```

State transitions are enforced at the database level using state machine guards — not just application logic.

---

## Key systems concepts demonstrated

| Concept | Where |
|---|---|
| Cache-aside with Redis | Reference data (categories, departments) |
| Idempotency keys | All POST endpoints via middleware |
| At-least-once messaging | Azure Service Bus queues and topics |
| Optimistic concurrency | RowVersion on Expense entity |
| State machine guard | All expense status transitions |
| EF Core for writes | Expense submission, approval, rejection |
| Dapper for reads | Expense listing, dashboard aggregations |
| Role-based data access | Employee, Manager, FinanceAdmin scoping |

---

## Quick start

See [Wiki/07-development-guide.md](Wiki/07-development-guide.md) for the full setup guide.

**Prerequisites:**
- .NET 10 SDK
- Docker Desktop
- Azure CLI (`brew install azure-cli`)
- Azure subscription (free tier resources only)

**Run in 5 steps:**

```bash
# 1. Clone
git clone https://github.com/your-actual-github-username/approva.git
cd approva

# 2. Start Redis
docker compose up -d

# 3. Configure environment
cp appsettings.Development.json.example appsettings.Development.json
# Edit with your Azure SQL and Service Bus connection strings

# 4. Apply migrations
cd Approva.API
dotnet ef database update

# 5. Run
dotnet run
```

API runs at `https://localhost:7xxx`. OpenAPI docs at `/openapi/v1.json`.

---

## Project structure

```
/Approva.API
  /Domain          — Entities, enums
  /Application     — Services, interfaces, DTOs
  /Infrastructure
    /Persistence   — EF DbContext, migrations
    /ReadRepos     — Dapper read repositories
    /Cache         — Redis cache and idempotency
    /Messaging     — Service Bus publishers
  /Controllers     — API surface
  /Middleware      — Idempotency middleware
  /Extensions      — Seed data, service registration
/Wiki              — Architecture documentation
```

---

## Documentation

| Document | Description |
|---|---|
| [00-overview.md](Wiki/00-overview.md) | What Approva is and why it was built |
| [01-architecture.md](Wiki/01-architecture.md) | System design and request lifecycle |
| [02-data-model.md](Wiki/02-data-model.md) | Entities, relationships, design decisions |
| [03-infrastructure.md](Wiki/03-infrastructure.md) | Azure SQL, Redis, Service Bus setup |
| [04-api-reference.md](Wiki/04-api-reference.md) | All endpoints documented |
| [05-concepts.md](Wiki/05-concepts.md) | Systems concepts implemented in context |
| [06-decisions.md](Wiki/06-decisions.md) | Architecture Decision Records |
| [07-development-guide.md](Wiki/07-development-guide.md) | Local setup end to end |
| [08-roadmap.md](Wiki/08-roadmap.md) | Planned Tier 2 and Tier 3 features |

---

## Current build status

| Feature | Status |
|---|---|
| Data model + migrations | ✅ Complete |
| Expense submission | ✅ Complete |
| Expense approval / rejection | ✅ Complete |
| Expense listing (role-scoped) | ✅ Complete |
| Dashboard aggregations | ✅ Complete |
| Reference data with caching | ✅ Complete |
| Idempotency middleware | ✅ Complete |
| React frontend | 🔲 Tier 2 |
| Authentication (JWT) | 🔲 Tier 2 |
| AFD + APIM integration | 🔲 Tier 2 |
| Full stack optimization | 🔲 Tier 2 |

---

## License

MIT
