# Task 2 of 8: Week Schedule reads and Slot commands reach only the linked Teacher (backend)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 1 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 2-4 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\GetWeekSchedule\GetWeekScheduleInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\MarkSlotAvailable\MarkSlotAvailableInteractor.cs`
- Test: `tests\DrivingLessons.Application.Test\Queries\GetWeekScheduleInteractorTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Commands\MarkSlotUnavailableInteractorTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Commands\MarkSlotAvailableInteractorTest.cs` (**new**)

No DI change: `Application\DependencyInjection.cs:81-83` registers the three interactors as concrete types (`services.AddScoped<MarkSlotUnavailableInteractor>()` and so on), and `Program.cs:31` already registers `ICurrentUser` as scoped `HttpCurrentUser`, so the container resolves the new constructor parameter. Nothing else constructs these interactors: the only callers are `WeekScheduleQueryController.GetByTeacherAndWeek` and `WeekScheduleCommandController.MarkSlotUnavailable` / `MarkSlotAvailable`, which receive them through `[FromServices]` and keep their `ExecuteAsync` calls unchanged.

**Interfaces:**
- Consumes (task 1):
  - `DrivingLessons.Application.Auth.ICurrentUser { UserId Id; Role Role; TeacherId? TeacherId }`
  - `DrivingLessons.Application.Auth.CurrentUserExtension.MayReach(this ICurrentUser user, TeacherId teacherId)`: true for an Administrator (linked or not), for a Teacher only when `user.TeacherId == teacherId`.
  - Existing `WeekScheduleNotFoundException(Guid teacherId, DateOnly weekStart)` and `WeekScheduleNotFoundException(WeekScheduleId id)` (`Application\Common\Exceptions\`), mapped to 404 by `ApiExceptionFilter`.
- Produces:
  - `GetWeekScheduleInteractor(IWeekScheduleQueries queries, ICurrentUser currentUser)`; `ExecuteAsync(Guid teacherId, DateOnly weekStart)` unchanged. A Teacher asking for another Teacher's week gets `WeekScheduleNotFoundException(teacherId, weekStart)` before the query runs.
  - `MarkSlotUnavailableInteractor(IWeekScheduleRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser)` and `MarkSlotAvailableInteractor` with the same parameters; `ExecuteAsync(Guid id, Guid slotId)` unchanged. A Teacher marking a Slot in another Teacher's Week Schedule gets `WeekScheduleNotFoundException(weekScheduleId)` after the load, before the Slot lookup, and nothing is committed.
  - HTTP contract (task 4 smoke, tasks 6-8): for a Teacher-role User, `GET api/week-schedules/by-teacher-and-week?teacherId={other}` and `POST api/week-schedules/{otherTeachersWeek}/slots/{slotId}/mark-unavailable|mark-available` return **404**, with the same body as a Week Schedule that doesn't exist. An Administrator, linked or not, is unaffected.

**Why:** AC 2-4, Review Focus 1-2. A Teacher-role token passes the `TeacherOrAdministrator` policy on these three endpoints (#89), so the interactor is the only place that can tell "own Teacher" from "other Teacher". The refusal reuses the not-found exceptions so a crafted id can't even confirm that another Teacher's Week Schedule exists. `TeacherId.Of(teacherId)` follows the other interactors (`CreateWeekScheduleInteractor`, `DeleteTeacherInteractor`); an empty `teacherId` now fails in `EntityId` like everywhere else instead of reaching the query.

- [ ] **Step 1: Write the failing Get Week Schedule test**

Create `tests\DrivingLessons.Application.Test\Queries\GetWeekScheduleInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetWeekScheduleInteractorTest
{
    private static readonly DateOnly WeekStartDate = new(2026, 10, 4);

    private IWeekScheduleQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private GetWeekScheduleInteractor interactor = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;
    private GetWeekScheduleResponse ownWeekSchedule = null!;
    private GetWeekScheduleResponse otherWeekSchedule = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IWeekScheduleQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetWeekScheduleInteractor(queries, currentUser);

        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
        ownWeekSchedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = ownTeacherId.Value,
            WeekStart = WeekStartDate
        };
        otherWeekSchedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = otherTeacherId.Value,
            WeekStart = WeekStartDate
        };

        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._))
         .Returns((GetWeekScheduleResponse?)null);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(ownTeacherId.Value, WeekStartDate))
         .Returns(ownWeekSchedule);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(otherTeacherId.Value, WeekStartDate))
         .Returns(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Teacher_Reaches_Own_Week_Schedule()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(ownTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(ownWeekSchedule);
    }

    [TestMethod]
    public async Task Other_Teachers_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);
        var notFound = new WeekScheduleNotFoundException(otherTeacherId.Value, WeekStartDate);

        //when
        var act = () => interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        var exception = await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        exception.Message.ShouldBe(notFound.Message);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Without_A_Linked_Teacher_Reaches_No_Week_Schedule()
    {
        //given
        SignInAs(Role.Teacher, null);

        //when
        var act = () => interactor.ExecuteAsync(ownTeacherId.Value, WeekStartDate);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Reaches_Any_Teachers_Week_Schedule()
    {
        //given
        SignInAs(Role.Administrator, null);

        //when
        var result = await interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Linked_Administrator_Reaches_Another_Teachers_Week_Schedule()
    {
        //given
        SignInAs(Role.Administrator, ownTeacherId);

        //when
        var result = await interactor.ExecuteAsync(otherTeacherId.Value, WeekStartDate);

        //then
        result.ShouldBe(otherWeekSchedule);
    }

    [TestMethod]
    public async Task Missing_Own_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, ownTeacherId);
        var nextWeek = WeekStartDate.AddDays(7);

        //when
        var act = () => interactor.ExecuteAsync(ownTeacherId.Value, nextWeek);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => queries.GetByTeacherAndWeekAsync(ownTeacherId.Value, nextWeek)).MustHaveHappenedOnceExactly();
    }

    private void SignInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
```

- [ ] **Step 2: Write the failing Mark Slot Unavailable test**

Create `tests\DrivingLessons.Application.Test\Commands\MarkSlotUnavailableInteractorTest.cs`. Application.Test has no FakeBuilders, so the Week Schedule comes from `Teacher.Create` + `WeekSchedule.Create` (every Slot starts Open) and the Slot is picked through the aggregate root.

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.MarkSlotUnavailable;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class MarkSlotUnavailableInteractorTest
{
    private IWeekScheduleRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ICurrentUser currentUser = null!;
    private MarkSlotUnavailableInteractor interactor = null!;
    private Teacher teacher = null!;
    private Teacher otherTeacher = null!;
    private WeekSchedule weekSchedule = null!;
    private Slot slot = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IWeekScheduleRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new MarkSlotUnavailableInteractor(repository, unitOfWork, currentUser);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        otherTeacher = Teacher.Create(TeacherName.Of("Yael Carmi"), Email.Of("yael@school.example"));
        weekSchedule = WeekSchedule.Create(teacher, WeekStart.Of(new DateOnly(2026, 10, 4)));
        slot = weekSchedule.Slots.First();

        A.CallTo(() => repository.GetAsync(A<WeekScheduleId>._)).Returns((WeekSchedule?)null);
        A.CallTo(() => repository.GetAsync(weekSchedule.Id)).Returns(weekSchedule);
    }

    [TestMethod]
    public async Task Teacher_Marks_Own_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_Slot_Is_Not_Marked()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);
        var notFound = new WeekScheduleNotFoundException(weekSchedule.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        var exception = await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        exception.Message.ShouldBe(notFound.Message);
        slot.State.ShouldBe(SlotState.Open);
        weekSchedule.UncommittedEvents.OfType<SlotMarkedUnavailable>().ShouldBeEmpty();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Other_Teachers_Week_Schedule_Is_Not_Found_Before_The_Slot_Lookup()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, Guid.NewGuid());

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Marks_Any_Teachers_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Administrator, null);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Marks_Another_Teachers_Slot_Unavailable()
    {
        //given
        SignInAs(Role.Administrator, otherTeacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Unavailable);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), slot.Id.Value);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private void SignInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
```

- [ ] **Step 3: Write the failing Mark Slot Available test**

Create `tests\DrivingLessons.Application.Test\Commands\MarkSlotAvailableInteractorTest.cs`. The Slot is marked Unavailable through the aggregate in `Init`, so marking it Open is a valid transition.

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.MarkSlotAvailable;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class MarkSlotAvailableInteractorTest
{
    private IWeekScheduleRepository repository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ICurrentUser currentUser = null!;
    private MarkSlotAvailableInteractor interactor = null!;
    private Teacher teacher = null!;
    private Teacher otherTeacher = null!;
    private WeekSchedule weekSchedule = null!;
    private Slot slot = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IWeekScheduleRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new MarkSlotAvailableInteractor(repository, unitOfWork, currentUser);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        otherTeacher = Teacher.Create(TeacherName.Of("Yael Carmi"), Email.Of("yael@school.example"));
        weekSchedule = WeekSchedule.Create(teacher, WeekStart.Of(new DateOnly(2026, 10, 4)));
        slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        A.CallTo(() => repository.GetAsync(A<WeekScheduleId>._)).Returns((WeekSchedule?)null);
        A.CallTo(() => repository.GetAsync(weekSchedule.Id)).Returns(weekSchedule);
    }

    [TestMethod]
    public async Task Teacher_Marks_Own_Slot_Available()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Open);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Other_Teachers_Slot_Is_Not_Marked()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);
        var notFound = new WeekScheduleNotFoundException(weekSchedule.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        var exception = await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        exception.Message.ShouldBe(notFound.Message);
        slot.State.ShouldBe(SlotState.Unavailable);
        weekSchedule.UncommittedEvents.OfType<SlotMarkedAvailable>().ShouldBeEmpty();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Other_Teachers_Week_Schedule_Is_Not_Found_Before_The_Slot_Lookup()
    {
        //given
        SignInAs(Role.Teacher, otherTeacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(weekSchedule.Id.Value, Guid.NewGuid());

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Administrator_Marks_Any_Teachers_Slot_Available()
    {
        //given
        SignInAs(Role.Administrator, null);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Open);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Linked_Administrator_Marks_Another_Teachers_Slot_Available()
    {
        //given
        SignInAs(Role.Administrator, otherTeacher.Id);

        //when
        await interactor.ExecuteAsync(weekSchedule.Id.Value, slot.Id.Value);

        //then
        slot.State.ShouldBe(SlotState.Open);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Week_Schedule_Is_Not_Found()
    {
        //given
        SignInAs(Role.Teacher, teacher.Id);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), slot.Id.Value);

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private void SignInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetWeekScheduleInteractorTest|FullyQualifiedName~MarkSlotUnavailableInteractorTest|FullyQualifiedName~MarkSlotAvailableInteractorTest"`

**Expected:** FAIL at build with `CS1729: 'GetWeekScheduleInteractor' does not contain a constructor that takes 2 arguments` and `CS1729: 'MarkSlotUnavailableInteractor' does not contain a constructor that takes 3 arguments` (and the same for `MarkSlotAvailableInteractor`).

- [ ] **Step 5: Scope Get Week Schedule**

Replace `src\DrivingLessons.Application\Queries\GetWeekSchedule\GetWeekScheduleInteractor.cs`. Before:

```csharp
using DrivingLessons.Application.Common.Exceptions;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleInteractor
{
    private readonly IWeekScheduleQueries queries;

    public GetWeekScheduleInteractor(IWeekScheduleQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetWeekScheduleResponse> ExecuteAsync(Guid teacherId, DateOnly weekStart)
    {
        var weekSchedule = await queries.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (weekSchedule is null)
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        return weekSchedule;
    }
}
```

After:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetWeekSchedule;

public class GetWeekScheduleInteractor
{
    private readonly IWeekScheduleQueries queries;
    private readonly ICurrentUser currentUser;

    public GetWeekScheduleInteractor(IWeekScheduleQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<GetWeekScheduleResponse> ExecuteAsync(Guid teacherId, DateOnly weekStart)
    {
        var resolvedTeacherId = TeacherId.Of(teacherId);

        if (!currentUser.MayReach(resolvedTeacherId))
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        var weekSchedule = await queries.GetByTeacherAndWeekAsync(teacherId, weekStart);

        if (weekSchedule is null)
        {
            throw new WeekScheduleNotFoundException(teacherId, weekStart);
        }

        return weekSchedule;
    }
}
```

- [ ] **Step 6: Scope Mark Slot Unavailable**

Replace `src\DrivingLessons.Application\Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs`. Before:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotUnavailable;

public class MarkSlotUnavailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public MarkSlotUnavailableInteractor(IWeekScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotUnavailable(slot);

        await unitOfWork.CommitAsync();
    }
}
```

After:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotUnavailable;

public class MarkSlotUnavailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUser currentUser;

    public MarkSlotUnavailableInteractor(
        IWeekScheduleRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        if (!currentUser.MayReach(weekSchedule.TeacherId))
        {
            throw new WeekScheduleNotFoundException(weekScheduleId);
        }

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotUnavailable(slot);

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 7: Scope Mark Slot Available**

Replace `src\DrivingLessons.Application\Commands\MarkSlotAvailable\MarkSlotAvailableInteractor.cs`. Before:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotAvailable;

public class MarkSlotAvailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public MarkSlotAvailableInteractor(IWeekScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotAvailable(slot);

        await unitOfWork.CommitAsync();
    }
}
```

After:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.MarkSlotAvailable;

public class MarkSlotAvailableInteractor
{
    private readonly IWeekScheduleRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUser currentUser;

    public MarkSlotAvailableInteractor(
        IWeekScheduleRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
    }

    public async Task ExecuteAsync(Guid id, Guid slotId)
    {
        var weekScheduleId = WeekScheduleId.Of(id);

        var weekSchedule = await repository.GetAsync(weekScheduleId)
                           ?? throw new WeekScheduleNotFoundException(weekScheduleId);

        if (!currentUser.MayReach(weekSchedule.TeacherId))
        {
            throw new WeekScheduleNotFoundException(weekScheduleId);
        }

        var resolvedSlotId = SlotId.Of(slotId);

        var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == resolvedSlotId)
                   ?? throw new SlotNotFoundException(weekScheduleId, resolvedSlotId);

        weekSchedule.MarkSlotAvailable(slot);

        await unitOfWork.CommitAsync();
    }
}
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetWeekScheduleInteractorTest|FullyQualifiedName~MarkSlotUnavailableInteractorTest|FullyQualifiedName~MarkSlotAvailableInteractorTest"`

**Expected:** PASS (18 tests: 6 per class). If `Other_Teachers_Week_Schedule_Is_Not_Found_Before_The_Slot_Lookup` fails with `SlotNotFoundException`, the `MayReach` guard sits after the Slot lookup; move it up to right after the load.

Then confirm nothing else constructs the interactors by hand (only `DependencyInjection.cs` and the two controllers may name them):

```bash
git grep -n "GetWeekScheduleInteractor\|MarkSlotUnavailableInteractor\|MarkSlotAvailableInteractor" -- src tests
```

**Expected:** the three interactor files, `src/DrivingLessons.Application/DependencyInjection.cs` (three `AddScoped<...>()` lines), `WeekScheduleQueryController.cs`, `WeekScheduleCommandController.cs` (two lines) and the three new test files. No `new GetWeekScheduleInteractor(` or `new MarkSlot` outside the tests.

Then build and run the whole backend suite:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

**Expected:** build succeeds with 0 warnings and 0 errors; both suites PASS (`SourceTextTest` included: no long dashes or ellipsis in the new files).

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Application/Queries/GetWeekSchedule/GetWeekScheduleInteractor.cs src/DrivingLessons.Application/Commands/MarkSlotUnavailable/MarkSlotUnavailableInteractor.cs src/DrivingLessons.Application/Commands/MarkSlotAvailable/MarkSlotAvailableInteractor.cs tests/DrivingLessons.Application.Test/Queries/GetWeekScheduleInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/MarkSlotUnavailableInteractorTest.cs tests/DrivingLessons.Application.Test/Commands/MarkSlotAvailableInteractorTest.cs
git commit -m "feat(api): Week Schedule reads and Slot changes reach only the linked Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
