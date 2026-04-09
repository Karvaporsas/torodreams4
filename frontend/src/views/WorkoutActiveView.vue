<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutDetail, type WorkoutExercise } from '../composables/useWorkout'
import { useExercises, type Exercise } from '../composables/useExercises'

const router = useRouter()
const { fetchWorkout, addExercise, removeExercise, addSet, updateSet, deleteSet, completeWorkout } = useWorkout()
const { fetchExercises } = useExercises()

const workout = ref<WorkoutDetail | null>(null)
const exercises = ref<Exercise[]>([])
const loading = ref(true)
const error = ref('')
const completing = ref(false)

// Live timer
const elapsed = ref(0)
let timerHandle: ReturnType<typeof setInterval> | null = null

function startTimer(startedAt: string) {
  const update = () => {
    elapsed.value = Math.floor((Date.now() - new Date(startedAt).getTime()) / 1000)
  }
  update()
  timerHandle = setInterval(update, 1000)
}

const elapsedLabel = computed(() => {
  const m = Math.floor(elapsed.value / 60)
  const s = elapsed.value % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
})

// Exercise picker
const selectedExerciseId = ref<number | ''>('')

// Per-exercise add-set forms: keyed by workoutExercise.id
const setForms = ref<Record<number, { weightKg: string; reps: string }>>({})

function ensureSetForm(weId: number) {
  if (!setForms.value[weId]) {
    setForms.value[weId] = { weightKg: '', reps: '' }
  }
}

// Available exercises not yet in the workout
const availableExercises = computed(() => {
  if (!workout.value) return exercises.value
  const used = new Set(workout.value.exercises.map((we) => we.exerciseId))
  return exercises.value.filter((e) => !used.has(e.id))
})

async function handleAddExercise() {
  if (!workout.value || selectedExerciseId.value === '') return
  error.value = ''
  try {
    const we = await addExercise(workout.value.id, Number(selectedExerciseId.value))
    workout.value.exercises.push(we)
    ensureSetForm(we.id)
    selectedExerciseId.value = ''
  } catch {
    error.value = 'Failed to add exercise.'
  }
}

async function handleRemoveExercise(we: WorkoutExercise) {
  if (!workout.value) return
  if (!confirm(`Remove "${we.exerciseName}" and all its sets?`)) return
  error.value = ''
  try {
    await removeExercise(workout.value.id, we.id)
    workout.value.exercises = workout.value.exercises.filter((e) => e.id !== we.id)
  } catch {
    error.value = 'Failed to remove exercise.'
  }
}

async function handleAddSet(we: WorkoutExercise) {
  if (!workout.value) return
  ensureSetForm(we.id)
  const form = setForms.value[we.id]!
  const weightKg = parseFloat(form.weightKg)
  const reps = parseInt(form.reps)
  if (isNaN(weightKg) || isNaN(reps) || reps < 1) return
  error.value = ''
  try {
    const set = await addSet(workout.value.id, we.id, weightKg, reps)
    we.sets.push(set)
    form.weightKg = ''
    form.reps = ''
  } catch {
    error.value = 'Failed to add set.'
  }
}

async function handleToggleDone(weId: number, setId: number, current: boolean) {
  error.value = ''
  try {
    const updated = await updateSet(setId, { isDone: !current })
    const we = workout.value?.exercises.find((e) => e.id === weId)
    if (we) {
      const idx = we.sets.findIndex((s) => s.id === setId)
      if (idx !== -1) we.sets[idx] = updated
    }
  } catch {
    error.value = 'Failed to update set.'
  }
}

async function handleDeleteSet(we: WorkoutExercise, setId: number) {
  error.value = ''
  try {
    await deleteSet(setId)
    we.sets = we.sets.filter((s) => s.id !== setId)
  } catch {
    error.value = 'Failed to delete set.'
  }
}

async function handleComplete() {
  if (!workout.value) return
  if (!confirm('Complete this workout?')) return
  completing.value = true
  error.value = ''
  try {
    await completeWorkout(workout.value.id)
    router.push('/workouts')
  } catch {
    error.value = 'Failed to complete workout.'
    completing.value = false
  }
}

onMounted(async () => {
  const id = getActiveWorkoutId()
  if (!id) {
    router.push('/workouts')
    return
  }
  try {
    const [w, ex] = await Promise.all([fetchWorkout(id), fetchExercises()])
    workout.value = w
    exercises.value = ex
    workout.value.exercises.forEach((we) => ensureSetForm(we.id))
    startTimer(w.startedAt)
  } catch {
    error.value = 'Failed to load workout.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  if (timerHandle) clearInterval(timerHandle)
})
</script>

<template>
  <div class="active-view">
    <div v-if="loading" class="center">Loading…</div>

    <template v-else-if="workout">
      <div class="header">
        <div>
          <h1>Active Workout</h1>
          <div class="timer">{{ elapsedLabel }}</div>
        </div>
        <button class="btn btn-complete" :disabled="completing" @click="handleComplete">
          {{ completing ? 'Completing…' : 'Complete Workout' }}
        </button>
      </div>

      <p v-if="error" class="error">{{ error }}</p>

      <!-- Exercise picker -->
      <div class="add-exercise-row">
        <select v-model="selectedExerciseId">
          <option value="">— pick an exercise —</option>
          <option v-for="e in availableExercises" :key="e.id" :value="e.id">{{ e.name }}</option>
        </select>
        <button class="btn btn-add" :disabled="selectedExerciseId === ''" @click="handleAddExercise">
          Add Exercise
        </button>
      </div>

      <!-- Exercise list -->
      <div v-if="workout.exercises.length" class="exercises">
        <div v-for="we in workout.exercises" :key="we.id" class="exercise-card">
          <div class="exercise-header">
            <span class="exercise-name">{{ we.exerciseName }}</span>
            <button class="btn btn-danger btn-sm" @click="handleRemoveExercise(we)">Remove</button>
          </div>

          <!-- Sets table -->
          <table v-if="we.sets.length" class="sets-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Weight (kg)</th>
                <th>Reps</th>
                <th>Done</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in we.sets" :key="s.id" :class="{ done: s.isDone }">
                <td>{{ s.setNumber }}</td>
                <td>{{ s.weightKg }}</td>
                <td>{{ s.reps }}</td>
                <td>
                  <input
                    type="checkbox"
                    :checked="s.isDone"
                    @change="handleToggleDone(we.id, s.id, s.isDone)"
                  />
                </td>
                <td>
                  <button class="btn btn-danger btn-xs" @click="handleDeleteSet(we, s.id)">✕</button>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Add set form -->
          <div class="add-set-row" v-if="setForms[we.id]">
            <input
              v-model="setForms[we.id]!.weightKg"
              type="number"
              min="0"
              step="0.5"
              placeholder="kg"
            />
            <input
              v-model="setForms[we.id]!.reps"
              type="number"
              min="1"
              placeholder="reps"
            />
            <button class="btn btn-add btn-sm" @click="handleAddSet(we)">Add Set</button>
          </div>
        </div>
      </div>

      <p v-else class="hint">Add your first exercise above.</p>
    </template>

    <p v-else class="error">{{ error }}</p>
  </div>
</template>

<style scoped>
.active-view {
  max-width: 720px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.center {
  text-align: center;
  padding: 3rem;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
  gap: 1rem;
}

h1 {
  margin: 0 0 0.25rem;
}

.timer {
  font-size: 2rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: #42b883;
}

.add-exercise-row {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.add-exercise-row select {
  flex: 1;
  padding: 0.45rem 0.75rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.95rem;
}

.exercises {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.exercise-card {
  border: 1px solid #ddd;
  border-radius: 6px;
  padding: 1rem;
  background: #fafafa;
}

.exercise-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.exercise-name {
  font-weight: 600;
  font-size: 1rem;
}

.sets-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
  margin-bottom: 0.75rem;
}

.sets-table th,
.sets-table td {
  text-align: left;
  padding: 0.35rem 0.5rem;
  border-bottom: 1px solid #eee;
}

.sets-table th {
  font-weight: 600;
  color: #555;
}

.sets-table tr.done td {
  color: #42b883;
  text-decoration: line-through;
}

.sets-table tr.done td:last-child {
  text-decoration: none;
}

.add-set-row {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.add-set-row input {
  width: 80px;
  padding: 0.35rem 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.9rem;
}

.hint {
  color: #888;
  font-style: italic;
}

/* Buttons */
.btn {
  padding: 0.45rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  color: white;
  font-size: 0.9rem;
}
.btn-sm  { padding: 0.3rem 0.7rem; font-size: 0.85rem; }
.btn-xs  { padding: 0.15rem 0.4rem; font-size: 0.8rem; }

.btn-complete { background: #42b883; }
.btn-add      { background: #2980b9; }
.btn-danger   { background: #c0392b; }

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #c00;
  margin-bottom: 1rem;
}
</style>
