# Task 2 of 10: Student profile value objects

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\StudentId.cs`, `StudentName.cs`, `PhoneNumber.cs`, `Address.cs`, `LessonsStartDate.cs`, `LicenseType.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentNameMustNotBeEmptyException.cs`, `PhoneNumberMustBeValidException.cs`, `AddressMustNotBeEmptyException.cs`, `LicenseTypeMustNotBeEmptyException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\StudentNameTest.cs`, `PhoneNumberTest.cs`, `AddressTest.cs`, `LicenseTypeTest.cs`

Before coding, open `src\DrivingLessons.Domain\Values\TeacherId.cs`, `TeacherName.cs` and `tests\DrivingLessons.Domain.Test\Values\TeacherNameTest.cs` and match their exact shape (record, private ctor, static `Of()` that trims and throws when empty).

**No `StudentNotes`** — the CSV notes column (`הערות`) is ignored like other Berosh bookkeeping columns: it has no v1 consumer (not on the student form, not in the Excel export, not in the mockup). Do not model it (planning decision 7).

**`LicenseType` is a free-text value object** — Berosh semantics are unconfirmed (transmission words vs license class vs free text); free text is the safe superset, and transmission truth remains the Car. Flagged as open item 2 in the [README](README.md).

**`LessonsStartDate`** wraps `DateOnly` with a factory and no validation — factories without logic are not tested (per `domain-testing.md`).

Bidi characters are written as `\u` escape sequences in every C# literal below — never paste the invisible characters themselves into source files.

- [ ] **Step 1: Write failing tests**

`tests\DrivingLessons.Domain.Test\Values\StudentNameTest.cs` — copy `TeacherNameTest.cs` verbatim, rename `TeacherName` → `StudentName` and `TeacherNameMustNotBeEmptyException` → `StudentNameMustNotBeEmptyException`.

`tests\DrivingLessons.Domain.Test\Values\PhoneNumberTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class PhoneNumberTest
{
    [TestMethod]
    public void Phone_Number_Strips_Directional_Marks_And_Whitespace()
    {
        //given
        var wrapped = "\u200F 052-1234567 \u200E";

        //when
        var phone = PhoneNumber.Of(wrapped);

        //then
        phone.Value.ShouldBe("052-1234567");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("054")]
    [DataRow("abc")]
    public void Must_Contain_At_Least_Seven_Digits(string value)
    {
        //when
        var act = () => PhoneNumber.Of(value);

        //then
        Should.Throw<PhoneNumberMustBeValidException>(act);
    }
}
```

The strips test doubles as the formatting test: the dash inside `052-1234567` survives cleaning — dashes and inner formatting are kept for display.

`tests\DrivingLessons.Domain.Test\Values\AddressTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class AddressTest
{
    [TestMethod]
    public void Address_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var address = Address.Of($"  {raw}  ");

        //then
        address.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Address_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => Address.Of(value!);

        //then
        Should.Throw<AddressMustNotBeEmptyException>(act);
    }
}
```

`tests\DrivingLessons.Domain.Test\Values\LicenseTypeTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class LicenseTypeTest
{
    [TestMethod]
    public void License_Type_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var licenseType = LicenseType.Of($"  {raw}  ");

        //then
        licenseType.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void License_Type_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => LicenseType.Of(value!);

        //then
        Should.Throw<LicenseTypeMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: FAIL (compilation error) — `StudentName`, `PhoneNumber`, `Address`, `LicenseType` and their exceptions do not exist.

- [ ] **Step 3: Implement**

`src\DrivingLessons.Domain\Values\StudentId.cs` — copy `TeacherId.cs` verbatim, rename `TeacherId` → `StudentId`.

`src\DrivingLessons.Domain\Values\StudentName.cs` — copy `TeacherName.cs` verbatim, rename `TeacherName` → `StudentName` and `TeacherNameMustNotBeEmptyException` → `StudentNameMustNotBeEmptyException`.

`src\DrivingLessons.Domain\Exceptions\StudentNameMustNotBeEmptyException.cs` — copy `TeacherNameMustNotBeEmptyException.cs` verbatim, rename the class and change the message to `"Student name must not be empty."`.

`src\DrivingLessons.Domain\Values\PhoneNumber.cs` — same cleaning as `NationalId` (task 1): strip bidi marks, replace non-breaking space, trim. Then require at least 7 digit characters but **keep dashes and inner formatting for display** — the cleaned string is stored as-is. The `BidiMarks` set is deliberately duplicated from `NationalId` so each value object stays self-contained:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record PhoneNumber
{
    private const int MinimumDigitCount = 7;

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

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Of(string value)
    {
        var withoutMarks = RemoveBidiMarks(value);
        var normalized = withoutMarks.Replace('\u00A0', ' ').Trim();
        var digitCount = normalized.Count(char.IsAsciiDigit);

        if (digitCount < MinimumDigitCount)
        {
            throw new PhoneNumberMustBeValidException();
        }

        return new PhoneNumber(normalized);
    }

    private static string RemoveBidiMarks(string value)
    {
        var kept = value.Where(c => !BidiMarks.Contains(c));

        return string.Concat(kept);
    }
}
```

`src\DrivingLessons.Domain\Exceptions\PhoneNumberMustBeValidException.cs` — parameterless, no payload in the message (a phone number is PII — mirror `EmailMustBeValidException`):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PhoneNumberMustBeValidException : DomainException
{
    public PhoneNumberMustBeValidException()
        : base("Phone number must be a valid phone number.")
    {
    }
}
```

`src\DrivingLessons.Domain\Values\Address.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Address
{
    public string Value { get; }

    private Address(string value)
    {
        Value = value;
    }

    public static Address Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AddressMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new Address(normalized);
    }
}
```

`src\DrivingLessons.Domain\Exceptions\AddressMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class AddressMustNotBeEmptyException : DomainException
{
    public AddressMustNotBeEmptyException()
        : base("Address must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Values\LicenseType.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record LicenseType
{
    public string Value { get; }

    private LicenseType(string value)
    {
        Value = value;
    }

    public static LicenseType Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LicenseTypeMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new LicenseType(normalized);
    }
}
```

`src\DrivingLessons.Domain\Exceptions\LicenseTypeMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class LicenseTypeMustNotBeEmptyException : DomainException
{
    public LicenseTypeMustNotBeEmptyException()
        : base("License type must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Values\LessonsStartDate.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public record LessonsStartDate
{
    public DateOnly Value { get; }

    private LessonsStartDate(DateOnly value)
    {
        Value = value;
    }

    public static LessonsStartDate Of(DateOnly value)
    {
        return new LessonsStartDate(value);
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj`
Expected: PASS (all new + existing tests green).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): add student profile value objects"
```

---

**Next:** [task-03-student-aggregate.md](task-03-student-aggregate.md)
