# Task 10 of 11: Docker — Dockerfile, docker-compose.yml, .env.example

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–9 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, files at repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Hosting:** Single deployable — the .NET API serves the Angular build from `wwwroot`. Docker Compose: postgres:17 + multi-stage app image. Production config comes from env vars; the real `.env` is gitignored (task 1) while `.env.example` is committed.

---

**Files:**
- Create: `Dockerfile`, `docker-compose.yml`, `.env.example`, `.dockerignore` (all repo root)

- [ ] **Step 1: `Dockerfile`** (multi-stage)

```dockerfile
# Stage 1: build the Angular client
FROM node:24-alpine AS client-build
WORKDIR /client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# Stage 2: build and publish the .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /src
COPY DrivingLessons.sln ./
COPY src/ ./src/
RUN dotnet publish src/DrivingLessons.Presentation.Web/DrivingLessons.Presentation.Web.csproj -c Release -o /app/publish

# Stage 3: runtime image serving API + SPA
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=server-build /app/publish ./
COPY --from=client-build /client/dist/client/browser ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DrivingLessons.Presentation.Web.dll"]
```

- [ ] **Step 2: `docker-compose.yml`**

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: drivinglessons
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d drivinglessons"]
      interval: 5s
      timeout: 3s
      retries: 12

  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=drivinglessons;Username=app;Password=${POSTGRES_PASSWORD}"
      Admin__Email: ${ADMIN_EMAIL}
      Admin__Password: ${ADMIN_PASSWORD}
      Jwt__Issuer: DrivingLessons
      Jwt__Audience: DrivingLessons
      Jwt__SigningKey: ${JWT_SIGNING_KEY}
      Jwt__ExpiryHours: "12"
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  pgdata:
```

- [ ] **Step 3: `.env.example`** (committed; real `.env` is gitignored)

```dotenv
POSTGRES_PASSWORD=change-me-strong-db-password
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=change-me-strong-admin-password
# Minimum 32 characters (HS256). Generate: openssl rand -base64 48
JWT_SIGNING_KEY=change-me-to-a-random-string-of-at-least-32-chars
```

- [ ] **Step 4: `.dockerignore`**

```
**/node_modules
**/dist
**/bin
**/obj
.git
.env
docs
```

- [ ] **Step 5: Build the image + commit**

Run: `docker compose build` — expected: image builds.

```bash
git add Dockerfile docker-compose.yml .env.example .dockerignore
git commit -m "feat: dockerfile and compose for postgres plus single app image"
```

---

**Next:** [task-11-manual-verification.md](task-11-manual-verification.md)
