import { useRouter } from 'vue-router'

const TOKEN_KEY = 'auth_token'

export function parseTokenPayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    if (!part) return null
    return JSON.parse(atob(part))
  } catch {
    return null
  }
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
    if (!payload) return false
    const roles = payload['role']
    if (Array.isArray(roles)) return roles.includes('Admin')
    return roles === 'Admin'
  }

  function logout(): void {
    localStorage.removeItem(TOKEN_KEY)
    router.push('/login')
  }

  return { getToken, setToken, isAuthenticated, isAdmin, logout }
}
