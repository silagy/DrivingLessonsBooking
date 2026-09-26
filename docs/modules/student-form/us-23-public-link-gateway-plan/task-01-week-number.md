c# Task 1 of 6: `WeekStart.WeekNumber` + teacher email uses it

> Part of [US-23: Public Link Gateway](README.md). Work on branch `24-us-23-public-link-gateway` (create it from `main` if it does not exist: `git switch -c 24-us-23-public-link-gateway`), commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Domain\Values\WeekStart.cs`
- Modify: `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs:1,37-38`
- Test: `tests\DrivingLessons.Domain.Test\Values\WeekStartTest.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `public int WeekNumber { get; }` on `DrivingLessons.Domain.Values.WeekStart` — the ISO-8601 week number of the week's Monday. Task 2's response DTO projects `x.WeekStart.WeekNumber`.

**Why:** the student page shows "Week 25 (Jun 14–19)" (mockup `SWinClosed`) and the teacher's email subject says "Week N Requests…" (requirements §6.4). Both must agree. The grid week runs Sunday–Friday, so the Monday decides the ISO week; today the email handler takes the ISO week of the **Sunday**, which is one week behind (Sunday belongs to the previous ISO week). Behavior lives with the data (`ddd-architecture.md`), so it goes on the value object.

- [x] **Step 1: Write the failing test**

Add this test method to `tests\DrivingLessons.Domain.Test\Values\WeekStartTest.cs`, after `Of__Must_Be_Sunday` (inside the class):

```csharp
    [TestMethod]
    [DataRow("2026-06-14", 25)]
    [DataRow("2026-12-27", 53)]
    [DataRow("2027-01-03", 1)]
    [DataRow("2025-12-28", 1)]
    public void Week_Number_Is_The_Iso_Week_Of_Its_Monday(string sunday, int expectedWeekNumber)
    {
        //given
        var weekStart = WeekStart.Of(DateOnly.Parse(sunday));

        //when
        var weekNumber = weekStart.WeekNumber;

        //then
        weekNumber.ShouldBe(expectedWeekNumber);
    }
```

The rows pin the year boundaries: 2026 has 53 ISO weeks (it starts on a Thursday), so the week of Sun 27 Dec 2026 is week 53; the week of Sun 28 Dec 2025 is week 1 **of 2026**. A Sunday-based calculation would return 24, 52, 53, 52 — every row fails on the old logic.

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~WeekStartTest"`
Expected: build FAILS with `'WeekStart' does not contain a definition for 'WeekNumber'`.

- [x] **Step 3: Implement `WeekNumber`**

Replace the whole of `src\DrivingLessons.Domain\Values\WeekStart.cs` with:

```csharp
using System.Globalization;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record WeekStart
{
    private const int DaysFromSundayToMonday = 1;

    public DateOnly Value { get; }

    public int WeekNumber
    {
        get
        {
            var monday = Value.AddDays(DaysFromSundayToMonday);
            var mondayDateTime = monday.ToDateTime(TimeOnly.MinValue);

            return ISOWeek.GetWeekOfYear(mondayDateTime);
        }
    }

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

`WeekNumber` has no backing field, so record equality and the EF `WeekStartConverter` mapping are unchanged.

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~WeekStartTest"`
Expected: PASS (all `WeekStartTest` cases, including the 4 new rows).

- [x] **Step 5: Make the teacher email use the same number**

In `src\DrivingLessons.Application\EventHandlers\PublicationClosedHandler.cs`:

1. Delete the first line `using System.Globalization;` (no longer used).
2. Replace these two lines inside `HandleAsync`:

```csharp
        var weekDate = publication.WeekStart.Value.ToDateTime(TimeOnly.MinValue);
        var week = ISOWeek.GetWeekOfYear(weekDate);
```

with:

```csharp
        var week = publication.WeekStart.WeekNumber;
```

The rest of the method (`$"Week {week} Requests - ..."`) is unchanged.

- [x] **Step 6: Full backend check**

Run: `dotnet build` then `dotnet test`
Expected: build clean (no unused-using warnings introduced), all Domain and Application tests PASS.

- [x] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain/Values/WeekStart.cs src/DrivingLessons.Application/EventHandlers/PublicationClosedHandler.cs tests/DrivingLessons.Domain.Test/Values/WeekStartTest.cs
git commit -m "feat(domain): derive week number from the week's Monday

The Sunday-Friday grid belongs to the ISO week of its Monday. The
teacher email now uses the same number the student form will show."
```

---

**Next:** [task-02-link-query.md](task-02-link-query.md)
