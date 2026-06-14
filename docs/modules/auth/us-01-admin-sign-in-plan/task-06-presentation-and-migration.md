# Task 6 of 11: Presentation.Web — controller, Program.cs, config, first migration

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–5 complete (this task commits the task 5 work together with the migration). Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Architecture:** Modular monolith, DDD, Onion. Controllers with zero logic delegating to thin interactors; a global exception filter maps domain violations (the 401 mapping for login also lives in the filter). Root namespace: `DrivingLessons`.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice.

## Risks / Gotchas for this task

- **`MapFallbackToFile(...).AllowAnonymous()` is load-bearing** — the authorize-by-default fallback policy applies to the SPA fallback endpoint; without it the login page itself 401s.
- **Unknown `/api/*` URLs return index.html (200)** via SPA fallback instead of 404 — acceptable for v1.
- **`Database.MigrateAsync()` on startup** is fine for a single instance; unsafe with replicas — revisit if scaling out.
- **`dotnet ef migrations add` executes Program.cs** at design time — keep `appsettings.Development.json` populated so the connection string parses.
- **No CORS anywhere:** dev uses the ng serve proxy (same-origin from the browser's view); prod is single-origin.

---

**Files:**
- Create: `src/DrivingLessons.Presentation.Web/Controllers/AuthController.cs`
- Replace: `src/DrivingLessons.Presentation.Web/Program.cs`
- Replace: `src/DrivingLessons.Presentation.Web/appsettings.json`, `appsettings.Development.json`
- Replace: `src/DrivingLessons.Presentation.Web/Properties/launchSettings.json`
- Create (generated): `src/DrivingLessons.Infrastructure/Persistence/Migrations/*`

- [ ] **Step 1: `AuthController.cs`** (zero logic — even 401 mapping lives in the filter)

```csharp
using DrivingLessons.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginInteractor login) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) =>
        login.Handle(command, cancellationToken);
}
```

- [ ] **Step 2: `Program.cs`** (complete file)

```csharp
using System.Text;
using DrivingLessons.Presentation.Web.Filters;
using DrivingLessons.Application;
using DrivingLessons.Infrastructure;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// Authentication required by default; anything public must opt out with [AllowAnonymous] / .AllowAnonymous().
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Migrate + seed the single admin from configuration (acceptable for a single-instance v1 deployment).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value;
    await AdminSeeder.SeedAsync(db, adminOptions);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback: Angular owns client-side routes. Must be AllowAnonymous or the fallback policy blocks index.html.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();
```

- [ ] **Step 3: `appsettings.json`** (complete file — production values come from env vars)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": ""
  },
  "Admin": {
    "Email": "",
    "Password": ""
  },
  "Jwt": {
    "Issuer": "DrivingLessons",
    "Audience": "DrivingLessons",
    "SigningKey": "",
    "ExpiryHours": 12
  }
}
```

- [ ] **Step 4: `appsettings.Development.json`** (dev-only secrets, fine to commit)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=drivinglessons;Username=app;Password=devpassword"
  },
  "Admin": {
    "Email": "admin@local.dev",
    "Password": "DevAdmin#2026"
  },
  "Jwt": {
    "Issuer": "DrivingLessons",
    "Audience": "DrivingLessons",
    "SigningKey": "dev-only-signing-key-at-least-32-characters-long!",
    "ExpiryHours": 12
  }
}
```

- [ ] **Step 5: `Properties/launchSettings.json`** — pin dev port 5080 (Angular proxy targets it)

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 6: Create the first migration** (does not need a running DB)

```
dotnet ef migrations add InitialCreate --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir Persistence/Migrations
```

Expected: migration files in `src/DrivingLessons.Infrastructure/Persistence/Migrations/`. Commit them.

- [ ] **Step 7: Build + commit**

Run: `dotnet build` — expected: success.

```bash
git add -A
git commit -m "feat(api): jwt auth with login interactor, admin seeding, initial migration"
```

- [ ] **Step 8: Smoke-test the API** (optional but recommended)

```
docker run -d --name dl-postgres -p 5432:5432 -e POSTGRES_DB=drivinglessons -e POSTGRES_USER=app -e POSTGRES_PASSWORD=devpassword postgres:17
dotnet run --project src/DrivingLessons.Presentation.Web
```

Then: `curl -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}'`
Expected: 200 with `accessToken`. Wrong password → 401 ProblemDetails.

---

**Next:** [task-07-angular-workspace.md](task-07-angular-workspace.md)
