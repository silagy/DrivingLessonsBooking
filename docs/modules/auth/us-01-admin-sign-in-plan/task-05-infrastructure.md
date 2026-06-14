# Task 5 of 11: Infrastructure — entity, DbContext, options, adapters, seeder

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–4 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.
>
> **Commit note:** this task ends with a build only — the commit comes after Task 6 together with the migration.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Architecture:** Modular monolith, DDD, Onion. PostgreSQL via EF Core. Admin auth is **infrastructure-level** — there is deliberately no Admin domain aggregate. Root namespace: `DrivingLessons`.

**Onion references:** `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice.

---

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

**Next:** [task-06-presentation-and-migration.md](task-06-presentation-and-migration.md)
