# Task 4 of 10: Infrastructure — persistence, queries, migration

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–3 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\WeekScheduleIdConverter.cs`, `SlotIdConverter.cs`, `WeekStartConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\WeekScheduleConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\WeekScheduleRepository.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\WeekScheduleQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`, `src\DrivingLessons.Infrastructure\DependencyInjection.cs`

Open `TeacherIdConverter.cs`, `TeacherConfiguration.cs`, `TeacherRepository.cs`, `TeacherQueries.cs` first and mirror them exactly (namespaces, base-call style).

- [ ] **Step 1: Converters**

`Converters\WeekScheduleIdConverter.cs` (mirror `TeacherIdConverter.cs`):

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class WeekScheduleIdConverter : ValueConverter<WeekScheduleId, Guid>
{
    public WeekScheduleIdConverter()
        : base(
            id => id.Value,
            value => WeekScheduleId.Of(value))
    {
    }
}
```

`Converters\SlotIdConverter.cs` — same shape for `SlotId`.

`Converters\WeekStartConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class WeekStartConverter : ValueConverter<WeekStart, DateOnly>
{
    public WeekStartConverter()
        : base(
            weekStart => weekStart.Value,
            value => WeekStart.Of(value))
    {
    }
}
```

- [ ] **Step 2: Entity configuration**

`EntityConfigurations\WeekScheduleConfiguration.cs` (mirror `TeacherConfiguration.cs`'s `OwnsMany` + shadow-FK + field-access pattern; snake_case everywhere; enums stored as their numeric value — no explicit conversion needed):

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class WeekScheduleConfiguration : IEntityTypeConfiguration<WeekSchedule>
{
    public void Configure(EntityTypeBuilder<WeekSchedule> builder)
    {
        builder.ToTable("week_schedules");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<WeekScheduleIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.WeekStart)
            .HasColumnName("week_start")
            .HasConversion<WeekStartConverter>();

        builder
            .HasIndex(x => new { x.TeacherId, x.WeekStart })
            .IsUnique();

        builder.OwnsMany(
            x => x.Slots,
            slots =>
            {
                slots.ToTable("slots");

                slots
                    .Property(s => s.Id)
                    .HasColumnName("id")
                    .HasConversion<SlotIdConverter>();

                slots.HasKey(s => s.Id);

                slots
                    .Property<WeekScheduleId>("week_schedule_id")
                    .HasConversion<WeekScheduleIdConverter>()
                    .IsRequired();

                slots
                    .WithOwner()
                    .HasForeignKey("week_schedule_id");

                slots
                    .Property(s => s.Day)
                    .HasColumnName("day");

                slots
                    .Property(s => s.Window)
                    .HasColumnName("window");

                slots
                    .Property(s => s.State)
                    .HasColumnName("state");
            });

        builder
            .Navigation(x => x.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

- [ ] **Step 3: Repository and queries**

`Repositories\WeekScheduleRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class WeekScheduleRepository : IWeekScheduleRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public WeekScheduleRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<WeekSchedule?> GetAsync(WeekScheduleId id)
    {
        return await dbContext.WeekSchedules.FindAsync(id);
    }

    public async Task<WeekSchedule?> GetByTeacherAndWeekAsync(TeacherId teacherId, WeekStart weekStart)
    {
        return await dbContext
                         .WeekSchedules
                         .FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.WeekStart == weekStart);
    }

    public void Add(WeekSchedule weekSchedule)
    {
        dbContext.WeekSchedules.Add(weekSchedule);
    }
}
```

`Queries\WeekScheduleQueries.cs`:

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class WeekScheduleQueries : IWeekScheduleQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public WeekScheduleQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetWeekScheduleResponse?> GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .WeekSchedules
                         .Where(x => x.TeacherId == resolvedTeacherId && x.WeekStart == resolvedWeekStart)
                         .Select(GetWeekScheduleResponse.Selector)
                         .FirstOrDefaultAsync();
    }
}
```

- [ ] **Step 4: DbContext + DI**

In `DrivingLessonsDbContext.cs` add next to `Teachers`:

```csharp
public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();
```

(match the existing `DbSet` declaration style — if `Teachers` uses `{ get; set; }`, follow that instead).

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs` add next to the teacher registrations:

```csharp
services.AddScoped<IWeekScheduleRepository, WeekScheduleRepository>();
services.AddScoped<IWeekScheduleQueries, WeekScheduleQueries>();
```

- [ ] **Step 5: Build, then generate the migration**

Run: `dotnet build`
Expected: success.

Run:
```bash
dotnet ef migrations add AddWeekSchedules --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```
Expected: migration created under `EntityFramework\Migrations\` with `week_schedules` table (unique index on `teacher_id, week_start`) and `slots` table (FK `week_schedule_id`, cascade delete). Inspect the generated file to confirm.

- [ ] **Step 6: Run all tests, commit**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj` — PASS.

```bash
git add src/DrivingLessons.Infrastructure
git commit -m "feat(infrastructure): week schedule persistence, queries and migration"
```

---

**Next:** [task-05-controllers.md](task-05-controllers.md)
