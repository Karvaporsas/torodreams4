<script setup lang="ts">
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

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="loading">Loading…</p>

    <form class="add-form" @submit.prevent="handleAdd">
      <h2>Add Exercise</h2>
      <div class="form-row">
        <input v-model="newName" type="text" placeholder="Name *" required />
        <input v-model="newDescription" type="text" placeholder="Description (optional)" />
        <button type="submit" :disabled="adding">{{ adding ? 'Adding…' : 'Add' }}</button>
      </div>
    </form>

    <table v-if="exercises.length">
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
            <td><input v-model="editName" type="text" required /></td>
            <td><input v-model="editDescription" type="text" placeholder="(optional)" /></td>
            <td class="actions">
              <button class="btn-save" @click="handleUpdate(ex.id)">Save</button>
              <button class="btn-cancel" @click="cancelEdit">Cancel</button>
            </td>
          </template>
          <template v-else>
            <td>{{ ex.name }}</td>
            <td class="description">{{ ex.description ?? '—' }}</td>
            <td class="actions">
              <button class="btn-edit" @click="startEdit(ex)">Edit</button>
              <button class="btn-delete" @click="handleDelete(ex.id, ex.name)">Delete</button>
            </td>
          </template>
        </tr>
      </tbody>
    </table>

    <p v-else-if="!loading">No exercises yet.</p>
  </div>
</template>

<style scoped>
.admin-exercises {
  max-width: 760px;
  margin: 2rem auto;
  padding: 0 1rem;
}

h1 {
  margin-bottom: 1.5rem;
}

h2 {
  font-size: 1rem;
  margin-bottom: 0.5rem;
}

.add-form {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  padding: 1rem;
  margin-bottom: 1.5rem;
}

.form-row {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.form-row input {
  flex: 1;
  min-width: 140px;
  padding: 0.45rem 0.75rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.95rem;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.95rem;
}

th, td {
  text-align: left;
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid #e0e0e0;
}

th {
  background: #f0f0f0;
  font-weight: 600;
}

.description {
  color: #666;
}

td input {
  width: 100%;
  padding: 0.35rem 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.9rem;
}

.actions {
  display: flex;
  gap: 0.4rem;
}

button {
  padding: 0.35rem 0.8rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.875rem;
  color: white;
}

.btn-edit   { background: #42b883; }
.btn-delete { background: #c0392b; }
.btn-save   { background: #2980b9; }
.btn-cancel { background: #888; }

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #c00;
  margin-bottom: 1rem;
}
</style>
