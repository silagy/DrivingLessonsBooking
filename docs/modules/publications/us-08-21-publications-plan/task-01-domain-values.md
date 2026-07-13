# Task 1 of 14: Domain values — IDs, PublicationState, ShareableLinkToken, SubmissionWindow

> Part of the [US-08–21 Publications plan](README.md). This is the first task — no prior task required. Work on branch `9-us-08-21-publications-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\PublicationId.cs`, `TeacherExcelVersionId.cs`, `PublicationState.cs`, `ShareableLinkToken.cs`, `SubmissionWindow.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\ShareableLinkTokenMustNotBeEmptyException.cs`, `SubmissionWindowEndMustBeAfterStartException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\ShareableLinkTokenTest.cs`, `SubmissionWindowTest.cs`

Before coding, open `src\DrivingLessons.Domain\Values\WeekScheduleId.cs` and `WeekStart.cs` and match their exact shape (record, private ctor, `New()`/`Of()` factories, single-line `new(...)` bodies). The typed IDs in this task are `WeekScheduleId` with the names swapped.

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Values\ShareableLinkTokenTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class ShareableLinkTokenTest
{
    [TestMethod]
    public void New_Produces_Non_Empty_Token()
    {
        //when
        var token = ShareableLinkToken.New();

        //then
        token.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void New_Produces_Distinct_Tokens()
    {
        //when
        var first = ShareableLinkToken.New();
        var second = ShareableLinkToken.New();

        //then
        first.Value.ShouldNotBe(second.Value);
    }

    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var token = ShareableLinkToken.Of(value);

        //then
        token.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Of__Must_Not_Be_Empty(string value)
    {
        //when
        var act = () => ShareableLinkToken.Of(value);

        //then
        Should.Throw<ShareableLinkTokenMustNotBeEmptyException>(act);
    }
}
```

`tests\DrivingLessons.Domain.Test\Values\SubmissionWindowTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SubmissionWindowTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddDays(3);

        //when
        var window = SubmissionWindow.Of(startUtc, endUtc);

        //then
        window.StartUtc.ShouldBe(startUtc);
        window.EndUtc.ShouldBe(endUtc);
    }

    [TestMethod]
    public void Of__End_Must_Be_After_Start_When_Equal()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc;

        //when
        var act = () => SubmissionWindow.Of(startUtc, endUtc);

        //then
        Should.Throw<SubmissionWindowEndMustBeAfterStartException>(act);
    }

    [TestMethod]
    public void Of__End_Must_Be_After_Start_When_Earlier()
    {
        //given
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddSeconds(-1);

        //when
        var act = () => SubmissionWindow.Of(startUtc, endUtc);

        //then
        Should.Throw<SubmissionWindowEndMustBeAfterStartException>(act);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL — `ShareableLinkToken`, `SubmissionWindow`, and the two exceptions do not exist yet (compile errors).

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Values\PublicationId.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record PublicationId : EntityId
{
    private PublicationId(Guid value)
        : base(value)
    {
    }

    public static PublicationId New()
    {
        return new PublicationId(Guid.NewGuid());
    }

    public static PublicationId Of(Guid value)
    {
        return new PublicationId(value);
    }
}
```

`src\DrivingLessons.Domain\Values\TeacherExcelVersionId.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record TeacherExcelVersionId : EntityId
{
    private TeacherExcelVersionId(Guid value)
        : base(value)
    {
    }

    public static TeacherExcelVersionId New()
    {
        return new TeacherExcelVersionId(Guid.NewGuid());
    }

    public static TeacherExcelVersionId Of(Guid value)
    {
        return new TeacherExcelVersionId(value);
    }
}
```

`src\DrivingLessons.Domain\Values\PublicationState.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum PublicationState
{
    Draft = 10,
    Published = 20,
    Open = 30,
    Closed = 40
}
```

`src\DrivingLessons.Domain\Values\ShareableLinkToken.cs` (`RandomNumberGenerator` is BCL — allowed in Domain, same as `Guid.NewGuid()` in `WeekScheduleId.New`):

```csharp
using System.Security.Cryptography;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record ShareableLinkToken
{
    private const int TokenByteLength = 32;

    public string Value { get; }

    private ShareableLinkToken(string value)
    {
        Value = value;
    }

    public static ShareableLinkToken New()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var base64 = Convert.ToBase64String(bytes);
        var token = base64
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", string.Empty);

        return new ShareableLinkToken(token);
    }

    public static ShareableLinkToken Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ShareableLinkTokenMustNotBeEmptyException();
        }

        return new ShareableLinkToken(value.Trim());
    }
}
```

`src\DrivingLessons.Domain\Values\SubmissionWindow.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SubmissionWindow
{
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }

    private SubmissionWindow(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public static SubmissionWindow Of(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new SubmissionWindowEndMustBeAfterStartException(startUtc, endUtc);
        }

        return new SubmissionWindow(startUtc, endUtc);
    }
}
```

`src\DrivingLessons.Domain\Exceptions\ShareableLinkTokenMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class ShareableLinkTokenMustNotBeEmptyException : DomainException
{
    public ShareableLinkTokenMustNotBeEmptyException()
        : base("Shareable link token must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SubmissionWindowEndMustBeAfterStartException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWindowEndMustBeAfterStartException : DomainException
{
    public SubmissionWindowEndMustBeAfterStartException(DateTimeOffset startUtc, DateTimeOffset endUtc)
        : base($"Submission window end {endUtc:O} must be after start {startUtc:O}.")
    {
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): publication values, state enum, shareable link token and submission window"
```

---

**Next:** [task-02-publication-aggregate.md](task-02-publication-aggregate.md)
