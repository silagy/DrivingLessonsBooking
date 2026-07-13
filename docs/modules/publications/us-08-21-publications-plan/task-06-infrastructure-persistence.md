# Task 6 of 14: Infrastructure — persistence, queries, migration

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–5 complete (domain values, `Publication` aggregate + `IPublicationRepository`, domain-event dispatch, application commands + seams, application queries + DTOs + `PublicationClosed` handler). Work on branch `9-us-08-21-publications-module`, all commands from the repo root.

This task maps the `Publication` aggregate to PostgreSQL, implements its repository and query side, stubs `ISubmissionQueries` (real impl lands with `module:student-form`), registers everything, and generates the `AddPublications` migration.

Open `Converters\WeekScheduleIdConverter.cs`, `Converters\WeekStartConverter.cs`, `Converters\TeacherIdConverter.cs`, `WeekScheduleConfiguration.cs`, `TeacherConfiguration.cs`, `WeekScheduleRepository.cs`, `WeekScheduleQueries.cs`, and the newest migration under `EntityFramework\Migrations\` first and mirror them exactly (namespaces, one-liner converters, `OwnsMany` shadow-FK + field-access pattern, snake_case columns).

**Files:**
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\PublicationIdConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\TeacherExcelVersionIdConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\ShareableLinkTokenConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\PublicationConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\PublicationRepository.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Generated: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_AddPublications.cs` (+ `.Designer.cs`, snapshot update)

---

- [ ] **Step 1: Converters**

Three one-liner `ValueConverter` subclasses, identical in shape to `WeekScheduleIdConverter`.

`Converters\PublicationIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PublicationIdConverter : ValueConverter<PublicationId, Guid>
{
    public PublicationIdConverter()
        : base(
            id => id.Value,
            value => PublicationId.Of(value))
    {
    }
}
```

`Converters\TeacherExcelVersionIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TeacherExcelVersionIdConverter : ValueConverter<TeacherExcelVersionId, Guid>
{
    public TeacherExcelVersionIdConverter()
        : base(
            id => id.Value,
            value => TeacherExcelVersionId.Of(value))
    {
    }
}
```

`Converters\ShareableLinkTokenConverter.cs` (record-with-single-string-value, converts to `string` like the token round-trips through `Of`):

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class ShareableLinkTokenConverter : ValueConverter<ShareableLinkToken, string>
{
    public ShareableLinkTokenConverter()
        : base(
            token => token.Value,
            value => ShareableLinkToken.Of(value))
    {
    }
}
```

- [ ] **Step 2: Entity configuration**

`EntityConfigurations\PublicationConfiguration.cs`. The `WeekStart` reuses the existing `WeekStartConverter` (with a **unique** index — one Publication per calendar week, decision #1); `State` is stored as its numeric enum value (no explicit conversion, matching `WeekScheduleConfiguration`'s `state` column); `LinkToken` gets a unique index; `Window` is a **nullable** `OwnsOne` (null in Draft, columns `window_start_utc`/`window_end_utc` as `timestamptz`); `TeacherVersions` is an `OwnsMany` to its own table with a shadow FK `publication_id`, mirroring `Slots`.

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<PublicationIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.WeekStart)
            .HasColumnName("week_start")
            .HasConversion<WeekStartConverter>();

        builder
            .HasIndex(x => x.WeekStart)
            .IsUnique();

        builder
            .Property(x => x.State)
            .HasColumnName("state");

        builder
            .Property(x => x.LinkToken)
            .HasColumnName("link_token")
            .HasConversion<ShareableLinkTokenConverter>();

        builder
            .HasIndex(x => x.LinkToken)
            .IsUnique();

        builder.OwnsOne(
            x => x.Window,
            window =>
            {
                window
                    .Property(w => w.StartUtc)
                    .HasColumnName("window_start_utc")
                    .HasColumnType("timestamptz");

                window
                    .Property(w => w.EndUtc)
                    .HasColumnName("window_end_utc")
                    .HasColumnType("timestamptz");
            });

        builder.OwnsMany(
            x => x.TeacherVersions,
            versions =>
            {
                versions.ToTable("publication_teacher_versions");

                versions
                    .Property(v => v.Id)
                    .HasColumnName("id")
                    .HasConversion<TeacherExcelVersionIdConverter>();

                versions.HasKey(v => v.Id);

                versions
                    .Property<PublicationId>("publication_id")
                    .HasConversion<PublicationIdConverter>()
                    .IsRequired();

                versions
                    .WithOwner()
                    .HasForeignKey("publication_id");

                versions
                    .Property(v => v.TeacherId)
                    .HasColumnName("teacher_id")
                    .HasConversion<TeacherIdConverter>();

                versions
                    .Property(v => v.Version)
                    .HasColumnName("version");
            });

        builder
            .Navigation(x => x.TeacherVersions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

Gotcha (plan risk #2): a nullable owned `SubmissionWindow` is the one EF mapping shape this repo has not used before. Because `Window` is a nullable reference, EF makes both columns nullable automatically. If constructor binding fails on the first migration run, add a private parameterless ctor to the `SubmissionWindow` record; do **not** make the columns non-nullable.

- [ ] **Step 3: Repository**

`Repositories\PublicationRepository.cs` — inject the concrete `DrivingLessonsDbContext` (no `UnitOfWork` property, per the overriding conventions); PK lookup via `FindAsync`, predicate lookups via `FirstOrDefaultAsync`, and the two due-boundary scans filter on `State` + the owned `Window` columns.

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class PublicationRepository : IPublicationRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public PublicationRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Publication?> GetAsync(PublicationId id)
    {
        return await dbContext.Publications.FindAsync(id);
    }

    public async Task<Publication?> GetByWeekAsync(WeekStart weekStart)
    {
        return await dbContext
                         .Publications
                         .FirstOrDefaultAsync(x => x.WeekStart == weekStart);
    }

    public async Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token)
    {
        return await dbContext
                         .Publications
                         .FirstOrDefaultAsync(x => x.LinkToken == token);
    }

    public async Task<IReadOnlyList<Publication>> GetPublishedDueToOpenAsync(DateTimeOffset asOfUtc)
    {
        return await dbContext
                         .Publications
                         .Where(x => x.State == PublicationState.Published && x.Window!.StartUtc <= asOfUtc)
                         .ToListAsync();
    }

    public async Task<IReadOnlyList<Publication>> GetOpenDueToCloseAsync(DateTimeOffset asOfUtc)
    {
        return await dbContext
                         .Publications
                         .Where(x => x.State == PublicationState.Open && x.Window!.EndUtc <= asOfUtc)
                         .ToListAsync();
    }

    public void Add(Publication publication)
    {
        dbContext.Publications.Add(publication);
    }
}
```

- [ ] **Step 4: Queries**

`Queries\PublicationQueries.cs`. `GetByWeekAsync` projects via `GetPublicationResponse.Selector`. `GetDashboardAsync` returns the **base** dashboard from SQL — `SlotCounts` from the teacher's WeekSchedule slots with `RequestCount = 0`, the three stat fields zero/null, `LatestExcelVersion` from that teacher's `TeacherExcelVersion`; `GetPublicationDashboardInteractor` later overlays the real counts/stats from `ISubmissionQueries` (dashboard composition rule, task-05). `FindHistoryAsync` joins `publications` × `week_schedules` (the teachers who prepared that week) × the teacher's name, carrying that teacher's latest version. `GetTeacherIdsWithScheduleForWeekAsync` reads the `week_schedules` side by `week_start` (used by `ClosePublicationInteractor` to resolve the version set).

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class PublicationQueries : IPublicationQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public PublicationQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetPublicationResponse?> GetByWeekAsync(DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .Publications
                         .Where(x => x.WeekStart == resolvedWeekStart)
                         .Select(GetPublicationResponse.Selector)
                         .FirstOrDefaultAsync();
    }

    public async Task<GetPublicationDashboardResponse?> GetDashboardAsync(Guid publicationId, Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        var header = await dbContext
                             .Publications
                             .Where(x => x.Id == resolvedPublicationId)
                             .Select(x => new
                             {
                                 x.State,
                                 WindowStartUtc = (DateTimeOffset?)x.Window!.StartUtc,
                                 WindowEndUtc = (DateTimeOffset?)x.Window!.EndUtc,
                                 LinkToken = x.LinkToken.Value,
                                 WeekStart = x.WeekStart,
                                 LatestExcelVersion = x.TeacherVersions
                                                       .Where(v => v.TeacherId == resolvedTeacherId)
                                                       .Select(v => (int?)v.Version)
                                                       .FirstOrDefault()
                             })
                             .FirstOrDefaultAsync();

        if (header is null)
        {
            return null;
        }

        var slotCounts = await dbContext
                                 .WeekSchedules
                                 .Where(x => x.TeacherId == resolvedTeacherId && x.WeekStart == header.WeekStart)
                                 .SelectMany(x => x.Slots)
                                 .OrderBy(slot => slot.Day)
                                 .ThenBy(slot => slot.Window)
                                 .Select(slot => new SlotCountForGetPublicationDashboardResponse
                                 {
                                     SlotId = slot.Id.Value,
                                     Day = slot.Day,
                                     Window = slot.Window,
                                     State = slot.State,
                                     RequestCount = 0
                                 })
                                 .ToListAsync();

        return new GetPublicationDashboardResponse
        {
            State = header.State,
            WindowStartUtc = header.WindowStartUtc,
            WindowEndUtc = header.WindowEndUtc,
            LinkToken = header.LinkToken,
            StudentsSubmitted = 0,
            TotalPicks = 0,
            LastSubmissionAtUtc = null,
            LatestExcelVersion = header.LatestExcelVersion,
            SlotCounts = slotCounts
        };
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync()
    {
        var query = from publication in dbContext.Publications
                    from weekSchedule in dbContext.WeekSchedules
                        .Where(ws => ws.WeekStart == publication.WeekStart)
                    join teacher in dbContext.Teachers
                        on weekSchedule.TeacherId equals teacher.Id
                    orderby publication.WeekStart descending, teacher.Name
                    select new ItemForFindPublicationHistoryResponse
                    {
                        PublicationId = publication.Id.Value,
                        WeekStart = publication.WeekStart.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        State = publication.State,
                        WindowStartUtc = (DateTimeOffset?)publication.Window!.StartUtc,
                        WindowEndUtc = (DateTimeOffset?)publication.Window!.EndUtc,
                        LatestExcelVersion = publication.TeacherVersions
                                                        .Where(v => v.TeacherId == teacher.Id)
                                                        .Select(v => (int?)v.Version)
                                                        .FirstOrDefault()
                    };

        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetTeacherIdsWithScheduleForWeekAsync(DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        return await dbContext
                         .WeekSchedules
                         .Where(x => x.WeekStart == resolvedWeekStart)
                         .Select(x => x.TeacherId.Value)
                         .ToListAsync();
    }
}
```

Gotcha (plan risk #5): the history join and the dashboard subquery cross value-converted `week_start`/`teacher_id`. Inspect the generated SQL when you first exercise `/history`. If the `join`/correlated `Where` will not translate, filter through the raw provider values (`EF.Property<DateOnly>(...)`) or resolve the week keys to plain `DateOnly` before the query. Ordering by `teacher.Name` orders on the converted `name` column; if that fails to translate, drop the secondary `orderby` and sort in memory after `ToListAsync`.

- [ ] **Step 5: `ISubmissionQueries` stub**

`Queries\SubmissionQueries.cs` — the infrastructure impl returns empty counts and zero stats until `module:student-form` builds the submission tables. There is no comment: the emptiness **is** the behavior, and the dashboard lights up automatically when the real implementation replaces this one.

```csharp
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class SubmissionQueries : ISubmissionQueries
{
    public Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId)
    {
        IReadOnlyDictionary<Guid, int> counts = new Dictionary<Guid, int>();

        return Task.FromResult(counts);
    }

    public Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId)
    {
        var stats = new SubmissionStats(0, 0, null);

        return Task.FromResult(stats);
    }
}
```

- [ ] **Step 6: DbContext**

In `DrivingLessonsDbContext.cs`, add the `DbSet` next to `WeekSchedules` (match the existing expression-bodied `Set<T>()` style):

```csharp
public DbSet<Publication> Publications => Set<Publication>();
```

The `Publication` type is in `DrivingLessons.Domain.Entities`, already covered by the existing `using DrivingLessons.Domain.Entities;`. Leave `CommitAsync` and its domain-event dispatch loop as delivered in task-03 — this task does not touch it.

- [ ] **Step 7: DI registration**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, add next to the week-schedule registrations:

```csharp
services.AddScoped<IPublicationRepository, PublicationRepository>();
services.AddScoped<IPublicationQueries, PublicationQueries>();
services.AddScoped<ISubmissionQueries, SubmissionQueries>();
```

`IPublicationRepository` is in `DrivingLessons.Domain.Repositories` (already imported); `IPublicationQueries`/`ISubmissionQueries` are in `DrivingLessons.Application.Queries` (already imported); the impls are in `...Infrastructure.EntityFramework.Repositories`/`...Queries` (already imported).

- [ ] **Step 8: Build**

Run: `dotnet build`

Expected: `Build succeeded.` with zero errors.

- [ ] **Step 9: Generate the migration**

Run (short-flag form of the week-schedules command):

```bash
dotnet ef migrations add AddPublications -p src\DrivingLessons.Infrastructure -s src\DrivingLessons.Presentation.Web
```

Expected: three files appear under `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\`, named with a UTC timestamp prefix:

```
{yyyyMMddHHmmss}_AddPublications.cs
{yyyyMMddHHmmss}_AddPublications.Designer.cs
```

plus an updated `DrivingLessonsDbContextModelSnapshot.cs`. (The most recent existing migration is `20260712185139_AddWeekSchedules.cs`, so the new prefix sorts after it.)

Open the generated `..._AddPublications.cs` and sanity-check that `Up` creates the two tables with the expected shape (EF generates the body — **do not hand-write it**):

```csharp
migrationBuilder.CreateTable(
    name: "publications",
    columns: table => new
    {
        id = table.Column<Guid>(type: "uuid", nullable: false),
        week_start = table.Column<DateOnly>(type: "date", nullable: false),
        state = table.Column<int>(type: "integer", nullable: false),
        link_token = table.Column<string>(type: "text", nullable: false),
        window_start_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
        window_end_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_publications", x => x.id);
    });

migrationBuilder.CreateTable(
    name: "publication_teacher_versions",
    columns: table => new
    {
        id = table.Column<Guid>(type: "uuid", nullable: false),
        teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
        version = table.Column<int>(type: "integer", nullable: false),
        publication_id = table.Column<Guid>(type: "uuid", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_publication_teacher_versions", x => x.id);
        table.ForeignKey(
            name: "FK_publication_teacher_versions_publications_publication_id",
            column: x => x.publication_id,
            principalTable: "publications",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    });
```

Confirm the two unique indexes are present:

```csharp
migrationBuilder.CreateIndex(
    name: "IX_publications_week_start",
    table: "publications",
    column: "week_start",
    unique: true);

migrationBuilder.CreateIndex(
    name: "IX_publications_link_token",
    table: "publications",
    column: "link_token",
    unique: true);
```

- [ ] **Step 10: Verify no pending model changes**

Run:

```bash
dotnet ef migrations has-pending-model-changes -p src\DrivingLessons.Infrastructure -s src\DrivingLessons.Presentation.Web
```

Expected: `No changes have been made to the model since the last migration.` (If it reports pending changes, a `HasConversion`/`OwnsOne`/`OwnsMany` line is missing — reconcile the configuration and re-add the migration after deleting the just-generated files.)

- [ ] **Step 11: Run domain tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`

Expected: all tests PASS (this task adds no domain behavior; the run confirms nothing regressed).

- [ ] **Step 12: Commit**

```bash
git add src/DrivingLessons.Infrastructure
git commit -m "feat(infra): publication persistence, queries, submission stub and migration"
```

---

**Next:** [task-07-infrastructure-services.md](task-07-infrastructure-services.md)
