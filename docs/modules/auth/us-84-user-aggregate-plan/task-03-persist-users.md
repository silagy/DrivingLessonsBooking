# Task 3 of 6: Persist Users

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Requires tasks 1 and 2 committed. Work on branch `84-user-aggregate-sign-in`.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\UserIdConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\UserNameConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\PasswordHashConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\SecurityStampConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\UserConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\UserRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (add the `Users` set after `AdminUsers`)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (register `IUserRepository` after `IAdminAccountGateway`)
- Create (generated): `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_AddUsers.cs`, `{timestamp}_AddUsers.Designer.cs`; Modify (generated): `DrivingLessonsDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: from task 2, `User` and `IUserRepository` (`GetByEmailAsync(Email)`, `AnyExistAsync()`, `Add(User)`); from task 1, `UserId`, `UserName`, `PasswordHash`, `SecurityStamp`. Existing `EmailConverter`, `TeacherIdConverter`.
- Produces:
  - Table `users` with columns `id uuid PK`, `name text`, `email varchar(320) UNIQUE`, `password_hash text`, `role integer`, `teacher_id uuid NULL`, `security_stamp text`, `is_deleted boolean`. Task 5's data migration writes these exact column names.
  - `DrivingLessonsDbContext.Users` (`DbSet<User>`)
  - `UserRepository : IUserRepository`, registered scoped

**Why:** Task 5 switches login to Users, and the app can only start then if `IUserRepository` resolves and the `users` table exists. `admin_users` stays untouched here. Task 5 copies its row and drops it in one migration (README decision 5).

There are no repository tests in this codebase. This task is proven by the build, the no-pending-model-changes check, and the table shape in the real database (Step 6).

- [ ] **Step 1: Write the converters**

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\UserIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class UserIdConverter : ValueConverter<UserId, Guid>
{
    public UserIdConverter()
        : base(
            id => id.Value,
            value => UserId.Of(value))
    {
    }
}
```

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\UserNameConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class UserNameConverter : ValueConverter<UserName, string>
{
    public UserNameConverter()
        : base(
            name => name.Value,
            value => UserName.Of(value))
    {
    }
}
```

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\PasswordHashConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PasswordHashConverter : ValueConverter<PasswordHash, string>
{
    public PasswordHashConverter()
        : base(
            hash => hash.Value,
            value => PasswordHash.Of(value))
    {
    }
}
```

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\SecurityStampConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SecurityStampConverter : ValueConverter<SecurityStamp, string>
{
    public SecurityStampConverter()
        : base(
            stamp => stamp.Value,
            value => SecurityStamp.Of(value))
    {
    }
}
```

- [ ] **Step 2: Write the entity configuration**

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\UserConfiguration.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const int EmailMaxLength = 320;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<UserIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<UserNameConverter>();

        builder
            .Property(x => x.SignInEmail)
            .HasColumnName("email")
            .HasMaxLength(EmailMaxLength)
            .HasConversion<EmailConverter>();

        builder
            .HasIndex(x => x.SignInEmail)
            .IsUnique();

        builder
            .Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion<PasswordHashConverter>();

        builder
            .Property(x => x.Role)
            .HasColumnName("role");

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasConversion<SecurityStampConverter>();

        builder
            .Property(x => x.IsDeleted)
            .HasColumnName("is_deleted");
    }
}
```

No `HasQueryFilter` (README decision 9) and no foreign key on `teacher_id` (README decision 8).

- [ ] **Step 3: Write the repository**

`src\DrivingLessons.Infrastructure\EntityFramework\Repositories\UserRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public UserRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<User?> GetByEmailAsync(DrivingLessons.Domain.Values.Email signInEmail)
    {
        return await dbContext
                         .Users
                         .FirstOrDefaultAsync(x => x.SignInEmail == signInEmail);
    }

    public async Task<bool> AnyExistAsync()
    {
        return await dbContext.Users.AnyAsync();
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}
```

`Email` is fully qualified on purpose. Inside `DrivingLessons.Infrastructure.*`, the bare name `Email` resolves to the `DrivingLessons.Infrastructure.Email` namespace first, which is why `EmailConverter` qualifies it too.

- [ ] **Step 4: Register the set and the repository**

In `DrivingLessonsDbContext.cs`, add after the `AdminUsers` line:

```csharp
    public DbSet<User> Users => Set<User>();
```

`DrivingLessons.Domain.Entities` is already imported there.

In `DependencyInjection.cs`, add after `services.AddScoped<IAdminAccountGateway, AdminAccountGateway>();`:

```csharp
        services.AddScoped<IUserRepository, UserRepository>();
```

`DrivingLessons.Domain.Repositories` and `DrivingLessons.Infrastructure.EntityFramework.Repositories` are already imported there.

Run: `dotnet build`
Expected: build succeeds with no new warnings.

- [ ] **Step 5: Generate the migration and check its shape**

```bash
dotnet ef migrations add AddUsers --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web --output-dir EntityFramework\Migrations
```

Open the generated `{timestamp}_AddUsers.cs`. `Up` must contain exactly one `CreateTable(name: "users", ...)` with these columns, plus one unique index. Nothing may touch `admin_users` or any other table:

```csharp
id = table.Column<Guid>(type: "uuid", nullable: false),
name = table.Column<string>(type: "text", nullable: false),
email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
password_hash = table.Column<string>(type: "text", nullable: false),
role = table.Column<int>(type: "integer", nullable: false),
teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
security_stamp = table.Column<string>(type: "text", nullable: false),
is_deleted = table.Column<bool>(type: "boolean", nullable: false)
```

```csharp
migrationBuilder.CreateIndex(
    name: "IX_users_email",
    table: "users",
    column: "email",
    unique: true);
```

`Down` must be a single `DropTable(name: "users")`. If the generated file differs (another table touched, `teacher_id` not nullable, a missing column), fix the configuration in Step 2, delete the migration with `dotnet ef migrations remove --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web`, and generate it again. Don't hand-edit this migration.

Then confirm the model and the snapshot agree:

```bash
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 6: Apply the migration to the local database and inspect the table**

Use the compose Postgres (see README). Then:

```bash
dotnet ef database update --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons -c "\d users"
```

Expected: the eight columns from Step 5, `"PK_users" PRIMARY KEY, btree (id)` and `"IX_users_email" UNIQUE, btree (email)`. `admin_users` still exists (`\dt admin_users` lists it).

- [ ] **Step 7: Run every test**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: every test PASS.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/Converters/UserIdConverter.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/Converters/UserNameConverter.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/Converters/PasswordHashConverter.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/Converters/SecurityStampConverter.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/UserConfiguration.cs src/DrivingLessons.Infrastructure/EntityFramework/Repositories/UserRepository.cs src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs src/DrivingLessons.Infrastructure/DependencyInjection.cs src/DrivingLessons.Infrastructure/EntityFramework/Migrations
git commit -m "feat(infrastructure): persist Users in a users table (#84)"
```

End the commit message with the attribution trailer from the session's instructions.
