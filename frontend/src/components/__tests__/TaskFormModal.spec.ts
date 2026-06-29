import { beforeEach, describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import TaskFormModal from '../TaskFormModal.vue'
import type { Task } from '@/stores/tasks'

// Render the teleported dialog content in place so it's queryable through the wrapper.
const mountOptions = { global: { stubs: { teleport: true } } }

describe('TaskFormModal', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('shows required-field errors and does not call the API on empty submit', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TaskFormModal, { props: { task: null }, ...mountOptions })
    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.text()).toContain('Title is required.')
    expect(wrapper.text()).toContain('Due date is required.')
    expect(fetchMock).not.toHaveBeenCalled()
    vi.unstubAllGlobals()
  })

  it('prefills the fields and reads "Edit task" in edit mode', () => {
    const task: Task = {
      id: 1,
      title: 'Existing task',
      description: 'Some detail',
      dueDate: '2026-07-01',
      isCompleted: false,
      createdAtUtc: '',
      updatedAtUtc: '',
    }
    const wrapper = mount(TaskFormModal, { props: { task }, ...mountOptions })

    expect(wrapper.text()).toContain('Edit task')
    expect((wrapper.find('#task-title').element as HTMLInputElement).value).toBe('Existing task')
    expect((wrapper.find('#task-due').element as HTMLInputElement).value).toBe('2026-07-01')
  })
})
