# Task 3 of 11: Persistence — converters, `SubmissionConfiguration`, repositories, `AddSubmissions` migration

> Part of [US-33…US-41: First Submission](README.md). Requires task 2 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Repositories\ISubmissionRepository.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs` (+ `GetActiveByNationalIdAsync`)
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\SubmissionIdConverter.cs`, `SlotRequestIdConverter.cs`, `TargetSessionCountConverter.cs`, `SlotConstraintConverter.cs`, `RankConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\SubmissionConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\SubmissionRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (+ `Submissions`)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Generated: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\<timestamp>_AddSubmissions.cs` (+ `.Designer.cs`, updated `DrivingLessonsDbContextModelSnapshot.cs`)

**Interfaces:**
- Consumes (task 2): `Submission` (backing field `slotRequests`), `SlotRequest`; (task 1): `SubmissionId`, `SlotRequestId`, `TargetSessionCount`, `SlotConstraint` (`MaxLength`), `Rank`, `SessionType`.
- Produces (task 4 relies on these exact names):
  - `ISubmissionRepository { Task<Submission?> GetByPublicationAndStudentAsync(PublicationId publicationId, StudentId studentId); void Add(Submission submission); }` — returns `null`, never throws (ddd-architecture repository rule).
  - `IStudentRepository.GetActiveByNationalIdAsync(NationalId nationalId) : Task<Student?>` — **active** students only, `null` otherwise. A student removed from the roster by a later upload is "not on file" for the submission commands exactly as for identify (slice-2 decision 4), so a student deactivated between identify and submit gets the same 404 and the client's "contact your school" message (**Review Focus 6**). Task 2's `SubmissionStudentMustBeActiveException` stays as the aggregate's own invariant (defense in depth).
  - `DrivingLessonsDbContext.Submissions` (task 6's queries read it).
  - Tables `submissions` and `slot_requests` (below).

Precedents to open before coding: `EntityConfigurations\WeekScheduleConfiguration.cs` (owned collection with a typed key and a shadow FK), `EntityConfigurations\StudentConfiguration.cs` (nullable single-value converter, unique index), `EntityConfigurations\PublicationConfiguration.cs` (`timestamptz`), `Converters\StudentNameConverter.cs`, `Repositories\WeekScheduleRepository.cs`. The roster module's [task 7](../../roster/us-49-roster-plan/task-07-infrastructure-persistence.md) is the migration-step precedent.

**Target schema:**

```
submissions                                   slot_requests
───────────────────────────────────           ─────────────────────────────────────────
id                 uuid  PK                   id               uuid  PK
publication_id     uuid        ┐ unique       submission_id    uuid  FK → submissions.id (cascade)
student_id         uuid        ┘ together     slot_id          uuid
week_schedule_id   uuid  indexed              session_type     integer   (10 Single, 20 Double)
target_count       integer                    constraint_text  varchar(200) null
submitted_at_utc   timestamptz                rank             integer
revised_at_utc     timestamptz null
```

- `(publication_id, student_id)` **unique** — one submission per student per week (requirements §5.6), and the database's last word when two POSTs race (README open item 1). It also serves the repository lookup.
- `week_schedule_id` indexed — task 6's per-teacher dashboard counts join through it.
- No foreign keys to `publications`, `students`, `week_schedules` or `slots`: aggregates reference each other by id only, the same as `week_schedules.teacher_id` and `students.teacher_id`.
- The constraint column is `constraint_text` because `constraint` is a reserved word in PostgreSQL.

- [ ] **Step 1: Repository interfaces**

`src\DrivingLessons.Domain\Repositories\ISubmissionRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ISubmissionRepository
{
    Task<Submission?> GetByPublicationAndStudentAsync(PublicationId publicationId, StudentId studentId);

    void Add(Submission submission);
}
```

Replace `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs` with:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId);

    void Add(Student student);
}
```

- [ ] **Step 2: Converters**

`...\Converters\SubmissionIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SubmissionIdConverter : ValueConverter<SubmissionId, Guid>
{
    public SubmissionIdConverter()
        : base(
            id => id.Value,
            value => SubmissionId.Of(value))
    {
    }
}
```

`...\Converters\SlotRequestIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SlotRequestIdConverter : ValueConverter<SlotRequestId, Guid>
{
    public SlotRequestIdConverter()
        : base(
            id => id.Value,
            value => SlotRequestId.Of(value))
    {
    }
}
```

`...\Converters\TargetSessionCountConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class TargetSessionCountConverter : ValueConverter<TargetSessionCount, int>
{
    public TargetSessionCountConverter()
        : base(
            targetCount => targetCount.Value,
            value => TargetSessionCount.Of(value))
    {
    }
}
```

`...\Converters\SlotConstraintConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SlotConstraintConverter : ValueConverter<SlotConstraint, string>
{
    public SlotConstraintConverter()
        : base(
            constraint => constraint.Value,
            value => SlotConstraint.Of(value))
    {
    }
}
```

`...\Converters\RankConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RankConverter : ValueConverter<Rank, int>
{
    public RankConverter()
        : base(
            rank => rank.Value,
            value => Rank.Of(value))
    {
    }
}
```

The nullable `SlotConstraint?` column needs no special converter: EF never calls a converter for `null` (same as `AddressConverter` for `Student.Address`).

- [ ] **Step 3: `SubmissionConfiguration`**

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\SubmissionConfiguration.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<SubmissionIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.PublicationId)
            .HasColumnName("publication_id")
            .HasConversion<PublicationIdConverter>();

        builder
            .Property(x => x.StudentId)
            .HasColumnName("student_id")
            .HasConversion<StudentIdConverter>();

        builder
            .HasIndex(x => new { x.PublicationId, x.StudentId })
            .IsUnique();

        builder
            .Property(x => x.WeekScheduleId)
            .HasColumnName("week_schedule_id")
            .HasConversion<WeekScheduleIdConverter>();

        builder.HasIndex(x => x.WeekScheduleId);

        builder
            .Property(x => x.TargetCount)
            .HasColumnName("target_count")
            .HasConversion<TargetSessionCountConverter>();

        builder
            .Property(x => x.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.RevisedAtUtc)
            .HasColumnName("revised_at_utc")
            .HasColumnType("timestamptz");

        builder.OwnsMany(
            x => x.SlotRequests,
            requests =>
            {
                requests.ToTable("slot_requests");

                requests
                    .Property(r => r.Id)
                    .HasColumnName("id")
                    .HasConversion<SlotRequestIdConverter>()
                    .ValueGeneratedNever();

                requests.HasKey(r => r.Id);

                requests
                    .Property<SubmissionId>("submission_id")
                    .HasConversion<SubmissionIdConverter>()
                    .IsRequired();

                requests
                    .WithOwner()
                    .HasForeignKey("submission_id");

                requests
                    .Property(r => r.SlotId)
                    .HasColumnName("slot_id")
                    .HasConversion<SlotIdConverter>();

                requests
                    .Property(r => r.SessionType)
                    .HasColumnName("session_type");

                requests
                    .Property(r => r.Constraint)
                    .HasColumnName("constraint_text")
                    .HasConversion<SlotConstraintConverter>()
                    .HasMaxLength(SlotConstraint.MaxLength);

                requests
                    .Property(r => r.Rank)
                    .HasColumnName("rank")
                    .HasConversion<RankConverter>();
            });

        builder
            .Navigation(x => x.SlotRequests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

Notes for the implementer:
- `ValueGeneratedNever()` on the slot-request key: `Revise` (task 2) clears the list and adds requests with fresh `SlotRequestId.New()` keys to an **already tracked** submission. EF must treat those as inserts; with a client-set key and no value generation it does. (The main key needs nothing extra — a new `Submission` is always `Add`ed.) If the task-5 PUT smoke ever answers 500 with a `DbUpdateConcurrencyException`, this line is the first thing to check.
- `HasMaxLength(SlotConstraint.MaxLength)` reads the domain constant, so the column and the rule can never drift apart (**Review Focus 7**).
- Enums are stored as their numeric value (ddd-architecture EF rules) — `session_type` holds `10`/`20`.

- [ ] **Step 4: Repositories**

`src\DrivingLessons.Infrastructure\EntityFramework\Repositories\SubmissionRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public SubmissionRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Submission?> GetByPublicationAndStudentAsync(PublicationId publicationId, StudentId studentId)
    {
        return await dbContext
                         .Submissions
                         .FirstOrDefaultAsync(x => x.PublicationId == publicationId && x.StudentId == studentId);
    }

    public void Add(Submission submission)
    {
        dbContext.Submissions.Add(submission);
    }
}
```

Owned collections load with their owner, so `SlotRequests` is populated without an `Include` (same as `WeekSchedule.Slots`).

In `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`, add the using `using DrivingLessons.Domain.Values;` after `using DrivingLessons.Domain.Repositories;`, and add this method after `FindAllAsync()`:

```csharp

    public async Task<Student?> GetActiveByNationalIdAsync(NationalId nationalId)
    {
        return await dbContext
                         .Students
                         .FirstOrDefaultAsync(x => x.NationalId == nationalId && x.IsActive);
    }
```

`x.NationalId == nationalId` compares through `NationalIdConverter`, exactly like slice 2's `StudentQueries.GetActiveByNationalIdAsync`; `national_id` already has a unique index. `IsActive` in the predicate is the same "the roster is the active students" reading as identify (slice-2 decision 4) and as `ITeacherRepository.FindActiveAsync` / `ICarRepository.FindActiveAsync`.

- [ ] **Step 5: DbContext and DI**

In `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`, add after the `RosterImports` property (keep the blank line between properties):

```csharp

    public DbSet<Submission> Submissions => Set<Submission>();
```

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, add after `services.AddScoped<ISubmissionQueries, SubmissionQueries>();`:

```csharp
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
```

- [ ] **Step 6: Build, then generate the migration**

Run: `dotnet build`
Expected: success, no new warnings.

Run:
```bash
dotnet ef migrations add AddSubmissions --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```
Expected: `Done.` and three changed files under `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\` (`<timestamp>_AddSubmissions.cs`, its `.Designer.cs`, and the updated `DrivingLessonsDbContextModelSnapshot.cs`). `dotnet ef` is the local tool pinned in `.config\dotnet-tools.json` (run `dotnet tool restore` once if the command is not found); it runs `Program.cs` at design time, so `appsettings.Development.json` must keep its connection string — no database is contacted.

Inspect `<timestamp>_AddSubmissions.cs` and confirm, against the target schema above:
- `CreateTable("submissions")` with `id`, `publication_id`, `student_id`, `week_schedule_id`, `target_count` (integer), `submitted_at_utc` (timestamptz, not null), `revised_at_utc` (timestamptz, nullable).
- `CreateTable("slot_requests")` with `id`, `submission_id` (FK to `submissions.id`, `onDelete: ReferentialAction.Cascade`), `slot_id`, `session_type` (integer), `constraint_text` (`character varying(200)`, nullable), `rank` (integer).
- Indexes: **unique** `IX_submissions_publication_id_student_id`, `IX_submissions_week_schedule_id`, `IX_slot_requests_submission_id`.
- `Down` drops `slot_requests` before `submissions`.
- **Nothing else** — no change to any existing table (if the migration touches `students`, `publications` or `week_schedules`, the model has drifted: stop and investigate before continuing).

Then:

```bash
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
dotnet ef migrations script AddStudentsAndRosterImports AddSubmissions --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: `No changes have been made to the model since the last migration.`, then a SQL script containing exactly the two `CREATE TABLE` statements, the three `CREATE … INDEX` statements and the `__EFMigrationsHistory` insert. The migration is first **applied** to a real (throwaway) database in task 5, when the API starts against `drivinglessons_us33_smoke`; startup applies pending migrations.

- [ ] **Step 7: Run all tests**

Run: `dotnet test`
Expected: every test PASS (nothing here is unit-tested — it is exercised end to end by the task-5 smoke; the test run proves the new interface member did not break `DrivingLessons.Application.Test`, which references Infrastructure).

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Domain/Repositories src/DrivingLessons.Infrastructure
git commit -m "feat(infrastructure): persist submissions and their slot requests

submissions (one per student per publication, unique) and slot_requests
(rank, session type, optional 200-character constraint) with the
AddSubmissions migration; students are now loadable by national ID."
```

---

**Next:** [task-04-submission-commands.md](task-04-submission-commands.md)
