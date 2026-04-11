<script setup lang="ts">
import '../assets/admin-exercises.css'
import { ref, onMounted } from 'vue'
import { useExercises, type Exercise, type ExerciseInput } from '../composables/useExercises'

const { fetchExercises, createExercise, updateExercise, deleteExercise } = useExercises()

const exercises = ref<Exercise[]>([])
const loading = ref(false)
const error = ref('')

function createEmptyForm(): ExerciseInput {
  return {
    slug: '',
    name: '',
    description: '',
    category: 'General',
    bodyRegion: 'Full Body',
    movementPattern: 'General',
    primaryMuscleGroup: 'General',
    primaryEquipment: '',
    secondaryEquipment: '',
    difficultyLevel: 'Beginner',
    trainingStyle: 'Strength',
    isUnilateral: false,
    isArchived: false,
    aliases: [],
    secondaryMuscleGroups: [],
  }
}

const newExercise = ref<ExerciseInput>(createEmptyForm())
const newAliases = ref('')
const newSecondaryMuscles = ref('')
const adding = ref(false)

const editingId = ref<number | null>(null)
const editExercise = ref<ExerciseInput>(createEmptyForm())
const editAliases = ref('')
const editSecondaryMuscles = ref('')

function normalizeCsv(value: string): string[] {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter((item, index, items) => item.length > 0 && items.findIndex((candidate) => candidate.toLowerCase() === item.toLowerCase()) === index)
}

function buildPayload(form: ExerciseInput, aliasesText: string, musclesText: string): ExerciseInput {
  return {
    slug: form.slug?.trim() || undefined,
    name: form.name.trim(),
    description: form.description?.trim() || undefined,
    category: form.category?.trim() || undefined,
    bodyRegion: form.bodyRegion?.trim() || undefined,
    movementPattern: form.movementPattern?.trim() || undefined,
    primaryMuscleGroup: form.primaryMuscleGroup?.trim() || undefined,
    primaryEquipment: form.primaryEquipment?.trim() || undefined,
    secondaryEquipment: form.secondaryEquipment?.trim() || undefined,
    difficultyLevel: form.difficultyLevel?.trim() || undefined,
    trainingStyle: form.trainingStyle?.trim() || undefined,
    isUnilateral: !!form.isUnilateral,
    isArchived: !!form.isArchived,
    aliases: normalizeCsv(aliasesText),
    secondaryMuscleGroups: normalizeCsv(musclesText),
  }
}

async function loadExercises() {
  loading.value = true
  error.value = ''
  try {
    exercises.value = await fetchExercises(true)
  } catch {
    error.value = 'Failed to load exercises.'
  } finally {
    loading.value = false
  }
}

async function handleAdd() {
  if (!newExercise.value.name.trim()) return
  adding.value = true
  error.value = ''
  try {
    const created = await createExercise(buildPayload(newExercise.value, newAliases.value, newSecondaryMuscles.value))
    exercises.value.push(created)
    exercises.value.sort((a, b) => a.name.localeCompare(b.name))
    newExercise.value = createEmptyForm()
    newAliases.value = ''
    newSecondaryMuscles.value = ''
  } catch {
    error.value = 'Failed to create exercise.'
  } finally {
    adding.value = false
  }
}

function startEdit(ex: Exercise) {
  editingId.value = ex.id
  editExercise.value = {
    slug: ex.slug,
    name: ex.name,
    description: ex.description ?? '',
    category: ex.category,
    bodyRegion: ex.bodyRegion,
    movementPattern: ex.movementPattern,
    primaryMuscleGroup: ex.primaryMuscleGroup,
    primaryEquipment: ex.primaryEquipment ?? '',
    secondaryEquipment: ex.secondaryEquipment ?? '',
    difficultyLevel: ex.difficultyLevel,
    trainingStyle: ex.trainingStyle,
    isUnilateral: ex.isUnilateral,
    isArchived: ex.isArchived,
    aliases: [...ex.aliases],
    secondaryMuscleGroups: [...ex.secondaryMuscleGroups],
  }
  editAliases.value = ex.aliases.join(', ')
  editSecondaryMuscles.value = ex.secondaryMuscleGroups.join(', ')
}

function cancelEdit() {
  editingId.value = null
  editExercise.value = createEmptyForm()
  editAliases.value = ''
  editSecondaryMuscles.value = ''
}

async function handleUpdate(id: number) {
  if (!editExercise.value.name.trim()) return
  error.value = ''
  try {
    const updated = await updateExercise(id, buildPayload(editExercise.value, editAliases.value, editSecondaryMuscles.value))
    const idx = exercises.value.findIndex((e) => e.id === id)
    if (idx !== -1) exercises.value[idx] = updated
    exercises.value.sort((a, b) => a.name.localeCompare(b.name))
    cancelEdit()
  } catch {
    error.value = 'Failed to update exercise.'
  }
}

async function handleDelete(id: number, name: string) {
  if (!confirm(`Delete "${name}"?`)) return
  error.value = ''
  try {
    await deleteExercise(id)
    exercises.value = exercises.value.filter((e) => e.id !== id)
  } catch {
    error.value = 'Failed to delete exercise.'
  }
}

onMounted(loadExercises)
</script>

<template>
  <div class="admin-exercises">
    <h1>Exercise Library</h1>

    <p v-if="error" class="admin-error">{{ error }}</p>
    <p v-if="loading">Loading…</p>

    <form class="admin-form-card" @submit.prevent="handleAdd">
      <div class="admin-form-header">
        <h2>Add Exercise</h2>
        <button type="submit" class="btn btn-primary btn-sm" :disabled="adding">
          {{ adding ? 'Adding…' : 'Add' }}
        </button>
      </div>

      <div class="admin-form-grid">
        <label class="admin-field">
          <span>Name *</span>
          <input v-model="newExercise.name" type="text" required class="input" />
        </label>
        <label class="admin-field">
          <span>Slug</span>
          <input v-model="newExercise.slug" type="text" placeholder="auto-generated if blank" class="input" />
        </label>
        <label class="admin-field admin-field-wide">
          <span>Description</span>
          <input v-model="newExercise.description" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Category</span>
          <input v-model="newExercise.category" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Body region</span>
          <input v-model="newExercise.bodyRegion" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Movement pattern</span>
          <input v-model="newExercise.movementPattern" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Primary muscle</span>
          <input v-model="newExercise.primaryMuscleGroup" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Primary equipment</span>
          <input v-model="newExercise.primaryEquipment" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Secondary equipment</span>
          <input v-model="newExercise.secondaryEquipment" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Difficulty</span>
          <input v-model="newExercise.difficultyLevel" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Training style</span>
          <input v-model="newExercise.trainingStyle" type="text" class="input" />
        </label>
        <label class="admin-field">
          <span>Aliases</span>
          <input v-model="newAliases" type="text" placeholder="comma-separated" class="input" />
        </label>
        <label class="admin-field">
          <span>Secondary muscles</span>
          <input v-model="newSecondaryMuscles" type="text" placeholder="comma-separated" class="input" />
        </label>
        <label class="admin-checkbox">
          <input v-model="newExercise.isUnilateral" type="checkbox" />
          <span>Unilateral</span>
        </label>
        <label class="admin-checkbox">
          <input v-model="newExercise.isArchived" type="checkbox" />
          <span>Archived</span>
        </label>
      </div>
    </form>

    <div v-if="exercises.length" class="admin-exercise-list">
      <article v-for="ex in exercises" :key="ex.id" class="admin-exercise-card" :class="{ 'admin-exercise-card--archived': ex.isArchived }">
        <template v-if="editingId === ex.id">
          <div class="admin-form-header">
            <h2>Edit Exercise</h2>
            <div class="admin-actions">
              <button type="button" class="btn btn-primary btn-sm" @click="handleUpdate(ex.id)">Save</button>
              <button type="button" class="btn btn-secondary btn-sm" @click="cancelEdit">Cancel</button>
            </div>
          </div>

          <div class="admin-form-grid">
            <label class="admin-field">
              <span>Name *</span>
              <input v-model="editExercise.name" type="text" required class="input" />
            </label>
            <label class="admin-field">
              <span>Slug</span>
              <input v-model="editExercise.slug" type="text" class="input" />
            </label>
            <label class="admin-field admin-field-wide">
              <span>Description</span>
              <input v-model="editExercise.description" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Category</span>
              <input v-model="editExercise.category" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Body region</span>
              <input v-model="editExercise.bodyRegion" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Movement pattern</span>
              <input v-model="editExercise.movementPattern" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Primary muscle</span>
              <input v-model="editExercise.primaryMuscleGroup" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Primary equipment</span>
              <input v-model="editExercise.primaryEquipment" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Secondary equipment</span>
              <input v-model="editExercise.secondaryEquipment" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Difficulty</span>
              <input v-model="editExercise.difficultyLevel" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Training style</span>
              <input v-model="editExercise.trainingStyle" type="text" class="input" />
            </label>
            <label class="admin-field">
              <span>Aliases</span>
              <input v-model="editAliases" type="text" placeholder="comma-separated" class="input" />
            </label>
            <label class="admin-field">
              <span>Secondary muscles</span>
              <input v-model="editSecondaryMuscles" type="text" placeholder="comma-separated" class="input" />
            </label>
            <label class="admin-checkbox">
              <input v-model="editExercise.isUnilateral" type="checkbox" />
              <span>Unilateral</span>
            </label>
            <label class="admin-checkbox">
              <input v-model="editExercise.isArchived" type="checkbox" />
              <span>Archived</span>
            </label>
          </div>
        </template>

        <template v-else>
          <div class="admin-exercise-card-header">
            <div>
              <div class="admin-title-row">
                <h2>{{ ex.name }}</h2>
                <span v-if="ex.isArchived" class="admin-badge">Archived</span>
                <span v-if="ex.isUnilateral" class="admin-badge admin-badge-secondary">Unilateral</span>
              </div>
              <p class="admin-slug">{{ ex.slug }}</p>
            </div>
            <div class="admin-actions">
              <button type="button" class="btn btn-accent-ghost btn-sm" @click="startEdit(ex)">Edit</button>
              <button type="button" class="btn btn-danger btn-sm" @click="handleDelete(ex.id, ex.name)">Delete</button>
            </div>
          </div>

          <p class="admin-description">{{ ex.description ?? 'No description.' }}</p>

          <div class="admin-meta-grid">
            <div><span>Category</span><strong>{{ ex.category }}</strong></div>
            <div><span>Body region</span><strong>{{ ex.bodyRegion }}</strong></div>
            <div><span>Movement pattern</span><strong>{{ ex.movementPattern }}</strong></div>
            <div><span>Primary muscle</span><strong>{{ ex.primaryMuscleGroup }}</strong></div>
            <div><span>Primary equipment</span><strong>{{ ex.primaryEquipment ?? '—' }}</strong></div>
            <div><span>Secondary equipment</span><strong>{{ ex.secondaryEquipment ?? '—' }}</strong></div>
            <div><span>Difficulty</span><strong>{{ ex.difficultyLevel }}</strong></div>
            <div><span>Training style</span><strong>{{ ex.trainingStyle }}</strong></div>
          </div>

          <div class="admin-list-row">
            <span>Aliases</span>
            <strong>{{ ex.aliases.length ? ex.aliases.join(', ') : '—' }}</strong>
          </div>
          <div class="admin-list-row">
            <span>Secondary muscles</span>
            <strong>{{ ex.secondaryMuscleGroups.length ? ex.secondaryMuscleGroups.join(', ') : '—' }}</strong>
          </div>
        </template>
      </article>
    </div>

    <p v-else-if="!loading">No exercises yet.</p>
  </div>
</template>
