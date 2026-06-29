import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'

describe('App health check', () => {
  it('shows the loading state while the health request is in flight', () => {
    // Keep fetch pending so the component stays in its initial loading state.
    vi.stubGlobal(
      'fetch',
      vi.fn(() => new Promise(() => {})),
    )

    const wrapper = mount(App)

    expect(wrapper.text()).toContain('Checking the API')
  })
})
