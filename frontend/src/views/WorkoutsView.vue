<script setup lang="ts">
import '../assets/workouts.css'
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutSummary } from '../composables/useWorkout'
import WorkoutCard from '../components/WorkoutCard.vue'

const router = useRouter()
const { startWorkout, fetchWorkouts } = useWorkout()

const workouts = ref<WorkoutSummary[]>([])
const loading = ref(false)
const error = ref('')
const starting = ref(false)
const hasActive = ref(false)

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
    <!-- Header -->
    <div class="workouts-header">
      <h1>Workout History</h1>
      <div class="workouts-header-actions">
        <router-link v-if="hasActive" to="/workouts/active" class="btn btn-secondary">
          Continue Workout
        </router-link>
        <button v-else class="btn btn-primary" :disabled="starting" @click="handleStartWorkout">
          <span v-if="starting" class="spinner"></span>
          {{ starting ? 'Starting…' : 'Start New Workout' }}
        </button>
      </div>
    </div>

    <p v-if="error" class="workouts-error">{{ error }}</p>

    <!-- Skeleton loading -->
    <div v-if="loading" class="workouts-list">
      <div v-for="n in 4" :key="n" class="skeleton workouts-skeleton-card"></div>
    </div>

    <!-- Card list -->
    <div v-else-if="workouts.length" class="workouts-list">
      <WorkoutCard v-for="w in workouts" :key="w.id" :workout="w" />
    </div>

    <!-- Empty state -->
    <div v-else class="workouts-empty">
      <div class="workouts-empty-icon" aria-hidden="true">
        <svg viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="10" y="34" width="12" height="12" rx="3" fill="currentColor" opacity="0.3"/>
          <rect x="10" y="28" width="12" height="8" rx="2" fill="currentColor" opacity="0.5"/>
          <rect x="10" y="44" width="12" height="8" rx="2" fill="currentColor" opacity="0.5"/>
          <rect x="22" y="36" width="36" height="8" rx="4" fill="currentColor" opacity="0.6"/>
          <rect x="58" y="34" width="12" height="12" rx="3" fill="currentColor" opacity="0.3"/>
          <rect x="58" y="28" width="12" height="8" rx="2" fill="currentColor" opacity="0.5"/>
          <rect x="58" y="44" width="12" height="8" rx="2" fill="currentColor" opacity="0.5"/>
          <circle cx="40" cy="18" r="6" stroke="currentColor" stroke-width="2" opacity="0.4"/>
          <path d="M34 62 Q40 56 46 62" stroke="currentColor" stroke-width="2" stroke-linecap="round" opacity="0.4"/>
        </svg>
      </div>
      <h2 class="workouts-empty-title">No workouts yet</h2>
      <p class="workouts-empty-text">Start your first workout to begin tracking your fitness journey.</p>
      <button class="btn btn-primary" :disabled="starting" @click="handleStartWorkout">
        <span v-if="starting" class="spinner"></span>
        {{ starting ? 'Starting…' : 'Start Your First Workout' }}
      </button>
    </div>

    <!-- FAB (mobile only) -->
    <div class="workouts-fab" v-if="!loading">
      <router-link v-if="hasActive" to="/workouts/active" class="btn btn-primary workouts-fab-btn" aria-label="Continue workout">
        <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M2 10a8 8 0 1116 0A8 8 0 012 10zm6.39-2.908a.75.75 0 01.766.027l3.5 2.25a.75.75 0 010 1.262l-3.5 2.25A.75.75 0 018 12.25v-4.5a.75.75 0 01.39-.658z" clip-rule="evenodd" />
        </svg>
      </router-link>
      <button v-else class="btn btn-primary workouts-fab-btn" :disabled="starting" @click="handleStartWorkout" aria-label="Start new workout">
        <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z" />
        </svg>
      </button>
    </div>
  </div>
</template>

