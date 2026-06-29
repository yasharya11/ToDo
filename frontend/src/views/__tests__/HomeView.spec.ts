import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import HomeView from '../HomeView.vue'

describe('HomeView', () => {
  it('shows the loading state while the health request is in flight', () => {
    // Keep fetch pending so the component stays in its initial loading state.
    vi.stubGlobal(
      'fetch',
      vi.fn(() => new Promise(() => {})),
    )

    const wrapper = mount(HomeView)

    expect(wrapper.text()).toContain('Checking the API')
  })
})
