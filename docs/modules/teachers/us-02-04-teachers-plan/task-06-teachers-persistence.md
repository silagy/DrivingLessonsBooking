# Task 6 of 12: Teachers persistence — converters, configuration, repository, queries, migration

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–5 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** EF mapping for the Teacher aggregate (`teachers` + `cars` tables, snake_case, typed-ID/value-object converters, `OwnsMany` with shadow FK), `TeacherRepository`, `TeacherQueries`, DI registrations, and the `AddTeachers` migration.

**Gotchas baked in:** explicit `HasKey(c => c.Id)` on the owned collection (EF otherwise keys it as ownerFK + synthetic id) · shadow FK named `"teacher_id"` (never a CLR property on `Car`) · `UsePropertyAccessMode(PropertyAccessMode.Field)` so the `cars` backing field is used for materialization · `FindAsync` loads owned collections automatically.

---

**Files:**
- Create: `src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/Converters/TeacherIdConverter.cs`, `CarIdConverter.cs`, `TeacherNameConverter.cs`, `EmailConverter.cs`, `CarNameConverter.cs`, `CarTypeConverter.cs`
- Create: `src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/TeacherConfiguration.cs`
- Create: `src/DrivingLessons.Infrastructure/EntityFramework/Repositories/TeacherRepository.cs`
- Create: `src/DrivingLessons.Infrastructure/EntityFramework/Queries/TeacherQueries.cs`
- Modify: `src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs` (add `DbSet<Teacher>`)
- Modify: `src/DrivingLessons.Infrastructure/DependencyInjection.cs` (two registrations)
- Create (generated): `EntityFramework/Migrations/*_AddTeachers.*`

- [x] **Step 1: The six converters — one file each in `EntityConfigurations/Converters/`**

`TeacherIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherIdConverter : ValueConverter<TeacherId, Guid>
{
    public TeacherIdConverter()
        : base(
            id => id.Value,
            value => TeacherId.Of(value))
    {
    }
}
```

`CarIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarIdConverter : ValueConverter<CarId, Guid>
{
    public CarIdConverter()
        : base(
            id => id.Value,
            value => CarId.Of(value))
    {
    }
}
```

`TeacherNameConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherNameConverter : ValueConverter<TeacherName, string>
{
    public TeacherNameConverter()
        : base(
            name => name.Value,
            value => TeacherName.Of(value))
    {
    }
}
```

`EmailConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(
            email => email.Value,
            value => Email.Of(value))
    {
    }
}
```

`CarNameConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarNameConverter : ValueConverter<CarName, string>
{
    public CarNameConverter()
        : base(
            name => name.Value,
            value => CarName.Of(value))
    {
    }
}
```

`CarTypeConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class CarTypeConverter : ValueConverter<CarType, string>
{
    public CarTypeConverter()
        : base(
            type => type.Value,
            value => CarType.Of(value))
    {
    }
}
```

- [x] **Step 2: `EntityConfigurations/TeacherConfiguration.cs`**

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<TeacherIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<TeacherNameConverter>();

        builder
            .Property(x => x.ContactEmail)
            .HasColumnName("contact_email")
            .HasConversion<EmailConverter>();

        builder.OwnsMany(
            x => x.Cars,
            cars =>
            {
                cars.ToTable("cars");

                cars
                    .Property(c => c.Id)
                    .HasColumnName("id")
                    .HasConversion<CarIdConverter>();

                cars.HasKey(c => c.Id);

                cars.WithOwner().HasForeignKey("teacher_id");

                cars
                    .Property(c => c.Name)
                    .HasColumnName("name")
                    .HasConversion<CarNameConverter>();

                cars
                    .Property(c => c.Type)
                    .HasColumnName("type")
                    .HasConversion<CarTypeConverter>();

                cars
                    .Property(c => c.Transmission)
                    .HasColumnName("transmission");
            });

        builder
            .Navigation(x => x.Cars)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

- [x] **Step 3: Add the DbSet — `EntityFramework/DrivingLessonsDbContext.cs`**

Add below the existing `AdminUsers` property (plus `using DrivingLessons.Domain.Entities;`):

```csharp
    public DbSet<Teacher> Teachers => Set<Teacher>();
```

- [x] **Step 4: `EntityFramework/Repositories/TeacherRepository.cs`**

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public TeacherRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Teacher?> GetAsync(TeacherId id)
    {
        return await dbContext.Teachers.FindAsync(id);
    }

    public void Add(Teacher teacher)
    {
        dbContext.Teachers.Add(teacher);
    }
}
```

- [x] **Step 5: `EntityFramework/Queries/TeacherQueries.cs`**

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class TeacherQueries : ITeacherQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public TeacherQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetTeacherResponse?> GetAsync(Guid id)
    {
        var teacherId = TeacherId.Of(id);

        return await dbContext
                         .Teachers
                         .Where(x => x.Id == teacherId)
                         .Select(GetTeacherResponse.Selector)
                         .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync()
    {
        return await dbContext
                         .Teachers
                         .Select(ItemForFindTeachersResponse.Selector)
                         .ToListAsync();
    }
}
```

- [x] **Step 6: Register — `src/DrivingLessons.Infrastructure/DependencyInjection.cs`**

Add two `using` lines and two registrations next to the existing gateway registration:

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Infrastructure.EntityFramework.Queries;
using DrivingLessons.Infrastructure.EntityFramework.Repositories;
```

```csharp
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITeacherQueries, TeacherQueries>();
```

- [x] **Step 7: Generate and review the migration**

```
dotnet build
dotnet ef migrations add AddTeachers --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
dotnet ef migrations script --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Review the generated migration and script. Expected: two new tables — `teachers` (`id uuid` PK, `name text`, `contact_email text`) and `cars` (`id uuid` PK, `teacher_id uuid` FK → teachers, cascade delete, index on `teacher_id`, `name text`, `type text`, `transmission integer`). No changes to `admin_users`. If `admin_users` appears in the diff, task 5 left drift — stop and fix there first.

- [x] **Step 8: Verify + commit**

Run: `dotnet test` — expected: all tests PASS.

```bash
git add src
git commit -m "feat(infra): teacher persistence with owned cars and AddTeachers migration"
```

---

**Next:** [task-07-controllers.md](task-07-controllers.md)
