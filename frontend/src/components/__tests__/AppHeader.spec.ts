import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AppHeader from '../AppHeader.vue'

describe('AppHeader', () => {
  it('renders the Tasker brand', () => {
    const wrapper = mount(AppHeader)

    expect(wrapper.text()).toContain('Tasker')
  })

  it('renders content passed into the actions slot', () => {
    const wrapper = mount(AppHeader, {
      slots: { actions: '<button>Log out</button>' },
    })

    expect(wrapper.find('.app-header__actions').text()).toContain('Log out')
  })
})
