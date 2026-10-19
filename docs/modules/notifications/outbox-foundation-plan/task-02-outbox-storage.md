# Task 2 of 6: Outbox storage, and every commit in one transaction

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`. Requires task 1 committed.

**Files:**
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxDeliveryState.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxMessage.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxDelivery.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxDeliveryMustBePendingException.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\OutboxSerializer.cs`
- Create: `src\DrivingLessons.Infrastructure\Outbox\Outbox.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxMessageConfiguration.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxDeliveryConfiguration.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (two `DbSet`s, `CommitAsync` opens a transaction)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (`IOutbox`)
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\{timestamp}_AddOutbox.cs` (+ `.Designer.cs`, snapshot), generated
- Test: `tests\DrivingLessons.Application.Test\Outbox\OutboxDeliveryTest.cs`, `OutboxSerializerTest.cs`, `OutboxTest.cs` (new)

**Interfaces:**
- Consumes: task 1's `IIntegrationEvent`, `IOutbox`, `IntegrationEventCatalog` (`NameOf`, `ConsumersOf`); `TimeProvider` (registered as `TimeProvider.System`); `DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext>, IDomainEventDispatcher)`.
- Produces (used by tasks 3 and 4):
  - `DrivingLessonsDbContext.OutboxMessages : DbSet<OutboxMessage>`, `.OutboxDeliveries : DbSet<OutboxDelivery>`.
  - `OutboxMessage.Create(string eventName, string payload, DateTimeOffset occurredAtUtc)`; properties `Id`, `EventName`, `Payload`, `OccurredAtUtc`.
  - `OutboxDelivery.Create(Guid messageId, string consumer, DateTimeOffset nowUtc)`; `Succeed(DateTimeOffset nowUtc)`; `Fail(Exception exception, DateTimeOffset nowUtc, int maxAttempts)`; properties `Id`, `MessageId`, `Consumer`, `State`, `Attempts`, `NextAttemptAtUtc`, `LockedUntilUtc`, `LastError`, `CompletedAtUtc`; `const int LastErrorMaxLength = 1000`.
  - `OutboxSerializer.Serialize(IIntegrationEvent) : string`, `.Deserialize(string payload, Type eventType) : IIntegrationEvent`.
  - `Outbox : IOutbox`, registered scoped.
  - Tables `outbox_messages(id, event_name, payload jsonb, occurred_at_utc)` and `outbox_deliveries(id, message_id → outbox_messages ON DELETE CASCADE, consumer, state, attempts, next_attempt_at_utc, locked_until_utc, last_error, completed_at_utc)`, unique `(message_id, consumer)`, index `(state, next_attempt_at_utc)`.
  - `CommitAsync()` runs every save, every domain-event dispatch cycle and every outbox write in **one** transaction, opened by `CommitAsync` when none is open.

Nothing calls `IOutbox` until task 4 and nothing reads the tables until task 3.

- [ ] **Step 1: Write the failing delivery tests**

Create `tests\DrivingLessons.Application.Test\Outbox\OutboxDeliveryTest.cs`:

```csharp
using DrivingLessons.Infrastructure.Outbox;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxDeliveryTest
{
    private const int MaxAttempts = 8;

    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void A_New_Delivery_Is_Pending_And_Due_Now()
    {
        //given
        var messageId = Guid.NewGuid();

        //when
        var delivery = OutboxDelivery.Create(messageId, "SendTeacherExcelEmailsConsumer", NowUtc);

        //then
        delivery.MessageId.ShouldBe(messageId);
        delivery.Consumer.ShouldBe("SendTeacherExcelEmailsConsumer");
        delivery.State.ShouldBe(OutboxDeliveryState.Pending);
        delivery.Attempts.ShouldBe(0);
        delivery.NextAttemptAtUtc.ShouldBe(NowUtc);
        delivery.LockedUntilUtc.ShouldBeNull();
        delivery.CompletedAtUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Succeeds_And_Releases_Its_Lease()
    {
        //given
        var delivery = Pending();
        var laterUtc = NowUtc.AddSeconds(3);

        //when
        delivery.Succeed(laterUtc);

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Succeeded);
        delivery.Attempts.ShouldBe(1);
        delivery.CompletedAtUtc.ShouldBe(laterUtc);
        delivery.LockedUntilUtc.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(2, 2)]
    [DataRow(3, 4)]
    [DataRow(4, 8)]
    [DataRow(5, 16)]
    [DataRow(6, 32)]
    [DataRow(7, 60)]
    public void A_Failure_Schedules_The_Next_Attempt_With_A_Growing_Gap(int failures, int expectedMinutes)
    {
        //given
        var delivery = Pending();

        //when
        for (var failure = 0; failure < failures; failure++)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);
        }

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Pending);
        delivery.Attempts.ShouldBe(failures);
        delivery.NextAttemptAtUtc.ShouldBe(NowUtc.AddMinutes(expectedMinutes));
        delivery.LockedUntilUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Keeps_The_Error_Type_And_Message()
    {
        //given
        var delivery = Pending();

        //when
        delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);

        //then
        delivery.LastError.ShouldBe("TimeoutException: SMTP timed out.");
    }

    [TestMethod]
    public void Cuts_A_Long_Error_To_Its_Maximum_Length()
    {
        //given
        var delivery = Pending();
        var longMessage = new string('x', 5000);

        //when
        delivery.Fail(new TimeoutException(longMessage), NowUtc, MaxAttempts);

        //then
        delivery.LastError!.Length.ShouldBe(OutboxDelivery.LastErrorMaxLength);
    }

    [TestMethod]
    public void Becomes_Dead_After_The_Last_Attempt()
    {
        //given
        var delivery = Pending();

        //when
        for (var failure = 0; failure < MaxAttempts; failure++)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);
        }

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Dead);
        delivery.Attempts.ShouldBe(MaxAttempts);
        delivery.CompletedAtUtc.ShouldBe(NowUtc);
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Succeed(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.Succeed(NowUtc);

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Fail(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    private static OutboxDelivery Pending()
    {
        return OutboxDelivery.Create(Guid.NewGuid(), "SendTeacherExcelEmailsConsumer", NowUtc);
    }

    private static OutboxDelivery InState(OutboxDeliveryState state)
    {
        var delivery = Pending();

        if (state == OutboxDeliveryState.Succeeded)
        {
            delivery.Succeed(NowUtc);
        }

        if (state == OutboxDeliveryState.Dead)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, 1);
        }

        return delivery;
    }
}
```

- [ ] **Step 2: Write the failing serializer and outbox tests**

Create `tests\DrivingLessons.Application.Test\Outbox\OutboxSerializerTest.cs`:

```csharp
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.Outbox;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxSerializerTest
{
    [TestMethod]
    public void A_Payload_Round_Trips_With_Its_Nested_List()
    {
        //given
        var sample = new SampleClosed(Guid.NewGuid(), [new SampleVersion(Guid.NewGuid(), 2)]);

        //when
        var payload = OutboxSerializer.Serialize(sample);
        var restored = (SampleClosed)OutboxSerializer.Deserialize(payload, typeof(SampleClosed));

        //then
        restored.Id.ShouldBe(sample.Id);
        restored.Versions.ShouldBe(sample.Versions);
    }

    [TestMethod]
    public void Writes_Camel_Case_Property_Names()
    {
        //given
        var sample = new SampleClosed(Guid.Empty, []);

        //when
        var payload = OutboxSerializer.Serialize(sample);

        //then
        payload.ShouldBe("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"versions\":[]}");
    }

    public record SampleClosed(Guid Id, IReadOnlyList<SampleVersion> Versions) : IIntegrationEvent;

    public record SampleVersion(Guid TeacherId, int Version);
}
```

`ShouldBe` on two lists of records compares item by item, and records compare by value.

Create `tests\DrivingLessons.Application.Test\Outbox\OutboxTest.cs`. Adding entities to a `DbContext` never opens a connection, so this runs without Postgres:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Outbox;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxTest
{
    private const string SampleName = "sample-happened.v1";

    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private DrivingLessonsDbContext context = null!;
    private IntegrationEventCatalog catalog = null!;
    private Infrastructure.Outbox.Outbox outbox = null!;

    [TestInitialize]
    public void Init()
    {
        var options = new DbContextOptionsBuilder<DrivingLessonsDbContext>()
                      .UseNpgsql("Host=localhost;Database=never_opened")
                      .Options;
        context = new DrivingLessonsDbContext(options, A.Fake<IDomainEventDispatcher>());
        catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        var timeProvider = A.Fake<TimeProvider>();
        A.CallTo(() => timeProvider.GetUtcNow()).Returns(NowUtc);
        outbox = new Infrastructure.Outbox.Outbox(context, catalog, timeProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        context.Dispose();
    }

    [TestMethod]
    public void Adds_The_Message_Under_The_Events_Registered_Name()
    {
        //given
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        outbox.Add(sample);

        //then
        var message = context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem().Entity;
        message.EventName.ShouldBe(SampleName);
        message.OccurredAtUtc.ShouldBe(NowUtc);
        OutboxSerializer.Deserialize(message.Payload, typeof(SampleHappened)).ShouldBe(sample);
    }

    [TestMethod]
    public void Adds_One_Pending_Delivery_Per_Consumer()
    {
        //given
        catalog.AddConsumer<SampleHappened, FirstConsumer>();
        catalog.AddConsumer<SampleHappened, SecondConsumer>();

        //when
        outbox.Add(new SampleHappened(Guid.NewGuid()));

        //then
        var message = context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem().Entity;
        var deliveries = context.ChangeTracker.Entries<OutboxDelivery>().Select(x => x.Entity).ToList();
        deliveries.Select(x => x.Consumer).ShouldBe([nameof(FirstConsumer), nameof(SecondConsumer)]);
        deliveries.ShouldAllBe(x => x.MessageId == message.Id && x.State == OutboxDeliveryState.Pending && x.NextAttemptAtUtc == NowUtc);
    }

    [TestMethod]
    public void An_Event_Without_Consumers_Is_Still_Stored_With_No_Delivery()
    {
        //given
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        outbox.Add(sample);

        //then
        context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem();
        context.ChangeTracker.Entries<OutboxDelivery>().ShouldBeEmpty();
    }

    [TestMethod]
    public void An_Unregistered_Event_Is_Refused()
    {
        //given
        var unregistered = new UnregisteredHappened();

        //when
        var act = () => outbox.Add(unregistered);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    public record SampleHappened(Guid Id) : IIntegrationEvent;

    public record UnregisteredHappened : IIntegrationEvent;

    public class FirstConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.CompletedTask;
        }
    }

    public class SecondConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.CompletedTask;
        }
    }
}
```

`TimeProvider` is faked with FakeItEasy, as in `ReviseSubmissionInteractorTest`; the test project has no `FakeTimeProvider` package and this slice adds none.

- [ ] **Step 3: Run the tests to confirm they fail to compile**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DrivingLessons.Application.Test.Outbox"
```

Expected: build error, `DrivingLessons.Infrastructure.Outbox` does not exist.

- [ ] **Step 4: Create the outbox records**

`src\DrivingLessons.Infrastructure\Outbox\OutboxDeliveryState.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Outbox;

public enum OutboxDeliveryState
{
    Pending,
    Succeeded,
    Dead
}
```

`src\DrivingLessons.Infrastructure\Outbox\OutboxMessage.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    private OutboxMessage()
    {
    }

    private OutboxMessage(Guid id, string eventName, string payload, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public static OutboxMessage Create(string eventName, string payload, DateTimeOffset occurredAtUtc)
    {
        var id = Guid.CreateVersion7();

        return new OutboxMessage(id, eventName, payload, occurredAtUtc);
    }
}
```

`src\DrivingLessons.Infrastructure\Outbox\OutboxDeliveryMustBePendingException.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxDeliveryMustBePendingException : Exception
{
    public OutboxDeliveryMustBePendingException(Guid deliveryId, OutboxDeliveryState state)
        : base($"Outbox delivery [{deliveryId}] must be pending but is [{state}].")
    {
    }
}
```

`src\DrivingLessons.Infrastructure\Outbox\OutboxDelivery.cs`:

```csharp
namespace DrivingLessons.Infrastructure.Outbox;

public class OutboxDelivery
{
    public const int LastErrorMaxLength = 1000;

    private const int MaxBackoffMinutes = 60;

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string Consumer { get; private set; } = string.Empty;

    public OutboxDeliveryState State { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private OutboxDelivery()
    {
    }

    private OutboxDelivery(Guid id, Guid messageId, string consumer, DateTimeOffset nextAttemptAtUtc)
    {
        Id = id;
        MessageId = messageId;
        Consumer = consumer;
        State = OutboxDeliveryState.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    public static OutboxDelivery Create(Guid messageId, string consumer, DateTimeOffset nowUtc)
    {
        var id = Guid.CreateVersion7();

        return new OutboxDelivery(id, messageId, consumer, nowUtc);
    }

    public void Succeed(DateTimeOffset nowUtc)
    {
        MustBePending();

        Attempts++;
        State = OutboxDeliveryState.Succeeded;
        CompletedAtUtc = nowUtc;
        LockedUntilUtc = null;
    }

    public void Fail(Exception exception, DateTimeOffset nowUtc, int maxAttempts)
    {
        MustBePending();

        Attempts++;
        LastError = Describe(exception);
        LockedUntilUtc = null;

        if (Attempts >= maxAttempts)
        {
            State = OutboxDeliveryState.Dead;
            CompletedAtUtc = nowUtc;
            return;
        }

        var backoff = BackoffAfter(Attempts);
        NextAttemptAtUtc = nowUtc.Add(backoff);
    }

    private void MustBePending()
    {
        if (State != OutboxDeliveryState.Pending)
        {
            throw new OutboxDeliveryMustBePendingException(Id, State);
        }
    }

    private static TimeSpan BackoffAfter(int attempts)
    {
        var exponent = attempts - 1;
        var minutes = Math.Min(Math.Pow(2, exponent), MaxBackoffMinutes);

        return TimeSpan.FromMinutes(minutes);
    }

    private static string Describe(Exception exception)
    {
        var typeName = exception.GetType().Name;
        var description = $"{typeName}: {exception.Message}";

        return description.Length <= LastErrorMaxLength
            ? description
            : description[..LastErrorMaxLength];
    }
}
```

These are infrastructure records (README conventions): no `AggregateRoot`, no typed IDs, no events. Only Infrastructure creates and changes them.

- [ ] **Step 5: Create the serializer and the outbox**

`src\DrivingLessons.Infrastructure\Outbox\OutboxSerializer.cs`:

```csharp
using System.Text.Json;
using DrivingLessons.Application.IntegrationEvents;

namespace DrivingLessons.Infrastructure.Outbox;

public static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();

        return JsonSerializer.Serialize(integrationEvent, eventType, Options);
    }

    public static IIntegrationEvent Deserialize(string payload, Type eventType)
    {
        var restored = JsonSerializer.Deserialize(payload, eventType, Options);

        return (IIntegrationEvent)restored!;
    }
}
```

A payload of `null` can't be written (`Serialize` takes a non-null event), so `restored!` is safe for rows this code wrote.

`src\DrivingLessons.Infrastructure\Outbox\Outbox.cs`:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;

namespace DrivingLessons.Infrastructure.Outbox;

public class Outbox : IOutbox
{
    private readonly DrivingLessonsDbContext context;
    private readonly IntegrationEventCatalog catalog;
    private readonly TimeProvider timeProvider;

    public Outbox(DrivingLessonsDbContext context, IntegrationEventCatalog catalog, TimeProvider timeProvider)
    {
        this.context = context;
        this.catalog = catalog;
        this.timeProvider = timeProvider;
    }

    public void Add(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();
        var eventName = catalog.NameOf(eventType);
        var consumers = catalog.ConsumersOf(eventName);
        var payload = OutboxSerializer.Serialize(integrationEvent);
        var nowUtc = timeProvider.GetUtcNow();

        var message = OutboxMessage.Create(eventName, payload, nowUtc);
        context.OutboxMessages.Add(message);

        foreach (var consumer in consumers)
        {
            var delivery = OutboxDelivery.Create(message.Id, consumer, nowUtc);
            context.OutboxDeliveries.Add(delivery);
        }
    }
}
```

The class is named `Outbox` in the namespace `…Infrastructure.Outbox`; the tests refer to it as `Infrastructure.Outbox.Outbox` to avoid the clash. If the compiler still finds the name ambiguous anywhere, rename the class to `EntityFrameworkOutbox` and the test field type with it; the rest of the plan only uses `IOutbox`.

- [ ] **Step 6: Map the tables and commit in one transaction**

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxMessageConfiguration.cs`:

```csharp
using DrivingLessons.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private const int EventNameMaxLength = 200;

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(EventNameMaxLength);

        builder
            .Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        builder
            .Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamptz");
    }
}
```

`src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\OutboxDeliveryConfiguration.cs`:

```csharp
using DrivingLessons.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations;

public class OutboxDeliveryConfiguration : IEntityTypeConfiguration<OutboxDelivery>
{
    private const int ConsumerMaxLength = 200;
    private const int StateMaxLength = 20;

    public void Configure(EntityTypeBuilder<OutboxDelivery> builder)
    {
        builder.ToTable("outbox_deliveries");

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.MessageId)
            .HasColumnName("message_id");

        builder
            .HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.Consumer)
            .HasColumnName("consumer")
            .HasMaxLength(ConsumerMaxLength);

        builder
            .Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(StateMaxLength);

        builder
            .Property(x => x.Attempts)
            .HasColumnName("attempts");

        builder
            .Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.LockedUntilUtc)
            .HasColumnName("locked_until_utc")
            .HasColumnType("timestamptz");

        builder
            .Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(OutboxDelivery.LastErrorMaxLength);

        builder
            .Property(x => x.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .HasColumnType("timestamptz");

        builder
            .HasIndex(x => new { x.MessageId, x.Consumer })
            .IsUnique();

        builder.HasIndex(x => new { x.State, x.NextAttemptAtUtc });
    }
}
```

In `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`:

1. Add `using DrivingLessons.Infrastructure.Outbox;`.
2. After `public DbSet<Submission> Submissions => Set<Submission>();` add:

```csharp
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<OutboxDelivery> OutboxDeliveries => Set<OutboxDelivery>();
```

3. Replace the body of `CommitAsync` so it opens a transaction when none is open, and disposes it after the commit:

```csharp
    public async Task CommitAsync()
    {
        if (Database.CurrentTransaction is null)
        {
            await Database.BeginTransactionAsync();
        }

        try
        {
            await SaveAndDispatchAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrentChangeException(exception);
        }

        var transaction = Database.CurrentTransaction!;

        await transaction.CommitAsync();
        await transaction.DisposeAsync();
    }
```

Before writing it, re-read the method on `main` and keep anything else it does that this snippet doesn't show. The snippet assumes the current body is exactly the `try`/`catch` around `SaveAndDispatchAsync()` followed by committing `Database.CurrentTransaction` when it isn't null.

Why: when no transaction is open, `SaveAndDispatchAsync` saves the aggregate under auto-commit **before** the domain-event handlers run. The rows a handler adds (task 4: the outbox message for a close) then land in a second commit, and a crash in between loses the event. One transaction makes it all or nothing (README decision 6). `TeacherAndCarLocks` already opens a transaction before `CommitAsync`; that path is unchanged except for the dispose. When `CommitAsync` throws, nothing is committed: the scoped `DbContext` disposes the open transaction at the end of the request or job, which rolls it back.

Checked at planning time: no interactor calls `CommitAsync` more than once. Task 4's consumer does, once per Teacher; after the dispose `Database.CurrentTransaction` is `null` again, so each call opens its own transaction.

- [ ] **Step 7: Register the outbox**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, add `using DrivingLessons.Infrastructure.Outbox;` and, after `services.AddScoped<ITeacherAndCarLocks, TeacherAndCarLocks>();`:

```csharp
        services.AddScoped<IOutbox, Outbox.Outbox>();
```

`IntegrationEventCatalog` is registered by `AddApplication` the first time an event is added (task 4). Until then `Outbox` can't be resolved, and nothing resolves it.

- [ ] **Step 8: Run the tests**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DrivingLessons.Application.Test.Outbox"
```

Expected: all `OutboxDeliveryTest`, `OutboxSerializerTest` and `OutboxTest` tests pass.

- [ ] **Step 9: Generate and read the migration**

```bash
dotnet ef migrations add AddOutbox --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

Read the generated `…_AddOutbox.cs`. It must create exactly `outbox_messages` and `outbox_deliveries` with the columns above, `payload` as `jsonb`, the cascade foreign key, the unique `(message_id, consumer)` index and the `(state, next_attempt_at_utc)` index, and touch no other table. Then:

```bash
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

Expected: no pending changes.

- [ ] **Step 10: Full build and suites**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: everything passes. Existing interactor tests fake `IUnitOfWork`, so the transaction change doesn't reach them; the smoke in task 6 proves it on Postgres.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Outbox src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/OutboxMessageConfiguration.cs src/DrivingLessons.Infrastructure/EntityFramework/EntityConfigurations/OutboxDeliveryConfiguration.cs src/DrivingLessons.Infrastructure/EntityFramework/DrivingLessonsDbContext.cs src/DrivingLessons.Infrastructure/EntityFramework/Migrations src/DrivingLessons.Infrastructure/DependencyInjection.cs tests/DrivingLessons.Application.Test/Outbox
git commit -m "feat(notifications): outbox tables, deliveries with retries, and one transaction per commit"
```
