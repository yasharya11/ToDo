import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { request } from '@/api/http'

const TOKEN_KEY = 'tasker.token'
const EMAIL_KEY = 'tasker.email'

// localStorage can throw (private mode, storage disabled), so persistence is best-effort: the
// in-memory refs stay the source of truth and these helpers just degrade quietly.
function readStorage(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

function writeStorage(key: string, value: string | null): void {
  try {
    if (value === null) {
      localStorage.removeItem(key)
    } else {
      localStorage.setItem(key, value)
    }
  } catch {
    // Ignore — see note above.
  }
}

interface AuthResponse {
  accessToken: string
}

/**
 * Authentication state for the SPA: the bearer token and the signed-in email, persisted to
 * localStorage so a page reload stays logged in. Register/login talk to the minimal-JWT API; the
 * token is attached to task requests by the HTTP layer (see configureHttp in main.ts).
 */
export const useAuthStore = defineStore('auth', () => {
  // Hydrate from localStorage so a refresh keeps the session.
  const token = ref<string | null>(readStorage(TOKEN_KEY))
  const email = ref<string | null>(readStorage(EMAIL_KEY))

  const isAuthenticated = computed(() => token.value !== null)

  function setSession(accessToken: string, userEmail: string): void {
    // Normalize for display to match how the API stores it (trimmed + lowercased).
    const normalizedEmail = userEmail.trim().toLowerCase()
    token.value = accessToken
    email.value = normalizedEmail
    writeStorage(TOKEN_KEY, accessToken)
    writeStorage(EMAIL_KEY, normalizedEmail)
  }

  /** Logs in and stores the session. Throws ApiError on bad credentials (401) or validation (400). */
  async function login(userEmail: string, password: string): Promise<void> {
    const response = await request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: { email: userEmail, password },
    })
    setSession(response.accessToken, userEmail)
  }

  /**
   * Registers an account, then logs in with the same credentials so the user lands authenticated
   * (register returns 201 with no token). Throws ApiError on duplicate email (409) or
   * validation (400).
   */
  async function register(userEmail: string, password: string): Promise<void> {
    await request<void>('/api/auth/register', {
      method: 'POST',
      body: { email: userEmail, password },
    })
    await login(userEmail, password)
  }

  /** Clears the session in memory and storage. The caller redirects to /login. */
  function logout(): void {
    token.value = null
    email.value = null
    writeStorage(TOKEN_KEY, null)
    writeStorage(EMAIL_KEY, null)
  }

  return { token, email, isAuthenticated, login, register, logout }
})
