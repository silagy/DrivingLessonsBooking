# Task 4 of 14: Application commands + seams (excel / email / scheduler) + DI

> Part of the [US-08–21 Publications plan](README.md). Requires tasks 1–3 complete (`Publication` aggregate, `IPublicationRepository`, domain-event dispatch). Work on branch `9-us-08-21-publications-module`, commands from the repo root.
>
> **Ordering dependency:** `ClosePublicationInteractor` (and its DI registration) references `IPublicationQueries`, whose interface + `SubmissionStats`/DTOs are declared in [task-05](task-05-application-queries-handlers.md). Tasks 04 and 05 are the two halves of the Application layer and compile together: the full `dotnet build` in Step 6 goes green **once task-05 has added `Application\Queries\IPublicationQueries.cs`**. Every other interactor in this task builds on its own. Execute 04 then 05 back-to-back; do not stop at a red build between them.

**Files:**
- Create: `src\DrivingLessons.Application\Abstractions\IExcelGenerator.cs` (+ `ExcelFile`), `Abstractions\IEmailSender.cs` (+ `EmailMessage`), `Abstractions\IPublicationScheduler.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\PublicationNotFoundException.cs`
- Create: `src\DrivingLessons.Application\Commands\PublishPublication\PublishPublicationInteractor.cs`, `PublishPublicationRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ExtendPublicationWindow\ExtendPublicationWindowInteractor.cs`, `ExtendPublicationWindowRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ReopenPublication\ReopenPublicationInteractor.cs`, `ReopenPublicationRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\OpenPublication\OpenPublicationInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\ClosePublication\ClosePublicationInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`

There is no interactor test project for these command classes — they are thin orchestration verified by `dotnet build` and the Scalar smoke test in task 09. All inject `IPublicationRepository` + `IUnitOfWork` **separately** (repositories carry no `UnitOfWork` property — follow the code, per prior slices). Before coding, open `CreateWeekScheduleInteractor.cs` and `MarkSlotUnavailableInteractor.cs` to match ctor field order and the resolve-or-throw shape.

- [ ] **Step 1: Application-layer abstractions (seams)**

These isolate the not-yet-built Excel/email/scheduling from the pipeline; infrastructure implements them in tasks 07–08. `ExcelFile`/`EmailMessage` are records.

`src\DrivingLessons.Application\Abstractions\IExcelGenerator.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Abstractions;

public interface IExcelGenerator
{
    Task<ExcelFile> GenerateAsync(PublicationId publicationId, TeacherId teacherId);
}

public record ExcelFile(string FileName, byte[] Content, string ContentType);
```

`src\DrivingLessons.Application\Abstractions\IEmailSender.cs`:

```csharp
namespace DrivingLessons.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}

public record EmailMessage(string ToEmail, string Subject, string Body, ExcelFile Attachment);
```

`src\DrivingLessons.Application\Abstractions\IPublicationScheduler.cs` — takes raw `Guid`/`DateTimeOffset` (the scheduler is an infrastructure concern that keys Quartz jobs by the publication's raw id):

```csharp
namespace DrivingLessons.Application.Abstractions;

public interface IPublicationScheduler
{
    Task SchedulePublicationJobsAsync(Guid publicationId, DateTimeOffset startUtc, DateTimeOffset endUtc);

    Task RescheduleCloseAsync(Guid publicationId, DateTimeOffset endUtc);
}
```

- [ ] **Step 2: Not-found exception**

Two constructors — one for id-based resolution (the command interactors), one for week-based resolution (task-05's `GetPublicationInteractor`, which only knows the week). Mirrors the two-ctor shape of the existing `WeekScheduleNotFoundException`.

`src\DrivingLessons.Application\Common\Exceptions\PublicationNotFoundException.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common.Exceptions;

public class PublicationNotFoundException : NotFoundException
{
    public PublicationNotFoundException(PublicationId id)
        : base($"Publication {id.Value} was not found.")
    {
    }

    public PublicationNotFoundException(DateOnly weekStart)
        : base($"Publication for week {weekStart:yyyy-MM-dd} was not found.")
    {
    }
}
```

- [ ] **Step 3: Publish + Extend + Reopen interactors (scheduler after commit)**

Each resolves-or-throws, calls the domain method, commits, **then** calls the scheduler (never schedule before the state is durably persisted). `Publish` schedules both jobs; `ExtendWindow`/`Reopen` only reschedule the close.

`src\DrivingLessons.Application\Commands\PublishPublication\PublishPublicationRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.PublishPublication;

public record PublishPublicationRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
```

`src\DrivingLessons.Application\Commands\PublishPublication\PublishPublicationInteractor.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.PublishPublication;

public class PublishPublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationScheduler scheduler;
    private readonly IUnitOfWork unitOfWork;

    public PublishPublicationInteractor(
        IPublicationRepository repository,
        IPublicationScheduler scheduler,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.scheduler = scheduler;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var window = SubmissionWindow.Of(startUtc, endUtc);
        publication.Publish(window);

        await unitOfWork.CommitAsync();

        await scheduler.SchedulePublicationJobsAsync(id, startUtc, endUtc);
    }
}
```

`src\DrivingLessons.Application\Commands\ExtendPublicationWindow\ExtendPublicationWindowRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ExtendPublicationWindow;

public record ExtendPublicationWindowRequest(DateTimeOffset NewEndUtc);
```

`src\DrivingLessons.Application\Commands\ExtendPublicationWindow\ExtendPublicationWindowInteractor.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ExtendPublicationWindow;

public class ExtendPublicationWindowInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationScheduler scheduler;
    private readonly IUnitOfWork unitOfWork;

    public ExtendPublicationWindowInteractor(
        IPublicationRepository repository,
        IPublicationScheduler scheduler,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.scheduler = scheduler;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, DateTimeOffset newEndUtc)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        publication.ExtendWindow(newEndUtc);

        await unitOfWork.CommitAsync();

        await scheduler.RescheduleCloseAsync(id, newEndUtc);
    }
}
```

`src\DrivingLessons.Application\Commands\ReopenPublication\ReopenPublicationRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ReopenPublication;

public record ReopenPublicationRequest(DateTimeOffset NewEndUtc);
```

`src\DrivingLessons.Application\Commands\ReopenPublication\ReopenPublicationInteractor.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReopenPublication;

public class ReopenPublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationScheduler scheduler;
    private readonly IUnitOfWork unitOfWork;

    public ReopenPublicationInteractor(
        IPublicationRepository repository,
        IPublicationScheduler scheduler,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.scheduler = scheduler;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, DateTimeOffset newEndUtc)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        publication.Reopen(newEndUtc);

        await unitOfWork.CommitAsync();

        await scheduler.RescheduleCloseAsync(id, newEndUtc);
    }
}
```

- [ ] **Step 4: Open interactor (internal — Quartz/reconciliation only, no scheduler call)**

`src\DrivingLessons.Application\Commands\OpenPublication\OpenPublicationInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.OpenPublication;

public class OpenPublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public OpenPublicationInteractor(IPublicationRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        publication.Open();

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 5: Close interactor (resolves teacher ids via `IPublicationQueries`)**

The aggregate never queries other aggregates (decision #6): the interactor resolves the teachers that prepared a WeekSchedule for the week and passes their typed ids to `Close`. `CommitAsync` raises `PublicationClosed`, which task-05's handler turns into per-teacher Excel + email. `IPublicationQueries` is declared in task-05 (see the ordering note above).

`src\DrivingLessons.Application\Commands\ClosePublication\ClosePublicationInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ClosePublication;

public class ClosePublicationInteractor
{
    private readonly IPublicationRepository repository;
    private readonly IPublicationQueries publicationQueries;
    private readonly IUnitOfWork unitOfWork;

    public ClosePublicationInteractor(
        IPublicationRepository repository,
        IPublicationQueries publicationQueries,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.publicationQueries = publicationQueries;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var publicationId = PublicationId.Of(id);

        var publication = await repository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var weekStart = publication.WeekStart.Value;
        var teacherGuids = await publicationQueries.GetTeacherIdsWithScheduleForWeekAsync(weekStart);
        var teacherIds = teacherGuids.Select(TeacherId.Of);

        publication.Close(teacherIds);

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 6: Register interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the usings and register the five interactors after the domain-event handler from task 03:

```csharp
using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Application.Commands.ExtendPublicationWindow;
using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Application.Commands.PublishPublication;
using DrivingLessons.Application.Commands.ReopenPublication;
```

```csharp
services.AddScoped<PublishPublicationInteractor>();
services.AddScoped<ExtendPublicationWindowInteractor>();
services.AddScoped<ReopenPublicationInteractor>();
services.AddScoped<OpenPublicationInteractor>();
services.AddScoped<ClosePublicationInteractor>();
```

- [ ] **Step 7: Build**

Run: `dotnet build`
Expected once task-05 has added `IPublicationQueries`: build succeeds. If you run it before task-05, the only errors are `IPublicationQueries` not found in `ClosePublicationInteractor.cs` and `DependencyInjection.cs` (CS0246) — continue straight into task-05, then build green.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Application
git commit -m "feat(app): publication command interactors and excel/email/scheduler seams"
```

---

**Next:** [task-05-application-queries-handlers.md](task-05-application-queries-handlers.md)
