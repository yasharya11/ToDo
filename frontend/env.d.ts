/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the ASP.NET Core API (see .env.example). */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
