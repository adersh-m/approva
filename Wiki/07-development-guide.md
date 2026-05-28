# 07 — Development Guide

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0+ | https://dot.net/download |
| Docker Desktop | Latest | https://docker.com/products/docker-desktop |
| Azure CLI | Latest | `brew install azure-cli` (Mac) |
| Git | Any | Included on Mac |

You also need:
- An Azure subscription (free account works)
- Azure SQL Database (free tier) — see setup below
- Azure Service Bus namespace (Standard tier) — see setup below

---

## 1. Clone the repository

```bash
git clone https://github.com/yourusername/approva.git
cd approva
```

---

## 2. Start Redis

Redis runs locally in Docker. Start it with:

```bash
docker compose up -d
```

Verify it's running:

```bash
docker exec expense-redis redis-cli ping
# Expected: PONG
```

---

## 3. Set up Azure SQL

**Create the database (if not already done):**

1. Azure Portal → Create a resource → SQL Database
2. Create a new server if needed
3. Pricing tier → Apply free offer (or select Serverless)
4. Authentication → SQL authentication → set username and password
5. Create

**Configure firewall:**

Azure Portal → your SQL server → Networking → Add your client IP → Save

**Note your connection string:**
```
Server={server}.database.windows.net;Database={database};User Id={username};Password={password};TrustServerCertificate=True
```

---

## 4. Set up Azure Service Bus

**Create the namespace (if not already done):**

1. Azure Portal → Create a resource → Service Bus
2. Pricing tier → **Standard** (required for topics)
3. Create

**Create queues:**
- `expense-submitted-queue` (max delivery count: 5)
- `reimbursement-queue` (max delivery count: 5)

**Create topics and subscriptions:**
- `expense-approved-topic` → subscriptions: `notification-sub`, `audit-sub`, `analytics-sub`
- `expense-rejected-topic` → subscriptions: `notification-sub`, `audit-sub`
- `policy-violation-topic` → subscriptions: `manager-sub`, `compliance-sub`

**Get connection string:**
Namespace → Shared access policies → RootManageSharedAccessKey → Primary Connection String

---

## 5. Configure environment

Copy the example config:

```bash
cd Approva.API
cp appsettings.Development.json.example appsettings.Development.json
```

Edit `appsettings.Development.json` with your actual values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER.database.windows.net;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True",
    "Redis": "localhost:6379",
    "ServiceBus": "Endpoint=sb://YOUR_NAMESPACE.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY"
  }
}
```

**Never commit `appsettings.Development.json`.** It is in `.gitignore`.

---

## 6. Apply database migrations

```bash
cd Approva.API
dotnet ef database update
```

This creates all tables and inserts seed data:
- 3 departments: Engineering, Finance, Operations
- 5 categories: Travel, Meals, Software, Equipment, Training
- 3 users: employee@test.com, manager@test.com, admin@test.com

---

## 7. Run the API

```bash
dotnet run
```

The API starts at `https://localhost:{port}`. Check the console output for the exact port.

OpenAPI document available at: `https://localhost:{port}/openapi/v1.json`

---

## 8. Test the API

Use the `.http` file in the project root (`Approva.http`) with VS Code REST Client, or test manually with curl.

**Get reference data (no auth required):**
```bash
curl https://localhost:{port}/api/reference/categories -k
```

**Submit an expense:**
```bash
curl -X POST https://localhost:{port}/api/expenses \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: $(uuidgen)" \
  -H "X-Employee-Id: 00000000-0000-0000-0000-000000000010" \
  -d '{
    "title": "Team lunch",
    "amount": 75.00,
    "categoryId": "00000000-0000-0000-0000-000000000002",
    "departmentId": "00000000-0000-0000-0000-000000000020"
  }' -k
```

**Approve an expense (use the ID from the submission response):**
```bash
curl -X POST https://localhost:{port}/api/expenses/{id}/approve \
  -H "Idempotency-Key: $(uuidgen)" \
  -H "X-Employee-Id: 00000000-0000-0000-0000-000000000011" \
  -k
```

**Seed user IDs for testing:**

| Role | Email | ID |
|---|---|---|
| Employee | employee@test.com | 00000000-0000-0000-0000-000000000010 |
| Manager | manager@test.com | 00000000-0000-0000-0000-000000000011 |
| FinanceAdmin | admin@test.com | 00000000-0000-0000-0000-000000000012 |

---

## Common issues

**Azure SQL connection timeout on first request**
The free tier database pauses after 1 hour of inactivity. The first request after a pause takes 15–30 seconds to wake it up. Subsequent requests are normal.

**Firewall blocking connection**
Your IP may have changed since you added the firewall rule. Check your current IP (`curl ifconfig.me`) and update the Azure SQL firewall rule if it doesn't match.

**Redis connection refused**
Docker container may not be running. Run `docker compose up -d` and verify with `docker ps`.

**Service Bus authentication error**
Check that the connection string in `appsettings.Development.json` matches the Primary Connection String from Azure Portal → your namespace → Shared access policies → RootManageSharedAccessKey.

---

## Useful commands

```bash
# Start Redis
docker compose up -d

# Stop Redis
docker compose down

# Check Redis keys
docker exec expense-redis redis-cli keys "*"

# Clear Redis cache
docker exec expense-redis redis-cli flushall

# Add a new migration
dotnet ef migrations add {MigrationName}

# Apply migrations
dotnet ef database update

# Revert last migration
dotnet ef migrations remove

# Build
dotnet build

# Run tests (Tier 2)
dotnet test
```
