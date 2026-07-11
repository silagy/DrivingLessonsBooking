# Task 2 of 12: Typed IDs + value objects + exceptions (TDD)

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires task 1 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** All value types the Teacher aggregate needs: `TeacherId`, `CarId`, `TeacherName`, `Email`, `CarName`, `CarType`, `Transmission`. Nominal records, private constructor + `Of()`/`New()` factories, one exception class per broken rule. TDD: write each test first, watch it fail to compile/pass, then implement.

**Rules:** exception messages for PII values (names, emails) carry **no payload**. Normalization happens inside `Of()`. Never positional records.

---

**Files:**
- Create: `src/DrivingLessons.Domain/Values/TeacherId.cs`, `CarId.cs`, `TeacherName.cs`, `Email.cs`, `CarName.cs`, `CarType.cs`, `Transmission.cs`
- Create: `src/DrivingLessons.Domain/Exceptions/TeacherNameMustNotBeEmptyException.cs`, `EmailMustBeValidException.cs`, `CarNameMustNotBeEmptyException.cs`, `CarTypeMustNotBeEmptyException.cs`
- Test: `tests/DrivingLessons.Domain.Test/Values/TeacherIdTest.cs`, `TeacherNameTest.cs`, `EmailTest.cs`, `CarNameTest.cs`, `CarTypeTest.cs`

- [ ] **Step 1: Write the failing tests — `tests/DrivingLessons.Domain.Test/Values/TeacherIdTest.cs`**

```csharp
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TeacherIdTest
{
    [TestMethod]
    public void New_Ids_Are_Unique()
    {
        //given
        var first = TeacherId.New();
        var second = TeacherId.New();

        //expected
        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Of_Wraps_The_Value()
    {
        //given
        var value = Guid.NewGuid();

        //when
        var id = TeacherId.Of(value);

        //then
        id.Value.ShouldBe(value);
    }

    [TestMethod]
    public void Id_Must_Not_Be_Empty()
    {
        //when
        var act = () => TeacherId.Of(Guid.Empty);

        //then
        Should.Throw<ArgumentException>(act);
    }
}
```

- [ ] **Step 2: `tests/DrivingLessons.Domain.Test/Values/TeacherNameTest.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TeacherNameTest
{
    [TestMethod]
    public void Name_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var name = TeacherName.Of($"  {raw}  ");

        //then
        name.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Name_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => TeacherName.Of(value!);

        //then
        Should.Throw<TeacherNameMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 3: `tests/DrivingLessons.Domain.Test/Values/EmailTest.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class EmailTest
{
    [TestMethod]
    public void Email_Is_Normalized_To_Lower_Case()
    {
        //when
        var email = Email.Of(" Noa@Example.com ");

        //then
        email.Value.ShouldBe("noa@example.com");
    }

    [TestMethod]
    public void Equal_Concepts_Are_Equal_Instances()
    {
        //given
        var first = Email.Of("Noa@Example.com");
        var second = Email.Of("noa@example.com");

        //expected
        first.ShouldBe(second);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not-an-email")]
    [DataRow("a b@example.com")]
    public void Email_Must_Be_Valid(string? value)
    {
        //when
        var act = () => Email.Of(value!);

        //then
        Should.Throw<EmailMustBeValidException>(act);
    }
}
```

- [ ] **Step 4: `tests/DrivingLessons.Domain.Test/Values/CarNameTest.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class CarNameTest
{
    [TestMethod]
    public void Name_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var name = CarName.Of($"  {raw}  ");

        //then
        name.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Name_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => CarName.Of(value!);

        //then
        Should.Throw<CarNameMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 5: `tests/DrivingLessons.Domain.Test/Values/CarTypeTest.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class CarTypeTest
{
    [TestMethod]
    public void Type_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var type = CarType.Of($"  {raw}  ");

        //then
        type.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Type_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => CarType.Of(value!);

        //then
        Should.Throw<CarTypeMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 6: Run the tests — expected: compilation failures (types do not exist yet)**

Run: `dotnet test`

- [ ] **Step 7: `src/DrivingLessons.Domain/Values/TeacherId.cs`**

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record TeacherId : EntityId
{
    private TeacherId(Guid value)
        : base(value)
    {
    }

    public static TeacherId New()
    {
        return new TeacherId(Guid.NewGuid());
    }

    public static TeacherId Of(Guid value)
    {
        return new TeacherId(value);
    }
}
```

- [ ] **Step 8: `src/DrivingLessons.Domain/Values/CarId.cs`**

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record CarId : EntityId
{
    private CarId(Guid value)
        : base(value)
    {
    }

    public static CarId New()
    {
        return new CarId(Guid.NewGuid());
    }

    public static CarId Of(Guid value)
    {
        return new CarId(value);
    }
}
```

- [ ] **Step 9: `src/DrivingLessons.Domain/Values/TeacherName.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TeacherName
{
    public string Value { get; }

    private TeacherName(string value)
    {
        Value = value;
    }

    public static TeacherName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TeacherNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new TeacherName(normalized);
    }
}
```

- [ ] **Step 10: `src/DrivingLessons.Domain/Values/Email.cs`**

```csharp
using System.Net.Mail;
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new EmailMustBeValidException();
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out _))
        {
            throw new EmailMustBeValidException();
        }

        return new Email(normalized);
    }
}
```

- [ ] **Step 11: `src/DrivingLessons.Domain/Values/CarName.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record CarName
{
    public string Value { get; }

    private CarName(string value)
    {
        Value = value;
    }

    public static CarName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CarNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new CarName(normalized);
    }
}
```

- [ ] **Step 12: `src/DrivingLessons.Domain/Values/CarType.cs`**

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record CarType
{
    public string Value { get; }

    private CarType(string value)
    {
        Value = value;
    }

    public static CarType Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CarTypeMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new CarType(normalized);
    }
}
```

- [ ] **Step 13: `src/DrivingLessons.Domain/Values/Transmission.cs`**

```csharp
namespace DrivingLessons.Domain.Values;

public enum Transmission
{
    Automatic = 10,
    Manual = 20
}
```

- [ ] **Step 14: The four exception classes**

`src/DrivingLessons.Domain/Exceptions/TeacherNameMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherNameMustNotBeEmptyException : DomainException
{
    public TeacherNameMustNotBeEmptyException()
        : base("Teacher name must not be empty.")
    {
    }
}
```

`src/DrivingLessons.Domain/Exceptions/EmailMustBeValidException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class EmailMustBeValidException : DomainException
{
    public EmailMustBeValidException()
        : base("Email must be a valid email address.")
    {
    }
}
```

`src/DrivingLessons.Domain/Exceptions/CarNameMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class CarNameMustNotBeEmptyException : DomainException
{
    public CarNameMustNotBeEmptyException()
        : base("Car name must not be empty.")
    {
    }
}
```

`src/DrivingLessons.Domain/Exceptions/CarTypeMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class CarTypeMustNotBeEmptyException : DomainException
{
    public CarTypeMustNotBeEmptyException()
        : base("Car type must not be empty.")
    {
    }
}
```

- [ ] **Step 15: Verify + commit**

Run: `dotnet test` — expected: all tests PASS.

```bash
git add src tests
git commit -m "feat(domain): teacher and car value objects with typed ids"
```

---

**Next:** [task-03-teacher-aggregate.md](task-03-teacher-aggregate.md)
