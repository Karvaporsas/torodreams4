<script setup lang="ts">
import '../assets/workout-active.css'
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useWorkout, getActiveWorkoutId, type WorkoutDetail, type WorkoutExercise } from '../composables/useWorkout'
import { useExercises, type Exercise } from '../composables/useExercises'

const router = useRouter()
const { fetchWorkout, addExercise, removeExercise, addSet, updateSet, deleteSet, completeWorkout } = useWorkout()
const { fetchExerciseSearch } = useExercises()

const workout = ref<WorkoutDetail | null>(null)
const loading = ref(true)
const error = ref('')
const completing = ref(false)

// ── Live timer ───────────────────────────────────────────────
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
  const h = Math.floor(elapsed.value / 3600)
  const m = Math.floor((elapsed.value % 3600) / 60)
  const s = elapsed.value % 60
  if (h > 0) {
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
  }
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
})

// ── Exercise picker ──────────────────────────────────────────
const exerciseSearch = ref('')
const exerciseCategory = ref('')
const exerciseEquipment = ref('')
const searchResults = ref<Exercise[]>([])
const searchLoading = ref(false)
const searchError = ref('')
const searchHasMore = ref(false)
const searchTotalCount = ref(0)
const addingExerciseId = ref<number | null>(null)
let searchDebounceHandle: ReturnType<typeof setTimeout> | null = null

const categoryOptions = [
  'Arms',
  'Back',
  'Carry',
  'Chest',
  'Conditioning',
  'Core',
  'Hinge',
  'Kettlebell',
  'Locomotion',
  'Lower Body Isolation',
  'Lunge',
  'Olympic Lift',
  'Plyometric',
  'Shoulders',
  'Squat',
]

const equipmentOptions = [
  'Barbell',
  'Battle Ropes',
  'Bodyweight',
  'Cable',
  'Cardio Machine',
  'Dumbbell',
  'Jump Rope',
  'Kettlebell',
  'Machine',
  'Medicine Ball',
  'Sled',
]

// ── Per-exercise add-set forms ───────────────────────────────
const setForms = ref<Record<number, { weightKg: string; reps: string }>>({})

function ensureSetForm(weId: number) {
  if (!setForms.value[weId]) {
    setForms.value[weId] = { weightKg: '', reps: '' }
  }
}

const usedExerciseIds = computed(() => new Set(workout.value?.exercises.map((we) => we.exerciseId) ?? []))

const availableSearchResults = computed(() => {
  return searchResults.value.filter((exercise) => !usedExerciseIds.value.has(exercise.id))
})

// ── Inline confirm states (E5-T6) ────────────────────────────
const confirmRemoveId = ref<number | null>(null)
const confirmComplete = ref(false)

// ── Collapse state (E5-T5) ───────────────────────────────────
const collapsedExercises = ref(new Set<number>())

function toggleCollapse(weId: number) {
  const next = new Set(collapsedExercises.value)
  if (next.has(weId)) {
    next.delete(weId)
  } else {
    next.add(weId)
  }
  collapsedExercises.value = next
}

// ── Actions ──────────────────────────────────────────────────
async function loadSearchResults() {
  searchLoading.value = true
  searchError.value = ''
  try {
    const response = await fetchExerciseSearch({
      search: exerciseSearch.value.trim() || undefined,
      category: exerciseCategory.value || undefined,
      equipment: exerciseEquipment.value || undefined,
      page: 1,
      pageSize: 8,
      sort: 'name',
    })

    searchResults.value = response.items
    searchHasMore.value = response.hasMore
    searchTotalCount.value = response.totalCount
  } catch {
    searchError.value = 'Failed to load exercises.'
  } finally {
    searchLoading.value = false
  }
}

async function handleAddExercise(exercise: Exercise) {
  if (!workout.value) return
  addingExerciseId.value = exercise.id
  error.value = ''
  try {
    const we = await addExercise(workout.value.id, exercise.id)
    workout.value.exercises.push(we)
    ensureSetForm(we.id)
    searchResults.value = searchResults.value.filter((item) => item.id !== exercise.id)
    searchTotalCount.value = Math.max(0, searchTotalCount.value - 1)
  } catch {
    error.value = 'Failed to add exercise.'
  } finally {
    addingExerciseId.value = null
  }
}

async function handleRemoveExercise(we: WorkoutExercise) {
  if (!workout.value) return
  error.value = ''
  try {
    await removeExercise(workout.value.id, we.id)
    workout.value.exercises = workout.value.exercises.filter((e) => e.id !== we.id)
    confirmRemoveId.value = null
  } catch {
    error.value = 'Failed to remove exercise.'
    confirmRemoveId.value = null
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
  completing.value = true
  error.value = ''
  try {
    await completeWorkout(workout.value.id)
    router.push('/workouts')
  } catch {
    error.value = 'Failed to complete workout.'
    completing.value = false
    confirmComplete.value = false
  }
}

onMounted(async () => {
  const id = getActiveWorkoutId()
  if (!id) {
    router.push('/workouts')
    return
  }
  try {
    workout.value = await fetchWorkout(id)
    workout.value.exercises.forEach((we) => ensureSetForm(we.id))
    startTimer(workout.value.startedAt)
    await loadSearchResults()
  } catch {
    error.value = 'Failed to load workout.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  if (timerHandle) clearInterval(timerHandle)
  if (searchDebounceHandle) clearTimeout(searchDebounceHandle)
})

watch([exerciseSearch, exerciseCategory, exerciseEquipment], () => {
  if (searchDebounceHandle) clearTimeout(searchDebounceHandle)
  searchDebounceHandle = setTimeout(() => {
    void loadSearchResults()
  }, 250)
})
</script>

<template>
  <div class="active-view">

    <div v-if="loading" class="active-view-center">
      <span class="spinner"></span>
    </div>

    <template v-else-if="workout">

      <!-- ── Sticky timer bar (E5-T1) ─────────────────────── -->
      <header class="active-topbar">
        <RouterLink to="/workouts" class="active-breadcrumb" aria-label="Back to Workouts">
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clip-rule="evenodd" />
          </svg>
          <span class="active-breadcrumb-label">Workouts</span>
        </RouterLink>

        <div class="active-timer" aria-live="off">{{ elapsedLabel }}</div>

        <!-- Complete / inline confirm (E5-T6) -->
        <div class="active-topbar-actions">
          <Transition name="fade" mode="out-in">
            <button
              v-if="!confirmComplete"
              key="complete-btn"
              class="btn btn-primary btn-sm active-complete-btn"
              :disabled="completing"
              @click="confirmComplete = true"
            >
              <span v-if="completing" class="spinner"></span>
              {{ completing ? 'Completing…' : 'Complete' }}
            </button>
            <div v-else key="confirm-complete" class="active-confirm-row">
              <span class="active-confirm-label">Finish?</span>
              <button class="btn btn-primary btn-sm" :disabled="completing" @click="handleComplete">Yes</button>
              <button class="btn btn-ghost btn-sm" @click="confirmComplete = false">No</button>
            </div>
          </Transition>
        </div>
      </header>

      <!-- ── Body ─────────────────────────────────────────── -->
      <div class="active-body">
        <p v-if="error" class="active-error">{{ error }}</p>

        <!-- Exercise picker (E5-T4) -->
        <div class="active-picker">
          <div class="active-picker-controls">
            <input
              v-model="exerciseSearch"
              type="text"
              class="input active-picker-search"
              placeholder="Search exercises by name, alias, muscle, or equipment"
            />
            <select v-model="exerciseCategory" class="input select active-picker-filter">
              <option value="">All categories</option>
              <option v-for="option in categoryOptions" :key="option" :value="option">{{ option }}</option>
            </select>
            <select v-model="exerciseEquipment" class="input select active-picker-filter">
              <option value="">All equipment</option>
              <option v-for="option in equipmentOptions" :key="option" :value="option">{{ option }}</option>
            </select>
          </div>

          <p v-if="searchError" class="active-picker-feedback active-picker-feedback--error">{{ searchError }}</p>
          <p v-else-if="searchLoading" class="active-picker-feedback">Searching exercises…</p>
          <p v-else-if="availableSearchResults.length" class="active-picker-feedback">
            Showing {{ availableSearchResults.length }} of {{ searchTotalCount }} matching exercises.
            <span v-if="searchHasMore">Refine your search to narrow the list.</span>
          </p>
          <p v-else class="active-picker-feedback">
            {{ searchTotalCount === 0 ? 'No exercises matched your search.' : 'All matching exercises are already in this workout.' }}
          </p>

          <div v-if="availableSearchResults.length" class="active-picker-results" role="listbox" aria-label="Exercise search results">
            <button
              v-for="exercise in availableSearchResults"
              :key="exercise.id"
              type="button"
              class="active-picker-result"
              :disabled="addingExerciseId === exercise.id"
              @click="handleAddExercise(exercise)"
            >
              <span class="active-picker-result-main">
                <span class="active-picker-result-name">{{ exercise.name }}</span>
                <span class="active-picker-result-meta">
                  {{ exercise.primaryMuscleGroup }} • {{ exercise.movementPattern }} • {{ exercise.primaryEquipment ?? 'Bodyweight' }}
                </span>
                <span v-if="exercise.description" class="active-picker-result-description">{{ exercise.description }}</span>
              </span>
              <span class="active-picker-result-action">
                {{ addingExerciseId === exercise.id ? 'Adding…' : '+ Add' }}
              </span>
            </button>
          </div>
        </div>

        <!-- Exercise list (E5-T2, E5-T5) -->
        <div v-if="workout.exercises.length" class="active-exercises">
          <div v-for="we in workout.exercises" :key="we.id" class="exercise-card">

            <!-- Card header -->
            <div class="exercise-card-header">
              <div class="exercise-card-title">
                <!-- Collapse toggle (E5-T5) -->
                <button
                  class="btn btn-ghost btn-icon btn-sm exercise-collapse-btn"
                  :class="{ 'exercise-collapse-btn--collapsed': collapsedExercises.has(we.id) }"
                  @click="toggleCollapse(we.id)"
                  :aria-label="collapsedExercises.has(we.id) ? 'Expand sets' : 'Collapse sets'"
                >
                  <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M5.22 8.22a.75.75 0 011.06 0L10 11.94l3.72-3.72a.75.75 0 111.06 1.06l-4.25 4.25a.75.75 0 01-1.06 0L5.22 9.28a.75.75 0 010-1.06z" clip-rule="evenodd" />
                  </svg>
                </button>
                <span class="exercise-card-name">{{ we.exerciseName }}</span>
                <span class="badge badge-muted">
                  {{ we.sets.length }} set{{ we.sets.length !== 1 ? 's' : '' }}
                </span>
              </div>

              <!-- Remove / inline confirm (E5-T6) -->
              <div class="exercise-card-actions">
                <button
                  v-if="confirmRemoveId !== we.id"
                  class="btn btn-ghost btn-sm"
                  @click="confirmRemoveId = we.id"
                >
                  Remove
                </button>
                <div v-else class="exercise-confirm-row">
                  <span class="exercise-confirm-label">Remove?</span>
                  <button class="btn btn-danger btn-sm" @click="handleRemoveExercise(we)">Yes</button>
                  <button class="btn btn-ghost btn-sm" @click="confirmRemoveId = null">No</button>
                </div>
              </div>
            </div>

            <!-- Sets — collapsible (E5-T5) -->
            <Transition name="collapse">
              <div v-if="!collapsedExercises.has(we.id)" class="exercise-sets-wrapper">

                <!-- Set rows with slide-in animation (E5-T3) -->
                <TransitionGroup name="list" tag="div" class="set-list">
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
                    <!-- Done toggle (E5-T3) -->
                    <button
                      class="set-row__check"
                      :class="{ 'set-row__check--done': s.isDone }"
                      @click="handleToggleDone(we.id, s.id, s.isDone)"
                      :aria-label="s.isDone ? 'Mark undone' : 'Mark done'"
                    >
                      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                      </svg>
                    </button>
                    <button
                      class="set-row__delete"
                      @click="handleDeleteSet(we, s.id)"
                      aria-label="Delete set"
                    >
                      <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
                      </svg>
                    </button>
                  </div>
                </TransitionGroup>

                <!-- Add set form -->
                <div class="add-set-row" v-if="setForms[we.id]">
                  <input
                    v-model="setForms[we.id]!.weightKg"
                    type="number"
                    min="0"
                    step="0.5"
                    placeholder="kg"
                    class="input add-set-input"
                  />
                  <input
                    v-model="setForms[we.id]!.reps"
                    type="number"
                    min="1"
                    placeholder="reps"
                    class="input add-set-input"
                  />
                  <button class="btn btn-accent-ghost btn-sm" @click="handleAddSet(we)">+ Set</button>
                </div>

              </div>
            </Transition>
          </div>
        </div>

        <div v-else class="active-hint">
          <p>Pick an exercise above to get started.</p>
        </div>
      </div>

    </template>

    <p v-else class="active-error">{{ error }}</p>
  </div>
</template>
