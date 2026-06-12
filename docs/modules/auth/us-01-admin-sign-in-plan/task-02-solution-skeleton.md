# Task 2 of 11: Solution + Onion projects

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires task 1 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication — while scaffolding the full greenfield skeleton every later slice builds on.

**Architecture:** Modular monolith, DDD, Onion: ASP.NET Core Web API (.NET 10) serving the Angular build, PostgreSQL via EF Core. Root namespace decision: `DrivingLessons` (not `DrivingLessonsBooking`) — booking is explicitly out of scope for v1; the namespace shouldn't be named after an excluded feature.

**Onion references:** `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice · full skeleton scaffolding.

---

**Files:**
- Create: `DrivingLessons.sln`, four projects under `src/`, `.config/dotnet-tools.json`
- Delete: template noise (`Class1.cs` × 3, Presentation.Web weather sample, `DrivingLessons.Presentation.Web.http`)

- [ ] **Step 1: Scaffold solution and projects (run from repo root)**

```
dotnet new sln -n DrivingLessons
dotnet new classlib -n DrivingLessons.Domain -o src/DrivingLessons.Domain -f net10.0
dotnet new classlib -n DrivingLessons.Application -o src/DrivingLessons.Application -f net10.0
dotnet new classlib -n DrivingLessons.Infrastructure -o src/DrivingLessons.Infrastructure -f net10.0
dotnet new webapi -n DrivingLessons.Presentation.Web -o src/DrivingLessons.Presentation.Web -f net10.0 --use-controllers
dotnet sln DrivingLessons.sln add src/DrivingLessons.Domain src/DrivingLessons.Application src/DrivingLessons.Infrastructure src/DrivingLessons.Presentation.Web
```

- [ ] **Step 2: Wire Onion references**

```
dotnet add src/DrivingLessons.Application reference src/DrivingLessons.Domain
dotnet add src/DrivingLessons.Infrastructure reference src/DrivingLessons.Application
dotnet add src/DrivingLessons.Presentation.Web reference src/DrivingLessons.Application
dotnet add src/DrivingLessons.Presentation.Web reference src/DrivingLessons.Infrastructure
```

- [ ] **Step 3: Delete template noise**

Delete: `src/DrivingLessons.Domain/Class1.cs`, `src/DrivingLessons.Application/Class1.cs`, `src/DrivingLessons.Infrastructure/Class1.cs`, and from Presentation.Web: `WeatherForecast.cs`, `Controllers/WeatherForecastController.cs` (if generated), `DrivingLessons.Presentation.Web.http`.

- [ ] **Step 4: Add packages + EF tool manifest**

```
dotnet add src/DrivingLessons.Application package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/DrivingLessons.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/DrivingLessons.Infrastructure package Microsoft.Extensions.Identity.Core
dotnet add src/DrivingLessons.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add src/DrivingLessons.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions
dotnet add src/DrivingLessons.Infrastructure package Microsoft.Extensions.Options.DataAnnotations
dotnet add src/DrivingLessons.Presentation.Web package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/DrivingLessons.Presentation.Web package Microsoft.EntityFrameworkCore.Design
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: solution skeleton with onion layers"
```

---

**Next:** [task-03-exceptions-and-filter.md](task-03-exceptions-and-filter.md)
