import { useRouter } from 'vue-router'
import { useAuth } from './useAuth'

const API_BASE = 'http://localhost:5000'

export interface Exercise {
  id: number
  name: string
  description?: string | null
}

export function useExercises() {
  const { getToken } = useAuth()
  const router = useRouter()

  function authHeaders(): Record<string, string> {
    return { Authorization: `Bearer ${getToken()}` }
  }

  async function handleResponse<T>(res: Response): Promise<T> {
    if (res.status === 401) {
      router.push('/login')
      throw new Error('Unauthorized')
    }
    if (!res.ok) throw new Error(`Request failed: ${res.status}`)
    return res.json() as Promise<T>
  }

  async function fetchExercises(): Promise<Exercise[]> {
    const res = await fetch(`${API_BASE}/api/exercises`, {
      headers: authHeaders(),
    })
    return handleResponse<Exercise[]>(res)
  }

  async function createExercise(name: string, description?: string): Promise<Exercise> {
    const res = await fetch(`${API_BASE}/api/exercises`, {
      method: 'POST',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, description }),
    })
    return handleResponse<Exercise>(res)
  }

  async function updateExercise(id: number, name: string, description?: string): Promise<Exercise> {
    const res = await fetch(`${API_BASE}/api/exercises/${id}`, {
      method: 'PUT',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, description }),
    })
    return handleResponse<Exercise>(res)
  }

  async function deleteExercise(id: number): Promise<void> {
    const res = await fetch(`${API_BASE}/api/exercises/${id}`, {
      method: 'DELETE',
      headers: authHeaders(),
    })
    if (res.status === 401) {
      router.push('/login')
      throw new Error('Unauthorized')
    }
    if (!res.ok) throw new Error(`Request failed: ${res.status}`)
  }

  return { fetchExercises, createExercise, updateExercise, deleteExercise }
}
