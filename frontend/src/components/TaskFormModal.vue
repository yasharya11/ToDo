<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTasksStore, type Task } from '@/stores/tasks'
import { ApiError, fieldErrors } from '@/api/http'
import ModalDialog from '@/components/ModalDialog.vue'

// One modal for both create (task = null) and edit (task = the row). On success it emits `saved`;
// the store has already updated the list, so the caller just closes the modal.
const props = defineProps<{ task: Task | null }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const store = useTasksStore()
const isEdit = computed(() => props.task !== null)

const TITLE_MAX = 200
const DESCRIPTION_MAX = 2000

const title = ref(props.task?.title ?? '')
const description = ref(props.task?.description ?? '')
const dueDate = ref(props.task?.dueDate ?? '')
const isCompleted = ref(props.task?.isCompleted ?? false)

const titleError = ref<string | null>(null)
const descriptionError = ref<string | null>(null)
const dueDateError = ref<string | null>(null)
const serverError = ref<string | null>(null)
const submitting = ref(false)

const titleInput = ref<HTMLInputElement | null>(null)
onMounted(() => titleInput.value?.focus())

// Client checks mirror the API's DataAnnotations so the user gets instant feedback; the API stays
// the authoritative validator (its 400 field errors are mapped back below).
function validate(): boolean {
  const trimmedTitle = title.value.trim()
  if (!trimmedTitle) {
    titleError.value = 'Title is required.'
  } else if (title.value.length > TITLE_MAX) {
    titleError.value = 'Title must be 200 characters or fewer.'
  } else {
    titleError.value = null
  }

  descriptionError.value =
    description.value.length > DESCRIPTION_MAX
      ? 'Description must be 2000 characters or fewer.'
      : null

  dueDateError.value = dueDate.value ? null : 'Due date is required.'

  return !titleError.value && !descriptionError.value && !dueDateError.value
}

async function onSubmit(): Promise<void> {
  serverError.value = null
  if (!validate()) {
    return
  }

  submitting.value = true
  try {
    const trimmedDescription = description.value.trim()
    const input = {
      title: title.value.trim(),
      description: trimmedDescription === '' ? null : trimmedDescription,
      dueDate: dueDate.value,
    }
    if (props.task) {
      await store.updateTask(props.task.id, { ...input, isCompleted: isCompleted.value })
    } else {
      await store.createTask(input)
    }
    emit('saved')
  } catch (error) {
    if (error instanceof ApiError && error.status === 400) {
      const fields = fieldErrors(error.problem)
      titleError.value = fields.title ?? null
      descriptionError.value = fields.description ?? null
      dueDateError.value = fields.duedate ?? null
      if (!fields.title && !fields.description && !fields.duedate) {
        serverError.value = error.message
      }
    } else if (error instanceof ApiError) {
      // e.g. 404 if the task was deleted elsewhere, or a network error.
      serverError.value = error.message
    } else {
      serverError.value = 'Something went wrong. Please try again.'
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <ModalDialog labelledby="task-form-title" @close="emit('close')">
    <form class="form" novalidate @submit.prevent="onSubmit">
      <header class="form__head">
        <h2 id="task-form-title" class="form__title">{{ isEdit ? 'Edit task' : 'Create task' }}</h2>
        <button type="button" class="form__close" aria-label="Close" @click="emit('close')">
          ×
        </button>
      </header>

      <div class="form__body">
        <div v-if="serverError" class="form__banner" role="alert">{{ serverError }}</div>

        <div class="field">
          <label class="field__label" for="task-title">
            Title <span class="field__req">*</span>
          </label>
          <input
            id="task-title"
            ref="titleInput"
            v-model="title"
            type="text"
            class="field__input"
            :class="{ 'field__input--error': titleError }"
            placeholder="What needs to be done?"
            :aria-invalid="titleError ? 'true' : undefined"
          />
          <div class="field__meta">
            <span v-if="titleError" class="field__error">
              <span aria-hidden="true">!</span>{{ titleError }}
            </span>
            <span
              class="field__counter"
              :class="{ 'field__counter--over': title.length > TITLE_MAX }"
            >
              {{ title.length }} / {{ TITLE_MAX }}
            </span>
          </div>
        </div>

        <div class="field">
          <label class="field__label" for="task-description">
            Description <span class="field__optional">(optional)</span>
          </label>
          <textarea
            id="task-description"
            v-model="description"
            class="field__input field__textarea"
            :class="{ 'field__input--error': descriptionError }"
            rows="3"
            placeholder="Add more detail…"
          ></textarea>
          <div class="field__meta">
            <span v-if="descriptionError" class="field__error">
              <span aria-hidden="true">!</span>{{ descriptionError }}
            </span>
            <span
              class="field__counter"
              :class="{ 'field__counter--over': description.length > DESCRIPTION_MAX }"
            >
              {{ description.length }} / {{ DESCRIPTION_MAX }}
            </span>
          </div>
        </div>

        <div class="field">
          <label class="field__label" for="task-due">
            Due date <span class="field__req">*</span>
          </label>
          <input
            id="task-due"
            v-model="dueDate"
            type="date"
            class="field__input"
            :class="{ 'field__input--error': dueDateError }"
            :aria-invalid="dueDateError ? 'true' : undefined"
          />
          <p v-if="dueDateError" class="field__error">
            <span aria-hidden="true">!</span>{{ dueDateError }}
          </p>
          <p v-else class="field__hint">A calendar date — no time of day.</p>
        </div>

        <label v-if="isEdit" class="form__check">
          <input v-model="isCompleted" type="checkbox" class="form__checkbox" />
          <span>Mark as completed</span>
        </label>

        <div class="form__footer">
          <button type="button" class="btn-cancel" :disabled="submitting" @click="emit('close')">
            Cancel
          </button>
          <button type="submit" class="btn-submit" :disabled="submitting">
            <span v-if="submitting" class="btn-spinner" aria-hidden="true"></span>
            {{ submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create task' }}
          </button>
        </div>
      </div>
    </form>
  </ModalDialog>
</template>

<style scoped>
.form__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-4) var(--sp-5);
  border-bottom: 1px solid var(--c-border);
}

.form__title {
  font-size: 18px;
  font-weight: 700;
}

.form__close {
  padding: 0 4px;
  background: transparent;
  border: none;
  color: var(--c-text-subtle);
  font-size: 22px;
  line-height: 1;
}

.form__close:hover {
  color: var(--c-text);
}

.form__body {
  padding: var(--sp-5);
}

.form__banner {
  margin-bottom: var(--sp-4);
  padding: 11px 12px;
  background: var(--c-overdue-bg);
  border: 1px solid #f3c9c4;
  border-radius: var(--r-sm);
  color: var(--c-overdue);
  font-size: 13px;
  font-weight: 500;
}

.field {
  margin-bottom: var(--sp-4);
}

.field__label {
  display: block;
  margin-bottom: 6px;
  font-size: 13px;
  font-weight: 600;
}

.field__req {
  color: var(--c-overdue);
}

.field__optional {
  color: var(--c-text-subtle);
  font-weight: 400;
}

.field__input {
  width: 100%;
  padding: 10px 12px;
  background: #fff;
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  font-size: 15px;
  color: var(--c-text);
}

.field__input:focus {
  outline: none;
  border-color: var(--c-primary);
  box-shadow: 0 0 0 3px var(--c-primary-weak);
}

.field__input--error {
  border-width: 1.5px;
  border-color: var(--c-overdue);
}

.field__input--error:focus {
  box-shadow: 0 0 0 3px var(--c-overdue-bg);
}

.field__textarea {
  min-height: 72px;
  resize: vertical;
}

.field__meta {
  display: flex;
  gap: 8px;
  margin-top: 5px;
}

.field__error {
  display: flex;
  align-items: center;
  gap: 5px;
  color: var(--c-overdue);
  font-size: 12px;
  font-weight: 500;
}

.field__counter {
  margin-left: auto;
  color: var(--c-text-subtle);
  font-size: 12px;
  white-space: nowrap;
}

.field__counter--over {
  color: var(--c-overdue);
  font-weight: 600;
}

.field__hint {
  margin-top: 6px;
  color: var(--c-text-subtle);
  font-size: 12px;
}

.form__check {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: var(--sp-5);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
}

.form__checkbox {
  width: 18px;
  height: 18px;
  accent-color: var(--c-primary);
  cursor: pointer;
}

.form__footer {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}

.btn-cancel {
  padding: 10px 18px;
  background: transparent;
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  color: var(--c-text);
  font-size: 14px;
  font-weight: 600;
}

.btn-cancel:hover:not(:disabled) {
  background: var(--c-surface-2);
}

.btn-submit {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  background: var(--c-primary);
  border: none;
  border-radius: var(--r-sm);
  color: #fff;
  font-size: 14px;
  font-weight: 600;
}

.btn-submit:hover:not(:disabled) {
  background: var(--c-primary-hover);
}

.btn-cancel:disabled,
.btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.45);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
</style>
