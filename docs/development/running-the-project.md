# Running the Project

How to run the driving-lessons app locally. Two setups: **Docker Compose** (integrated, prod-like) and **from-source live dev** (hot reload). See [tech-stack.md](../tech-stack.md) for the architecture.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with the engine running)
- [.NET SDK 10](https://dotnet.microsoft.com/download) — for from-source dev
- [Node.js 24+](https://nodejs.org/) — for the Angular client

## Ports & defaults

| Thing | Where |
|-------|-------|
| App (Docker, API + SPA) | http://localhost:8080 |
| API from source (`dotnet run`) | http://localhost:5080 |
| Angular dev server | http://localhost:4200 |
| PostgreSQL | localhost:5432 |
| Dev admin login | `admin@local.dev` / `DevAdmin#2026` |

The client always calls relative `/api/...` URLs. In dev, `client/proxy.conf.json` proxies `/api` → `http://localhost:5080`.

## Option A — Docker Compose (integrated)

Runs the single deployable (Kestrel serving the Angular build) plus PostgreSQL. Migrations apply and the admin user is seeded automatically on startup.

```bash
cp .env.example .env   # then fill in values
docker compose up --build
```

Open http://localhost:8080. Tear down with `docker compose down` (add `-v` to also drop the database volume).

This setup does **not** hot-reload — rebuild with `docker compose up --build` after changes. Use Option B for active development.

## Option B — From-source live dev (hot reload)

Postgres in Docker; the API and client run from source and reload on save. Use three terminals.

```bash
# 0. (once) stop the bundled stack so you only run Postgres here
docker compose down

# 1. PostgreSQL (host-published on 5432)
docker run -d --name dl-postgres -p 5432:5432 \
  -e POSTGRES_DB=drivinglessons -e POSTGRES_USER=app -e POSTGRES_PASSWORD=devpassword postgres:17
# (already created it before? just: docker start dl-postgres)

# 2. API with hot reload — listens on :5080, auto-migrates + seeds admin
dotnet watch run --project src/DrivingLessons.Presentation.Web

# 3. Angular with hot reload — http://localhost:4200, /api proxies to :5080
cd client && npm start
```

Open http://localhost:4200 and sign in with the dev admin credentials above. Edit any `.ts` / `.html` / `.scss` (frontend) or `.cs` (backend) and the change reloads automatically. Toggle EN / עב in the corner to check Hebrew RTL.

## Build & test

```bash
dotnet build                          # backend
dotnet test                           # domain unit tests
cd client && npm run build            # production client build
cd client && npm test                 # client unit tests
```
