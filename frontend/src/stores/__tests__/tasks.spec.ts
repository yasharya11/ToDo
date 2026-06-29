import { afterEach, beforeEach, describe, it, expect, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useTasksStore, type Task } from '../tasks'

function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    text: async () => JSON.stringify(body),
    json: async () => body,
  } as Response
}

function task(overrides: Partial<Task>): Task {
  return {
    id: 1,
    title: 'Task',
    description: null,
    dueDate: '2026-06-30',
    isCompleted: false,
    createdAtUtc: '2026-06-01T00:00:00Z',
    updatedAtUtc: '2026-06-01T00:00:00Z',
    ...overrides,
  }
}

describe('tasks store', () => {
  beforeEach(() => setActivePinia(createPinia()))
  afterEach(() => vi.unstubAllGlobals())

  it('loads tasks into the ready state', async () => {
    const data = [task({ id: 1 }), task({ id: 2 })]
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, data)))
    const store = useTasksStore()

    await store.fetchTasks()

    expect(store.status).toBe('ready')
    expect(store.tasks).toHaveLength(2)
  })

  it('enters the error state when the request fails', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(500, { title: 'oops' })),
    )
    const store = useTasksStore()

    await store.fetchTasks()

    expect(store.status).toBe('error')
    expect(store.tasks).toHaveLength(0)
  })

  it('counts only incomplete, past-due tasks as overdue', async () => {
    const data = [
      task({ id: 1, dueDate: '2020-01-01', isCompleted: false }), // overdue
      task({ id: 2, dueDate: '2020-01-01', isCompleted: true }), // past but done
      task({ id: 3, dueDate: '2999-01-01', isCompleted: false }), // upcoming
    ]
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, data)))
    const store = useTasksStore()

    await store.fetchTasks()

    expect(store.overdueCount).toBe(1)
  })
})
