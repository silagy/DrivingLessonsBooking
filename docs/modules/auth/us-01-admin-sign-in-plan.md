# US-01: Admin Signs In with Email and Password — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement GitHub issue #2 (US-01) — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication — while scaffolding the full greenfield skeleton every later slice builds on.

**Architecture:** Modular monolith, DDD, Onion (per PRD issue #1): ASP.NET Core Web API (.NET 10) serving the Angular build, PostgreSQL via EF Core, Docker Compose. Admin auth is **infrastructure-level** — there is deliberately no Admin domain aggregate. JWT bearer tokens (user decision), admin credentials seeded from configuration (user decision), **no automated tests for this slice** (PRD: v1 tests cover domain aggregates only; user confirmed).

**Tech Stack:** .NET 10 / C# 14, EF Core 10 + Npgsql, ASP.NET Core JwtBearer, Angular 21+ (zoneless, standalone, signals only), PrimeNG (matching major) + @primeuix/themes, Transloco (runtime he/en toggle, RTL-first), Docker Compose (postgres:17 + multi-stage app image).

## Context

The repo contains only docs (`README.md`, `docs/requirements.md`, `docs/designer-prompt.md`) — no code. Branch `2-us-01-admin-signs-in-with-email-and-password` exists for this slice. The PRD (issue #1) fixes the architecture rules: controllers with zero logic delegating to thin interactors, a global exception filter mapping domain violations to 409, value objects at the application boundary (deferred to aggregate slices), Angular with signals only and every visible string a translation key, Hebrew RTL-first.

**Acceptance criteria (issue #2):** Given I open the admin app not signed in and have valid administrator credentials, when I submit my email and password on the login page, then I am signed in and taken to the admin area, which is unreachable without authentication.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice · full skeleton scaffolding.

**Root namespace decision:** `DrivingLessons` (not `DrivingLessonsBooking`) — booking is explicitly out of scope for v1; the namespace shouldn't be named after an excluded feature.

## Repository Layout (target)

```
DrivingLessonsBooking\
├── DrivingLessons.sln
├── src\
│   ├── DrivingLessons.Domain\            # DomainException now; aggregates in future slices
│   ├── DrivingLessons.Application\       # LoginInteractor, auth ports, app exceptions
│   ├── DrivingLessons.Infrastructure\    # EF Core, JWT, password hashing, seeding, migrations
│   └── DrivingLessons.Presentation.Web\               # AuthController, exception filter, Program.cs, SPA host
├── client\                               # Angular workspace
│   ├── public\i18n\{en,he}.json
│   └── src\app\{core, features\auth, features\admin-shell}
├── Dockerfile  docker-compose.yml  .env.example  .dockerignore  .gitignore  .editorconfig
```

Onion references: `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.

---

### Task 1: Repo hygiene — .gitignore + .editorconfig

**Files:**
- Create: `.gitignore` (via `dotnet new gitignore`, then append)
- Create: `.editorconfig`

- [ ] **Step 1: Generate .gitignore and append Node/Angular/env entries**

```
dotnet new gitignore
```

Append to `.gitignore`:

```gitignore

# Node / Angular
node_modules/
client/dist/
client/.angular/

# Environment
.env
```

- [ ] **Step 2: Create `.editorconfig`**

```editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{ts,html,scss,json,yml,yaml}]
indent_size = 2

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_when_type_is_apparent = true:suggestion
```

- [ ] **Step 3: Commit**

```bash
git add .gitignore .editorconfig
git commit -m "chore: gitignore and editorconfig"
```

---

### Task 2: Solution + Onion projects

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

### Task 3: Exception base types + global exception filter (PRD-mandated)

**Files:**
- Create: `src/DrivingLessons.Domain/Common/DomainException.cs`
- Create: `src/DrivingLessons.Application/Common/Exceptions/NotFoundException.cs`
- Create: `src/DrivingLessons.Application/Common/Exceptions/AuthenticationFailedException.cs`
- Create: `src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs`

- [ ] **Step 1: `DomainException.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

/// <summary>Base type for domain rule violations. Mapped to HTTP 409 by the API exception filter.</summary>
public abstract class DomainException(string message) : Exception(message);
```

- [ ] **Step 2: `NotFoundException.cs`**

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

/// <summary>Requested resource does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);
```

- [ ] **Step 3: `AuthenticationFailedException.cs`**

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

/// <summary>Login failed. Deliberately carries no detail about which credential was wrong. Mapped to HTTP 401.</summary>
public sealed class AuthenticationFailedException() : Exception("Invalid credentials.");
```

- [ ] **Step 4: `ApiExceptionFilter.cs`**

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, title) = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DomainException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return; // unknown exception: let the default 500 pipeline handle it
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        })
        { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
```

- [ ] **Step 5: Build + commit**

Run: `dotnet build` — expected: success.

```bash
git add src
git commit -m "feat(api): exception base types and global exception filter"
```

---

### Task 4: Application layer — auth ports + login interactor

**Files:**
- Create: `src/DrivingLessons.Application/Auth/LoginCommand.cs`, `LoginResult.cs`, `IAdminAccountGateway.cs`, `IPasswordVerifier.cs`, `IJwtTokenGenerator.cs`, `LoginInteractor.cs`
- Create: `src/DrivingLessons.Application/DependencyInjection.cs`

Note: no value objects here on purpose — VOs belong to aggregates (Teacher/Student slices). Auth is infrastructure-level and pre-aggregate.

- [ ] **Step 1: `LoginCommand.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record LoginCommand(string Email, string Password);
```

- [ ] **Step 2: `LoginResult.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
```

- [ ] **Step 3: `IAdminAccountGateway.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record AdminAccount(Guid Id, string Email, string PasswordHash);

public interface IAdminAccountGateway
{
    Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: `IPasswordVerifier.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public interface IPasswordVerifier
{
    bool Verify(string passwordHash, string providedPassword);
}
```

- [ ] **Step 5: `IJwtTokenGenerator.cs`**

```csharp
namespace DrivingLessons.Application.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    IssuedToken Generate(Guid adminId, string email);
}
```

- [ ] **Step 6: `LoginInteractor.cs`**

```csharp
using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Auth;

public sealed class LoginInteractor(
    IAdminAccountGateway accounts,
    IPasswordVerifier passwords,
    IJwtTokenGenerator tokens)
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var account = await accounts.FindByEmailAsync(normalizedEmail, cancellationToken);

        // Single failure path: never reveal whether the email or the password was wrong.
        if (account is null || !passwords.Verify(account.PasswordHash, command.Password ?? string.Empty))
        {
            throw new AuthenticationFailedException();
        }

        var issued = tokens.Generate(account.Id, account.Email);
        return new LoginResult(issued.AccessToken, issued.ExpiresAtUtc);
    }
}
```

- [ ] **Step 7: `DependencyInjection.cs`**

```csharp
using DrivingLessons.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginInteractor>();
        return services;
    }
}
```

- [ ] **Step 8: Build + commit**

Run: `dotnet build` — expected: success.

```bash
git add src
git commit -m "feat(app): login interactor with auth ports"
```

---

### Task 5: Infrastructure — entity, DbContext, options, adapters, seeder

**Files:**
- Create: `src/DrivingLessons.Infrastructure/Auth/AdminUser.cs`, `AdminAccountGateway.cs`, `PasswordVerifier.cs`, `JwtTokenGenerator.cs`, `AdminSeeder.cs`
- Create: `src/DrivingLessons.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/DrivingLessons.Infrastructure/Options/AdminOptions.cs`, `JwtOptions.cs`
- Create: `src/DrivingLessons.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: `AdminUser.cs`**

```csharp
namespace DrivingLessons.Infrastructure.Auth;

/// <summary>Infrastructure-level credential record. Deliberately NOT a domain aggregate (PRD: no Admin aggregate).</summary>
public sealed class AdminUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
}
```

- [ ] **Step 2: `AppDbContext.cs`**

```csharp
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(builder =>
        {
            builder.ToTable("admin_users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.Property(x => x.PasswordHash).IsRequired();
        });
    }
}
```

- [ ] **Step 3: `AdminOptions.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
```

- [ ] **Step 4: `JwtOptions.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required, MinLength(32)] // HS256 needs >= 256-bit key
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 168)]
    public int ExpiryHours { get; init; } = 12; // single admin, working-day-plus expiry
}
```

- [ ] **Step 5: `AdminAccountGateway.cs`**

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class AdminAccountGateway(AppDbContext dbContext) : IAdminAccountGateway
{
    public async Task<AdminAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Email == normalizedEmail, cancellationToken);

        return admin is null ? null : new AdminAccount(admin.Id, admin.Email, admin.PasswordHash);
    }
}
```

- [ ] **Step 6: `PasswordVerifier.cs`**

```csharp
using DrivingLessons.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Dummy = new();

    public bool Verify(string passwordHash, string providedPassword) =>
        Hasher.VerifyHashedPassword(Dummy, passwordHash, providedPassword)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
```

- [ ] **Step 7: `JwtTokenGenerator.cs`**

```csharp
using System.Text;
using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DrivingLessons.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public IssuedToken Generate(Guid adminId, string email)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(jwt.ExpiryHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = adminId.ToString(),
                [JwtRegisteredClaimNames.Email] = email,
                ["role"] = "admin"
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new IssuedToken(token, expiresAt);
    }
}
```

- [ ] **Step 8: `AdminSeeder.cs`** (idempotent upsert; re-hashing on every boot means rotating the env var rotates the credential)

```csharp
using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Auth;

/// <summary>Upserts the single admin row from configuration on startup. No registration UI exists.</summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, AdminOptions options, CancellationToken cancellationToken = default)
    {
        var hasher = new PasswordHasher<AdminUser>();
        var normalizedEmail = options.Email.Trim().ToLowerInvariant();

        var admin = await dbContext.AdminUsers.SingleOrDefaultAsync(cancellationToken);
        if (admin is null)
        {
            admin = new AdminUser { Id = Guid.NewGuid(), Email = normalizedEmail, PasswordHash = string.Empty };
            dbContext.AdminUsers.Add(admin);
        }

        admin.Email = normalizedEmail;
        admin.PasswordHash = hasher.HashPassword(admin, options.Password);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 9: `DependencyInjection.cs`** (Infrastructure)

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.Options;
using DrivingLessons.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAdminAccountGateway, AdminAccountGateway>();
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
```

- [ ] **Step 10: Build**

Run: `dotnet build` — expected: success. (Commit comes after Task 6 with the migration.)

---

### Task 6: Presentation.Web — controller, Program.cs, config, first migration

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

### Task 7: Angular workspace + PrimeNG + Transloco + RTL toggle

**Files:**
- Create: `client/` workspace (`ng new`), `client/proxy.conf.json`
- Modify: `client/angular.json` (proxy), `client/src/index.html`, `client/src/styles.scss`
- Create: `client/src/app/transloco-loader.ts`, `client/public/i18n/en.json`, `client/public/i18n/he.json`
- Create: `client/src/app/core/language.service.ts`
- Replace: `client/src/app/app.config.ts`, `client/src/app/app.ts`

- [ ] **Step 1: Scaffold workspace + packages** (from repo root)

```
npx @angular/cli@latest new client --directory client --style scss --ssr false --skip-git --zoneless
cd client
npm install primeng @primeuix/themes primeicons @jsverse/transloco
```

PrimeNG major MUST match the Angular major `ng new` produced (e.g., Angular 21 ↔ `primeng@21`). If PrimeNG lags the newest Angular major, scaffold one major lower: `npx @angular/cli@21 new ...`.

- [ ] **Step 2: `client/proxy.conf.json`** + wire into `angular.json` under `projects.client.architect.serve.options`

```json
{
  "/api": {
    "target": "http://localhost:5080",
    "secure": false
  }
}
```

```json
"proxyConfig": "proxy.conf.json"
```

- [ ] **Step 3: `client/src/index.html`** — Hebrew/RTL-first defaults

```html
<!doctype html>
<html lang="he" dir="rtl">
<head>
  <meta charset="utf-8">
  <title>Driving Lessons</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

- [ ] **Step 4: `client/src/styles.scss`**

```scss
@import "primeicons/primeicons.css";

* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; height: 100%; font-family: system-ui, sans-serif; }
```

- [ ] **Step 5: `client/src/app/transloco-loader.ts`**

```ts
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(lang: string) {
    return this.http.get<Translation>(`/i18n/${lang}.json`);
  }
}
```

- [ ] **Step 6: `client/public/i18n/en.json`**

```json
{
  "auth": {
    "title": "Admin Sign In",
    "email": "Email",
    "password": "Password",
    "submit": "Sign in",
    "invalidCredentials": "Incorrect email or password"
  },
  "shell": {
    "title": "Driving Lessons Planner",
    "logout": "Sign out",
    "languageToggle": "עברית"
  },
  "dashboard": {
    "title": "Dashboard",
    "placeholder": "Welcome. Planning features arrive in the next slices."
  }
}
```

- [ ] **Step 7: `client/public/i18n/he.json`**

```json
{
  "auth": {
    "title": "כניסת מנהל",
    "email": "אימייל",
    "password": "סיסמה",
    "submit": "כניסה",
    "invalidCredentials": "אימייל או סיסמה שגויים"
  },
  "shell": {
    "title": "מערכת תכנון שיעורי נהיגה",
    "logout": "התנתקות",
    "languageToggle": "English"
  },
  "dashboard": {
    "title": "לוח בקרה",
    "placeholder": "ברוך הבא. יכולות התכנון יגיעו בפרוסות הבאות."
  }
}
```

(The toggle label is itself a translation key whose value is the other language's name — keeps the "every visible string is a key" rule intact.)

- [ ] **Step 8: `client/src/app/core/language.service.ts`** (signals only; flips `dir` for RTL)

```ts
import { effect, inject, Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

export type AppLanguage = 'he' | 'en';
const STORAGE_KEY = 'app_lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);

  readonly lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'he',
  );

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      this.transloco.setActiveLang(lang);
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'he' ? 'rtl' : 'ltr';
    });
  }

  toggle(): void {
    this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
  }
}
```

- [ ] **Step 9: `client/src/app/app.config.ts`** (complete file; `authInterceptor` file is created in Task 8 — create both in the same commit)

```ts
import { ApplicationConfig, provideZonelessChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { TranslocoHttpLoader } from './transloco-loader';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({ theme: { preset: Aura } }),
    provideTransloco({
      config: {
        availableLangs: ['he', 'en'],
        defaultLang: 'he',
        fallbackLang: 'en',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
```

- [ ] **Step 10: `client/src/app/app.ts`** (root component — instantiating LanguageService makes the dir/lang effect run app-wide)

```ts
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  private readonly language = inject(LanguageService);
}
```

(No separate commit yet — Tasks 7–9 commit together once the client compiles.)

---

### Task 8: Client auth — service, interceptor, guard, routes

**Files:**
- Create: `client/src/app/core/auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts`
- Replace: `client/src/app/app.routes.ts`

**Token storage decision: `localStorage`** — the single admin survives page refreshes and tab reopens without re-login; XSS exposure is an accepted v1 risk for a one-user internal tool (same posture as the PRD's email-only student identity).

- [ ] **Step 1: `auth.service.ts`**

```ts
import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

const TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly isAuthenticated = computed(() => this.token() !== null);

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(TOKEN_KEY, response.accessToken);
          this.token.set(response.accessToken);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
    void this.router.navigate(['/login']);
  }
}
```

- [ ] **Step 2: `auth.interceptor.ts`**

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Expired/invalid token on any API call (except the login attempt itself): drop session, go to login.
      if (error.status === 401 && !req.url.includes('/api/auth/login')) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
```

- [ ] **Step 3: `auth.guard.ts`**

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};
```

- [ ] **Step 4: `app.routes.ts`**

```ts
import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/admin-shell/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
```

---

### Task 9: Login page + admin shell

**Files:**
- Create: `client/src/app/features/auth/login.component.ts`, `.html`, `.scss`
- Create: `client/src/app/features/admin-shell/admin.routes.ts`, `admin-shell.component.ts`, `.html`, `dashboard.component.ts`

- [ ] **Step 1: `login.component.ts`**

```ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TranslocoPipe, InputTextModule, PasswordModule, ButtonModule, MessageModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly error = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      return;
    }
    this.loading.set(true);
    this.error.set(false);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      },
    });
  }
}
```

- [ ] **Step 2: `login.component.html`** (logical CSS + PrimeNG, mirror-correct under `dir="rtl"`)

```html
<div class="login-page">
  <form class="login-card" [formGroup]="form" (ngSubmit)="submit()">
    <h1>{{ 'auth.title' | transloco }}</h1>

    <label for="email">{{ 'auth.email' | transloco }}</label>
    <input pInputText id="email" type="email" formControlName="email" autocomplete="username" />

    <label for="password">{{ 'auth.password' | transloco }}</label>
    <p-password
      inputId="password"
      formControlName="password"
      [feedback]="false"
      [toggleMask]="true"
      styleClass="login-password" />

    @if (error()) {
      <p-message severity="error" [text]="'auth.invalidCredentials' | transloco" />
    }

    <p-button
      type="submit"
      [label]="'auth.submit' | transloco"
      [loading]="loading()"
      [disabled]="form.invalid" />
  </form>
</div>
```

- [ ] **Step 3: `login.component.scss`**

```scss
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-card {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  width: min(22rem, 90vw);

  label {
    text-align: start; // logical property: correct in both LTR and RTL
  }

  :is(input, .login-password) {
    width: 100%;
  }
}
```

- [ ] **Step 4: `admin.routes.ts`**

```ts
import { Routes } from '@angular/router';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [{ path: '', component: DashboardComponent }],
  },
];
```

- [ ] **Step 5: `admin-shell.component.ts`**

```ts
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, TranslocoPipe, ToolbarModule, ButtonModule],
  templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly language = inject(LanguageService);
}
```

- [ ] **Step 6: `admin-shell.component.html`**

```html
<p-toolbar>
  <ng-template #start>
    <strong>{{ 'shell.title' | transloco }}</strong>
  </ng-template>
  <ng-template #end>
    <p-button [label]="'shell.languageToggle' | transloco" (onClick)="language.toggle()" text />
    <p-button [label]="'shell.logout' | transloco" (onClick)="auth.logout()" severity="secondary" text />
  </ng-template>
</p-toolbar>
<main style="padding: 1rem">
  <router-outlet />
</main>
```

- [ ] **Step 7: `dashboard.component.ts`**

```ts
import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-dashboard',
  imports: [TranslocoPipe],
  template: `
    <h2>{{ 'dashboard.title' | transloco }}</h2>
    <p>{{ 'dashboard.placeholder' | transloco }}</p>
  `,
})
export class DashboardComponent {}
```

- [ ] **Step 8: Verify client builds, then commit Tasks 7–9**

Run (in `client/`): `npm run build` — expected: success. If the initial-bundle budget fails, raise `budgets` in `angular.json` (warning 1.5MB / error 2MB).

```bash
git add client
git commit -m "feat(client): angular workspace with primeng/transloco, jwt auth flow, login page, admin shell"
```

---

### Task 10: Docker — Dockerfile, docker-compose.yml, .env.example

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

## Manual Verification (acceptance criteria)

Dev mode:
1. `docker run -d --name dl-postgres -p 5432:5432 -e POSTGRES_DB=drivinglessons -e POSTGRES_USER=app -e POSTGRES_PASSWORD=devpassword postgres:17` (skip if already running from Task 6).
2. `dotnet run --project src/DrivingLessons.Presentation.Web` — log shows migration applied; `admin_users` has exactly one row.
3. `cd client && npx ng serve` → open `http://localhost:4200`.
4. **Guard:** deep-link to `http://localhost:4200/` with empty localStorage → redirected to `/login`.
5. **Wrong password:** valid email + wrong password → `auth.invalidCredentials` message; network shows 401 with no field-level detail; still on `/login`.
6. **Right credentials:** `admin@local.dev` / `DevAdmin#2026` → navigated to `/` admin shell; JWT in localStorage. ✅ acceptance criterion.
7. **RTL/i18n:** loads in Hebrew with `dir="rtl"`; toggle → English/LTR; reload preserves choice; layout mirror-correct both ways.
8. **Refresh persistence:** F5 on `/` stays in the admin area.
9. **Logout:** → back at `/login`; deep-link to `/` → redirected again.
10. **No token via API:** `curl http://localhost:5080/api/anything` → 401.

Production mode:
11. `cp .env.example .env`, fill values, `docker compose up --build` → `http://localhost:8080` serves the login page from Kestrel; repeat steps 4–9.
12. Deep-link `http://localhost:8080/` and `/login` directly — SPA fallback serves index.html, Angular router takes over.

## Risks / Gotchas

- **PrimeNG major must match Angular major.** If PrimeNG lags the newest Angular, scaffold with the previous Angular major (`npx @angular/cli@<n> new`).
- **`MapFallbackToFile(...).AllowAnonymous()` is load-bearing** — the authorize-by-default fallback policy applies to the SPA fallback endpoint; without it the login page itself 401s.
- **Unknown `/api/*` URLs return index.html (200)** via SPA fallback instead of 404 — acceptable for v1.
- **`Database.MigrateAsync()` on startup** is fine for a single instance; unsafe with replicas — revisit if scaling out.
- **`dotnet ef migrations add` executes Program.cs** at design time — keep `appsettings.Development.json` populated so the connection string parses.
- **No CORS anywhere:** dev uses the ng serve proxy (same-origin from the browser's view); prod is single-origin.
- **RTL:** PrimeNG styled mode respects `document.documentElement.dir`; use only logical CSS (`text-align: start`, `margin-inline`) in custom styles.
- **Zoneless + signals:** keep Transloco `reRenderOnLangChange: true` so the pipe re-renders on toggle.
