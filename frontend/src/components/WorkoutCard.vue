<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import '../assets/workout-card.css'
import type { WorkoutSummary } from '../composables/useWorkout'

const props = defineProps<{
  workout: WorkoutSummary
  compact?: boolean
}>()

const to = computed(() =>
  props.workout.completedAt ? `/workouts/${props.workout.id}` : '/workouts/active'
)

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

function formatDuration(seconds?: number | null): string {
  if (!seconds) return '—'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  return h > 0 ? `${h}h ${m}m` : `${m}m`
}
</script>

<template>
  <RouterLink
    :to="to"
    class="workout-card"
    :class="{ 'workout-card--compact': compact }"
  >
    <!-- Icon -->
    <div class="workout-card__icon">
      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path fill-rule="evenodd" d="M6 3.75A2.75 2.75 0 018.75 1h2.5A2.75 2.75 0 0114 3.75v.443c.572.055 1.14.122 1.706.2C17.053 4.582 18 5.75 18 7.07v3.469c0 1.126-.694 2.191-1.83 2.54-1.952.599-4.024.921-6.17.921s-4.219-.322-6.17-.921C2.694 12.73 2 11.665 2 10.539V7.07c0-1.32.947-2.489 2.294-2.676A41.047 41.047 0 016 4.193V3.75zm6.5 0v.325a41.122 41.122 0 00-5 0V3.75c0-.69.56-1.25 1.25-1.25h2.5c.69 0 1.25.56 1.25 1.25zM10 10a1 1 0 00-1 1v.01a1 1 0 001 1h.01a1 1 0 001-1V11a1 1 0 00-1-1H10z" clip-rule="evenodd" />
        <path d="M3 15.055v-.684c.278.097.562.186.851.268C5.793 15.175 7.86 15.5 10 15.5s4.207-.325 6.149-.861c.289-.082.573-.17.851-.268v.684c0 1.329-.956 2.512-2.318 2.692a41.202 41.202 0 01-8.364 0C3.956 17.567 3 16.384 3 15.055z" />
      </svg>
    </div>

    <!-- Full layout -->
    <template v-if="!compact">
      <div class="workout-card__body">
        <div class="workout-card__date">{{ formatDate(workout.startedAt) }}</div>
        <div class="workout-card__time">{{ formatTime(workout.startedAt) }}</div>
        <div class="workout-card__pills">
          <span class="badge badge-muted">{{ formatDuration(workout.durationSeconds) }}</span>
          <span class="badge badge-muted">
            {{ workout.exerciseCount }} exercise{{ workout.exerciseCount !== 1 ? 's' : '' }}
          </span>
        </div>
      </div>
      <div class="workout-card__side">
        <span v-if="workout.completedAt" class="badge badge-accent">Completed</span>
        <span v-else class="badge badge-warning">In Progress</span>
        <svg class="workout-card__arrow" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
        </svg>
      </div>
    </template>

    <!-- Compact layout (HomeView recent panel) -->
    <template v-else>
      <div class="workout-card__body">
        <div class="workout-card__date">{{ formatDate(workout.startedAt) }}</div>
        <div class="workout-card__meta">
          <span>{{ formatDuration(workout.durationSeconds) }}</span>
          <span>{{ workout.exerciseCount }} exercise{{ workout.exerciseCount !== 1 ? 's' : '' }}</span>
        </div>
      </div>
      <div class="workout-card__side">
        <span v-if="workout.completedAt" class="badge badge-accent">Done</span>
        <span v-else class="badge badge-warning">Active</span>
      </div>
    </template>
  </RouterLink>
</template>
