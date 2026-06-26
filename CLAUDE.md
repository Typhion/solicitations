# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Layout

Three independent areas, deployed as separate containers:

- `backend/` — .NET 10 / C# solution (`backend.sln`).
- `frontend/` — Angular 22 single-page app (npm).
- `deploy/` — Docker Compose stack that runs on the Hetzner server.

## Backend (`backend/`)

Clean Architecture, minimal APIs, **no MediatR**. Dependencies point inward:
`WebApi → Infrastructure → Application → Domain`.

- `Domain/` — aggregates + value objects, no framework deps. `Domain/Core/Entity.cs` is the
  base entity (assigns a `Guid Id` in its ctor); `Domain/Core/DomainException.cs` is the
  business-rule violation (maps to 409). Aggregates own their children via **encapsulated
  collections** (e.g. `Solicitation.AddMeeting(...)`, never a public setter).
- `Application/` — use-case services, DTOs, and the **interfaces** Infrastructure implements
  (`ISolicitationRepository`, `ICurrentUser`, `ISecureTokenService`, `IUserRegistrar`,
  `IUserDirectory`, `IEmailSender`, …). References Domain only.
- `Infrastructure/` — EF Core (Npgsql) + ASP.NET Identity + JWT. EF config lives in
  `Persistence/Configurations/` (auto-applied via `ApplyConfigurationsFromAssembly`);
  repositories implement the Application interfaces.
- `WebApi/` — composition root (`Program.cs` wires every layer) + HTTP. Endpoints are
  minimal-API groups in `Api/*Endpoints.cs`; FluentValidation validators in `Validation/`
  run via a generic `ValidationFilter<T>` endpoint filter; `Errors/GlobalExceptionHandler`
  turns exceptions into RFC-7807 ProblemDetails (NotFound→404, DomainException→409, else 500).
- Tests: `Application.Tests.Unit` (xUnit + NSubstitute + FluentAssertions) and
  `Integration.Tests` (`WebApplicationFactory` + **Testcontainers** Postgres).

### Commands (from repo root)

- Build: `dotnet build backend/backend.sln`
- Test (all): `dotnet test backend/backend.sln` — **integration tests need Docker** (Testcontainers)
- One test: `dotnet test backend/backend.sln --filter "FullyQualifiedName~MyTest"`
- Run: `dotnet run --project backend/WebApi` (Swagger/OpenAPI at `/openapi`, health at `/health/live` + `/health/ready`)
- EF migration: `dotnet ef migrations add <Name> --project backend/Infrastructure --startup-project backend/WebApi`
  (applied automatically on boot by `InitialiseDatabaseAsync` → `MigrateAsync`)

### Conventions & gotchas

- `Nullable` + `ImplicitUsings` are on everywhere — don't add `using`s for common namespaces;
  keep reference types non-null (use `= null!` on EF-populated properties).
- **Application feature namespaces are plural** (`Application.Solicitations`) to avoid shadowing
  the singular Domain namespace/type (`Domain.Solicitation.Solicitation`). Reusing the singular
  name on the Application side makes the bare type name bind to the namespace — use the plural.
- Endpoints inject services directly (no mediator). Expected failures **return** a result or
  throw `NotFoundException`/`DomainException`; only unexpected errors throw to the 500 path.
- Auth: short-lived JWT access tokens + rotating, hashed, single-use **refresh tokens** (with
  reuse detection). Registration is **invite-gated** (admin issues invites); `Admin` vs `Member`
  roles drive policies. Per-user data (solicitations) is owner-scoped via `ICurrentUser`.
- Secrets via user-secrets (dev) / env (prod), never in `appsettings`: `Jwt__Key`,
  `Seed__AdminPassword`, `ConnectionStrings__Default`. JWT options are validated on start.
- EF: store enums as strings (`HasConversion<string>()`); aggregate-child entities added through
  a navigation need `ValueGeneratedNever()` on their `Id` (the domain assigns it).
- List endpoints are paginated (`?page=&pageSize=`) returning `PagedResult<T>`.

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
