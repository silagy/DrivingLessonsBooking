# Task 8 of 14: Infrastructure — Quartz scheduling + reconciliation

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–7 complete (through the Excel/email services). Work on branch `9-us-08-21-publications-module`, all commands from the repo root.

This task builds the automatic window open/close (US-16) with **Quartz.NET**: two `IJob`s that call the `Open`/`Close` interactors, a `PublicationScheduler` that registers deterministic per-publication jobs at publish and reschedules the close on extend/reopen, and a startup `IHostedService` that reconciles boundaries missed while the app was down. It implements the `IPublicationScheduler` seam that `PublishPublicationInteractor`/`ExtendPublicationWindowInteractor`/`ReopenPublicationInteractor` already depend on (task-04).

Read `src\DrivingLessons.Infrastructure\DependencyInjection.cs` and `src\DrivingLessons.Presentation.Web\Program.cs` first: `AddInfrastructure(builder.Configuration)` is already called before `builder.Build()`, and the generic host runs hosted services after the `Program.cs` migration block, so registering Quartz + the reconciliation service inside `AddInfrastructure` needs **no** `Program.cs` change.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj` (add `Quartz`, `Quartz.Extensions.Hosting`)
- Create: `src\DrivingLessons.Infrastructure\Scheduling\OpenPublicationJob.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\ClosePublicationJob.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\PublicationScheduler.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\PublicationReconciliationHostedService.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`

---

- [ ] **Step 1: Add the Quartz packages**

Run:

```bash
dotnet add src\DrivingLessons.Infrastructure package Quartz
dotnet add src\DrivingLessons.Infrastructure package Quartz.Extensions.Hosting
```

Expected: two `PackageReference`s added to `DrivingLessons.Infrastructure.csproj` (current stable line `3.13.*`):

```xml
<PackageReference Include="Quartz" Version="3.13.1" />
<PackageReference Include="Quartz.Extensions.Hosting" Version="3.13.1" />
```

- [ ] **Step 2: The jobs**

`Scheduling\OpenPublicationJob.cs` — reads the publication id from the `JobDataMap` (stored as a string, parsed back), resolves `OpenPublicationInteractor` from DI (Quartz's Microsoft-DI job factory runs each fire in its own scope, so the scoped interactor and its `DbContext` resolve cleanly), and swallows a `DomainException` from a stale fire (e.g. a job that fires after the publication was already opened/closed/reopened — the guard `MustBePublished` throws, the job no-ops).

```csharp
using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Domain.Common;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class OpenPublicationJob : IJob
{
    public const string PublicationIdKey = "publicationId";

    private readonly OpenPublicationInteractor interactor;

    public OpenPublicationJob(OpenPublicationInteractor interactor)
    {
        this.interactor = interactor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var raw = context.MergedJobDataMap.GetString(PublicationIdKey);
        var publicationId = Guid.Parse(raw!);

        try
        {
            await interactor.ExecuteAsync(publicationId);
        }
        catch (DomainException)
        {
        }
    }
}
```

`Scheduling\ClosePublicationJob.cs` — same shape for the close side. This is the job whose stale-fire tolerance backs the extend/reopen race (plan risk #4): after an extend or reopen reschedules the close, an already-queued old close can still fire; `MustBeOpen` throws, the `catch` swallows it.

```csharp
using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Domain.Common;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class ClosePublicationJob : IJob
{
    public const string PublicationIdKey = "publicationId";

    private readonly ClosePublicationInteractor interactor;

    public ClosePublicationJob(ClosePublicationInteractor interactor)
    {
        this.interactor = interactor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var raw = context.MergedJobDataMap.GetString(PublicationIdKey);
        var publicationId = Guid.Parse(raw!);

        try
        {
            await interactor.ExecuteAsync(publicationId);
        }
        catch (DomainException)
        {
        }
    }
}
```

`DomainException` is the abstract base in `DrivingLessons.Domain.Common`; every publication guard (`PublicationMustBePublishedException`, `PublicationMustBeOpenException`, …) derives from it, so one `catch` covers every stale-fire case.

- [ ] **Step 3: The scheduler**

`Scheduling\PublicationScheduler.cs` implements `IPublicationScheduler` (`Application\Abstractions`) over `ISchedulerFactory` (registered as a singleton by `AddQuartz`). Job and trigger keys are **deterministic** per publication id (group `publications`, name `open-{id}`/`close-{id}`) so extend/reopen can find and replace the exact close trigger. The publication id travels in the job data map as a string.

```csharp
using DrivingLessons.Application.Abstractions;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

public class PublicationScheduler : IPublicationScheduler
{
    private const string JobGroup = "publications";

    private readonly ISchedulerFactory schedulerFactory;

    public PublicationScheduler(ISchedulerFactory schedulerFactory)
    {
        this.schedulerFactory = schedulerFactory;
    }

    public async Task SchedulePublicationJobsAsync(Guid publicationId, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var openJob = BuildJob<OpenPublicationJob>(publicationId, OpenKey(publicationId));
        var openTrigger = BuildTrigger(OpenKey(publicationId), startUtc);
        await scheduler.ScheduleJob(openJob, openTrigger);

        var closeJob = BuildJob<ClosePublicationJob>(publicationId, CloseKey(publicationId));
        var closeTrigger = BuildTrigger(CloseKey(publicationId), endUtc);
        await scheduler.ScheduleJob(closeJob, closeTrigger);
    }

    public async Task RescheduleCloseAsync(Guid publicationId, DateTimeOffset endUtc)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var closeKey = CloseKey(publicationId);
        var triggerKey = new TriggerKey(closeKey.Name, closeKey.Group);
        var newTrigger = BuildTrigger(closeKey, endUtc);

        var exists = await scheduler.CheckExists(triggerKey);

        if (exists)
        {
            await scheduler.RescheduleJob(triggerKey, newTrigger);
            return;
        }

        var closeJob = BuildJob<ClosePublicationJob>(publicationId, closeKey);
        await scheduler.ScheduleJob(closeJob, newTrigger);
    }

    private static IJobDetail BuildJob<TJob>(Guid publicationId, JobKey jobKey)
        where TJob : IJob
    {
        return JobBuilder
                   .Create<TJob>()
                   .WithIdentity(jobKey)
                   .UsingJobData(OpenPublicationJob.PublicationIdKey, publicationId.ToString())
                   .Build();
    }

    private static ITrigger BuildTrigger(JobKey jobKey, DateTimeOffset fireAtUtc)
    {
        return TriggerBuilder
                   .Create()
                   .WithIdentity(jobKey.Name, jobKey.Group)
                   .StartAt(fireAtUtc)
                   .Build();
    }

    private static JobKey OpenKey(Guid publicationId)
    {
        return new JobKey($"open-{publicationId}", JobGroup);
    }

    private static JobKey CloseKey(Guid publicationId)
    {
        return new JobKey($"close-{publicationId}", JobGroup);
    }
}
```

The trigger shares the job's name/group for a stable one-to-one identity; `RescheduleJob` replaces the pending close in place, and the fallback `ScheduleJob` covers reopen (whose close trigger no longer exists after the prior close fired). The data-map key constant is identical (`"publicationId"`) on both jobs, so referencing `OpenPublicationJob.PublicationIdKey` when building either job is safe.

- [ ] **Step 4: The reconciliation hosted service**

`Scheduling\PublicationReconciliationHostedService.cs`. On startup, in a fresh DI scope: open every `Published` publication whose start is already past, close every `Open` publication whose end is already past (both via the interactors, so the same domain guards + events + emails fire as a live boundary would), then register Quartz jobs for the windows still in the future. `TimeProvider` is injected (registered in Step 5).

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrivingLessons.Infrastructure.Scheduling;

public class PublicationReconciliationHostedService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;

    public PublicationReconciliationHostedService(IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var repository = provider.GetRequiredService<IPublicationRepository>();
        var openInteractor = provider.GetRequiredService<OpenPublicationInteractor>();
        var closeInteractor = provider.GetRequiredService<ClosePublicationInteractor>();
        var scheduler = provider.GetRequiredService<IPublicationScheduler>();

        var nowUtc = timeProvider.GetUtcNow();

        var dueToOpen = await repository.GetPublishedDueToOpenAsync(nowUtc);
        foreach (var publication in dueToOpen)
        {
            await openInteractor.ExecuteAsync(publication.Id.Value);
        }

        var dueToClose = await repository.GetOpenDueToCloseAsync(nowUtc);
        foreach (var publication in dueToClose)
        {
            await closeInteractor.ExecuteAsync(publication.Id.Value);
        }

        await RegisterFutureJobsAsync(repository, scheduler);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task RegisterFutureJobsAsync(IPublicationRepository repository, IPublicationScheduler scheduler)
    {
        var pendingOpen = await repository.GetPublishedDueToOpenAsync(DateTimeOffset.MaxValue);
        foreach (var publication in pendingOpen)
        {
            var window = publication.Window!;
            await scheduler.SchedulePublicationJobsAsync(publication.Id.Value, window.StartUtc, window.EndUtc);
        }

        var pendingClose = await repository.GetOpenDueToCloseAsync(DateTimeOffset.MaxValue);
        foreach (var publication in pendingClose)
        {
            var window = publication.Window!;
            await scheduler.RescheduleCloseAsync(publication.Id.Value, window.EndUtc);
        }
    }
}
```

After the catch-up passes, the two `DateTimeOffset.MaxValue` scans return every publication still `Published` (future opens → schedule open + close) and every publication still `Open` (future closes → schedule/replace the close). The states are disjoint, so no job is double-registered; Quartz keys are deterministic, so a re-run on the next restart replaces rather than duplicates.

- [ ] **Step 5: DI registration**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, register `TimeProvider`, the Quartz host, the two jobs, the scheduler, and the reconciliation service. Add near the other registrations:

```csharp
services.AddSingleton(TimeProvider.System);

services.AddQuartz();
services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

services.AddTransient<OpenPublicationJob>();
services.AddTransient<ClosePublicationJob>();
services.AddScoped<IPublicationScheduler, PublicationScheduler>();

services.AddHostedService<PublicationReconciliationHostedService>();
```

Add the usings: `using Quartz;`, `using DrivingLessons.Infrastructure.Scheduling;`. (`DrivingLessons.Application.Abstractions` is already imported from task-07.) `AddQuartz` registers `ISchedulerFactory` as a singleton and uses the Microsoft-DI job factory by default, so the transient jobs resolve their scoped interactors per fire.

- [ ] **Step 6: Build**

Run: `dotnet build`

Expected: `Build succeeded.` with zero errors.

- [ ] **Step 7: Run domain tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`

Expected: all tests PASS.

- [ ] **Step 8: Restart-reconciliation test (documented; full E2E in task 14)**

Verify the missed-boundary catch-up runs on startup:

1. Ensure `Email:Enabled=false` (task-07) and the app points at a running Postgres (`appsettings.Development.json`).
2. Prepare a week-schedule for a teacher, publish its Publication (task-04 endpoint / Scalar) with a window whose **end is a minute or two in the future**.
3. Stop the app before the close fires.
4. Wait until the window end is in the past, then start the app: `dotnet run --project src\DrivingLessons.Presentation.Web`.
5. Expected in the startup logs: the reconciliation service closes the overdue Publication and the `LoggingEmailSender` logs one `Email disabled. Skipped send…` line per teacher with subject `Week {n} Requests - {teacher} - v1`. `GET api/publications/by-week` then reports state `Closed`.
6. Repeat with the window **start** in the past but **end** in the future: on start the Publication is opened and a future close job is registered (it fires automatically at the end time).

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Infrastructure
git commit -m "feat(infra): quartz open/close jobs, scheduler and startup reconciliation"
```

---

**Next:** [task-09-controllers.md](task-09-controllers.md)
