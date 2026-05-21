# CarServiceTrack — Assignment 4 Deployment Documentation

> This file covers deployment, architecture, and defence checklist for Assignment 4 / Phase 2.
> It does **not** replace `README.md`.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Assignment 4 Requirement Mapping](#2-assignment-4-requirement-mapping)
3. [Architecture](#3-architecture)
4. [Domain Entities](#4-domain-entities)
5. [REST API](#5-rest-api)
6. [Authentication](#6-authentication)
7. [MVC and Admin UX](#7-mvc-and-admin-ux)
8. [Translations](#8-translations)
9. [Separate Frontend Client](#9-separate-frontend-client)
10. [Docker Deployment](#10-docker-deployment)
11. [GitLab CI/CD](#11-gitlab-cicd)
12. [Test Coverage](#12-test-coverage)
13. [Manual Deployment Verification](#13-manual-deployment-verification)
14. [Troubleshooting](#14-troubleshooting)
15. [Final Defence Notes](#15-final-defence-notes)

---

## 1. Project Overview

**CarServiceTrack** is a car service management application that helps workshops, mechanics, and vehicle owners track repair orders, payments, and service history.

**Problem it solves:** Manual paper-based tracking of service orders is error-prone and hard to share between workshop staff and customers. CarServiceTrack provides a digital record of every vehicle's service history, parts used, payments, and repair photos.

**User roles:**

| Role | Description |
|------|-------------|
| `client` | Vehicle owner. Registers vehicles, creates and tracks service orders, views payments. |
| `mechanic` | Workshop staff. Sees assigned orders, updates order status. |
| `admin` | Full access. Manages workshops, mechanics, services catalogue, spare parts, all orders. |

**Main features:**

- Register vehicles and link them to an owner account
- Create service orders for a vehicle at a specific workshop
- Track order status through lifecycle: Pending → Accepted → InProgress → WaitingForParts → Completed / Cancelled
- Record services performed and spare parts used per order
- Payment tracking with payment method and status
- Upload repair photos per order
- Admin area for managing the services and parts catalogue
- Mechanic area for viewing assigned orders
- REST API with JWT authentication consumed by a separate Vue 3 frontend
- UI available in English and Estonian

---

## 2. Assignment 4 Requirement Mapping

| Requirement | Where implemented | Status |
|---|---|---|
| Clean/Onion architecture | `Base.Contracts`, `Base.Domain`, `App.Domain`, `App.DAL.Contracts`, `App.DAL.EF`, `App.BLL`, `App.DTO`, `WebApp` | Done |
| Domain design — minimum 10 meaningful entities | 15 entities: see [Section 4](#4-domain-entities) | Done |
| REST API controllers | `WebApp/ApiControllers/` — Vehicles, Services, ServiceOrders, SpareParts, Workshops, Mechanics, Payments, ServiceOrderParts, RepairPhotos + AccountController | Done |
| API versioning | URL segment versioning via `Asp.Versioning`; route: `/api/v{version:apiVersion}/[controller]`; currently v1 | Done |
| Public DTOs | `App.DTO/v1/` — VehicleDto, ServiceOrderDto, PaymentDto, SparePartDto, WorkshopDto, MechanicDto, ServiceDto, RepairPhotoDto, ServiceOrderPartDto + Identity DTOs | Done |
| Swagger | Enabled in `WebApp/Program.cs`; multi-version UI; URL: `/swagger` | Done |
| JWT authentication | `WebApp/Program.cs` JwtBearer scheme; key from config `JWT:Key`; `AccountController` login endpoint | Done |
| Refresh tokens | `AppRefreshToken` entity; `RefreshTokenRepository`; `POST /api/v1/identity/account/refreshtokendata`; token rotation on refresh | Done |
| MVC client UX | `WebApp/Controllers/` — HomeController, VehiclesController, ServiceOrdersController, PaymentsController, ListItemsController | Done |
| Admin Area UX | `WebApp/Areas/Admin/` — Dashboard, Workshops, Mechanics, Services, SpareParts, ServiceOrders, ServiceOrderParts | Done |
| ViewModels instead of ViewBag/ViewData | `WebApp/ViewModels/`, `WebApp/Areas/Admin/ViewModels/`, `WebApp/Areas/Mechanic/ViewModels/` | Done |
| UI translations with resx | `App.Resources/` — `Domain/Person.resx`, `Views/Home.resx`, `Views/CarService.resx` + Estonian variants (`.et.resx`) | Done |
| DB translations with LangStr | `Base.Domain/LangStr.cs`; used in `Service.Name`, `Service.Description`, `Workshop.Name`, `Workshop.Address`, `SparePart.Name`, `ListItem.Summary` | Done |
| IDOR protection | `CarServiceTrack.Tests.Integration/Controllers/IborSecurityTests.cs`; ownership checks in BLL services | Done |
| Repositories | `App.DAL.EF/Repositories/` — one repository per entity; `BaseRepository` base class | Done |
| Unit of Work | `App.DAL.EF/Repositories/AppUnitOfWork.cs` implements `IAppUnitOfWork` | Done |
| BLL/services | `App.BLL/` — one service per entity; `AppBll` facade; `IAppBll` interface | Done |
| Mappers | `App.BLL/Mappers/` — VehicleMapper, ServiceOrderMapper, PaymentMapper, etc. | Done |
| Test coverage | 117 unit tests + 57 integration tests = 174 total; see [Section 12](#12-test-coverage) | Done |
| Separate frontend client | Vue 3 SPA in `client-app/`; deployed to separate domain | Done |
| Frontend deploy to separate domain | `https://alejeg-front.proxy.itcollege.ee` (nginx container, port 81) | Done |
| CORS handling | `Program.cs` policy `CorsAllowAll`; allowed origins from config `Cors:AllowedOrigins` | Done |
| CI/CD deploy for app + DB + frontend | `.gitlab-ci.yml` — `dotnet-tests` stage + `deploy` stage; docker compose handles all three services | Done |

---

## 3. Architecture

CarServiceTrack follows the **Clean / Onion architecture** pattern. Each layer only depends on layers closer to the centre; domain and contracts never depend on infrastructure.

```
┌─────────────────────────────────────────────────┐
│  WebApp  (Presentation layer)                   │
│  API controllers, MVC controllers, Views,       │
│  Areas/Admin, Areas/Mechanic, Program.cs        │
│  ↓ depends on                                   │
├─────────────────────────────────────────────────┤
│  App.BLL  (Business Logic Layer)                │
│  AppBll facade, service classes, BLL DTOs,      │
│  mappers between domain and BLL DTOs            │
│  ↓ depends on                                   │
├─────────────────────────────────────────────────┤
│  App.DAL.Contracts  (DAL interfaces)            │
│  IAppUnitOfWork, IVehicleRepository, etc.       │
│  (no EF dependency here)                        │
│  ↓ implemented by                              │
├─────────────────────────────────────────────────┤
│  App.DAL.EF  (Data Access Layer — EF Core)      │
│  AppDbContext (PostgreSQL), AppUnitOfWork,      │
│  concrete repository classes                    │
│  ↓ talks to                                     │
├─────────────────────────────────────────────────┤
│  Database (PostgreSQL)                          │
└─────────────────────────────────────────────────┘

Independent layers (no outward dependencies):
  App.Domain  →  entity classes, enums, AppUser/AppRole/AppRefreshToken
  Base.Domain →  LangStr, base entity classes
  Base.Contracts → base repository/UoW interfaces
  Base.Helpers →  shared utilities
  App.DTO     →  public API DTOs (versioned under v1/)
  App.Resources → resx translation files
```

**Important rules followed in this project:**

- API and MVC controllers call **BLL services only** — never repositories or DbContext directly.
- DAL EF repositories are never injected into controllers.
- `App.DTO` DTOs are used for the public REST API (serialized to/from JSON).
- `ViewModels` (not `ViewBag`/`ViewData`) carry data to Razor views.
- `LangStr` stores multilingual strings in the database as a JSON dictionary.

**Frontend (separate project):**

```
client-app/   (Vue 3 + Vite SPA)
  ↓ calls REST API over HTTP
WebApp ApiControllers  (JWT-authenticated)
```

---

## 4. Domain Entities

All entities live in `App.Domain/` and `App.Domain/Identity/`.

| Entity | Purpose | Important relationships |
|--------|---------|------------------------|
| `AppUser` | Identity user (extends IdentityUser\<Guid\>) | Has many `AppRefreshToken`; has one `Owner` |
| `AppRole` | Identity role (extends IdentityRole\<Guid\>) | — |
| `AppRefreshToken` | Stores JWT refresh tokens with expiry and rotation support | Belongs to `AppUser` |
| `Owner` | Vehicle owner profile linked to a user account | Belongs to `AppUser`; has many `Vehicle` |
| `Vehicle` | A car registered by an owner | Belongs to `Owner`; has many `ServiceOrder` |
| `Workshop` | A repair workshop (Name and Address are multilingual) | Has many `MechanicInWorkshop`; has many `ServiceOrder` |
| `Mechanic` | A mechanic employee (can have an AppUser account) | Belongs to `AppUser` (optional); linked to workshops via `MechanicInWorkshop`; has many `ServiceOrder` |
| `MechanicInWorkshop` | Junction — assigns a mechanic to a workshop with a date range | Belongs to `Mechanic` and `Workshop` |
| `Service` | A service type in the catalogue (e.g. Oil Change) — Name and Description multilingual | Has many `ServiceOrderItem` |
| `SparePart` | A spare part in the catalogue — Name multilingual | Has many `ServiceOrderPart` |
| `ServiceOrder` | A repair order for a vehicle at a workshop | Belongs to `Vehicle`, `Workshop`, `Mechanic`; has many `ServiceOrderItem`, `ServiceOrderPart`, `ServiceOrderStatusHistory`, `RepairPhoto`; has one `Payment` |
| `ServiceOrderItem` | A service performed within an order (quantity + price) | Belongs to `ServiceOrder` and `Service` |
| `ServiceOrderPart` | A spare part used in an order (quantity + price) | Belongs to `ServiceOrder` and `SparePart` |
| `ServiceOrderStatusHistory` | Audit trail of status changes for an order | Belongs to `ServiceOrder` |
| `Payment` | Payment record for a service order | Belongs to `ServiceOrder` |
| `RepairPhoto` | Uploaded photo file for a service order | Belongs to `ServiceOrder` |
| `ListItem` | Demo to-do item (used in earlier phases) | Belongs to `AppUser` |

**Enums (`App.Domain/Enums/`):**

- `ServiceOrderStatus`: Pending, Accepted, InProgress, WaitingForParts, Completed, Cancelled
- `PaymentStatus`: Pending, Paid, PartiallyPaid, Refunded, Cancelled

**Total meaningful domain entities: 15** (Owner, Vehicle, Workshop, Mechanic, MechanicInWorkshop, Service, SparePart, ServiceOrder, ServiceOrderItem, ServiceOrderPart, ServiceOrderStatusHistory, Payment, RepairPhoto, AppUser/AppRole/AppRefreshToken).

---

## 5. REST API

### Base path and versioning

```
/api/v{version}/[controller]
```

- Versioning: **URL segment** — the version number is embedded in the URL path.
- Package: `Asp.Versioning` (configured in `WebApp/Program.cs`).
- Current version: **v1** (`[ApiVersion("1.0")]` on all controllers).
- `SubstituteApiVersionInUrl: true` — `{version}` in the route template is replaced automatically.

### Swagger

```
https://alejeg-pp.proxy.itcollege.ee/swagger
```

Swagger UI lists all versioned API groups. On localhost during development:
```
https://localhost:{port}/swagger
```

### Authentication endpoints (`WebApp/ApiControllers/Identity/AccountController.cs`)

| Method | URL | Auth required | Description |
|--------|-----|--------------|-------------|
| POST | `/api/v1/identity/account/register` | No | Register new user |
| POST | `/api/v1/identity/account/login` | No | Login; returns JWT + refresh token |
| POST | `/api/v1/identity/account/refreshtokendata` | No | Exchange refresh token for new JWT |
| POST | `/api/v1/identity/account/logout` | JWT | Logout; invalidates refresh token |

Query parameter `expiresInSeconds` on register/login controls JWT lifetime.

### Main resource endpoints

All resource endpoints require a valid JWT Bearer token (`Authorization: Bearer <token>`).

| Controller | Base route | Notes |
|-----------|-----------|-------|
| `VehiclesController` | `/api/v1/vehicles` | GET all (authenticated); POST/PUT/DELETE require role `admin` or `client`; ownership checked in BLL |
| `ServicesController` | `/api/v1/services` | GET all/by id: public; POST/PUT/DELETE: role `admin` only |
| `ServiceOrdersController` | `/api/v1/serviceorders` | Full CRUD; authenticated |
| `SparePartsController` | `/api/v1/spareparts` | Full CRUD; authenticated |
| `WorkshopsController` | `/api/v1/workshops` | Full CRUD; authenticated |
| `MechanicsController` | `/api/v1/mechanics` | Full CRUD; authenticated |
| `PaymentsController` | `/api/v1/payments` | Full CRUD; authenticated |
| `ServiceOrderPartsController` | `/api/v1/serviceorderparts` | Full CRUD; authenticated |
| `RepairPhotosController` | `/api/v1/repairphotos` | Upload and retrieve repair photos; authenticated |

### IDOR protection

User-owned resources (vehicles, service orders, payments) check in the BLL that the requesting user's ID matches the resource owner. Integration tests for this are in `CarServiceTrack.Tests.Integration/Controllers/IborSecurityTests.cs`.

---

## 6. Authentication

### JWT login flow

1. Client sends `POST /api/v1/identity/account/login` with `{ email, password }`.
2. Server validates credentials via ASP.NET Core Identity.
3. Server returns `{ jwt, refreshToken, email, firstname, lastname, roleNames[] }`.
4. Client stores JWT in `localStorage` under key `cst_jwt` and refresh token under `cst_refresh_token`.
5. All subsequent API calls include `Authorization: Bearer <jwt>` header (injected by axios interceptor in `client-app/src/services/api.ts`).

### Refresh token flow

1. When an API call returns HTTP 401, the axios interceptor automatically calls `POST /api/v1/identity/account/refreshtokendata` with the stored refresh token.
2. Server rotates the token: returns new JWT + new refresh token, and invalidates the old one.
3. `AppRefreshToken.PreviousRefreshToken` is kept temporarily to handle race conditions.
4. Client stores the new tokens and retries the original request.

### Logout

- Client calls `POST /api/v1/identity/account/logout` with the refresh token.
- Server marks the refresh token as expired in the database.
- Client removes `cst_jwt`, `cst_refresh_token`, `cst_user_email` from `localStorage`.

### Role-based access

| Role | What they can do |
|------|-----------------|
| `admin` | All admin area pages, manage catalogue (services, spare parts, workshops, mechanics), all service orders |
| `client` | Register vehicles, create and view own service orders, view payments |
| `mechanic` | View assigned orders via Mechanic area dashboard |

### JWT configuration (server side)

File: `WebApp/Program.cs`

```
JWT:Key       — signing secret (from appsettings / environment)
JWT:Issuer    — token issuer string
JWT:Audience  — token audience string
ClockSkew: TimeSpan.Zero  — no tolerance for expired tokens
```

Refresh tokens are stored in the `AppRefreshTokens` table (PostgreSQL) via `RefreshTokenRepository`.

---

## 7. MVC and Admin UX

### Normal MVC pages

Path: `WebApp/Controllers/`

| Controller | Route | Auth | Description |
|-----------|-------|------|-------------|
| `HomeController` | `/` | No | Home page, privacy, language switcher (`SetLanguage`) |
| `VehiclesController` | `/Vehicles` | Yes | View vehicles; CRUD requires role `admin` or `client` |
| `ServiceOrdersController` | `/ServiceOrders` | Yes | View own service orders |
| `PaymentsController` | `/Payments` | Yes | View own payments |
| `ListItemsController` | `/ListItems` | Yes | Demo to-do items |

Views are in `WebApp/Views/`. ViewModels are in `WebApp/ViewModels/`:

- `VehicleClientViewModel`, `ServiceOrderClientViewModel`, `PaymentClientViewModel`
- `ListItemCreateViewModel`, `ListItemEditViewModel`, `ListItemIndexItemViewModel`, `ListItemDetailsViewModel`, `ListItemDeleteViewModel`
- `ErrorViewModel`

### Admin Area

Path: `WebApp/Areas/Admin/`

All admin routes require role `admin` (`[Authorize(Roles="admin")]`).

| Controller | Route | Manages |
|-----------|-------|---------|
| `DashboardController` | `/Admin/Dashboard` | Summary stats |
| `WorkshopsController` | `/Admin/Workshops` | Workshop CRUD |
| `MechanicsController` | `/Admin/Mechanics` | Mechanic CRUD |
| `ServicesController` | `/Admin/Services` | Service catalogue CRUD |
| `SparePartsController` | `/Admin/SpareParts` | Spare parts catalogue CRUD |
| `ServiceOrdersController` | `/Admin/ServiceOrders` | All service orders |
| `ServiceOrderPartsController` | `/Admin/ServiceOrderParts` | Parts used per order |

Admin ViewModels are in `WebApp/Areas/Admin/ViewModels/`:
`DashboardViewModel`, `WorkshopViewModel`, `MechanicViewModel`, `ServiceAdminViewModel`, `SparePartAdminViewModel`, `ServiceOrderAdminViewModel`, `ServiceOrderPartAdminViewModel`.

### Mechanic Area

Path: `WebApp/Areas/Mechanic/`

All mechanic routes require role `mechanic` (`[Authorize(Roles="mechanic")]`).

| Controller | Route | Description |
|-----------|-------|-------------|
| `DashboardController` | `/Mechanic/Dashboard` | Mechanic's assigned orders summary |
| `OrdersController` | `/Mechanic/Orders` | Mechanic's order list |

ViewModels: `MechanicDashboardViewModel`, `MechanicOrderViewModel`.

**Note:** `ViewBag` and `ViewData` are not used for passing model data to views — all data goes through typed ViewModels, as required by the assignment.

---

## 8. Translations

### UI translations (resx files)

Resource files are in `App.Resources/`:

| File | Culture | Used for |
|------|---------|---------|
| `Domain/Person.resx` | en (English, default) | Person-related labels |
| `Domain/Person.et.resx` | et (Estonian) | Person-related labels |
| `Views/Home.resx` | en | Home page strings |
| `Views/Home.et.resx` | et | Home page strings |
| `Views/CarService.resx` | en | Car service labels |
| `Views/CarService.et.resx` | et | Car service labels |

Localization is configured in `WebApp/Program.cs`:

- Supported cultures: read from config key `SupportedCultures`.
- Default culture: `en`.
- Culture selection priority:
  1. Query string parameter (e.g. `?culture=et`)
  2. Cookie (`CookieRequestCultureProvider`)
- Language can be switched from the home page via `HomeController.SetLanguage(culture, returnUrl)`.

### DB translations (LangStr)

`Base.Domain/LangStr.cs` stores multilingual text as a `Dictionary<string, string>` serialised to a single database column (JSON). The key is the culture code (e.g. `"en"`, `"et"`).

**Fields that use LangStr:**

| Entity | Field |
|--------|-------|
| `Service` | `Name`, `Description` |
| `Workshop` | `Name`, `Address` |
| `SparePart` | `Name` |
| `ListItem` | `Summary` |

**Culture fallback chain** (in `LangStr.Translate(culture)`):
1. Exact culture match (e.g. `"et-EE"`)
2. Neutral culture (e.g. `"et"`)
3. `LangStr.DefaultCulture` (configured via `LangStrDefaultCulture` in appsettings, default `"en"`)

**Culture fix for CI:** `Base.Domain/LangStr.cs` constructor falls back to `"en"` when `CultureInfo.CurrentUICulture.Name` is empty (happens on bare Linux runners). `CarServiceTrack.Tests.Unit/TestCultureSetup.cs` sets culture `"en"` via `[ModuleInitializer]` before any tests run.

---

## 9. Separate Frontend Client

### Technology

| Item | Value |
|------|-------|
| Framework | Vue 3.3.8 |
| Build tool | Vite 5.0.0 |
| Language | TypeScript 5.3.2 |
| Routing | vue-router 4.2.5 |
| State | Pinia 2.1.7 |
| HTTP client | axios 1.6.0 |
| i18n | vue-i18n 9.6.5 (en + et) |
| E2E testing | Playwright 1.44.0 |

### Folder structure

```
client-app/
  src/
    services/
      api.ts           — axios instance, token logic, refresh interceptor
      authService.ts   — login, register, logout
      vehicleService.ts
      orderService.ts
      paymentService.ts
      workshopService.ts
      serviceService.ts
      sparePartService.ts
      serviceOrderPartService.ts
      repairPhotoService.ts
    stores/
      auth.ts          — Pinia store for user/token state
    router/
      index.ts         — route definitions + auth guard
    views/
      HomeView.vue
      LoginView.vue
      RegisterView.vue
      vehicles/        — VehiclesView, VehicleCreateView, VehicleEditView, VehicleDetailView
      orders/          — OrdersView, OrderCreateView, OrderDetailView, OrderUpdateStatusView, OrderProgressView
      payments/        — PaymentsView, PaymentDetailView
      services/        — ServicesView
      AdminSparePartsView.vue
    components/
      NavBar.vue
    i18n/
      index.ts, en.ts, et.ts
    types/
      index.ts
  e2e/                 — Playwright E2E tests
  nginx.conf           — served by nginx in Docker
  .env.production      — VITE_API_BASE_URL=https://alejeg-pp.proxy.itcollege.ee
```

### API base URL

`VITE_API_BASE_URL` is a Vite build-time environment variable baked into the JS bundle.

| Environment | Value |
|-------------|-------|
| Development (`.env.development`) | `http://localhost:5065` (via Vite proxy `/api`) |
| Production (`.env.production` and `.env`) | `https://alejeg-pp.proxy.itcollege.ee` |

The variable is used in `src/services/api.ts`:
```ts
const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5065';
```

**Important:** In production the frontend must point to the real backend URL, not `localhost`. The `.env.production` file is correct. The docker-compose.yml `build.args.VITE_API_BASE_URL: http://localhost:80` overrides this during `docker compose up --build`. If the frontend shows CORS or network errors in production, rebuild with the correct arg or remove the override from `docker-compose.yml`.

### Authentication in the frontend

- JWT stored in `localStorage` key `cst_jwt`.
- Refresh token stored in `localStorage` key `cst_refresh_token`.
- Axios request interceptor attaches `Authorization: Bearer <jwt>` to every API call.
- Axios response interceptor catches 401, calls `/api/v1/identity/account/refreshtokendata`, rotates tokens, retries original request.
- Auth state (`isLoggedIn`, `userEmail`, roles) managed in `stores/auth.ts` (Pinia).
- Route guard in `router/index.ts` redirects unauthenticated users to `/login`.

### CRUD via frontend (minimum 3 entities)

| Entity | Views |
|--------|-------|
| Vehicles | VehiclesView, VehicleCreateView, VehicleEditView, VehicleDetailView |
| Service Orders | OrdersView, OrderCreateView, OrderDetailView, OrderUpdateStatusView |
| Payments | PaymentsView, PaymentDetailView |

### Deployed URLs

| Service | URL |
|---------|-----|
| Frontend | `https://alejeg-front.proxy.itcollege.ee` |
| Backend / API | `https://alejeg-pp.proxy.itcollege.ee` |
| Swagger | `https://alejeg-pp.proxy.itcollege.ee/swagger` |

---

## 10. Docker Deployment

### Services in `docker-compose.yml`

| Service | Image built from | Exposed port | Internal port |
|---------|-----------------|-------------|--------------|
| `frontend` | Root `Dockerfile` (nginx + Vue dist) | `81:80` | 80 |
| `backend` | `WebApp/Dockerfile` (ASP.NET Core) | `80:8080` | 8080 |
| `db` | `postgres:16` | `5432:5432` | 5432 |

No `container_name` fields are set — Docker Compose generates names from the project name, so parallel pipeline jobs (`-p e2e`, `-p personalproject`) cannot conflict.

### Volumes

```yaml
volumes:
  personalproject_pgdata:
```

PostgreSQL data is persisted to a named Docker volume. Data survives `docker compose down` and redeploys.

### Environment variables (backend)

Passed in `docker-compose.yml`:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=carservicetrack;Username=postgres;Password=postgres
```

Additional settings (JWT key, CORS origins, etc.) must be set in `appsettings.Production.json` or as additional environment variables. Needs verification for exact server configuration.

### How frontend talks to backend

In production the Vue app makes HTTP calls directly to `https://alejeg-pp.proxy.itcollege.ee` (the backend's public domain). The nginx inside the frontend container only serves static files and handles Vue Router's HTML5 history mode:

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

There is no nginx proxy pass to the backend — the browser talks directly to the backend's public URL.

### PostgreSQL healthcheck

The `backend` service only starts after `db` passes:

```yaml
db:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres -d carservicetrack"]
    interval: 5s
    timeout: 5s
    retries: 10
    start_period: 10s
```

### Deployment commands

**Deploy / redeploy:**

```bash
docker compose -p personalproject down || true
docker compose -p personalproject up --build --remove-orphans -d
```

**Check running containers:**

```bash
docker compose -p personalproject ps
```

**View logs:**

```bash
docker compose -p personalproject logs backend --tail=100
docker compose -p personalproject logs frontend --tail=100
docker compose -p personalproject logs db --tail=100
```

**Follow logs in real time:**

```bash
docker compose -p personalproject logs -f backend
```

---

## 11. GitLab CI/CD

Pipeline file: `.gitlab-ci.yml`

### Stages

```yaml
stages:
  - test
  - deploy
```

### Jobs

#### `dotnet-tests` (mandatory)

- Stage: `test`
- Runs on: `main` branch and merge requests
- Runner: tagged `shared`
- Docker image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Steps:
  1. `dotnet restore pesonal.sln`
  2. Run unit tests with JUnit logger → `TestResults/unit-results.xml`
  3. Run integration tests with JUnit logger → `TestResults/integration-results.xml`
- Artifacts: JUnit XML reports uploaded to GitLab (shown in MR test tab), coverage data, expire in 7 days.

**Important — shell runner note:** If the GitLab runner uses a **shell executor** (not Docker executor), the `image:` field is ignored. In that case .NET 10 SDK must be installed on the runner server itself. Check with: `dotnet --version`. Required version: `10.x`.

**JUnit logger requirement:** The `--logger "junit;LogFilePath=..."` option requires the `GitHubActionsTestLogger` or `JunitXml.TestLogger` NuGet package in the test projects. If the logger package is missing, the test step fails with "No test logger found". Needs verification that the package is referenced in the test `.csproj` files.

#### `e2e-tests` (manual / optional)

- Stage: `test`
- `when: manual` — must be triggered manually from the GitLab pipeline UI.
- `allow_failure: true` — pipeline stays green even if E2E fails.
- Docker image: `mcr.microsoft.com/playwright:v1.44.0-jammy`
- Starts a docker-compose stack with project name `e2e`, runs Playwright tests, then tears down.

#### `deploy` (automatic on `main`)

- Stage: `deploy`
- Runs on: `main` branch only
- Runs the two deployment commands:
  ```bash
  docker compose -p personalproject down || true
  docker compose -p personalproject up --build --remove-orphans -d
  ```

### Manually retrying a pipeline

1. Go to **GitLab → CI/CD → Pipelines**.
2. Click the failed pipeline.
3. Click **Retry** (top right) to re-run failed jobs only, or click the individual job and **Retry**.

### What a green pipeline proves

- All 117 unit tests pass.
- All 57 integration tests pass.
- Application builds successfully (Docker images rebuild on deploy).
- New code is deployed to the server automatically on merge to `main`.

---

## 12. Test Coverage

### Test projects

| Project | Type | Count |
|---------|------|-------|
| `CarServiceTrack.Tests.Unit` | Unit tests (BLL services, mocked repositories) | 117 tests |
| `CarServiceTrack.Tests.Integration` | Integration tests (real HTTP + test DB) | 57 tests |
| `client-app/e2e/` | Playwright E2E (browser automation) | manual trigger in CI |

**Unit test files** (`CarServiceTrack.Tests.Unit/Services/`):
VehicleServiceTests, ServiceOrderServiceTests, PaymentServiceTests, SparePartServiceTests, WorkshopServiceTests, MechanicServiceTests, OwnerServiceTests, ServiceServiceTests, StatusHistoryServiceTests, RefreshTokenServiceTests, ServiceOrderPartServiceTests, RepairPhotoServiceTests.

**Integration test files** (`CarServiceTrack.Tests.Integration/Controllers/`):
AccountControllerTests, VehiclesControllerTests, IborSecurityTests, PaymentsControllerTests, ServiceOrdersControllerTests.

### Running tests locally

Restore dependencies first:

```bash
dotnet restore pesonal.sln
```

Run unit tests only:

```bash
dotnet test CarServiceTrack.Tests.Unit/CarServiceTrack.Tests.Unit.csproj --configuration Release
```

Run integration tests only:

```bash
dotnet test CarServiceTrack.Tests.Integration/CarServiceTrack.Tests.Integration.csproj --configuration Release
```

Run all tests:

```bash
dotnet test pesonal.sln --configuration Release
```

Expected result: **174 tests passed, 0 failed**.

### Running tests with coverage collection

```bash
dotnet test pesonal.sln --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults/
```

Coverage reports are placed in `TestResults/`. Use `reportgenerator` tool to convert to HTML if needed.

### On server (shell runner)

Same commands apply. .NET 10 SDK must be installed on the server. Verify:

```bash
dotnet --version
```

---

## 13. Manual Deployment Verification

Checklist for defence or teacher review:

- [ ] Open frontend URL: `https://alejeg-front.proxy.itcollege.ee`
  - Page loads without errors; navbar visible.
- [ ] Open Swagger: `https://alejeg-pp.proxy.itcollege.ee/swagger`
  - All API endpoint groups visible; v1 group listed.
- [ ] Register a new user via frontend or Swagger `POST /api/v1/identity/account/register`.
- [ ] Login via frontend — JWT token received; redirected to home/dashboard.
- [ ] Create a Vehicle from the frontend (`/vehicles/create`).
- [ ] Create a Service Order for the vehicle.
- [ ] View payment for the order.
- [ ] Logout; confirm you are redirected to login and API calls return 401.
- [ ] Login as admin (Needs verification — check seed data for admin credentials).
- [ ] Open `/Admin/Dashboard` — admin dashboard loads.
- [ ] Open `/Admin/Services` — manage services catalogue.
- [ ] Open `/Admin/Workshops` — manage workshops.
- [ ] Open `/Admin/SpareParts` — manage spare parts.
- [ ] Check IDOR: log in as a second user; try to access the first user's vehicle by ID in the URL — should be rejected (403 or 404).
- [ ] Check database container:
  ```bash
  docker compose -p personalproject ps
  ```
  All containers should show `Up` and `db` should show `healthy`.
- [ ] Check backend logs for errors:
  ```bash
  docker compose -p personalproject logs backend --tail=100
  ```
- [ ] Open GitLab pipeline for latest `main` commit — pipeline should be green.

---

## 14. Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| `dotnet: command not found` | .NET SDK not installed on server | Install .NET 10 SDK: `sudo apt-get install -y dotnet-sdk-10.0` or download from microsoft.com/dotnet |
| `NETSDK1045: The current .NET SDK does not support targeting .NET 10` | .NET SDK version is too old (e.g. 6 or 8) | Install .NET 10 SDK; verify with `dotnet --version` |
| Frontend sends requests to `localhost` | `VITE_API_BASE_URL` was `http://localhost:80` at build time (from docker-compose.yml build arg) | Either change the build arg in `docker-compose.yml` to the real backend URL, or remove the arg so the Dockerfile default (`https://alejeg-pp.proxy.itcollege.ee`) is used |
| Docker container name conflict | Old fixed `container_name` values left in compose file | `container_name` fields have been removed; Docker Compose generates unique names per project (`-p`) |
| `port is already allocated` | Another process or compose stack holds the port | Stop the conflicting stack: `docker compose -p <name> down`; or find the process: `sudo ss -tlnp | grep :80` |
| JUnit logger not found | `GitHubActionsTestLogger` or `JunitXml.TestLogger` package not in test project `.csproj` | Add the NuGet package to the test project; verify with `dotnet list package` |
| `TestResults` artifacts missing in GitLab | Logger did not write the file (wrong path or missing package) | Check the `artifacts.reports.junit` path in `.gitlab-ci.yml` matches the actual output path |
| PostgreSQL connection string wrong | `Host` name doesn't match the service name or wrong credentials | In Docker Compose the hostname is the service name `db`; check `ConnectionStrings__DefaultConnection` env var |
| CORS error in browser | Backend `Cors:AllowedOrigins` config does not include the frontend URL | Add `https://alejeg-front.proxy.itcollege.ee` to `Cors:AllowedOrigins` in backend config |
| Swagger not opening | Swagger middleware not configured for Production environment | Check `WebApp/Program.cs` — Swagger may be guarded by `if (app.Environment.IsDevelopment())`; move outside that block for testing |
| Login returns "Invalid email/password" | Wrong credentials, or seed data not run | Check `DataInitialization:SeedIdentity` config key is `true`; check seeded user emails in `AppDataInit.cs` |
| `Culture is required!` / LangStr exception | `CultureInfo.CurrentUICulture.Name` is empty on Linux runner | Fixed in `Base.Domain/LangStr.cs` (falls back to `"en"`) and `CarServiceTrack.Tests.Unit/TestCultureSetup.cs` (`[ModuleInitializer]` sets culture before tests) |

---

## 15. Final Defence Notes

During the defence, show the following in order:

1. **Architecture folders** — open solution in IDE; point to `App.Domain`, `App.DAL.Contracts`, `App.DAL.EF`, `App.BLL`, `App.DTO`, `WebApp` layers. Explain the dependency direction.

2. **Domain entities** — open `App.Domain/` folder; show 15 entities, explain `LangStr` fields and relationships (e.g. `ServiceOrder` → `Vehicle` → `Owner`).

3. **Swagger / API versioning** — open `https://alejeg-pp.proxy.itcollege.ee/swagger`; show v1 group, URL pattern `/api/v1/...`; demonstrate a GET endpoint live.

4. **JWT login** — use Swagger to call `POST /api/v1/identity/account/login`; show the JWT + refresh token in the response; explain the refresh flow.

5. **MVC / Admin UX** — open `https://alejeg-pp.proxy.itcollege.ee`; login as admin; navigate to `/Admin/Dashboard`, `/Admin/Services`, `/Admin/Workshops`; create or edit a record. Show that ViewModels are used (open the controller + ViewModel class in IDE).

6. **Separate frontend** — open `https://alejeg-front.proxy.itcollege.ee`; login; show CRUD for Vehicles, Service Orders; open browser DevTools Network tab to show requests going to `https://alejeg-pp.proxy.itcollege.ee`.

7. **CRUD from frontend against backend** — create a vehicle, place a service order, view payment — showing the full user flow end-to-end.

8. **CI/CD pipeline** — open GitLab → CI/CD → Pipelines; show the green pipeline on `main`; point to `dotnet-tests` job output showing `174 passed`; explain the deploy stage.

9. **Deployed URLs** — confirm both URLs are live: `https://alejeg-front.proxy.itcollege.ee` (frontend) and `https://alejeg-pp.proxy.itcollege.ee` (backend/API).

10. **Tests / coverage** — run `dotnet test pesonal.sln --configuration Release` locally or show the last CI run; confirm 174/174 pass. Point to unit test files in `CarServiceTrack.Tests.Unit/Services/` and IDOR test in `CarServiceTrack.Tests.Integration/Controllers/IborSecurityTests.cs`.
