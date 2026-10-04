# Task 1 of 6: `User.ChangeDetails`, `User.ChangeRole` and `User.SetTemporaryPassword` (TDD)

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Work on branch `87-edit-users`.

**Files:**
- Modify: `src\DrivingLessons.Domain\Entities\User.cs`
- Create: `src\DrivingLessons.Domain\Events\UserDetailsChanged.cs`
- Create: `src\DrivingLessons.Domain\Events\UserRoleChanged.cs`
- Create: `src\DrivingLessons.Domain\Events\UserTemporaryPasswordSet.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserAlreadyHasRoleException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`

**Interfaces:**
- Consumes (existing): `User`, `UserFakeBuilder` (`Build()`, `BuildDeleted()`, `WithRole(Role)`, `WithTeacher(Teacher)`), `TeacherFakeBuilder.Build()`, `Faker.FakeString()`, `Faker.FakeEmail()`, `UserName.Of`, `Email.Of`, `PasswordHash.Of`, `SecurityStamp.New()`, `UserAlreadyDeletedException`, `UserRoleMustBeDefinedException`, `UserWithTeacherRoleMustHaveLinkedTeacherException`.
- Produces:
  - `User.ChangeDetails(UserName name, Email signInEmail) : void`. Raises `UserDetailsChanged`. Keeps the security stamp.
  - `User.ChangeRole(Role role) : void`. Raises `UserRoleChanged` and rotates the stamp. Throws `UserAlreadyDeletedException`, `UserRoleMustBeDefinedException`, `UserAlreadyHasRoleException` or `UserWithTeacherRoleMustHaveLinkedTeacherException`, checked in that order.
  - `User.SetTemporaryPassword(PasswordHash passwordHash) : void`. Raises `UserTemporaryPasswordSet` and rotates the stamp. Throws `UserAlreadyDeletedException`.
  - Events: `UserDetailsChanged(UserId UserId, UserName Name, Email SignInEmail)`, `UserRoleChanged(UserId UserId, Role Role)`, `UserTemporaryPasswordSet(UserId UserId)`.
  - `UserAlreadyHasRoleException(UserId id)`, with API code `userAlreadyHasRole`. Task 2 adds its filter case and translation.

**Why:**
- #87 AC 1: the three methods exist, each with its own past-tense event, and Role change and password set rotate the stamp.
- #87 AC 2: changing to the current Role is rejected, and so is demoting to the Teacher Role without a linked Teacher.
- #87 AC 5: the linked Teacher can't change.
- README decisions 1-3; Review Focus 4.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~UserTest"
```

- [ ] **Step 1: Write the failing tests**

Append to `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`, after `Restore__Must_Be_Deleted` and before the closing brace of the class. The file already has every `using` these tests need.

```csharp
    [TestMethod]
    public void ChangeDetails()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.Name.ShouldBe(name);
        user.SignInEmail.ShouldBe(signInEmail);
    }

    [TestMethod]
    public void ChangeDetails__Keeps_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.SecurityStamp.ShouldBe(stampBefore);
    }

    [TestMethod]
    public void ChangeDetails__Keeps_The_Linked_Teacher()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .WithTeacher(teacher)
                   .Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void ChangeDetails__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user
            .UncommittedEvents
            .OfType<UserDetailsChanged>()
            .Where(x => x.UserId == user.Id
                        && x.Name == name
                        && x.SignInEmail == signInEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeDetails__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        var act = () => user.ChangeDetails(name, signInEmail);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }

    [TestMethod]
    [DataRow(Role.Administrator, Role.Teacher)]
    [DataRow(Role.Teacher, Role.Administrator)]
    public void ChangeRole(Role from, Role to)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(from)
                   .WithTeacher(TeacherFakeBuilder.Build())
                   .Build();

        //when
        user.ChangeRole(to);

        //then
        user.Role.ShouldBe(to);
    }

    [TestMethod]
    [DataRow(Role.Administrator, Role.Teacher)]
    [DataRow(Role.Teacher, Role.Administrator)]
    public void ChangeRole__Keeps_The_Linked_Teacher(Role from, Role to)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var user = new UserFakeBuilder()
                   .WithRole(from)
                   .WithTeacher(teacher)
                   .Build();

        //when
        user.ChangeRole(to);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void ChangeRole__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .Build();
        var stampBefore = user.SecurityStamp;

        //when
        user.ChangeRole(Role.Administrator);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void ChangeRole__Add_Event()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .Build();

        //when
        user.ChangeRole(Role.Administrator);

        //then
        user
            .UncommittedEvents
            .OfType<UserRoleChanged>()
            .Where(x => x.UserId == user.Id
                        && x.Role == Role.Administrator)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeRole__Must_Not_Have_Role(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();
        var stampBefore = user.SecurityStamp;

        //when
        var act = () => user.ChangeRole(role);

        //then
        Should.Throw<UserAlreadyHasRoleException>(act);
        user.SecurityStamp.ShouldBe(stampBefore);
    }

    [TestMethod]
    public void ChangeRole__Must_Have_Linked_Teacher_For_Teacher_Role()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Administrator)
                   .Build();

        //when
        var act = () => user.ChangeRole(Role.Teacher);

        //then
        Should.Throw<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        user.Role.ShouldBe(Role.Administrator);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(99)]
    public void ChangeRole__Must_Be_Defined_Role(int value)
    {
        //given
        var user = new UserFakeBuilder().Build();
        var role = (Role)value;

        //when
        var act = () => user.ChangeRole(role);

        //then
        Should.Throw<UserRoleMustBeDefinedException>(act);
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeRole__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .WithTeacher(TeacherFakeBuilder.Build())
                   .BuildDeleted();
        var otherRole = role is Role.Administrator
            ? Role.Teacher
            : Role.Administrator;

        //when
        var act = () => user.ChangeRole(otherRole);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }

    [TestMethod]
    public void SetTemporaryPassword()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user.PasswordHash.ShouldBe(passwordHash);
    }

    [TestMethod]
    public void SetTemporaryPassword__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void SetTemporaryPassword__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserTemporaryPasswordSet>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void SetTemporaryPassword__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => user.SetTemporaryPassword(passwordHash);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }
```

Note on `ChangeRole(Role.Administrator, Role.Teacher)`: `UserFakeBuilder.WithTeacher` links a Teacher for both Roles, so demoting this Administrator is legal.

- [ ] **Step 2: Run the tests and watch them fail**

Run the command above. Expected: the build fails on the missing members (`ChangeDetails`, `ChangeRole`, `SetTemporaryPassword`), the three events and `UserAlreadyHasRoleException`.

- [ ] **Step 3: Add the events**

`src\DrivingLessons.Domain\Events\UserDetailsChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserDetailsChanged(UserId UserId, UserName Name, Email SignInEmail)
    : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\UserRoleChanged.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserRoleChanged(UserId UserId, Role Role)
    : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\UserTemporaryPasswordSet.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserTemporaryPasswordSet(UserId UserId)
    : IDomainEvent;
```

Open `UserDeleted.cs` first and match its layout exactly. If it puts `: IDomainEvent` on the same line, do the same.

- [ ] **Step 4: Add the exception**

`src\DrivingLessons.Domain\Exceptions\UserAlreadyHasRoleException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyHasRoleException : DomainException
{
    public UserAlreadyHasRoleException(UserId id)
        : base($"User {id.Value} already has this Role.")
    {
    }
}
```

- [ ] **Step 5: Implement the three methods**

In `src\DrivingLessons.Domain\Entities\User.cs`, add these public methods after `Restore()`:

```csharp
    public void ChangeDetails(UserName name, Email signInEmail)
    {
        MustNotBeDeleted();

        Name = name;
        SignInEmail = signInEmail;

        AddEvent(new UserDetailsChanged(Id, name, signInEmail));
    }

    public void ChangeRole(Role role)
    {
        MustNotBeDeleted();
        MustHaveDefinedRole(role);
        MustNotHaveRole(role);
        MustBeLinkedToTeacherForTeacherRole(role);

        Role = role;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserRoleChanged(Id, role));
    }

    public void SetTemporaryPassword(PasswordHash passwordHash)
    {
        MustNotBeDeleted();

        PasswordHash = passwordHash;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserTemporaryPasswordSet(Id));
    }
```

Then add these private guards after `MustHaveLinkedTeacherForTeacherRole`:

```csharp
    private void MustNotHaveRole(Role role)
    {
        if (Role == role)
        {
            throw new UserAlreadyHasRoleException(Id);
        }
    }

    private void MustBeLinkedToTeacherForTeacherRole(Role role)
    {
        if (role is Role.Teacher
            && TeacherId is null)
        {
            throw new UserWithTeacherRoleMustHaveLinkedTeacherException();
        }
    }
```

The existing static `MustHaveLinkedTeacherForTeacherRole(Role, Teacher?)` checks a `Teacher` at creation. The new instance guard checks the stored `TeacherId`. They throw the same exception because they enforce the same invariant (README decision 2).

- [ ] **Step 6: Run the tests and watch them pass**

Run the command above. Expected: every `UserTest` case passes, the existing ones included.

- [ ] **Step 7: Run the domain suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: all green, including `SourceTextTest`.

- [ ] **Step 8: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(auth): User changes details and Role and gets a Temporary Password (#87)"
```

End the commit message with the attribution trailer from the session's instructions.
