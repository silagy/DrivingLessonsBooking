# Task 6 of 10: Application layer — repositories, import interactor, queries

> Part of [US-49: Roster Module](README.md). Requires tasks 1–5 complete. Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `IRosterImportRepository.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\ITeacherRepository.cs`, `ICarRepository.cs` (add `FindActiveAsync`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\TeacherRepository.cs`, `CarRepository.cs` (implement `FindActiveAsync` — needed so the solution builds)
- Create: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`, `ImportRosterRequest.cs`, `ImportRosterResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\IStudentQueries.cs`, `Queries\FindStudents\FindStudentsInteractor.cs`, `ItemForFindStudentsResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\IRosterImportQueries.cs`, `Queries\GetLatestRosterImport\GetLatestRosterImportInteractor.cs`, `GetLatestRosterImportResponse.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\RosterImportNotFoundException.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Create: `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`

Before coding, open `PublishPublicationInteractor.cs` (repository + `IUnitOfWork` field order, `??` alignment), `FindTeachersInteractor.cs` + `ItemForFindTeachersResponse.cs` (query interactor shape), `GetWeekScheduleResponse.cs` (nested response classes in one file), `PublicationNotFoundException.cs`, and `WeekScheduleCreatedHandlerTest.cs` (FakeItEasy test style) and match them exactly.

- [ ] **Step 1: Repository interfaces**

`src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyCollection<Student>> FindAllAsync();

    void Add(Student student);
}
```

`src\DrivingLessons.Domain\Repositories\IRosterImportRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Repositories;

public interface IRosterImportRepository
{
    void Add(RosterImport rosterImport);
}
```

Add to `ITeacherRepository.cs` (deleted teachers must not resolve from CSV names):

```csharp
Task<IReadOnlyCollection<Teacher>> FindActiveAsync();
```

Add to `ICarRepository.cs`:

```csharp
Task<IReadOnlyCollection<Car>> FindActiveAsync();
```

Implement both now so the solution keeps building. Both `TeacherConfiguration` and `CarConfiguration` already apply `HasQueryFilter(x => !x.IsDeleted)`, so a plain query is already active-only — the interface name makes that contract explicit. Add to `TeacherRepository.cs` (plus `using Microsoft.EntityFrameworkCore;`):

```csharp
public async Task<IReadOnlyCollection<Teacher>> FindActiveAsync()
{
    return await dbContext.Teachers.ToListAsync();
}
```

And the mirrored method on `CarRepository.cs`:

```csharp
public async Task<IReadOnlyCollection<Car>> FindActiveAsync()
{
    return await dbContext.Cars.ToListAsync();
}
```

- [ ] **Step 2: Import command**

`src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterRequest.cs` (the controller passes the file name and opened stream — `IFormFile` never crosses into Application):

```csharp
namespace DrivingLessons.Application.Commands.ImportRoster;

public record ImportRosterRequest(string FileName, Stream Content);
```

`src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ImportRoster;

public record ImportRosterResponse(Guid RosterImportId, int Added, int Updated, int Deactivated, int Failed);
```

`src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs` — the whole upload is one transaction: parse, resolve rows against active teachers/cars and existing students, deactivate absentees, persist one `RosterImport` record, commit once. Name and car lookups are dictionary lookups on the `TeacherName`/`CarName` value records (value-equal keys; `Of()` trims, and the parser already stripped bidi marks and NBSPs, so CSV cells normalize identically). `NationalId.Of` failures are caught as `DomainException` — that covers all three of its validation exceptions from Task 2 without coupling to their names:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ImportRoster;

public class ImportRosterInteractor
{
    private const string PrimaryStartDateFormat = "dd/MM/yyyy";
    private const string ShortStartDateFormat = "d/M/yyyy";

    private readonly IRosterCsvParser parser;
    private readonly IStudentRepository studentRepository;
    private readonly IRosterImportRepository rosterImportRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public ImportRosterInteractor(
        IRosterCsvParser parser,
        IStudentRepository studentRepository,
        IRosterImportRepository rosterImportRepository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.parser = parser;
        this.studentRepository = studentRepository;
        this.rosterImportRepository = rosterImportRepository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public async Task<ImportRosterResponse> ExecuteAsync(ImportRosterRequest request)
    {
        var rows = parser.Parse(request.Content);

        var activeTeachers = await teacherRepository.FindActiveAsync();
        var teachersByName = activeTeachers.ToDictionary(x => x.Name);

        var activeCars = await carRepository.FindActiveAsync();
        var carsByName = activeCars.ToDictionary(x => x.Name);

        var existingStudents = await studentRepository.FindAllAsync();
        var studentsByNationalId = existingStudents.ToDictionary(x => x.NationalId);

        var entries = new List<RosterImportEntry>();
        var failures = new List<RosterImportFailure>();
        var seenIds = new HashSet<NationalId>();

        foreach (var row in rows)
        {
            var failureReason = ProcessRow(row, teachersByName, carsByName, studentsByNationalId, seenIds, entries);

            if (failureReason is null)
            {
                continue;
            }

            var failure = RosterImportFailure.Of(row.RowNumber, row.FullName, failureReason.Value);
            failures.Add(failure);
        }

        DeactivateAbsentees(existingStudents, seenIds, entries);

        var fileName = RosterFileName.Of(request.FileName);
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, entries, failures);

        rosterImportRepository.Add(rosterImport);

        await unitOfWork.CommitAsync();

        return new ImportRosterResponse(
            rosterImport.Id.Value,
            rosterImport.AddedCount,
            rosterImport.UpdatedCount,
            rosterImport.DeactivatedCount,
            rosterImport.FailedCount);
    }

    private RosterRowFailureReason? ProcessRow(
        RosterCsvRow row,
        IReadOnlyDictionary<TeacherName, Teacher> teachersByName,
        IReadOnlyDictionary<CarName, Car> carsByName,
        IReadOnlyDictionary<NationalId, Student> studentsByNationalId,
        HashSet<NationalId> seenIds,
        List<RosterImportEntry> entries)
    {
        if (row.FullName is null)
        {
            return RosterRowFailureReason.MissingName;
        }

        if (row.Phone is null)
        {
            return RosterRowFailureReason.MissingPhone;
        }

        if (row.NationalId is null)
        {
            return RosterRowFailureReason.InvalidNationalId;
        }

        if (row.TeacherName is null)
        {
            return RosterRowFailureReason.MissingTeacher;
        }

        if (row.CarName is null)
        {
            return RosterRowFailureReason.MissingCar;
        }

        NationalId nationalId;

        try
        {
            nationalId = NationalId.Of(row.NationalId);
        }
        catch (DomainException)
        {
            return RosterRowFailureReason.InvalidNationalId;
        }

        var isFirstOccurrence = seenIds.Add(nationalId);

        if (!isFirstOccurrence)
        {
            return RosterRowFailureReason.DuplicateNationalId;
        }

        var teacherName = TeacherName.Of(row.TeacherName);

        if (!teachersByName.TryGetValue(teacherName, out var teacher))
        {
            return RosterRowFailureReason.UnknownTeacher;
        }

        var carName = CarName.Of(row.CarName);

        if (!carsByName.TryGetValue(carName, out var car))
        {
            return RosterRowFailureReason.UnknownCar;
        }

        LessonsStartDate? startDate = null;

        if (row.StartDate is not null)
        {
            if (!TryParseStartDate(row.StartDate, out var parsedStartDate))
            {
                return RosterRowFailureReason.InvalidStartDate;
            }

            startDate = LessonsStartDate.Of(parsedStartDate);
        }

        var name = StudentName.Of(row.FullName);
        var phone = PhoneNumber.Of(row.Phone);

        var address = row.Address is null
            ? null
            : Address.Of(row.Address);

        var licenseType = row.LicenseType is null
            ? null
            : LicenseType.Of(row.LicenseType);

        if (studentsByNationalId.TryGetValue(nationalId, out var student))
        {
            student.UpdateFromRoster(name, phone, teacher, car, address, startDate, licenseType);

            if (!student.IsActive)
            {
                student.Reactivate();
            }

            var updatedEntry = RosterImportEntry.Of(nationalId, RosterEntryOutcome.Updated);
            entries.Add(updatedEntry);

            return null;
        }

        var createdStudent = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);
        studentRepository.Add(createdStudent);

        var addedEntry = RosterImportEntry.Of(nationalId, RosterEntryOutcome.Added);
        entries.Add(addedEntry);

        return null;
    }

    private static void DeactivateAbsentees(
        IReadOnlyCollection<Student> existingStudents,
        HashSet<NationalId> seenIds,
        List<RosterImportEntry> entries)
    {
        var absentees = existingStudents.Where(x => x.IsActive && !seenIds.Contains(x.NationalId));

        foreach (var student in absentees)
        {
            student.Deactivate();

            var entry = RosterImportEntry.Of(student.NationalId, RosterEntryOutcome.Deactivated);
            entries.Add(entry);
        }
    }

    private static bool TryParseStartDate(string value, out DateOnly result)
    {
        if (DateOnly.TryParseExact(value, PrimaryStartDateFormat, out result))
        {
            return true;
        }

        return DateOnly.TryParseExact(value, ShortStartDateFormat, out result);
    }
}
```

Row-processing rules encoded above, in order:
1. Missing name/phone/id/teacher/car → `Missing*` failure (a missing ID reports `InvalidNationalId` — the enum has no `MissingNationalId`)
2. `NationalId.Of` throws → `InvalidNationalId`
3. Duplicate ID within the file → `DuplicateNationalId`; **first row wins**. The ID enters `seenIds` at this point, so an existing student whose later checks fail (unknown car, bad date) is *not* deactivated by the absentee sweep
4. Teacher/car name not in the active dictionaries → `UnknownTeacher` / `UnknownCar`
5. Empty start date → `null`; otherwise `dd/MM/yyyy` then `d/M/yyyy`, else `InvalidStartDate`
6. Existing student → `UpdateFromRoster` (+ `Reactivate()` if inactive) → `Updated`; new → `Student.Create` + `Add` → `Added`
7. Every pre-existing **active** student not seen in the file → `Deactivate()` → `Deactivated`; inactive absentees are skipped (`Deactivate` is not idempotent)

> Enum/value-object namespaces: `RosterEntryOutcome`, `RosterRowFailureReason`, `RosterImportEntry`, `RosterImportFailure`, `RosterFileName` were created in Tasks 1–4 — check their actual namespaces (`Domain.Values`) and `Of()` signatures before use; the existing files win.

- [ ] **Step 3: Query side**

`src\DrivingLessons.Application\Queries\IStudentQueries.cs`:

```csharp
using DrivingLessons.Application.Queries.FindStudents;

namespace DrivingLessons.Application.Queries;

public interface IStudentQueries
{
    Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId);
}
```

`src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs` — a composed (join) shape, so no `Selector`; the projection is inline in `StudentQueries` (Task 7), same as `ItemForFindPublicationHistoryResponse`:

```csharp
namespace DrivingLessons.Application.Queries.FindStudents;

public class ItemForFindStudentsResponse
{
    public Guid Id { get; init; }
    public string NationalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CarId { get; init; }
    public string CarName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
```

`src\DrivingLessons.Application\Queries\FindStudents\FindStudentsInteractor.cs`:

```csharp
namespace DrivingLessons.Application.Queries.FindStudents;

public class FindStudentsInteractor
{
    private readonly IStudentQueries queries;

    public FindStudentsInteractor(IStudentQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> ExecuteAsync(Guid? teacherId)
    {
        return await queries.FindAsync(teacherId);
    }
}
```

`src\DrivingLessons.Application\Queries\IRosterImportQueries.cs`:

```csharp
using DrivingLessons.Application.Queries.GetLatestRosterImport;

namespace DrivingLessons.Application.Queries;

public interface IRosterImportQueries
{
    Task<GetLatestRosterImportResponse?> GetLatestAsync();
}
```

`src\DrivingLessons.Application\Queries\GetLatestRosterImport\GetLatestRosterImportResponse.cs` — single-root projection with owned collections; the static `Selector` works exactly like `GetWeekScheduleResponse.Selector` does over the owned `Slots` collection. Nested item classes stay in this file, matching `GetWeekScheduleResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetLatestRosterImport;

public class GetLatestRosterImportResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime ImportedAtUtc { get; init; }
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Deactivated { get; init; }
    public int Failed { get; init; }
    public IReadOnlyCollection<EntryForGetLatestRosterImportResponse> Entries { get; init; } = [];
    public IReadOnlyCollection<FailureForGetLatestRosterImportResponse> Failures { get; init; } = [];

    public static Expression<Func<RosterImport, GetLatestRosterImportResponse>> Selector =>
        x => new GetLatestRosterImportResponse
        {
            Id = x.Id.Value,
            FileName = x.FileName.Value,
            ImportedAtUtc = x.ImportedAtUtc,
            Added = x.AddedCount,
            Updated = x.UpdatedCount,
            Deactivated = x.DeactivatedCount,
            Failed = x.FailedCount,
            Entries = x.Entries
                       .Select(entry => new EntryForGetLatestRosterImportResponse
                       {
                           NationalId = entry.NationalId.Value,
                           Outcome = entry.Outcome
                       })
                       .ToList(),
            Failures = x.Failures
                        .OrderBy(failure => failure.RowNumber)
                        .Select(failure => new FailureForGetLatestRosterImportResponse
                        {
                            RowNumber = failure.RowNumber,
                            StudentName = failure.StudentName,
                            Reason = failure.Reason
                        })
                        .ToList()
        };
}

public class EntryForGetLatestRosterImportResponse
{
    public string NationalId { get; init; } = string.Empty;
    public RosterEntryOutcome Outcome { get; init; }
}

public class FailureForGetLatestRosterImportResponse
{
    public int RowNumber { get; init; }
    public string? StudentName { get; init; }
    public RosterRowFailureReason Reason { get; init; }
}
```

`src\DrivingLessons.Application\Queries\GetLatestRosterImport\GetLatestRosterImportInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetLatestRosterImport;

public class GetLatestRosterImportInteractor
{
    private readonly IRosterImportQueries queries;

    public GetLatestRosterImportInteractor(IRosterImportQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetLatestRosterImportResponse> ExecuteAsync()
    {
        var rosterImport = await queries.GetLatestAsync();

        if (rosterImport is null)
        {
            throw new RosterImportNotFoundException();
        }

        return rosterImport;
    }
}
```

`src\DrivingLessons.Application\Common\Exceptions\RosterImportNotFoundException.cs` (extends `NotFoundException` → the `ApiExceptionFilter` maps it to 404):

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class RosterImportNotFoundException : NotFoundException
{
    public RosterImportNotFoundException()
        : base("No roster import was found.")
    {
    }
}
```

- [ ] **Step 4: Register interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, next to the publication interactors add (plus the three `using` directives):

```csharp
services.AddScoped<ImportRosterInteractor>();
services.AddScoped<FindStudentsInteractor>();
services.AddScoped<GetLatestRosterImportInteractor>();
```

- [ ] **Step 5: Interactor tests**

FakeItEasy fakes for all five ports plus `IUnitOfWork` (the project already references FakeItEasy 8.3.0). `123456782` and `987654324` satisfy the Israeli ID checksum; `123456789` does not — keep these exact values if Task 2's `NationalId` validates the checksum.

`tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Commands.ImportRoster;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ImportRosterInteractorTest
{
    private IRosterCsvParser parser = null!;
    private IStudentRepository studentRepository = null!;
    private IRosterImportRepository rosterImportRepository = null!;
    private ITeacherRepository teacherRepository = null!;
    private ICarRepository carRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ImportRosterInteractor interactor = null!;
    private Teacher teacher = null!;
    private Car car = null!;
    private RosterImport? persistedImport;

    [TestInitialize]
    public void Init()
    {
        parser = A.Fake<IRosterCsvParser>();
        studentRepository = A.Fake<IStudentRepository>();
        rosterImportRepository = A.Fake<IRosterImportRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        carRepository = A.Fake<ICarRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ImportRosterInteractor(
            parser,
            studentRepository,
            rosterImportRepository,
            teacherRepository,
            carRepository,
            unitOfWork,
            TimeProvider.System);

        teacher = Teacher.Create(TeacherName.Of("משה לוי"), Email.Of("moshe@school.co.il"));
        car = Car.Create(CarName.Of("טויוטה 123"), CarType.Of("יאריס"), Transmission.Manual);
        persistedImport = null;

        A.CallTo(() => teacherRepository.FindActiveAsync())
            .Returns(new List<Teacher> { teacher });

        A.CallTo(() => carRepository.FindActiveAsync())
            .Returns(new List<Car> { car });

        A.CallTo(() => rosterImportRepository.Add(A<RosterImport>._))
            .Invokes(call => persistedImport = call.GetArgument<RosterImport>(0));

        StudentsAre();
    }

    [TestMethod]
    public async Task New_Row_Adds_Student()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(
                x => x.NationalId == NationalId.Of("123456782"))))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Existing_Row_Updates_Student()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Updated.ShouldBe(1);
        student.Name.ShouldBe(StudentName.Of("דנה כהן"));
        student.Phone.ShouldBe(PhoneNumber.Of("0501234567"));
        A.CallTo(() => studentRepository.Add(A<Student>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Reappearing_Inactive_Student_Is_Reactivated()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeTrue();
        response.Updated.ShouldBe(1);
    }

    [TestMethod]
    public async Task Absent_Student_Is_Deactivated()
    {
        //given
        var student = ExistingStudent("123456782");
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        student.IsActive.ShouldBeFalse();
        response.Deactivated.ShouldBe(1);
        persistedImport!.Entries.ShouldContain(x =>
            x.NationalId == NationalId.Of("123456782") && x.Outcome == RosterEntryOutcome.Deactivated);
    }

    [TestMethod]
    public async Task Absent_Inactive_Student_Is_Skipped()
    {
        //given
        var student = ExistingStudent("123456782");
        student.Deactivate();
        StudentsAre(student);
        RowsAre(Row(2, "יוסי מזרחי", "987654324"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Deactivated.ShouldBe(0);
        student.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Invalid_National_Id_Is_Recorded_As_Failed_Row()
    {
        //given
        RowsAre(Row(2, "רות אברהם", "123456789"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2
            && x.StudentName == "רות אברהם"
            && x.Reason == RosterRowFailureReason.InvalidNationalId);
        A.CallTo(() => studentRepository.Add(A<Student>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unknown_Teacher_Is_Recorded_As_Failed_Row()
    {
        //given
        var row = new RosterCsvRow
        {
            RowNumber = 2,
            FullName = "דנה כהן",
            NationalId = "123456782",
            Phone = "0501234567",
            TeacherName = "מורה לא קיים",
            CarName = car.Name.Value
        };
        RowsAre(row);

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.UnknownTeacher);
    }

    [TestMethod]
    public async Task Unknown_Car_Is_Recorded_As_Failed_Row()
    {
        //given
        var row = new RosterCsvRow
        {
            RowNumber = 2,
            FullName = "דנה כהן",
            NationalId = "123456782",
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = "רכב לא קיים"
        };
        RowsAre(row);

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Failed.ShouldBe(1);
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 2 && x.Reason == RosterRowFailureReason.UnknownCar);
    }

    [TestMethod]
    public async Task Duplicate_National_Id_In_File_Keeps_First_Row()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"), Row(3, "דנה אחרת", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        response.Added.ShouldBe(1);
        response.Failed.ShouldBe(1);
        A.CallTo(() => studentRepository.Add(A<Student>.That.Matches(
                x => x.Name == StudentName.Of("דנה כהן"))))
            .MustHaveHappenedOnceExactly();
        persistedImport!.Failures.ShouldContain(x =>
            x.RowNumber == 3 && x.Reason == RosterRowFailureReason.DuplicateNationalId);
    }

    [TestMethod]
    public async Task Roster_Import_Record_Is_Persisted()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        var response = await interactor.ExecuteAsync(Request());

        //then
        A.CallTo(() => rosterImportRepository.Add(A<RosterImport>._))
            .MustHaveHappenedOnceExactly();
        persistedImport!.FileName.ShouldBe(RosterFileName.Of("roster.csv"));
        response.RosterImportId.ShouldBe(persistedImport.Id.Value);
    }

    [TestMethod]
    public async Task Commit_Happens_Once()
    {
        //given
        RowsAre(Row(2, "דנה כהן", "123456782"));

        //when
        await interactor.ExecuteAsync(Request());

        //then
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedOnceExactly();
    }

    private static ImportRosterRequest Request()
    {
        return new ImportRosterRequest("roster.csv", Stream.Null);
    }

    private void RowsAre(params RosterCsvRow[] rows)
    {
        A.CallTo(() => parser.Parse(A<Stream>._))
            .Returns(rows);
    }

    private void StudentsAre(params Student[] students)
    {
        A.CallTo(() => studentRepository.FindAllAsync())
            .Returns(students);
    }

    private RosterCsvRow Row(int rowNumber, string fullName, string nationalId)
    {
        return new RosterCsvRow
        {
            RowNumber = rowNumber,
            FullName = fullName,
            NationalId = nationalId,
            Phone = "0501234567",
            TeacherName = teacher.Name.Value,
            CarName = car.Name.Value
        };
    }

    private Student ExistingStudent(string nationalId)
    {
        var id = NationalId.Of(nationalId);
        var name = StudentName.Of("תלמיד קיים");
        var phone = PhoneNumber.Of("0500000000");

        return Student.Create(id, name, phone, teacher, car, null, null, null);
    }
}
```

> `Student.Create` / `CarType.Of` argument shapes come from Tasks 1–4 and the existing `Car` aggregate — the existing signatures win if they differ.

- [ ] **Step 6: Build + run all tests**

Run: `dotnet build`
Expected: success.

Run:
```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```
Expected: all tests PASS, including the 11 new interactor tests.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain src/DrivingLessons.Application src/DrivingLessons.Infrastructure tests/DrivingLessons.Application.Test
git commit -m "feat(application): add roster import application layer"
```

---

**Next:** [task-07-infrastructure-persistence.md](task-07-infrastructure-persistence.md)
