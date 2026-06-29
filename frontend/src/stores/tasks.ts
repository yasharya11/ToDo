import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { request } from '@/api/http'
import { dueStatus } from '@/utils/date'

/** A task as returned by the API (camelCase JSON of the TaskResponse DTO). */
export interface Task {
  id: number
  title: string
  description: string | null
  dueDate: string // calendar date, "yyyy-MM-dd"
  isCompleted: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

/** The fields a create/edit form submits (matches CreateTaskRequest; update adds isCompleted). */
export interface TaskInput {
  title: string
  description: string | null
  dueDate: string
}

export type LoadStatus = 'idle' | 'loading' | 'ready' | 'error'

/**
 * The signed-in user's tasks: the read path (load + hold the list reactively) plus the CRUD
 * mutations. Each mutation updates the local list from the API's response so the view reflects the
 * change immediately — no refetch — and rethrows on failure so the caller can show a visible error.
 */
export const useTasksStore = defineStore('tasks', () => {
  const tasks = ref<Task[]>([])
  const status = ref<LoadStatus>('idle')

  // Incomplete tasks whose due date is in the past — drives the "N overdue" header count.
  const overdueCount = computed(
    () =>
      tasks.value.filter((task) => !task.isCompleted && dueStatus(task.dueDate) === 'overdue')
        .length,
  )

  /**
   * Loads the current user's tasks. A 401 is handled by the HTTP layer (clear session + redirect
   * to login); any other failure leaves the store in the 'error' state for the retry UI.
   */
  async function fetchTasks(): Promise<void> {
    status.value = 'loading'
    try {
      tasks.value = await request<Task[]>('/api/tasks', { auth: true })
      status.value = 'ready'
    } catch {
      status.value = 'error'
    }
  }

  async function createTask(input: TaskInput): Promise<void> {
    const created = await request<Task>('/api/tasks', { method: 'POST', body: input, auth: true })
    tasks.value.push(created)
  }

  async function updateTask(
    id: number,
    input: TaskInput & { isCompleted: boolean },
  ): Promise<void> {
    const updated = await request<Task>(`/api/tasks/${id}`, {
      method: 'PUT',
      body: input,
      auth: true,
    })
    const index = tasks.value.findIndex((task) => task.id === id)
    if (index !== -1) {
      tasks.value[index] = updated
    }
  }

  async function deleteTask(id: number): Promise<void> {
    await request<void>(`/api/tasks/${id}`, { method: 'DELETE', auth: true })
    tasks.value = tasks.value.filter((task) => task.id !== id)
  }

  /** Flips completion via a full PUT (the API's update endpoint also toggles complete/reopen). */
  async function toggleComplete(task: Task): Promise<void> {
    await updateTask(task.id, {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
      isCompleted: !task.isCompleted,
    })
  }

  return {
    tasks,
    status,
    overdueCount,
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
    toggleComplete,
  }
})
