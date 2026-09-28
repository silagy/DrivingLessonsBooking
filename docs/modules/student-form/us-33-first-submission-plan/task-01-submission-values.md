# Task 1 of 11: Submission value objects — typed IDs, target count, session type, constraint, rank, pick

> Part of [US-33…US-41: First Submission](README.md). Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\SubmissionId.cs`, `SlotRequestId.cs`, `TargetSessionCount.cs`, `SessionType.cs`, `SlotConstraint.cs`, `Rank.cs`, `SlotPick.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\TargetSessionCountMustBePositiveException.cs`, `SlotConstraintMustNotBeEmptyException.cs`, `SlotConstraintMustNotExceedMaxLengthException.cs`, `RankMustBePositiveException.cs`, `SessionTypeMustBeSingleOrDoubleException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\TargetSessionCountTest.cs`, `SlotConstraintTest.cs`, `RankTest.cs`, `SlotPickTest.cs`

**Interfaces:**
- Consumes (already on `main`): `Slot` (child of `WeekSchedule`, reached through `WeekScheduleFakeBuilder.Build().Slots`), `Faker.FakeString()`, `DomainException`.
- Produces (tasks 2–4 rely on these exact names):
  - `record SubmissionId : EntityId` and `record SlotRequestId : EntityId` — `New()`, `Of(Guid)`.
  - `record TargetSessionCount { int Value; static Of(int); bool IsCoveredBy(int pickCount); }` — `Of` throws `TargetSessionCountMustBePositiveException` below 1; **no upper limit** (requirements §5.6).
  - `enum SessionType { Single = 10, Double = 20 }`.
  - `record SlotConstraint { const int MaxLength = 200; string Value; static Of(string); }` — trims; throws `SlotConstraintMustNotBeEmptyException` (blank) or `SlotConstraintMustNotExceedMaxLengthException` (> 200 after trimming). Absence is `null`, never an empty instance.
  - `record Rank { int Value; static Of(int); }` — throws `RankMustBePositiveException` below 1.
  - `record SlotPick { Slot Slot; SessionType SessionType; SlotConstraint? Constraint; static Of(Slot, SessionType, SlotConstraint?); }` — throws `SessionTypeMustBeSingleOrDoubleException` for an undefined enum value (the JSON enum converter accepts integers, so `"sessionType": 99` reaches the domain — Review Focus 9).

Precedents to open before coding: `Domain\Values\StudentName.cs` (normalizing single value), `Domain\Values\NationalId.cs` (length rule + payload-free exception), `Domain\Values\SlotId.cs` (typed ID), `tests\DrivingLessons.Domain.Test\Values\StudentNameTest.cs` (test shape).

Why these types (domain-building-blocks "Enum vs value object"): `SessionType` is a fixed compile-time set → enum. The target count and rank are ranges → value objects. The constraint is free text with a length cap → value object whose exceptions carry **no payload** (a constraint can hold personal details such as a workplace, and the message becomes the ProblemDetails `detail` returned to an anonymous caller). `SlotPick` is the domain's input shape for one pick — a resolved `Slot` plus the student's choices — so `Submission.Create/Revise` (task 2) take one list instead of three parallel ones. Rank is not on `SlotPick`: it is the pick's position in the list (US-37, decision 4).

- [x] **Step 1: Write the failing value-object tests**

`tests\DrivingLessons.Domain.Test\Values\TargetSessionCountTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TargetSessionCountTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(40)]
    public void Target_Count_Is_Any_Positive_Number(int value)
    {
        //when
        var targetCount = TargetSessionCount.Of(value);

        //then
        targetCount.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Target_Count_Must_Be_Positive(int value)
    {
        //when
        var act = () => TargetSessionCount.Of(value);

        //then
        Should.Throw<TargetSessionCountMustBePositiveException>(act);
    }

    [TestMethod]
    [DataRow(2, 2)]
    [DataRow(2, 5)]
    public void Target_Is_Covered_By_At_Least_As_Many_Picks(int target, int pickCount)
    {
        //given
        var targetCount = TargetSessionCount.Of(target);

        //when
        var isCovered = targetCount.IsCoveredBy(pickCount);

        //then
        isCovered.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(2, 1)]
    [DataRow(3, 0)]
    [DataRow(30, 22)]
    public void Target_Is_Not_Covered_By_Fewer_Picks(int target, int pickCount)
    {
        //given
        var targetCount = TargetSessionCount.Of(target);

        //when
        var isCovered = targetCount.IsCoveredBy(pickCount);

        //then
        isCovered.ShouldBeFalse();
    }
}
```

The `(30, 22)` row is **Review Focus 8** — a target far above what the grid can ever offer (22 slots) is a valid target that simply can never be covered; the domain does not cap it (requirements §5.6 "no upper limit").

`tests\DrivingLessons.Domain.Test\Values\SlotConstraintTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SlotConstraintTest
{
    [TestMethod]
    public void Constraint_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var constraint = SlotConstraint.Of($"  {raw}  ");

        //then
        constraint.Value.ShouldBe(raw);
    }

    [TestMethod]
    public void Constraint_May_Be_Exactly_The_Max_Length()
    {
        //given
        var raw = new string('a', SlotConstraint.MaxLength);

        //when
        var constraint = SlotConstraint.Of(raw);

        //then
        constraint.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constraint_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => SlotConstraint.Of(value!);

        //then
        Should.Throw<SlotConstraintMustNotBeEmptyException>(act);
    }

    [TestMethod]
    public void Constraint_Must_Not_Exceed_The_Max_Length()
    {
        //given
        var raw = new string('a', SlotConstraint.MaxLength + 1);

        //when
        var act = () => SlotConstraint.Of(raw);

        //then
        Should.Throw<SlotConstraintMustNotExceedMaxLengthException>(act);
    }

    [TestMethod]
    public void Too_Long_Message_Does_Not_Reveal_The_Constraint()
    {
        //given
        var raw = $"{Faker.FakeString()}{new string('a', SlotConstraint.MaxLength)}";

        //when
        var act = () => SlotConstraint.Of(raw);

        //then
        var exception = Should.Throw<SlotConstraintMustNotExceedMaxLengthException>(act);
        exception.Message.ShouldNotContain(raw[..20]);
    }
}
```

`tests\DrivingLessons.Domain.Test\Values\RankTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class RankTest
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    public void Rank_Is_Any_Positive_Position(int value)
    {
        //when
        var rank = Rank.Of(value);

        //then
        rank.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Rank_Must_Be_Positive(int value)
    {
        //when
        var act = () => Rank.Of(value);

        //then
        Should.Throw<RankMustBePositiveException>(act);
    }
}
```

`tests\DrivingLessons.Domain.Test\Values\SlotPickTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SlotPickTest
{
    [TestMethod]
    public void Pick_Keeps_Slot_Session_Type_And_Constraint()
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();
        var constraint = SlotConstraint.Of(Faker.FakeString());

        //when
        var pick = SlotPick.Of(slot, SessionType.Double, constraint);

        //then
        pick.Slot.ShouldBe(slot);
        pick.SessionType.ShouldBe(SessionType.Double);
        pick.Constraint.ShouldBe(constraint);
    }

    [TestMethod]
    public void Constraint_Is_Optional()
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();

        //when
        var pick = SlotPick.Of(slot, SessionType.Single, null);

        //then
        pick.Constraint.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    [DataRow(99)]
    public void Session_Type_Must_Be_Single_Or_Double(int value)
    {
        //given
        var slot = WeekScheduleFakeBuilder.Build().Slots.First();
        var sessionType = (SessionType)value;

        //when
        var act = () => SlotPick.Of(slot, sessionType, null);

        //then
        Should.Throw<SessionTypeMustBeSingleOrDoubleException>(act);
    }
}
```

The slot comes from the `WeekSchedule` root's public `Slots` — never `Slot.Create` (domain-testing "test through aggregate roots only"; `Slot.Create` is `internal` anyway).

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~TargetSessionCountTest|FullyQualifiedName~SlotConstraintTest|FullyQualifiedName~RankTest|FullyQualifiedName~SlotPickTest"`
Expected: build FAILS — `TargetSessionCount`, `SlotConstraint`, `Rank`, `SlotPick`, `SessionType` and the five exceptions do not exist.

- [x] **Step 3: Typed IDs**

`src\DrivingLessons.Domain\Values\SubmissionId.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record SubmissionId : EntityId
{
    private SubmissionId(Guid value)
        : base(value)
    {
    }

    public static SubmissionId New()
    {
        return new SubmissionId(Guid.NewGuid());
    }

    public static SubmissionId Of(Guid value)
    {
        return new SubmissionId(value);
    }
}
```

`src\DrivingLessons.Domain\Values\SlotRequestId.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record SlotRequestId : EntityId
{
    private SlotRequestId(Guid value)
        : base(value)
    {
    }

    public static SlotRequestId New()
    {
        return new SlotRequestId(Guid.NewGuid());
    }

    public static SlotRequestId Of(Guid value)
    {
        return new SlotRequestId(value);
    }
}
```

No tests for these two — they hold no logic beyond the `EntityId` base (already covered by `TeacherIdTest` / `WeekScheduleIdTest`).

- [x] **Step 4: Exceptions**

`src\DrivingLessons.Domain\Exceptions\TargetSessionCountMustBePositiveException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TargetSessionCountMustBePositiveException : DomainException
{
    public TargetSessionCountMustBePositiveException(int value)
        : base($"Target session count must be at least 1 (was {value}).")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SlotConstraintMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SlotConstraintMustNotBeEmptyException : DomainException
{
    public SlotConstraintMustNotBeEmptyException()
        : base("Slot constraint must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SlotConstraintMustNotExceedMaxLengthException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SlotConstraintMustNotExceedMaxLengthException : DomainException
{
    public SlotConstraintMustNotExceedMaxLengthException(int maxLength)
        : base($"Slot constraint must be at most {maxLength} characters.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\RankMustBePositiveException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class RankMustBePositiveException : DomainException
{
    public RankMustBePositiveException(int value)
        : base($"Rank must be at least 1 (was {value}).")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SessionTypeMustBeSingleOrDoubleException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SessionTypeMustBeSingleOrDoubleException : DomainException
{
    public SessionTypeMustBeSingleOrDoubleException()
        : base("Session type must be Single or Double.")
    {
    }
}
```

- [x] **Step 5: `TargetSessionCount`, `SessionType`, `Rank`**

`src\DrivingLessons.Domain\Values\TargetSessionCount.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TargetSessionCount
{
    private const int Minimum = 1;

    public int Value { get; }

    private TargetSessionCount(int value)
    {
        Value = value;
    }

    public static TargetSessionCount Of(int value)
    {
        if (value < Minimum)
        {
            throw new TargetSessionCountMustBePositiveException(value);
        }

        return new TargetSessionCount(value);
    }

    public bool IsCoveredBy(int pickCount)
    {
        return pickCount >= Value;
    }
}
```

`IsCoveredBy` is the "Picks ≥ target count" rule (requirements §7 validation) living on the type, so `Submission` never unwraps `.Value` to compare (domain-building-blocks "Usage Rules").

`src\DrivingLessons.Domain\Values\SessionType.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum SessionType
{
    Single = 10,
    Double = 20
}
```

`src\DrivingLessons.Domain\Values\Rank.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Rank
{
    private const int First = 1;

    public int Value { get; }

    private Rank(int value)
    {
        Value = value;
    }

    public static Rank Of(int value)
    {
        if (value < First)
        {
            throw new RankMustBePositiveException(value);
        }

        return new Rank(value);
    }
}
```

- [x] **Step 6: `SlotConstraint`**

`src\DrivingLessons.Domain\Values\SlotConstraint.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SlotConstraint
{
    public const int MaxLength = 200;

    public string Value { get; }

    private SlotConstraint(string value)
    {
        Value = value;
    }

    public static SlotConstraint Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SlotConstraintMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            throw new SlotConstraintMustNotExceedMaxLengthException(MaxLength);
        }

        return new SlotConstraint(normalized);
    }
}
```

`MaxLength` is `public const` so the EF configuration (task 3) sizes the column from the same number (`HasMaxLength(SlotConstraint.MaxLength)`), and the client's `maxlength` (task 9) mirrors it for immediate feedback (**Review Focus 7**). 200 characters is three lines on a 375px screen — "only after 16:00" and "pick me up from work" fit many times over.

- [x] **Step 7: `SlotPick`**

`src\DrivingLessons.Domain\Values\SlotPick.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SlotPick
{
    public Slot Slot { get; }
    public SessionType SessionType { get; }
    public SlotConstraint? Constraint { get; }

    private SlotPick(Slot slot, SessionType sessionType, SlotConstraint? constraint)
    {
        Slot = slot;
        SessionType = sessionType;
        Constraint = constraint;
    }

    public static SlotPick Of(Slot slot, SessionType sessionType, SlotConstraint? constraint)
    {
        if (!Enum.IsDefined(sessionType))
        {
            throw new SessionTypeMustBeSingleOrDoubleException();
        }

        return new SlotPick(slot, sessionType, constraint);
    }
}
```

`Enum.IsDefined` is the one place an undefined `SessionType` can be stopped: `Program.cs` registers `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, whose default `allowIntegerValues: true` binds `"sessionType": 99` without a 400.

- [x] **Step 8: Run tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~TargetSessionCountTest|FullyQualifiedName~SlotConstraintTest|FullyQualifiedName~RankTest|FullyQualifiedName~SlotPickTest"`
Expected: 26 tests PASS (TargetSessionCount 10, SlotConstraint 7, Rank 4, SlotPick 5).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS.

- [x] **Step 9: Commit**

```bash
git add src/DrivingLessons.Domain/Values src/DrivingLessons.Domain/Exceptions tests/DrivingLessons.Domain.Test/Values
git commit -m "feat(domain): submission value objects

Typed submission and slot-request IDs, a target session count with no
upper limit, Single/Double session type, a trimmed 200-character slot
constraint, rank, and the slot pick a submission is built from.
Exceptions carry no free text."
```

---

**Next:** [task-02-submission-aggregate.md](task-02-submission-aggregate.md)
