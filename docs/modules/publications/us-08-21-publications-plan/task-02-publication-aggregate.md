# Task 2 of 14: Publication aggregate + TeacherExcelVersion child entity

> Part of the [US-08–21 Publications plan](README.md). Requires task 1 complete. Work on branch `9-us-08-21-publications-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\Publication.cs`, `Entities\TeacherExcelVersion.cs`
- Create: `src\DrivingLessons.Domain\Events\PublicationCreated.cs`, `PublicationPublished.cs`, `PublicationOpened.cs`, `PublicationClosed.cs`, `PublicationWindowExtended.cs`, `PublicationReopened.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PublicationMustBeDraftException.cs`, `PublicationMustBePublishedException.cs`, `PublicationMustBeOpenException.cs`, `PublicationMustBeClosedException.cs`, `WindowExtensionMustBeLaterException.cs`
- Create: `src\DrivingLessons.Domain\Repositories\IPublicationRepository.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\PublicationTest.cs`, `Entities\Fake\PublicationFakeBuilder.cs`

Test through the aggregate root only — never instantiate `TeacherExcelVersion` directly. Before coding, open `src\DrivingLessons.Domain\Entities\WeekSchedule.cs` and `Slot.cs` to match the aggregate/child conventions (private ctors, `AddEvent`, `internal` child factory/mutators, `IReadOnlyCollection` exposure).

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Entities\Fake\PublicationFakeBuilder.cs` (`StateBuilders` per `.claude/rules/domain-testing.md` — every `PublicationState`; build methods chain along the real progression):

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class PublicationFakeBuilder
{
    public static readonly Dictionary<PublicationState, Func<Publication>> StateBuilders = new()
    {
        { PublicationState.Draft, BuildDraft },
        { PublicationState.Published, BuildPublished },
        { PublicationState.Open, BuildOpen },
        { PublicationState.Closed, BuildClosed }
    };

    public static Publication BuildDraft()
    {
        var sunday = Faker.FakeSunday();
        var weekStart = WeekStart.Of(sunday);

        return Publication.Create(weekStart);
    }

    public static Publication BuildPublished()
    {
        var publication = BuildDraft();
        var window = FakeWindow();
        publication.Publish(window);

        return publication;
    }

    public static Publication BuildOpen()
    {
        var publication = BuildPublished();
        publication.Open();

        return publication;
    }

    public static Publication BuildClosed()
    {
        var publication = BuildOpen();
        publication.Close([TeacherId.New()]);

        return publication;
    }

    public static SubmissionWindow FakeWindow()
    {
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddDays(3);

        return SubmissionWindow.Of(startUtc, endUtc);
    }
}
```

`tests\DrivingLessons.Domain.Test\Entities\PublicationTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class PublicationTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var publication = Publication.Create(weekStart);

        //then
        publication.WeekStart.ShouldBe(weekStart);
        publication.IsDraft.ShouldBeTrue();
        publication.LinkToken.Value.ShouldNotBeNullOrWhiteSpace();
        publication.TeacherVersions.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var publication = Publication.Create(weekStart);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationCreated>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.WeekStart == weekStart)
                   .Where(x => x.LinkToken == publication.LinkToken)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Publish()
    {
        //given
        var publication = PublicationFakeBuilder.BuildDraft();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        publication.Publish(window);

        //then
        publication.IsPublished.ShouldBeTrue();
        publication.Window.ShouldBe(window);
    }

    [TestMethod]
    public void Publish__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildDraft();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        publication.Publish(window);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationPublished>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.Window == window)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Open)]
    [DataRow(PublicationState.Closed)]
    public void Publish__Must_Be_Draft(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        var act = () => publication.Publish(window);

        //then
        Should.Throw<PublicationMustBeDraftException>(act);
    }

    [TestMethod]
    public void Open()
    {
        //given
        var publication = PublicationFakeBuilder.BuildPublished();

        //when
        publication.Open();

        //then
        publication.IsOpen.ShouldBeTrue();
    }

    [TestMethod]
    public void Open__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildPublished();

        //when
        publication.Open();

        //then
        publication.UncommittedEvents
                   .OfType<PublicationOpened>()
                   .Where(x => x.PublicationId == publication.Id)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Open)]
    [DataRow(PublicationState.Closed)]
    public void Open__Must_Be_Published(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();

        //when
        var act = () => publication.Open();

        //then
        Should.Throw<PublicationMustBePublishedException>(act);
    }

    [TestMethod]
    public void Close()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();

        //when
        publication.Close([teacherId]);

        //then
        publication.IsClosed.ShouldBeTrue();
        publication.TeacherVersions.ShouldContain(x => x.TeacherId == teacherId && x.Version == 1);
    }

    [TestMethod]
    public void Close_Increments_Version_On_Reopen_And_Close()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();
        publication.Close([teacherId]);
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(5);
        publication.Reopen(newEndUtc);

        //when
        publication.Close([teacherId]);

        //then
        publication.TeacherVersions.ShouldContain(x => x.TeacherId == teacherId && x.Version == 2);
    }

    [TestMethod]
    public void Close__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();

        //when
        publication.Close([teacherId]);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationClosed>()
                   .Where(x => x.PublicationId == publication.Id)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Close__Must_Be_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var teacherId = TeacherId.New();

        //when
        var act = () => publication.Close([teacherId]);

        //then
        Should.Throw<PublicationMustBeOpenException>(act);
    }

    [TestMethod]
    public void Extend_Window()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var newEndUtc = publication.Window!.EndUtc.AddDays(2);

        //when
        publication.ExtendWindow(newEndUtc);

        //then
        publication.Window!.EndUtc.ShouldBe(newEndUtc);
    }

    [TestMethod]
    public void Extend_Window__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var newEndUtc = publication.Window!.EndUtc.AddDays(2);

        //when
        publication.ExtendWindow(newEndUtc);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationWindowExtended>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.NewEndUtc == newEndUtc)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Extend_Window__Must_Be_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(10);

        //when
        var act = () => publication.ExtendWindow(newEndUtc);

        //then
        Should.Throw<PublicationMustBeOpenException>(act);
    }

    [TestMethod]
    public void Extend_Window__Must_Be_Later()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var sameEndUtc = publication.Window!.EndUtc;

        //when
        var act = () => publication.ExtendWindow(sameEndUtc);

        //then
        Should.Throw<WindowExtensionMustBeLaterException>(act);
    }

    [TestMethod]
    public void Reopen()
    {
        //given
        var publication = PublicationFakeBuilder.BuildClosed();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        publication.Reopen(newEndUtc);

        //then
        publication.IsOpen.ShouldBeTrue();
        publication.Window!.EndUtc.ShouldBe(newEndUtc);
    }

    [TestMethod]
    public void Reopen__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildClosed();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        publication.Reopen(newEndUtc);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationReopened>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.NewEndUtc == newEndUtc)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Open)]
    public void Reopen__Must_Be_Closed(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        var act = () => publication.Reopen(newEndUtc);

        //then
        Should.Throw<PublicationMustBeClosedException>(act);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL — `Publication`, `TeacherExcelVersion`, the events and exceptions do not exist yet (compile errors).

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Entities\TeacherExcelVersion.cs` (child entity — mirrors `Slot.cs`: `internal` factory/mutator, no events):

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class TeacherExcelVersion : Entity<TeacherExcelVersionId>
{
    public TeacherId TeacherId { get; private set; }
    public int Version { get; private set; }

    private TeacherExcelVersion()
    {
    }

    private TeacherExcelVersion(TeacherExcelVersionId id, TeacherId teacherId, int version)
        : base(id)
    {
        TeacherId = teacherId;
        Version = version;
    }

    internal static TeacherExcelVersion Create(TeacherId teacherId)
    {
        const int initialVersion = 1;
        var id = TeacherExcelVersionId.New();

        return new TeacherExcelVersion(id, teacherId, initialVersion);
    }

    internal void Increment()
    {
        Version += 1;
    }
}
```

`src\DrivingLessons.Domain\Entities\Publication.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Publication : AggregateRoot<PublicationId>
{
    private readonly List<TeacherExcelVersion> teacherVersions = [];

    public WeekStart WeekStart { get; private set; }
    public PublicationState State { get; private set; }
    public ShareableLinkToken LinkToken { get; private set; }
    public SubmissionWindow? Window { get; private set; }

    public IReadOnlyCollection<TeacherExcelVersion> TeacherVersions => teacherVersions.AsReadOnly();

    public bool IsDraft => State is PublicationState.Draft;
    public bool IsPublished => State is PublicationState.Published;
    public bool IsOpen => State is PublicationState.Open;
    public bool IsClosed => State is PublicationState.Closed;

    private Publication()
    {
    }

    private Publication(PublicationId id, WeekStart weekStart, PublicationState state, ShareableLinkToken linkToken)
        : base(id)
    {
        WeekStart = weekStart;
        State = state;
        LinkToken = linkToken;

        AddEvent(new PublicationCreated(id, weekStart, linkToken));
    }

    public static Publication Create(WeekStart weekStart)
    {
        const PublicationState state = PublicationState.Draft;
        var id = PublicationId.New();
        var linkToken = ShareableLinkToken.New();

        return new Publication(id, weekStart, state, linkToken);
    }

    public void Publish(SubmissionWindow window)
    {
        MustBeDraft();

        Window = window;
        State = PublicationState.Published;

        AddEvent(new PublicationPublished(Id, window));
    }

    public void Open()
    {
        MustBePublished();

        State = PublicationState.Open;

        AddEvent(new PublicationOpened(Id));
    }

    public void Close(IEnumerable<TeacherId> teacherIds)
    {
        MustBeOpen();

        State = PublicationState.Closed;

        foreach (var teacherId in teacherIds)
        {
            var existing = teacherVersions.FirstOrDefault(x => x.TeacherId == teacherId);

            if (existing is not null)
            {
                existing.Increment();
            }
            else
            {
                var version = TeacherExcelVersion.Create(teacherId);
                teacherVersions.Add(version);
            }
        }

        AddEvent(new PublicationClosed(Id));
    }

    public void ExtendWindow(DateTimeOffset newEndUtc)
    {
        MustBeOpen();
        WindowExtensionMustBeLater(newEndUtc);

        Window = SubmissionWindow.Of(Window!.StartUtc, newEndUtc);

        AddEvent(new PublicationWindowExtended(Id, newEndUtc));
    }

    public void Reopen(DateTimeOffset newEndUtc)
    {
        MustBeClosed();

        Window = SubmissionWindow.Of(Window!.StartUtc, newEndUtc);
        State = PublicationState.Open;

        AddEvent(new PublicationReopened(Id, newEndUtc));
    }

    private void MustBeDraft()
    {
        if (!IsDraft)
        {
            throw new PublicationMustBeDraftException(Id);
        }
    }

    private void MustBePublished()
    {
        if (!IsPublished)
        {
            throw new PublicationMustBePublishedException(Id);
        }
    }

    private void MustBeOpen()
    {
        if (!IsOpen)
        {
            throw new PublicationMustBeOpenException(Id);
        }
    }

    private void MustBeClosed()
    {
        if (!IsClosed)
        {
            throw new PublicationMustBeClosedException(Id);
        }
    }

    private void WindowExtensionMustBeLater(DateTimeOffset newEndUtc)
    {
        if (newEndUtc <= Window!.EndUtc)
        {
            throw new WindowExtensionMustBeLaterException(Id, newEndUtc);
        }
    }
}
```

`src\DrivingLessons.Domain\Events\PublicationCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationCreated(PublicationId PublicationId, WeekStart WeekStart, ShareableLinkToken LinkToken) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\PublicationPublished.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationPublished(PublicationId PublicationId, SubmissionWindow Window) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\PublicationOpened.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationOpened(PublicationId PublicationId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\PublicationClosed.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationClosed(PublicationId PublicationId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\PublicationWindowExtended.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationWindowExtended(PublicationId PublicationId, DateTimeOffset NewEndUtc) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\PublicationReopened.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record PublicationReopened(PublicationId PublicationId, DateTimeOffset NewEndUtc) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\PublicationMustBeDraftException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeDraftException : DomainException
{
    public PublicationMustBeDraftException(PublicationId id)
        : base($"Publication {id.Value} must be draft.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\PublicationMustBePublishedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBePublishedException : DomainException
{
    public PublicationMustBePublishedException(PublicationId id)
        : base($"Publication {id.Value} must be published.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\PublicationMustBeOpenException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeOpenException : DomainException
{
    public PublicationMustBeOpenException(PublicationId id)
        : base($"Publication {id.Value} must be open.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\PublicationMustBeClosedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class PublicationMustBeClosedException : DomainException
{
    public PublicationMustBeClosedException(PublicationId id)
        : base($"Publication {id.Value} must be closed.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\WindowExtensionMustBeLaterException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class WindowExtensionMustBeLaterException : DomainException
{
    public WindowExtensionMustBeLaterException(PublicationId id, DateTimeOffset newEndUtc)
        : base($"Publication {id.Value} window extension {newEndUtc:O} must be later than the current end.")
    {
    }
}
```

`src\DrivingLessons.Domain\Repositories\IPublicationRepository.cs` (no `UnitOfWork` property — matches `IWeekScheduleRepository`; exact signatures from the contract):

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IPublicationRepository
{
    Task<Publication?> GetAsync(PublicationId id);

    Task<Publication?> GetByWeekAsync(WeekStart weekStart);

    Task<Publication?> GetByLinkTokenAsync(ShareableLinkToken token);

    Task<IReadOnlyList<Publication>> GetPublishedDueToOpenAsync(DateTimeOffset asOfUtc);

    Task<IReadOnlyList<Publication>> GetOpenDueToCloseAsync(DateTimeOffset asOfUtc);

    void Add(Publication publication);
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): publication aggregate with lifecycle, per-teacher excel versions and repository interface"
```

---

**Next:** [task-03-domain-event-dispatch.md](task-03-domain-event-dispatch.md)
