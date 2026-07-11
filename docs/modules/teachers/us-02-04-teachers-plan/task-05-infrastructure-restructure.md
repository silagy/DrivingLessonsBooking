# Task 5 of 12: Infrastructure restructure — DbContext rename/move only

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–4 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** Bring US-01's persistence layer to the rules-compliant shape **without any behavior or model change**: rename `AppDbContext` → `DrivingLessonsDbContext`, move `Persistence\` → `EntityFramework\`, implement `IUnitOfWork`, switch to `ApplyConfigurationsFromAssembly` with an `AdminUserConfiguration` class, and fix the existing migration's namespaces. Isolating this rename in its own commit keeps the diff reviewable and lets `dotnet ef migrations has-pending-model-changes` prove zero model drift.

**CRITICAL:** migration IDs and migration class names must NOT change — `__EFMigrationsHistory` in existing databases stores `20260613173834_InitialCreate` and must keep matching. Only namespaces, the `[DbContext(...)]` attributes, and the snapshot class name change.

---

**Files:**
- Rename dir: `src/DrivingLessons.Infrastructure/Persistence/` → `src/DrivingLessons.Infrastructure/EntityFramework/`
- Rename: `AppDbContext.cs` → `EntityFramework/DrivingLessonsDbContext.cs`
- Rename: `Migrations/AppDbContextModelSnapshot.cs` → `Migrations/DrivingLessonsDbContextModelSnapshot.cs`
- Create: `src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/AdminUserConfiguration.cs`
- Modify: `Migrations/20260613173834_InitialCreate.cs`, `Migrations/20260613173834_InitialCreate.Designer.cs`
- Modify: `src/DrivingLessons.Infrastructure/DependencyInjection.cs`, `Auth/AdminAccountGateway.cs`, `Auth/AdminSeeder.cs`
- Modify: `src/DrivingLessons.Presentation.Web/Program.cs`

- [ ] **Step 1: Move the folder with git so history follows**

```bash
git mv src/DrivingLessons.Infrastructure/Persistence src/DrivingLessons.Infrastructure/EntityFramework
git mv src/DrivingLessons.Infrastructure/EntityFramework/AppDbContext.cs src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs
git mv src/DrivingLessons.Infrastructure/EntityFramework/Migrations/AppDbContextModelSnapshot.cs src/DrivingLessons.Infrastructure/EntityFramework/Migrations/DrivingLessonsDbContextModelSnapshot.cs
```

- [ ] **Step 2: `src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs` (full replacement)**

`CommitAsync` is `SaveChangesAsync` only — no event dispatch until the first real handler exists (locked decision). Note the class is no longer `sealed` and mapping moves to configuration classes.

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options)
        : base(options)
    {
    }

    public async Task CommitAsync()
    {
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
```

- [ ] **Step 3: `src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/AdminUserConfiguration.cs`**

Byte-identical model to the old inline mapping — change nothing but the location.

```csharp
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired();
    }
}
```

- [ ] **Step 4: Fix the migration files (namespace + context references ONLY)**

`Migrations/20260613173834_InitialCreate.cs` — change the namespace line only:

```csharp
namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
```

`Migrations/20260613173834_InitialCreate.Designer.cs` — three changes: the `using`, the namespace, the attribute:

```csharp
using DrivingLessons.Infrastructure.EntityFramework;
```

```csharp
namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
```

```csharp
    [DbContext(typeof(DrivingLessonsDbContext))]
    [Migration("20260613173834_InitialCreate")]
    partial class InitialCreate
```

`Migrations/DrivingLessonsDbContextModelSnapshot.cs` — four changes: the `using`, the namespace, the attribute, the class name:

```csharp
using DrivingLessons.Infrastructure.EntityFramework;
```

```csharp
namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
```

```csharp
    [DbContext(typeof(DrivingLessonsDbContext))]
    partial class DrivingLessonsDbContextModelSnapshot : ModelSnapshot
```

The `BuildTargetModel`/`BuildModel` bodies stay untouched.

- [ ] **Step 5: `src/DrivingLessons.Infrastructure/DependencyInjection.cs` (full replacement)**

Registers the context under its new name and exposes it as `IUnitOfWork`.

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DrivingLessonsDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DrivingLessonsDbContext>());

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

- [ ] **Step 6: Update the two auth-slice consumers**

`src/DrivingLessons.Infrastructure/Auth/AdminAccountGateway.cs` — replace the `using DrivingLessons.Infrastructure.Persistence;` line with `using DrivingLessons.Infrastructure.EntityFramework;` and the constructor/field type `AppDbContext` with `DrivingLessonsDbContext`.

`src/DrivingLessons.Infrastructure/Auth/AdminSeeder.cs` — same two changes: the `using` and the `SeedAsync` parameter type:

```csharp
public static async Task SeedAsync(DrivingLessonsDbContext dbContext, AdminOptions options, CancellationToken cancellationToken = default)
```

- [ ] **Step 7: `src/DrivingLessons.Presentation.Web/Program.cs` — two changes**

Replace the `using DrivingLessons.Infrastructure.Persistence;` line with:

```csharp
using DrivingLessons.Infrastructure.EntityFramework;
```

Replace the startup-scope resolution:

```csharp
    var db = scope.ServiceProvider.GetRequiredService<DrivingLessonsDbContext>();
```

- [ ] **Step 8: Verify — build, migration list, zero drift**

```
dotnet build
dotnet tool restore
dotnet ef migrations list --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Expected: build succeeds; the list shows exactly `20260613173834_InitialCreate`; the drift check reports **no pending model changes** (proves the `AdminUserConfiguration` move is byte-identical). If it reports changes, diff the snapshot against the configuration before proceeding — do not add a migration.

- [ ] **Step 9: Commit**

```bash
git add -A src
git commit -m "refactor(infra): rename AppDbContext to DrivingLessonsDbContext under EntityFramework"
```

---

**Next:** [task-06-teachers-persistence.md](task-06-teachers-persistence.md)
