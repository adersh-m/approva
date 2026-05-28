# 00 — Overview

## What Approva is

Approva is an expense management and approval system. Employees submit expenses. Managers approve or reject them within their department. Finance admins oversee the full picture across the organization and trigger reimbursements.

The domain is intentionally familiar — everyone understands what an expense approval workflow does. That familiarity keeps the focus where it belongs: on the systems engineering decisions underneath, not on learning the problem space.

---

## What Approva demonstrates

Approva is a learning project built with a specific goal: moving from writing code to designing systems.

Most projects demonstrate implementation skills — can you wire up an API, can you query a database, can you build a UI. Approva is built to demonstrate something different: can you make the right architectural decisions, explain the tradeoffs, and connect technical choices to business outcomes.

Every decision in this codebase has a reason:

- **Why EF Core for writes and Dapper for reads?** Because EF's change tracking adds value on transactional operations and overhead on complex read queries. The right tool per operation, not one tool for everything.
- **Why idempotency keys on every POST endpoint?** Because networks are unreliable and users retry. A payment or expense submitted twice should create one record, not two.
- **Why a topic for expense approval but a queue for expense submission?** Because approval is a broadcast event — multiple systems need to know. Submission is a point-to-point task — one manager needs to be notified.
- **Why state machine guards at the database level?** Because application-level checks are vulnerable to race conditions. Two managers approving the same expense simultaneously should produce one approval, not two.

These aren't textbook answers. They're production patterns implemented in a real working system.

---

## The three lenses this project applies

Every engineering decision in Approva was made through three lenses simultaneously:

**Business lens** — What does the business need right now, and what will it need next? The v1 scope is deliberately constrained: no file upload, no email sending, no currency conversion. These are explicit deferrals, not omissions. The system is designed to add them without structural rework.

**Systems lens** — How should this system behave under load, failure, and scale? Idempotency protects against retries. The state machine guard protects against race conditions. Redis protects the database from repeated reads of stable data. Service Bus decouples services so a notification failure doesn't cascade into a submission failure.

**Code lens** — How do I implement this correctly and maintainably? EF for writes, Dapper for reads. No business logic in controllers. No SQL in controllers. No caching logic in controllers. Clean boundaries that make the system predictable.

---

## What this project is not

- It is not a production system. Real authentication, file storage, email delivery, and deployment pipelines are deferred to later tiers.
- It is not a showcase of every technology. The stack is deliberate and minimal — Azure SQL, Redis, Azure Service Bus, .NET 10, React. Nothing added speculatively.
- It is not a tutorial. The decisions made here are reasoned tradeoffs, not default choices.

---

## Project tiers

Approva is built in three tiers, each adding a layer of systems engineering depth:

| Tier | Focus | Status |
|---|---|---|
| Tier 1 | Caching, idempotency, messaging, concurrency, EF vs Dapper | ✅ Complete |
| Tier 2 | AFD/APIM flow, DB optimization, CORS, load balancing, full stack optimization, React frontend | 🔲 Planned |
| Tier 3 | CAP theorem in practice, business-correlated engineering, SSH operations, production hardening | 🔲 Planned |
