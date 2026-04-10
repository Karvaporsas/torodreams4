<script setup lang="ts">
import '../assets/home.css'
import { ref, computed, onMounted } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutSummary } from '../composables/useWorkout'
import WorkoutCard from '../components/WorkoutCard.vue'

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
        <WorkoutCard
          v-for="w in recentWorkouts"
          :key="w.id"
          :workout="w"
          compact
        />
      </div>

      <div v-else class="home-recent-empty">
        No workouts yet — start your first one above!
      </div>
    </section>

  </div>
</template>

