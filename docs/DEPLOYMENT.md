# Live Deployment Links

- Frontend: https://alejeg-finalfront.proxy.itcollege.ee
- Backend API: https://alejeg-a5back.proxy.itcollege.ee
- Backend Swagger: https://alejeg-a5back.proxy.itcollege.ee/swagger

---

# CarServiceTrack — Deployment Guide

## Table of Contents

1. [Project Deployment Overview](#project-deployment-overview)
2. [Proxy and Port Mapping](#proxy-and-port-mapping)
3. [Docker Compose Services](#docker-compose-services)
4. [Backend Dockerfile](#backend-dockerfile)
5. [Frontend Dockerfile](#frontend-dockerfile)
6. [Environment Variables](#environment-variables)
7. [CORS](#cors)
8. [GitLab CI/CD](#gitlab-cicd)
9. [Manual Deployment Commands](#manual-deployment-commands)
10. [EF Core Migrations](#ef-core-migrations)
11. [Frontend Source Layout](#frontend-source-layout)
12. [Verification Checklist](#verification-checklist)
13. [Troubleshooting](#troubleshooting)
14. [Final Deployment Summary](#final-deployment-summary)

---

## Project Deployment Overview

CarServiceTrack is deployed using Docker Compose and GitLab CI/CD. The frontend (Vue 3 + Vite + nginx) and the backend (ASP.NET Core 10) are built as separate Docker images and hosted as separate services on the same server.

- **Frontend** is served by nginx inside a Docker container, exposed on host port `98`.
- **Backend** is an ASP.NET Core 10 application, exposed on host port `99`.
- **Database** is PostgreSQL 16, running as a Docker service with named volume `a5_pgdata`.

GitLab CI/CD automatically runs unit, integration, and architecture tests on every push to `main` and on merge requests. On every push to `main` the deploy stage runs `docker compose -p a5project up --build`.

---

## Proxy and Port Mapping

The college reverse proxy routes public domains to the deployment server at `<server-ip>`:

| Public URL | Server Host Port | Container Port | Service |
|---|---|---|---|
| https://alejeg-finalfront.proxy.itcollege.ee | 98 | 80 (nginx) | frontend |
| https://alejeg-a5back.proxy.itcollege.ee | 99 | 8080 (ASP.NET Core) | backend |

Request flow for the frontend:
```
Browser
  → https://alejeg-finalfront.proxy.itcollege.ee
  → <server-ip>:98
  → Docker container: nginx:80
  → Serves Vue SPA static files
  → JS bundle calls https://alejeg-a5back.proxy.itcollege.ee/api/v1/...
```

Request flow for a backend API call:
```
Frontend JS / Swagger
  → https://alejeg-a5back.proxy.itcollege.ee/api/v1/...
  → <server-ip>:99
  → Docker container: ASP.NET Core :8080
  → Returns JSON
```

---

## Docker Compose Services

The `docker-compose.yml` defines three services. All are started under the Docker Compose project name **`a5project`**.

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
  VITE_API_BASE_URL: https://alejeg-a5back.proxy.itcollege.ee
```

`VITE_API_BASE_URL` is baked into the compiled JS bundle at Docker build time and cannot be changed at runtime without rebuilding.

### backend

Builds the ASP.NET Core web application from `WebApp/Dockerfile`.

| Property | Value |
|---|---|
| Build context | `.` (repository root) |
| Dockerfile | `WebApp/Dockerfile` |
| Port mapping | `99:8080` |
| Depends on | `db` (waits for healthy) |
| Restart policy | `unless-stopped` |

Environment variables set at runtime:
```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=carservicetrack;Username=postgres;Password=<password>
```

Because `ASPNETCORE_ENVIRONMENT` is `Production`, the backend loads `appsettings.json` and then merges `appsettings.Production.json`, which sets the production CORS allowed origins.

### db

PostgreSQL 16 database.

| Property | Value |
|---|---|
| Image | `postgres:16` |
| Volume | `a5_pgdata:/var/lib/postgresql/data` |
| Restart policy | `unless-stopped` |
| Health check | `pg_isready -U postgres -d carservicetrack` (every 5 s, 10 retries) |

The backend service uses `depends_on: db: condition: service_healthy` so it only starts after the database passes its health check.

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

- **Stage 1 (build):** Full .NET SDK 10 image. Copies the entire repository, restores NuGet packages, and publishes `WebApp` in Release configuration. All `appsettings*.json` files are included in the publish output automatically.
- **Stage 2 (runtime):** Lighter ASP.NET Core runtime image. Copies only the published output. Sets `ASPNETCORE_URLS=http://+:8080` so the app listens on all interfaces on port 8080.
- `ASPNETCORE_ENVIRONMENT=Production` is injected by `docker-compose.yml` at container start time, not inside the Dockerfile.

---

## Frontend Dockerfile

The root `Dockerfile` builds the Vue SPA:

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY client-app/package*.json ./
RUN npm ci
COPY client-app/ .
ARG VITE_API_BASE_URL=https://alejeg-a5back.proxy.itcollege.ee
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY client-app/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist /usr/share/nginx/html
```

- **Stage 1 (build):** Node 20 Alpine. Installs dependencies with `npm ci`, then runs `vite build`. The `VITE_API_BASE_URL` argument is baked into the JS bundle at this stage.
- **Stage 2 (serve):** nginx Alpine serves the compiled `dist/` directory as static files using `client-app/nginx.conf`.

The default value for `VITE_API_BASE_URL` in the Dockerfile is `https://alejeg-a5back.proxy.itcollege.ee`. `docker-compose.yml` passes the same value as a build argument, so both standalone and compose builds produce the same result.

There is also `client-app/Dockerfile` with an identical pattern — used for standalone client-only builds.

### How VITE_API_BASE_URL reaches runtime JS

`client-app/src/services/api.ts`:
```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5065'
const API_V1 = `${API_BASE_URL}/api/v1`
```

The `?? 'http://localhost:5065'` fallback is only active in local development when no environment file is loaded.

---

## Environment Variables

### Backend (set by docker-compose.yml at runtime)

| Variable | Value | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activates `appsettings.Production.json`; controls CORS and error handling |
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=carservicetrack;Username=postgres;Password=<password>` | PostgreSQL connection string using the Docker service name `db` |

### Database (set by docker-compose.yml)

| Variable | Description |
|---|---|
| `POSTGRES_DB` | Database name created on first start (`carservicetrack`) |
| `POSTGRES_USER` | PostgreSQL superuser |
| `POSTGRES_PASSWORD` | Superuser password — set to a real secret outside of local/dev use |

### Frontend (set at Docker build time)

| Variable | Value | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `https://alejeg-a5back.proxy.itcollege.ee` | Baked into JS bundle; cannot be changed without rebuild |

### Local development overrides

`client-app/.env.development` (committed):
```
VITE_API_BASE_URL=http://localhost:5065
```

`client-app/.env` (committed, used when no environment-specific file matches):
```
VITE_API_BASE_URL=https://alejeg-a5back.proxy.itcollege.ee
```

---

## CORS

The production backend explicitly allows cross-origin requests only from the deployed frontend.

**`WebApp/appsettings.Production.json`:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://alejeg-finalfront.proxy.itcollege.ee"
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
- **Origins:** `https://alejeg-finalfront.proxy.itcollege.ee` (production only)
- **Headers:** Any — covers `Authorization: Bearer <token>`, `Content-Type`, custom headers
- **Methods:** Any — covers GET, POST, PUT, PATCH, DELETE

No `AllowCredentials()` is needed or used — JWT Bearer authentication does not use cookies.

**Development CORS** (`WebApp/appsettings.Development.json`) includes:
- `http://localhost:5173` (Vite dev server)
- `http://localhost:8080`
- `https://alejeg-finalfront.proxy.itcollege.ee`

---

## GitLab CI/CD

`.gitlab-ci.yml` defines two stages: `test` and `deploy`.

### Stage: test

#### `dotnet-tests` job

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

JUnit XML reports are published as GitLab test reports (visible in MR). Code coverage Cobertura XMLs are stored as artifacts (expire in 7 days).

Current test counts: 114 unit tests, 74 integration tests, 15 architecture tests — all passing (see the root [README](../README.md#tests) for how to run them).

Integration tests use `WebApplicationFactory<Program>` with SQLite in-memory databases — no PostgreSQL server is needed in CI.

#### `e2e-tests` job

Manual trigger only (`when: manual`), `allow_failure: true`. Starts the full stack with `docker compose -p a5-e2e up --build -d`, runs Playwright tests against `http://localhost:98`, then tears down with `docker compose -p a5-e2e down`.

### Stage: deploy

Runs only on push to `main`:

```yaml
script:
  - docker compose -p a5project down || true
  - docker compose -p a5project up --build --remove-orphans -d
```

Stops the existing `a5project` deployment, rebuilds all images, and starts fresh. `--remove-orphans` removes containers for services removed from the compose file.

---

## Manual Deployment Commands

### Deploy or redeploy

```bash
# Stop current deployment
docker compose -p a5project down

# Rebuild all images and start (detached)
docker compose -p a5project up --build --remove-orphans -d

# Verify running containers
docker ps
```

### View logs

```bash
# Backend logs (live stream)
docker compose -p a5project logs -f backend

# Frontend logs (live stream)
docker compose -p a5project logs -f frontend

# Database logs (live stream)
docker compose -p a5project logs -f db

# All services combined
docker compose -p a5project logs -f
```

### Database shell

```bash
docker compose -p a5project exec db psql -U postgres -d carservicetrack
```

### Local development

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

Each module owns its own `DbContext` and its own migrations folder — there is no shared `App.DAL.EF` project. Run these from the solution root, targeting the module you're changing:

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

To remove the last migration for a module, replace `add <MigrationName>` with `remove` in the same form.

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
├── .env                  # Default environment (production API URL)
├── .env.development      # Dev environment (local API URL)
├── .env.production       # Production environment (documents the production API URL)
├── Dockerfile            # Standalone client-only Docker build
├── nginx.conf            # Nginx config for the built SPA
├── package.json
└── vite.config.ts
```

---

## Verification Checklist

After deployment, verify:

- [ ] https://alejeg-finalfront.proxy.itcollege.ee opens — frontend loads, navbar is visible
- [ ] https://alejeg-a5back.proxy.itcollege.ee/swagger opens — all API endpoint groups are listed
- [ ] Register a new user via frontend — succeeds, redirects to home
- [ ] Login with registered credentials — JWT received, stored in browser
- [ ] Vehicles page loads — authenticated GET request returns data
- [ ] DevTools → Network: API requests go to `https://alejeg-a5back.proxy.itcollege.ee`
- [ ] No CORS errors in browser console
- [ ] `docker ps` shows backend with `0.0.0.0:99->8080/tcp`
- [ ] `docker ps` shows frontend with `0.0.0.0:98->80/tcp`
- [ ] `docker ps` shows `db` container as `(healthy)`
- [ ] JWT-protected endpoints return data (not 401)
- [ ] Admin panel does not use ViewBag/ViewData (page titles come from ViewModels)
- [ ] CI/CD deploy job on GitLab shows green for `main`
- [ ] CI deploy job uses project name `a5project` (check job log)
- [ ] Unit tests: `dotnet test CarServiceTrack.Tests.Unit --configuration Release` → 114 passed
- [ ] Integration tests: `dotnet test CarServiceTrack.Tests.Integration --configuration Release` → 74 passed
- [ ] Architecture tests: `dotnet test Architecture.Tests --configuration Release` → 15 passed

---

## Troubleshooting

### Frontend calls old backend URL or localhost

**Symptom:** Browser Network tab shows requests to `localhost:5065` or a stale domain.

**Cause:** `VITE_API_BASE_URL` was either not set or was set to the wrong value at Docker build time. The fallback `http://localhost:5065` in `api.ts` activates.

**Fix:** Confirm `docker-compose.yml` build args:
```yaml
args:
  VITE_API_BASE_URL: https://alejeg-a5back.proxy.itcollege.ee
```
Then force a rebuild: `docker compose -p a5project up --build -d`

---

### CORS error in browser console

**Symptom:** `Access to fetch at 'https://alejeg-a5back...' from origin 'https://alejeg-finalfront...' has been blocked by CORS policy`

**Cause:** Backend is not running in Production environment, or `appsettings.Production.json` is missing / not included in the publish output.

**Fix 1:** Verify `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT: Production` for the backend service.

**Fix 2:** Verify `WebApp/appsettings.Production.json` exists and contains:
```json
{
  "Cors": {
    "AllowedOrigins": ["https://alejeg-finalfront.proxy.itcollege.ee"]
  }
}
```

**Fix 3:** Rebuild after any config change: `docker compose -p a5project up --build -d`

---

### Backend cannot connect to database

**Symptom:** Backend container exits immediately or logs show `Unable to obtain data source`. Health check for `db` fails or times out.

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

**Symptom:** `docker ps` shows two sets of containers (old and new), port conflicts.

**Cause:** A previous deployment used a different project name.

**Fix:**
```bash
docker compose -p <old-project-name> down || true
docker compose -p a5project up --build --remove-orphans -d
```

---

### Proxy points to wrong port

**Symptom:** Public URLs return connection refused or 502, but containers are running.

**Cause:** The server-side proxy configuration maps the public domain to the wrong host port.

**Verify:**
- `alejeg-finalfront.proxy.itcollege.ee` must proxy to `<server-ip>:98`
- `alejeg-a5back.proxy.itcollege.ee` must proxy to `<server-ip>:99`

Check with: `docker ps` — look for `0.0.0.0:98->80/tcp` and `0.0.0.0:99->8080/tcp`.

---

### Database volume is empty (missing seed data)

**Symptom:** Login fails with "user not found"; no roles or workshops exist.

**Cause:** Volume was deleted or `DataInitialization` flags in `appsettings.json` are set to `false`.

**Check `WebApp/appsettings.json`:**
```json
"DataInitialization": {
  "SeedIdentity": true,
  "SeedData": true,
  "MigrateDatabase": true
}
```

Seed identities and reference data (roles, a couple of demo workshops, services, and spare parts) are created by `WebApp/Program.cs` on startup when these flags are enabled. This project's public deployment has working seeded accounts; they are intentionally not listed here — do not assume a fixed set of credentials, and rotate/replace them before treating this configuration as a template for a real deployment.

---

### Swagger not accessible

**Symptom:** `https://alejeg-a5back.proxy.itcollege.ee/swagger` returns 404 or timeout.

**Cause:** Backend container is not running or crashed on startup.

**Diagnosis:**
```bash
docker compose -p a5project ps
docker compose -p a5project logs backend
```

Swagger is enabled in all environments (not gated by `IsDevelopment()`).

---

### Frontend loads but login always fails

**Symptom:** POST to `/api/v1/identity/account/login` returns 404, 500, or the request never reaches the backend.

**Cause 1:** `VITE_API_BASE_URL` is wrong — check Network tab (see above).

**Cause 2:** JWT configuration missing or default placeholder used. Check `WebApp/appsettings.json`:
```json
"JWT": {
  "Key": "...",
  "Issuer": "carservicetrack.ee",
  "Audience": "carservicetrack.ee"
}
```

**Cause 3:** The backend `RequireHttpsMetadata = true` in `Program.cs` — test client must use HTTPS. In production this is correct; in local testing use HTTPS or override the setting.

---

## Final Deployment Summary

| Item | Value |
|---|---|
| Frontend | https://alejeg-finalfront.proxy.itcollege.ee |
| Backend API | https://alejeg-a5back.proxy.itcollege.ee |
| Swagger | https://alejeg-a5back.proxy.itcollege.ee/swagger |
| Frontend port | 98:80 |
| Backend port | 99:8080 |
| Compose project | a5project |
| Database service | db |
| Database volume | a5_pgdata |
| Backend environment | Production |
| Production CORS | https://alejeg-finalfront.proxy.itcollege.ee |
| CI/CD | GitLab CI — stages: test → deploy |
| Unit tests | 114 passed |
| Integration tests | 74 passed |
| Architecture tests | 15 passed |
