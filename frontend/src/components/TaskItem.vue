<script setup lang="ts">
import { computed } from 'vue'
import type { Task } from '@/stores/tasks'
import { dueStatus, formatDueDate } from '@/utils/date'

const props = defineProps<{ task: Task }>()

// The row is presentational and emits intent; TaskListView wires these to the store + forms (#23).
defineEmits<{ toggle: []; edit: []; delete: [] }>()

type Tone = 'overdue' | 'due-today' | 'completed' | 'neutral'

// The single status pill, derived from completion + the due date's relation to today.
const pill = computed<{ tone: Tone; text: string; dot: boolean; check: boolean }>(() => {
  if (props.task.isCompleted) {
    return { tone: 'completed', text: 'Completed', dot: false, check: true }
  }
  const due = formatDueDate(props.task.dueDate)
  switch (dueStatus(props.task.dueDate)) {
    case 'overdue':
      return { tone: 'overdue', text: `Overdue · ${due}`, dot: true, check: false }
    case 'today':
      return { tone: 'due-today', text: `Due today · ${due}`, dot: true, check: false }
    default:
      return { tone: 'neutral', text: `Due ${due}`, dot: false, check: false }
  }
})
</script>

<template>
  <div class="task" :class="{ 'task--done': task.isCompleted }">
    <div class="task__main">
      <button
        type="button"
        class="task__check"
        role="checkbox"
        :aria-checked="task.isCompleted"
        :aria-label="task.isCompleted ? 'Mark as not completed' : 'Mark as completed'"
        @click="$emit('toggle')"
      >
        <span v-if="task.isCompleted" aria-hidden="true">✓</span>
      </button>

      <div class="task__body">
        <p class="task__title">{{ task.title }}</p>
        <p v-if="task.description" class="task__desc">{{ task.description }}</p>
        <div class="task__meta">
          <span class="task__pill" :class="`task__pill--${pill.tone}`">
            <span v-if="pill.dot" class="task__dot" aria-hidden="true"></span>
            <span v-if="pill.check" aria-hidden="true">✓</span>
            {{ pill.text }}
          </span>
        </div>
      </div>
    </div>

    <div class="task__actions">
      <button type="button" class="task__btn" @click="$emit('edit')">Edit</button>
      <button type="button" class="task__btn task__btn--danger" @click="$emit('delete')">
        Delete
      </button>
    </div>
  </div>
</template>

<style scoped>
.task {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-4);
  padding: var(--sp-4);
  background: var(--c-surface);
  border: 1px solid var(--c-border);
  border-radius: var(--r-md);
  box-shadow: var(--shadow-sm);
}

.task__main {
  display: flex;
  flex: 1;
  gap: var(--sp-4);
  min-width: 0;
}

.task__check {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: none;
  width: 19px;
  height: 19px;
  margin-top: 1px;
  padding: 0;
  background: #fff;
  border: 1.5px solid var(--c-border-strong);
  border-radius: 5px;
  color: #fff;
  font-size: 12px;
  line-height: 1;
}

.task--done .task__check {
  background: var(--c-primary);
  border-color: var(--c-primary);
}

.task__body {
  flex: 1;
  min-width: 0;
}

.task__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--c-text);
}

.task__desc {
  margin-top: 3px;
  font-size: 13px;
  color: var(--c-text-muted);
}

.task--done .task__title,
.task--done .task__desc {
  color: var(--c-text-subtle);
  text-decoration: line-through;
}

.task__meta {
  margin-top: 10px;
}

.task__pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 3px 9px;
  border-radius: var(--r-pill);
  font-size: 12px;
  font-weight: 600;
  line-height: 1.3;
}

.task__pill--overdue {
  color: var(--c-overdue);
  background: var(--c-overdue-bg);
}

.task__pill--due-today {
  color: var(--c-due-today);
  background: var(--c-due-today-bg);
}

.task__pill--completed {
  color: var(--c-completed);
  background: var(--c-completed-bg);
}

.task__pill--neutral {
  color: var(--c-neutral);
  background: var(--c-neutral-bg);
}

.task__dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
}

.task__actions {
  display: flex;
  gap: 4px;
}

.task__btn {
  padding: 5px 11px;
  background: transparent;
  border: 1px solid var(--c-border);
  border-radius: var(--r-sm);
  color: var(--c-text-muted);
  font-size: 12px;
  font-weight: 600;
}

.task__btn:hover {
  border-color: var(--c-border-strong);
  color: var(--c-text);
}

.task__btn--danger {
  color: var(--c-danger);
}

.task__btn--danger:hover {
  border-color: var(--c-danger-bg);
  color: var(--c-danger);
  background: var(--c-danger-bg);
}

/* Mobile: stack the actions below the body as full-width buttons (matches the kit). */
@media (max-width: 640px) {
  .task {
    flex-direction: column;
    gap: var(--sp-3);
  }

  .task__actions {
    gap: 8px;
    padding-top: var(--sp-3);
    border-top: 1px solid var(--c-border);
  }

  .task__btn {
    flex: 1;
    padding: 8px;
    text-align: center;
    font-size: 13px;
    color: var(--c-text);
  }

  .task__btn--danger {
    color: var(--c-danger);
  }
}
</style>
