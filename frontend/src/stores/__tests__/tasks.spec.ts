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

function emptyResponse(status: number): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    text: async () => '',
    json: async () => null,
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

  it('createTask posts and appends the returned task', async () => {
    const created = task({ id: 99, title: 'New task' })
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(201, created)))
    const store = useTasksStore()

    await store.createTask({ title: 'New task', description: null, dueDate: '2026-07-01' })

    expect(store.tasks).toHaveLength(1)
    expect(store.tasks[0]?.id).toBe(99)
  })

  it('updateTask replaces the task with the server response', async () => {
    const updated = task({ id: 1, title: 'Updated', isCompleted: true })
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, updated)))
    const store = useTasksStore()
    store.tasks.push(task({ id: 1, title: 'Old' }))

    await store.updateTask(1, {
      title: 'Updated',
      description: null,
      dueDate: '2026-07-01',
      isCompleted: true,
    })

    expect(store.tasks[0]?.title).toBe('Updated')
    expect(store.tasks[0]?.isCompleted).toBe(true)
  })

  it('deleteTask removes the task on a 204', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(emptyResponse(204)))
    const store = useTasksStore()
    store.tasks.push(task({ id: 1 }), task({ id: 2 }))

    await store.deleteTask(1)

    expect(store.tasks.map((t) => t.id)).toEqual([2])
  })

  it('toggleComplete PUTs the flipped completion state', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(jsonResponse(200, task({ id: 1, isCompleted: true })))
    vi.stubGlobal('fetch', fetchMock)
    const store = useTasksStore()
    store.tasks.push(task({ id: 1, isCompleted: false }))

    await store.toggleComplete(store.tasks[0]!)

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit
    expect(JSON.parse(init.body as string).isCompleted).toBe(true)
    expect(store.tasks[0]?.isCompleted).toBe(true)
  })
})
