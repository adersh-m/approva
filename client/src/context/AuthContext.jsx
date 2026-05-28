import { createContext, useContext, useState } from 'react'

const SEED_USERS = {
  'employee@test.com': {
    id: '00000000-0000-0000-0000-000000000010',
    name: 'Test Employee',
    email: 'employee@test.com',
    role: 'Employee',
  },
  'manager@test.com': {
    id: '00000000-0000-0000-0000-000000000011',
    name: 'Test Manager',
    email: 'manager@test.com',
    role: 'Manager',
  },
  'admin@test.com': {
    id: '00000000-0000-0000-0000-000000000012',
    name: 'Test Admin',
    email: 'admin@test.com',
    role: 'FinanceAdmin',
  },
}

const SESSION_KEY = 'approva_user'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = sessionStorage.getItem(SESSION_KEY)
    return stored ? JSON.parse(stored) : null
  })

  function login(email) {
    const found = SEED_USERS[email.toLowerCase().trim()]
    if (!found) throw new Error('User not found')
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(found))
    setUser(found)
  }

  function logout() {
    sessionStorage.removeItem(SESSION_KEY)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
