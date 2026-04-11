import { useRouter } from 'vue-router'
import { useAuth } from './useAuth'

const API_BASE = 'http://localhost:5000'

export interface Exercise {
  id: number
  slug: string
  name: string
  description?: string | null
  category: string
  bodyRegion: string
  movementPattern: string
  primaryMuscleGroup: string
  primaryEquipment?: string | null
  secondaryEquipment?: string | null
  difficultyLevel: string
  trainingStyle: string
  isUnilateral: boolean
  isArchived: boolean
  searchTerms: string
  createdAtUtc: string
  updatedAtUtc: string
  aliases: string[]
  secondaryMuscleGroups: string[]
}

export interface ExerciseInput {
  slug?: string
  name: string
  description?: string
  category?: string
  bodyRegion?: string
  movementPattern?: string
  primaryMuscleGroup?: string
  primaryEquipment?: string
  secondaryEquipment?: string
  difficultyLevel?: string
  trainingStyle?: string
  isUnilateral?: boolean
  isArchived?: boolean
  aliases?: string[]
  secondaryMuscleGroups?: string[]
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

  async function fetchExercises(includeArchived = false): Promise<Exercise[]> {
    const res = await fetch(`${API_BASE}/api/exercises?includeArchived=${includeArchived}`, {
      headers: authHeaders(),
    })
    return handleResponse<Exercise[]>(res)
  }

  async function createExercise(input: ExerciseInput): Promise<Exercise> {
    const res = await fetch(`${API_BASE}/api/exercises`, {
      method: 'POST',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    })
    return handleResponse<Exercise>(res)
  }

  async function updateExercise(id: number, input: ExerciseInput): Promise<Exercise> {
    const res = await fetch(`${API_BASE}/api/exercises/${id}`, {
      method: 'PUT',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
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
