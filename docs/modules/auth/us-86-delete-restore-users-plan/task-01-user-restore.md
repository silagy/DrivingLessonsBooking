# Task 1 of 7: Restore a Deleted User (TDD)

> Part of [#86: Delete and Restore Users](README.md). Requires the plan commit. Work on branch `86-delete-restore-users`.

**Files:**
- Modify: `src\DrivingLessons.Domain\Entities\User.cs`
- Create: `src\DrivingLessons.Domain\Events\UserRestored.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserAlreadyActiveException.cs`
- Test: `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`

**Interfaces:**
- Consumes: existing `User` (`Delete()`, `IsDeleted`, `SecurityStamp`, `AddEvent`), `SecurityStamp.New()`, `UserFakeBuilder.Build()` / `BuildDeleted()` / `WithRole(Role)`, and `StudentAlreadyActiveException` as the shape to copy.
- Produces:
  - `User.Restore() : void`. It throws `UserAlreadyActiveException` when the User is not deleted. Otherwise it sets `IsDeleted = false`, sets a new `SecurityStamp` and adds `UserRestored`.
  - `record UserRestored(UserId UserId) : IDomainEvent`
  - `UserAlreadyActiveException(UserId id)`, code `userAlreadyActive`. Tasks 2 and 6 use this code.

**Why:** #86 AC 1: `User.Restore` is non-idempotent, changes the security stamp and emits its own event. `UserTest` covers the happy path, `__Add_Event` and `__Must_Be_*`. README decision 7. `User.Delete()` already ships with all of this (#84), so only Restore is new.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~UserTest"
```

- [ ] **Step 1: Write the failing tests**

Append to `UserTest` in `tests\DrivingLessons.Domain.Test\Entities\UserTest.cs`, after `Delete__Must_Not_Be_Deleted`:

```csharp
    [TestMethod]
    public void Restore()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();

        //when
        user.Restore();

        //then
        user.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Restore__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();
        var stampBefore = user.SecurityStamp;

        //when
        user.Restore();

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void Restore__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();

        //when
        user.Restore();

        //then
        user
            .UncommittedEvents
            .OfType<UserRestored>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Restore__Must_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();

        //when
        var act = () => user.Restore();

        //then
        Should.Throw<UserAlreadyActiveException>(act);
    }
```

- [ ] **Step 2: Run the tests and watch them fail**

Run the command above. Expected: the build fails because `User.Restore`, `UserRestored` and `UserAlreadyActiveException` don't exist.

- [ ] **Step 3: Add the event and the exception**

`src\DrivingLessons.Domain\Events\UserRestored.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record UserRestored(UserId UserId) : IDomainEvent;
```

`src\DrivingLessons.Domain\Exceptions\UserAlreadyActiveException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserAlreadyActiveException : DomainException
{
    public UserAlreadyActiveException(UserId id)
        : base($"User {id.Value} is already active.")
    {
    }
}
```

- [ ] **Step 4: Add `Restore` to the aggregate**

In `src\DrivingLessons.Domain\Entities\User.cs`, add after `Delete()`:

```csharp
    public void Restore()
    {
        MustBeDeleted();

        IsDeleted = false;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserRestored(Id));
    }
```

Then add after `MustNotBeDeleted()`:

```csharp
    private void MustBeDeleted()
    {
        if (!IsDeleted)
        {
            throw new UserAlreadyActiveException(Id);
        }
    }
```

- [ ] **Step 5: Run the tests and watch them pass**

Run the command above. Expected: every `UserTest` passes, the four new ones included (`Restore__Must_Be_Deleted` runs twice).

- [ ] **Step 6: Run the whole domain suite**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: all green. `SourceTextTest` must pass: no long dashes or ellipsis characters in the new files.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Domain tests/DrivingLessons.Domain.Test
git commit -m "feat(domain): restore a Deleted User with a fresh security stamp (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
