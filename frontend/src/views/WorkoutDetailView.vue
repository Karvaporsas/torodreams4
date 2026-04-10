<script setup lang="ts">
import '../assets/workout-detail.css'
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useWorkout, type WorkoutDetail } from '../composables/useWorkout'

const route = useRoute()
const router = useRouter()
const { fetchWorkout } = useWorkout()

const workout = ref<WorkoutDetail | null>(null)
const loading = ref(true)
const error = ref('')

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString()
}

function formatDuration(seconds?: number | null): string {
  if (!seconds) return '—'
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}m ${s}s`
}

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
    <router-link to="/workouts" class="detail-back-link">← Back to history</router-link>

    <div v-if="loading" class="detail-center">Loading…</div>

    <template v-else-if="workout">
      <h1>Workout Detail</h1>
      <div class="detail-meta">
        <span>{{ formatDate(workout.startedAt) }}</span>
        <span class="detail-meta-sep">·</span>
        <span>{{ formatDuration(workout.durationSeconds) }}</span>
      </div>

      <div v-if="workout.exercises.length" class="detail-exercises">
        <div v-for="we in workout.exercises" :key="we.id" class="detail-exercise-card">
          <h2 class="detail-exercise-name">{{ we.exerciseName }}</h2>

          <table v-if="we.sets.length" class="detail-sets-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Weight (kg)</th>
                <th>Reps</th>
                <th>Done</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in we.sets" :key="s.id" :class="{ 'set-done': s.isDone }">
                <td>{{ s.setNumber }}</td>
                <td>{{ s.weightKg }}</td>
                <td>{{ s.reps }}</td>
                <td>{{ s.isDone ? '✓' : '—' }}</td>
              </tr>
            </tbody>
          </table>

          <p v-else class="detail-no-sets">No sets recorded.</p>
        </div>
      </div>

      <p v-else class="detail-hint">No exercises were added to this workout.</p>
    </template>

    <p v-else class="detail-error">{{ error }}</p>
  </div>
</template>


