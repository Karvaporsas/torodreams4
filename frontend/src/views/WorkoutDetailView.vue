<script setup lang="ts">
import '../assets/workout-detail.css'
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useWorkout, type WorkoutDetail } from '../composables/useWorkout'

const route = useRoute()
const router = useRouter()
const { fetchWorkout } = useWorkout()

const workout = ref<WorkoutDetail | null>(null)
const loading = ref(true)
const error = ref('')

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function formatDuration(seconds?: number | null): string {
  if (!seconds) return '—'
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return s > 0 ? `${m}m ${s}s` : `${m}m`
}

const totalSets = computed(() =>
  workout.value?.exercises.reduce((sum, we) => sum + we.sets.length, 0) ?? 0,
)

const totalReps = computed(() =>
  workout.value?.exercises.reduce(
    (sum, we) => sum + we.sets.reduce((s, set) => s + set.reps, 0),
    0,
  ) ?? 0,
)

const totalVolume = computed(() => {
  const raw =
    workout.value?.exercises.reduce(
      (sum, we) => sum + we.sets.reduce((s, set) => s + set.weightKg * set.reps, 0),
      0,
    ) ?? 0
  return Math.round(raw * 10) / 10
})

onMounted(async () => {
  const id = Number(route.params.id)
  if (!id) {
    router.push('/workouts')
    return
  }
  try {
    workout.value = await fetchWorkout(id)
  } catch {
    error.value = 'Failed to load workout.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="detail-view">

    <!-- Back link (E6-T4) -->
    <router-link to="/workouts" class="detail-back-btn">
      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path fill-rule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clip-rule="evenodd" />
      </svg>
      History
    </router-link>

    <div v-if="loading" class="detail-center">
      <span class="spinner"></span>
    </div>

    <template v-else-if="workout">

      <!-- Header: date as h1, stat pills (E6-T1) -->
      <header class="detail-header">
        <h1 class="detail-date">{{ formatDate(workout.startedAt) }}</h1>
        <div class="detail-pills">
          <span class="badge badge-muted">⏱ {{ formatDuration(workout.durationSeconds) }}</span>
          <span class="badge badge-muted">
            {{ workout.exercises.length }}
            exercise{{ workout.exercises.length !== 1 ? 's' : '' }}
          </span>
          <span v-if="workout.completedAt" class="badge badge-accent">Completed</span>
          <span v-else class="badge badge-warning">In Progress</span>
        </div>
      </header>

      <!-- Summary banner (E6-T3) -->
      <div v-if="totalSets > 0" class="detail-summary card">
        <div class="stat-tile">
          <span class="stat-tile-value">{{ totalSets }}</span>
          <span class="stat-tile-label">Sets</span>
        </div>
        <div class="detail-summary-sep"></div>
        <div class="stat-tile">
          <span class="stat-tile-value">{{ totalReps }}</span>
          <span class="stat-tile-label">Reps</span>
        </div>
        <div class="detail-summary-sep"></div>
        <div class="stat-tile">
          <span class="stat-tile-value">{{ totalVolume.toLocaleString() }}</span>
          <span class="stat-tile-label">Volume (kg)</span>
        </div>
      </div>

      <!-- Exercise cards matching active-view style, read-only (E6-T2) -->
      <div v-if="workout.exercises.length" class="detail-exercises">
        <div v-for="we in workout.exercises" :key="we.id" class="exercise-card">
          <div class="exercise-card-header">
            <div class="exercise-card-title">
              <span class="exercise-card-name">{{ we.exerciseName }}</span>
              <span class="badge badge-muted">
                {{ we.sets.length }} set{{ we.sets.length !== 1 ? 's' : '' }}
              </span>
            </div>
          </div>

          <div v-if="we.sets.length" class="exercise-sets-wrapper">
            <div
              v-for="s in we.sets"
              :key="s.id"
              class="set-row"
              :class="{ 'set-row--done': s.isDone }"
            >
              <span class="set-row__num">#{{ s.setNumber }}</span>
              <span class="set-row__weight">
                {{ s.weightKg }}<span class="set-row__unit">kg</span>
              </span>
              <span class="set-row__reps">
                {{ s.reps }}<span class="set-row__unit">reps</span>
              </span>
              <span class="set-row__status" :class="{ 'set-row__status--done': s.isDone }">
                <svg v-if="s.isDone" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                </svg>
              </span>
            </div>
          </div>

          <p v-else class="detail-no-sets">No sets recorded.</p>
        </div>
      </div>

      <p v-else class="detail-hint">No exercises were added to this workout.</p>
    </template>

    <p v-else class="detail-error">{{ error }}</p>
  </div>
</template>