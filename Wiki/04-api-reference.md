# 04 — API Reference

## Common headers

All endpoints that mutate state require:

| Header | Description |
|---|---|
| `Idempotency-Key` | UUID v4, client-generated before the request. Required on all POST endpoints. |
| `X-Employee-Id` | GUID of the acting user. Temporary — replaced by JWT claims in Tier 2. |
| `X-Employee-Role` | Role of the acting user: `Employee`, `Manager`, or `FinanceAdmin`. Temporary — replaced by JWT claims in Tier 2. |

## HTTP status codes

| Code | Meaning |
|---|---|
| 200 | Success — existing resource returned (idempotent repeat) |
| 201 | Success — new resource created |
| 400 | Validation failure or missing required header |
| 403 | Authenticated but not authorized (wrong department, wrong role) |
| 404 | Resource not found |
| 409 | Concurrency conflict or invalid state transition |
| 500 | Unhandled server error |

---

## Expenses

### POST /api/expenses
Submit a new expense.

**Required headers:** `Idempotency-Key`, `X-Employee-Id`

**Request body:**
```json
{
  "title": "Team lunch",
  "amount": 120.50,
  "currencyCode": "USD",
  "receiptUrl": "https://...",
  "categoryId": "00000000-0000-0000-0000-000000000002",
  "departmentId": "00000000-0000-0000-0000-000000000020"
}
```

| Field | Required | Constraints |
|---|---|---|
| title | Yes | Max 200 chars |
| amount | Yes | Greater than 0 |
| currencyCode | No | Default "USD" |
| receiptUrl | No | Max 2048 chars |
| categoryId | Yes | Must exist |
| departmentId | Yes | Must exist |

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "title": "Team lunch",
    "amount": 120.50,
    "currencyCode": "USD",
    "receiptUrl": null,
    "status": "Draft",
    "employeeId": "uuid",
    "categoryId": "uuid",
    "departmentId": "uuid",
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-15T10:30:00Z"
  },
  "error": null
}
```

**Response 400:** Missing Idempotency-Key or validation failure
**Response 200:** Same Idempotency-Key used again — returns stored response from Redis

---

### POST /api/expenses/{id}/approve
Approve a submitted expense.

**Required headers:** `Idempotency-Key`, `X-Employee-Id` (manager's ID)

**Request body:** None

**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "status": "Approved",
    "approvedAt": "2026-01-15T11:00:00Z",
    "approvedById": "uuid",
    ...
  },
  "error": null
}
```

**Response 403:** Manager's department does not match expense department
**Response 404:** Expense not found
**Response 409:** Expense is not in Submitted status, or concurrency conflict

---

### POST /api/expenses/{id}/reject
Reject a submitted expense.

**Required headers:** `Idempotency-Key`, `X-Employee-Id` (manager's ID)

**Request body:**
```json
{
  "rejectionReason": "Receipt missing. Please resubmit with receipt attached."
}
```

| Field | Required | Constraints |
|---|---|---|
| rejectionReason | Yes | Max 1000 chars |

**Response 200:** Updated expense with status "Rejected"
**Response 400:** Missing rejection reason
**Response 403:** Department mismatch
**Response 409:** Invalid state transition

---

### GET /api/expenses
List expenses. Results scoped by role.

**Required headers:** `X-Employee-Id`, `X-Employee-Role`

**Query parameters:**

| Param | Default | Description |
|---|---|---|
| page | 1 | Page number |
| pageSize | 20 | Items per page |
| status | — | Filter by status (Manager and FinanceAdmin only) |
| departmentId | — | Filter by department (FinanceAdmin only) |

**Role behavior:**
- `Employee` → own expenses only, status filter ignored
- `Manager` → department expenses only
- `FinanceAdmin` → all expenses, all filters apply

**Response 200:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "title": "Team lunch",
        "amount": 120.50,
        "currencyCode": "USD",
        "status": "Submitted",
        "categoryName": "Meals",
        "employeeName": "Test Employee",
        "departmentName": "Engineering",
        "submittedAt": "2026-01-15T10:45:00Z",
        "createdAt": "2026-01-15T10:30:00Z"
      }
    ],
    "totalCount": 6,
    "page": 1,
    "pageSize": 20
  },
  "error": null
}
```

---

### GET /api/expenses/dashboard
Expense summary dashboard. Scoped by role.

**Required headers:** `X-Employee-Id`, `X-Employee-Role`

**Query parameters:**

| Param | Default | Description |
|---|---|---|
| periodStart | 30 days ago | Start of reporting period |
| periodEnd | today | End of reporting period |

**Role behavior:**
- `Employee` → HTTP 403
- `Manager` → department summary only
- `FinanceAdmin` → organization-wide summary

**Response 200:**
```json
{
  "success": true,
  "data": {
    "totalExpenses": 12,
    "totalAmount": 3450.00,
    "pendingCount": 4,
    "pendingAmount": 1200.00,
    "approvedCount": 6,
    "approvedAmount": 1800.00,
    "rejectedCount": 2,
    "rejectedAmount": 450.00,
    "reimbursedCount": 0,
    "reimbursedAmount": 0.00,
    "periodStart": "2025-12-15T00:00:00Z",
    "periodEnd": "2026-01-15T00:00:00Z"
  },
  "error": null
}
```

---

## Reference data

### GET /api/reference/categories
All active expense categories.

**Required headers:** None

**Response 200:**
```json
{
  "success": true,
  "data": [
    { "id": "uuid", "name": "Travel", "description": "Travel-related expenses" },
    { "id": "uuid", "name": "Meals", "description": "Meal and entertainment expenses" },
    { "id": "uuid", "name": "Software", "description": "Software licenses and subscriptions" },
    { "id": "uuid", "name": "Equipment", "description": "Hardware and equipment purchases" },
    { "id": "uuid", "name": "Training", "description": "Training and certification expenses" }
  ],
  "error": null
}
```

First call: fetched from Azure SQL, cached in Redis for 24 hours.
Subsequent calls: served from Redis.

---

### GET /api/reference/departments
All departments.

**Required headers:** None

**Response 200:**
```json
{
  "success": true,
  "data": [
    { "id": "uuid", "name": "Engineering" },
    { "id": "uuid", "name": "Finance" },
    { "id": "uuid", "name": "Operations" }
  ],
  "error": null
}
```

Cached in Redis for 24 hours.
