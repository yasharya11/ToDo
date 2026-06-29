// Single source of truth for where the API lives, so the URL is configured once
// rather than hard-coded at each call site. Override with VITE_API_BASE_URL
// (see frontend/.env.example); defaults to the local API for a fresh clone.
export const API_BASE_URL: string =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5270'

/** Join the configured API base URL with a request path. */
export function apiUrl(path: string): string {
  const base = API_BASE_URL.replace(/\/+$/, '')
  const suffix = path.startsWith('/') ? path : `/${path}`
  return `${base}${suffix}`
}
