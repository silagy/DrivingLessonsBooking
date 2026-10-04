# Task 1 of 6: `Password` and `User.ChangePassword` (TDD)

> Part of [#88: Change My Own Password](README.md). Work on branch `88-change-my-password`.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\Password.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\PasswordMustNotBeEmptyException.cs`
- Create: `src\DrivingLessons.Domain\Events\UserPasswordChanged.cs`
- Modify: `src\DrivingLessons.Domain\Entities\User.cs`
- Test: `tests\DrivingLessons.Domain.Test\Values\PasswordTest.cs` (new)
- Test: `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`

**Interfaces:**
- Consumes (existing): `User`, `UserFakeBuilder` (`Build()`, `BuildDeleted()`, `WithRole(Role)`), `Faker.FakeString()`, `PasswordHash.Of`, `SecurityStamp.New()`, `UserAlreadyDeletedException`, private guard `User.MustNotBeDeleted()`.
- Produces:
  - `Password.Of(string value) : Password`, with `string Value`. Throws `PasswordMustNotBeEmptyException` for `null`, empty or whitespace. Keeps the value verbatim, including surrounding spaces.
  - `PasswordMustNotBeEmptyException()`, with API code `passwordMustNotBeEmpty`. Task 2 adds its filter case and translation.
  - `User.ChangePassword(PasswordHash passwordHash) : void`. Stores the hash, rotates the security stamp and raises `UserPasswordChanged`. Throws `UserAlreadyDeletedException`.
  - Event `UserPasswordChanged(UserId UserId)`. It carries no hash.

**Why:**
- #88 AC 1: "`User.ChangePassword` receives an already-hashed new password value object, changes the security stamp, and emits its own event (`UserTest`)".
- README decisions 3 and 4; Review Focus 4.

- [ ] **Step 1: Write the failing `Password` tests**

Create `tests\DrivingLessons.Domain.Test\Values\PasswordTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class PasswordTest
{
    [TestMethod]
    public void Password_Keeps_Its_Surrounding_Spaces()
    {
        //given
        var raw = $" {Faker.FakeString()} ";

        //when
        var password = Password.Of(raw);

        //then
        password.Value.ShouldBe(raw);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Password_Must_Not_Be_Empty(string? value)
    {
        //when
        var act = () => Password.Of(value!);

        //then
        Should.Throw<PasswordMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 2: Write the failing `User.ChangePassword` tests**

Append these tests at the end of the `UserTest` class in `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`, after `SetTemporaryPassword__Must_Not_Be_Deleted`. The file's existing usings already cover them.

```csharp
    [TestMethod]
    public void ChangePassword()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.PasswordHash.ShouldBe(passwordHash);
    }

    [TestMethod]
    public void ChangePassword__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangePassword__Keeps_The_Role_And_The_Linked_Teacher(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();
        var teacherBefore = user.TeacherId;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.Role.ShouldBe(role);
        user.TeacherId.ShouldBe(teacherBefore);
    }

    [TestMethod]
    public void ChangePassword__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserPasswordChanged>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void ChangePassword__Does_Not_Raise_The_Temporary_Password_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserTemporaryPasswordSet>()
            .ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangePassword__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => user.ChangePassword(passwordHash);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }
```

- [ ] **Step 3: Run the domain tests and see them fail**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: the build fails with `CS0103: The name 'Password' does not exist`, `CS0246` for `PasswordMustNotBeEmptyException` / `UserPasswordChanged`, and `CS1061: 'User' does not contain a definition for 'ChangePassword'`.

- [ ] **Step 4: Add the exception, the value object and the event**

Create `src\DrivingLessons.Domain\Exceptions\PasswordMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class PasswordMustNotBeEmptyException : DomainException
{
    public PasswordMustNotBeEmptyException()
        : base("Password must not be empty.")
    {
    }
}
```

Create `src\DrivingLessons.Domain\Values\Password.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record Password
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Password Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PasswordMustNotBeEmptyException();
        }

        return new Password(value);
    }
}
```

Create `src\DrivingLessons.Domain\Events\UserPasswordChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserPasswordChanged(UserId UserId) : IDomainEvent;
```

- [ ] **Step 5: Add `User.ChangePassword`**

In `src\DrivingLessons.Domain\Entities\User.cs`, add this method directly after `SetTemporaryPassword`:

```csharp
    public void ChangePassword(PasswordHash passwordHash)
    {
        MustNotBeDeleted();

        PasswordHash = passwordHash;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserPasswordChanged(Id));
    }
```

- [ ] **Step 6: Run the domain tests and see them pass**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: every test passes, including `PasswordTest` (4 cases) and the 8 new `UserTest` cases. `SourceTextTest` is green too.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): a User can change their own password, rotating the security stamp (#88)"
```

End the commit message with the attribution trailer from the session's instructions.
