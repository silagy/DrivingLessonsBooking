# Task 1 of 10: NationalId value object

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\NationalId.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\NationalIdMustBeDigitsException.cs`, `NationalIdMustBeAtMostNineDigitsException.cs`, `NationalIdMustHaveValidCheckDigitException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\NationalIdTest.cs`
- Modify: `tests\DrivingLessons.Domain.Test\Common\Faker.cs`

Before coding, open `src\DrivingLessons.Domain\Values\TeacherName.cs`, `src\DrivingLessons.Domain\Exceptions\EmailMustBeValidException.cs` and `tests\DrivingLessons.Domain.Test\Values\TeacherNameTest.cs` and match their exact shape (record, private ctor, static `Of()`, parameterless exception message).

**PII rule (planning decision 1):** exception messages must never carry the offending ID value — mirror `EmailMustBeValidException`, which carries no payload. Roster membership remains the actual access gate; this value object only validates format.

**Bidi characters are written as `\u` escape sequences** in every C# literal below — never paste the invisible characters themselves into source files.

**Check-digit background** (Israeli national ID): over the 9 digits, weights alternate 1,2,1,2,… by position; any two-digit product is reduced by 9 (digit sum); the total must be divisible by 10. Zero-padding never changes the checksum because leading zeros contribute 0 — so a short valid ID equals its padded 9-digit form.

The two fixed test values are verified by hand:

- `"1234566"` → padded `"001234566"`: products per position are 0·1=0, 0·2=0, 1·1=1, 2·2=4, 3·1=3, 4·2=8, 5·1=5, 6·2=12→3, 6·1=6 — sum 30, divisible by 10 ✓
- `"123456782"`: 1·1=1, 2·2=4, 3·1=3, 4·2=8, 5·1=5, 6·2=12→3, 7·1=7, 8·2=16→7, 2·1=2 — sum 40 ✓

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Values\NationalIdTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class NationalIdTest
{
    [TestMethod]
    public void National_Id_Is_Zero_Padded_To_Nine_Digits()
    {
        //given
        var shortId = "1234566";

        //when
        var nationalId = NationalId.Of(shortId);

        //then
        nationalId.Value.ShouldBe("001234566");
    }

    [TestMethod]
    public void National_Id_Strips_Directional_Marks()
    {
        //given
        var wrapped = "\u200F123456782\u200E";

        //when
        var nationalId = NationalId.Of(wrapped);

        //then
        nationalId.Value.ShouldBe("123456782");
    }

    [TestMethod]
    [DataRow("12345678a")]
    [DataRow("123-45678")]
    [DataRow("")]
    [DataRow(" ")]
    public void Must_Contain_Only_Digits(string value)
    {
        //when
        var act = () => NationalId.Of(value);

        //then
        Should.Throw<NationalIdMustBeDigitsException>(act);
    }

    [TestMethod]
    [DataRow("1234567890")]
    public void Must_Be_At_Most_Nine_Digits(string value)
    {
        //when
        var act = () => NationalId.Of(value);

        //then
        Should.Throw<NationalIdMustBeAtMostNineDigitsException>(act);
    }

    [TestMethod]
    public void Must_Have_Valid_Check_Digit()
    {
        //given
        var valid = Faker.FakeNationalId().Value;
        var lastDigit = valid[^1] - '0';
        var flippedDigit = (lastDigit + 1) % 10;
        var invalid = valid[..8] + flippedDigit;

        //when
        var act = () => NationalId.Of(invalid);

        //then
        Should.Throw<NationalIdMustHaveValidCheckDigitException>(act);
    }

    [TestMethod]
    public void Equal_National_Ids_With_Different_Padding_Are_Equal()
    {
        //given
        var shortForm = NationalId.Of("1234566");
        var paddedForm = NationalId.Of("001234566");

        //expected
        shortForm.ShouldBe(paddedForm);
    }
}
```

The fixed literals are the asserted values themselves (padding and check-digit arithmetic), so they stay inline per the assertion rules; randomized valid IDs come from the new `Faker` helper.

Add to `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (inside the existing class; add `using DrivingLessons.Domain.Values;` at the top of the file). It generates 8 random digits and computes the 9th so the checksum holds:

```csharp
public static NationalId FakeNationalId()
{
    var digits = new int[9];

    for (var i = 0; i < 8; i++)
    {
        digits[i] = Random.Shared.Next(0, 10);
    }

    var sum = 0;

    for (var i = 0; i < 8; i++)
    {
        var weight = i % 2 == 0
            ? 1
            : 2;
        var product = digits[i] * weight;
        var reduced = product > 9
            ? product - 9
            : product;
        sum += reduced;
    }

    digits[8] = (10 - (sum % 10)) % 10;

    return NationalId.Of(string.Concat(digits));
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL (compilation error) — `NationalId` and the three exception types do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Values\NationalId.cs`. Input cleaning: strip Unicode bidirectional control marks (Berosh CSV cells may carry them), replace non-breaking spaces (`\u00A0`) with regular spaces, then trim:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record NationalId
{
    private const int MaxLength = 9;

    private static readonly char[] BidiMarks =
    [
        '\u200E',
        '\u200F',
        '\u202A',
        '\u202B',
        '\u202C',
        '\u202D',
        '\u202E',
        '\u2066',
        '\u2067',
        '\u2068',
        '\u2069'
    ];

    public string Value { get; }

    private NationalId(string value)
    {
        Value = value;
    }

    public static NationalId Of(string value)
    {
        var withoutMarks = RemoveBidiMarks(value);
        var normalized = withoutMarks.Replace('\u00A0', ' ').Trim();

        if (normalized.Length == 0 || !normalized.All(char.IsAsciiDigit))
        {
            throw new NationalIdMustBeDigitsException();
        }

        if (normalized.Length > MaxLength)
        {
            throw new NationalIdMustBeAtMostNineDigitsException();
        }

        var padded = normalized.PadLeft(MaxLength, '0');

        if (!HasValidCheckDigit(padded))
        {
            throw new NationalIdMustHaveValidCheckDigitException();
        }

        return new NationalId(padded);
    }

    private static string RemoveBidiMarks(string value)
    {
        var kept = value.Where(c => !BidiMarks.Contains(c));

        return string.Concat(kept);
    }

    private static bool HasValidCheckDigit(string digits)
    {
        var sum = 0;

        for (var i = 0; i < MaxLength; i++)
        {
            var weight = i % 2 == 0
                ? 1
                : 2;
            var product = (digits[i] - '0') * weight;
            var reduced = product > 9
                ? product - 9
                : product;
            sum += reduced;
        }

        return sum % 10 == 0;
    }
}
```

`src\DrivingLessons.Domain\Exceptions\NationalIdMustBeDigitsException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustBeDigitsException : DomainException
{
    public NationalIdMustBeDigitsException()
        : base("National ID must contain only digits.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\NationalIdMustBeAtMostNineDigitsException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustBeAtMostNineDigitsException : DomainException
{
    public NationalIdMustBeAtMostNineDigitsException()
        : base("National ID must be at most nine digits.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\NationalIdMustHaveValidCheckDigitException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class NationalIdMustHaveValidCheckDigitException : DomainException
{
    public NationalIdMustHaveValidCheckDigitException()
        : base("National ID must have a valid check digit.")
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
git commit -m "feat(domain): add NationalId value object with check-digit validation"
```

---

**Next:** [task-02-student-values.md](task-02-student-values.md)
