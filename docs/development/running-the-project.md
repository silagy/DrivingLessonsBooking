# Running the Project

How to run the driving-lessons app locally. See [tech-stack.md](../tech-stack.md) for the architecture.

| Setup | What runs where | Reloads on save? | Use it for |
|-------|-----------------|------------------|------------|
| **A. Docker Compose** | App (API + SPA) and Postgres, all in Docker | No, rebuild the image | Checking the real container before a deploy |
| **B. Live dev** (recommended day to day) | Postgres in Docker, API and client from source | Yes | Writing code |

Both setups share **the same Postgres container and data** (`drivinglessonsbooking-postgres-1`, volume `drivinglessonsbooking_pgdata`), so you can switch between them without losing anything.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/), with the engine running
- [.NET SDK 10](https://dotnet.microsoft.com/download), for Setup B
- [Node.js 24+](https://nodejs.org/), for the Angular client in Setup B
- An IDE is optional: Rider or Visual Studio can run the API instead of `dotnet watch`

All commands below are one line each, so they work in PowerShell, cmd and bash. Run them from the repository root unless a step says otherwise.

## Ports & defaults

| Thing | Where |
|-------|-------|
| App (Docker, API + SPA) | http://localhost:8080 |
| API from source | http://localhost:5080 |
| Angular dev server | http://localhost:4200 |
| OpenAPI explorer (Scalar, dev only) | http://localhost:5080/scalar/v1 |
| PostgreSQL | localhost:5432 |
| Dev admin login (Setup B) | `admin@local.dev` / `DevAdmin#2026` |
| Admin login (Setup A) | `ADMIN_EMAIL` / `ADMIN_PASSWORD` from `.env` |
| Mailpit inbox (optional, local email) | http://localhost:8025 (SMTP on 1025) |

The client always calls relative `/api/...` URLs. In dev, `client/proxy.conf.json` proxies `/api` to `http://localhost:5080`.

## One-time setup: `.env`

Docker Compose reads its settings from `.env` in the repository root.

1. Copy the example:
   ```bash
   cp .env.example .env
   ```
   (PowerShell: `Copy-Item .env.example .env`)
2. Set **`POSTGRES_PASSWORD=devpassword`**. This must match the password in `src/DrivingLessons.Presentation.Web/appsettings.Development.json`, because the API run from source (Setup B) connects to the same database with that connection string.
3. Fill in `ADMIN_EMAIL`, `ADMIN_PASSWORD` and `JWT_SIGNING_KEY` (at least 32 characters). Leave `EMAIL_ENABLED=false`.

Your local `.env` is for your machine only. The strong production passwords go in the `.env` on the server, never in this one.

> **Postgres reads `POSTGRES_PASSWORD` only once**, when it first creates the data volume. Changing `.env` later does **not** change the database password. The app then fails at startup with `28P01: password authentication failed for user "app"`. See [Troubleshooting](#troubleshooting).

## Setup A: Docker Compose (integrated)

Runs the single deployable (Kestrel serving the Angular build) plus PostgreSQL, the same way production does. Migrations apply on startup. On an empty database, the first Administrator is created from `ADMIN_EMAIL` / `ADMIN_PASSWORD`. Once any User exists, changing those values does nothing: the stored email and password stay as they are.

1. Build and start everything:
   ```bash
   docker compose up -d --build
   ```
2. Open http://localhost:8080 and sign in with `ADMIN_EMAIL` / `ADMIN_PASSWORD` from `.env`.
3. Watch the app logs if something looks wrong:
   ```bash
   docker compose logs -f app
   ```

**Getting your changes in:** the image is built from source inside Docker (see the `Dockerfile`). Building the solution in your IDE does **not** change what the container runs. After any backend or client change, rebuild:

```bash
docker compose up -d --build
```

Stop it with `docker compose down`. Your data is kept. `docker compose down -v` also **deletes the database volume and all local data**.

## Setup B: Live dev (hot reload), recommended day to day

Postgres runs in Docker. The API and the Angular client run from source and reload when you save. You need three terminals, or two terminals plus your IDE.

### Step 1: Start only Postgres

```bash
docker compose stop app
docker compose up -d postgres
```

The first line frees port 8080 if Setup A was running. It does nothing if it wasn't. Check that Postgres is healthy:

```bash
docker compose ps
```

### Step 2: Run the API (port 5080)

Pick one:

- **Terminal, with hot reload:**
  ```bash
  dotnet watch run --project src/DrivingLessons.Presentation.Web
  ```
- **Rider / Visual Studio:** open `DrivingLessons.sln`, select the `DrivingLessons.Presentation.Web` project with the **`http`** launch profile, and press Run or Debug. Use this when you want breakpoints. Rider's and Visual Studio's hot reload apply most code edits, and you restart for the rest.

Both run in the `Development` environment, use `appsettings.Development.json`, apply migrations on startup and, on an empty database, create the dev Administrator from `appsettings.Development.json`. Wait for `Now listening on: http://localhost:5080`.

### Step 3: Run the Angular client (port 4200)

In a new terminal:

```bash
cd client
npm ci
npm start
```

Run `npm ci` the first time, and again whenever `client/package-lock.json` changes. After that, `npm start` is enough.

### Step 4: Open the app

Open http://localhost:4200 and sign in with `admin@local.dev` / `DevAdmin#2026`. Edit any `.ts` / `.html` / `.scss` (client) or `.cs` (backend) file and the change reloads automatically. Toggle EN / עב in the corner to check Hebrew RTL.

**Getting your changes in:**

| You changed | What to do |
|-------------|------------|
| Client code (`client\`) | Nothing, the browser reloads |
| Backend code (`.cs`) | Nothing with `dotnet watch`. If it prints that a change needs a restart (rude edit), press `Ctrl+R` in that terminal. In the IDE, apply hot reload or restart |
| A new EF migration | Restart the API. Migrations apply on startup |
| `appsettings*.json` or `Program.cs` DI setup | Restart the API |
| `client/package.json` dependencies | Stop `npm start`, run `npm ci`, start again |

### Stopping Setup B

Press `Ctrl+C` in the API and client terminals, then:

```bash
docker compose stop postgres
```

## Switching between setups

| From → to | Commands |
|-----------|----------|
| A → B | `docker compose stop app`, then Steps 2 and 3 of Setup B |
| B → A | `Ctrl+C` the API and client, then `docker compose up -d --build` |

You don't need `down` to switch. The database stays the same either way.

### Moving off the old `dl-postgres` container

Earlier versions of this guide ran Postgres as a separate `dl-postgres` container. It also binds port 5432, so it conflicts with the Compose Postgres, and its migration history is out of date. Stop it and use the Compose one:

```bash
docker stop dl-postgres
docker compose up -d postgres
```

Once you're sure you don't need its data, remove it with `docker rm dl-postgres`. This deletes that data for good.

## API explorer (Scalar / OpenAPI)

When the API runs from source (Setup B), an interactive OpenAPI explorer is served at:

- Scalar UI: http://localhost:5080/scalar/v1
- Raw spec: http://localhost:5080/openapi/v1.json

It's enabled in the **Development** environment only. The Docker Compose stack (Setup A) runs in Production and does **not** expose it. Reach it on the API host (`:5080`) directly, not through the Angular dev server (`:4200`), which only proxies `/api`.

To call secured endpoints: `POST /api/auth/login` with the dev admin credentials, copy the returned `accessToken`, click **Authorize** in Scalar and paste it. Requests then send `Authorization: Bearer <token>`.

## Email locally (Mailpit)

At every window close the API emails each teacher their Excel file over SMTP ([ADR 0005](../decisions/0005-email-over-ses-smtp.md)). In Development it points at a local Mailpit on port 1025, but sends nothing until you turn it on. With `Email:Enabled` false (the default) it logs `Email disabled. Skipped send…` instead.

1. Start the local inbox (SMTP on 1025, web UI on 8025):
   ```bash
   docker run -d --name dl-mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
   ```
   If you created it before, run `docker start dl-mailpit` instead.
2. Run the API with email on:
   ```bash
   dotnet run --project src/DrivingLessons.Presentation.Web -- --Email:Enabled=true
   ```
3. Publish a week with a short window. When it closes, each teacher's email (Hebrew subject, the `.xlsx` attached) appears at http://localhost:8025.

In Docker Compose and production, set the `EMAIL_*` variables in `.env` (see `.env.example`).

## Build & test

```bash
dotnet build
dotnet test
```

Client, from `client\`:

```bash
npm run build
npm test
```

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `28P01: password authentication failed for user "app"` | The database volume was created with a different password than the one the app now uses | Put `POSTGRES_PASSWORD` in `.env` back to the original value. Or, to keep the data, reset it in the database (below). Or wipe the data with `docker compose down -v` |
| `42P07: relation "..." already exists` on API startup | The API is connected to the old `dl-postgres` container, whose migration history is out of date | [Move off `dl-postgres`](#moving-off-the-old-dl-postgres-container) |
| Port 5432 already in use | `dl-postgres` or a local Postgres install is running | `docker stop dl-postgres`, or stop the local service |
| Port 8080 already in use | The Compose `app` container is still running | `docker compose stop app` |
| Docker shows old behavior after a code change | The image wasn't rebuilt | `docker compose up -d --build` |
| `'ng' is not recognized` on `npm start` | `client\node_modules` is missing or only partly installed | Run `npm ci` in `client\`, then `npm start` |
| `npm ci` fails with `EPERM ... esbuild.exe` | A running `ng serve` (or an old one left over) is holding the file | Stop every `ng serve` (`Ctrl+C`, or end the `node` / `esbuild` processes in Task Manager), then run `npm ci` again |

To make the database password match the current `POSTGRES_PASSWORD` in `.env` without losing data, run this from **bash** (Git Bash or WSL). PowerShell breaks the quoting:

```bash
docker compose up -d --force-recreate postgres
docker compose exec -T postgres sh -c 'echo "ALTER USER app PASSWORD :'"'"'pw'"'"';" | psql -U app -d drivinglessons -v pw="$POSTGRES_PASSWORD"'
```

The first line makes sure the container has the current `.env` value. Inside the container, local connections don't need a password, so this works even while the old password is unknown.
