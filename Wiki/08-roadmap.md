# 08 — Roadmap

Approva is built in three tiers. Each tier adds a layer of systems engineering depth to the existing codebase. Features are added to the same application — not separate projects.

---

## Tier 1 — Foundation (Complete)

**Goal:** Build a working expense management system that demonstrates core distributed systems patterns.

| Feature | Status |
|---|---|
| Data model and migrations | ✅ |
| Expense submission with idempotency | ✅ |
| Expense approval and rejection | ✅ |
| State machine guard and concurrency control | ✅ |
| Role-scoped expense listing | ✅ |
| Dashboard aggregations via Dapper | ✅ |
| Reference data with Redis cache | ✅ |
| Azure Service Bus messaging | ✅ |

**Concepts covered:** Caching strategies, idempotency, queues vs topics, concurrency, EF Core vs Dapper.

---

## Tier 2 — Systems Depth (Planned)

**Goal:** Add the infrastructure and optimization layer. Replace placeholder auth headers with real JWT authentication. Add a React frontend. Understand the full request lifecycle from browser to database.

### Authentication (JWT)
Replace `X-Employee-Id` and `X-Employee-Role` headers with real JWT Bearer token authentication. Auto-provision User records on first authenticated request from the token claims.

### React Frontend
Build the client-side application:
- Expense submission form with category/department dropdowns (served from cached reference data)
- Expense list with role-based filtering and pagination
- Manager approval dashboard
- Finance admin overview with period selector

### CORS
Configure CORS correctly for the React frontend. Understand why CORS exists (same-origin policy), where it applies (browser to API only), and how it interacts with the AFD → APIM → API chain.

### Azure Front Door + APIM
Add Azure Front Door and API Management in front of the API. Understand:
- How the request flows: browser → AFD → APIM → API
- IP whitelisting between layers
- How the original client IP is preserved in headers
- How APIM validates callers and protects the backend

### Database optimization (read-heavy)
- Add execution plan analysis for the dashboard query
- Add covering indexes based on actual query patterns
- Implement read replica routing for dashboard and listing queries
- Add query result caching for dashboard summaries (5 minute TTL)

### Load balancing
Deploy multiple API instances behind AFD. Understand:
- Round-robin vs sticky sessions
- How Redis distributed cache enables stateless API instances
- How idempotency keys work across multiple instances
- Health check configuration

### Full stack optimization
- Frontend: bundle analysis, lazy loading, code splitting
- API: response compression, output caching
- Database: N+1 audit, query plan review
- Network: HTTP/2, CDN for static assets

---

## Tier 3 — Architecture Maturity (Planned)

**Goal:** Apply the three-lens framework explicitly — business, systems, code. Understand distributed systems theory in the context of the working system.

### CAP Theorem in practice
Map Approva's data consistency decisions to CAP theorem:
- Where does Approva choose Consistency over Availability? (expense status transitions)
- Where does it choose Availability over Consistency? (cached reference data, idempotency responses)
- What would change if Approva needed to operate across regions?

### Message consumers (real implementation)
Implement the Service Bus consumers that were stubbed in Tier 1 and 2:
- Notification consumer: log manager notification intent (email sending deferred)
- Audit consumer: write AuditLog records from topic messages
- Analytics consumer: maintain denormalized summary tables for dashboard performance

### SSH and operations
- Deploy to Azure App Service via SSH
- Configure environment variables in the App Service (not appsettings files)
- Set up basic monitoring and alerting in Application Insights
- Understand the difference between development and production configuration management

### Business-correlated engineering session
Document the v1 → v2 evolution plan:
- What was deferred in v1 and why (file upload, email delivery, currency conversion)
- What the correct order of additions is and what each unlocks for the business
- How to communicate the technical tradeoffs of each addition in business terms
- What would break at 10x traffic and what the mitigation plan is

### Production hardening
- Structured logging with correlation IDs across the request lifecycle
- Global exception handling middleware
- Health check endpoints for all dependencies (SQL, Redis, Service Bus)
- Circuit breaker pattern for Service Bus publisher
- Rate limiting middleware

---

## What Approva will look like at Tier 3 completion

A fully deployed, production-hardened expense management system with:
- Real JWT authentication
- React frontend
- Azure Front Door + APIM infrastructure
- Multiple API instances behind a load balancer
- Redis distributed cache shared across instances
- Real-time audit logging via Service Bus consumers
- Application Insights monitoring
- CI/CD pipeline

Every component documented with architecture decisions, business rationale, and operational runbook.
