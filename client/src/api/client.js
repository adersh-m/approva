const SESSION_KEY = 'approva_user'

function getUser() {
  const stored = sessionStorage.getItem(SESSION_KEY)
  return stored ? JSON.parse(stored) : null
}

export async function apiFetch(path, options = {}) {
  const user = getUser()

  const headers = {
    'Content-Type': 'application/json',
    ...(user && {
      'X-Employee-Id': user.id,
      'X-Employee-Role': user.role,
    }),
    ...options.headers,
  }

  const response = await fetch(path, { ...options, headers })
  const body = await response.json().catch(() => null)

  if (!response.ok) {
    const message = body?.error ?? `Request failed with status ${response.status}`
    const err = new Error(message)
    err.status = response.status
    throw err
  }

  return body
}
