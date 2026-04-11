<script setup lang="ts">
import '../assets/admin-exercises.css'
import { computed, onMounted, ref } from 'vue'
import {
  useExercises,
  type Exercise,
  type ExerciseCatalogStatus,
  type ExerciseInput,
  type ExerciseSearchParams,
  type ExerciseSearchResponse,
} from '../composables/useExercises'

const PAGE_SIZE = 12

const {
  fetchExerciseSearch,
  fetchAdminCatalogStatus,
  reimportExerciseCatalog,
  createExercise,
  updateExercise,
  deleteExercise,
} = useExercises()

const loading = ref(false)
const statusLoading = ref(false)
const submitting = ref(false)
const importRunning = ref(false)
const error = ref('')

const searchResult = ref<ExerciseSearchResponse>({
  items: [],
  page: 1,
  pageSize: PAGE_SIZE,
  totalCount: 0,
  hasMore: false,
})

const catalogStatus = ref<ExerciseCatalogStatus | null>(null)
const editingId = ref<number | null>(null)

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

const editExercise = ref<ExerciseInput>(createEmptyForm())
const editAliases = ref('')
const editSecondaryMuscles = ref('')

const filters = ref<ExerciseSearchParams>({
  search: '',
  category: '',
  movementPattern: '',
  equipment: '',
  bodyRegion: '',
  trainingStyle: '',
  sort: 'updated-desc',
  includeArchived: true,
  page: 1,
  pageSize: PAGE_SIZE,
})

const currentPage = computed(() => searchResult.value.page)
const totalPages = computed(() => Math.max(1, Math.ceil(searchResult.value.totalCount / searchResult.value.pageSize)))

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

function buildExistingPayload(exercise: Exercise, overrides: Partial<ExerciseInput> = {}): ExerciseInput {
  return {
    slug: exercise.slug,
    name: exercise.name,
    description: exercise.description ?? '',
    category: exercise.category,
    bodyRegion: exercise.bodyRegion,
    movementPattern: exercise.movementPattern,
    primaryMuscleGroup: exercise.primaryMuscleGroup,
    primaryEquipment: exercise.primaryEquipment ?? '',
    secondaryEquipment: exercise.secondaryEquipment ?? '',
    difficultyLevel: exercise.difficultyLevel,
    trainingStyle: exercise.trainingStyle,
    isUnilateral: exercise.isUnilateral,
    isArchived: exercise.isArchived,
    aliases: [...exercise.aliases],
    secondaryMuscleGroups: [...exercise.secondaryMuscleGroups],
    ...overrides,
  }
}

function startEdit(exercise: Exercise) {
  editingId.value = exercise.id
  editExercise.value = buildExistingPayload(exercise)
  editAliases.value = exercise.aliases.join(', ')
  editSecondaryMuscles.value = exercise.secondaryMuscleGroups.join(', ')
}

function cancelEdit() {
  editingId.value = null
  editExercise.value = createEmptyForm()
  editAliases.value = ''
  editSecondaryMuscles.value = ''
}

function resetNewForm() {
  newExercise.value = createEmptyForm()
  newAliases.value = ''
  newSecondaryMuscles.value = ''
}

function setErrorMessage(err: unknown, fallback: string) {
  error.value = err instanceof Error && err.message ? err.message : fallback
}

async function loadExercises(page = filters.value.page ?? 1) {
  loading.value = true
  error.value = ''

  filters.value.page = page

  try {
    searchResult.value = await fetchExerciseSearch({
      ...filters.value,
      page,
      pageSize: PAGE_SIZE,
    })
  } catch (err) {
    setErrorMessage(err, 'Failed to load exercises.')
  } finally {
    loading.value = false
  }
}

async function loadCatalogStatus() {
  statusLoading.value = true

  try {
    catalogStatus.value = await fetchAdminCatalogStatus()
  } catch (err) {
    setErrorMessage(err, 'Failed to load catalog status.')
  } finally {
    statusLoading.value = false
  }
}

async function handleAdd() {
  if (!newExercise.value.name.trim()) {
    error.value = 'Name is required.'
    return
  }

  submitting.value = true
  error.value = ''

  try {
    await createExercise(buildPayload(newExercise.value, newAliases.value, newSecondaryMuscles.value))
    resetNewForm()
    await loadExercises(1)
  } catch (err) {
    setErrorMessage(err, 'Failed to create exercise.')
  } finally {
    submitting.value = false
  }
}

async function handleUpdate(id: number) {
  if (!editExercise.value.name.trim()) {
    error.value = 'Name is required.'
    return
  }

  submitting.value = true
  error.value = ''

  try {
    await updateExercise(id, buildPayload(editExercise.value, editAliases.value, editSecondaryMuscles.value))
    cancelEdit()
    await loadExercises(currentPage.value)
  } catch (err) {
    setErrorMessage(err, 'Failed to update exercise.')
  } finally {
    submitting.value = false
  }
}

async function handleToggleArchive(exercise: Exercise) {
  submitting.value = true
  error.value = ''

  try {
    await updateExercise(
      exercise.id,
      buildPayload(
        {
          ...buildExistingPayload(exercise),
          isArchived: !exercise.isArchived,
        },
        exercise.aliases.join(', '),
        exercise.secondaryMuscleGroups.join(', '),
      ),
    )
    if (editingId.value === exercise.id) {
      cancelEdit()
    }
    await loadExercises(currentPage.value)
  } catch (err) {
    setErrorMessage(err, `Failed to ${exercise.isArchived ? 'unarchive' : 'archive'} exercise.`)
  } finally {
    submitting.value = false
  }
}

async function handleDelete(exercise: Exercise) {
  if (!confirm(`Delete "${exercise.name}"?`)) return

  submitting.value = true
  error.value = ''

  try {
    await deleteExercise(exercise.id)

    const remainingItems = searchResult.value.items.length - 1
    const previousPage = currentPage.value > 1 && remainingItems === 0 ? currentPage.value - 1 : currentPage.value

    await loadExercises(previousPage)
  } catch (err) {
    setErrorMessage(err, 'Failed to delete exercise.')
  } finally {
    submitting.value = false
  }
}

async function handleReimport() {
  importRunning.value = true
  error.value = ''

  try {
    catalogStatus.value = await reimportExerciseCatalog()
    await loadExercises(currentPage.value)
  } catch (err) {
    setErrorMessage(err, 'Failed to re-import the exercise catalog.')
    await loadCatalogStatus()
  } finally {
    importRunning.value = false
  }
}

async function applyFilters() {
  await loadExercises(1)
}

async function resetFilters() {
  filters.value = {
    search: '',
    category: '',
    movementPattern: '',
    equipment: '',
    bodyRegion: '',
    trainingStyle: '',
    sort: 'updated-desc',
    includeArchived: true,
    page: 1,
    pageSize: PAGE_SIZE,
  }

  await loadExercises(1)
}

async function changePage(page: number) {
  if (page < 1 || page > totalPages.value || page === currentPage.value) {
    return
  }

  await loadExercises(page)
}

function formatDate(value?: string | null): string {
  if (!value) return '—'

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleString()
}

onMounted(async () => {
  await Promise.all([loadExercises(1), loadCatalogStatus()])
})
</script>

<template>
  <div class="admin-exercises">
    <div class="admin-page-header">
      <div>
        <h1>Exercise Library</h1>
        <p class="admin-page-copy">Manage the catalog with server-side search, archive controls, and import status.</p>
      </div>
      <div class="admin-page-stats">
        <span class="admin-stat-pill">{{ searchResult.totalCount }} total</span>
        <span class="admin-stat-pill">{{ currentPage }} / {{ totalPages }} pages</span>
      </div>
    </div>

    <p v-if="error" class="admin-error">{{ error }}</p>

    <section class="admin-panel-grid">
      <article class="admin-form-card admin-status-card">
        <div class="admin-form-header">
          <div>
            <h2>Catalog status</h2>
            <p class="admin-section-copy">Track the repo-owned catalog version and the latest import result.</p>
          </div>
          <button type="button" class="btn btn-primary btn-sm" :disabled="importRunning || statusLoading" @click="handleReimport">
            {{ importRunning ? 'Re-importing…' : 'Re-import catalog' }}
          </button>
        </div>

        <p v-if="statusLoading" class="admin-muted">Loading catalog status…</p>
        <div v-else-if="catalogStatus" class="admin-status-grid">
          <div><span>Catalog version</span><strong>{{ catalogStatus.currentCatalogVersion ?? 'Unknown' }}</strong></div>
          <div><span>Catalog file</span><strong>{{ catalogStatus.catalogFileExists ? 'Available' : 'Missing' }}</strong></div>
          <div><span>Last import status</span><strong>{{ catalogStatus.lastImport?.status ?? 'No imports yet' }}</strong></div>
          <div><span>Last import by</span><strong>{{ catalogStatus.lastImport?.triggeredBy ?? '—' }}</strong></div>
          <div><span>Imported at</span><strong>{{ formatDate(catalogStatus.lastImport?.importedAtUtc) }}</strong></div>
          <div><span>Catalog path</span><strong class="admin-break">{{ catalogStatus.catalogPath }}</strong></div>
          <div><span>Rows touched</span><strong>{{ catalogStatus.lastImport ? `${catalogStatus.lastImport.created ?? 0} created / ${catalogStatus.lastImport.updated ?? 0} updated / ${catalogStatus.lastImport.unchanged ?? 0} unchanged` : '—' }}</strong></div>
          <div><span>Last message</span><strong>{{ catalogStatus.lastImport?.message ?? catalogStatus.catalogMessage ?? '—' }}</strong></div>
        </div>
      </article>

      <form class="admin-form-card" @submit.prevent="applyFilters">
        <div class="admin-form-header">
          <div>
            <h2>Find exercises</h2>
            <p class="admin-section-copy">Search and filter the library without loading the whole catalog at once.</p>
          </div>
          <div class="admin-actions">
            <button type="button" class="btn btn-secondary btn-sm" @click="resetFilters">Reset</button>
            <button type="submit" class="btn btn-primary btn-sm">Apply</button>
          </div>
        </div>

        <div class="admin-form-grid">
          <label class="admin-field admin-field-wide">
            <span>Search</span>
            <input v-model="filters.search" type="text" class="input" placeholder="name, aliases, muscles, equipment…" />
          </label>
          <label class="admin-field">
            <span>Category</span>
            <input v-model="filters.category" type="text" class="input" />
          </label>
          <label class="admin-field">
            <span>Movement pattern</span>
            <input v-model="filters.movementPattern" type="text" class="input" />
          </label>
          <label class="admin-field">
            <span>Equipment</span>
            <input v-model="filters.equipment" type="text" class="input" />
          </label>
          <label class="admin-field">
            <span>Body region</span>
            <input v-model="filters.bodyRegion" type="text" class="input" />
          </label>
          <label class="admin-field">
            <span>Training style</span>
            <input v-model="filters.trainingStyle" type="text" class="input" />
          </label>
          <label class="admin-field">
            <span>Sort</span>
            <select v-model="filters.sort" class="input">
              <option value="updated-desc">Recently updated</option>
              <option value="updated">Oldest updated</option>
              <option value="name">Name A-Z</option>
              <option value="name-desc">Name Z-A</option>
            </select>
          </label>
          <label class="admin-checkbox">
            <input v-model="filters.includeArchived" type="checkbox" />
            <span>Include archived</span>
          </label>
        </div>
      </form>
    </section>

    <form class="admin-form-card" @submit.prevent="handleAdd">
      <div class="admin-form-header">
        <div>
          <h2>Add exercise</h2>
          <p class="admin-section-copy">Create a new library entry with the full metadata shape used by the catalog.</p>
        </div>
        <button type="submit" class="btn btn-primary btn-sm" :disabled="submitting">
          {{ submitting ? 'Saving…' : 'Add exercise' }}
        </button>
      </div>

      <div class="admin-form-grid">
        <label class="admin-field">
          <span>Name *</span>
          <input v-model="newExercise.name" type="text" required class="input" />
        </label>
        <label class="admin-field">
          <span>Slug</span>
          <input v-model="newExercise.slug" type="text" class="input" placeholder="auto-generated if blank" />
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
          <input v-model="newAliases" type="text" class="input" placeholder="comma-separated" />
        </label>
        <label class="admin-field">
          <span>Secondary muscles</span>
          <input v-model="newSecondaryMuscles" type="text" class="input" placeholder="comma-separated" />
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

    <div class="admin-list-header">
      <h2>Catalog results</h2>
      <div class="admin-pagination">
        <button type="button" class="btn btn-secondary btn-sm" :disabled="currentPage <= 1 || loading" @click="changePage(currentPage - 1)">
          Previous
        </button>
        <span class="admin-muted">Page {{ currentPage }} of {{ totalPages }}</span>
        <button type="button" class="btn btn-secondary btn-sm" :disabled="currentPage >= totalPages || loading" @click="changePage(currentPage + 1)">
          Next
        </button>
      </div>
    </div>

    <p v-if="loading" class="admin-muted">Loading exercises…</p>
    <p v-else-if="!searchResult.items.length" class="admin-muted">No exercises match the current filters.</p>

    <div v-else class="admin-exercise-list">
      <article
        v-for="exercise in searchResult.items"
        :key="exercise.id"
        class="admin-exercise-card"
        :class="{ 'admin-exercise-card--archived': exercise.isArchived }"
      >
        <template v-if="editingId === exercise.id">
          <div class="admin-form-header">
            <div>
              <h2>Edit exercise</h2>
              <p class="admin-section-copy">Reference count: {{ exercise.workoutReferenceCount }}</p>
            </div>
            <div class="admin-actions">
              <button type="button" class="btn btn-primary btn-sm" :disabled="submitting" @click="handleUpdate(exercise.id)">Save</button>
              <button type="button" class="btn btn-secondary btn-sm" :disabled="submitting" @click="cancelEdit">Cancel</button>
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
              <input v-model="editAliases" type="text" class="input" placeholder="comma-separated" />
            </label>
            <label class="admin-field">
              <span>Secondary muscles</span>
              <input v-model="editSecondaryMuscles" type="text" class="input" placeholder="comma-separated" />
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
                <h2>{{ exercise.name }}</h2>
                <span v-if="exercise.isArchived" class="admin-badge">Archived</span>
                <span v-if="exercise.isUnilateral" class="admin-badge admin-badge-secondary">Unilateral</span>
                <span v-if="exercise.isReferencedByWorkouts" class="admin-badge admin-badge-accent">
                  {{ exercise.workoutReferenceCount }} workouts
                </span>
              </div>
              <p class="admin-slug">{{ exercise.slug }}</p>
            </div>
            <div class="admin-actions">
              <button type="button" class="btn btn-accent-ghost btn-sm" @click="startEdit(exercise)">Edit</button>
              <button type="button" class="btn btn-secondary btn-sm" :disabled="submitting" @click="handleToggleArchive(exercise)">
                {{ exercise.isArchived ? 'Unarchive' : 'Archive' }}
              </button>
              <button
                type="button"
                class="btn btn-danger btn-sm"
                :disabled="submitting || exercise.isReferencedByWorkouts"
                :title="exercise.isReferencedByWorkouts ? 'Referenced exercises are archived instead of deleted.' : 'Delete exercise'"
                @click="handleDelete(exercise)"
              >
                Delete
              </button>
            </div>
          </div>

          <p class="admin-description">{{ exercise.description ?? 'No description.' }}</p>

          <div class="admin-meta-grid">
            <div><span>Category</span><strong>{{ exercise.category }}</strong></div>
            <div><span>Body region</span><strong>{{ exercise.bodyRegion }}</strong></div>
            <div><span>Movement pattern</span><strong>{{ exercise.movementPattern }}</strong></div>
            <div><span>Primary muscle</span><strong>{{ exercise.primaryMuscleGroup }}</strong></div>
            <div><span>Primary equipment</span><strong>{{ exercise.primaryEquipment ?? '—' }}</strong></div>
            <div><span>Secondary equipment</span><strong>{{ exercise.secondaryEquipment ?? '—' }}</strong></div>
            <div><span>Difficulty</span><strong>{{ exercise.difficultyLevel }}</strong></div>
            <div><span>Training style</span><strong>{{ exercise.trainingStyle }}</strong></div>
            <div><span>Updated</span><strong>{{ formatDate(exercise.updatedAtUtc) }}</strong></div>
            <div><span>References</span><strong>{{ exercise.workoutReferenceCount }}</strong></div>
          </div>

          <div class="admin-list-row">
            <span>Aliases</span>
            <strong>{{ exercise.aliases.length ? exercise.aliases.join(', ') : '—' }}</strong>
          </div>
          <div class="admin-list-row">
            <span>Secondary muscles</span>
            <strong>{{ exercise.secondaryMuscleGroups.length ? exercise.secondaryMuscleGroups.join(', ') : '—' }}</strong>
          </div>
        </template>
      </article>
    </div>
  </div>
</template>
