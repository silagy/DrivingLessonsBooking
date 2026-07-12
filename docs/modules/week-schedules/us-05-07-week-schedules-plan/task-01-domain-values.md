# Task 1 of 10: Domain values — IDs, WeekStart, enums, grid definition

> Part of [US-05–07: Week Schedules Module](README.md). Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\WeekScheduleId.cs`, `SlotId.cs`, `WeekStart.cs`, `SlotState.cs`, `SlotWindowType.cs`, `SlotWindowTimes.cs`, `WeekGridDefinition.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\WeekStartMustBeSundayException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\WeekStartTest.cs`, `WeekScheduleIdTest.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Common\Faker.cs`

Before coding, open `src\DrivingLessons.Domain\Values\TeacherId.cs` and `tests\DrivingLessons.Domain.Test\Values\TeacherIdTest.cs` and match their exact shape (record, private ctor, `New()`/`Of()`).

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Values\WeekStartTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class WeekStartTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var sunday = new DateOnly(2026, 7, 19);

        //when
        var weekStart = WeekStart.Of(sunday);

        //then
        weekStart.Value.ShouldBe(sunday);
    }

    [TestMethod]
    [DataRow("2026-07-20")]
    [DataRow("2026-07-21")]
    [DataRow("2026-07-22")]
    [DataRow("2026-07-23")]
    [DataRow("2026-07-24")]
    [DataRow("2026-07-25")]
    public void Of__Must_Be_Sunday(string date)
    {
        //given
        var notSunday = DateOnly.Parse(date);

        //when
        var act = () => WeekStart.Of(notSunday);

        //then
        Should.Throw<WeekStartMustBeSundayException>(act);
    }
}
```

`tests\DrivingLessons.Domain.Test\Values\WeekScheduleIdTest.cs` — copy `TeacherIdTest.cs` verbatim, rename `TeacherId` → `WeekScheduleId`.

Add to `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (inside the existing class):

```csharp
public static DateOnly FakeSunday()
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    var daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
    var weeksAhead = Random.Shared.Next(1, 52);

    return today.AddDays(daysUntilSunday + (weeksAhead * 7));
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL — `WeekStart`, `WeekScheduleId`, `WeekStartMustBeSundayException` do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Values\WeekScheduleId.cs` (copy `TeacherId.cs`, rename) and `SlotId.cs` (same, rename to `SlotId`):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record WeekScheduleId : EntityId
{
    private WeekScheduleId(Guid value)
        : base(value)
    {
    }

    public static WeekScheduleId New()
    {
        var value = Guid.NewGuid();

        return new WeekScheduleId(value);
    }

    public static WeekScheduleId Of(Guid value)
    {
        return new WeekScheduleId(value);
    }
}
```

> Match the actual `TeacherId.cs` shape exactly — if its base-call or member layout differs from the above, the existing file wins.

`src\DrivingLessons.Domain\Values\WeekStart.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record WeekStart
{
    public DateOnly Value { get; }

    private WeekStart(DateOnly value)
    {
        Value = value;
    }

    public static WeekStart Of(DateOnly value)
    {
        if (value.DayOfWeek is not DayOfWeek.Sunday)
        {
            throw new WeekStartMustBeSundayException(value);
        }

        return new WeekStart(value);
    }
}
```

`src\DrivingLessons.Domain\Exceptions\WeekStartMustBeSundayException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class WeekStartMustBeSundayException : DomainException
{
    public WeekStartMustBeSundayException(DateOnly value)
        : base($"Week start {value:yyyy-MM-dd} must be a Sunday.")
    {
    }
}
```

`src\DrivingLessons.Domain\Values\SlotState.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum SlotState
{
    Open = 10,
    Unavailable = 20
}
```

`src\DrivingLessons.Domain\Values\SlotWindowType.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum SlotWindowType
{
    Morning = 10,
    Noon = 20,
    Afternoon = 30,
    Evening = 40
}
```

`src\DrivingLessons.Domain\Values\SlotWindowTimes.cs` (hardcoded windows, decision 15):

```csharp
namespace DrivingLessons.Domain.Values;

public static class SlotWindowTimes
{
    public static TimeOnly StartOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => new TimeOnly(7, 0),
            SlotWindowType.Noon => new TimeOnly(12, 0),
            SlotWindowType.Afternoon => new TimeOnly(15, 0),
            SlotWindowType.Evening => new TimeOnly(18, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(window))
        };
    }

    public static TimeOnly EndOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => new TimeOnly(12, 0),
            SlotWindowType.Noon => new TimeOnly(15, 0),
            SlotWindowType.Afternoon => new TimeOnly(18, 0),
            SlotWindowType.Evening => new TimeOnly(22, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(window))
        };
    }
}
```

`src\DrivingLessons.Domain\Values\WeekGridDefinition.cs` (requirements §5.3):

```csharp
namespace DrivingLessons.Domain.Values;

public static class WeekGridDefinition
{
    public static readonly IReadOnlyList<DayOfWeek> Days =
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    public static IReadOnlyList<SlotWindowType> WindowsFor(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Saturday => [],
            DayOfWeek.Friday =>
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon
            ],
            _ =>
            [
                SlotWindowType.Morning,
                SlotWindowType.Noon,
                SlotWindowType.Afternoon,
                SlotWindowType.Evening
            ]
        };
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): week schedule values, week grid definition and week start rule"
```

---

**Next:** [task-02-week-schedule-aggregate.md](task-02-week-schedule-aggregate.md)
