# Task 2 of 10: WeekSchedule aggregate + Slot child entity

> Part of [US-05–07: Week Schedules Module](README.md). Requires task 1 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\WeekSchedule.cs`, `Entities\Slot.cs`
- Create: `src\DrivingLessons.Domain\Events\WeekScheduleCreated.cs`, `SlotMarkedUnavailable.cs`, `SlotMarkedAvailable.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\SlotMustBeOpenException.cs`, `SlotMustBeUnavailableException.cs`, `SlotNotInWeekScheduleException.cs`
- Create: `src\DrivingLessons.Domain\Repositories\IWeekScheduleRepository.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\WeekScheduleTest.cs`, `Entities\Fake\WeekScheduleFakeBuilder.cs`

Test through the aggregate root only — never instantiate `Slot` directly.

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Entities\Fake\WeekScheduleFakeBuilder.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class WeekScheduleFakeBuilder
{
    public static WeekSchedule Build()
    {
        var teacher = TeacherFakeBuilder.Build();
        var sunday = Faker.FakeSunday();
        var weekStart = WeekStart.Of(sunday);

        return WeekSchedule.Create(teacher, weekStart);
    }
}
```

`tests\DrivingLessons.Domain.Test\Entities\WeekScheduleTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class WeekScheduleTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        //then
        weekSchedule.TeacherId.ShouldBe(teacher.Id);
        weekSchedule.WeekStart.ShouldBe(weekStart);
        weekSchedule.Slots.ShouldAllBe(x => x.IsOpen);
    }

    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    public void Create__Weekday_Has_All_Four_Windows(DayOfWeek day)
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var windows = weekSchedule.Slots
                                  .Where(x => x.Day == day)
                                  .Select(x => x.Window)
                                  .ToList();

        //then
        windows.ShouldBe(
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon,
                SlotWindowType.Afternoon,
                SlotWindowType.Evening
            ],
            ignoreOrder: true);
    }

    [TestMethod]
    public void Create__Friday_Has_Morning_And_Noon_Only()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var windows = weekSchedule.Slots
                                  .Where(x => x.Day == DayOfWeek.Friday)
                                  .Select(x => x.Window)
                                  .ToList();

        //then
        windows.ShouldBe(
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon
            ],
            ignoreOrder: true);
    }

    [TestMethod]
    public void Create__Saturday_Does_Not_Exist()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();

        //when
        var saturdaySlots = weekSchedule.Slots.Where(x => x.Day == DayOfWeek.Saturday);

        //then
        saturdaySlots.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var weekSchedule = WeekSchedule.Create(teacher, weekStart);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<WeekScheduleCreated>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.TeacherId == teacher.Id)
                    .Where(x => x.WeekStart == weekStart)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        weekSchedule.MarkSlotUnavailable(slot);

        //then
        slot.IsUnavailable.ShouldBeTrue();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Add_Event()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        weekSchedule.MarkSlotUnavailable(slot);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<SlotMarkedUnavailable>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.SlotId == slot.Id)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Slot_Must_Be_Open()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        var act = () => weekSchedule.MarkSlotUnavailable(slot);

        //then
        Should.Throw<SlotMustBeOpenException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Unavailable__Slot_Must_Be_In_Week_Schedule()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var otherWeekSchedule = WeekScheduleFakeBuilder.Build();
        var foreignSlot = otherWeekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotUnavailable(foreignSlot);

        //then
        Should.Throw<SlotNotInWeekScheduleException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Available()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        weekSchedule.MarkSlotAvailable(slot);

        //then
        slot.IsOpen.ShouldBeTrue();
    }

    [TestMethod]
    public void Mark_Slot_Available__Add_Event()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        weekSchedule.MarkSlotAvailable(slot);

        //then
        weekSchedule.UncommittedEvents
                    .OfType<SlotMarkedAvailable>()
                    .Where(x => x.WeekScheduleId == weekSchedule.Id)
                    .Where(x => x.SlotId == slot.Id)
                    .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Mark_Slot_Available__Slot_Must_Be_Unavailable()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var slot = weekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotAvailable(slot);

        //then
        Should.Throw<SlotMustBeUnavailableException>(act);
    }

    [TestMethod]
    public void Mark_Slot_Available__Slot_Must_Be_In_Week_Schedule()
    {
        //given
        var weekSchedule = WeekScheduleFakeBuilder.Build();
        var otherWeekSchedule = WeekScheduleFakeBuilder.Build();
        var foreignSlot = otherWeekSchedule.Slots.First();

        //when
        var act = () => weekSchedule.MarkSlotAvailable(foreignSlot);

        //then
        Should.Throw<SlotNotInWeekScheduleException>(act);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL — `WeekSchedule`, `Slot`, events, exceptions do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Entities\Slot.cs` (child entity — mirrors `Car.cs`: `internal` mutators, no events):

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Slot : Entity<SlotId>
{
    public DayOfWeek Day { get; private set; }
    public SlotWindowType Window { get; private set; }
    public SlotState State { get; private set; }

    public bool IsOpen => State is SlotState.Open;
    public bool IsUnavailable => State is SlotState.Unavailable;

    private Slot()
    {
    }

    private Slot(SlotId id, DayOfWeek day, SlotWindowType window, SlotState state)
        : base(id)
    {
        Day = day;
        Window = window;
        State = state;
    }

    internal static Slot Create(DayOfWeek day, SlotWindowType window)
    {
        const SlotState state = SlotState.Open;
        var id = SlotId.New();

        return new Slot(id, day, window, state);
    }

    internal void MarkUnavailable()
    {
        State = SlotState.Unavailable;
    }

    internal void MarkAvailable()
    {
        State = SlotState.Open;
    }
}
```

`src\DrivingLessons.Domain\Entities\WeekSchedule.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class WeekSchedule : AggregateRoot<WeekScheduleId>
{
    private readonly List<Slot> slots = [];

    public TeacherId TeacherId { get; private set; }
    public WeekStart WeekStart { get; private set; }

    public IReadOnlyCollection<Slot> Slots => slots.AsReadOnly();

    private WeekSchedule()
    {
    }

    private WeekSchedule(WeekScheduleId id, TeacherId teacherId, WeekStart weekStart)
        : base(id)
    {
        TeacherId = teacherId;
        WeekStart = weekStart;

        var createdEvent = new WeekScheduleCreated(id, teacherId, weekStart);
        AddEvent(createdEvent);
    }

    public static WeekSchedule Create(Teacher teacher, WeekStart weekStart)
    {
        var id = WeekScheduleId.New();
        var weekSchedule = new WeekSchedule(id, teacher.Id, weekStart);
        weekSchedule.CreateOpenSlots();

        return weekSchedule;
    }

    public void MarkSlotUnavailable(Slot slot)
    {
        MustOwnSlot(slot);
        SlotMustBeOpen(slot);

        slot.MarkUnavailable();

        AddEvent(new SlotMarkedUnavailable(Id, slot.Id));
    }

    public void MarkSlotAvailable(Slot slot)
    {
        MustOwnSlot(slot);
        SlotMustBeUnavailable(slot);

        slot.MarkAvailable();

        AddEvent(new SlotMarkedAvailable(Id, slot.Id));
    }

    private void CreateOpenSlots()
    {
        foreach (var day in WeekGridDefinition.Days)
        {
            var windows = WeekGridDefinition.WindowsFor(day);

            foreach (var window in windows)
            {
                var slot = Slot.Create(day, window);
                slots.Add(slot);
            }
        }
    }

    private void MustOwnSlot(Slot slot)
    {
        var ownsSlot = slots.Contains(slot);

        if (!ownsSlot)
        {
            throw new SlotNotInWeekScheduleException(Id, slot.Id);
        }
    }

    private static void SlotMustBeOpen(Slot slot)
    {
        if (!slot.IsOpen)
        {
            throw new SlotMustBeOpenException(slot.Id);
        }
    }

    private static void SlotMustBeUnavailable(Slot slot)
    {
        if (!slot.IsUnavailable)
        {
            throw new SlotMustBeUnavailableException(slot.Id);
        }
    }
}
```

`src\DrivingLessons.Domain\Events\WeekScheduleCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record WeekScheduleCreated(WeekScheduleId WeekScheduleId, TeacherId TeacherId, WeekStart WeekStart) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\SlotMarkedUnavailable.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SlotMarkedUnavailable(WeekScheduleId WeekScheduleId, SlotId SlotId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\SlotMarkedAvailable.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SlotMarkedAvailable(WeekScheduleId WeekScheduleId, SlotId SlotId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\SlotMustBeOpenException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotMustBeOpenException : DomainException
{
    public SlotMustBeOpenException(SlotId id)
        : base($"Slot {id.Value} must be open.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SlotMustBeUnavailableException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotMustBeUnavailableException : DomainException
{
    public SlotMustBeUnavailableException(SlotId id)
        : base($"Slot {id.Value} must be unavailable.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SlotNotInWeekScheduleException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class SlotNotInWeekScheduleException : DomainException
{
    public SlotNotInWeekScheduleException(WeekScheduleId weekScheduleId, SlotId slotId)
        : base($"Slot {slotId.Value} is not in week schedule {weekScheduleId.Value}.")
    {
    }
}
```

`src\DrivingLessons.Domain\Repositories\IWeekScheduleRepository.cs` (no `UnitOfWork` property — matches `ITeacherRepository`):

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IWeekScheduleRepository
{
    Task<WeekSchedule?> GetAsync(WeekScheduleId id);

    Task<WeekSchedule?> GetByTeacherAndWeekAsync(TeacherId teacherId, WeekStart weekStart);

    void Add(WeekSchedule weekSchedule);
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): week schedule aggregate with open/unavailable slot transitions"
```

---

**Next:** [task-03-application-layer.md](task-03-application-layer.md)
