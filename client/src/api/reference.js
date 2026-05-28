import { apiFetch } from './client'

export function getCategories() {
  return apiFetch('/api/reference/categories')
}

export function getDepartments() {
  return apiFetch('/api/reference/departments')
}
