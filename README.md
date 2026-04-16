# CarServiceTrack — Full-Stack University Submission

A full-stack car service management application built with:
- **Backend**: ASP.NET Core 8 Web API + MVC Admin Area
- **Frontend**: Vue 3 + TypeScript + Vite SPA (`client-app/`)
- **Database**: PostgreSQL (via Docker) or SQLite (local dev)
- **Auth**: JWT + Refresh Tokens

---

## Solution Structure

This project is organized as **one unified full-stack solution** rooted at the repository root.
Open `HWDemo.sln` in JetBrains Rider (or Visual Studio) to see both the backend and frontend together.

```
project-root/
├── HWDemo.sln               ← Open this in Rider — shows backend + frontend together
│
├── ── Backend (.NET projects, visible in solution tree) ──
├── WebApp/                  # ASP.NET Core backend (API + MVC Admin)
├── App.Domain/              # Domain entities
├── App.DAL.EF/              # EF Core DbContext + Migrations
├── App.DTO/                 # Data Transfer Objects (API contracts)
├── App.Resources/           # Localization resources (en/et)
├── Base.Contracts/          # Shared interfaces
├── Base.Domain/             # Base entity classes
├── Base.Helpers/            # JWT / Identity helpers
│
├── ── Frontend (Vue 3 SPA, visible as "Frontend" solution folder) ──
├── client-app/              # Vue 3 SPA frontend
│   ├── src/
│   │   ├── components/      # Shared UI components (NavBar, etc.)
│   │   ├── i18n/            # Translations (en / et)
│   │   ├── router/          # Vue Router routes
│   │   ├── services/        # Axios API service layer
│   │   ├── stores/          # Pinia state stores
│   │   ├── types/           # TypeScript type definitions
│   │   └── views/           # Page components
│   ├── .env                 # Dev environment (API base URL)
│   ├── .env.production      # Production environment
│   ├── Dockerfile           # Vue app Docker build
│   ├── nginx.conf           # Nginx config (SPA + API proxy)
│   ├── package.json
│   └── vite.config.ts
│
├── ── Infrastructure ──
├── docker-compose.yml       # Full-stack Docker Compose (backend + frontend + DB)
├── Dockerfile               # ASP.NET Core Docker build
├── Directory.Build.Props    # Shared MSBuild properties
└── README.md
```

### How the solution is organized in Rider

When you open `HWDemo.sln` in Rider, the **Solution Explorer** shows:

| Solution Folder | Contents |
|-----------------|----------|
| `App`           | WebApp, App.Domain, App.DAL.EF, App.DTO, App.Resources |
| `Base`          | Base.Domain, Base.Contracts, Base.Helpers |
| `Frontend`      | All `client-app/` source files (Vue, TypeScript, config) — visible as solution items |
| `!Solution Items` | README.md, .gitignore, Directory.Build.Props |
| `!CICD`         | docker-compose.yml, Dockerfile, .gitlab-ci.yml, .dockerignore |

> **Note:** The `Frontend` folder is a **Solution Folder** (not a .NET project). It contains the Vue/Vite
> source files as solution items so they are browsable and editable directly from the Rider solution tree.
> The Vue app remains a fully independent Node.js/Vite project — it is not converted to a .NET project.

---

## Opening the Project in Rider

1. **Open `HWDemo.sln`** from the project root in JetBrains Rider.
2. In the **Solution Explorer**, expand the `Frontend` folder to browse all Vue/TypeScript source files.
3. You can edit `.vue`, `.ts`, and config files directly from Rider's solution tree.
4. To run the Vue dev server, open a terminal in Rider and run `cd client-app && npm run dev`.
5. To run the backend, use the standard Rider run configuration for `WebApp`.

---

## Running Locally (Development)

### 1. Backend — ASP.NET Core

Requirements: .NET 8 SDK

```bash
# From project root — apply migrations and seed data
dotnet ef database --project App.DAL.EF --startup-project WebApp update

# Run the backend (listens on http://localhost:5065)
dotnet run --project WebApp
```

The backend exposes:
- REST API: `http://localhost:5065/api/v1/`
- Swagger UI: `http://localhost:5065/swagger`
- Admin MVC area: `http://localhost:5065/Admin`

### 2. Frontend — Vue 3 SPA

Requirements: Node.js 18+

```bash
cd client-app

# Install dependencies
npm install

# Start dev server with hot-reload (proxies /api to localhost:5065)
npm run dev
```

The Vue app runs at `http://localhost:5173` and proxies all `/api/*` requests to the ASP.NET backend at `http://localhost:5065` (configured in `vite.config.ts`).

---

## Running with Docker Compose (Production-like)

Requirements: Docker + Docker Compose

```bash
# Build and start all services (backend, frontend, PostgreSQL, pgAdmin)
docker compose up --build
```

| Service    | URL                          | Description                  |
|------------|------------------------------|------------------------------|
| Vue SPA    | http://localhost:3000        | Vue frontend (nginx)         |
| ASP.NET    | http://localhost:8080        | Backend API + Admin          |
| pgAdmin    | http://localhost:88          | PostgreSQL admin UI          |

**How the Docker networking works:**
- The Vue app is served by nginx on port 3000.
- nginx proxies all `/api/*` requests to the `aspnet` container on port 8080 (internal Docker network).
- The browser only ever talks to `localhost:3000` — no CORS issues.

---

## Environment Variables

### `client-app/.env` (development)
```
VITE_API_BASE_URL=http://localhost:5065
```

### `client-app/.env.production` (Docker / production)
```
VITE_API_BASE_URL=
```
Empty string means same-origin — nginx handles the proxy to the backend.

---

## EF Core Migrations

Run from the solution root:

```bash
dotnet tool update -g dotnet-ef

# Add a new migration
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add <MigrationName>

# Remove last migration
dotnet ef migrations --project App.DAL.EF --startup-project WebApp remove

# Apply migrations
dotnet ef database --project App.DAL.EF --startup-project WebApp update

# Drop database
dotnet ef database --project App.DAL.EF --startup-project WebApp drop
```

---

## Docker Image Build (manual)

```bash
# Build and push ASP.NET backend image
docker buildx build --progress=plain --force-rm --push -t <your-registry>/webapp:latest .

# Multi-platform build (e.g. Apple Silicon)
docker buildx create --name mybuilder --bootstrap --use
docker buildx build --platform linux/amd64 -t <your-registry>/webapp:latest --push .
```

---

## Default Admin Credentials (seeded)

| Role  | Email                  | Password   |
|-------|------------------------|------------|
| Admin | admin@carservice.com   | Admin123!  |
| User  | user@carservice.com    | User123!   |

*(Seeded by `App.DAL.EF/Seeding/AppDataInit.cs` on first run)*

---

## Tech Stack

| Layer      | Technology                              |
|------------|-----------------------------------------|
| Backend    | ASP.NET Core 8, EF Core, PostgreSQL     |
| Auth       | ASP.NET Identity, JWT, Refresh Tokens   |
| API Docs   | Swagger / OpenAPI (Swashbuckle)         |
| Frontend   | Vue 3, TypeScript, Vite, Pinia, Axios   |
| i18n       | vue-i18n (English + Estonian)           |
| Routing    | Vue Router 4                            |
| Container  | Docker, Docker Compose, nginx           |
