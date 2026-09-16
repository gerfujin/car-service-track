# CarServiceTrack — Deployment Guide

> **Historical document.** Previously deployed to a TalTech IT College server behind a
> reverse proxy (`*.proxy.itcollege.ee`) via GitLab CI and Docker Compose. That
> environment is no longer available. This guide otherwise still describes the
> current Docker/CORS/CI setup — see the [README](../README.md#running-locally) for
> how to run the project locally today.

## Table of Contents

1. [Project Deployment Overview](#project-deployment-overview)
2. [Port Mapping](#port-mapping)
3. [Docker Compose Services](#docker-compose-services)
4. [Backend Dockerfile](#backend-dockerfile)
5. [Frontend Dockerfile](#frontend-dockerfile)
6. [Environment Variables](#environment-variables)
7. [CORS](#cors)
8. [GitLab CI/CD](#gitlab-cicd)
9. [Manual Docker Compose Commands](#manual-docker-compose-commands)
10. [EF Core Migrations](#ef-core-migrations)
11. [Frontend Source Layout](#frontend-source-layout)
12. [Troubleshooting](#troubleshooting)

---

## Project Deployment Overview

CarServiceTrack was deployed using Docker Compose and GitLab CI/CD. The frontend
(Vue 3 + Vite + nginx) and the backend (ASP.NET Core 10) were built as separate
Docker images and hosted as separate services on the same server.

- **Frontend** was served by nginx inside a Docker container, exposed on host port `98`.
- **Backend** was an ASP.NET Core 10 application, exposed on host port `99`.
- **Database** was PostgreSQL 16, running as a Docker service with named volume `a5_pgdata`.

GitLab CI/CD still runs unit, integration, and architecture tests on every push to
`main` and on merge requests (see [GitLab CI/CD](#gitlab-cicd)). The `deploy` stage
that used to push `main` to the college server has been removed from
`.gitlab-ci.yml`, since that host is no longer reachable.

---

## Port Mapping

A reverse proxy on the college server used to route two public domains to this
deployment's host ports. Locally, `docker-compose.yml` exposes the same ports
directly, without a proxy in front:

| Service | Host Port | Container Port |
|---|---|---|
| frontend (nginx) | 98 | 80 |
| backend (ASP.NET Core) | 99 | 8080 |

Request flow for the frontend, unchanged whether run locally or (previously) behind
the college proxy:
```
Browser
  → http://localhost:98        (or the public domain, when one existed)
  → Docker container: nginx:80
  → Serves Vue SPA static files
  → JS bundle calls the backend's /api/v1/... over VITE_API_BASE_URL
```

---

## Docker Compose Services

The `docker-compose.yml` at the repo root defines three services, started together
with `docker compose up --build`. All local defaults are safe to run as-is; see
[Environment Variables](#environment-variables) to override them.

### frontend

Builds the Vue 3 SPA from the root `Dockerfile` (Node 20 Alpine build → nginx Alpine serve).

| Property | Value |
|---|---|
| Build context | `.` (repository root) |
| Dockerfile | `Dockerfile` (root) |
| Port mapping | `98:80` |
| Depends on | `backend` |
| Restart policy | `unless-stopped` |

Build argument passed at build time:
```yaml
args:
  VITE_API_BASE_URL: ${VITE_API_BASE_URL:-http://localhost:99}
```

`VITE_API_BASE_URL` is baked into the compiled JS bundle at Docker build time and
cannot be changed at runtime without rebuilding.

### backend

Builds the ASP.NET Core web application from `WebApp/Dockerfile`.

| Property | Value |
|---|---|
| Build context | `.` (repository root) |
| Dockerfile | `WebApp/Dockerfile` |
| Port mapping | `99:8080` |
| Depends on | `db` (waits for healthy) |
| Restart policy | `unless-stopped` |

Environment variables set at runtime (see `docker-compose.yml` for the full list,
including CORS origin and JWT key):
```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=carservicetrack;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
```

Because `ASPNETCORE_ENVIRONMENT` is `Production`, the backend loads `appsettings.json`
and then merges `appsettings.Production.json`, which sets the CORS allowed origins
(overridable via `Cors__AllowedOrigins__0`, see [CORS](#cors)).

### db

PostgreSQL 16 database.

| Property | Value |
|---|---|
| Image | `postgres:16` |
| Volume | `a5_pgdata:/var/lib/postgresql/data` |
| Restart policy | `unless-stopped` |
| Health check | `pg_isready -U postgres -d carservicetrack` (every 5 s, 10 retries) |

The backend service uses `depends_on: db: condition: service_healthy` so it only
starts after the database passes its health check.

---

## Backend Dockerfile

`WebApp/Dockerfile` uses a two-stage build:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore WebApp/WebApp.csproj
RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApp.dll"]
```

- **Stage 1 (build):** Full .NET SDK 10 image. Copies the entire repository, restores
  NuGet packages, and publishes `WebApp` in Release configuration. All
  `appsettings*.json` files are included in the publish output automatically.
- **Stage 2 (runtime):** Lighter ASP.NET Core runtime image. Copies only the
  published output. Sets `ASPNETCORE_URLS=http://+:8080` so the app listens on all
  interfaces on port 8080.
- `ASPNETCORE_ENVIRONMENT=Production` is injected by `docker-compose.yml` at
  container start time, not inside the Dockerfile.

---

## Frontend Dockerfile

The root `Dockerfile` builds the Vue SPA:

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY client-app/package*.json ./
RUN npm ci
COPY client-app/ .
ARG VITE_API_BASE_URL=http://localhost:99
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY client-app/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist /usr/share/nginx/html
```

- **Stage 1 (build):** Node 20 Alpine. Installs dependencies with `npm ci`, then runs
  `vite build`. The `VITE_API_BASE_URL` argument is baked into the JS bundle at this
  stage.
- **Stage 2 (serve):** nginx Alpine serves the compiled `dist/` directory as static
  files using `client-app/nginx.conf`.

`docker-compose.yml` passes `VITE_API_BASE_URL` as a build argument (defaulting to
`http://localhost:99`), so both standalone and compose builds produce the same
result unless overridden.

There is also `client-app/Dockerfile` with an identical pattern — used for
standalone client-only builds.

### How VITE_API_BASE_URL reaches runtime JS

`client-app/src/services/api.ts`:
```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5065'
const API_V1 = `${API_BASE_URL}/api/v1`
```

The `?? 'http://localhost:5065'` fallback is only active in local development when
no environment file is loaded.

---

## Environment Variables

All of these have working local defaults in `docker-compose.yml` — nothing below is
required to run `docker compose up --build`. Override any of them by copying the
root [.env.example](../.env.example) to `.env`.

| Variable | Local default | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:99` | Frontend build-time API base URL; baked into the JS bundle |
| `FRONTEND_ORIGIN` | `http://localhost:98` | Backend CORS allowed origin |
| `POSTGRES_PASSWORD` | `postgres` | Password for both the `db` and backend services |
| `JWT_KEY` | a disposable placeholder committed in `docker-compose.yml` | JWT signing key; generate a real one with `openssl rand -base64 64` for anything beyond a throwaway local stack |

### Local development overrides (outside Docker)

`client-app/.env.development` (committed), used by `npm run dev`:
```
VITE_API_BASE_URL=http://localhost:5065
```

See `client-app/.env.example` for how the committed `.env` / `.env.development` /
`.env.production` files relate to each other.

---

## CORS

The backend's CORS policy allows only the configured frontend origin(s).

**`WebApp/appsettings.Production.json`** (local default; override via
`Cors__AllowedOrigins__0` or `FRONTEND_ORIGIN` in `docker-compose.yml`):
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:98"
    ]
  }
}
```

**How the policy is built** (`WebApp/Program.cs`):

```csharp
var localFrontendOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(x => !string.IsNullOrWhiteSpace(x))
    .Select(x => x.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? Array.Empty<string>();

options.AddPolicy("CorsAllowAll", policy =>
{
    if (localFrontendOrigins.Length > 0)
    {
        policy.WithOrigins(localFrontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("X-Version", "X-Version-Created-At");
        return;
    }
    // Fallback only when no origins are configured (e.g., Testing environment)
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
          .WithExposedHeaders("X-Version", "X-Version-Created-At");
});
```

The policy allows:
- **Origins:** whatever is configured via `Cors:AllowedOrigins` (production/compose) —
  locally, `http://localhost:98`
- **Headers:** Any — covers `Authorization: Bearer <token>`, `Content-Type`, custom headers
- **Methods:** Any — covers GET, POST, PUT, PATCH, DELETE

No `AllowCredentials()` is needed or used — JWT Bearer authentication does not use cookies.

**Development CORS** (`WebApp/appsettings.Development.json`, used by `dotnet run`)
allows `http://localhost:5173` (Vite dev server) and `http://localhost:8080`.

---

## GitLab CI/CD

`.gitlab-ci.yml` defines one stage: `test`.

### `dotnet-tests` job

Runs automatically on every push to `main` and every merge request.

```yaml
script:
  - dotnet restore pesonal.sln
  - dotnet test CarServiceTrack.Tests.Unit/CarServiceTrack.Tests.Unit.csproj
      --configuration Release --no-restore
      --logger "junit;LogFilePath=TestResults/unit-results.xml"
      --collect:"XPlat Code Coverage" --results-directory TestResults/unit
  - dotnet test CarServiceTrack.Tests.Integration/CarServiceTrack.Tests.Integration.csproj
      --configuration Release --no-restore
      --logger "junit;LogFilePath=TestResults/integration-results.xml"
      --collect:"XPlat Code Coverage" --results-directory TestResults/integration
```

JUnit XML reports are published as GitLab test reports (visible in MR). Code
coverage Cobertura XMLs are stored as artifacts (expire in 7 days).

Current test counts: 114 unit tests, 74 integration tests, 15 architecture tests —
all passing (see the root [README](../README.md#tests) for how to run them).

Integration tests use `WebApplicationFactory<Program>` with SQLite in-memory
databases — no PostgreSQL server is needed in CI.

### `e2e-tests` job

Manual trigger only (`when: manual`), `allow_failure: true`. Starts the full stack
with `docker compose -p a5-e2e up --build -d`, runs Playwright tests against
`http://localhost:98`, then tears down with `docker compose -p a5-e2e down`. This
job is self-contained (it only talks to containers it starts itself), so it does
not depend on the retired college server.

### Removed: `deploy` stage

The pipeline previously had a `deploy` stage that ran on every push to `main`:
```yaml
script:
  - docker compose -p a5project down || true
  - docker compose -p a5project up --build --remove-orphans -d
```
This has been removed — it targeted the college server's Docker host, which is no
longer accessible.

> **Note:** `dotnet-tests` and `e2e-tests` both carry `tags: [shared]`, the same tag
> the removed `deploy` job used. Whether that GitLab Runner is generic shared CI
> infrastructure or was tied specifically to the retired server isn't determinable
> from this repo — if pipelines stop picking up jobs, that tag/runner is the first
> thing to check.

---

## Manual Docker Compose Commands

These operate on whatever host you run them on — there's no remote target anymore,
so for a local checkout they act on your own machine.

### Start or restart the stack

```bash
# Stop the current stack
docker compose -p a5project down

# Rebuild all images and start (detached)
docker compose -p a5project up --build --remove-orphans -d

# Verify running containers
docker ps
```

### View logs

```bash
docker compose -p a5project logs -f backend
docker compose -p a5project logs -f frontend
docker compose -p a5project logs -f db
docker compose -p a5project logs -f
```

### Database shell

```bash
docker compose -p a5project exec db psql -U postgres -d carservicetrack
```

### Without Docker

```bash
# Backend (requires PostgreSQL running locally)
dotnet restore pesonal.sln
dotnet run --project WebApp

# Frontend (in a separate terminal)
cd client-app
npm install
npm run dev
# Dev server starts at http://localhost:5173
# VITE_API_BASE_URL=http://localhost:5065 (from .env.development)
```

---

## EF Core Migrations

Each module owns its own `DbContext` and its own migrations folder — there is no
shared `App.DAL.EF` project. Run these from the solution root, targeting the module
you're changing:

```bash
dotnet tool update -g dotnet-ef

# Users module
dotnet ef migrations add <MigrationName> --project Modules/Users/Users.Infrastructure --startup-project WebApp
dotnet ef database update --project Modules/Users/Users.Infrastructure --startup-project WebApp

# Workshops module
dotnet ef migrations add <MigrationName> --project Modules/Workshops/Workshops.Infrastructure --startup-project WebApp
dotnet ef database update --project Modules/Workshops/Workshops.Infrastructure --startup-project WebApp

# Orders module
dotnet ef migrations add <MigrationName> --project Modules/Orders/Orders.Infrastructure --startup-project WebApp
dotnet ef database update --project Modules/Orders/Orders.Infrastructure --startup-project WebApp
```

To remove the last migration for a module, replace `add <MigrationName>` with
`remove` in the same form.

---

## Frontend Source Layout

```
client-app/
├── src/
│   ├── components/      # Shared UI components (NavBar.vue)
│   ├── i18n/             # vue-i18n translations (en.ts, et.ts) + setup (index.ts)
│   ├── router/           # Vue Router routes and auth/role guards (index.ts)
│   ├── services/         # Axios API service layer, one file per resource
│   │                     # (authService, vehicleService, orderService, paymentService,
│   │                     #  serviceService, sparePartService, workshopService,
│   │                     #  repairPhotoService, serviceOrderPartService, profileService)
│   ├── stores/           # Pinia stores (auth.ts, profile.ts)
│   ├── types/            # Shared TypeScript types (index.ts)
│   └── views/            # Page components
│       ├── orders/       # Order list/create/detail/progress/status-update views
│       ├── payments/     # Payment list/detail views
│       ├── services/     # Services catalogue view
│       └── vehicles/     # Vehicle list/create/detail/edit views
├── .env                  # Fallback environment
├── .env.development      # Dev environment (local API URL)
├── .env.production       # Production environment (placeholder — no live deployment)
├── .env.example          # Documents how the above relate to each other
├── Dockerfile            # Standalone client-only Docker build
├── nginx.conf            # Nginx config for the built SPA
├── package.json
└── vite.config.ts
```

---

## Troubleshooting

These apply to running the stack locally with `docker compose up --build`.

### Frontend calls the wrong backend URL

**Symptom:** Browser Network tab shows requests to a stale or unreachable domain
instead of `http://localhost:99`.

**Cause:** `VITE_API_BASE_URL` was set to the wrong value at Docker build time — it's
baked into the bundle and can't be changed at runtime.

**Fix:** Check the `VITE_API_BASE_URL` build arg in `docker-compose.yml` (or your
root `.env`, see [Environment Variables](#environment-variables)), then force a
rebuild: `docker compose -p a5project up --build -d`

---

### CORS error in browser console

**Symptom:** `Access to fetch at 'http://localhost:99/...' from origin
'http://localhost:98' has been blocked by CORS policy`

**Cause:** Backend is not running in Production environment, `appsettings.Production.json`
is missing from the publish output, or `Cors:AllowedOrigins` / `FRONTEND_ORIGIN`
doesn't match the origin the frontend is actually served from.

**Fix 1:** Verify `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT: Production` for
the backend service.

**Fix 2:** Verify `WebApp/appsettings.Production.json` exists and its
`Cors:AllowedOrigins` (or the `FRONTEND_ORIGIN` override) matches the URL you're
loading the frontend from.

**Fix 3:** Rebuild after any config change: `docker compose -p a5project up --build -d`

---

### Backend cannot connect to database

**Symptom:** Backend container exits immediately or logs show
`Unable to obtain data source`. Health check for `db` fails or times out.

**Cause:** Database not yet ready, or volume is corrupted.

**Diagnosis:**
```bash
docker compose -p a5project ps
docker compose -p a5project logs db
```

**Fix (if volume is corrupted — data loss):**
```bash
docker compose -p a5project down -v
docker compose -p a5project up --build -d
```

---

### Wrong Docker Compose project name leaves old containers running

**Symptom:** `docker ps` shows two sets of containers, port conflicts.

**Cause:** A previous run used a different project name (`-p` flag).

**Fix:**
```bash
docker compose -p <old-project-name> down || true
docker compose -p a5project up --build --remove-orphans -d
```

---

### Database volume is empty (missing seed data)

**Symptom:** Login fails with "user not found"; no roles or workshops exist.

**Cause:** Volume was deleted, or `DataInitialization` flags in `appsettings.json`
are set to `false`.

**Check `WebApp/appsettings.json`:**
```json
"DataInitialization": {
  "SeedIdentity": true,
  "SeedData": true,
  "MigrateDatabase": true
}
```

When these flags are enabled, `WebApp/Program.cs` seeds roles, reference data
(workshops, services, spare parts), and three test login accounts on startup — see
the root [README](../README.md#running-locally) for the seeded credentials.

---

### Swagger not accessible

**Symptom:** `http://localhost:99/swagger` returns 404 or timeout.

**Cause:** Backend container is not running or crashed on startup.

**Diagnosis:**
```bash
docker compose -p a5project ps
docker compose -p a5project logs backend
```

Swagger is enabled in all environments (not gated by `IsDevelopment()`).

---

### Login always fails

**Symptom:** POST to `/api/v1/identity/account/login` returns 404, 500, or the
request never reaches the backend.

**Cause 1:** `VITE_API_BASE_URL` is wrong — check the Network tab (see above).

**Cause 2:** JWT configuration missing. Check `JWT__Key` is set (see
[Environment Variables](#environment-variables) — `docker-compose.yml` sets a
working local default, but a standalone `dotnet run` outside Docker needs it set
via `dotnet user-secrets` or an environment variable).

**Cause 3:** The backend has `RequireHttpsMetadata` from `JWT:RequireHttpsMetadata`
in config. This only matters if an external `Authority` is configured (it isn't, by
default) — the symmetric-key JWT validation used here doesn't require HTTPS.
