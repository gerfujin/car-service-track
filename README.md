# CarServiceTrack

A car service management system: clients book vehicles in for service, mechanics work orders and log repairs, admins run the workshop. Full-stack learning project (ASP.NET Core backend, Vue 3 SPA frontend) built as a portfolio piece for junior developer applications.

## Live demo

- Frontend: https://alejeg-finalfront.proxy.itcollege.ee
- Swagger: https://alejeg-a5back.proxy.itcollege.ee/swagger

<!-- ![CarServiceTrack screenshot](docs/screenshot.png) -->

## What it does

The system has three roles, enforced both in the API (`[Authorize(Roles = ...)]`) and in the SPA router.

**Client**
- Register / log in (JWT + refresh tokens)
- Add, edit, and remove their own vehicles
- Create a service order for a vehicle and pick services
- Track order status and progress
- Pay for a completed order

**Mechanic**
- See assigned orders on a dashboard (MVC `Areas/Mechanic`)
- Update order status (`PATCH /api/v1/service-orders/{id}/status`)
- Upload and delete repair photos
- Add spare parts used on an order

**Admin**
- Everything a mechanic can do, plus:
- Manage workshops, mechanics, services, and spare parts (MVC `Areas/Admin`)
- Manage service orders and service order parts across all clients
- Create payments for an order
- A small admin view also exists in the SPA (`/admin/spare-parts`)

<!-- TODO: confirm whether the MVC Areas/Admin and Areas/Mechanic pages are still the primary admin/mechanic UI, or whether the SPA is meant to fully replace them — the router only exposes one admin route. -->

## Tech stack

| Layer | Technology | Version |
|---|---|---|
| Backend | ASP.NET Core (Web API + MVC) | .NET 10 |
| ORM | Entity Framework Core (Npgsql provider) | 10.0.5 / 10.0.1 |
| Database | PostgreSQL | 16 |
| Auth | ASP.NET Core Identity, JWT Bearer | 10.0.5 |
| Inter-module messaging | MediatR | 12.4.1 |
| API docs | Swashbuckle (Swagger/OpenAPI), Asp.Versioning | 10.1.7 / 8.1.1 |
| Architecture enforcement | NetArchTest.Rules | via `Architecture.Tests` |
| Frontend | Vue 3 + TypeScript + Vite | Vue ^3.3.8, Vite ^5.0.0 |
| Frontend state | Pinia | ^2.1.7 |
| Frontend routing | Vue Router | ^4.2.5 |
| HTTP client | Axios | ^1.6.0 |
| i18n | vue-i18n (English + Estonian) | ^9.6.5 |
| E2E tests | Playwright | ^1.44.0 |
| Containers | Docker, Docker Compose, nginx | — |
| CI/CD | GitLab CI | — |

## Architecture

The backend is a **modular monolith**: one deployable ASP.NET Core process (`WebApp`), split into independent modules that only talk to each other through explicit contracts. This is the main thing to look at in this repo — it's enforced by an automated architecture test, not just a convention.

Three modules, each following the same four-layer shape:

```
Modules/
├── Users/          # accounts, identity, vehicles
│   ├── Users.Domain           # entities: Owner, Vehicle, AppUser/AppRole
│   ├── Users.Contracts        # cross-module surface: IUsersUnitOfWork, MediatR requests
│   ├── Users.Application      # services, MediatR handlers
│   ├── Users.Infrastructure   # UsersDbContext, EF Core repositories, migrations
│   └── Users.Presentation     # API controllers + MVC controllers/views
├── Workshops/      # workshops, mechanics, services, spare parts
│   └── (same five projects, prefixed Workshops.*)
└── Orders/         # service orders, order status history, payments, repair photos
    └── (same five projects, prefixed Orders.*)
```

Layer rules:
- **Domain** — plain entities, no dependency on anything outside the module and `Base.Domain`.
- **Contracts** — the only thing another module is allowed to reference: unit-of-work interfaces and MediatR requests/notifications. A `*.Contracts` project cannot depend on any other module at all, including another module's `Contracts`.
- **Application** — MediatR handlers and services, implements the module's own use cases.
- **Infrastructure** — the module's own `DbContext` (each module owns its own EF Core schema and migrations), repositories.
- **Presentation** — API controllers (`ApiControllers/`) and, where the module has an admin/mechanic UI, MVC controllers and views (`Controllers/`, `Areas/`).

Cross-module calls go through MediatR requests defined in the target module's `Contracts` project — e.g. `Workshops.Application` depends on `Users.Contracts`, never on `Users.Domain` or `Users.Infrastructure` directly. `WebApp` is the composition root: it references every module's `Infrastructure` and `Presentation` project and wires them up in `Program.cs` (`AddUsersModule()`, `AddWorkshopsModule()`, `AddOrdersModule()`).

Shared, module-agnostic code lives outside `Modules/`:
- `Base.Domain` / `Base.Contracts` — base entity types and shared interfaces used by all modules.
- `Base.Helpers` — JWT/Identity helpers.
- `App.DTO` — API request/response DTOs shared across module presentation layers.
- `App.Resources` — localization resource files (en/et).

**`Architecture.Tests`** enforces the module boundary with [NetArchTest](https://github.com/BenMorris/NetArchTest): for every module, no `Domain`/`Application`/`Infrastructure`/`Presentation` assembly may reference another module's corresponding layers, and no `Contracts` assembly may reference another module at all. This runs as part of the normal test suite — a stray cross-module `using` fails the build, not a code review.

## Running locally

### With Docker

Requires Docker and Docker Compose.

```bash
docker compose up --build
```

This starts three services (see `docker-compose.yml`): frontend at `http://localhost:98`, backend + Swagger at `http://localhost:99/swagger`, and a PostgreSQL 16 `db` service on a named volume (`a5_pgdata`), internal to the Compose network.

### Without Docker

Requires .NET 10 SDK, Node.js 20+, and a local PostgreSQL 16 instance.

**Backend:**

```bash
dotnet restore pesonal.sln
dotnet ef database update --project Modules/Users/Users.Infrastructure --startup-project WebApp
dotnet ef database update --project Modules/Workshops/Workshops.Infrastructure --startup-project WebApp
dotnet ef database update --project Modules/Orders/Orders.Infrastructure --startup-project WebApp
dotnet run --project WebApp
```

Each module owns its own `DbContext` and migrations. Backend listens on `http://localhost:5065` (Swagger at `/swagger`).

**Frontend** (separate terminal):

```bash
cd client-app
npm install
npm run dev
```

The dev server runs at `http://localhost:5173` and proxies `/api` requests to `http://localhost:5065` (see `vite.config.ts`).

## Configuration

Backend config lives in `appsettings.json` / `appsettings.{Environment}.json`, overridable by environment variables (ASP.NET Core's `__` maps to nested JSON keys):

| Variable | Purpose | Example |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=carservicetrack;Username=postgres;Password=<password>` |
| `JWT__Key` | Symmetric signing key for JWTs | see below |
| `JWT__Issuer` / `JWT__Audience` | JWT issuer/audience claims | `carservicetrack.ee` |
| `Cors__AllowedOrigins__0` | Allowed frontend origin(s) for CORS | `http://localhost:5173` |
| `VITE_API_BASE_URL` | Frontend build-time API base URL | `http://localhost:5065` |

Generate a JWT signing key locally with `openssl rand -base64 64`, then set it via `dotnet user-secrets` in development or as a `JWT__Key` environment variable in any deployed environment — never commit a real key to `appsettings.json`. If `Cors:AllowedOrigins` is left empty, the backend falls back to allowing any origin (`Program.cs`); that fallback is for local/testing use only.

## Tests

Three test projects, 203 tests total, all passing:

| Project | Type | Count |
|---|---|---|
| `CarServiceTrack.Tests.Unit` | Unit tests | 114 |
| `CarServiceTrack.Tests.Integration` | Integration tests (`WebApplicationFactory`, SQLite in-memory) | 74 |
| `Architecture.Tests` | Module-boundary rules (NetArchTest) | 15 |

There is also a Playwright E2E suite under `client-app/e2e/`, run manually in CI (`e2e-tests` job in `.gitlab-ci.yml`) against a full `docker compose` stack.

```bash
dotnet test pesonal.sln                                         # run everything
dotnet test CarServiceTrack.Tests.Unit/CarServiceTrack.Tests.Unit.csproj
```

## Deployment

The project deploys as two Docker images (frontend/nginx and backend) plus a PostgreSQL container, orchestrated by `docker-compose.yml` and built/deployed by GitLab CI (`.gitlab-ci.yml`): tests run on every push and merge request, and `main` auto-deploys on a green build. The frontend bakes its API base URL into the JS bundle at Docker build time (`VITE_API_BASE_URL` build arg), so changing the backend URL requires a rebuild, not just a config change. The backend's CORS policy is environment-driven (`Cors:AllowedOrigins` in `appsettings.{Environment}.json`) and only allows the deployed frontend origin in production.

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for the full deployment guide, including port/proxy mapping, CORS details, manual deployment commands, and troubleshooting.
