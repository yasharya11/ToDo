import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TaskItem from '../TaskItem.vue'
import type { Task } from '@/stores/tasks'

function task(overrides: Partial<Task>): Task {
  return {
    id: 1,
    title: 'Review pull request',
    description: null,
    dueDate: '2999-01-01',
    isCompleted: false,
    createdAtUtc: '',
    updatedAtUtc: '',
    ...overrides,
  }
}

describe('TaskItem', () => {
  it('renders an overdue pill for a past-due incomplete task', () => {
    const wrapper = mount(TaskItem, { props: { task: task({ dueDate: '2020-01-01' }) } })

    expect(wrapper.find('.task__pill').classes()).toContain('task__pill--overdue')
    expect(wrapper.text()).toContain('Overdue')
  })

  it('renders a completed pill and strikes through a done task', () => {
    const wrapper = mount(TaskItem, { props: { task: task({ isCompleted: true }) } })

    expect(wrapper.find('.task__pill').classes()).toContain('task__pill--completed')
    expect(wrapper.classes()).toContain('task--done')
  })

  it('emits toggle/edit/delete from its controls', async () => {
    const wrapper = mount(TaskItem, { props: { task: task({}) } })

    await wrapper.find('.task__check').trigger('click')
    await wrapper.find('.task__btn--danger').trigger('click')

    expect(wrapper.emitted('toggle')).toHaveLength(1)
    expect(wrapper.emitted('delete')).toHaveLength(1)
  })
})
