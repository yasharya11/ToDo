<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTasksStore, type Task } from '@/stores/tasks'
import TaskItem from '@/components/TaskItem.vue'

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

// Create/edit/delete/complete are wired to the forms + store in #23. Stubbed here so the read-only
// list view stands on its own (the empty-state CTA still needs a button to render).
function onCreate(): void {
  // #23: open the create-task form.
}

onMounted(() => {
  void store.fetchTasks()
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
      <button type="button" class="btn-primary" @click="onCreate">Create your first task</button>
    </div>

    <!-- Ready: the list. -->
    <div v-else class="tasks__panel">
      <header class="tasks__header">
        <div>
          <h1 class="tasks__title">My tasks</h1>
          <p class="tasks__count">{{ countLabel }}</p>
        </div>
        <button type="button" class="btn-primary tasks__create" @click="onCreate">
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
        <TaskItem v-for="task in visibleTasks" :key="task.id" :task="task" />
      </div>
      <p v-else class="tasks__empty-filter">No {{ filter }} tasks.</p>
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
