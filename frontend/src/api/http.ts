import { apiUrl } from './client'

/** ASP.NET Core ProblemDetails — the API's error shape for every non-2xx response. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  // Present on model-validation (400) responses: field name -> list of messages.
  errors?: Record<string, string[]>
}

/**
 * Thrown for any non-2xx response, and for a network failure (with status 0). Carries the parsed
 * ProblemDetails so callers can surface the right field error or user-facing banner.
 */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, message: string, problem: ProblemDetails | null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

// Registered once at startup (see main.ts) so this module stays decoupled from the auth store
// and router — no import cycle. Authenticated requests read the token here and, on a 401, hand
// off to the unauthorized handler (clear the session and return to login).
let tokenProvider: () => string | null = () => null
let unauthorizedHandler: () => void = () => {}

export function configureHttp(config: {
  token: () => string | null
  onUnauthorized: () => void
}): void {
  tokenProvider = config.token
  unauthorizedHandler = config.onUnauthorized
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Attach the bearer token, and treat a 401 as an expired session (logout + redirect). */
  auth?: boolean
}

/**
 * Single entry point for API calls. Serializes the JSON body, attaches the bearer token for
 * authenticated requests, and turns any failure into a typed {@link ApiError}. Returns the parsed
 * JSON body, or undefined for an empty response (201 from register, 204 from delete).
 */
export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = false } = options

  const headers: Record<string, string> = {}
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  if (auth) {
    const token = tokenProvider()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  let response: Response
  try {
    response = await fetch(apiUrl(path), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    // No HTTP response at all: API down, DNS failure, or a blocked CORS preflight.
    throw new ApiError(0, 'Unable to reach the server. Check your connection and try again.', null)
  }

  if (auth && response.status === 401) {
    // An authenticated call was rejected — the token is missing or expired. Clear it and redirect.
    unauthorizedHandler()
  }

  if (!response.ok) {
    const problem = await readProblem(response)
    const message = problem?.detail ?? problem?.title ?? `Request failed (${response.status}).`
    throw new ApiError(response.status, message, problem)
  }

  // 201 (register) and 204 (delete) carry no body.
  const text = await response.text()
  return text ? (JSON.parse(text) as T) : (undefined as T)
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

/**
 * Flattens a ProblemDetails validation payload into a per-field message map with lowercased keys
 * (the API uses PascalCase keys like "Email"/"Password"), keeping the first message per field.
 */
export function fieldErrors(problem: ProblemDetails | null): Record<string, string> {
  const result: Record<string, string> = {}
  if (!problem?.errors) {
    return result
  }
  for (const [key, messages] of Object.entries(problem.errors)) {
    const first = messages[0]
    if (first) {
      result[key.toLowerCase()] = first
    }
  }
  return result
}
