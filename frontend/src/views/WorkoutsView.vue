<script setup lang="ts">
import '../assets/workouts.css'
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
    <div class="workouts-header">
      <h1>Workout History</h1>
      <div class="workouts-header-actions">
        <router-link v-if="hasActive" to="/workouts/active" class="btn btn-secondary">
          Continue Active Workout
        </router-link>
        <button v-else class="btn btn-primary" :disabled="starting" @click="handleStartWorkout">
          {{ starting ? 'Starting…' : 'Start New Workout' }}
        </button>
      </div>
    </div>

    <p v-if="error" class="workouts-error">{{ error }}</p>
    <p v-if="loading">Loading…</p>

    <table v-if="workouts.length" class="workouts-table">
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
              class="workouts-link"
            >View</router-link>
            <router-link v-else to="/workouts/active" class="workouts-link">Continue</router-link>
          </td>
        </tr>
      </tbody>
    </table>

    <p v-else-if="!loading" class="workouts-empty">No workouts yet. Start your first one!</p>
  </div>
</template>


