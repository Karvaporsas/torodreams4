import { useRouter } from 'vue-router'

const TOKEN_KEY = 'auth_token'

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

  function logout(): void {
    localStorage.removeItem(TOKEN_KEY)
    router.push('/login')
  }

  return { getToken, setToken, isAuthenticated, logout }
}
