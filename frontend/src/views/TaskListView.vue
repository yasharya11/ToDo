<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useTasksStore, type Task } from '@/stores/tasks'
import TaskItem from '@/components/TaskItem.vue'
import TaskFormModal from '@/components/TaskFormModal.vue'
import DeleteDialog from '@/components/DeleteDialog.vue'

const store = useTasksStore()

type Filter = 'all' | 'active' | 'completed'
const filter = ref<Filter>('all')
const filterTabs: { value: Filter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'active', label: 'Active' },
  { value: 'completed', label: 'Completed' },
]

const visibleTasks = computed<Task[]>(() => {
  const filtered = store.tasks.filter((task) => {
    if (filter.value === 'active') return !task.isCompleted
    if (filter.value === 'completed') return task.isCompleted
    return true
  })
  // Incomplete first, then earliest due date; completed tasks sink to the bottom.
  return [...filtered].sort((a, b) => {
    if (a.isCompleted !== b.isCompleted) return a.isCompleted ? 1 : -1
    return a.dueDate.localeCompare(b.dueDate)
  })
})

const countLabel = computed(() => {
  const total = store.tasks.length
  const base = `${total} ${total === 1 ? 'task' : 'tasks'}`
  return store.overdueCount > 0 ? `${base} · ${store.overdueCount} overdue` : base
})

// --- Create / edit form ---
const formOpen = ref(false)
const editingTask = ref<Task | null>(null)

function openCreate(): void {
  editingTask.value = null
  formOpen.value = true
}
function openEdit(task: Task): void {
  editingTask.value = task
  formOpen.value = true
}
function closeForm(): void {
  formOpen.value = false
  editingTask.value = null
}

// --- Delete confirmation ---
const deletingTask = ref<Task | null>(null)
const deleteSubmitting = ref(false)
const deleteError = ref<string | null>(null)

function openDelete(task: Task): void {
  deletingTask.value = task
  deleteError.value = null
}
function closeDelete(): void {
  deletingTask.value = null
  deleteError.value = null
}
async function confirmDelete(): Promise<void> {
  if (!deletingTask.value) return
  deleteSubmitting.value = true
  deleteError.value = null
  try {
    await store.deleteTask(deletingTask.value.id)
    closeDelete()
  } catch {
    deleteError.value = "Couldn't delete the task. Please try again."
  } finally {
    deleteSubmitting.value = false
  }
}

// --- Inline completion toggle (no modal): surface failures in a transient toast ---
const actionError = ref<string | null>(null)
let actionErrorTimer: ReturnType<typeof setTimeout> | undefined

function flashError(message: string): void {
  actionError.value = message
  if (actionErrorTimer) clearTimeout(actionErrorTimer)
  actionErrorTimer = setTimeout(() => (actionError.value = null), 4000)
}
async function onToggle(task: Task): Promise<void> {
  try {
    await store.toggleComplete(task)
  } catch {
    flashError("Couldn't update the task. Please try again.")
  }
}

onMounted(() => {
  void store.fetchTasks()
})
onBeforeUnmount(() => {
  if (actionErrorTimer) clearTimeout(actionErrorTimer)
})
</script>

<template>
  <section class="tasks">
    <!-- Loading: spinner + skeleton rows. -->
    <div v-if="store.status === 'loading'" class="tasks__panel">
      <div class="tasks__loading">
        <span class="spinner" aria-hidden="true"></span>
        <span>Loading your tasks…</span>
      </div>
      <div class="tasks__list" aria-hidden="true">
        <div v-for="n in 3" :key="n" class="skeleton">
          <div class="skeleton__check"></div>
          <div class="skeleton__body">
            <div class="skeleton__line skeleton__line--title"></div>
            <div class="skeleton__line skeleton__line--pill"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Server failure: message + retry. -->
    <div v-else-if="store.status === 'error'" class="tasks__state" role="alert">
      <div class="state-icon state-icon--error" aria-hidden="true">!</div>
      <h2 class="state__title">We couldn't load your tasks</h2>
      <p class="state__text">Something went wrong. Check your connection and try again.</p>
      <button type="button" class="btn-secondary" @click="store.fetchTasks()">
        <span aria-hidden="true">↻</span> Retry
      </button>
    </div>

    <!-- Empty: no tasks yet. -->
    <div v-else-if="store.tasks.length === 0" class="tasks__state">
      <div class="state-icon state-icon--brand" aria-hidden="true">+</div>
      <h2 class="state__title">No tasks yet</h2>
      <p class="state__text">Create your first task to start tracking what needs to get done.</p>
      <button type="button" class="btn-primary" @click="openCreate">Create your first task</button>
    </div>

    <!-- Ready: the list. -->
    <div v-else class="tasks__panel">
      <header class="tasks__header">
        <div>
          <h1 class="tasks__title">My tasks</h1>
          <p class="tasks__count">{{ countLabel }}</p>
        </div>
        <button type="button" class="btn-primary tasks__create" @click="openCreate">
          <span class="tasks__plus" aria-hidden="true">+</span> Create task
        </button>
      </header>

      <div class="tasks__filters" role="tablist" aria-label="Filter tasks">
        <button
          v-for="tab in filterTabs"
          :key="tab.value"
          type="button"
          role="tab"
          :aria-selected="filter === tab.value"
          class="tasks__filter"
          :class="{ 'tasks__filter--active': filter === tab.value }"
          @click="filter = tab.value"
        >
          {{ tab.label }}
        </button>
      </div>

      <div v-if="visibleTasks.length > 0" class="tasks__list">
        <TaskItem
          v-for="task in visibleTasks"
          :key="task.id"
          :task="task"
          @toggle="onToggle(task)"
          @edit="openEdit(task)"
          @delete="openDelete(task)"
        />
      </div>
      <p v-else class="tasks__empty-filter">No {{ filter }} tasks.</p>
    </div>

    <TaskFormModal v-if="formOpen" :task="editingTask" @close="closeForm" @saved="closeForm" />
    <DeleteDialog
      v-if="deletingTask"
      :title="deletingTask.title"
      :deleting="deleteSubmitting"
      :error="deleteError"
      @confirm="confirmDelete"
      @close="closeDelete"
    />

    <div v-if="actionError" class="toast" role="alert" @click="actionError = null">
      {{ actionError }}
    </div>
  </section>
</template>

<style scoped>
.tasks {
  padding: var(--sp-6) var(--sp-5) var(--sp-7);
}

.tasks__panel {
  max-width: 760px;
  margin: 0 auto;
}

/* Header */
.tasks__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--sp-4);
  margin-bottom: var(--sp-4);
}

.tasks__title {
  font-size: var(--fs-lg);
  font-weight: 700;
}

.tasks__count {
  margin-top: 2px;
  font-size: 13px;
  color: var(--c-text-muted);
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 9px 16px;
  background: var(--c-primary);
  border: none;
  border-radius: var(--r-sm);
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
}

.btn-primary:hover {
  background: var(--c-primary-hover);
}

.tasks__plus {
  font-size: 17px;
  font-weight: 400;
  line-height: 1;
}

/* Filter tabs */
.tasks__filters {
  display: inline-flex;
  gap: 2px;
  margin-bottom: var(--sp-4);
  padding: 3px;
  background: var(--c-surface-2);
  border: 1px solid var(--c-border);
  border-radius: var(--r-md);
}

.tasks__filter {
  padding: 6px 16px;
  background: transparent;
  border: none;
  border-radius: 6px;
  color: var(--c-text-muted);
  font-size: 13px;
  font-weight: 500;
}

.tasks__filter:hover {
  color: var(--c-text);
}

.tasks__filter--active {
  background: var(--c-surface);
  color: var(--c-text);
  font-weight: 600;
  box-shadow: var(--shadow-sm);
}

/* List */
.tasks__list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.tasks__empty-filter {
  padding: var(--sp-6) 0;
  color: var(--c-text-muted);
  font-size: 14px;
  text-align: center;
}

/* Loading */
.tasks__loading {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: var(--sp-5);
  color: var(--c-text-muted);
  font-size: 14px;
  font-weight: 500;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid var(--c-border-strong);
  border-top-color: var(--c-primary);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

.skeleton {
  display: flex;
  gap: var(--sp-4);
  padding: var(--sp-4);
  background: var(--c-surface);
  border: 1px solid var(--c-border);
  border-radius: var(--r-md);
  animation: pulse 1.4s ease-in-out infinite;
}

.skeleton:nth-child(2) {
  animation-delay: 0.15s;
}

.skeleton:nth-child(3) {
  animation-delay: 0.3s;
}

.skeleton__check {
  width: 19px;
  height: 19px;
  flex: none;
  border-radius: 5px;
  background: var(--c-surface-2);
}

.skeleton__body {
  flex: 1;
}

.skeleton__line {
  border-radius: 4px;
  background: var(--c-surface-2);
}

.skeleton__line--title {
  width: 55%;
  height: 13px;
}

.skeleton__line--pill {
  width: 120px;
  height: 18px;
  margin-top: 12px;
  border-radius: var(--r-pill);
}

/* Empty / error states */
.tasks__state {
  display: flex;
  flex-direction: column;
  align-items: center;
  max-width: 400px;
  margin: 0 auto;
  padding: var(--sp-7) 0;
  text-align: center;
}

.state-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 64px;
  margin-bottom: var(--sp-4);
  border-radius: 16px;
  font-size: 28px;
  font-weight: 700;
}

.state-icon--brand {
  background: var(--c-primary-weak);
  border: 1px solid #cfe7e9;
  color: var(--c-primary);
}

.state-icon--error {
  background: var(--c-overdue-bg);
  border: 1px solid #f3c9c4;
  color: var(--c-overdue);
}

.state__title {
  font-size: 19px;
  font-weight: 700;
}

.state__text {
  margin: 8px 0 var(--sp-5);
  color: var(--c-text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  background: var(--c-surface);
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  color: var(--c-text);
  font-size: 14px;
  font-weight: 600;
}

.btn-secondary:hover {
  background: var(--c-surface-2);
}

/* Transient error toast for inline actions (e.g. a failed completion toggle). */
.toast {
  position: fixed;
  left: 50%;
  bottom: var(--sp-5);
  transform: translateX(-50%);
  z-index: 200;
  max-width: calc(100vw - 2 * var(--sp-4));
  padding: 11px 16px;
  background: var(--c-overdue);
  border-radius: var(--r-sm);
  box-shadow: var(--shadow-md);
  color: #fff;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
}

/* Mobile: full-width create button below the title. */
@media (max-width: 640px) {
  .tasks {
    padding: var(--sp-4);
  }

  .tasks__header {
    flex-direction: column;
  }

  .tasks__create {
    width: 100%;
    justify-content: center;
  }

  .tasks__filters {
    display: flex;
  }

  .tasks__filter {
    flex: 1;
    text-align: center;
  }
}
</style>
