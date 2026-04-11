import { useRouter } from 'vue-router'

const TOKEN_KEY = 'auth_token'
const ROLE_CLAIM = 'role'
const ROLE_URI_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export function parseTokenPayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    if (!part) return null
    const normalized = part.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
    return JSON.parse(atob(padded))
  } catch {
    return null
  }
}

export function getTokenRoles(payload: Record<string, unknown> | null): string[] {
  if (!payload) return []

  const roles = payload[ROLE_CLAIM] ?? payload[ROLE_URI_CLAIM]
  if (Array.isArray(roles)) {
    return roles.filter((role): role is string => typeof role === 'string')
  }

  return typeof roles === 'string' ? [roles] : []
}

export function useAuth() {
  const router = useRouter()

  function getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  }

  function setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token)
  }

  function isAuthenticated(): boolean {
    return !!getToken()
  }

  function isAdmin(): boolean {
    const token = getToken()
    if (!token) return false
    const payload = parseTokenPayload(token)
    return getTokenRoles(payload).includes('Admin')
  }

  function logout(): void {
    localStorage.removeItem(TOKEN_KEY)
    router.push('/login')
  }

  return { getToken, setToken, isAuthenticated, isAdmin, logout }
}
