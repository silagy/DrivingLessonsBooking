# Task 2 of 18: The outbox processor: claim, deliver, retry, dead, purge

> Part of [Student Notifications (stage 1)](README.md), milestone A. Work on branch `notifications-outbox`, commands from the repo root in bash. Requires task 1 committed.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\Outbox\OutboxDelivery.cs` (state methods)
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxRetryPolicy.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxOptions.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\IntegrationEventDelivery.cs` (runs one delivery: resolve, deserialize, call the consumer)
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxProcessor.cs` (`BackgroundService`)
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxPurge.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Presentation.Web\appsettings.json` (`Outbox` section)
- Test: `tests\DrivingLessons.Application.Test\Outbox\OutboxDeliveryTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Outbox\OutboxRetryPolicyTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Outbox\IntegrationEventDeliveryTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Outbox\OutboxOptionsTest.cs` (new)

**Interfaces:**
- Consumes (task 1): `OutboxMessage`, `OutboxDelivery`, `OutboxDeliveryState`, `OutboxJson.Options`, `IntegrationEventTypes.Resolve`, `IntegrationEventConsumerRegistration`, `IIntegrationEventConsumer<T>`, `DeliveryOutcome`, `DeliveryAttempt`, `DrivingLessonsDbContext.OutboxDeliveries`.
- Produces:
  - `OutboxDelivery.MarkProcessed(DateTimeOffset processedAtUtc)`, `.Hold(DateTimeOffset untilUtc)`, `.MarkFailed(string error, DateTimeOffset nextAttemptAtUtc)`, `.MarkDead(string error)`. Each one requires `State == Pending` and throws `InvalidOperationException` otherwise: this is infrastructure, so there are no domain exceptions.
  - `OutboxRetryPolicy.MaximumAttempts == 8`, `OutboxRetryPolicy.DelayAfter(int attempts) : TimeSpan` (30 s, 1 min, 5 min, 15 min, 30 min, 1 h, 2 h).
  - `IntegrationEventDelivery.RunAsync(OutboxMessage message, OutboxDelivery delivery, IServiceProvider scopedProvider) : Task<DeliveryOutcome>`. It throws `OutboxConsumerNotRegisteredException` or `OutboxEventTypeUnknownException`, which make the processor go dead at once, without retrying.
  - Config `Outbox:{PollIntervalSeconds=5, BatchSize=10, RetentionDays=30}`.

- [ ] **Step 1: Write the failing delivery-state tests**

`OutboxDeliveryTest` (through `OutboxMessage.Create(…, ["SampleConsumer"])`, like an aggregate root):

| Test | Asserts |
|------|---------|
| `Mark_Processed` | `State == Processed`, `ProcessedAtUtc` set, `Attempts == 1` |
| `Hold__Keeps_It_Pending_Without_Counting_An_Attempt` | `State == Pending`, `NextAttemptAtUtc == until`, `Attempts == 0` |
| `Mark_Failed__Counts_The_Attempt_And_Reschedules` | `Attempts == 1`, `NextAttemptAtUtc == next`, `LastError == error` |
| `Mark_Failed__Cuts_The_Error_To_2000_Characters` | a 5,000-character error is stored as 2,000 |
| `Mark_Dead` | `State == Dead`, `Attempts` incremented, `LastError` set |
| `State_Change__Must_Be_Pending` `[DataRow("processed")] [DataRow("dead")]` | every method throws `InvalidOperationException` |

- [ ] **Step 2: Write the failing policy and options tests**

`OutboxRetryPolicyTest`:
- `Delay_After` `[DataRow(1, 30)] [DataRow(2, 60)] [DataRow(3, 300)] [DataRow(4, 900)] [DataRow(5, 1800)] [DataRow(6, 3600)] [DataRow(7, 7200)]` in seconds.
- `Gives_Up_After_The_Eighth_Attempt`: `IsLastAttempt(8)` is true and `IsLastAttempt(7)` is false.

`OutboxOptionsTest`: the defaults are 5 / 10 / 30; `PollIntervalSeconds` `[Range(1, 300)]`, `BatchSize` `[Range(1, 100)]` and `RetentionDays` `[Range(1, 365)]` reject 0 (same `Validator.TryValidateObject` style as `EmailOptionsTest`).

- [ ] **Step 3: Write the failing dispatch tests**

`IntegrationEventDeliveryTest` uses a real `ServiceCollection` with the test events and consumers of task 1 (`SampleHappened`, a `RecordingSampleConsumer` that stores what it received and returns a configurable outcome):

| Test | Asserts |
|------|---------|
| `Hands_The_Deserialized_Event_To_The_Named_Consumer` | the consumer receives an event equal to the one written |
| `Passes_The_Attempt_Number_And_Whether_It_Is_The_Last` | with `Attempts == 7` already → `DeliveryAttempt(8, true)` |
| `Returns_The_Consumer_Outcome` | `HoldUntil(t)` comes back unchanged |
| `A_Consumer_That_Is_No_Longer_Registered_Throws_Not_Registered` | delivery consumer `"GoneConsumer"` → `OutboxConsumerNotRegisteredException` |
| `An_Unknown_Event_Type_Throws_Unknown` | message `event_type = "nobody"` → `OutboxEventTypeUnknownException` |
| `A_Consumer_Exception_Propagates` | the consumer throws `IOException` → same exception |

- [ ] **Step 4: Run the tests to see them fail**

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Outbox"`
Expected: FAIL. The build breaks: `OutboxDelivery` does not contain `MarkProcessed`.

- [ ] **Step 5: Implement the state methods, the policy, the options and the dispatch**

`OutboxRetryPolicy`: a `static readonly TimeSpan[] Delays` of the seven delays, `MaximumAttempts = Delays.Length + 1`, `DelayAfter(attempts) => Delays[attempts - 1]`, `IsLastAttempt(number) => number >= MaximumAttempts`.

`OutboxOptions` (`SectionName = "Outbox"`), the defaults and ranges above, bound with `ValidateDataAnnotations().ValidateOnStart()` like `EmailOptions`.

`IntegrationEventDelivery.RunAsync`:
1. Find the `IntegrationEventConsumerRegistration` whose `ConsumerName == delivery.Consumer`, or throw `OutboxConsumerNotRegisteredException(delivery.Id, delivery.Consumer)`.
2. Look up `IntegrationEventTypes.Resolve(message.EventType, message.SchemaVersion)`, or throw `OutboxEventTypeUnknownException(message.Id, message.EventType, message.SchemaVersion)`.
3. `JsonSerializer.Deserialize(message.Payload, eventType, OutboxJson.Options)`.
4. Resolve the consumer from `scopedProvider.GetRequiredService(registration.ConsumerType)`, and invoke `HandleAsync` through the closed generic interface (the same reflection pattern as `DomainEventDispatcher`).
5. `attempt = new DeliveryAttempt(delivery.Attempts + 1, OutboxRetryPolicy.IsLastAttempt(delivery.Attempts + 1))`.

The two exceptions live in `Infrastructure\Outbox\` and derive from `Exception`: they are operational faults, not domain rules.

- [ ] **Step 6: Implement the processor**

The claim needs a transaction: open it with `await context.Database.BeginTransactionAsync()` before the query below.

`OutboxProcessor : BackgroundService` takes `IServiceScopeFactory`, `IOptions<OutboxOptions>`, `TimeProvider` and `ILogger<OutboxProcessor>`. `ExecuteAsync` loops until `stoppingToken`: `ProcessBatchAsync()`, then `PurgeIfDueAsync()`, then `Task.Delay(PollInterval, timeProvider, stoppingToken)`. An exception from a batch is logged at Error and the loop goes on: a database blip must never kill the processor.

`ProcessBatchAsync` runs in **one claim scope** (its own `DrivingLessonsDbContext`) and **one transaction**:

```csharp
var now = timeProvider.GetUtcNow();
var batchSize = options.Value.BatchSize;

var claimed = await context
                    .OutboxDeliveries
                    .FromSql($"""
                              SELECT * FROM outbox_deliveries
                              WHERE state = {(int)OutboxDeliveryState.Pending} AND next_attempt_at_utc <= {now}
                              ORDER BY next_attempt_at_utc
                              LIMIT {batchSize}
                              FOR UPDATE SKIP LOCKED
                              """)
                    .ToListAsync();
```

For each claimed delivery, in order: load its `OutboxMessage` (`FindAsync`), create a **separate** scope for the consumer, run `IntegrationEventDelivery.RunAsync`, and record the result on the claimed (tracked) delivery:

| Result | Delivery |
|--------|----------|
| `DeliveryOutcome.Done` | `MarkProcessed(now)` + Debug log `"Outbox delivery [{DeliveryId}] processed."` |
| `DeliveryOutcome.HoldUntil(t)` | `Hold(t)` |
| `OutboxConsumerNotRegisteredException` / `OutboxEventTypeUnknownException` | `MarkDead(message)` + Error log |
| any other exception, attempt is the last | `MarkDead(exception.ToString())` + Error log `"Outbox delivery [{DeliveryId}] of message [{MessageId}] to [{Consumer}] is dead after {Attempts} attempts."` |
| any other exception | `MarkFailed(exception.ToString(), now + DelayAfter(attempts + 1))` + Warning log, IDs only |

Then `SaveChangesAsync()` and commit the claim transaction. The consumer's own writes were committed by its own scope's `CommitAsync`. That is why every consumer must be safe to run twice: a crash between the consumer's commit and this commit re-delivers.

Keep it sequential, one batch at a time. The volume is a few hundred deliveries a week.

- [ ] **Step 7: Implement the daily purge**

`OutboxPurge.PurgeAsync(DrivingLessonsDbContext, DateTimeOffset olderThanUtc)` runs two `ExecuteDeleteAsync` calls: deliveries in `Processed` or `Dead` whose `processed_at_utc` (or, for dead ones, `next_attempt_at_utc`) is older than the cut-off, then messages with no delivery left that occurred before the cut-off. The processor calls it at most once per 24 hours (the last run kept in a field; on start it runs once).

- [ ] **Step 8: Register and configure**

`DependencyInjection.cs`: bind `OutboxOptions`, `services.AddScoped<IntegrationEventDelivery>()`, `services.AddHostedService<OutboxProcessor>()`. Register it **after** `PublicationReconciliationHostedService`. Hosted services start in order, so the reconciliation's opens and closes are already in the outbox when the processor first polls.

`appsettings.json`: `"Outbox": { "PollIntervalSeconds": 5, "BatchSize": 10, "RetentionDays": 30 }`.

- [ ] **Step 9: Run the tests**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: all pass. The claim query, the held lock and `SKIP LOCKED` are proven in task 4 Step 6.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Outbox src/DrivingLessons.Infrastructure/DependencyInjection.cs \
        src/DrivingLessons.Presentation.Web/appsettings.json tests/DrivingLessons.Application.Test/Outbox
git commit -m "feat(outbox): in-process processor claims due deliveries, retries with backoff, goes dead after 8 attempts"
```
