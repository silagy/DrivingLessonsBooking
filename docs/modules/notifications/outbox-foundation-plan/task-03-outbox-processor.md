# Task 3 of 6: The outbox processor on Quartz

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`. Requires tasks 1 and 2 committed.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\Options\OutboxOptions.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxProcessor.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\ProcessOutboxJob.cs`
- Create: `src\DrivingLessons.Infrastructure\Scheduling\PurgeOutboxJob.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (options, processor, jobs, Quartz schedule)
- Modify: `src\DrivingLessons.Presentation.Web\appsettings.json` (`Outbox` section)
- Test: `tests\DrivingLessons.Application.Test\Outbox\OutboxOptionsTest.cs` (new)

**Interfaces:**
- Consumes: task 1's `IntegrationEventCatalog` (`TypeOf`, `ConsumerOf`), `IntegrationEventCatalogException`, `IntegrationEventConsumerRegistration.InvokeAsync` and `DeliveryOutcome`; task 2's `OutboxDelivery` (`Succeed`, `Fail`), `OutboxMessage`, `OutboxSerializer.Deserialize`, `DrivingLessonsDbContext.OutboxDeliveries` / `.OutboxMessages`; Quartz 3.18.2 (`AddQuartz`, `ScheduleJob<T>`, `[DisallowConcurrentExecution]`).
- Produces:
  - `OutboxOptions` (section `Outbox`): `PollSeconds` 5, `BatchSize` 20, `MaxAttempts` 8, `LeaseSeconds` 300, `RetentionDays` 30, all validated with `[Range]` at startup.
  - `OutboxProcessor.ProcessDueAsync(CancellationToken)`: leases due deliveries and runs each once. `OutboxProcessor.PurgeAsync()`: deletes old succeeded deliveries and orphaned messages.
  - `ProcessOutboxJob` every `PollSeconds`, `PurgeOutboxJob` every hour, both `[DisallowConcurrentExecution]`, scheduled at startup from configuration (README decision 13).

After this task the job runs every 5 seconds against empty tables. Task 4 gives it the first event.

- [ ] **Step 1: Write the failing options test**

Create `tests\DrivingLessons.Application.Test\Outbox\OutboxOptionsTest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Infrastructure.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxOptionsTest
{
    [TestMethod]
    public void The_Defaults_Are_Valid()
    {
        //given
        var options = new OutboxOptions();

        //when
        var errors = Validate(options);

        //then
        errors.ShouldBeEmpty();
        options.PollSeconds.ShouldBe(5);
        options.BatchSize.ShouldBe(20);
        options.MaxAttempts.ShouldBe(8);
        options.LeaseSeconds.ShouldBe(300);
        options.RetentionDays.ShouldBe(30);
    }

    [TestMethod]
    [DataRow(0, 20, 8, 300, 30, nameof(OutboxOptions.PollSeconds))]
    [DataRow(5, 0, 8, 300, 30, nameof(OutboxOptions.BatchSize))]
    [DataRow(5, 20, 0, 300, 30, nameof(OutboxOptions.MaxAttempts))]
    [DataRow(5, 20, 8, 10, 30, nameof(OutboxOptions.LeaseSeconds))]
    [DataRow(5, 20, 8, 300, 0, nameof(OutboxOptions.RetentionDays))]
    public void Out_Of_Range_Settings_Are_Refused(int pollSeconds, int batchSize, int maxAttempts, int leaseSeconds, int retentionDays, string member)
    {
        //given
        var options = new OutboxOptions
        {
            PollSeconds = pollSeconds,
            BatchSize = batchSize,
            MaxAttempts = maxAttempts,
            LeaseSeconds = leaseSeconds,
            RetentionDays = retentionDays
        };

        //when
        var errors = Validate(options);

        //then
        errors.ShouldHaveSingleItem().MemberNames.ShouldBe([member]);
    }

    private static List<ValidationResult> Validate(OutboxOptions options)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        return results;
    }
}
```

- [ ] **Step 2: Create the options and the processor**

`src\DrivingLessons.Infrastructure\Options\OutboxOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace DrivingLessons.Infrastructure.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(1, 3600)]
    public int PollSeconds { get; init; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 20;

    [Range(1, 50)]
    public int MaxAttempts { get; init; } = 8;

    [Range(30, 3600)]
    public int LeaseSeconds { get; init; } = 300;

    [Range(1, 365)]
    public int RetentionDays { get; init; } = 30;
}
```

`src\DrivingLessons.Infrastructure\Outbox\OutboxProcessor.cs`:

```csharp
using System.Text.Json;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxProcessor
{
    private const int PurgeBatchSize = 500;

    private readonly DrivingLessonsDbContext context;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IntegrationEventCatalog catalog;
    private readonly OutboxOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OutboxProcessor> logger;

    public OutboxProcessor(
        DrivingLessonsDbContext context,
        IServiceScopeFactory scopeFactory,
        IntegrationEventCatalog catalog,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        this.context = context;
        this.scopeFactory = scopeFactory;
        this.catalog = catalog;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        var deliveryIds = await LeaseDueAsync();

        foreach (var deliveryId in deliveryIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await DeliverAsync(deliveryId);
        }
    }

    public async Task PurgeAsync()
    {
        var nowUtc = timeProvider.GetUtcNow();
        var cutoffUtc = nowUtc.AddDays(-options.RetentionDays);
        var succeeded = nameof(OutboxDeliveryState.Succeeded);

        await context.Database.ExecuteSqlAsync(
            $"""
             DELETE FROM outbox_deliveries
             WHERE id IN (
                 SELECT id FROM outbox_deliveries
                 WHERE state = {succeeded} AND completed_at_utc < {cutoffUtc}
                 LIMIT {PurgeBatchSize})
             """);

        await context.Database.ExecuteSqlAsync(
            $"""
             DELETE FROM outbox_messages AS m
             WHERE m.occurred_at_utc < {cutoffUtc}
               AND NOT EXISTS (SELECT 1 FROM outbox_deliveries AS d WHERE d.message_id = m.id)
             """);
    }

    private async Task<List<Guid>> LeaseDueAsync()
    {
        var nowUtc = timeProvider.GetUtcNow();
        var leasedUntilUtc = nowUtc.AddSeconds(options.LeaseSeconds);
        var pending = nameof(OutboxDeliveryState.Pending);
        var batchSize = options.BatchSize;

        return await context.Database
                            .SqlQuery<Guid>(
                                $"""
                                 UPDATE outbox_deliveries
                                 SET locked_until_utc = {leasedUntilUtc}
                                 WHERE id IN (
                                     SELECT id FROM outbox_deliveries
                                     WHERE state = {pending}
                                       AND next_attempt_at_utc <= {nowUtc}
                                       AND (locked_until_utc IS NULL OR locked_until_utc < {nowUtc})
                                     ORDER BY next_attempt_at_utc
                                     LIMIT {batchSize}
                                     FOR UPDATE SKIP LOCKED)
                                 RETURNING id AS "Value"
                                 """)
                            .ToListAsync();
    }

    private async Task DeliverAsync(Guid deliveryId)
    {
        var delivery = await context.OutboxDeliveries.SingleAsync(x => x.Id == deliveryId);
        var message = await context.OutboxMessages.SingleAsync(x => x.Id == delivery.MessageId);

        IntegrationEventConsumerRegistration registration;
        IIntegrationEvent integrationEvent;

        try
        {
            registration = catalog.ConsumerOf(message.EventName, delivery.Consumer);
            var eventType = catalog.TypeOf(message.EventName);
            integrationEvent = OutboxSerializer.Deserialize(message.Payload, eventType);
        }
        catch (Exception exception) when (exception is IntegrationEventCatalogException or JsonException)
        {
            var deadAtUtc = timeProvider.GetUtcNow();
            delivery.MarkDead(exception, deadAtUtc);

            LogFailure(exception, message, delivery);

            await context.SaveChangesAsync();
            return;
        }

        await RunConsumerAsync(registration, integrationEvent, message, delivery);

        await context.SaveChangesAsync();
    }

    private async Task RunConsumerAsync(
        IntegrationEventConsumerRegistration registration,
        IIntegrationEvent integrationEvent,
        OutboxMessage message,
        OutboxDelivery delivery)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var outcome = await registration.InvokeAsync(scope.ServiceProvider, integrationEvent);

            Apply(outcome, delivery);
        }
        catch (Exception exception)
        {
            var failedAtUtc = timeProvider.GetUtcNow();
            delivery.Fail(exception, failedAtUtc, options.MaxAttempts);

            LogFailure(exception, message, delivery);
        }
    }

    private void Apply(DeliveryOutcome outcome, OutboxDelivery delivery)
    {
        if (outcome.HoldUntilUtc is { } holdUntilUtc)
        {
            delivery.Hold(holdUntilUtc);
            return;
        }

        var completedAtUtc = timeProvider.GetUtcNow();
        delivery.Succeed(completedAtUtc);
    }

    private void LogFailure(Exception exception, OutboxMessage message, OutboxDelivery delivery)
    {
        if (delivery.State == OutboxDeliveryState.Dead)
        {
            logger.LogError(
                exception,
                "Outbox delivery [{DeliveryId}] of [{EventName}] to [{Consumer}] is dead after [{Attempts}] attempts.",
                delivery.Id,
                message.EventName,
                delivery.Consumer,
                delivery.Attempts);

            return;
        }

        logger.LogWarning(
            exception,
            "Outbox delivery [{DeliveryId}] of [{EventName}] to [{Consumer}] failed on attempt [{Attempts}], next at [{NextAttemptAtUtc}].",
            delivery.Id,
            message.EventName,
            delivery.Consumer,
            delivery.Attempts,
            delivery.NextAttemptAtUtc);
    }
}
```

How it works:
- **Lease:** one `UPDATE … RETURNING` statement under Postgres auto-commit. `FOR UPDATE SKIP LOCKED` makes a second instance skip rows the first one is leasing; the row lock ends with the statement, and `locked_until_utc` keeps them away for `LeaseSeconds` (README decision 7). `SqlQuery<Guid>` with `AS "Value"` is EF Core's scalar form; nothing composes over it, so EF sends the SQL as written.
- **Run:** each delivery's consumer runs in a **new DI scope**, so its `DbContext`, repositories and `IUnitOfWork` are its own. The processor's context only tracks the delivery and message rows, and saves them after the consumer returns or throws.
- **Can't be resolved → dead at once** (README decision 17): an event name or consumer name the catalog doesn't know (a consumer removed or renamed, an event no longer registered), or a payload that doesn't deserialize, will fail the same way on every attempt. `MarkDead` records it after one attempt and logs it at Error, instead of retrying 8 times over two hours.
- **The consumer's outcome** (README decision 16): `Done` → `Succeed`; `HoldUntil(instant)` → `Hold`, which keeps the delivery `Pending` until that instant without counting an attempt. A hold isn't logged: it's planned behavior (slice 5's quiet hours), not a problem.
- **Catch-all around the consumer:** any exception the consumer throws is a failed attempt, recorded on the row and logged, and never stops the batch or the job. This is the one place a broad `catch (Exception)` is right: the processor doesn't know what consumers throw. The narrower `catch … when` above runs first, before the consumer, so a catalog or JSON problem never counts as a consumer failure.
- **Crash:** if the process dies inside `RunConsumerAsync`, the lease expires and the delivery runs again (at least once, README decision 9).

If `SqlQuery` turns out to wrap the `UPDATE` in a sub-select at runtime (task 6 Step 3 shows a Postgres syntax error in the log), run the same SQL through `context.Database.GetDbConnection()` with an `NpgsqlCommand` and read the `id`s from its reader. Keep the statement unchanged.

- [ ] **Step 3: Create the jobs**

`src\DrivingLessons.Infrastructure\Scheduling\ProcessOutboxJob.cs`:

```csharp
using DrivingLessons.Infrastructure.Outbox;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class ProcessOutboxJob : IJob
{
    public const string JobGroup = "outbox";

    private readonly OutboxProcessor processor;

    public ProcessOutboxJob(OutboxProcessor processor)
    {
        this.processor = processor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await processor.ProcessDueAsync(context.CancellationToken);
    }
}
```

`src\DrivingLessons.Infrastructure\Scheduling\PurgeOutboxJob.cs`:

```csharp
using DrivingLessons.Infrastructure.Outbox;
using Quartz;

namespace DrivingLessons.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public class PurgeOutboxJob : IJob
{
    private readonly OutboxProcessor processor;

    public PurgeOutboxJob(OutboxProcessor processor)
    {
        this.processor = processor;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await processor.PurgeAsync();
    }
}
```

`OpenPublicationJob` swallows `DomainException` because its interactor may legitimately refuse. These jobs let an exception from the lease or the purge (database down) reach Quartz, which logs it; the next run tries again.

- [ ] **Step 4: Register and schedule**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`:

1. After the `EmailOptions` registration add:

```csharp
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
```

2. Replace `services.AddQuartz();` with:

```csharp
        var outboxSection = configuration.GetSection(OutboxOptions.SectionName);
        var outboxOptions = outboxSection.Get<OutboxOptions>() ?? new OutboxOptions();

        services.AddQuartz(quartz =>
        {
            quartz.ScheduleJob<ProcessOutboxJob>(
                trigger => trigger
                           .WithIdentity("process-outbox", ProcessOutboxJob.JobGroup)
                           .StartNow()
                           .WithSimpleSchedule(schedule => schedule
                                                           .WithIntervalInSeconds(outboxOptions.PollSeconds)
                                                           .RepeatForever()),
                job => job.WithIdentity("process-outbox", ProcessOutboxJob.JobGroup));

            quartz.ScheduleJob<PurgeOutboxJob>(
                trigger => trigger
                           .WithIdentity("purge-outbox", ProcessOutboxJob.JobGroup)
                           .StartNow()
                           .WithSimpleSchedule(schedule => schedule
                                                           .WithIntervalInHours(1)
                                                           .RepeatForever()),
                job => job.WithIdentity("purge-outbox", ProcessOutboxJob.JobGroup));
        });
```

3. After `services.AddTransient<ClosePublicationJob>();` add:

```csharp
        services.AddScoped<OutboxProcessor>();
        services.AddTransient<ProcessOutboxJob>();
        services.AddTransient<PurgeOutboxJob>();
```

The interval is read once at startup to build the trigger; the same values are bound and validated through `IOptions<OutboxOptions>` for the processor. An invalid `PollSeconds` still fails startup through `ValidateOnStart()`.

Quartz's DI job factory creates a scope per job run (the existing `OpenPublicationJob` already relies on it for its scoped interactor), so each run gets a fresh `OutboxProcessor` and `DbContext`.

- [ ] **Step 5: Configuration**

In `src\DrivingLessons.Presentation.Web\appsettings.json`, add after the `Email` section:

```json
  "Outbox": {
    "PollSeconds": 5,
    "BatchSize": 20,
    "MaxAttempts": 8,
    "LeaseSeconds": 300,
    "RetentionDays": 30
  },
```

Keep the JSON valid (commas). No environment variable is needed: these are defaults, and `Outbox__PollSeconds` etc. override them if ever needed.

- [ ] **Step 6: Run the tests and build**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~OutboxOptionsTest"
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: `OutboxOptionsTest` 6 passed; everything else passes.

- [ ] **Step 7: Start the app once**

Start the API against your local development database (as in `docs/development/running-the-project.md`) and watch the log for 30 seconds. Expected: it starts, applies `AddOutbox` if not applied, and logs no error from `ProcessOutboxJob` or `PurgeOutboxJob` (the catalog is registered by `AddApplication` since task 1, empty until task 4). Stop the app.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Options/OutboxOptions.cs src/DrivingLessons.Infrastructure/Outbox/OutboxProcessor.cs src/DrivingLessons.Infrastructure/Scheduling/ProcessOutboxJob.cs src/DrivingLessons.Infrastructure/Scheduling/PurgeOutboxJob.cs src/DrivingLessons.Infrastructure/DependencyInjection.cs src/DrivingLessons.Presentation.Web/appsettings.json tests/DrivingLessons.Application.Test/Outbox/OutboxOptionsTest.cs
git commit -m "feat(notifications): outbox processor and purge on Quartz"
```
