import { beforeEach, describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import LoginView from '../LoginView.vue'

// A bare router so useRoute/useRouter/RouterLink resolve; the other routes are stubs.
const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/', name: 'home', component: { template: '<div />' } },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/register', name: 'register', component: { template: '<div />' } },
  ],
})

describe('LoginView', () => {
  let pinia: Pinia

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
  })

  it('shows required-field errors and does not call the API on empty submit', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)

    await router.push('/login')
    await router.isReady()
    const wrapper = mount(LoginView, { global: { plugins: [router, pinia] } })

    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.text()).toContain('Email is required.')
    expect(wrapper.text()).toContain('Password is required.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('rejects a malformed email (e.g. "yash") without calling the API', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)

    await router.push('/login')
    await router.isReady()
    const wrapper = mount(LoginView, { global: { plugins: [router, pinia] } })

    await wrapper.find('#login-email').setValue('yash')
    await wrapper.find('#login-password').setValue('password123')
    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.text()).toContain('A valid email address is required.')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
