import { apiFetch } from './client'

export function getExpenses({ page = 1, pageSize = 20, status, departmentId } = {}) {
  const params = new URLSearchParams({ page, pageSize })
  if (status) params.set('status', status)
  if (departmentId) params.set('departmentId', departmentId)
  return apiFetch(`/api/expenses?${params}`)
}

export function submitExpense(data, idempotencyKey) {
  return apiFetch('/api/expenses', {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
    body: JSON.stringify(data),
  })
}

export function approveExpense(id, idempotencyKey) {
  return apiFetch(`/api/expenses/${id}/approve`, {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
  })
}

export function rejectExpense(id, rejectionReason, idempotencyKey) {
  return apiFetch(`/api/expenses/${id}/reject`, {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
    body: JSON.stringify({ rejectionReason }),
  })
}

export function getDashboard({ periodStart, periodEnd } = {}) {
  const params = new URLSearchParams()
  if (periodStart) params.set('periodStart', periodStart)
  if (periodEnd) params.set('periodEnd', periodEnd)
  const qs = params.toString()
  return apiFetch(`/api/expenses/dashboard${qs ? `?${qs}` : ''}`)
}
