# Task 1 of 6: User value types (TDD)

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Work on branch `84-user-aggregate-sign-in`, which already has the plan committed.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\UserId.cs`
- Create: `src\DrivingLessons.Domain\Values\UserName.cs`
- Create: `src\DrivingLessons.Domain\Values\PasswordHash.cs`
- Create: `src\DrivingLessons.Domain\Values\SecurityStamp.cs`
- Create: `src\DrivingLessons.Domain\Values\Role.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserNameMustNotBeEmptyException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PasswordHashMustNotBeEmptyException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\SecurityStampMustNotBeEmptyException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\UserNameTest.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\PasswordHashTest.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\SecurityStampTest.cs`

**Interfaces:**
- Consumes: `DrivingLessons.Domain.Common.EntityId`, `DomainException` (exist); `Faker.FakeString()` in `tests\DrivingLessons.Domain.Test\Common\Faker.cs` (exists).
- Produces (all in namespace `DrivingLessons.Domain.Values`):
  - `UserId : EntityId` with `static UserId New()` and `static UserId Of(Guid value)`
  - `UserName` with `string Value` and `static UserName Of(string value)` (trims; throws `UserNameMustNotBeEmptyException`)
  - `PasswordHash` with `string Value` and `static PasswordHash Of(string value)` (keeps the value byte for byte; throws `PasswordHashMustNotBeEmptyException`)
  - `SecurityStamp` with `string Value`, `static SecurityStamp New()` and `static SecurityStamp Of(string value)` (throws `SecurityStampMustNotBeEmptyException`)
  - `enum Role { Administrator = 10, Teacher = 20 }`

**Why:** The `User` aggregate (task 2) takes no primitives. Its name, password hash and security stamp each need a type. The password hash type is how "the domain never sees plaintext" (#84) is enforced: only an already-hashed string fits.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~UserNameTest|FullyQualifiedName~PasswordHashTest|FullyQualifiedName~SecurityStampTest"
```

- [ ] **Step 1: Write the failing tests**

Create `tests\DrivingLessons.Domain.Test\Values\UserNameTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class UserNameTest
{
    [TestMethod]
    public void Name_Is_Trimmed()
    {
        //given
        var raw = Faker.FakeString();

        //when
        var name = UserName.Of($"  {raw}  ");

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
        var act = () => UserName.Of(value!);

        //then
        Should.Throw<UserNameMustNotBeEmptyException>(act);
    }
}
```

Create `tests\DrivingLessons.Domain.Test\Values\PasswordHashTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class PasswordHashTest
{
    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var hash = PasswordHash.Of(value);

        //then
        hash.Value.ShouldBe(value);
    }

    [TestMethod]
    public void Hash_Is_Kept_Exactly_As_Given()
    {
        //given
        var value = $" {Faker.FakeString()} ";

        //when
        var hash = PasswordHash.Of(value);

        //then
        hash.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Hash_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => PasswordHash.Of(value!);

        //then
        Should.Throw<PasswordHashMustNotBeEmptyException>(act);
    }
}
```

Create `tests\DrivingLessons.Domain.Test\Values\SecurityStampTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class SecurityStampTest
{
    [TestMethod]
    public void New_Produces_Non_Empty_Stamp()
    {
        //when
        var stamp = SecurityStamp.New();

        //then
        stamp.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void New_Produces_Distinct_Stamps()
    {
        //when
        var first = SecurityStamp.New();
        var second = SecurityStamp.New();

        //then
        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Of()
    {
        //given
        var value = Faker.FakeString();

        //when
        var stamp = SecurityStamp.Of(value);

        //then
        stamp.Value.ShouldBe(value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Of__Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => SecurityStamp.Of(value!);

        //then
        Should.Throw<SecurityStampMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the test command above.
Expected: build FAILS with `CS0103: The name 'UserName' does not exist in the current context` (and the same for `PasswordHash`, `SecurityStamp` and the three exceptions).

- [ ] **Step 3: Write the exceptions**

`src\DrivingLessons.Domain\Exceptions\UserNameMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserNameMustNotBeEmptyException : DomainException
{
    public UserNameMustNotBeEmptyException()
        : base("User name must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\PasswordHashMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PasswordHashMustNotBeEmptyException : DomainException
{
    public PasswordHashMustNotBeEmptyException()
        : base("Password hash must not be empty.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\SecurityStampMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SecurityStampMustNotBeEmptyException : DomainException
{
    public SecurityStampMustNotBeEmptyException()
        : base("Security stamp must not be empty.")
    {
    }
}
```

- [ ] **Step 4: Write the value types**

`src\DrivingLessons.Domain\Values\UserId.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Values;

public record UserId : EntityId
{
    private UserId(Guid value)
        : base(value)
    {
    }

    public static UserId New()
    {
        return new UserId(Guid.NewGuid());
    }

    public static UserId Of(Guid value)
    {
        return new UserId(value);
    }
}
```

`src\DrivingLessons.Domain\Values\UserName.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record UserName
{
    public string Value { get; }

    private UserName(string value)
    {
        Value = value;
    }

    public static UserName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UserNameMustNotBeEmptyException();
        }

        var normalized = value.Trim();

        return new UserName(normalized);
    }
}
```

`src\DrivingLessons.Domain\Values\PasswordHash.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PasswordHashMustNotBeEmptyException();
        }

        return new PasswordHash(value);
    }
}
```

`src\DrivingLessons.Domain\Values\SecurityStamp.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record SecurityStamp
{
    private const string CompactGuidFormat = "N";

    public string Value { get; }

    private SecurityStamp(string value)
    {
        Value = value;
    }

    public static SecurityStamp New()
    {
        var value = Guid.NewGuid().ToString(CompactGuidFormat);

        return new SecurityStamp(value);
    }

    public static SecurityStamp Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SecurityStampMustNotBeEmptyException();
        }

        return new SecurityStamp(value);
    }
}
```

`src\DrivingLessons.Domain\Values\Role.cs`:

```csharp
namespace DrivingLessons.Domain.Values;

public enum Role
{
    Administrator = 10,
    Teacher = 20
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run the test command above.
Expected: PASS, 15 test cases counting each `DataRow` (`UserNameTest` 4, `PasswordHashTest` 5, `SecurityStampTest` 6). The runner may print data rows grouped under their method.

Then run the whole domain suite to confirm nothing else broke:

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: every test PASS.

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Domain/Values/UserId.cs src/DrivingLessons.Domain/Values/UserName.cs src/DrivingLessons.Domain/Values/PasswordHash.cs src/DrivingLessons.Domain/Values/SecurityStamp.cs src/DrivingLessons.Domain/Values/Role.cs src/DrivingLessons.Domain/Exceptions/UserNameMustNotBeEmptyException.cs src/DrivingLessons.Domain/Exceptions/PasswordHashMustNotBeEmptyException.cs src/DrivingLessons.Domain/Exceptions/SecurityStampMustNotBeEmptyException.cs tests/DrivingLessons.Domain.Test/Values/UserNameTest.cs tests/DrivingLessons.Domain.Test/Values/PasswordHashTest.cs tests/DrivingLessons.Domain.Test/Values/SecurityStampTest.cs
git commit -m "feat(domain): add the User value types and Role (#84)"
```

End the commit message with the attribution trailer from the session's instructions.
