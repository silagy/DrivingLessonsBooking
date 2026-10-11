# Task 3 of 8: Dashboard, Excel download and History reach only the linked Teacher (backend)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 2 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 2-6 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\DownloadPublicationExcel\DownloadPublicationExcelInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\FindPublicationHistory\FindPublicationHistoryInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\IPublicationQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs:99-123`
- Test: `tests\DrivingLessons.Application.Test\Queries\GetPublicationDashboardInteractorTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Queries\DownloadPublicationExcelInteractorTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Queries\FindPublicationHistoryInteractorTest.cs` (**new**)

**Interfaces:**
- Consumes (task 1):
  - `ICurrentUser { UserId Id; Role Role; TeacherId? TeacherId }` in `DrivingLessons.Application.Auth`, registered scoped in `Program.cs`.
  - `CurrentUserExtension.MayReach(this ICurrentUser user, TeacherId teacherId)`: true for an Administrator (linked or not), for a Teacher only when `user.TeacherId == teacherId` (false when null).
- Produces:
  - `GetPublicationDashboardInteractor(IPublicationQueries publicationQueries, ISubmissionQueries submissionQueries, ICurrentUser currentUser)`; `ExecuteAsync(Guid publicationId, Guid teacherId)` unchanged. A refusal throws `PublicationNotFoundException(PublicationId.Of(publicationId))` before any query runs.
  - `DownloadPublicationExcelInteractor(IPublicationRepository repository, IExcelGenerator excelGenerator, ICurrentUser currentUser)`; `ExecuteAsync(Guid id, Guid teacherId)` unchanged. A refusal throws `PublicationNotFoundException` before `repository.GetAsync`.
  - `IPublicationQueries.FindHistoryAsync(Guid? teacherId)`: null = every Teacher's rows, a value = only that Teacher's rows.
  - `FindPublicationHistoryInteractor(IPublicationQueries queries, ICurrentUser currentUser)`; `ExecuteAsync()` unchanged. A Teacher passes `TeacherId.Value`; an Administrator (linked or not) passes null; a Teacher without a linked Teacher gets an empty list and the query never runs.
  - HTTP contract (unchanged routes, `PublicationQueryController`): `GET api/publications/{id}/dashboard?teacherId=` and `GET api/publications/{id}/excel?teacherId=` return **404** for a Teacher asking for another Teacher; `GET api/publications/history` returns only the Teacher's own rows. No controller change: the controller already passes `id` and `teacherId` straight through.

**Why:** AC 2-4 and 6, Review Focus 1 and 4. The dashboard is where a Teacher sees Submissions (per-Teacher counts and stats), so scoping it covers Submissions. The Teacher id is an input on both the dashboard and the Excel download, so the check runs before anything is loaded and another Teacher's Publication isn't revealed to exist. History has no Teacher input, so the query filters (decision 5). `GetPublicationInteractor` (by week) stays unscoped on purpose (decision 6).

`FindHistoryAsync` has exactly one caller (`FindPublicationHistoryInteractor`) and one implementer (`PublicationQueries`); no fake implements `IPublicationQueries` by hand (tests use FakeItEasy), so nothing else changes. Verify with `grep -rn "FindHistoryAsync" src tests --include=*.cs` before Step 4: the only hits are the interface, `PublicationQueries.cs` and `FindPublicationHistoryInteractor.cs` (plus the new test after Step 3).

- [ ] **Step 1: Write the failing dashboard test**

Create `tests\DrivingLessons.Application.Test\Queries\GetPublicationDashboardInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetPublicationDashboardInteractorTest
{
    private const int StudentsSubmitted = 3;
    private const int TotalPicks = 7;

    private IPublicationQueries publicationQueries = null!;
    private ISubmissionQueries submissionQueries = null!;
    private ICurrentUser currentUser = null!;
    private GetPublicationDashboardInteractor interactor = null!;
    private Guid publicationId;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        publicationQueries = A.Fake<IPublicationQueries>();
        submissionQueries = A.Fake<ISubmissionQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetPublicationDashboardInteractor(publicationQueries, submissionQueries, currentUser);

        publicationId = Guid.NewGuid();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        var dashboard = new GetPublicationDashboardResponse
        {
            State = PublicationState.Open,
            LinkToken = "link-token",
            LatestExcelVersion = 2,
            SlotCounts = []
        };
        var noCounts = new Dictionary<Guid, int>();
        var stats = new SubmissionStats(StudentsSubmitted, TotalPicks, null);

        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._))
         .Returns((GetPublicationDashboardResponse?)null);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, ownTeacherId.Value)).Returns(dashboard);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value)).Returns(dashboard);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._)).Returns(noCounts);
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).Returns(stats);
    }

    [TestMethod]
    public async Task Teacher_Sees_Own_Dashboard()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        result.TotalPicks.ShouldBe(TotalPicks);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, ownTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_Dashboard_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Dashboard()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => publicationQueries.GetDashboardAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Any_Teachers_Dashboard()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Sees_Another_Teachers_Dashboard()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publicationId, otherTeacherId.Value);

        //then
        result.StudentsSubmitted.ShouldBe(StudentsSubmitted);
        A.CallTo(() => publicationQueries.GetDashboardAsync(publicationId, otherTeacherId.Value))
         .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Publication_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Administrator, null);
        var missingPublicationId = Guid.NewGuid();

        //when
        var act = () => interactor.ExecuteAsync(missingPublicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => submissionQueries.GetStatsAsync(A<Guid>._, A<Guid>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
```

- [ ] **Step 2: Write the failing Excel download test**

Create `tests\DrivingLessons.Application.Test\Queries\DownloadPublicationExcelInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class DownloadPublicationExcelInteractorTest
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private IPublicationRepository repository = null!;
    private IExcelGenerator excelGenerator = null!;
    private ICurrentUser currentUser = null!;
    private DownloadPublicationExcelInteractor interactor = null!;
    private Publication publication = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;
    private ExcelFile ownExcel = null!;
    private ExcelFile otherExcel = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IPublicationRepository>();
        excelGenerator = A.Fake<IExcelGenerator>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new DownloadPublicationExcelInteractor(repository, excelGenerator, currentUser);

        publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        ownExcel = new ExcelFile("own.xlsx", [1, 2, 3], ExcelContentType);
        otherExcel = new ExcelFile("other.xlsx", [4, 5, 6], ExcelContentType);

        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).Returns((Publication?)null);
        A.CallTo(() => repository.GetAsync(publication.Id)).Returns(publication);
        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, ownTeacherId)).Returns(ownExcel);
        A.CallTo(() => excelGenerator.GenerateAsync(publication.Id, otherTeacherId)).Returns(otherExcel);
    }

    [TestMethod]
    public async Task Teacher_Downloads_Own_Excel()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, ownTeacherId.Value);

        //then
        result.ShouldBe(ownExcel);
    }

    [TestMethod]
    public async Task Other_Teachers_Excel_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var act = () => interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).MustNotHaveHappened();
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Downloads_No_Excel()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(publication.Id.Value, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => repository.GetAsync(A<PublicationId>._)).MustNotHaveHappened();
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Downloads_Any_Teachers_Excel()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        result.ShouldBe(otherExcel);
    }

    [TestMethod]
    public async Task Linked_Administrator_Downloads_Another_Teachers_Excel()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(publication.Id.Value, otherTeacherId.Value);

        //then
        result.ShouldBe(otherExcel);
    }

    [TestMethod]
    public async Task Missing_Publication_Is_Not_Found()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);
        var missingPublicationId = Guid.NewGuid();

        //when
        var act = () => interactor.ExecuteAsync(missingPublicationId, ownTeacherId.Value);

        //then
        await Should.ThrowAsync<PublicationNotFoundException>(act);
        A.CallTo(() => excelGenerator.GenerateAsync(A<PublicationId>._, A<TeacherId>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }
}
```

- [ ] **Step 3: Write the failing History test**

Create `tests\DrivingLessons.Application.Test\Queries\FindPublicationHistoryInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class FindPublicationHistoryInteractorTest
{
    private IPublicationQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private FindPublicationHistoryInteractor interactor = null!;
    private TeacherId ownTeacherId = null!;
    private List<ItemForFindPublicationHistoryResponse> ownRows = null!;
    private List<ItemForFindPublicationHistoryResponse> everyRow = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IPublicationQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new FindPublicationHistoryInteractor(queries, currentUser);

        ownTeacherId = TeacherId.New();
        var ownRow = HistoryRow(ownTeacherId.Value, "Yael Carmi");
        var otherRow = HistoryRow(Guid.NewGuid(), "Dana Levi");
        ownRows = [ownRow];
        everyRow = [ownRow, otherRow];

        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).Returns(ownRows);
        A.CallTo(() => queries.FindHistoryAsync(null)).Returns(everyRow);
    }

    [TestMethod]
    public async Task Teacher_Sees_Only_Own_History()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(ownRows);
        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).MustHaveHappenedOnceExactly();
        A.CallTo(() => queries.FindHistoryAsync(null)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Sees_Every_Teachers_History()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(everyRow);
        A.CallTo(() => queries.FindHistoryAsync(null)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Sees_Every_Teachers_History()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(everyRow);
        A.CallTo(() => queries.FindHistoryAsync(null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => queries.FindHistoryAsync(ownTeacherId.Value)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Sees_No_History()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBeEmpty();
        A.CallTo(() => queries.FindHistoryAsync(A<Guid?>._)).MustNotHaveHappened();
    }

    private void SignedInAs(Role role, TeacherId? linkedTeacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(linkedTeacherId);
    }

    private static ItemForFindPublicationHistoryResponse HistoryRow(Guid teacherId, string teacherName)
    {
        return new ItemForFindPublicationHistoryResponse
        {
            PublicationId = Guid.NewGuid(),
            WeekStart = new DateOnly(2026, 10, 4),
            TeacherId = teacherId,
            TeacherName = teacherName,
            State = PublicationState.Closed,
            LatestExcelVersion = 1
        };
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetPublicationDashboardInteractorTest|FullyQualifiedName~DownloadPublicationExcelInteractorTest|FullyQualifiedName~FindPublicationHistoryInteractorTest"`

Expected: build FAILS with `CS1729: 'GetPublicationDashboardInteractor' does not contain a constructor that takes 3 arguments`, the same for `DownloadPublicationExcelInteractor` (3 arguments) and `FindPublicationHistoryInteractor` (2 arguments), and `CS1501: No overload for method 'FindHistoryAsync' takes 1 arguments`.

- [ ] **Step 5: Scope the dashboard**

Replace `src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardInteractor.cs` with:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationDashboard;

public class GetPublicationDashboardInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly ISubmissionQueries submissionQueries;
    private readonly ICurrentUser currentUser;

    public GetPublicationDashboardInteractor(
        IPublicationQueries publicationQueries,
        ISubmissionQueries submissionQueries,
        ICurrentUser currentUser)
    {
        this.publicationQueries = publicationQueries;
        this.submissionQueries = submissionQueries;
        this.currentUser = currentUser;
    }

    public async Task<GetPublicationDashboardResponse> ExecuteAsync(Guid publicationId, Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new PublicationNotFoundException(resolvedPublicationId);
        }

        var dashboard = await publicationQueries.GetDashboardAsync(publicationId, teacherId);

        if (dashboard is null)
        {
            throw new PublicationNotFoundException(resolvedPublicationId);
        }

        var counts = await submissionQueries.GetSlotRequestCountsAsync(publicationId, teacherId);
        var stats = await submissionQueries.GetStatsAsync(publicationId, teacherId);

        var slotCounts = dashboard.SlotCounts
                                  .Select(slot => new SlotCountForGetPublicationDashboardResponse
                                  {
                                      SlotId = slot.SlotId,
                                      Day = slot.Day,
                                      Window = slot.Window,
                                      State = slot.State,
                                      RequestCount = counts.TryGetValue(slot.SlotId, out var count)
                                          ? count
                                          : 0
                                  })
                                  .ToList();

        return new GetPublicationDashboardResponse
        {
            State = dashboard.State,
            WindowStartUtc = dashboard.WindowStartUtc,
            WindowEndUtc = dashboard.WindowEndUtc,
            LinkToken = dashboard.LinkToken,
            StudentsSubmitted = stats.StudentsSubmitted,
            TotalPicks = stats.TotalPicks,
            LastSubmissionAtUtc = stats.LastSubmissionAtUtc,
            LatestExcelVersion = dashboard.LatestExcelVersion,
            SlotCounts = slotCounts
        };
    }
}
```

The only changes: the `using DrivingLessons.Application.Auth;`, the `currentUser` field and constructor parameter, and the resolve-then-`MayReach` guard before `GetDashboardAsync`. The second `PublicationNotFoundException` now reuses `resolvedPublicationId`. A `teacherId` of `Guid.Empty` now fails in `TeacherId.Of` (`ArgumentException`) before any query, exactly as the Excel download already does today.

- [ ] **Step 6: Scope the Excel download**

Replace `src\DrivingLessons.Application\Queries\DownloadPublicationExcel\DownloadPublicationExcelInteractor.cs` with:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.DownloadPublicationExcel;

public class DownloadPublicationExcelInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IExcelGenerator excelGenerator;
    private readonly ICurrentUser currentUser;

    public DownloadPublicationExcelInteractor(
        IPublicationRepository repository,
        IExcelGenerator excelGenerator,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.excelGenerator = excelGenerator;
        this.currentUser = currentUser;
    }

    public async Task<ExcelFile> ExecuteAsync(Guid id, Guid teacherId)
    {
        var publicationId = PublicationId.Of(id);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new PublicationNotFoundException(publicationId);
        }

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        return await excelGenerator.GenerateAsync(publication.Id, resolvedTeacherId);
    }
}
```

`TeacherId.Of(teacherId)` moves up from after the load to before it, so the guard can use it; the constructor now wraps one parameter per line.

- [ ] **Step 7: Filter History by an optional Teacher**

In `src\DrivingLessons.Application\Queries\IPublicationQueries.cs`, replace:

```csharp
    Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync();
```

with:

```csharp
    Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync(Guid? teacherId);
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs` (usings unchanged: `DrivingLessons.Domain.Values` is already there), replace the whole `FindHistoryAsync` method (lines 99-123):

```csharp
    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync()
    {
        var query = from publication in dbContext.Publications
                    from weekSchedule in dbContext.WeekSchedules
                        .Where(ws => ws.WeekStart == publication.WeekStart)
                    join teacher in dbContext.Teachers
                        on weekSchedule.TeacherId equals teacher.Id
```

(the rest of the method unchanged) with:

```csharp
    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync(Guid? teacherId)
    {
        var teachers = dbContext.Teachers.AsQueryable();

        if (teacherId is not null)
        {
            var resolvedTeacherId = TeacherId.Of(teacherId.Value);
            teachers = teachers.Where(x => x.Id == resolvedTeacherId);
        }

        var query = from publication in dbContext.Publications
                    from weekSchedule in dbContext.WeekSchedules
                        .Where(ws => ws.WeekStart == publication.WeekStart)
                    join teacher in teachers
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
```

This is the `StudentQueries.FindAsync(Guid? teacherId)` pattern: the filter goes on the `Teachers` source before the join, comparing the typed `TeacherId` through its value converter (the same comparison `GetDashboardAsync` already makes on `TeacherVersions`), so the whole query still translates to one SQL statement with a `WHERE t."Id" = @id` when filtered. `AsQueryable()` lets `var` hold the narrowed `Where` result, as in `StudentQueries`.

Replace `src\DrivingLessons.Application\Queries\FindPublicationHistory\FindPublicationHistoryInteractor.cs` with:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class FindPublicationHistoryInteractor
{
    private readonly IPublicationQueries queries;
    private readonly ICurrentUser currentUser;

    public FindPublicationHistoryInteractor(IPublicationQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> ExecuteAsync()
    {
        if (currentUser.Role is Role.Administrator)
        {
            return await queries.FindHistoryAsync(null);
        }

        var linkedTeacherId = currentUser.TeacherId;

        if (linkedTeacherId is null)
        {
            return [];
        }

        return await queries.FindHistoryAsync(linkedTeacherId.Value);
    }
}
```

An Administrator, linked or not, passes null (decision 5, Review Focus 4). A Teacher without a linked Teacher can't happen through `HttpCurrentUser` (task 1 throws first), but if the port ever reports one, it gets nothing rather than every row (Review Focus 3).

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetPublicationDashboardInteractorTest|FullyQualifiedName~DownloadPublicationExcelInteractorTest|FullyQualifiedName~FindPublicationHistoryInteractorTest"`

Expected: PASS, 16 tests (6 dashboard, 6 Excel, 4 History).

Then run the whole backend:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: build succeeds without new warnings; both suites PASS (including `ControllerAuthorizationTest` and `SourceTextTest`, untouched); `has-pending-model-changes` prints `No changes have been made to the model since the last migration.` (this task changes no entity or configuration). The History query's SQL translation is exercised against Postgres by the task 4 smoke (`teacher history` as a Teacher returns only that Teacher's rows).

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Application/Queries/GetPublicationDashboard/GetPublicationDashboardInteractor.cs src/DrivingLessons.Application/Queries/DownloadPublicationExcel/DownloadPublicationExcelInteractor.cs src/DrivingLessons.Application/Queries/FindPublicationHistory/FindPublicationHistoryInteractor.cs src/DrivingLessons.Application/Queries/IPublicationQueries.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/PublicationQueries.cs tests/DrivingLessons.Application.Test/Queries/GetPublicationDashboardInteractorTest.cs tests/DrivingLessons.Application.Test/Queries/DownloadPublicationExcelInteractorTest.cs tests/DrivingLessons.Application.Test/Queries/FindPublicationHistoryInteractorTest.cs
git commit -m "feat(api): Submissions, Excel and History reach only the linked Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
