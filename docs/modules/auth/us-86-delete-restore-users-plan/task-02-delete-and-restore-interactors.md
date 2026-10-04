# Task 2 of 7: Delete and Restore interactors with the lockout rules (TDD)

> Part of [#86: Delete and Restore Users](README.md). Requires task 1 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\UserMustNotDeleteSelfException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserMustNotBeLastActiveAdministratorException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserLinkedTeacherMustNotBeDeletedException.cs`
- Modify: `src\DrivingLessons.Domain\Repositories\IUserRepository.cs` (adds `GetAsync`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\UserRepository.cs`
- Modify: `src\DrivingLessons.Application\Queries\IUserQueries.cs` (adds `CountActiveAdministratorsAsync`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`
- Create: `src\DrivingLessons.Application\Auth\ICurrentUser.cs`
- Create: `src\DrivingLessons.Application\Commands\DeleteUser\DeleteUserInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\RestoreUser\RestoreUserInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (five `errors.*` keys)
- Test: `tests\DrivingLessons.Application.Test\Commands\DeleteUserInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\RestoreUserInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (five cases)

**Interfaces:**
- Consumes:
  - From task 1: `User.Restore()` and `UserAlreadyActiveException`.
  - Existing: `User.Delete()`, `UserAlreadyDeletedException`, `UserNotFoundException(UserId)` (`Application\Common\Exceptions`), `ITeacherRepository.GetAsync(TeacherId) : Task<Teacher?>` (the Teacher query filter hides deleted Teachers), `IUnitOfWork.CommitAsync()`, `Role`, `UserId.Of(Guid)`.
- Produces:
  - `ICurrentUser { UserId Id { get; } }` in namespace `DrivingLessons.Application.Auth`. Task 3 implements it.
  - `IUserRepository.GetAsync(UserId id) : Task<User?>`. Task 4 uses it too.
  - `IUserQueries.CountActiveAdministratorsAsync() : Task<int>`
  - `DeleteUserInteractor.ExecuteAsync(Guid id) : Task` and `RestoreUserInteractor.ExecuteAsync(Guid id) : Task`, registered scoped. Task 3's controller calls them.
  - Exceptions and codes: `UserMustNotDeleteSelfException()` → `userMustNotDeleteSelf`, `UserMustNotBeLastActiveAdministratorException()` → `userMustNotBeLastActiveAdministrator`, `UserLinkedTeacherMustNotBeDeletedException(UserId)` → `userLinkedTeacherMustNotBeDeleted`.
  - Translation keys `errors.userAlreadyDeleted`, `errors.userAlreadyActive`, `errors.userMustNotDeleteSelf`, `errors.userMustNotBeLastActiveAdministrator`, `errors.userLinkedTeacherMustNotBeDeleted`. Task 6 shows them.

**Why:**
- #86 AC 2: deleting yourself is rejected, and so is deleting the last active Administrator. Interactor tests fake the current-user and query ports.
- #86 AC 3: deleting a User leaves their Teacher record untouched.
- #86 AC 7: every new exception gets an `ApiExceptionFilterTest` case.
- README decisions 3, 4 and 6; Review Focus 3.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~DeleteUserInteractorTest|FullyQualifiedName~RestoreUserInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"
```

- [ ] **Step 1: Write the failing interactor tests**

`tests\DrivingLessons.Application.Test\Commands\DeleteUserInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.DeleteUser;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class DeleteUserInteractorTest
{
    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private IUnitOfWork unitOfWork = null!;
    private DeleteUserInteractor interactor = null!;
    private User signedInAdministrator = null!;
    private User otherAdministrator = null!;
    private Teacher teacher = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new DeleteUserInteractor(repository, queries, currentUser, unitOfWork);

        signedInAdministrator = UserOf("owner@school.example", Role.Administrator, null);
        otherAdministrator = UserOf("ronit@school.example", Role.Administrator, null);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, teacher);

        A.CallTo(() => currentUser.Id).Returns(signedInAdministrator.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(signedInAdministrator.Id)).Returns(signedInAdministrator);
        A.CallTo(() => repository.GetAsync(otherAdministrator.Id)).Returns(otherAdministrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(2);
    }

    [TestMethod]
    public async Task Deletes_A_Teacher_Role_User()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deletes_An_Administrator_While_Another_Active_Administrator_Remains()
    {
        //when
        await interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        otherAdministrator.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Deleting_A_Teacher_Role_User_Does_Not_Count_Administrators()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);

        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleting_A_User_Leaves_The_Linked_Teacher_Untouched()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacher.IsDeleted.ShouldBeFalse();
        teacherUser.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Deleting_Yourself_Is_Rejected()
    {
        //when
        var act = () => interactor.ExecuteAsync(signedInAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserMustNotDeleteSelfException>(act);
        signedInAdministrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Last_Active_Administrator_Is_Not_Deleted()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);

        //when
        var act = () => interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserMustNotBeLastActiveAdministratorException>(act);
        otherAdministrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        otherAdministrator.Delete();

        //when
        var act = () => interactor.ExecuteAsync(otherAdministrator.Id.Value);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static User UserOf(string signInEmail, Role role, Teacher? linkedTeacher)
    {
        return User.Create(
            UserName.Of("Test User"),
            Email.Of(signInEmail),
            PasswordHash.Of("hash"),
            role,
            linkedTeacher);
    }
}
```

`tests\DrivingLessons.Application.Test\Commands\RestoreUserInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.RestoreUser;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class RestoreUserInteractorTest
{
    private IUserRepository repository = null!;
    private ITeacherRepository teacherRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private RestoreUserInteractor interactor = null!;
    private Teacher teacher = null!;
    private User administrator = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        teacherRepository = A.Fake<ITeacherRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new RestoreUserInteractor(repository, teacherRepository, unitOfWork);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        administrator = UserOf("ronit@school.example", Role.Administrator, null);
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, teacher);
        administrator.Delete();
        teacherUser.Delete();

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(administrator.Id)).Returns(administrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
    }

    [TestMethod]
    public async Task Restores_A_Deleted_User_Without_A_Linked_Teacher()
    {
        //when
        await interactor.ExecuteAsync(administrator.Id.Value);

        //then
        administrator.IsDeleted.ShouldBeFalse();
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Restores_A_Deleted_User_Whose_Linked_Teacher_Is_Active()
    {
        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        teacherUser.IsDeleted.ShouldBeFalse();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task User_Whose_Linked_Teacher_Is_Deleted_Is_Not_Restored()
    {
        //given
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns((Teacher?)null);

        //when
        var act = () => interactor.ExecuteAsync(teacherUser.Id.Value);

        //then
        await Should.ThrowAsync<UserLinkedTeacherMustNotBeDeletedException>(act);
        teacherUser.IsDeleted.ShouldBeTrue();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Active_User_Is_Rejected_As_Already_Active()
    {
        //given
        administrator.Restore();

        //when
        var act = () => interactor.ExecuteAsync(administrator.Id.Value);

        //then
        await Should.ThrowAsync<UserAlreadyActiveException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid());

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    private static User UserOf(string signInEmail, Role role, Teacher? linkedTeacher)
    {
        return User.Create(
            UserName.Of("Test User"),
            Email.Of(signInEmail),
            PasswordHash.Of("hash"),
            role,
            linkedTeacher);
    }
}
```

- [ ] **Step 2: Write the failing filter cases**

Add to `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, next to `Teacher_With_An_Active_User_Is_A_Conflict_With_Its_Rule_As_Code`. The file already has `using DrivingLessons.Domain.Exceptions;` and `using DrivingLessons.Domain.Values;`.

```csharp
    [TestMethod]
    public void Already_Deleted_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyDeletedException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyDeleted");
    }

    [TestMethod]
    public void Already_Active_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyActiveException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyActive");
    }

    [TestMethod]
    public void Deleting_Yourself_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotDeleteSelfException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotDeleteSelf");
    }

    [TestMethod]
    public void Last_Active_Administrator_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotBeLastActiveAdministratorException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotBeLastActiveAdministrator");
    }

    [TestMethod]
    public void Deleted_Linked_Teacher_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserLinkedTeacherMustNotBeDeletedException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userLinkedTeacherMustNotBeDeleted");
    }
```

- [ ] **Step 3: Run the tests and watch them fail**

Run the command above. Expected: the build fails on the missing types (`ICurrentUser`, the interactors, the three exceptions, `GetAsync`, `CountActiveAdministratorsAsync`).

- [ ] **Step 4: Add the three rule exceptions**

`src\DrivingLessons.Domain\Exceptions\UserMustNotDeleteSelfException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotDeleteSelfException : DomainException
{
    public UserMustNotDeleteSelfException()
        : base("A User must not delete themselves.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\UserMustNotBeLastActiveAdministratorException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotBeLastActiveAdministratorException : DomainException
{
    public UserMustNotBeLastActiveAdministratorException()
        : base("The last active Administrator must stay active.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\UserLinkedTeacherMustNotBeDeletedException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserLinkedTeacherMustNotBeDeletedException : DomainException
{
    public UserLinkedTeacherMustNotBeDeletedException(UserId id)
        : base($"User {id.Value} is linked to a deleted Teacher.")
    {
    }
}
```

- [ ] **Step 5: Add the ports**

`src\DrivingLessons.Application\Auth\ICurrentUser.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public interface ICurrentUser
{
    UserId Id { get; }
}
```

In `src\DrivingLessons.Domain\Repositories\IUserRepository.cs`, add as the first member:

```csharp
    Task<User?> GetAsync(UserId id);
```

In `src\DrivingLessons.Application\Queries\IUserQueries.cs`, add as the last member:

```csharp
    Task<int> CountActiveAdministratorsAsync();
```

- [ ] **Step 6: Implement them in Infrastructure**

In `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\UserRepository.cs`, add `using DrivingLessons.Domain.Values;` and this method before `GetByEmailAsync`. Keep `DrivingLessons.Domain.Values.Email` fully qualified as it is: inside `DrivingLessons.Infrastructure`, `Email` still resolves to the namespace.

```csharp
    public async Task<User?> GetAsync(UserId id)
    {
        return await dbContext.Users.FindAsync(id);
    }
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`, add after `ActiveExistsLinkedToTeacherAsync`:

```csharp
    public async Task<int> CountActiveAdministratorsAsync()
    {
        return await dbContext
                         .Users
                         .CountAsync(x => x.Role == Role.Administrator && !x.IsDeleted);
    }
```

- [ ] **Step 7: Add the interactors**

`src\DrivingLessons.Application\Commands\DeleteUser\DeleteUserInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.DeleteUser;

public class DeleteUserInteractor
{
    private const int LastAdministratorCount = 1;

    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly ICurrentUser currentUser;
    private readonly IUnitOfWork unitOfWork;

    public DeleteUserInteractor(
        IUserRepository repository,
        IUserQueries queries,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.currentUser = currentUser;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        MustNotBeCurrentUser(userId);

        await MustNotBeLastActiveAdministratorAsync(user);

        user.Delete();

        await unitOfWork.CommitAsync();
    }

    private void MustNotBeCurrentUser(UserId userId)
    {
        if (userId == currentUser.Id)
        {
            throw new UserMustNotDeleteSelfException();
        }
    }

    private async Task MustNotBeLastActiveAdministratorAsync(User user)
    {
        if (user.Role is not Role.Administrator
            || user.IsDeleted)
        {
            return;
        }

        var activeAdministrators = await queries.CountActiveAdministratorsAsync();

        if (activeAdministrators <= LastAdministratorCount)
        {
            throw new UserMustNotBeLastActiveAdministratorException();
        }
    }
}
```

`src\DrivingLessons.Application\Commands\RestoreUser\RestoreUserInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.RestoreUser;

public class RestoreUserInteractor
{
    private readonly IUserRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IUnitOfWork unitOfWork;

    public RestoreUserInteractor(
        IUserRepository repository,
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        await LinkedTeacherMustNotBeDeletedAsync(user);

        user.Restore();

        await unitOfWork.CommitAsync();
    }

    private async Task LinkedTeacherMustNotBeDeletedAsync(User user)
    {
        var teacherId = user.TeacherId;

        if (teacherId is null)
        {
            return;
        }

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            throw new UserLinkedTeacherMustNotBeDeletedException(user.Id);
        }
    }
}
```

If `UserId` turns out not to support `==` by value (check `Domain\Common\EntityId.cs`), use `userId.Equals(currentUser.Id)` instead and keep the test unchanged.

- [ ] **Step 8: Register the interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.Commands.DeleteUser;` and `using DrivingLessons.Application.Commands.RestoreUser;` in alphabetical order with the other `Commands` usings. Then add after `services.AddScoped<CreateUserInteractor>();`:

```csharp
        services.AddScoped<DeleteUserInteractor>();
        services.AddScoped<RestoreUserInteractor>();
```

`ICurrentUser` is registered in task 3. Until then, resolving `DeleteUserInteractor` at runtime would fail, but nothing calls it yet.

- [ ] **Step 9: Run the tests and watch them pass**

Run the command above. Expected: every `DeleteUserInteractorTest`, `RestoreUserInteractorTest` and `ApiExceptionFilterTest` case passes.

- [ ] **Step 10: Add the error translations**

In `client\public\i18n\en.json`, inside the top-level `"errors"` object, add these keys right after `"userNotFound"`:

```json
    "userAlreadyDeleted": "This User is already deleted. Refresh the page.",
    "userAlreadyActive": "This User is already active.",
    "userMustNotDeleteSelf": "You can't delete yourself.",
    "userMustNotBeLastActiveAdministrator": "You can't delete the last active Administrator.",
    "userLinkedTeacherMustNotBeDeleted": "This User's linked Teacher was deleted, so the User can't be restored.",
```

In `client\public\i18n\he.json`, at the same place:

```json
    "userAlreadyDeleted": "המשתמש הזה כבר נמחק. רעננו את הדף.",
    "userAlreadyActive": "המשתמש הזה כבר פעיל.",
    "userMustNotDeleteSelf": "אי אפשר למחוק את עצמך.",
    "userMustNotBeLastActiveAdministrator": "אי אפשר למחוק את מנהל המערכת הפעיל האחרון.",
    "userLinkedTeacherMustNotBeDeleted": "המורה המקושר למשתמש הזה נמחק, ולכן אי אפשר לשחזר את המשתמש.",
```

Keep the JSON valid: watch the commas around the inserted block. The copy for the first four comes from the design's copy deck (`err.alreadyDeleted`, `err.alreadyActive`, `err.deleteSelf`, `err.deleteLastAdmin`). The fifth is new (README decision 4).

- [ ] **Step 11: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: all green, `translations.spec.ts` included (same keys in both languages, no long dashes or ellipsis characters).

- [ ] **Step 12: Commit**

```bash
git add src tests client/public/i18n
git commit -m "feat: delete and restore Users, refusing self and the last active Administrator (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
