<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutSummary } from '../composables/useWorkout'

const router = useRouter()
const { startWorkout, fetchWorkouts } = useWorkout()

const workouts = ref<WorkoutSummary[]>([])
const loading = ref(false)
const error = ref('')
const starting = ref(false)
const hasActive = ref(false)

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString()
}

function formatDuration(seconds?: number | null): string {
  if (!seconds) return '—'
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}m ${s}s`
}

async function handleStartWorkout() {
  starting.value = true
  error.value = ''
  try {
    await startWorkout()
    router.push('/workouts/active')
  } catch {
    error.value = 'Failed to start workout.'
    starting.value = false
  }
}

onMounted(async () => {
  hasActive.value = getActiveWorkoutId() !== null
  loading.value = true
  try {
    workouts.value = await fetchWorkouts()
  } catch {
    error.value = 'Failed to load workouts.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="workouts-view">
    <div class="header">
      <h1>Workout History</h1>
      <div class="header-actions">
        <router-link v-if="hasActive" to="/workouts/active" class="btn btn-continue">
          Continue Active Workout
        </router-link>
        <button v-else class="btn btn-start" :disabled="starting" @click="handleStartWorkout">
          {{ starting ? 'Starting…' : 'Start New Workout' }}
        </button>
      </div>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="loading">Loading…</p>

    <table v-if="workouts.length">
      <thead>
        <tr>
          <th>Date</th>
          <th>Duration</th>
          <th>Exercises</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="w in workouts" :key="w.id">
          <td>{{ formatDate(w.startedAt) }}</td>
          <td>{{ w.completedAt ? formatDuration(w.durationSeconds) : 'In progress' }}</td>
          <td>{{ w.exerciseCount }}</td>
          <td>
            <router-link
              v-if="w.completedAt"
              :to="`/workouts/${w.id}`"
              class="link"
            >View</router-link>
            <router-link v-else to="/workouts/active" class="link">Continue</router-link>
          </td>
        </tr>
      </tbody>
    </table>

    <p v-else-if="!loading">No workouts yet. Start your first one!</p>
  </div>
</template>

<style scoped>
.workouts-view {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  gap: 1rem;
}

.header-actions {
  display: flex;
  gap: 0.5rem;
}

.btn {
  padding: 0.5rem 1.1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  color: white;
  text-decoration: none;
  display: inline-block;
}

.btn-start    { background: #42b883; }
.btn-continue { background: #e67e22; }

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.95rem;
}

th, td {
  text-align: left;
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid #e0e0e0;
}

th {
  background: #f0f0f0;
  font-weight: 600;
}

.link {
  color: #42b883;
  text-decoration: none;
  font-weight: 500;
}

.link:hover {
  text-decoration: underline;
}

.error {
  color: #c00;
  margin-bottom: 1rem;
}
</style>
