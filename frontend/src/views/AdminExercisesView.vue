<script setup lang="ts">
import '../assets/admin-exercises.css'
import { ref, onMounted } from 'vue'
import { useExercises, type Exercise } from '../composables/useExercises'

const { fetchExercises, createExercise, updateExercise, deleteExercise } = useExercises()

const exercises = ref<Exercise[]>([])
const loading = ref(false)
const error = ref('')

// Add form
const newName = ref('')
const newDescription = ref('')
const adding = ref(false)

// Edit state
const editingId = ref<number | null>(null)
const editName = ref('')
const editDescription = ref('')

async function loadExercises() {
  loading.value = true
  error.value = ''
  try {
    exercises.value = await fetchExercises()
  } catch (e) {
    error.value = 'Failed to load exercises.'
  } finally {
    loading.value = false
  }
}

async function handleAdd() {
  if (!newName.value.trim()) return
  adding.value = true
  error.value = ''
  try {
    const created = await createExercise(newName.value.trim(), newDescription.value.trim() || undefined)
    exercises.value.push(created)
    newName.value = ''
    newDescription.value = ''
  } catch {
    error.value = 'Failed to create exercise.'
  } finally {
    adding.value = false
  }
}

function startEdit(ex: Exercise) {
  editingId.value = ex.id
  editName.value = ex.name
  editDescription.value = ex.description ?? ''
}

function cancelEdit() {
  editingId.value = null
}

async function handleUpdate(id: number) {
  if (!editName.value.trim()) return
  error.value = ''
  try {
    const updated = await updateExercise(id, editName.value.trim(), editDescription.value.trim() || undefined)
    const idx = exercises.value.findIndex((e) => e.id === id)
    if (idx !== -1) exercises.value[idx] = updated
    editingId.value = null
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

    <form class="admin-add-form" @submit.prevent="handleAdd">
      <h2>Add Exercise</h2>
      <div class="form-row">
        <input v-model="newName" type="text" placeholder="Name *" required class="input" />
        <input v-model="newDescription" type="text" placeholder="Description (optional)" class="input" />
        <button type="submit" class="btn btn-primary btn-sm" :disabled="adding">{{ adding ? 'Adding…' : 'Add' }}</button>
      </div>
    </form>

    <table v-if="exercises.length" class="admin-table">
      <thead>
        <tr>
          <th>Name</th>
          <th>Description</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="ex in exercises" :key="ex.id">
          <template v-if="editingId === ex.id">
            <td><input v-model="editName" type="text" required class="input" /></td>
            <td><input v-model="editDescription" type="text" placeholder="(optional)" class="input" /></td>
            <td class="actions">
              <button class="btn btn-primary btn-sm" @click="handleUpdate(ex.id)">Save</button>
              <button class="btn btn-secondary btn-sm" @click="cancelEdit">Cancel</button>
            </td>
          </template>
          <template v-else>
            <td>{{ ex.name }}</td>
            <td class="description">{{ ex.description ?? '—' }}</td>
            <td class="actions">
              <button class="btn btn-accent-ghost btn-sm" @click="startEdit(ex)">Edit</button>
              <button class="btn btn-danger btn-sm" @click="handleDelete(ex.id, ex.name)">Delete</button>
            </td>
          </template>
        </tr>
      </tbody>
    </table>

    <p v-else-if="!loading">No exercises yet.</p>
  </div>
</template>


