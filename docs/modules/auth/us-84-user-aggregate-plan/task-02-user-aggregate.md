# Task 2 of 6: User aggregate (TDD)

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Requires task 1 committed. Work on branch `84-user-aggregate-sign-in`.

**Files:**
- Create: `src\DrivingLessons.Domain\Entities\User.cs`
- Create: `src\DrivingLessons.Domain\Events\UserCreated.cs`
- Create: `src\DrivingLessons.Domain\Events\UserDeleted.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserWithTeacherRoleMustHaveLinkedTeacherException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserAlreadyDeletedException.cs`
- Create: `src\DrivingLessons.Domain\Repositories\IUserRepository.cs`
- Create: `tests\DrivingLessons.Domain.Test\Entities\Fake\UserFakeBuilder.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`

**Interfaces:**
- Consumes: from task 1, `UserId`, `UserName`, `PasswordHash`, `SecurityStamp`, `Role`. Existing `Email`, `Teacher`, `TeacherId`, `AggregateRoot<TId>`, `TeacherFakeBuilder.Build()`, `Faker`.
- Produces:
  - `DrivingLessons.Domain.Entities.User : AggregateRoot<UserId>` with read-only properties `UserName Name`, `Email SignInEmail`, `PasswordHash PasswordHash`, `Role Role`, `TeacherId? TeacherId`, `SecurityStamp SecurityStamp`, `bool IsDeleted`
  - `static User Create(UserName name, Email signInEmail, PasswordHash passwordHash, Role role, Teacher? teacher)`
  - `void Delete()` (throws `UserAlreadyDeletedException`; replaces the security stamp)
  - `record UserCreated(UserId UserId, UserName Name, Email SignInEmail, Role Role, TeacherId? TeacherId) : IDomainEvent`
  - `record UserDeleted(UserId UserId) : IDomainEvent`
  - `UserWithTeacherRoleMustHaveLinkedTeacherException()`, `UserAlreadyDeletedException(UserId id)`
  - `IUserRepository` with `Task<User?> GetByEmailAsync(Email signInEmail)`, `Task<bool> AnyExistAsync()`, `void Add(User user)`
  - Test helper `UserFakeBuilder` with `WithRole(Role)`, `WithTeacher(Teacher)`, `Build()`, `BuildDeleted()`

**Why:** #84 acceptance criteria 1 and 2. `Delete` is here too (README decision 1): task 5 needs a Deleted User to prove one cannot sign in. The repository returns Deleted Users as well (README decision 9). The login interactor rejects them, and the later Users screen lists and restores them.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~UserTest"
```

- [ ] **Step 1: Write the fake builder and the failing tests**

Create `tests\DrivingLessons.Domain.Test\Entities\Fake\UserFakeBuilder.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public class UserFakeBuilder
{
    private Role role = Role.Administrator;
    private Teacher? teacher;

    public UserFakeBuilder WithRole(Role role)
    {
        this.role = role;

        return this;
    }

    public UserFakeBuilder WithTeacher(Teacher teacher)
    {
        this.teacher = teacher;

        return this;
    }

    public User Build()
    {
        var resolvedTeacher = role is Role.Teacher
            ? teacher ?? TeacherFakeBuilder.Build()
            : teacher;
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        return User.Create(name, signInEmail, passwordHash, role, resolvedTeacher);
    }

    public User BuildDeleted()
    {
        var user = Build();
        user.Delete();

        return user;
    }
}
```

Create `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class UserTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var user = User.Create(name, signInEmail, passwordHash, Role.Administrator, null);

        //then
        user.Name.ShouldBe(name);
        user.SignInEmail.ShouldBe(signInEmail);
        user.PasswordHash.ShouldBe(passwordHash);
        user.Role.ShouldBe(Role.Administrator);
        user.TeacherId.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Create__Links_The_Teacher(Role role)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .WithTeacher(teacher)
                   .Build();

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());
        var teacher = TeacherFakeBuilder.Build();

        //when
        var user = User.Create(name, signInEmail, passwordHash, Role.Teacher, teacher);

        //then
        user
            .UncommittedEvents
            .OfType<UserCreated>()
            .Where(x => x.UserId == user.Id
                        && x.Name == name
                        && x.SignInEmail == signInEmail
                        && x.Role == Role.Teacher
                        && x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Create__Must_Have_Linked_Teacher_For_Teacher_Role()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => User.Create(name, signInEmail, passwordHash, Role.Teacher, null);

        //then
        Should.Throw<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
    }

    [TestMethod]
    public void New_User_Is_Not_Deleted()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //expected
        user.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void New_Users_Get_Distinct_Security_Stamps()
    {
        //given
        var first = new UserFakeBuilder().Build();
        var second = new UserFakeBuilder().Build();

        //expected
        first.SecurityStamp.ShouldNotBe(second.SecurityStamp);
    }

    [TestMethod]
    public void Delete()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //when
        user.Delete();

        //then
        user.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;

        //when
        user.Delete();

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //when
        user.Delete();

        //then
        user
            .UncommittedEvents
            .OfType<UserDeleted>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Delete__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();

        //when
        var act = () => user.Delete();

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the test command above.
Expected: build FAILS with `CS0246: The type or namespace name 'User' could not be found` (and the same for `UserCreated`, `UserDeleted` and the two exceptions).

- [ ] **Step 3: Write the events and exceptions**

`src\DrivingLessons.Domain\Events\UserCreated.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserCreated(UserId UserId, UserName Name, Email SignInEmail, Role Role, TeacherId? TeacherId)
    : IDomainEvent;
```

`src\DrivingLessons.Domain\Events\UserDeleted.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserDeleted(UserId UserId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\UserWithTeacherRoleMustHaveLinkedTeacherException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserWithTeacherRoleMustHaveLinkedTeacherException : DomainException
{
    public UserWithTeacherRoleMustHaveLinkedTeacherException()
        : base("A User with the Teacher Role must be linked to a Teacher.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\UserAlreadyDeletedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyDeletedException : DomainException
{
    public UserAlreadyDeletedException(UserId id)
        : base($"User {id.Value} is already deleted.")
    {
    }
}
```

- [ ] **Step 4: Write the aggregate**

`src\DrivingLessons.Domain\Entities\User.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class User : AggregateRoot<UserId>
{
    public UserName Name { get; private set; }
    public Email SignInEmail { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public TeacherId? TeacherId { get; private set; }
    public SecurityStamp SecurityStamp { get; private set; }
    public bool IsDeleted { get; private set; }

    private User()
    {
    }

    private User(
        UserId id,
        UserName name,
        Email signInEmail,
        PasswordHash passwordHash,
        Role role,
        TeacherId? teacherId,
        SecurityStamp securityStamp,
        bool isDeleted)
        : base(id)
    {
        Name = name;
        SignInEmail = signInEmail;
        PasswordHash = passwordHash;
        Role = role;
        TeacherId = teacherId;
        SecurityStamp = securityStamp;
        IsDeleted = isDeleted;

        var createdEvent = new UserCreated(id, name, signInEmail, role, teacherId);
        AddEvent(createdEvent);
    }

    public static User Create(
        UserName name,
        Email signInEmail,
        PasswordHash passwordHash,
        Role role,
        Teacher? teacher)
    {
        MustHaveLinkedTeacherForTeacherRole(role, teacher);

        const bool isDeleted = false;
        var id = UserId.New();
        var teacherId = teacher?.Id;
        var securityStamp = SecurityStamp.New();

        return new User(id, name, signInEmail, passwordHash, role, teacherId, securityStamp, isDeleted);
    }

    public void Delete()
    {
        MustNotBeDeleted();

        IsDeleted = true;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserDeleted(Id));
    }

    private static void MustHaveLinkedTeacherForTeacherRole(Role role, Teacher? teacher)
    {
        if (role is Role.Teacher
            && teacher is null)
        {
            throw new UserWithTeacherRoleMustHaveLinkedTeacherException();
        }
    }

    private void MustNotBeDeleted()
    {
        if (IsDeleted)
        {
            throw new UserAlreadyDeletedException(Id);
        }
    }
}
```

`SecurityStamp = SecurityStamp.New();` and `role is Role.Teacher` compile as intended: when a property and its type share a name, C# resolves `SecurityStamp.New()` and `Role.Teacher` to the type's static members.

- [ ] **Step 5: Write the repository interface**

`src\DrivingLessons.Domain\Repositories\IUserRepository.cs`:

```csharp
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email signInEmail);

    Task<bool> AnyExistAsync();

    void Add(User user);
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run the test command above.
Expected: PASS, 12 test cases counting each `DataRow`.

Then run the whole domain suite and the build:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: build succeeds with no new warnings, every test PASS.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain/Entities/User.cs src/DrivingLessons.Domain/Events/UserCreated.cs src/DrivingLessons.Domain/Events/UserDeleted.cs src/DrivingLessons.Domain/Exceptions/UserWithTeacherRoleMustHaveLinkedTeacherException.cs src/DrivingLessons.Domain/Exceptions/UserAlreadyDeletedException.cs src/DrivingLessons.Domain/Repositories/IUserRepository.cs tests/DrivingLessons.Domain.Test/Entities/UserTest.cs tests/DrivingLessons.Domain.Test/Entities/Fake/UserFakeBuilder.cs
git commit -m "feat(domain): add the User aggregate with Create and Delete (#84)"
```

End the commit message with the attribution trailer from the session's instructions.
