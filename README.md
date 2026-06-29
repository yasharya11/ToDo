# ToDo — Task Management App

A small, full-stack task manager: an **ASP.NET Core** Web API + a **Vue 3** SPA, with **SQLite** persistence and minimal **JWT** authentication with per-user task ownership. Built as a take-home assessment.

> **Status: 🚧 in active development.** This README documents the target design and is kept in sync with the code as features land. The **Implementation status** checklist below is the source of truth for what actually works today — nothing here is claimed as complete until it is checked off and verified end-to-end.

## Implementation status

**Phase 1 — Skeleton** (complete)
- [x] Solution + ASP.NET Core Web API project (`net10.0`)
- [x] xUnit integration-test project
- [x] Vue 3 + TypeScript frontend
- [x] Dev CORS + configurable API base URL
- [x] End-to-end smoke test (frontend ↔ backend)
- [x] `.gitignore` / repo hygiene

**Phase 2 — Persistence** (complete)
- [x] `User` + `TaskItem` entities (matches the data model below)
- [x] `AppDbContext` + EF Core SQLite (file-based), registered in DI
- [x] Initial migration, applied automatically on startup
- [x] Verified: DB is created on first run and data survives an API restart

**Phase 3 — Backend CRUD + validation** (complete)
- [x] `TasksController` with `GET /api/tasks`, `GET /api/tasks/{id}`, `POST`, `PUT /{id}` (also toggles completion), `DELETE /{id}`
- [x] Thin async `TaskService` over `DbContext` (async EF Core + `CancellationToken` throughout)
- [x] DTO validation → `400` and missing task → `404`, both as `ProblemDetails`
- [x] Verified: each verb persists to SQLite and returns the correct status code
- [ ] **Not yet auth-scoped.** Tasks are owned by a single seeded dev user; real per-user ownership (and the `Bearer` requirement) arrives in Phase 4. Until then the endpoints are open.

**Later phases** — frontend CRUD flows → minimal JWT auth + ownership → focused tests → final verification.

## Overview

The app lets a user register, log in, and manage a personal to-do list. A task has a title, an optional description, a due date, and a completion state. Authenticated users can create, view, edit, complete/reopen, and delete their own tasks — and only their own: every request is scoped to the signed-in user.

## Tech stack

| Layer | Choice |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), C# |
| Data | Entity Framework Core + SQLite (file-based, persists across restarts) |
| Auth | JWT bearer (access token) + ASP.NET Core `PasswordHasher` (PBKDF2) |
| Frontend | Vue 3 (Composition API) + TypeScript + Vite, Pinia, Vue Router |
| Tests | xUnit + `WebApplicationFactory` (integration) |

## Prerequisites

- **.NET SDK 10.0.x** — https://dotnet.microsoft.com/download
- **Node.js 22** (LTS) — `frontend/.nvmrc` pins it; run `nvm use` inside `frontend/`
- The EF Core CLI is *optional*; migrations are applied automatically on API startup.

## Getting started

**Prerequisites:** .NET 10 SDK and Node 22 (the frontend pins Node 22 via `frontend/.nvmrc`).

You can run **both with one command** (optional, below), or each app **separately** (the Backend / Frontend sections).

### Run both with one command (optional)
From the repo root, with Node 22 active:
```bash
nvm use            # reads .nvmrc → Node 22
npm install        # first run only — installs concurrently
npm run dev        # API + SPA together; Ctrl+C stops both
```
The API serves at **http://localhost:5270** (Scalar at `/scalar`) and the SPA at **http://localhost:5173**.

- Requires **Node 20+** (`concurrently`); Node 22 is needed for the SPA regardless.
- Uses the API's **http** profile (5270) — don't also run the API in Visual Studio (F5) at the same time; they'd collide on 5270.
- Stop with **Ctrl+C** (not by closing the terminal window) so the .NET process exits cleanly.

### Backend (API)
**Visual Studio 2026:** open `backend/ToDo.slnx` and press **F5** (the `https` profile). The API runs at **https://localhost:7168**, with the Scalar API explorer at **https://localhost:7168/scalar**.

**Or from the CLI** (no Visual Studio needed):
```bash
dotnet run --project backend/ToDoApi
```
This uses the `http` profile at **http://localhost:5270**; Scalar is at **http://localhost:5270/scalar**.

### Frontend (SPA)
```bash
cd frontend
nvm use            # reads frontend/.nvmrc → Node 22
npm install        # first run only
npm run dev        # http://localhost:5173
```

> The SPA reads the API base URL from `VITE_API_BASE_URL` (see [`frontend/.env.example`](frontend/.env.example)); it defaults to `http://localhost:5270`. The API allows the SPA dev origin `http://localhost:5173` via a configurable CORS policy (`Cors:AllowedOrigins`).

### Tests
```bash
cd backend
dotnet test
```

### Exploring the API

Once the API is running, you can exercise it two ways — nothing extra to install:
- **Scalar** — open `/scalar` in the browser for interactive API docs: browse every endpoint, try requests, and paste a bearer token to call the authenticated ones.
- **`ToDoApi.http`** — run the requests straight from VS 2026 or VS Code (REST Client extension); it can reuse the token from the `login` response on later requests.

## Project structure
```
ToDo/
├─ backend/
│  ├─ ToDo.slnx
│  ├─ ToDoApi/         ASP.NET Core Web API (one project)
│  └─ ToDoApi.Tests/   xUnit integration tests
├─ frontend/           Vue 3 + TS + Vite SPA
└─ README.md
```

## Architecture & key decisions

This is a small app, and the architecture is deliberately matched to that size. The guiding rule: **every abstraction must solve a real problem in *this* app.**

- **One backend project; no layered split.** No separate Domain/Application/Infrastructure projects, no `Repository`/Unit-of-Work wrappers, no CQRS/MediatR. For one entity and a handful of endpoints, that machinery is pure overhead. EF Core's `DbContext` *is* the unit of work and `DbSet<T>` *is* the repository — wrapping them again would add indirection without value. Controllers call a thin service that uses `DbContext` directly.
- **Frontend and backend are independent apps, not one bundled project.** The Vue SPA (Vite/npm) and the ASP.NET Core API (MSBuild) have separate toolchains and communicate only over an HTTP/JSON contract — with a dev CORS policy and a configurable API base URL. The SPA is just one consumer of the API, so the boundary stays clean, versionable, and independently deployable. The frontend is kept a plain npm project (no IDE-specific project file) so it clones and runs on any editor or OS.
- **SQLite (file), not in-memory.** Data must survive an API restart, so the app uses a file-based SQLite database. EF Core **migrations** define the schema and are applied automatically on startup, so the app runs on a fresh clone with no manual database steps.
- **Instants vs. calendar dates are modeled differently.** `CreatedAtUtc` / `UpdatedAtUtc` are *instants* → `DateTimeOffset` stored in UTC. A task's **due date is a *calendar date*** (not a moment in time) → modeled as `DateOnly`, so "June 30" stays June 30 in every timezone (no off-by-one when the client renders it).
- **Minimal JWT auth, not full ASP.NET Core Identity.** The app uses the framework's vetted primitives — `Microsoft.AspNetCore.Authentication.JwtBearer` to validate tokens and `PasswordHasher<User>` (PBKDF2) to hash passwords — without Identity's `UserManager`/`SignInManager` scaffolding or refresh-token machinery, which this exercise doesn't need. Register/login issue a short-lived access token.
- **Ownership enforced by construction.** Every task carries a `UserId`; every query is scoped to the authenticated user's id. There is no code path that returns another user's task, and cross-user access returns **404** (not 403) so the API doesn't reveal that the row exists.
- **DTOs at the boundary, `ProblemDetails` for errors.** Requests/responses use DTOs rather than exposing EF entities directly, and failures use the standard `ProblemDetails` format.

## Data model

**Task**

| Field | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Title` | string | required, trimmed, ≤ 200 chars |
| `Description` | string? | optional, ≤ 2000 chars |
| `DueDate` | `DateOnly` | required (calendar date; past dates allowed) |
| `IsCompleted` | bool | |
| `CreatedAtUtc` | `DateTimeOffset` | UTC |
| `UpdatedAtUtc` | `DateTimeOffset` | UTC |
| `UserId` | int | owner (FK) |

**User**

| Field | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Email` | string | unique |
| `PasswordHash` | string | PBKDF2 via `PasswordHasher` |

## API

**Health**
- `GET /api/health` → `{ "status": "ok" }` (liveness check; also exercised by the integration test)

**Auth**
- `POST /api/auth/register`
- `POST /api/auth/login` → `{ accessToken }`

**Tasks** — all require `Authorization: Bearer <token>` and are scoped to the current user:
- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `POST /api/tasks`
- `PUT /api/tasks/{id}` (also toggles completion)
- `DELETE /api/tasks/{id}`

## Validation & error handling

- DTO rules: empty/whitespace title rejected, title trimmed and length-capped, due date required, description length-capped. Failures return `400` with `ProblemDetails`.
- `404` when a task does not exist **or** is not owned by the caller.
- `401` for a missing/invalid token; a generic message on bad credentials.
- All errors surface to the user in the UI — never console-only.

## Authentication & ownership

Minimal JWT flow: `register` creates an account (password hashed with `PasswordHasher<User>`), `login` returns a short-lived access token, and the SPA sends it as a `Bearer` token on every request. Every task is associated with its owner via `UserId`, every query is scoped to the authenticated user, and ownership is enforced on list/get/update/delete — verified by automated cross-user tests. *Once auth lands, this section will document how a reviewer can register a test account.*

## Testing

Focused **integration tests** (xUnit + `WebApplicationFactory`) on the two highest-risk areas:
1. **Ownership** — User A cannot read/update/delete User B's task (expects `404`).
2. **Validation** — empty/whitespace title, missing due date, duplicate email, wrong password → correct status codes.

Rationale: a few real end-to-end tests on what could actually hurt beat many shallow tests that assert a button renders.

## Assumptions

- Each account is a single user; no teams, roles, or task sharing.
- A task belongs to exactly one user (the owner) and is never collaborative.
- `DueDate` is a calendar date with no time-of-day; past due dates are allowed (a task can legitimately become overdue).
- Email is the unique account identifier (no separate username).
- Per-user data volume is small, so the list endpoint returns all of a user's tasks without pagination.
- Single-instance deployment for the exercise (one API process, one SQLite file).

## Deliberately excluded (and why)

Scoped out to keep the core complete and honest within the timebox:

- **Lists, tags, subtasks, search/filtering, drag-reorder, recurring tasks, notifications** — product surface beyond a single-entity task manager.
- **Refresh-token rotation/revocation, full ASP.NET Core Identity, email verification, password reset, account lockout, httpOnly-cookie + CSRF, rate limiting** — production auth hardening; access-token-only is the deliberate minimal cut.
- **Postgres/SQL Server, pagination/indexing, response caching, audit logging** — scale concerns beyond this dataset.
- **Docker, CI/CD, deployment config, observability, E2E browser tests** — intentionally out of scope for a timeboxed exercise.

## Scalability

How this would scale beyond the take-home (explained, not built — see *Deliberately excluded*):

- **Stateless API.** Auth is a JWT with no server-side session, so the API scales horizontally behind a load balancer with no sticky sessions.
- **Database first.** SQLite is the binding constraint for concurrent, multi-instance use; the first move is swapping the EF Core provider to a server RDBMS (PostgreSQL / SQL Server), isolated to `DbContext` configuration.
- **Read path.** Add pagination, filtering, and indexes on the task list as data grows; add output/response caching for hot reads; serve the static SPA from a CDN.
- **Heavy / async work.** Anything slow or external (notifications, third-party integrations) moves off the request path to a queue + background worker rather than blocking the API thread.

## What I'd add with another day

- Refresh tokens with rotation, and moving the access token to an httpOnly cookie (+ CSRF) instead of `localStorage`.
- Pagination, filtering, and sorting on the task list (with supporting indexes).
- Optimistic concurrency on update (rowversion) for concurrent edits.
- Structured logging + basic request metrics.
- A small set of end-to-end (Playwright) tests over the critical flows.
- CI to run build + tests on every push.

## Trade-offs (current, honest)

- **Token in `localStorage`** → simple, but XSS-exposed; a production build would use an httpOnly cookie + CSRF protection.
- **Access-token-only** → no server-side revocation before expiry; mitigated by a short token lifetime.
- **Hard delete** → deletes are permanent; no trash/restore.
- **SQLite** → ideal for this exercise; a multi-user production deployment would move to a server RDBMS.
