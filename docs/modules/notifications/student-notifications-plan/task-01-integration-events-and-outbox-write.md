# Task 1 of 18: Integration events and the outbox write, in the same transaction

> Part of [Student Notifications (stage 1)](README.md), milestone A. Work on branch `notifications-outbox`, commands from the repo root in bash.

**Files:**
- Create: `src\DrivingLessons.Application\IntegrationEvents\IIntegrationEvent.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IIntegrationEventOutbox.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IIntegrationEventConsumer.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\DeliveryOutcome.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\DeliveryAttempt.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventConsumerRegistration.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventServiceCollectionExtension.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxMessage.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxDelivery.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxDeliveryState.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxJson.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\IntegrationEventTypes.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\IntegrationEventOutbox.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxMessageConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxDeliveryConfiguration.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (`CommitAsync` in one transaction, two `DbSet`s)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_AddOutbox.cs` (+ Designer, snapshot)
- Test: `tests\DrivingLessons.Application.Test\Outbox\IntegrationEventOutboxTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Outbox\IntegrationEventTypesTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\IntegrationEvents\IntegrationEventServiceCollectionExtensionTest.cs` (new)
- Create: `docs\decisions\0010-integration-events-and-outbox.md`
- Modify: `.claude\rules\ddd-architecture.md`, `CLAUDE.md` (file location rows)

**Interfaces:**
- Consumes (on `main`): `DrivingLessonsDbContext.CommitAsync` / `SaveAndDispatchAsync` (cycles of save → dispatch); `IDomainEventHandler<T>`; `TimeProvider` (registered as `TimeProvider.System`); `ITeacherAndCarLocks` (may have opened a transaction before `CommitAsync`).
- Produces:
  - `interface IIntegrationEvent { static abstract string EventType { get; } static abstract int SchemaVersion { get; } }`
  - `interface IIntegrationEventOutbox { void Add<TEvent>(TEvent integrationEvent) where TEvent : IIntegrationEvent; }`, which persists at the next `CommitAsync`, inside its transaction.
  - `interface IIntegrationEventConsumer<in TEvent> { Task<DeliveryOutcome> HandleAsync(TEvent integrationEvent, DeliveryAttempt attempt); }`
  - `DeliveryOutcome.Done` · `DeliveryOutcome.HoldUntil(DateTimeOffset)` · `record DeliveryAttempt(int Number, bool IsLast)`
  - `services.AddIntegrationEventConsumer<TEvent, TConsumer>()`, which registers `TConsumer` scoped plus one singleton `IntegrationEventConsumerRegistration(Type EventType, Type ConsumerType)` with `ConsumerName => ConsumerType.Name`.
  - `IntegrationEventTypes.Resolve(string eventType, int schemaVersion) : Type?`. Task 2's processor uses it to deserialize.
  - Tables `outbox_messages` and `outbox_deliveries` (README decision 4).

No producer and no consumer yet: after this task the outbox exists and every commit is one transaction, but nothing writes to it. Task 2 delivers. Task 3 is the first producer.

- [ ] **Step 1: Write the failing outbox tests**

Create `tests\DrivingLessons.Application.Test\Outbox\IntegrationEventOutboxTest.cs`. It builds a real `DrivingLessonsDbContext` on Npgsql options that never connect (`UseNpgsql("Host=unused")`): `Add` only tracks entities, so the test reads `context.ChangeTracker.Entries<OutboxMessage>()`.

Two test-only events and consumers sit at the bottom of the file (`SampleHappened` with `EventType => "sample-happened"`, version 1, payload `(Guid SampleId, string Label)`, and `OtherHappened`). Tests:

| Test | Asserts |
|------|---------|
| `Writes_The_Event_Type_Version_And_Occurred_At` | one tracked `OutboxMessage` with `EventType == "sample-happened"`, `SchemaVersion == 1`, and `OccurredAtUtc` from a `FakeTimeProvider`-style fake (`A.Fake<TimeProvider>()` returning a fixed `GetUtcNow()`) |
| `Writes_The_Payload_As_Camel_Case_Json` | `Payload` deserializes back to an equal `SampleHappened`, and contains `"sampleId"` |
| `Writes_One_Delivery_Per_Consumer_Of_That_Event` | two registrations for `SampleHappened`, one for `OtherHappened` → two deliveries, consumers `FirstSampleConsumer`, `SecondSampleConsumer`, each `Pending`, `Attempts == 0`, `NextAttemptAtUtc == OccurredAtUtc` |
| `Writes_The_Message_Even_Without_Consumers` | no registration → one message, zero deliveries |

- [ ] **Step 2: Write the failing registry and registration tests**

`tests\DrivingLessons.Application.Test\Outbox\IntegrationEventTypesTest.cs`:
- `Every_Integration_Event_Has_A_Unique_Type_And_Version`: `IntegrationEventTypes.FromAssembly(typeof(IIntegrationEvent).Assembly)` builds without throwing. It is empty in this task, and tasks 3, 11, 12 and 15 add one event each, which this test then covers.
- `Resolves_A_Registered_Type`, and `Returns_Null_For_An_Unknown_Type_Or_Version`, both on the test assembly's `SampleHappened`.
- `Two_Events_With_The_Same_Type_And_Version_Throw`, using two test events declaring `"duplicate"` v1 → `InvalidOperationException` naming both CLR types.

`tests\DrivingLessons.Application.Test\IntegrationEvents\IntegrationEventServiceCollectionExtensionTest.cs`:
- `Registers_The_Consumer_As_Scoped_And_Its_Registration`: a `ServiceCollection` after `AddIntegrationEventConsumer<SampleHappened, FirstSampleConsumer>()` holds a scoped `FirstSampleConsumer` descriptor and a singleton `IntegrationEventConsumerRegistration` with `ConsumerName == "FirstSampleConsumer"`.

- [ ] **Step 3: Run the tests to see them fail**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Outbox|FullyQualifiedName~IntegrationEvent"`
Expected: FAIL. The build breaks with `CS0246: The type or namespace name 'IIntegrationEvent' could not be found`.

- [ ] **Step 4: Add the Application contracts**

`src\DrivingLessons.Application\IntegrationEvents\IIntegrationEvent.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEvent
{
    static abstract string EventType { get; }

    static abstract int SchemaVersion { get; }
}
```

`IIntegrationEventOutbox.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEventOutbox
{
    void Add<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent;
}
```

`IIntegrationEventConsumer.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<DeliveryOutcome> HandleAsync(TEvent integrationEvent, DeliveryAttempt attempt);
}
```

`DeliveryOutcome.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public record DeliveryOutcome
{
    public static DeliveryOutcome Done { get; } = new(null);

    public DateTimeOffset? HoldUntilUtc { get; }

    public bool IsHeld => HoldUntilUtc is not null;

    private DeliveryOutcome(DateTimeOffset? holdUntilUtc)
    {
        HoldUntilUtc = holdUntilUtc;
    }

    public static DeliveryOutcome HoldUntil(DateTimeOffset instantUtc)
    {
        return new DeliveryOutcome(instantUtc);
    }
}
```

`DeliveryAttempt.cs`: `public record DeliveryAttempt(int Number, bool IsLast);`

`IntegrationEventConsumerRegistration.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public record IntegrationEventConsumerRegistration(Type EventType, Type ConsumerType)
{
    public string ConsumerName => ConsumerType.Name;
}
```

`IntegrationEventServiceCollectionExtension.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application.IntegrationEvents;

public static class IntegrationEventServiceCollectionExtension
{
    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        var registration = new IntegrationEventConsumerRegistration(typeof(TEvent), typeof(TConsumer));

        services.AddScoped<TConsumer>();
        services.AddSingleton(registration);

        return services;
    }
}
```

- [ ] **Step 5: Add the Infrastructure outbox records**

`Infrastructure\Outbox\OutboxDeliveryState.cs`: `public enum OutboxDeliveryState { Pending = 10, Processed = 20, Dead = 30 }`.

`OutboxMessage.cs`: private setters, a private parameterless constructor for EF, and `static OutboxMessage Create(string eventType, int schemaVersion, string payload, DateTimeOffset occurredAtUtc, IEnumerable<string> consumerNames)`. `Create` generates `Guid.CreateVersion7()` and creates one `OutboxDelivery` per consumer name (`OutboxDelivery.Create(messageId, consumerName, occurredAtUtc)`). `Deliveries` is an `IReadOnlyCollection<OutboxDelivery>` over a private list.

`OutboxDelivery.cs`: `Id`, `MessageId`, `Consumer`, `State`, `Attempts`, `NextAttemptAtUtc`, `LastError`, `ProcessedAtUtc`. `Create` sets `State = Pending`, `Attempts = 0` and `NextAttemptAtUtc = occurredAtUtc`. The state methods (`MarkProcessed`, `Hold`, `MarkFailed`, `MarkDead`) are added in task 2, together with their tests.

`OutboxJson.cs`: `public static class OutboxJson { public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web); }`.

`IntegrationEventTypes.cs`: `static IntegrationEventTypes FromAssembly(Assembly assembly)` scans the non-abstract types implementing `IIntegrationEvent`. It reads each type's static `EventType` and `SchemaVersion` by reflection (`type.GetProperty(nameof(IIntegrationEvent.EventType), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)`), keys a dictionary by `(eventType, schemaVersion)`, and throws `InvalidOperationException($"Integration events {a} and {b} share type '{t}' v{v}.")` on a duplicate. `Resolve(string, int) : Type?`.

`IntegrationEventOutbox.cs`:

```csharp
using System.Text.Json;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;

namespace DrivingLessons.Infrastructure.Outbox;

public class IntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly DrivingLessonsDbContext context;
    private readonly IEnumerable<IntegrationEventConsumerRegistration> registrations;
    private readonly TimeProvider timeProvider;

    public IntegrationEventOutbox(
        DrivingLessonsDbContext context,
        IEnumerable<IntegrationEventConsumerRegistration> registrations,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.registrations = registrations;
        this.timeProvider = timeProvider;
    }

    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent
    {
        var consumerNames = registrations
                            .Where(x => x.EventType == typeof(TEvent))
                            .Select(x => x.ConsumerName);

        var payload = JsonSerializer.Serialize(integrationEvent, OutboxJson.Options);
        var occurredAtUtc = timeProvider.GetUtcNow();

        var message = OutboxMessage.Create(TEvent.EventType, TEvent.SchemaVersion, payload, occurredAtUtc, consumerNames);

        context.OutboxMessages.Add(message);
    }
}
```

- [ ] **Step 6: Map the tables**

`OutboxMessageConfiguration`: table `outbox_messages`; `id` (key), `event_type` (`text`, required), `schema_version`, `payload` (`jsonb`), `occurred_at_utc` (`timestamptz`); `HasMany(x => x.Deliveries).WithOne().HasForeignKey(x => x.MessageId).OnDelete(Cascade)`; navigation via field.

`OutboxDeliveryConfiguration`: table `outbox_deliveries`; `id`, `message_id`, `consumer` (`text`), `state`, `attempts`, `next_attempt_at_utc` (`timestamptz`), `last_error` (`text`, nullable), `processed_at_utc` (`timestamptz`, nullable); unique index `(message_id, consumer)`; index `(state, next_attempt_at_utc)` named `ix_outbox_deliveries_due`.

`DrivingLessonsDbContext` gains `public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();` and `public DbSet<OutboxDelivery> OutboxDeliveries => Set<OutboxDelivery>();`.

- [ ] **Step 7: Put `CommitAsync` in one transaction**

Replace `CommitAsync` in `DrivingLessonsDbContext.cs` with:

```csharp
    public async Task CommitAsync()
    {
        await using var transaction = Database.CurrentTransaction ?? await Database.BeginTransactionAsync();

        try
        {
            await SaveAndDispatchAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrentChangeException(exception);
        }

        await transaction.CommitAsync();
    }
```

Why this is enough:
- When `TeacherAndCarLocks` already opened the transaction, `CurrentTransaction` returns it and the behavior is today's (commit at the end).
- When nothing did, every `SaveChangesAsync` of every cycle joins the new transaction. An exception anywhere (a handler, a later cycle, the outbox insert) leaves `CommitAsync` before `CommitAsync()` on the transaction, and `await using` disposes it, which rolls back.
- `SaveAndDispatchAsync` already starts each cycle with `SaveChangesAsync`, so outbox rows a handler adds in the last cycle are saved by the next one, before `roots.Count == 0` returns.

`PublicationClosedHandler` still emails inline in this task, now **inside** the transaction. That was already the case under the locks. Task 3 removes it, before milestone A merges.

- [ ] **Step 8: Register and migrate**

In `Infrastructure\DependencyInjection.cs`, after the `IUnitOfWork` line:

```csharp
        services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
        services.AddSingleton(IntegrationEventTypes.FromAssembly(typeof(IIntegrationEvent).Assembly));
```

```bash
dotnet ef migrations add AddOutbox --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

Open the generated migration. Expect `CreateTable("outbox_messages")`, `CreateTable("outbox_deliveries")` with the FK, the unique index and `ix_outbox_deliveries_due`, and nothing else.

- [ ] **Step 9: Run the tests to see them pass**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Expected: all pass, no pending model changes. The transaction itself is proven in the smoke (task 4 Step 3): this project has no database tests.

- [ ] **Step 10: Record the decision**

Create `docs\decisions\0010-integration-events-and-outbox.md` in the format of 0007/0009 (Status: Accepted, Date, Context, Decision, Consequences). It records:
- **Context:** handlers ran inside the caller's unit of work, between implicit transactions, and the email went out inline (intent §2).
- **Decision:** README decisions 2–8, with the table shapes and the claim query.
- **Rejected:** SNS/SQS, RabbitMQ and Kafka. They can't join a PostgreSQL transaction, so they would still need the outbox, plus a container to run (intent non-goals). A per-message lease column instead of a held lock was rejected too: `SKIP LOCKED` inside one claim transaction is simpler at this volume.
- **Consequences:** at-least-once delivery, so every consumer guards against a repeat; a crash between the provider accepting and the row being recorded resends once (Open Item 3); a consumer added later doesn't see old messages; dead deliveries are visible in SQL and in the logs, and are purged after 30 days.

Edit `.claude\rules\ddd-architecture.md`:
- Under **Domain Events**, replace "Events are dispatched after `SaveChanges` succeeds (in-process handlers, e.g. the Excel email on `PublicationClosed`), then `CommitEvents()` clears them" with: in-process handlers run inside the unit of work's single transaction; a handler never makes an outbound call, and anything outside the process goes through an integration event.
- Add a section **Integration Events and the Outbox**: the table from intent §5.1, file locations (`Application\IntegrationEvents\` for contracts, `Application\EventHandlers\{DomainEvent}Handler.cs` for the mapping, `Application\Consumers\{Job}Consumer.cs` for consumers, registered with `AddIntegrationEventConsumer`), the rules (primitives-only payloads; a stable `EventType`; a new `SchemaVersion` with a new record rather than changing a stored shape; every consumer guarded against a repeat; no outbound call outside a consumer), and a pointer to ADR 0010.
- Remove "Outbox pattern + message broker integration events — domain events are handled in-process" from **Deliberately Omitted**, and add "a message broker: the outbox processor is in-process (ADR 0010)" in its place.

Add four rows to `CLAUDE.md`'s File Location Quick Reference (integration event, domain-to-integration handler, consumer, consumer test). Under its Critical Rules add: **13. No outbound call inside a unit of work**: email and WhatsApp go through an integration event and a consumer.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Application/IntegrationEvents src/DrivingLessons.Infrastructure/Outbox \
        src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/OutboxMessageConfiguration.cs \
        src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/OutboxDeliveryConfiguration.cs \
        src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs \
        src/DrivingLessons.Infrastructure/DependencyInjection.cs \
        src/DrivingLessons.Infrastructure/EntityFramework/Migrations \
        tests/DrivingLessons.Application.Test/Outbox tests/DrivingLessons.Application.Test/IntegrationEvents \
        docs/decisions/0010-integration-events-and-outbox.md .claude/rules/ddd-architecture.md CLAUDE.md
git commit -m "feat(outbox): integration events written to an outbox in the unit of work's transaction"
```
