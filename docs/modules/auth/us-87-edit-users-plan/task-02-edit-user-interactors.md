# Task 2 of 6: Interactors for Edit details, Change Role and Set Temporary Password (TDD)

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Requires task 1 committed. Work on branch `87-edit-users`.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\UserMustNotChangeOwnRoleException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserMustNotDemoteLastActiveAdministratorException.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeUserDetails\ChangeUserDetailsRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeUserDetails\ChangeUserDetailsInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeUserRole\ChangeUserRoleRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeUserRole\ChangeUserRoleInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\SetUserTemporaryPassword\SetUserTemporaryPasswordRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\SetUserTemporaryPassword\SetUserTemporaryPasswordInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (three new `errors.*` keys, one reworded)
- Test: `tests\DrivingLessons.Application.Test\Commands\ChangeUserDetailsInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\ChangeUserRoleInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\SetUserTemporaryPasswordInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (three cases)

**Interfaces:**
- Consumes:
  - From task 1: `User.ChangeDetails(UserName, Email)`, `User.ChangeRole(Role)`, `User.SetTemporaryPassword(PasswordHash)`, `UserAlreadyHasRoleException(UserId)`.
  - Existing: `IUserRepository.GetAsync(UserId) : Task<User?>`, `IUserQueries.ExistsWithSignInEmailAsync(Email) : Task<bool>`, `IUserQueries.CountActiveAdministratorsAsync() : Task<int>`, `ICurrentUser.Id : UserId`, `IPasswordHasher.Hash(string) : PasswordHash`, `IUnitOfWork.CommitAsync()`, `UserNotFoundException(UserId)`, `UserSignInEmailAlreadyInUseException()`, `UserWithTeacherRoleMustHaveLinkedTeacherException()`, `TemporaryPasswordMustNotBeEmptyException`, `TemporaryPassword.Of(string)`, `UserName.Of`, `Email.Of`.
- Produces:
  - `ChangeUserDetailsRequest(string Name, string SignInEmail)` and `ChangeUserDetailsInteractor.ExecuteAsync(Guid id, ChangeUserDetailsRequest request) : Task`
  - `ChangeUserRoleRequest(Role Role)` and `ChangeUserRoleInteractor.ExecuteAsync(Guid id, ChangeUserRoleRequest request) : Task`
  - `SetUserTemporaryPasswordRequest(string TemporaryPassword)` and `SetUserTemporaryPasswordInteractor.ExecuteAsync(Guid id, SetUserTemporaryPasswordRequest request) : Task`
  - All three interactors are registered scoped. Task 3's controller calls them.
  - Exceptions and codes: `UserMustNotChangeOwnRoleException()` → `userMustNotChangeOwnRole`, `UserMustNotDemoteLastActiveAdministratorException()` → `userMustNotDemoteLastActiveAdministrator`.
  - Translation keys `errors.userAlreadyHasRole`, `errors.userMustNotChangeOwnRole`, `errors.userMustNotDemoteLastActiveAdministrator`, plus the reworded `errors.userWithTeacherRoleMustHaveLinkedTeacher`. Task 5 shows them in dialogs.

**Why:**
- #87 AC 3: editing an email to one another User already has is rejected.
- #87 AC 4: demoting yourself is rejected, and so is demoting the last active Administrator.
- #87 AC 5: the linked Teacher can't change.
- #87 AC 8: `ApiExceptionFilterTest` covers each new exception.
- README decisions 2, 4 and 5; Review Focus 1, 2 and 3.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ChangeUserDetailsInteractorTest|FullyQualifiedName~ChangeUserRoleInteractorTest|FullyQualifiedName~SetUserTemporaryPasswordInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"
```

- [ ] **Step 1: Write the failing interactor tests**

`tests\DrivingLessons.Application.Test\Commands\ChangeUserDetailsInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.ChangeUserDetails;
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
public class ChangeUserDetailsInteractorTest
{
    private const string OwnEmail = "dana.signin@school.example";
    private const string NewName = "Dana Levi-Cohen";
    private const string FreeEmail = "dana.new@school.example";
    private const string TakenEmail = "ronit@school.example";

    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeUserDetailsInteractor interactor = null!;
    private Teacher teacher = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeUserDetailsInteractor(repository, queries, unitOfWork);

        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        user = User.Create(
            UserName.Of("Dana Levi"),
            Email.Of(OwnEmail),
            PasswordHash.Of("hash"),
            Role.Teacher,
            teacher);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).Returns(false);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(Email.Of(TakenEmail))).Returns(true);
    }

    [TestMethod]
    public async Task Changes_The_Name_And_Sign_In_Email()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.Name.ShouldBe(UserName.Of(NewName));
        user.SignInEmail.ShouldBe(Email.Of(FreeEmail));
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Keeping_The_Own_Email_Does_Not_Check_Uniqueness()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, OwnEmail.ToUpperInvariant());

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.Name.ShouldBe(UserName.Of(NewName));
        user.SignInEmail.ShouldBe(Email.Of(OwnEmail));
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow(TakenEmail)]
    [DataRow("RONIT@school.example")]
    [DataRow(" ronit@school.example ")]
    public async Task Email_Another_User_Has_Is_Rejected(string signInEmail)
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, signInEmail);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<UserSignInEmailAlreadyInUseException>(act);
        user.SignInEmail.ShouldBe(Email.Of(OwnEmail));
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Changing_Details_Keeps_The_Linked_Teacher()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new ChangeUserDetailsRequest(NewName, FreeEmail);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

`Email.Of` lowercases and trims its input, so the `[DataRow]`s that differ only in case or spacing resolve to the taken email (Review Focus 2). `Email` is a record, so the fake's argument match `Email.Of(TakenEmail)` compares by value.

`tests\DrivingLessons.Application.Test\Commands\ChangeUserRoleInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.ChangeUserRole;
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
public class ChangeUserRoleInteractorTest
{
    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeUserRoleInteractor interactor = null!;
    private User signedInAdministrator = null!;
    private Teacher ronitTeacher = null!;
    private User linkedAdministrator = null!;
    private User unlinkedAdministrator = null!;
    private Teacher danaTeacher = null!;
    private User teacherUser = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeUserRoleInteractor(repository, queries, currentUser, unitOfWork);

        signedInAdministrator = UserOf("owner@school.example", Role.Administrator, null);
        ronitTeacher = Teacher.Create(TeacherName.Of("Ronit Avraham"), Email.Of("ronit@school.example"));
        linkedAdministrator = UserOf("ronit.signin@school.example", Role.Administrator, ronitTeacher);
        unlinkedAdministrator = UserOf("amit@school.example", Role.Administrator, null);
        danaTeacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana@school.example"));
        teacherUser = UserOf("dana.signin@school.example", Role.Teacher, danaTeacher);

        A.CallTo(() => currentUser.Id).Returns(signedInAdministrator.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(signedInAdministrator.Id)).Returns(signedInAdministrator);
        A.CallTo(() => repository.GetAsync(linkedAdministrator.Id)).Returns(linkedAdministrator);
        A.CallTo(() => repository.GetAsync(unlinkedAdministrator.Id)).Returns(unlinkedAdministrator);
        A.CallTo(() => repository.GetAsync(teacherUser.Id)).Returns(teacherUser);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(3);
    }

    [TestMethod]
    public async Task Gives_A_Teacher_Role_User_The_Administrator_Role_And_Keeps_The_Link()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Administrator);

        //when
        await interactor.ExecuteAsync(teacherUser.Id.Value, request);

        //then
        teacherUser.Role.ShouldBe(Role.Administrator);
        teacherUser.TeacherId.ShouldBe(danaTeacher.Id);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Gives_A_Linked_Administrator_The_Teacher_Role()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        await interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        linkedAdministrator.Role.ShouldBe(Role.Teacher);
        linkedAdministrator.TeacherId.ShouldBe(ronitTeacher.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Unlinked_Administrator_Does_Not_Get_The_Teacher_Role()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(unlinkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        unlinkedAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Demoting_Yourself_Is_Rejected_Before_The_Teacher_Link_Rule()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(signedInAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserMustNotChangeOwnRoleException>(act);
        signedInAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Last_Active_Administrator_Keeps_The_Administrator_Role()
    {
        //given
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).Returns(1);
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserMustNotDemoteLastActiveAdministratorException>(act);
        linkedAdministrator.Role.ShouldBe(Role.Administrator);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Same_Role_Is_Rejected_Without_Counting_Administrators()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(teacherUser.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyHasRoleException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        linkedAdministrator.Delete();
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(linkedAdministrator.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => queries.CountActiveAdministratorsAsync()).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new ChangeUserRoleRequest(Role.Teacher);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

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

`tests\DrivingLessons.Application.Test\Commands\SetUserTemporaryPasswordInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.SetUserTemporaryPassword;
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
public class SetUserTemporaryPasswordInteractorTest
{
    private const string Password = "Temporary#2027";

    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private SetUserTemporaryPasswordInteractor interactor = null!;
    private PasswordHash hashed = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new SetUserTemporaryPasswordInteractor(repository, passwordHasher, unitOfWork);

        hashed = PasswordHash.Of("hashed-temporary-password");
        user = User.Create(
            UserName.Of("Michal Dahan"),
            Email.Of("michal@school.example"),
            PasswordHash.Of("old-hash"),
            Role.Administrator,
            null);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => passwordHasher.Hash(Password)).Returns(hashed);
    }

    [TestMethod]
    public async Task Stores_The_Hashed_Temporary_Password_And_Signs_The_User_Out()
    {
        //given
        var stampBefore = user.SecurityStamp;
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        await interactor.ExecuteAsync(user.Id.Value, request);

        //then
        user.PasswordHash.ShouldBe(hashed);
        user.SecurityStamp.ShouldNotBe(stampBefore);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Temporary_Password_Is_Rejected(string password)
    {
        //given
        var request = new SetUserTemporaryPasswordRequest(password);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<TemporaryPasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        user.Delete();
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        var act = () => interactor.ExecuteAsync(user.Id.Value, request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Found()
    {
        //given
        var request = new SetUserTemporaryPasswordRequest(Password);

        //when
        var act = () => interactor.ExecuteAsync(Guid.NewGuid(), request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

- [ ] **Step 2: Write the failing filter cases**

Add to `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, after `Deleted_Linked_Teacher_Is_A_Conflict_With_Its_Rule_As_Code`. The file already has `using DrivingLessons.Domain.Exceptions;` and `using DrivingLessons.Domain.Values;`.

```csharp
    [TestMethod]
    public void Same_Role_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyHasRoleException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyHasRole");
    }

    [TestMethod]
    public void Changing_Your_Own_Role_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotChangeOwnRoleException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotChangeOwnRole");
    }

    [TestMethod]
    public void Demoting_The_Last_Active_Administrator_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotDemoteLastActiveAdministratorException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotDemoteLastActiveAdministrator");
    }
```

- [ ] **Step 3: Run the tests and watch them fail**

Run the command above. Expected: the build fails on the missing types (the three interactors, their requests and the two exceptions).

- [ ] **Step 4: Add the two rule exceptions**

`src\DrivingLessons.Domain\Exceptions\UserMustNotChangeOwnRoleException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotChangeOwnRoleException : DomainException
{
    public UserMustNotChangeOwnRoleException()
        : base("A User must not change their own Role.")
    {
    }
}
```

`src\DrivingLessons.Domain\Exceptions\UserMustNotDemoteLastActiveAdministratorException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserMustNotDemoteLastActiveAdministratorException : DomainException
{
    public UserMustNotDemoteLastActiveAdministratorException()
        : base("The last active Administrator must keep the Administrator Role.")
    {
    }
}
```

- [ ] **Step 5: Add the request records**

`src\DrivingLessons.Application\Commands\ChangeUserDetails\ChangeUserDetailsRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeUserDetails;

public record ChangeUserDetailsRequest(string Name, string SignInEmail);
```

`src\DrivingLessons.Application\Commands\ChangeUserRole\ChangeUserRoleRequest.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeUserRole;

public record ChangeUserRoleRequest(Role Role);
```

`src\DrivingLessons.Application\Commands\SetUserTemporaryPassword\SetUserTemporaryPasswordRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.SetUserTemporaryPassword;

public record SetUserTemporaryPasswordRequest(string TemporaryPassword);
```

- [ ] **Step 6: Add the interactors**

`src\DrivingLessons.Application\Commands\ChangeUserDetails\ChangeUserDetailsInteractor.cs`:

```csharp
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeUserDetails;

public class ChangeUserDetailsInteractor
{
    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly IUnitOfWork unitOfWork;

    public ChangeUserDetailsInteractor(
        IUserRepository repository,
        IUserQueries queries,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeUserDetailsRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.SignInEmail);

        await SignInEmailMustBeFreeAsync(user, signInEmail);

        user.ChangeDetails(name, signInEmail);

        await unitOfWork.CommitAsync();
    }

    private async Task SignInEmailMustBeFreeAsync(User user, Email signInEmail)
    {
        if (signInEmail == user.SignInEmail)
        {
            return;
        }

        var inUse = await queries.ExistsWithSignInEmailAsync(signInEmail);

        if (inUse)
        {
            throw new UserSignInEmailAlreadyInUseException();
        }
    }
}
```

`src\DrivingLessons.Application\Commands\ChangeUserRole\ChangeUserRoleInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeUserRole;

public class ChangeUserRoleInteractor
{
    private const int LastAdministratorCount = 1;

    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly ICurrentUser currentUser;
    private readonly IUnitOfWork unitOfWork;

    public ChangeUserRoleInteractor(
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

    public async Task ExecuteAsync(Guid id, ChangeUserRoleRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        MustNotBeCurrentUser(userId);

        await MustNotDemoteLastActiveAdministratorAsync(user, request.Role);

        user.ChangeRole(request.Role);

        await unitOfWork.CommitAsync();
    }

    private void MustNotBeCurrentUser(UserId userId)
    {
        if (userId == currentUser.Id)
        {
            throw new UserMustNotChangeOwnRoleException();
        }
    }

    private async Task MustNotDemoteLastActiveAdministratorAsync(User user, Role role)
    {
        if (user.Role is not Role.Administrator
            || role is not Role.Teacher
            || user.IsDeleted)
        {
            return;
        }

        var activeAdministrators = await queries.CountActiveAdministratorsAsync();

        if (activeAdministrators <= LastAdministratorCount)
        {
            throw new UserMustNotDemoteLastActiveAdministratorException();
        }
    }
}
```

`src\DrivingLessons.Application\Commands\SetUserTemporaryPassword\SetUserTemporaryPasswordInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.SetUserTemporaryPassword;

public class SetUserTemporaryPasswordInteractor
{
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public SetUserTemporaryPasswordInteractor(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, SetUserTemporaryPasswordRequest request)
    {
        var userId = UserId.Of(id);

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var temporaryPassword = TemporaryPassword.Of(request.TemporaryPassword);
        var passwordHash = passwordHasher.Hash(temporaryPassword.Value);

        user.SetTemporaryPassword(passwordHash);

        await unitOfWork.CommitAsync();
    }
}
```

In `ChangeUserRoleInteractor`, the self check runs before the domain call. An unlinked Administrator trying to change their own Role therefore gets `userMustNotChangeOwnRole`, not the Teacher-link rule (design 6d, Review Focus 3). The last-Administrator check skips Deleted Users, so a stale screen still gets `userAlreadyDeleted` from the domain. In `SetUserTemporaryPasswordInteractor`, `TemporaryPassword.Of` runs before `Hash`, so a blank password never reaches the hasher.

- [ ] **Step 7: Register the interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add these usings in alphabetical order among the `Commands` usings:
- `using DrivingLessons.Application.Commands.ChangeUserDetails;` and `using DrivingLessons.Application.Commands.ChangeUserRole;`, after `ChangeTeacherDetails`
- `using DrivingLessons.Application.Commands.SetUserTemporaryPassword;`, after `SeedFirstAdministrator`

Then add after `services.AddScoped<RestoreUserInteractor>();`:

```csharp
        services.AddScoped<ChangeUserDetailsInteractor>();
        services.AddScoped<ChangeUserRoleInteractor>();
        services.AddScoped<SetUserTemporaryPasswordInteractor>();
```

- [ ] **Step 8: Run the tests and watch them pass**

Run the command above. Expected: every case in the three new test classes and in `ApiExceptionFilterTest` passes.

- [ ] **Step 9: Add and reword the error translations**

In `client\public\i18n\en.json`, inside `"errors"`, add these right after `"userLinkedTeacherMustNotBeDeleted"`:

```json
    "userAlreadyHasRole": "This User already has this Role. Refresh the page.",
    "userMustNotChangeOwnRole": "You can't change your own Role.",
    "userMustNotDemoteLastActiveAdministrator": "This is the last active Administrator. Add another Administrator first.",
```

and replace the value of `"userWithTeacherRoleMustHaveLinkedTeacher"` with:

```json
    "userWithTeacherRoleMustHaveLinkedTeacher": "A User who isn't linked to a Teacher can't get the Teacher Role.",
```

In `client\public\i18n\he.json`, make the same changes:

```json
    "userAlreadyHasRole": "למשתמש כבר יש את התפקיד הזה. רעננו את הדף.",
    "userMustNotChangeOwnRole": "אי אפשר לשנות את התפקיד של עצמך.",
    "userMustNotDemoteLastActiveAdministrator": "זה מנהל המערכת הפעיל האחרון. הוסיפו מנהל מערכת נוסף לפני השינוי.",
```

```json
    "userWithTeacherRoleMustHaveLinkedTeacher": "אי אפשר להעביר לתפקיד מורה משתמש שלא מקושר למורה.",
```

The copy comes from the design's copy deck: `err.sameRole`, `err.demoteSelf`, `err.lastAdmin` and `err.demoteNoTeacher`. Keep the JSON valid by checking the commas around each inserted block.

- [ ] **Step 10: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Then, from `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: everything is green, including `translations.spec.ts`, which checks for the same keys in both languages and no long dashes or ellipsis characters.

- [ ] **Step 11: Commit**

```bash
git add src tests client/public/i18n
git commit -m "feat(auth): edit a User's details and Role and set a Temporary Password (#87)"
```

End the commit message with the attribution trailer from the session's instructions.
