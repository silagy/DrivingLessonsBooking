# Task 5 of 14: Application queries + DTOs + PublicationClosed handler

> Part of the [US-08–21 Publications plan](README.md). Requires tasks 1–4 complete (`Publication` aggregate, dispatch, command interactors + seams). Work on branch `9-us-08-21-publications-module`, commands from the repo root.
>
> This task adds the read side: the `IPublicationQueries`/`ISubmissionQueries` interfaces (referenced by task-04's `ClosePublicationInteractor`), the response DTOs with their `Selector`, the read interactors, and the `PublicationClosed` handler that emails each teacher a versioned Excel at close. Completing it makes the Application project build green (closes the ordering dependency noted in task-04). Infrastructure implements these query interfaces in task 06 and the seams in tasks 07–08.

**Files:**
- Create: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (+ `SubmissionStats`)
- Create: `src\DrivingLessons.Application\Queries\IPublicationQueries.cs`
- Create: `src\DrivingLessons.Application\Queries\GetPublication\GetPublicationResponse.cs`, `GetPublication\GetPublicationInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardResponse.cs` (+ `SlotCountForGetPublicationDashboardResponse`), `GetPublicationDashboard\GetPublicationDashboardInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\FindPublicationHistory\ItemForFindPublicationHistoryResponse.cs`, `FindPublicationHistory\FindPublicationHistoryInteractor.cs`
- Create: `src\DrivingLessons.Application\Queries\DownloadPublicationExcel\DownloadPublicationExcelInteractor.cs`
- Create: `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`

Queries bypass the domain and project through a static `Selector`; nested DTOs use the `{Property}For{Op}{Entity}Response` / `ItemForFind...Response` naming. Before coding, open `GetWeekScheduleResponse.cs` (Selector + nested `SlotForGetWeekScheduleResponse`) and `GetWeekScheduleInteractor.cs` to match style. `PublicationClosedHandler`'s `Teacher.Name.Value` / `Teacher.ContactEmail.Value` accessors are confirmed against `Teacher.cs` (a `TeacherName` and an `Email`, each exposing `.Value`).

- [ ] **Step 1: Query seams**

`src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` — the submission read side does not exist yet; infrastructure returns zeros/empty (task 06 stub) until `module:student-form` lands. Keyed by raw `Guid` (query boundary):

```csharp
namespace DrivingLessons.Application.Queries;

public interface ISubmissionQueries
{
    Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId);

    Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId);
}

public record SubmissionStats(int StudentsSubmitted, int TotalPicks, DateTimeOffset? LastSubmissionAtUtc);
```

`src\DrivingLessons.Application\Queries\IPublicationQueries.cs`:

```csharp
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;

namespace DrivingLessons.Application.Queries;

public interface IPublicationQueries
{
    Task<GetPublicationResponse?> GetByWeekAsync(DateOnly weekStart);

    Task<GetPublicationDashboardResponse?> GetDashboardAsync(Guid publicationId, Guid teacherId);

    Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistoryAsync();

    Task<IReadOnlyList<Guid>> GetTeacherIdsWithScheduleForWeekAsync(DateOnly weekStart);
}
```

- [ ] **Step 2: `GetPublicationResponse` (with Selector)**

The owned `Window` is nullable (null in Draft), so the window columns project through a null guard. `init` props; raw `Guid`/`string` at the boundary.

`src\DrivingLessons.Application\Queries\GetPublication\GetPublicationResponse.cs`:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublication;

public class GetPublicationResponse
{
    public Guid Id { get; init; }
    public DateOnly WeekStart { get; init; }
    public PublicationState State { get; init; }
    public string LinkToken { get; init; } = string.Empty;
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }

    public static Expression<Func<Publication, GetPublicationResponse>> Selector =>
        x => new GetPublicationResponse
        {
            Id = x.Id.Value,
            WeekStart = x.WeekStart.Value,
            State = x.State,
            LinkToken = x.LinkToken.Value,
            WindowStartUtc = x.Window == null
                ? (DateTimeOffset?)null
                : x.Window.StartUtc,
            WindowEndUtc = x.Window == null
                ? (DateTimeOffset?)null
                : x.Window.EndUtc
        };
}
```

- [ ] **Step 3: `GetPublicationInteractor`**

Wraps `GetByWeekAsync`; throws the week-based `PublicationNotFoundException` (added in task 04).

`src\DrivingLessons.Application\Queries\GetPublication\GetPublicationInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Application.Queries.GetPublication;

public class GetPublicationInteractor
{
    private readonly IPublicationQueries queries;

    public GetPublicationInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetPublicationResponse> ExecuteAsync(DateOnly weekStart)
    {
        var publication = await queries.GetByWeekAsync(weekStart);

        if (publication is null)
        {
            throw new PublicationNotFoundException(weekStart);
        }

        return publication;
    }
}
```

- [ ] **Step 4: Dashboard response + composition interactor**

`GetDashboardAsync` returns the base response from SQL — the teacher's WeekSchedule slots as `SlotCounts` with `RequestCount = 0`, zero/null stats, `LatestExcelVersion` from that teacher's `TeacherExcelVersion`. The interactor then overlays `ISubmissionQueries`: per-slot counts and the three stat fields (composition rule). Because the DTOs are `init`-only, the overlay rebuilds a new response — the counts stay 0 until `module:student-form` implements the stub.

`src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardResponse.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationDashboard;

public class GetPublicationDashboardResponse
{
    public PublicationState State { get; init; }
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }
    public string LinkToken { get; init; } = string.Empty;
    public int StudentsSubmitted { get; init; }
    public int TotalPicks { get; init; }
    public DateTimeOffset? LastSubmissionAtUtc { get; init; }
    public int? LatestExcelVersion { get; init; }
    public IReadOnlyList<SlotCountForGetPublicationDashboardResponse> SlotCounts { get; init; } = [];
}

public class SlotCountForGetPublicationDashboardResponse
{
    public Guid SlotId { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public int RequestCount { get; init; }
}
```

`src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardInteractor.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetPublicationDashboard;

public class GetPublicationDashboardInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly ISubmissionQueries submissionQueries;

    public GetPublicationDashboardInteractor(
        IPublicationQueries publicationQueries,
        ISubmissionQueries submissionQueries)
    {
        this.publicationQueries = publicationQueries;
        this.submissionQueries = submissionQueries;
    }

    public async Task<GetPublicationDashboardResponse> ExecuteAsync(Guid publicationId, Guid teacherId)
    {
        var dashboard = await publicationQueries.GetDashboardAsync(publicationId, teacherId);

        if (dashboard is null)
        {
            throw new PublicationNotFoundException(PublicationId.Of(publicationId));
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

- [ ] **Step 5: History response + interactor**

`src\DrivingLessons.Application\Queries\FindPublicationHistory\ItemForFindPublicationHistoryResponse.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class ItemForFindPublicationHistoryResponse
{
    public Guid PublicationId { get; init; }
    public DateOnly WeekStart { get; init; }
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public PublicationState State { get; init; }
    public DateTimeOffset? WindowStartUtc { get; init; }
    public DateTimeOffset? WindowEndUtc { get; init; }
    public int? LatestExcelVersion { get; init; }
}
```

`src\DrivingLessons.Application\Queries\FindPublicationHistory\FindPublicationHistoryInteractor.cs`:

```csharp
using DrivingLessons.Application.Queries;

namespace DrivingLessons.Application.Queries.FindPublicationHistory;

public class FindPublicationHistoryInteractor
{
    private readonly IPublicationQueries queries;

    public FindPublicationHistoryInteractor(IPublicationQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> ExecuteAsync()
    {
        return await queries.FindHistoryAsync();
    }
}
```

- [ ] **Step 6: Download-excel interactor**

Resolves the publication (404 if missing), then delegates to `IExcelGenerator`. Returns the `ExcelFile`; the controller wraps it as a `File(...)` result (task 09).

`src\DrivingLessons.Application\Queries\DownloadPublicationExcel\DownloadPublicationExcelInteractor.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.DownloadPublicationExcel;

public class DownloadPublicationExcelInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IExcelGenerator excelGenerator;

    public DownloadPublicationExcelInteractor(IPublicationRepository repository, IExcelGenerator excelGenerator)
    {
        this.repository = repository;
        this.excelGenerator = excelGenerator;
    }

    public async Task<ExcelFile> ExecuteAsync(Guid id, Guid teacherId)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var resolvedTeacherId = TeacherId.Of(teacherId);

        return await excelGenerator.GenerateAsync(publication.Id, resolvedTeacherId);
    }
}
```

- [ ] **Step 7: `PublicationClosed` handler (per-teacher Excel + email)**

Loads the closed publication; for each `TeacherExcelVersion` generates the Excel and sends an email whose subject carries the ISO week number and that teacher's version (US-17/18). `ISOWeek.GetWeekOfYear` needs a `DateTime`, so the `WeekStart` `DateOnly` is combined with `TimeOnly.MinValue`. Skips a teacher whose record is missing (defensive; the version set is derived from the same teachers).

`src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs`:

```csharp
using System.Globalization;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;

    public PublicationClosedHandler(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        var weekDate = publication.WeekStart.Value.ToDateTime(TimeOnly.MinValue);
        var week = ISOWeek.GetWeekOfYear(weekDate);

        foreach (var teacherVersion in publication.TeacherVersions)
        {
            var teacher = await teacherRepository.GetAsync(teacherVersion.TeacherId);

            if (teacher is null)
            {
                continue;
            }

            var excel = await excelGenerator.GenerateAsync(publication.Id, teacherVersion.TeacherId);
            var subject = $"Week {week} Requests - {teacher.Name.Value} - v{teacherVersion.Version}";
            var body = $"Attached are the collected requests for week {week}.";
            var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, excel);

            await emailSender.SendAsync(message);
        }
    }
}
```

- [ ] **Step 8: Register interactors + the PublicationClosed handler**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the usings and registrations after task 04's command interactors:

```csharp
using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
```

```csharp
services.AddScoped<GetPublicationInteractor>();
services.AddScoped<GetPublicationDashboardInteractor>();
services.AddScoped<FindPublicationHistoryInteractor>();
services.AddScoped<DownloadPublicationExcelInteractor>();
services.AddScoped<IDomainEventHandler<PublicationClosed>, PublicationClosedHandler>();
```

(`IDomainEventHandler<>` and `PublicationClosed` are already imported by task 03's `using DrivingLessons.Application.Common;` / `using DrivingLessons.Domain.Events;`.)

- [ ] **Step 9: Build + run all tests**

Run: `dotnet build`
Then: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Then: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: build succeeds (this closes task-04's ordering dependency — the Application project now compiles fully); all tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application
git commit -m "feat(app): publication read queries, DTOs, and PublicationClosed email handler"
```

---

**Next:** [task-06-infrastructure-persistence.md](task-06-infrastructure-persistence.md)
