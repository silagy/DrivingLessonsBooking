# Task 7 of 10: Infrastructure — persistence, query implementations, migration

> Part of [US-49: Roster Module](README.md). Requires tasks 1–6 complete. Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\Converters\StudentIdConverter.cs`, `NationalIdConverter.cs`, `StudentNameConverter.cs`, `PhoneNumberConverter.cs`, `AddressConverter.cs`, `LessonsStartDateConverter.cs`, `LicenseTypeConverter.cs`, `RosterImportIdConverter.cs`, `RosterFileNameConverter.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\StudentConfiguration.cs`, `RosterImportConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs`, `RosterImportRepository.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`, `RosterImportQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`, `src\DrivingLessons.Infrastructure\DependencyInjection.cs`

Open `TeacherIdConverter.cs`, `WeekStartConverter.cs`, `TeacherConfiguration.cs`, `PublicationConfiguration.cs` (owned collections via `OwnsMany` + shadow FK + `PropertyAccessMode.Field`), `TeacherRepository.cs`, `PublicationQueries.cs` (query-syntax join in `FindHistoryAsync`), and `WeekScheduleQueries.cs` first and mirror them exactly.

- [ ] **Step 1: Converters**

All nine follow the `TeacherIdConverter.cs` / `TeacherNameConverter.cs` / `WeekStartConverter.cs` shapes.

`Converters\StudentIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class StudentIdConverter : ValueConverter<StudentId, Guid>
{
    public StudentIdConverter()
        : base(
            id => id.Value,
            value => StudentId.Of(value))
    {
    }
}
```

`Converters\RosterImportIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RosterImportIdConverter : ValueConverter<RosterImportId, Guid>
{
    public RosterImportIdConverter()
        : base(
            id => id.Value,
            value => RosterImportId.Of(value))
    {
    }
}
```

`Converters\NationalIdConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class NationalIdConverter : ValueConverter<NationalId, string>
{
    public NationalIdConverter()
        : base(
            nationalId => nationalId.Value,
            value => NationalId.Of(value))
    {
    }
}
```

`Converters\StudentNameConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class StudentNameConverter : ValueConverter<StudentName, string>
{
    public StudentNameConverter()
        : base(
            name => name.Value,
            value => StudentName.Of(value))
    {
    }
}
```

`Converters\PhoneNumberConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class PhoneNumberConverter : ValueConverter<PhoneNumber, string>
{
    public PhoneNumberConverter()
        : base(
            phone => phone.Value,
            value => PhoneNumber.Of(value))
    {
    }
}
```

`Converters\AddressConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter()
        : base(
            address => address.Value,
            value => Address.Of(value))
    {
    }
}
```

`Converters\LessonsStartDateConverter.cs` (copy `WeekStartConverter.cs`):

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class LessonsStartDateConverter : ValueConverter<LessonsStartDate, DateOnly>
{
    public LessonsStartDateConverter()
        : base(
            startDate => startDate.Value,
            value => LessonsStartDate.Of(value))
    {
    }
}
```

`Converters\LicenseTypeConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class LicenseTypeConverter : ValueConverter<LicenseType, string>
{
    public LicenseTypeConverter()
        : base(
            licenseType => licenseType.Value,
            value => LicenseType.Of(value))
    {
    }
}
```

`Converters\RosterFileNameConverter.cs`:

```csharp
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class RosterFileNameConverter : ValueConverter<RosterFileName, string>
{
    public RosterFileNameConverter()
        : base(
            fileName => fileName.Value,
            value => RosterFileName.Of(value))
    {
    }
}
```

- [ ] **Step 2: Entity configurations**

`EntityConfigurations\StudentConfiguration.cs` — snake_case columns, UNIQUE index on `national_id`, optional columns nullable (no `IsRequired` on `Address`/`StartDate`/`LicenseType`):

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<StudentIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.NationalId)
            .HasColumnName("national_id")
            .HasConversion<NationalIdConverter>();

        builder
            .HasIndex(x => x.NationalId)
            .IsUnique();

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion<StudentNameConverter>();

        builder
            .Property(x => x.Phone)
            .HasColumnName("phone")
            .HasConversion<PhoneNumberConverter>();

        builder
            .Property(x => x.TeacherId)
            .HasColumnName("teacher_id")
            .HasConversion<TeacherIdConverter>();

        builder
            .Property(x => x.CarId)
            .HasColumnName("car_id")
            .HasConversion<CarIdConverter>();

        builder
            .Property(x => x.Address)
            .HasColumnName("address")
            .HasConversion<AddressConverter>();

        builder
            .Property(x => x.StartDate)
            .HasColumnName("start_date")
            .HasConversion<LessonsStartDateConverter>();

        builder
            .Property(x => x.LicenseType)
            .HasColumnName("license_type")
            .HasConversion<LicenseTypeConverter>();

        builder
            .Property(x => x.IsActive)
            .HasColumnName("is_active");
    }
}
```

`EntityConfigurations\RosterImportConfiguration.cs` — owned collections follow `PublicationConfiguration.cs`'s `TeacherVersions` pattern (`OwnsMany` + shadow FK + `PropertyAccessMode.Field`), with one difference: `RosterImportEntry`/`RosterImportFailure` are value-object records without typed IDs, so each owned table gets a shadow `int` identity key instead of an ID converter:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class RosterImportConfiguration : IEntityTypeConfiguration<RosterImport>
{
    public void Configure(EntityTypeBuilder<RosterImport> builder)
    {
        builder.ToTable("roster_imports");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion<RosterImportIdConverter>();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasConversion<RosterFileNameConverter>();

        builder
            .Property(x => x.ImportedAtUtc)
            .HasColumnName("imported_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.AddedCount)
            .HasColumnName("added_count");

        builder
            .Property(x => x.UpdatedCount)
            .HasColumnName("updated_count");

        builder
            .Property(x => x.DeactivatedCount)
            .HasColumnName("deactivated_count");

        builder
            .Property(x => x.FailedCount)
            .HasColumnName("failed_count");

        builder.OwnsMany(
            x => x.Entries,
            entries =>
            {
                entries.ToTable("roster_import_entries");

                entries.Property<int>("id");

                entries.HasKey("id");

                entries
                    .Property<RosterImportId>("roster_import_id")
                    .HasConversion<RosterImportIdConverter>()
                    .IsRequired();

                entries
                    .WithOwner()
                    .HasForeignKey("roster_import_id");

                entries
                    .Property(e => e.NationalId)
                    .HasColumnName("national_id")
                    .HasConversion<NationalIdConverter>();

                entries
                    .Property(e => e.Outcome)
                    .HasColumnName("outcome");
            });

        builder.OwnsMany(
            x => x.Failures,
            failures =>
            {
                failures.ToTable("roster_import_failures");

                failures.Property<int>("id");

                failures.HasKey("id");

                failures
                    .Property<RosterImportId>("roster_import_id")
                    .HasConversion<RosterImportIdConverter>()
                    .IsRequired();

                failures
                    .WithOwner()
                    .HasForeignKey("roster_import_id");

                failures
                    .Property(f => f.RowNumber)
                    .HasColumnName("row_number");

                failures
                    .Property(f => f.StudentName)
                    .HasColumnName("student_name");

                failures
                    .Property(f => f.Reason)
                    .HasColumnName("reason");
            });

        builder
            .Navigation(x => x.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .Navigation(x => x.Failures)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

> If Task 3's `RosterImport` exposes `Entries`/`Failures` directly from constructor-assigned readonly lists with matching backing-field names, the `UsePropertyAccessMode(PropertyAccessMode.Field)` calls work as-is; if it exposes plain get-only properties without backing fields EF can find, drop the two `Navigation` lines. The existing aggregate wins.

- [ ] **Step 3: Repositories and query implementations**

`Repositories\StudentRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public StudentRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Student>> FindAllAsync()
    {
        return await dbContext.Students.ToListAsync();
    }

    public void Add(Student student)
    {
        dbContext.Students.Add(student);
    }
}
```

`Repositories\RosterImportRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class RosterImportRepository : IRosterImportRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public RosterImportRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void Add(RosterImport rosterImport)
    {
        dbContext.RosterImports.Add(rosterImport);
    }
}
```

`Queries\StudentQueries.cs` — composed shape, so the projection is inline query-syntax like `PublicationQueries.FindHistoryAsync`; the optional filter value is extracted into a local **before** the LINQ expression (EF cannot translate `TeacherId.Of(...)` inside the tree):

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class StudentQueries : IStudentQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public StudentQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId)
    {
        var students = dbContext.Students.AsQueryable();

        if (teacherId is not null)
        {
            var resolvedTeacherId = TeacherId.Of(teacherId.Value);
            students = students.Where(x => x.TeacherId == resolvedTeacherId);
        }

        var query = from student in students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    orderby teacher.Name, student.Name
                    select new ItemForFindStudentsResponse
                    {
                        Id = student.Id.Value,
                        NationalId = student.NationalId.Value,
                        Name = student.Name.Value,
                        Phone = student.Phone.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        CarId = car.Id.Value,
                        CarName = car.Name.Value,
                        IsActive = student.IsActive
                    };

        return await query.ToListAsync();
    }
}
```

> Note: `Teachers` and `Cars` carry soft-delete query filters, so students of a deleted teacher/car drop out of this join. That matches the import rule (deleted teachers/cars never resolve), and such students get deactivated on the next upload anyway; revisit with `IgnoreQueryFilters()` only if the client ever needs to show them in the interim.

`Queries\RosterImportQueries.cs` — single-root projection through the static `Selector` (owned collections project fine inside a `Selector`; `WeekScheduleQueries` + `GetWeekScheduleResponse.Selector` is the proven precedent):

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetLatestRosterImport;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class RosterImportQueries : IRosterImportQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public RosterImportQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<GetLatestRosterImportResponse?> GetLatestAsync()
    {
        return await dbContext
                         .RosterImports
                         .OrderByDescending(x => x.ImportedAtUtc)
                         .Select(GetLatestRosterImportResponse.Selector)
                         .FirstOrDefaultAsync();
    }
}
```

- [ ] **Step 4: DbContext + DI**

In `DrivingLessonsDbContext.cs` add next to `Publications` (matching the expression-bodied `DbSet` style):

```csharp
public DbSet<Student> Students => Set<Student>();

public DbSet<RosterImport> RosterImports => Set<RosterImport>();
```

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs` add next to the publication registrations (plus `using DrivingLessons.Infrastructure.Csv;`):

```csharp
services.AddScoped<IStudentRepository, StudentRepository>();
services.AddScoped<IStudentQueries, StudentQueries>();
services.AddScoped<IRosterImportRepository, RosterImportRepository>();
services.AddScoped<IRosterImportQueries, RosterImportQueries>();
services.AddScoped<IRosterCsvParser, RosterCsvParser>();
```

- [ ] **Step 5: Build, then generate the migration**

Run: `dotnet build`
Expected: success.

Run:
```bash
dotnet ef migrations add AddStudentsAndRosterImports --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```
Expected: migration created under `EntityFramework\Migrations\`. Inspect the generated file and confirm:
- `students` table with a **unique** index on `national_id`, nullable `address`/`start_date`/`license_type`
- `roster_imports` table with `file_name`, `imported_at_utc` (timestamptz) and the four count columns
- `roster_import_entries` and `roster_import_failures` tables, each with a serial `id` key and FK `roster_import_id` → `roster_imports` (cascade delete)

- [ ] **Step 6: Run all tests, commit**

Run:
```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```
Expected: PASS.

```bash
git add src/DrivingLessons.Infrastructure
git commit -m "feat(infrastructure): persist students and roster imports"
```

---

**Next:** [task-08-controllers.md](task-08-controllers.md)
