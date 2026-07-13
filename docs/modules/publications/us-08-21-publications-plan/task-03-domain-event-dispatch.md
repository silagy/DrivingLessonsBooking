# Task 3 of 14: Domain-event dispatch + WeekScheduleCreated → Draft handler

> Part of the [US-08–21 Publications plan](README.md). Requires tasks 1–2 complete (`Publication` aggregate, `IPublicationRepository`, events). Work on branch `9-us-08-21-publications-module`, commands from the repo root.
>
> This is the cross-cutting mechanism the teachers/week-schedules slices deferred: `CommitAsync()` was `SaveChangesAsync()` only. This task turns it into a bounded dispatch loop and wires the first handler — a `WeekScheduleCreated` handler that idempotently creates the week's Draft `Publication` (decision #2). It also introduces the first application/infrastructure **unit test project** (`DrivingLessons.Application.Test`, MSTest + Shouldly + FakeItEasy) because the dispatcher and handlers are the first genuinely unit-testable non-domain logic in the repo.

**Files:**
- Create: `src\DrivingLessons.Domain\Common\IHasDomainEvents.cs`
- Modify: `src\DrivingLessons.Domain\Common\AggregateRoot.cs` (implement the marker — members already exist)
- Create: `src\DrivingLessons.Application\Common\IDomainEventHandler.cs`, `Common\IDomainEventDispatcher.cs`
- Create: `src\DrivingLessons.Application\EventHandlers\WeekScheduleCreatedHandler.cs`
- Create: `src\DrivingLessons.Infrastructure\DomainEvents\DomainEventDispatcher.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` (inject dispatcher + bounded `CommitAsync` loop)
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (register the handler)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (register the dispatcher)
- Create: `tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
- Test: `tests\DrivingLessons.Application.Test\DomainEvents\DomainEventDispatcherTest.cs`, `EventHandlers\WeekScheduleCreatedHandlerTest.cs`

This task is TDD: add the base types and interfaces, create the test project, write the failing tests, implement the dispatcher + handler, wire the DbContext + DI, then go green. Before coding, re-read the authoritative `CommitAsync` loop in the contract — reproduce it verbatim.

- [ ] **Step 1: Add the `IHasDomainEvents` marker (Domain)**

The DbContext lives in Infrastructure and must collect events off tracked aggregates without depending on the concrete `AggregateRoot<TId>` closed generic. A non-generic marker exposes exactly the two members `AggregateRoot<TId>` already has.

`src\DrivingLessons.Domain\Common\IHasDomainEvents.cs`:

```csharp
namespace DrivingLessons.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> UncommittedEvents { get; }

    void CommitEvents();
}
```

- [ ] **Step 2: Implement the marker on `AggregateRoot<TId>` (Domain)**

`AggregateRoot<TId>` already exposes `UncommittedEvents` and `CommitEvents()`; only the declaration changes. In `src\DrivingLessons.Domain\Common\AggregateRoot.cs`:

Before:

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : EntityId
```

After:

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : EntityId
```

No other line changes — the existing `UncommittedEvents` property and `CommitEvents()` method satisfy the interface.

- [ ] **Step 3: Add the dispatch abstractions (Application)**

`src\DrivingLessons.Application\Common\IDomainEventHandler.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Application.Common;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}
```

`src\DrivingLessons.Application\Common\IDomainEventDispatcher.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}
```

- [ ] **Step 4: Create the `DrivingLessons.Application.Test` project**

There is no application/infrastructure test project yet. Create one that references `Application` (handler + abstractions) and `Infrastructure` (the concrete `DomainEventDispatcher`), plus FakeItEasy and `Microsoft.Extensions.DependencyInjection` (the dispatcher test builds a real `ServiceProvider`). Mirror the package/`Using` shape of `tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`.

`tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest" Version="4.0.2" />
    <PackageReference Include="Shouldly" Version="4.3.0" />
    <PackageReference Include="FakeItEasy" Version="8.3.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.9" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\DrivingLessons.Application\DrivingLessons.Application.csproj" />
    <ProjectReference Include="..\..\src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

Add it to the solution:

```bash
dotnet sln DrivingLessons.sln add tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
```

Expected: `Project ... added to the solution.`

- [ ] **Step 5: Write the failing tests**

`tests\DrivingLessons.Application.Test\DomainEvents\DomainEventDispatcherTest.cs` — the dispatcher resolves every closed `IDomainEventHandler<>` for the event's runtime type and invokes it; unknown events are ignored:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Infrastructure.DomainEvents;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.DomainEvents;

[TestClass]
public class DomainEventDispatcherTest
{
    private record FakeDomainEvent(Guid Id) : IDomainEvent;

    [TestMethod]
    public async Task Dispatches_To_Registered_Handler()
    {
        //given
        var handler = A.Fake<IDomainEventHandler<FakeDomainEvent>>();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<FakeDomainEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new FakeDomainEvent(Guid.NewGuid());

        //when
        await dispatcher.DispatchAsync(domainEvent);

        //then
        A.CallTo(() => handler.HandleAsync(domainEvent))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Ignores_Events_Without_Handlers()
    {
        //given
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new FakeDomainEvent(Guid.NewGuid());

        //when
        var act = () => dispatcher.DispatchAsync(domainEvent);

        //then
        await Should.NotThrowAsync(act);
    }
}
```

`tests\DrivingLessons.Application.Test\EventHandlers\WeekScheduleCreatedHandlerTest.cs` — creates the Draft when the week has none, skips when it already exists (`2026-07-19` is a Sunday, matching the literal used in `WeekScheduleTest`):

```csharp
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;

namespace DrivingLessons.Application.Test.EventHandlers;

[TestClass]
public class WeekScheduleCreatedHandlerTest
{
    private IPublicationRepository repository = null!;
    private WeekScheduleCreatedHandler handler = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IPublicationRepository>();
        handler = new WeekScheduleCreatedHandler(repository);
    }

    [TestMethod]
    public async Task Creates_Draft_Publication_When_None_Exists()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));
        var domainEvent = new WeekScheduleCreated(WeekScheduleId.New(), TeacherId.New(), weekStart);

        A.CallTo(() => repository.GetByWeekAsync(weekStart))
            .Returns((Publication?)null);

        //when
        await handler.HandleAsync(domainEvent);

        //then
        A.CallTo(() => repository.Add(A<Publication>.That.Matches(x => x.WeekStart == weekStart)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Skips_Creation_When_Publication_Exists()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));
        var domainEvent = new WeekScheduleCreated(WeekScheduleId.New(), TeacherId.New(), weekStart);
        var existing = Publication.Create(weekStart);

        A.CallTo(() => repository.GetByWeekAsync(weekStart))
            .Returns(existing);

        //when
        await handler.HandleAsync(domainEvent);

        //then
        A.CallTo(() => repository.Add(A<Publication>._))
            .MustNotHaveHappened();
    }
}
```

Run: `dotnet build`
Expected: FAILS to compile — `DomainEventDispatcher` and `WeekScheduleCreatedHandler` do not exist yet (CS0246). This is the red state.

- [ ] **Step 6: Implement the dispatcher (Infrastructure)**

Reflection over the closed `IDomainEventHandler<>` for the event's runtime type; resolve all handlers from DI and await each. No comments.

`src\DrivingLessons.Infrastructure\DomainEvents\DomainEventDispatcher.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Infrastructure.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private const string HandleMethodName = nameof(IDomainEventHandler<IDomainEvent>.HandleAsync);

    private readonly IServiceProvider serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handleMethod = handlerType.GetMethod(HandleMethodName);
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var result = handleMethod!.Invoke(handler, [domainEvent]);
            var task = (Task)result!;

            await task;
        }
    }
}
```

- [ ] **Step 7: Implement the handler (Application)**

`src\DrivingLessons.Application\EventHandlers\WeekScheduleCreatedHandler.cs` — idempotent Draft creation; no commit (the outer `CommitAsync` loop persists handler-created aggregates in the same unit of work):

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class WeekScheduleCreatedHandler : IDomainEventHandler<WeekScheduleCreated>
{
    private readonly IPublicationRepository repository;

    public WeekScheduleCreatedHandler(IPublicationRepository repository)
    {
        this.repository = repository;
    }

    public async Task HandleAsync(WeekScheduleCreated domainEvent)
    {
        var existing = await repository.GetByWeekAsync(domainEvent.WeekStart);

        if (existing is not null)
        {
            return;
        }

        var publication = Publication.Create(domainEvent.WeekStart);

        repository.Add(publication);
    }
}
```

- [ ] **Step 8: Turn `CommitAsync` into the bounded dispatch loop (Infrastructure)**

The DbContext gains an `IDomainEventDispatcher` constructor parameter and replaces the one-line `CommitAsync`. `AddDbContext` resolves the extra parameter from the application service provider, so the dispatcher must be registered (Step 10). The loop saves, collects `UncommittedEvents` off tracked `IHasDomainEvents`, clears them, dispatches, and repeats until a save produces no new events — so handler-raised aggregates (the Draft `Publication`) persist in the same unit of work.

In `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`:

Before:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options)
        : base(options)
    {
    }

    public async Task CommitAsync()
    {
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
```

After:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework;

public class DrivingLessonsDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher dispatcher;

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<WeekSchedule> WeekSchedules => Set<WeekSchedule>();

    public DrivingLessonsDbContext(DbContextOptions<DrivingLessonsDbContext> options, IDomainEventDispatcher dispatcher)
        : base(options)
    {
        this.dispatcher = dispatcher;
    }

    public async Task CommitAsync()
    {
        const int maxCycles = 10;

        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            await SaveChangesAsync();

            var roots = ChangeTracker.Entries<IHasDomainEvents>()
                                     .Where(x => x.Entity.UncommittedEvents.Count > 0)
                                     .Select(x => x.Entity)
                                     .ToList();

            if (roots.Count == 0)
            {
                return;
            }

            var domainEvents = roots.SelectMany(x => x.UncommittedEvents).ToList();
            roots.ForEach(x => x.CommitEvents());

            foreach (var domainEvent in domainEvents)
            {
                await dispatcher.DispatchAsync(domainEvent);
            }
        }

        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DrivingLessonsDbContext).Assembly);
    }
}
```

- [ ] **Step 9: Register the handler (Application DI)**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the usings and register the handler after the week-schedule interactors:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Domain.Events;
```

```csharp
services.AddScoped<IDomainEventHandler<WeekScheduleCreated>, WeekScheduleCreatedHandler>();
```

- [ ] **Step 10: Register the dispatcher (Infrastructure DI)**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, add the using and register the dispatcher **before** `AddDbContext` so the context can resolve it:

```csharp
using DrivingLessons.Infrastructure.DomainEvents;
```

```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

services.AddDbContext<DrivingLessonsDbContext>(o =>
    o.UseNpgsql(configuration.GetConnectionString("Default")));
```

(`IDomainEventDispatcher` lives in `DrivingLessons.Application.Common`, already imported via the existing `using DrivingLessons.Application.Common;`.)

- [ ] **Step 11: Build + run all tests**

Run: `dotnet build`
Then: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Then: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: build succeeds; both test projects PASS (the two dispatcher tests + the two handler tests are green).

- [ ] **Step 12: Commit**

```bash
git add src/DrivingLessons.Domain src/DrivingLessons.Application src/DrivingLessons.Infrastructure tests/DrivingLessons.Application.Test DrivingLessons.sln
git commit -m "feat(infra): domain-event dispatch loop and WeekScheduleCreated draft handler"
```

---

**Next:** [task-04-application-commands.md](task-04-application-commands.md)
