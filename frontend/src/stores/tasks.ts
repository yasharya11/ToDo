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

export type LoadStatus = 'idle' | 'loading' | 'ready' | 'error'

/**
 * The signed-in user's tasks. #22 owns the read path (load + hold the list reactively so the view
 * and its filters update without a refresh); the create/edit/delete/complete mutations land in #23.
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

  return { tasks, status, overdueCount, fetchTasks }
})
