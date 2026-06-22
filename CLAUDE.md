# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

Early scaffolding. Most of the app is not built yet — prefer establishing patterns over
matching a large existing codebase.

## Layout

Three independent areas, deployed as separate containers:

- `backend/` — .NET 10 / C# solution (`backend.sln`).
- `frontend/` — Angular 22 single-page app (npm).
- `deploy/` — Docker Compose stack that runs on the Hetzner server.

## Backend (`backend/`)

- Build: `dotnet build backend/backend.sln`
- Test: `dotnet test backend/backend.sln` (no test project exists yet)
- Run one test (once a test project exists): `dotnet test --filter "FullyQualifiedName~MyTest"`

Currently only the `Domain` class library exists; the `.sln` keeps an empty `src`
solution folder as the placeholder for the future API/host project.

Domain is organized DDD-style: `Domain/Kernel/Entity.cs` is the shared base entity
(assigns a `Guid Id` on construction); each aggregate gets its own folder
(e.g. `Domain/User/`). `Nullable` and `ImplicitUsings` are enabled — don't add `using`
lines for the common namespaces and keep reference types non-null by default.

## Frontend (`frontend/`)

Run all commands from `frontend/`:

- Install: `npm install`
- Dev server: `npm start` (serves http://localhost:4200/)
- Build: `npm run build` (output in `dist/`)
- Test: `npm test` (Angular CLI + Vitest)
- Run a single test by name: `npm test -- -t "describe or it name"`

Formatting is Prettier (`.prettierrc`). Angular standalone-app style: providers are wired
in `src/app/app.config.ts`, routes in `src/app/app.routes.ts`.

## Deploy (`deploy/`)

`docker-compose.yml` is meant to run on the server at `/opt/sollicitations`, not locally.
Key facts before touching it:

- `web` (admin.typhion.com) and `api` (api.admin.typhion.com) attach to the **external**
  `traefik-web` network owned by a separate Traefik stack — this repo does not run
  Traefik. Public routing/TLS is via Traefik labels.
- Postgres (`db`) is on a private `internal` network only — never expose a host port or
  add Traefik labels to it.
- Images come from `ghcr.io/typhion/sollicitations-{web,api}`; CI rewrites only the
  `WEB_IMAGE`/`API_IMAGE` lines in the server's `.env` (SHA-pinned) per deploy. The
  Postgres secrets in `.env` are set once at bootstrap and must be preserved.
- The API reads its connection string from `ConnectionStrings__Default`, targeting the
  `db` service name on the private network.
- `deploy/.env.example` is the template; the real `.env` lives only on the server and is
  never committed.
