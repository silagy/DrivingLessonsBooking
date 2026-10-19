# Task 1 of 6: Integration event contracts and the catalog

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`, commands from the repo root in bash.

**Files:**
- Create: `src\DrivingLessons.Application\IntegrationEvents\IIntegrationEvent.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IIntegrationEventConsumer.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventConsumerRegistration.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventCatalog.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventCatalogException.cs`
- Create: `src\DrivingLessons.Application\IntegrationEvents\IntegrationEventServiceCollectionExtension.cs`
- Create: `src\DrivingLessons.Application\Abstractions\IOutbox.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (`AddIntegrationEventCatalog()`)
- Test: `tests\DrivingLessons.Application.Test\IntegrationEvents\IntegrationEventCatalogTest.cs` (new)

**Interfaces:**
- Consumes (on `main`): `Microsoft.Extensions.DependencyInjection` (already referenced by Application for `AddApplication`).
- Produces (used by tasks 2–4):
  - `interface IIntegrationEvent` (marker).
  - `interface IIntegrationEventConsumer<in TEvent> where TEvent : IIntegrationEvent { Task ConsumeAsync(TEvent integrationEvent); }`
  - `interface IOutbox { void Add(IIntegrationEvent integrationEvent); }`: queues the event in the current unit of work; nothing is written until `IUnitOfWork.CommitAsync()`.
  - `IntegrationEventCatalog` (singleton): `NameOf(Type) : string`, `TypeOf(string) : Type`, `ConsumersOf(string eventName) : IReadOnlyList<string>`, `ConsumerOf(string eventName, string consumerName) : IntegrationEventConsumerRegistration`.
  - `record IntegrationEventConsumerRegistration(string Name, Func<IServiceProvider, IIntegrationEvent, Task> InvokeAsync)`.
  - `services.AddIntegrationEventCatalog()` (registers the one catalog, called first in `AddApplication`), `services.AddIntegrationEvent<TEvent>(string name)` and `services.AddIntegrationEventConsumer<TEvent, TConsumer>()`; the last registers `TConsumer` as scoped. A consumer's name is its class name (README decision 4).
  - `IntegrationEventCatalogException`: thrown for a registration mistake (duplicate name, duplicate type, duplicate consumer, consumer of an unregistered event) and for a lookup of an unknown event or consumer.

Nothing registers an event yet, and nothing implements `IOutbox` until task 2, so the running app is unchanged.

- [ ] **Step 1: Write the failing catalog tests**

Create `tests\DrivingLessons.Application.Test\IntegrationEvents\IntegrationEventCatalogTest.cs`:

```csharp
using DrivingLessons.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.IntegrationEvents;

[TestClass]
public class IntegrationEventCatalogTest
{
    private const string SampleName = "sample-happened.v1";

    [TestMethod]
    public void Names_An_Event_By_Its_Registered_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var name = catalog.NameOf(typeof(SampleHappened));

        //then
        name.ShouldBe(SampleName);
    }

    [TestMethod]
    public void Finds_The_Event_Type_By_Its_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var eventType = catalog.TypeOf(SampleName);

        //then
        eventType.ShouldBe(typeof(SampleHappened));
    }

    [TestMethod]
    public void Lists_Every_Consumer_Of_An_Event_In_Registration_Order()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        catalog.AddConsumer<SampleHappened, RecordingConsumer>();
        catalog.AddConsumer<SampleHappened, OtherConsumer>();

        //when
        var consumers = catalog.ConsumersOf(SampleName);

        //then
        consumers.ShouldBe([nameof(RecordingConsumer), nameof(OtherConsumer)]);
    }

    [TestMethod]
    public void An_Event_Without_Consumers_Has_None()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var consumers = catalog.ConsumersOf(SampleName);

        //then
        consumers.ShouldBeEmpty();
    }

    [TestMethod]
    public void Refuses_Two_Events_With_The_Same_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.AddEvent<OtherHappened>(SampleName);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_The_Same_Event_Type_Under_Two_Names()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.AddEvent<SampleHappened>("sample-happened.v2");

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_A_Consumer_Of_An_Unregistered_Event()
    {
        //given
        var catalog = new IntegrationEventCatalog();

        //when
        var act = () => catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_The_Same_Consumer_Twice()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //when
        var act = () => catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    [DataRow("unknown.v1")]
    [DataRow("")]
    public void An_Unknown_Event_Name_Is_Refused(string eventName)
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.TypeOf(eventName);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void An_Unregistered_Event_Type_Is_Refused()
    {
        //given
        var catalog = new IntegrationEventCatalog();

        //when
        var act = () => catalog.NameOf(typeof(SampleHappened));

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void An_Unknown_Consumer_Is_Refused()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.ConsumerOf(SampleName, nameof(RecordingConsumer));

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public async Task A_Registered_Consumer_Is_Resolved_From_The_Scope_And_Receives_The_Event()
    {
        //given
        var services = new ServiceCollection();
        services.AddIntegrationEvent<SampleHappened>(SampleName);
        services.AddIntegrationEventConsumer<SampleHappened, RecordingConsumer>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var catalog = provider.GetRequiredService<IntegrationEventCatalog>();
        var registration = catalog.ConsumerOf(SampleName, nameof(RecordingConsumer));
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        await registration.InvokeAsync(scope.ServiceProvider, sample);

        //then
        scope.ServiceProvider.GetRequiredService<RecordingConsumer>().Received.ShouldBe([sample]);
    }

    [TestMethod]
    public void Registering_Through_Services_Keeps_One_Catalog()
    {
        //given
        var services = new ServiceCollection();

        //when
        services.AddIntegrationEvent<SampleHappened>(SampleName);
        services.AddIntegrationEvent<OtherHappened>("other-happened.v1");

        //then
        services.Count(x => x.ServiceType == typeof(IntegrationEventCatalog)).ShouldBe(1);
    }

    public record SampleHappened(Guid Id) : IIntegrationEvent;

    public record OtherHappened(Guid Id) : IIntegrationEvent;

    public class RecordingConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public List<SampleHappened> Received { get; } = [];

        public Task ConsumeAsync(SampleHappened integrationEvent)
        {
            Received.Add(integrationEvent);

            return Task.CompletedTask;
        }
    }

    public class OtherConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run the tests to confirm they fail to compile**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IntegrationEventCatalogTest"
```

Expected: build error, `DrivingLessons.Application.IntegrationEvents` does not exist.

- [ ] **Step 3: Create the contracts**

`src\DrivingLessons.Application\IntegrationEvents\IIntegrationEvent.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEvent
{
}
```

`src\DrivingLessons.Application\IntegrationEvents\IIntegrationEventConsumer.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent);
}
```

`src\DrivingLessons.Application\IntegrationEvents\IntegrationEventConsumerRegistration.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public record IntegrationEventConsumerRegistration(string Name, Func<IServiceProvider, IIntegrationEvent, Task> InvokeAsync);
```

`src\DrivingLessons.Application\IntegrationEvents\IntegrationEventCatalogException.cs`:

```csharp
namespace DrivingLessons.Application.IntegrationEvents;

public class IntegrationEventCatalogException : Exception
{
    public IntegrationEventCatalogException(string message)
        : base(message)
    {
    }
}
```

`src\DrivingLessons.Application\Abstractions\IOutbox.cs`:

```csharp
using DrivingLessons.Application.IntegrationEvents;

namespace DrivingLessons.Application.Abstractions;

public interface IOutbox
{
    void Add(IIntegrationEvent integrationEvent);
}
```

- [ ] **Step 4: Create the catalog**

`src\DrivingLessons.Application\IntegrationEvents\IntegrationEventCatalog.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application.IntegrationEvents;

public class IntegrationEventCatalog
{
    private readonly Dictionary<Type, string> namesByType = [];
    private readonly Dictionary<string, Type> typesByName = [];
    private readonly Dictionary<string, List<IntegrationEventConsumerRegistration>> consumersByEvent = [];

    public void AddEvent<TEvent>(string name)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);

        if (typesByName.ContainsKey(name))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{name}] is already registered.");
        }

        if (namesByType.ContainsKey(eventType))
        {
            throw new IntegrationEventCatalogException($"Integration event [{eventType.Name}] is already registered.");
        }

        namesByType.Add(eventType, name);
        typesByName.Add(name, eventType);
        consumersByEvent.Add(name, []);
    }

    public void AddConsumer<TEvent, TConsumer>()
        where TEvent : IIntegrationEvent
        where TConsumer : IIntegrationEventConsumer<TEvent>
    {
        var eventName = NameOf(typeof(TEvent));
        var consumerName = typeof(TConsumer).Name;
        var consumers = consumersByEvent[eventName];

        if (consumers.Any(x => x.Name == consumerName))
        {
            throw new IntegrationEventCatalogException($"Consumer [{consumerName}] of [{eventName}] is already registered.");
        }

        var registration = new IntegrationEventConsumerRegistration(consumerName, InvokeAsync);
        consumers.Add(registration);

        static Task InvokeAsync(IServiceProvider provider, IIntegrationEvent integrationEvent)
        {
            var consumer = provider.GetRequiredService<TConsumer>();

            return consumer.ConsumeAsync((TEvent)integrationEvent);
        }
    }

    public string NameOf(Type eventType)
    {
        if (!namesByType.TryGetValue(eventType, out var name))
        {
            throw new IntegrationEventCatalogException($"Integration event [{eventType.Name}] is not registered.");
        }

        return name;
    }

    public Type TypeOf(string eventName)
    {
        if (!typesByName.TryGetValue(eventName, out var eventType))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{eventName}] is not registered.");
        }

        return eventType;
    }

    public IReadOnlyList<string> ConsumersOf(string eventName)
    {
        var consumers = RegistrationsOf(eventName);

        return consumers.Select(x => x.Name).ToList();
    }

    public IntegrationEventConsumerRegistration ConsumerOf(string eventName, string consumerName)
    {
        var consumers = RegistrationsOf(eventName);
        var registration = consumers.FirstOrDefault(x => x.Name == consumerName);

        return registration
               ?? throw new IntegrationEventCatalogException($"Consumer [{consumerName}] of [{eventName}] is not registered.");
    }

    private List<IntegrationEventConsumerRegistration> RegistrationsOf(string eventName)
    {
        if (!consumersByEvent.TryGetValue(eventName, out var consumers))
        {
            throw new IntegrationEventCatalogException($"Integration event name [{eventName}] is not registered.");
        }

        return consumers;
    }
}
```

The `out var` in `TryGetValue` is the dictionary API, not a method of ours, so the "avoid `out`" rule doesn't apply.

- [ ] **Step 5: Create the registration extension**

`src\DrivingLessons.Application\IntegrationEvents\IntegrationEventServiceCollectionExtension.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application.IntegrationEvents;

public static class IntegrationEventServiceCollectionExtension
{
    public static IServiceCollection AddIntegrationEventCatalog(this IServiceCollection services)
    {
        CatalogOf(services);

        return services;
    }

    public static IServiceCollection AddIntegrationEvent<TEvent>(this IServiceCollection services, string name)
        where TEvent : IIntegrationEvent
    {
        var catalog = CatalogOf(services);
        catalog.AddEvent<TEvent>(name);

        return services;
    }

    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        var catalog = CatalogOf(services);
        catalog.AddConsumer<TEvent, TConsumer>();

        services.AddScoped<TConsumer>();

        return services;
    }

    private static IntegrationEventCatalog CatalogOf(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IntegrationEventCatalog));

        if (descriptor?.ImplementationInstance is IntegrationEventCatalog existing)
        {
            return existing;
        }

        var catalog = new IntegrationEventCatalog();
        services.AddSingleton(catalog);

        return catalog;
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.IntegrationEvents;` and make the first line of `AddApplication`:

```csharp
        services.AddIntegrationEventCatalog();
```

The catalog is then always registered, even before any event is (task 3's job resolves it on every run).

- [ ] **Step 6: Run the tests**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IntegrationEventCatalogTest"
```

Expected: 14 passed (13 test methods; the `DataRow` test runs twice).

- [ ] **Step 7: Full build and suites**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: build without warnings introduced by this task; all tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Application/IntegrationEvents src/DrivingLessons.Application/Abstractions/IOutbox.cs src/DrivingLessons.Application/DependencyInjection.cs tests/DrivingLessons.Application.Test/IntegrationEvents
git commit -m "feat(notifications): integration event contracts and the consumer catalog"
```
