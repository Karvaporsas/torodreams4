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
  isReferencedByWorkouts: boolean
  workoutReferenceCount: number
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

export interface ExerciseSearchParams {
  search?: string
  category?: string
  movementPattern?: string
  equipment?: string
  bodyRegion?: string
  trainingStyle?: string
  sort?: 'name' | 'name-desc' | 'updated' | 'updated-desc'
  page?: number
  pageSize?: number
  includeArchived?: boolean
}

export interface ExerciseSearchResponse {
  items: Exercise[]
  page: number
  pageSize: number
  totalCount: number
  hasMore: boolean
}

export interface ExerciseImportBatch {
  id: number
  catalogVersion?: string | null
  catalogPath: string
  status: string
  triggeredBy: string
  totalExercises?: number | null
  created?: number | null
  updated?: number | null
  unchanged?: number | null
  message?: string | null
  importedAtUtc: string
}

export interface ExerciseCatalogStatus {
  catalogPath: string
  catalogFileExists: boolean
  currentCatalogVersion?: string | null
  catalogMessage?: string | null
  lastImport?: ExerciseImportBatch | null
}

export function useExercises() {
  const { getToken } = useAuth()
  const router = useRouter()

  function authHeaders(): Record<string, string> {
    return { Authorization: `Bearer ${getToken()}` }
  }

  async function getErrorMessage(res: Response): Promise<string> {
    const contentType = res.headers.get('Content-Type') ?? ''

    if (contentType.includes('application/json')) {
      const payload = (await res.json()) as {
        error?: string
        detail?: string
        title?: string
        message?: string
      }

      return payload.error ?? payload.detail ?? payload.title ?? payload.message ?? `Request failed: ${res.status}`
    }

    const text = await res.text()
    return text || `Request failed: ${res.status}`
  }

  async function handleResponse<T>(res: Response): Promise<T> {
    if (res.status === 401) {
      router.push('/login')
      throw new Error('Unauthorized')
    }
    if (!res.ok) throw new Error(await getErrorMessage(res))
    if (res.status === 204) return undefined as T
    return res.json() as Promise<T>
  }

  function buildQuery(params: ExerciseSearchParams = {}): string {
    const query = new URLSearchParams()
    if (params.search) query.set('search', params.search)
    if (params.category) query.set('category', params.category)
    if (params.movementPattern) query.set('movementPattern', params.movementPattern)
    if (params.equipment) query.set('equipment', params.equipment)
    if (params.bodyRegion) query.set('bodyRegion', params.bodyRegion)
    if (params.trainingStyle) query.set('trainingStyle', params.trainingStyle)
    if (params.sort) query.set('sort', params.sort)
    if (params.page) query.set('page', String(params.page))
    if (params.pageSize) query.set('pageSize', String(params.pageSize))
    if (params.includeArchived) query.set('includeArchived', 'true')
    const search = query.toString()
    return search ? `?${search}` : ''
  }

  async function fetchExerciseSearch(params: ExerciseSearchParams = {}): Promise<ExerciseSearchResponse> {
    const res = await fetch(`${API_BASE}/api/exercises${buildQuery(params)}`, {
      headers: authHeaders(),
    })
    return handleResponse<ExerciseSearchResponse>(res)
  }

  async function fetchExercises(includeArchived = false): Promise<Exercise[]> {
    const response = await fetchExerciseSearch({ includeArchived, pageSize: 250 })
    return response.items
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
    if (!res.ok) throw new Error(await getErrorMessage(res))
  }

  async function fetchAdminCatalogStatus(): Promise<ExerciseCatalogStatus> {
    const res = await fetch(`${API_BASE}/api/admin/exercises/catalog-status`, {
      headers: authHeaders(),
    })

    return handleResponse<ExerciseCatalogStatus>(res)
  }

  async function reimportExerciseCatalog(): Promise<ExerciseCatalogStatus> {
    const res = await fetch(`${API_BASE}/api/admin/exercises/reimport`, {
      method: 'POST',
      headers: authHeaders(),
    })

    return handleResponse<ExerciseCatalogStatus>(res)
  }

  return {
    fetchExercises,
    fetchExerciseSearch,
    fetchAdminCatalogStatus,
    reimportExerciseCatalog,
    createExercise,
    updateExercise,
    deleteExercise,
  }
}
