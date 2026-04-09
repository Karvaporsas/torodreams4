import { useRouter } from 'vue-router'
import { useAuth } from './useAuth'

const API_BASE = 'http://localhost:5000'

export interface WorkoutSummary {
  id: number
  startedAt: string
  completedAt?: string | null
  durationSeconds?: number | null
  exerciseCount: number
}

export interface WorkoutSet {
  id: number
  setNumber: number
  weightKg: number
  reps: number
  isDone: boolean
}

export interface WorkoutExercise {
  id: number
  exerciseId: number
  exerciseName: string
  order: number
  sets: WorkoutSet[]
}

export interface WorkoutDetail {
  id: number
  startedAt: string
  completedAt?: string | null
  durationSeconds?: number | null
  exercises: WorkoutExercise[]
}

const ACTIVE_KEY = 'active_workout_id'

export function getActiveWorkoutId(): number | null {
  const v = localStorage.getItem(ACTIVE_KEY)
  return v ? Number(v) : null
}

export function setActiveWorkoutId(id: number): void {
  localStorage.setItem(ACTIVE_KEY, String(id))
}

export function clearActiveWorkoutId(): void {
  localStorage.removeItem(ACTIVE_KEY)
}

export function useWorkout() {
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

  async function handleVoid(res: Response): Promise<void> {
    if (res.status === 401) {
      router.push('/login')
      throw new Error('Unauthorized')
    }
    if (!res.ok) throw new Error(`Request failed: ${res.status}`)
  }

  async function startWorkout(): Promise<WorkoutSummary> {
    const res = await fetch(`${API_BASE}/api/workouts`, {
      method: 'POST',
      headers: authHeaders(),
    })
    const workout = await handleResponse<WorkoutSummary>(res)
    setActiveWorkoutId(workout.id)
    return workout
  }

  async function fetchWorkouts(): Promise<WorkoutSummary[]> {
    const res = await fetch(`${API_BASE}/api/workouts`, {
      headers: authHeaders(),
    })
    return handleResponse<WorkoutSummary[]>(res)
  }

  async function fetchWorkout(id: number): Promise<WorkoutDetail> {
    const res = await fetch(`${API_BASE}/api/workouts/${id}`, {
      headers: authHeaders(),
    })
    return handleResponse<WorkoutDetail>(res)
  }

  async function addExercise(workoutId: number, exerciseId: number, order?: number): Promise<WorkoutExercise> {
    const res = await fetch(`${API_BASE}/api/workouts/${workoutId}/exercises`, {
      method: 'POST',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ exerciseId, order }),
    })
    return handleResponse<WorkoutExercise>(res)
  }

  async function removeExercise(workoutId: number, workoutExerciseId: number): Promise<void> {
    const res = await fetch(`${API_BASE}/api/workouts/${workoutId}/exercises/${workoutExerciseId}`, {
      method: 'DELETE',
      headers: authHeaders(),
    })
    return handleVoid(res)
  }

  async function addSet(workoutId: number, workoutExerciseId: number, weightKg: number, reps: number): Promise<WorkoutSet> {
    const res = await fetch(`${API_BASE}/api/workouts/${workoutId}/exercises/${workoutExerciseId}/sets`, {
      method: 'POST',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ weightKg, reps }),
    })
    return handleResponse<WorkoutSet>(res)
  }

  async function updateSet(setId: number, updates: { weightKg?: number; reps?: number; isDone?: boolean }): Promise<WorkoutSet> {
    const res = await fetch(`${API_BASE}/api/workouts/sets/${setId}`, {
      method: 'PATCH',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    })
    return handleResponse<WorkoutSet>(res)
  }

  async function deleteSet(setId: number): Promise<void> {
    const res = await fetch(`${API_BASE}/api/workouts/sets/${setId}`, {
      method: 'DELETE',
      headers: authHeaders(),
    })
    return handleVoid(res)
  }

  async function completeWorkout(id: number): Promise<WorkoutSummary> {
    const res = await fetch(`${API_BASE}/api/workouts/${id}/complete`, {
      method: 'PATCH',
      headers: authHeaders(),
    })
    const workout = await handleResponse<WorkoutSummary>(res)
    clearActiveWorkoutId()
    return workout
  }

  return {
    startWorkout,
    fetchWorkouts,
    fetchWorkout,
    addExercise,
    removeExercise,
    addSet,
    updateSet,
    deleteSet,
    completeWorkout,
  }
}
