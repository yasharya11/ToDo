import { afterEach, beforeEach, describe, it, expect, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '../auth'
import { ApiError } from '@/api/http'

// Minimal Response stand-ins for the fields request() actually reads (ok/status/text/json).
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

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('starts unauthenticated with no token', () => {
    const auth = useAuthStore()

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.token).toBeNull()
  })

  it('stores the token + normalized email on login and persists them', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, { accessToken: 'jwt-token' })),
    )
    const auth = useAuthStore()

    await auth.login('Dr.Morgan@Clinic.io', 'password123')

    expect(auth.isAuthenticated).toBe(true)
    expect(auth.token).toBe('jwt-token')
    expect(auth.email).toBe('dr.morgan@clinic.io')
    expect(localStorage.getItem('tasker.token')).toBe('jwt-token')
  })

  it('throws ApiError(401) and stays signed out on bad credentials', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(401, { title: 'Invalid credentials.' })),
    )
    const auth = useAuthStore()

    await expect(auth.login('a@b.com', 'wrong')).rejects.toBeInstanceOf(ApiError)
    expect(auth.isAuthenticated).toBe(false)
    expect(localStorage.getItem('tasker.token')).toBeNull()
  })

  it('registers then logs in, ending authenticated', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(emptyResponse(201)) // register -> 201, no body
      .mockResolvedValueOnce(jsonResponse(200, { accessToken: 'jwt-token' })) // auto-login
    vi.stubGlobal('fetch', fetchMock)
    const auth = useAuthStore()

    await auth.register('new@user.io', 'password123')

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.token).toBe('jwt-token')
  })

  it('clears the session on logout', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, { accessToken: 'jwt-token' })),
    )
    const auth = useAuthStore()
    await auth.login('a@b.com', 'password123')

    auth.logout()

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.token).toBeNull()
    expect(localStorage.getItem('tasker.token')).toBeNull()
    expect(localStorage.getItem('tasker.email')).toBeNull()
  })
})
