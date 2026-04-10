<script setup lang="ts">
import '../assets/home.css'
import { ref, computed, onMounted } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutSummary } from '../composables/useWorkout'

const router = useRouter()
const { startWorkout, fetchWorkouts } = useWorkout()

const workouts = ref<WorkoutSummary[]>([])
const loading = ref(true)
const starting = ref(false)
const error = ref('')
const hasActive = ref(false)

// ── Stats ─────────────────────────────────────────────────

const totalWorkouts = computed(() => workouts.value.length)

const weeklyDurationLabel = computed(() => {
  const cutoff = Date.now() - 7 * 24 * 60 * 60 * 1000
  const secs = workouts.value
    .filter(w => w.completedAt && new Date(w.startedAt).getTime() >= cutoff)
    .reduce((sum, w) => sum + (w.durationSeconds ?? 0), 0)
  if (secs === 0) return '0m'
  const h = Math.floor(secs / 3600)
  const m = Math.floor((secs % 3600) / 60)
  return h > 0 ? `${h}h ${m}m` : `${m}m`
})

const streak = computed(() => {
  const completed = workouts.value
    .filter(w => w.completedAt)
    .map(w => new Date(w.startedAt).toDateString())
  if (completed.length === 0) return 0
  const uniqueDays = [...new Set(completed)]
  let count = 0
  const today = new Date()
  for (let i = 0; i < 365; i++) {
    const d = new Date(today)
    d.setDate(d.getDate() - i)
    if (uniqueDays.includes(d.toDateString())) {
      count++
    } else if (i > 0) {
      break
    }
  }
  return count
})

const recentWorkouts = computed(() => workouts.value.slice(0, 3))

// ── Actions ───────────────────────────────────────────────

async function handleStart() {
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

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })
}

function formatDuration(seconds?: number | null): string {
  if (!seconds) return '—'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  return h > 0 ? `${h}h ${m}m` : `${m}m`
}

onMounted(async () => {
  hasActive.value = getActiveWorkoutId() !== null
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
  <div class="home-view">

    <!-- Hero -->
    <section class="home-hero">
      <div class="home-greeting">
        <h1>Welcome back 👋</h1>
        <p>Ready to crush your workout?</p>
      </div>
      <div>
        <div v-if="hasActive" class="home-active-badge">
          <span class="home-active-dot"></span>
          Workout in progress
        </div>
        <RouterLink
          v-if="hasActive"
          to="/workouts/active"
          class="btn btn-primary home-cta-btn"
        >
          Continue Workout →
        </RouterLink>
        <button
          v-else
          class="btn btn-primary home-cta-btn home-cta-pulse"
          :disabled="starting"
          @click="handleStart"
        >
          <span v-if="starting" class="spinner"></span>
          {{ starting ? 'Starting…' : 'Start New Workout' }}
        </button>
        <p v-if="error" class="text-sm text-danger" style="margin-top: var(--space-2)">{{ error }}</p>
      </div>
    </section>

    <!-- Stats strip -->
    <section class="home-stats">
      <div class="home-stat-card">
        <div class="home-stat-value">{{ totalWorkouts }}</div>
        <div class="home-stat-label">Total Workouts</div>
      </div>
      <div class="home-stat-card">
        <div class="home-stat-value accent">{{ weeklyDurationLabel }}</div>
        <div class="home-stat-label">This Week</div>
      </div>
      <div class="home-stat-card">
        <div class="home-stat-value">{{ streak }} 🔥</div>
        <div class="home-stat-label">Day Streak</div>
      </div>
    </section>

    <!-- Recent workouts -->
    <section>
      <div class="home-recent-header">
        <h2>Recent Workouts</h2>
        <RouterLink to="/workouts" class="home-recent-link">View all →</RouterLink>
      </div>

      <div v-if="loading" class="home-skeleton-list">
        <div class="skeleton home-skeleton-card"></div>
        <div class="skeleton home-skeleton-card"></div>
        <div class="skeleton home-skeleton-card"></div>
      </div>

      <div v-else-if="recentWorkouts.length" class="home-recent-list">
        <component
          :is="w.completedAt ? RouterLink : 'div'"
          v-for="w in recentWorkouts"
          :key="w.id"
          :to="w.completedAt ? `/workouts/${w.id}` : '/workouts/active'"
          class="recent-workout-card"
        >
          <div class="recent-workout-icon">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M6 3.75A2.75 2.75 0 018.75 1h2.5A2.75 2.75 0 0114 3.75v.443c.572.055 1.14.122 1.706.2C17.053 4.582 18 5.75 18 7.07v3.469c0 1.126-.694 2.191-1.83 2.54-1.952.599-4.024.921-6.17.921s-4.219-.322-6.17-.921C2.694 12.73 2 11.665 2 10.539V7.07c0-1.32.947-2.489 2.294-2.676A41.047 41.047 0 016 4.193V3.75zm6.5 0v.325a41.122 41.122 0 00-5 0V3.75c0-.69.56-1.25 1.25-1.25h2.5c.69 0 1.25.56 1.25 1.25zM10 10a1 1 0 00-1 1v.01a1 1 0 001 1h.01a1 1 0 001-1V11a1 1 0 00-1-1H10z" clip-rule="evenodd"/>
              <path d="M3 15.055v-.684c.278.097.562.186.851.268C5.793 15.175 7.86 15.5 10 15.5s4.207-.325 6.149-.861c.289-.082.573-.17.851-.268v.684c0 1.329-.956 2.512-2.318 2.692a41.202 41.202 0 01-8.364 0C3.956 17.567 3 16.384 3 15.055z"/>
            </svg>
          </div>
          <div class="recent-workout-info">
            <div class="recent-workout-date">{{ formatDate(w.startedAt) }}</div>
            <div class="recent-workout-meta">
              <span>{{ formatDuration(w.durationSeconds) }}</span>
              <span>{{ w.exerciseCount }} exercise{{ w.exerciseCount !== 1 ? 's' : '' }}</span>
            </div>
          </div>
          <div class="recent-workout-status">
            <span v-if="w.completedAt" class="badge badge-accent">Done</span>
            <span v-else class="badge badge-warning">Active</span>
          </div>
        </component>
      </div>

      <div v-else class="home-recent-empty">
        No workouts yet — start your first one above!
      </div>
    </section>

  </div>
</template>

