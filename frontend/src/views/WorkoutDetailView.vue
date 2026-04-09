<script setup lang="ts">
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
    <router-link to="/workouts" class="back-link">← Back to history</router-link>

    <div v-if="loading" class="center">Loading…</div>

    <template v-else-if="workout">
      <h1>Workout Detail</h1>
      <div class="meta">
        <span>{{ formatDate(workout.startedAt) }}</span>
        <span class="sep">·</span>
        <span>{{ formatDuration(workout.durationSeconds) }}</span>
      </div>

      <div v-if="workout.exercises.length" class="exercises">
        <div v-for="we in workout.exercises" :key="we.id" class="exercise-card">
          <h2 class="exercise-name">{{ we.exerciseName }}</h2>

          <table v-if="we.sets.length" class="sets-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Weight (kg)</th>
                <th>Reps</th>
                <th>Done</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in we.sets" :key="s.id" :class="{ done: s.isDone }">
                <td>{{ s.setNumber }}</td>
                <td>{{ s.weightKg }}</td>
                <td>{{ s.reps }}</td>
                <td>{{ s.isDone ? '✓' : '—' }}</td>
              </tr>
            </tbody>
          </table>

          <p v-else class="no-sets">No sets recorded.</p>
        </div>
      </div>

      <p v-else class="hint">No exercises were added to this workout.</p>
    </template>

    <p v-else class="error">{{ error }}</p>
  </div>
</template>

<style scoped>
.detail-view {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.back-link {
  display: inline-block;
  margin-bottom: 1.25rem;
  color: #42b883;
  text-decoration: none;
  font-size: 0.9rem;
}

.back-link:hover {
  text-decoration: underline;
}

h1 {
  margin-bottom: 0.25rem;
}

.meta {
  color: #666;
  font-size: 0.95rem;
  margin-bottom: 1.5rem;
}

.sep {
  margin: 0 0.4rem;
}

.center {
  text-align: center;
  padding: 3rem;
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

.exercise-name {
  font-size: 1rem;
  font-weight: 600;
  margin: 0 0 0.75rem;
}

.sets-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
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
}

.no-sets,
.hint {
  color: #888;
  font-style: italic;
  margin: 0;
}

.error {
  color: #c00;
}
</style>
