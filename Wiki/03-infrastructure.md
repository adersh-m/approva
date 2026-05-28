# 03 — Infrastructure

## Overview

Approva uses three infrastructure components:

| Component | Technology | Where |
|---|---|---|
| Database | Azure SQL free tier | Azure cloud |
| Cache | Redis | Docker (local) |
| Messaging | Azure Service Bus Standard | Azure cloud |

This split is intentional. Azure SQL and Service Bus run in the cloud to eliminate local installation complexity and provide production-compatible behavior. Redis runs locally in Docker because there is no free cloud Redis tier worth using for local development, and the Docker image is 50MB with zero configuration.

---

## Azure SQL

**Why Azure SQL free tier?**
SQL Server on Mac requires Docker with ~2GB RAM overhead. Azure SQL free tier eliminates that — zero local RAM cost, production-compatible SQL Server dialect, EF migrations work identically.

**Free tier constraints:**
- 32GB storage
- 100,000 vCore seconds compute per month
- Serverless — pauses after 1 hour of inactivity
- First request after a pause takes 15–30 seconds (cold start)

For development workloads these constraints are irrelevant. Cold start is the only noticeable effect — the first API call after a break feels slow. Subsequent calls are normal.

**Authentication:**
SQL authentication (username + password) for the development connection string. Azure AD authentication is the correct choice for production — but token expiry every 60 minutes makes it impractical for local development without additional tooling.

**Connection string format:**
```
Server={server}.database.windows.net;
Database={database};
User Id={username};
Password={password};
TrustServerCertificate=True
```

**Firewall:**
Azure SQL blocks all connections by default. Your development machine's IP must be whitelisted in the server's firewall rules. If your IP changes (common with home ISPs), update the rule.

---

## Redis

**Why Redis in Docker?**
No free cloud Redis tier is suitable for local development. The Docker image is 50MB, starts in seconds, and requires no configuration.

**Start Redis:**
```bash
docker compose up redis -d
```

**Verify Redis is running:**
```bash
docker exec expense-redis redis-cli ping
# Returns: PONG
```

**Inspect cache keys during development:**
```bash
docker exec expense-redis redis-cli keys "*"
```

**What Approva stores in Redis:**

| Key pattern | Content | TTL |
|---|---|---|
| `idempotency:expenses:{uuid}` | Serialized API response | 24 hours |
| `idempotency:expenses:{id}:approve:{uuid}` | Serialized approval response | 24 hours |
| `cache:categories:all` | Serialized category list | 24 hours |
| `cache:departments:all` | Serialized department list | 24 hours |

**Eviction policy:** LRU (Least Recently Used). When Redis memory is full, the least recently accessed keys are evicted first.

---

## Azure Service Bus

**Why Standard tier?**
Basic tier supports queues only. Topics (pub/sub) require Standard tier. Approva uses both — queues for point-to-point tasks, topics for broadcast events. Standard tier is required.

**Cost at development volumes:**
Standard tier charges per operation. At development usage (hundreds of messages total), the cost is effectively $0.00 per month.

**Provisioned queues:**

| Queue | Max delivery count | Purpose |
|---|---|---|
| `expense-submitted-queue` | 5 | Manager notification on submission |
| `reimbursement-queue` | 5 | Payment processor trigger |

**Provisioned topics and subscriptions:**

| Topic | Subscriptions | Purpose |
|---|---|---|
| `expense-approved-topic` | notification-sub, audit-sub, analytics-sub | Broadcast approval event |
| `expense-rejected-topic` | notification-sub, audit-sub | Broadcast rejection event |
| `policy-violation-topic` | manager-sub, compliance-sub | Broadcast policy violation |

**Dead letter queue:**
Every queue and subscription has a dead letter queue. Messages that fail processing beyond the max delivery count are moved here automatically. Monitor the DLQ in Azure Portal during development to catch consumer bugs.

**Connection string format:**
```
Endpoint=sb://{namespace}.servicebus.windows.net/;
SharedAccessKeyName=RootManageSharedAccessKey;
SharedAccessKey={key}
```

---

## Environment configuration

**appsettings.Development.json** (never commit this file):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...database.windows.net;Database=...;User Id=...;Password=...;TrustServerCertificate=True",
    "Redis": "localhost:6379",
    "ServiceBus": "Endpoint=sb://....servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
  }
}
```

**appsettings.Development.json is in .gitignore.** It contains credentials and must never be committed to source control. Use `appsettings.Development.json.example` (committed, no real values) as the template.

---

## docker-compose.yml

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

Start: `docker compose up -d`
Stop: `docker compose down`

Only Redis runs in Docker. Azure SQL and Service Bus are cloud-hosted.

---

## RAM requirements

| Component | RAM usage |
|---|---|
| macOS | ~2–3GB |
| VS Code | ~300–500MB |
| Docker Desktop (Redis only) | ~300MB |
| Redis container | ~50MB |
| .NET API process | ~200–300MB |
| Total | ~3–4GB |

Comfortable on 8GB with Docker Desktop RAM cap set to 2GB (Docker Desktop → Settings → Resources → Memory).
