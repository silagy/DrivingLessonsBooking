# Task 1 of 6: Identify query — DTOs, not-found exception, interactor, EF implementation

> Part of [US-50/51/27: Identity & Routing](README.md). Work on branch `52-us-50-51-27-identity-and-routing`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs`
- Create: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentRequest.cs`
- Create: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs`
- Create: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IStudentQueries.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs`

**Interfaces:**
- Consumes (slice 1 / roster module, already on `main`): `IPublicationQueries.GetByLinkTokenExcludingDraftsAsync(string linkToken) : Task<GetPublicationByLinkResponse?>` (`WeekStart` is a `DateOnly`); `PublicationLinkNotFoundException()`; `NationalId.Of(string)` (throws `NationalIdMustBeDigitsException` / `NationalIdMustBeAtMostNineDigitsException` / `NationalIdMustHaveValidCheckDigitException`, all `DomainException`, none carrying the ID); `SlotWindowTimes.StartOf/EndOf(SlotWindowType)`.
- Produces (task 2 relies on these exact names):
  - `IdentifyStudentInteractor.ExecuteAsync(string linkToken, IdentifyStudentRequest request) : Task<IdentifyStudentResponse>` — throws `PublicationLinkNotFoundException` (404), a `NationalId` `DomainException` (409), or `StudentNotFoundException` (404).
  - `record IdentifyStudentRequest(string NationalId)`
  - `IdentifyStudentResponse { string StudentName; string TeacherName; string CarName; Transmission Transmission; IReadOnlyCollection<SlotForIdentifyStudentResponse> Slots; }`
  - `SlotForIdentifyStudentResponse { Guid Id; DayOfWeek Day; SlotWindowType Window; SlotState State; TimeOnly StartLocal; TimeOnly EndLocal; }` + static `Selector`
  - `IStudentQueries.GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart) : Task<IdentifyStudentResponse?>` — `null` when no **active** student has the ID; `Slots` empty when the teacher has no week schedule for `weekStart`.

The interface member and its EF implementation land together: `DrivingLessons.Application.Test` references Infrastructure, so the test project does not compile until `StudentQueries` implements the new member. Precedents to open before coding: `Queries\GetPublicationByLink\GetPublicationByLinkInteractor.cs` (query interactor + link lookup), `Queries\GetWeekSchedule\GetWeekScheduleResponse.cs` (slot DTO with computed local times), `Infrastructure\...\Queries\StudentQueries.cs` (student ⋈ teacher ⋈ car query syntax), `Infrastructure\...\Queries\PublicationQueries.cs` → `GetDashboardAsync` (two-step query, slots via `SelectMany`), `tests\DrivingLessons.Application.Test\Queries\GetPublicationByLinkInteractorTest.cs` (test shape).

- [ ] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationByLink;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class IdentifyStudentInteractorTest
{
    private const string RosterNationalId = "000000018";

    private IPublicationQueries publicationQueries = null!;
    private IStudentQueries studentQueries = null!;
    private IdentifyStudentInteractor interactor = null!;
    private string linkToken = null!;
    private DateOnly weekStart;

    [TestInitialize]
    public void Init()
    {
        publicationQueries = A.Fake<IPublicationQueries>();
        studentQueries = A.Fake<IStudentQueries>();
        interactor = new IdentifyStudentInteractor(publicationQueries, studentQueries);
        linkToken = ShareableLinkToken.New().Value;
        weekStart = new DateOnly(2026, 10, 4);

        var publication = new GetPublicationByLinkResponse
        {
            WeekStart = weekStart,
            WeekNumber = 41,
            State = PublicationState.Open,
            WindowStartUtc = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero),
            WindowEndUtc = new DateTimeOffset(2026, 10, 2, 11, 0, 0, TimeSpan.Zero)
        };

        A.CallTo(() => publicationQueries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns(publication);
    }

    [TestMethod]
    public async Task Returns_The_Roster_Student_For_The_Publication_Week()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var student = new IdentifyStudentResponse
        {
            StudentName = "Test Student",
            TeacherName = "Teacher Cohen",
            CarName = "Corolla White",
            Transmission = Transmission.Automatic
        };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(student);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var response = await interactor.ExecuteAsync(linkToken, request);

        //then
        response.ShouldBeSameAs(student);
    }

    [TestMethod]
    public async Task Publication_Must_Exist_For_The_Link()
    {
        //given
        var unknownLinkToken = ShareableLinkToken.New().Value;

        A.CallTo(() => publicationQueries.GetByLinkTokenExcludingDraftsAsync(unknownLinkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(unknownLinkToken, request);

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(A<NationalId>._, A<DateOnly>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_Must_Be_Active_On_The_Roster()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }

    [TestMethod]
    public async Task Not_Found_Message_Does_Not_Reveal_The_National_Id()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        var exception = await Should.ThrowAsync<StudentNotFoundException>(act);
        exception.Message.ShouldNotContain(RosterNationalId);
    }

    [TestMethod]
    [DataRow("000000019")]
    [DataRow("0000000181")]
    [DataRow("000 000 018")]
    public async Task National_Id_Must_Be_Well_Formed(string malformedNationalId)
    {
        //given
        var request = new IdentifyStudentRequest(malformedNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<DomainException>(act);
        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(A<NationalId>._, A<DateOnly>._))
            .MustNotHaveHappened();
    }
}
```

Why these tests:
- The data rows cover the three `NationalId` rules (bad check digit, ten digits, inner spaces). They all derive from `DomainException`, which is what `ApiExceptionFilter` maps to **409** — the status the client keys on. Inner spaces are rejected on purpose; the client strips them (README decision 6).
- `Not_Found_Message_Does_Not_Reveal_The_National_Id` pins Review Focus 4: the exception message becomes the ProblemDetails `detail` returned to an anonymous caller.
- `Publication_Must_Exist_For_The_Link` explicitly configures `null` — an unconfigured FakeItEasy `Task<T?>` returns a dummy object, not null.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IdentifyStudentInteractorTest"`
Expected: build FAILS — the `IdentifyStudent` namespace, `IdentifyStudentRequest`, `IdentifyStudentResponse`, `IdentifyStudentInteractor`, `StudentNotFoundException`, and `GetActiveByNationalIdAsync` do not exist.

- [ ] **Step 3: Not-found exception**

`src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs`:

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class StudentNotFoundException : NotFoundException
{
    public StudentNotFoundException()
        : base("No active student on the roster matches this national ID.")
    {
    }
}
```

Payload-free by design: the only identifier available is the national ID (PII, code-style "never log PII"), and the message is returned to an anonymous caller.

- [ ] **Step 4: Request DTO**

`src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentRequest.cs`:

```csharp
namespace DrivingLessons.Application.Queries.IdentifyStudent;

public record IdentifyStudentRequest(string NationalId);
```

Non-nullable `string` makes MVC treat the property as required — a missing or empty `nationalId` is a 400 before the interactor runs (task 2 smoke-tests it).

- [ ] **Step 5: Response DTOs**

`src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentResponse
{
    public string StudentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string CarName { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
    public IReadOnlyCollection<SlotForIdentifyStudentResponse> Slots { get; init; } = [];
}

public class SlotForIdentifyStudentResponse
{
    public Guid Id { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public TimeOnly StartLocal => SlotWindowTimes.StartOf(Window);
    public TimeOnly EndLocal => SlotWindowTimes.EndOf(Window);

    public static Expression<Func<Slot, SlotForIdentifyStudentResponse>> Selector =>
        x => new SlotForIdentifyStudentResponse
        {
            Id = x.Id.Value,
            Day = x.Day,
            Window = x.Window,
            State = x.State
        };
}
```

Same shape as `SlotForGetWeekScheduleResponse` (computed wall-clock times, requirements §8.3) but a distinct class — Swagger needs globally unique DTO names, and the admin DTO carries ids this student-safe response must not grow into. The top-level response has no `Selector`: it is composed from two queries (Step 8), like `ItemForFindStudentsResponse`.

- [ ] **Step 6: Query interface member**

Replace `src\DrivingLessons.Application\Queries\IStudentQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IStudentQueries
{
    Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId);

    Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart);
}
```

- [ ] **Step 7: Interactor**

`src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly IStudentQueries studentQueries;

    public IdentifyStudentInteractor(IPublicationQueries publicationQueries, IStudentQueries studentQueries)
    {
        this.publicationQueries = publicationQueries;
        this.studentQueries = studentQueries;
    }

    public async Task<IdentifyStudentResponse> ExecuteAsync(string linkToken, IdentifyStudentRequest request)
    {
        var publication = await publicationQueries.GetByLinkTokenExcludingDraftsAsync(linkToken)
                          ?? throw new PublicationLinkNotFoundException();

        var nationalId = NationalId.Of(request.NationalId);

        var student = await studentQueries.GetActiveByNationalIdAsync(nationalId, publication.WeekStart)
                      ?? throw new StudentNotFoundException();

        return student;
    }
}
```

Order matters: the link is resolved first, so an unknown or draft link is a 404 whatever the body holds (the student never reaches the roster through a dead link). No business rule lives here — "does it exist?" is the application's question (ddd-architecture rule 4); `NationalId.Of` owns validity.

- [ ] **Step 8: EF implementation**

Replace `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.IdentifyStudent;
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

    public async Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        var query = from student in dbContext.Students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    where student.NationalId == nationalId && student.IsActive
                    select new
                    {
                        TeacherId = teacher.Id,
                        StudentName = student.Name.Value,
                        TeacherName = teacher.Name.Value,
                        CarName = car.Name.Value,
                        car.Transmission
                    };

        var identified = await query.FirstOrDefaultAsync();

        if (identified is null)
        {
            return null;
        }

        var slots = await dbContext
                            .WeekSchedules
                            .Where(x => x.TeacherId == identified.TeacherId && x.WeekStart == resolvedWeekStart)
                            .SelectMany(x => x.Slots)
                            .OrderBy(slot => slot.Day)
                            .ThenBy(slot => slot.Window)
                            .Select(SlotForIdentifyStudentResponse.Selector)
                            .ToListAsync();

        return new IdentifyStudentResponse
        {
            StudentName = identified.StudentName,
            TeacherName = identified.TeacherName,
            CarName = identified.CarName,
            Transmission = identified.Transmission,
            Slots = slots
        };
    }
}
```

Notes for the implementer:
- `FindAsync` is unchanged — the file is shown whole only so the new usings land correctly.
- `student.NationalId == nationalId` goes through `NationalIdConverter` exactly like `PublicationRepository.GetByLinkTokenAsync` compares tokens; `national_id` already has a unique index (roster migration), so no migration is needed.
- `student.IsActive` is Review Focus 1: a student deactivated by a later upload is "not on file".
- Transmission comes from the **car** (requirements §5.2/§5.5, decision #18), never from the student.
- A teacher with no week schedule for the week yields `Slots = []` (README decision 5) — `Where` simply matches nothing.
- Slots are ordered Sunday→Friday (`DayOfWeek` Sunday = 0) then Morning→Evening (`SlotWindowType` 10…40), matching the admin grid query.

- [ ] **Step 9: Register the interactor**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using after `using DrivingLessons.Application.Queries.GetWeekSchedule;`:

```csharp
using DrivingLessons.Application.Queries.IdentifyStudent;
```

and register it after `services.AddScoped<FindStudentsInteractor>();`:

```csharp
        services.AddScoped<IdentifyStudentInteractor>();
```

(`IStudentQueries` and `IPublicationQueries` are already registered in `AddInfrastructure`.)

- [ ] **Step 10: Run tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IdentifyStudentInteractorTest"`
Expected: 7 tests PASS (4 methods + 3 data rows).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Application src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs tests/DrivingLessons.Application.Test/Queries/IdentifyStudentInteractorTest.cs
git commit -m "feat(app): identify a roster student for a publication link

Resolves the link, normalizes the national ID, and projects the active
student's teacher, car transmission and that teacher's week grid.
Not-found carries no national ID."
```

---

**Next:** [task-02-identify-endpoint.md](task-02-identify-endpoint.md)
