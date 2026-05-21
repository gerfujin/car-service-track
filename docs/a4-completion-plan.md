# CarServiceTrack: Assignment 4 / Phase 2 — Technical Audit & Completion Plan

> **Date:** 2026-05-21  
> **.NET SDK:** 10.0  
> **Project root:** `D:\IdeaProjects\PersonalP`

---

## 1. Current Project Structure

```
pesonal.sln
├── App.Domain/                    # Domain entities, enums, Identity (AppUser, AppRole, AppRefreshToken)
├── App.DAL.Contracts/             # Repository + UnitOfWork interfaces
├── App.DAL.EF/                    # EF Core DbContext, repositories, migrations, seeding
├── App.BLL/                       # Business logic: services, BLL DTOs, IAppBll facade, mappers
│   ├── Services/                  # 12 service interfaces + implementations
│   ├── DTO/                       # Internal BLL transfer objects (Bll*)
│   └── Mappers/                   # Domain ↔ BLL DTO mappers
├── App.DTO/                       # Public API contracts
│   └── v1/                        # Versioned DTOs (Identity, Vehicle, ServiceOrder, Payment, etc.)
├── App.Resources/                 # Localization resources (Domain/, Views/)
├── Base.Contracts/                # IBaseEntity, IBaseRepository, IBaseService, IBaseUnitOfWork
├── Base.Domain/                   # BaseEntity, LangStr
├── Base.Helpers/                  # IdentityHelpers (JWT generation/validation)
├── WebApp/                        # ASP.NET Core host
│   ├── ApiControllers/            # REST API controllers (under /api/v1/)
│   │   └── Identity/              # AccountController (register, login, refresh, logout)
│   ├── Areas/
│   │   ├── Admin/                 # Admin MVC area (controllers, views, viewmodels)
│   │   ├── Mechanic/              # Mechanic MVC area (controllers, views, viewmodels)
│   │   └── Identity/              # ASP.NET Identity scaffolded pages
│   ├── Controllers/               # Main-area MVC controllers (Home, Vehicles, ServiceOrders, Payments, ListItems)
│   ├── Mappers/                   # BLL DTO → API DTO mappers
│   ├── ViewModels/                # MVC ViewModels
│   └── Views/                     # Razor views
├── CarServiceTrack.Tests.Unit/    # xUnit + Moq unit tests (12 test classes)
├── CarServiceTrack.Tests.Integration/  # xUnit + WebApplicationFactory integration tests (5 test classes)
├── client-app/                    # Vue 3 + Vite SPA
│   ├── src/
│   │   ├── components/            # NavBar
│   │   ├── i18n/                  # en.ts, et.ts localization
│   │   ├── router/                # Vue Router routes
│   │   ├── services/              # Axios API service modules
│   │   ├── stores/                # Pinia auth store
│   │   ├── types/                 # TypeScript type definitions
│   │   └── views/                 # Vue SFC views
│   └── e2e/                       # Playwright E2E tests
├── docker-compose.yml             # 3 services: frontend (nginx), backend (.NET), db (PostgreSQL 16)
├── Dockerfile                     # Frontend build (Vue → nginx)
└── WebApp/Dockerfile              # Backend build (.NET)
```

---

## 2. Where Clean/Onion Architecture Is Already Implemented

| Layer | Status | Notes |
|-------|--------|-------|
| **Domain layer** (`App.Domain`) | ✅ Complete | Pure entities, enums, identity models — no EF or infrastructure dependencies |
| **Repository contracts** (`App.DAL.Contracts`) | ✅ Complete | `IAppUnitOfWork` + 12 repository interfaces |
| **Repository implementations** (`App.DAL.EF`) | ✅ Complete | EF Core repositories, `AppUnitOfWork`, migrations |
| **BLL facade** (`App.BLL`) | ✅ Complete | `IAppBll` / `AppBll` with 12 lazy-initialized service properties, single `SaveChangesAsync()` |
| **BLL services** (`App.BLL/Services`) | ✅ Complete | Each service wraps a repository + adds business rules |
| **Internal DTOs** (`App.BLL/DTO`) | ✅ Complete | `BllVehicle`, `BllServiceOrder`, etc. — decoupled from domain |
| **Public DTOs** (`App.DTO/v1`) | ✅ Complete | Versioned API contracts under `/api/v1` |
| **API controllers → BLL** | ✅ Complete | All 10 API controllers inject `IAppBll` |
| **DI registration** (`WebApp/Program.cs`) | ✅ Complete | `IAppUnitOfWork` + `IAppBll` registered as scoped |

**Architecture diagram (existing):**
```
WebApp.ApiControllers ──→ IAppBll (facade)
                              │
                     App.BLL.Services (business rules)
                              │
                     IAppUnitOfWork (DAL abstraction)
                              │
                     App.DAL.EF.Repositories → AppDbContext → PostgreSQL
```

---

## 3. MVC/Admin/Mechanic Controllers Using AppDbContext Directly

**Every MVC controller bypasses IAppBll and uses `AppDbContext` directly.** This is the primary gap.

### Admin Area (7 controllers)

| Controller | File | Uses |
|------------|------|------|
| `DashboardController` | [`WebApp/Areas/Admin/Controllers/DashboardController.cs`](WebApp/Areas/Admin/Controllers/DashboardController.cs:14) | `AppDbContext` |
| `MechanicsController` | [`WebApp/Areas/Admin/Controllers/MechanicsController.cs`](WebApp/Areas/Admin/Controllers/MechanicsController.cs:14) | `AppDbContext` |
| `ServiceOrdersController` | [`WebApp/Areas/Admin/Controllers/ServiceOrdersController.cs`](WebApp/Areas/Admin/Controllers/ServiceOrdersController.cs:16) | `AppDbContext` |
| `ServiceOrderPartsController` | [`WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs`](WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs:15) | `AppDbContext` |
| `ServicesController` | [`WebApp/Areas/Admin/Controllers/ServicesController.cs`](WebApp/Areas/Admin/Controllers/ServicesController.cs:15) | `AppDbContext` |
| `SparePartsController` | [`WebApp/Areas/Admin/Controllers/SparePartsController.cs`](WebApp/Areas/Admin/Controllers/SparePartsController.cs:15) | `AppDbContext` |
| `WorkshopsController` | [`WebApp/Areas/Admin/Controllers/WorkshopsController.cs`](WebApp/Areas/Admin/Controllers/WorkshopsController.cs:15) | `AppDbContext` |

### Mechanic Area (2 controllers)

| Controller | File | Uses |
|------------|------|------|
| `DashboardController` | [`WebApp/Areas/Mechanic/Controllers/DashboardController.cs`](WebApp/Areas/Mechanic/Controllers/DashboardController.cs:14) | `AppDbContext` |
| `OrdersController` | [`WebApp/Areas/Mechanic/Controllers/OrdersController.cs`](WebApp/Areas/Mechanic/Controllers/OrdersController.cs:15) | `AppDbContext` |

### Main Area (5 controllers)

| Controller | File | Uses |
|------------|------|------|
| `HomeController` | [`WebApp/Controllers/HomeController.cs`](WebApp/Controllers/HomeController.cs:8) | No data access (pure views) |
| `VehiclesController` | [`WebApp/Controllers/VehiclesController.cs`](WebApp/Controllers/VehiclesController.cs:15) | `AppDbContext` |
| `ServiceOrdersController` | [`WebApp/Controllers/ServiceOrdersController.cs`](WebApp/Controllers/ServiceOrdersController.cs:14) | `AppDbContext` |
| `PaymentsController` | [`WebApp/Controllers/PaymentsController.cs`](WebApp/Controllers/PaymentsController.cs:14) | `AppDbContext` |
| `ListItemsController` | [`WebApp/Controllers/ListItemsController.cs`](WebApp/Controllers/ListItemsController.cs:19) | `AppDbContext` |

---

## 4. Controllers Already Using IAppBll

**All 10 API controllers** (under `WebApp/ApiControllers/`) inject `IAppBll`:

| Controller | File | Notes |
|------------|------|-------|
| `AccountController` | [`WebApp/ApiControllers/Identity/AccountController.cs`](WebApp/ApiControllers/Identity/AccountController.cs:29) | JWT login/register/refresh/logout + RefreshToken service |
| `VehiclesController` | [`WebApp/ApiControllers/VehiclesController.cs`](WebApp/ApiControllers/VehiclesController.cs:21) | Full CRUD with IDOR |
| `ServicesController` | [`WebApp/ApiControllers/ServicesController.cs`](WebApp/ApiControllers/ServicesController.cs:18) | CRUD (admin-only writes) |
| `SparePartsController` | [`WebApp/ApiControllers/SparePartsController.cs`](WebApp/ApiControllers/SparePartsController.cs:18) | CRUD (admin-only writes) |
| `WorkshopsController` | [`WebApp/ApiControllers/WorkshopsController.cs`](WebApp/ApiControllers/WorkshopsController.cs:13) | Read-only (public) |
| `MechanicsController` | [`WebApp/ApiControllers/MechanicsController.cs`](WebApp/ApiControllers/MechanicsController.cs:14) | Read-only (public) |
| `ServiceOrdersController` | [`WebApp/ApiControllers/ServiceOrdersController.cs`](WebApp/ApiControllers/ServiceOrdersController.cs:21) | Full CRUD with IDOR |
| `PaymentsController` | [`WebApp/ApiControllers/PaymentsController.cs`](WebApp/ApiControllers/PaymentsController.cs:21) | Full CRUD with IDOR |
| `RepairPhotosController` | [`WebApp/ApiControllers/RepairPhotosController.cs`](WebApp/ApiControllers/RepairPhotosController.cs) | Photo upload/delete |
| `ServiceOrderPartsController` | [`WebApp/ApiControllers/ServiceOrderPartsController.cs`](WebApp/ApiControllers/ServiceOrderPartsController.cs) | CRUD |

---

## 5. Missing MVC/Admin Views

### Main-area Views (files verified on disk)

| Controller Action | View Expected | Status |
|-------------------|---------------|--------|
| `Vehicles.Details` | `Views/Vehicles/Details.cshtml` | ❌ **MISSING** |
| `Vehicles.Edit` | `Views/Vehicles/Edit.cshtml` | ❌ **MISSING** |
| `ServiceOrders.*` | `Views/ServiceOrders/Details.cshtml` | ❌ **MISSING** (no controller action either) |
| `Payments.*` | `Views/Payments/Details.cshtml` | ❌ **MISSING** (no controller action either) |

### Admin-area Views

| Controller Action | View Expected | Status |
|-------------------|---------------|--------|
| `ServiceOrderParts.Edit` | `Areas/Admin/Views/ServiceOrderParts/Edit.cshtml` | ❌ **MISSING** |
| `ServiceOrderParts.Delete` | `Areas/Admin/Views/ServiceOrderParts/Delete.cshtml` | ❌ **MISSING** |
| `ServiceOrderParts.Details` | `Areas/Admin/Views/ServiceOrderParts/Details.cshtml` | ❌ **MISSING** |
| `Workshops.Edit` | `Areas/Admin/Views/Workshops/Edit.cshtml` | ❌ **MISSING** |
| `Workshops.Delete` | `Areas/Admin/Views/Workshops/Delete.cshtml` | ❌ **MISSING** |

### Mechanic-area Views

All views exist for the current controller actions. No missing views detected.

---

## 6. ViewBag/ViewData Usage

| Location | Usage | Type |
|----------|-------|------|
| [`WebApp/Controllers/VehiclesController.cs:66`](WebApp/Controllers/VehiclesController.cs:66) | `ViewBag.IsReadOnly` | Dynamic flag |
| [`WebApp/Controllers/VehiclesController.cs:216`](WebApp/Controllers/VehiclesController.cs:216) | `ViewBag.IsReadOnly` | Dynamic flag |
| [`WebApp/Controllers/ServiceOrdersController.cs:66`](WebApp/Controllers/ServiceOrdersController.cs:66) | `ViewBag.CanUpdateStatus` | Dynamic flag |
| `Views/*/_Layout.cshtml` (3 files) | `ViewData["Title"]` | Standard ASP.NET pattern |
| 35+ `.cshtml` files | `ViewData["Title"]` | Standard ASP.NET pattern |
| [`WebApp/Areas/Admin/Controllers/MechanicsController.cs:61`](WebApp/Areas/Admin/Controllers/MechanicsController.cs:61) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/MechanicsController.cs:103`](WebApp/Areas/Admin/Controllers/MechanicsController.cs:103) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/MechanicsController.cs:135`](WebApp/Areas/Admin/Controllers/MechanicsController.cs:135) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/ServicesController.cs:60`](WebApp/Areas/Admin/Controllers/ServicesController.cs:60) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/ServicesController.cs:100`](WebApp/Areas/Admin/Controllers/ServicesController.cs:100) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/ServicesController.cs:131`](WebApp/Areas/Admin/Controllers/ServicesController.cs:131) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/SparePartsController.cs:59`](WebApp/Areas/Admin/Controllers/SparePartsController.cs:59) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/SparePartsController.cs:99`](WebApp/Areas/Admin/Controllers/SparePartsController.cs:99) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/SparePartsController.cs:130`](WebApp/Areas/Admin/Controllers/SparePartsController.cs:130) | `TempData["Success"]` | Flash message |
| [`WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs:93`](WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs:93) | `TempData["Error"]` | Flash message |
| [`WebApp/Areas/Mechanic/Controllers/OrdersController.cs:108`](WebApp/Areas/Mechanic/Controllers/OrdersController.cs:108) | `TempData["Success"]` | Flash message |

**Assessment:** `ViewData["Title"]` usage is acceptable ASP.NET convention (it's the standard way to set page titles). The `ViewBag.IsReadOnly`, `ViewBag.CanUpdateStatus`, and `TempData` flash messages are candidates for proper model properties instead of dynamic bags.

---

## 7. Hardcoded UI Strings (Not Using App.Resources)

### ViewData["Title"] — hardcoded English in every view EXCEPT:

Only 3 views use the `Localizer`:
- [`WebApp/Views/Vehicles/Index.cshtml:5`](WebApp/Views/Vehicles/Index.cshtml:5) — `Localizer["Vehicles"]`
- [`WebApp/Views/ServiceOrders/Index.cshtml:5`](WebApp/Views/ServiceOrders/Index.cshtml:5) — `Localizer["Orders"]`
- [`WebApp/Areas/Admin/Views/ServiceOrders/Index.cshtml:5`](WebApp/Areas/Admin/Views/ServiceOrders/Index.cshtml:5) — `Localizer["AdminServiceOrders"]`

**Remaining ~35 view title strings are hardcoded** (e.g., `"Dashboard"`, `"Add Mechanic"`, `"Spare Parts"`, `"Edit Service"`, etc.).

### Controller flash messages — ALL hardcoded:

Examples from controllers:
- `"Mechanic created successfully."` — [`WebApp/Areas/Admin/Controllers/MechanicsController.cs:61`](WebApp/Areas/Admin/Controllers/MechanicsController.cs:61)
- `"Service deleted successfully."` — [`WebApp/Areas/Admin/Controllers/ServicesController.cs:131`](WebApp/Areas/Admin/Controllers/ServicesController.cs:131)
- `"Spare part deleted successfully."` — [`WebApp/Areas/Admin/Controllers/SparePartsController.cs:130`](WebApp/Areas/Admin/Controllers/SparePartsController.cs:130)
- `"Service order or spare part not found."` — [`WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs:93`](WebApp/Areas/Admin/Controllers/ServiceOrderPartsController.cs:93)

### Hardcoded strings in views (inline text):

All label text, button text, table headers, and status displays are hardcoded English in:
- All Admin/Views/*.cshtml files
- All Mechanic/Views/*.cshtml files
- All main Views/*.cshtml files (except the 3 mentioned above)

**The App.Resources project exists** with localization `.resx` files for `Domain`, `Views/CarService`, and `Views/Home`, but they are only used in ~3 views. The project has an Estonian (`et`) translation already set up in `App.Resources/Views/CarService.et.resx` and `App.Resources/Views/Home.et.resx`.

---

## 8. Current Test Projects & Test Commands

### Unit Tests: `CarServiceTrack.Tests.Unit`

| Framework | xUnit 2.9.3 + Moq 4.20.72 + FluentAssertions 7.2.0 |
|-----------|-----------------------------------------------------|
| Target | `net10.0` |
| References | `App.BLL`, `App.Domain` |
| Test count | 12 test files (one per BLL service) |
| Command | `dotnet test CarServiceTrack.Tests.Unit/CarServiceTrack.Tests.Unit.csproj` |

### Integration Tests: `CarServiceTrack.Tests.Integration`

| Framework | xUnit 2.9.3 + `WebApplicationFactory<Program>` + SQLite in-memory |
|-----------|-------------------------------------------------------------------|
| Target | `net10.0` |
| References | `WebApp`, `App.Domain`, `App.DAL.EF`, `App.DTO`, `Base.Helpers` |
| Test count | 5 test files (Account, IDOR security, Payments, ServiceOrders, Vehicles) |
| Command | `dotnet test CarServiceTrack.Tests.Integration/CarServiceTrack.Tests.Integration.csproj` |

### E2E Tests: `client-app/e2e/`

| Framework | Playwright |
|-----------|------------|
| Test count | 3 test files (auth, payments, service-orders, token-refresh, vehicles) |
| Command | `cd client-app && npm run test:e2e` |

### Run all .NET tests:
```bash
dotnet test
```

---

## 9. Vue Client Build Setup

| Item | Value |
|------|-------|
| Framework | Vue 3.3 + TypeScript 5.3 |
| Build tool | Vite 5.0 |
| State management | Pinia (auth store) |
| HTTP client | Axios (with JWT interceptor in `api.ts`) |
| i18n | `vue-i18n` 9.6 (en + et locales) |
| Router | `vue-router` 4.2 |
| Dev server | Port 5173, proxy `/api` → `http://localhost:5065` |
| Production build | `npm run build` → `dist/` served by nginx |
| Docker | [`client-app/Dockerfile`](client-app/Dockerfile) — Vue app served via nginx:alpine on port 80 |

**Vue routes** (from [`client-app/src/router/index.ts`](client-app/src/router/index.ts)):
- `/` — Home
- `/login`, `/register` — Auth
- `/vehicles`, `/vehicles/create`, `/vehicles/:id`, `/vehicles/:id/edit` — Vehicles
- `/orders`, `/orders/create`, `/orders/:id`, `/orders/:id/progress`, `/orders/:id/update-status` — Service Orders
- `/payments`, `/payments/:id` — Payments
- `/services` — Services catalog
- `/admin/spare-parts` — Admin spare parts management

---

## 10. Risks That Must NOT Be Broken

| Risk Area | Constraint | Why |
|-----------|------------|-----|
| **REST API routes** | All routes under `/api/v1/` must remain unchanged | Frontend and integration tests depend on exact routes |
| **JWT auth flow** | `POST /api/v1/identity/Account/Login`, `/Register`, `/RefreshTokenData`, `/Logout` | E2E tests and frontend auth store use exact shapes |
| **Public DTOs** | All classes in `App.DTO/v1/` and their property names/serialization | Breaking changes would break Vue client types and API contracts |
| **Vue client services** | All `*.ts` files in `client-app/src/services/` | API service layer must maintain contract compatibility |
| **Vue client routes** | All routes in `client-app/src/router/index.ts` | Must not break navigation |
| **Docker compose** | [`docker-compose.yml`](docker-compose.yml) — frontend (81), backend (80), db (5432) | Must keep same service names, ports, depends_on |
| **Dockerfiles** | Dockerfile (frontend), WebApp/Dockerfile (backend) | Must keep same build steps and exposed ports |
| **Migrations** | All files in `App.DAL.EF/Migrations/` | Do NOT delete or rename existing migrations; only ADD new ones if schema changes are absolutely necessary |
| **Database schema** | All entity tables/columns | Do NOT remove columns or tables that are in use; only add new ones |
| **Solution/project names** | `pesonal.sln`, all `.csproj` names and namespaces | Do NOT rename |
| **Existing features** | ALL existing functionality (ListItems, RepairPhotos, ServiceOrderParts, etc.) | Do NOT delete |

---

## Key Findings Summary

| # | Finding | Severity |
|---|---------|----------|
| 1 | IAppBll is registered in DI but ONLY API controllers use it | 🔴 Major |
| 2 | 14 MVC controllers (Admin/Mechanic/Main) use AppDbContext directly | 🔴 Major |
| 3 | 7 MVC views are missing (Vehicles Details/Edit, ServiceOrderParts Edit/Delete/Details, Workshops Edit/Delete) | 🟡 Medium |
| 4 | ~35 ViewData["Title"] strings are hardcoded, only 3 use Localizer | 🟡 Medium |
| 5 | All TempData["Success"]/["Error"] flash messages are hardcoded English | 🟡 Medium |
| 6 | All inline view text (labels, buttons, table headers) is hardcoded English | 🟡 Medium |
| 7 | ViewBag.IsReadOnly and ViewBag.CanUpdateStatus used instead of model properties | 🟢 Low |
| 8 | API layer is fully migrated to Clean Architecture ✅ | — |
| 9 | BLL layer is complete with 12 services ✅ | — |
| 10 | Test suite is comprehensive (12 unit + 5 integration + 4 e2e) ✅ | — |

---

## Recommended Incremental Improvements (Phase 2)

1. **Refactor MVC controllers to use IAppBll** (highest priority):
   - Replace `AppDbContext _context` with `IAppBll _bll` in all 14 MVC controllers
   - Add missing repository/service methods where needed (e.g., dashboard aggregates)
   - Keep existing URL routes, authorization attributes, and ViewModels unchanged

2. **Create missing views**:
   - `Views/Vehicles/Details.cshtml`, `Views/Vehicles/Edit.cshtml`
   - `Areas/Admin/Views/ServiceOrderParts/Edit.cshtml`, `Delete.cshtml`, `Details.cshtml`
   - `Areas/Admin/Views/Workshops/Edit.cshtml`, `Delete.cshtml`

3. **Localize hardcoded strings**:
   - Add resource keys to `App.Resources/Views/CarService.resx` for all view titles
   - Replace `"string literal"` with `Localizer["KeyName"]` in all `.cshtml` files
   - Move controller flash messages to resource-based localization

4. **Replace ViewBag with model properties**:
   - Move `IsReadOnly` to `VehicleClientListViewModel`
   - Move `CanUpdateStatus` to `ServiceOrderClientListViewModel`
