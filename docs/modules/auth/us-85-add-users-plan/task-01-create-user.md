# Task 1 of 7: Create a User (TDD)

> Part of [#85: Add Users from a New Users Screen](README.md). Work on branch `85-add-users-screen`, with the plan committed.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\TemporaryPassword.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\TemporaryPasswordMustNotBeEmptyException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\UserSignInEmailAlreadyInUseException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\TeacherAlreadyLinkedToUserException.cs`
- Create: `src\DrivingLessons.Application\Queries\IUserQueries.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateUser\CreateUserRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateUser\CreateUserResponse.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateUser\CreateUserInteractor.cs`
- Create: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs` (register `CreateUserInteractor`)
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs` (register `IUserQueries`)
- Test: `tests\DrivingLessons.Domain.Test\Values\TemporaryPasswordTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\CreateUserInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` (three cases)

**Interfaces:**
- Consumes (existing): `User.Create(UserName name, Email signInEmail, PasswordHash passwordHash, Role role, Teacher? teacher)` (throws `UserWithTeacherRoleMustHaveLinkedTeacherException` for `Role.Teacher` with a null Teacher), `User.Id`, `UserName.Of`, `Email.Of` (trims and lower-cases), `IPasswordHasher.Hash(string) : PasswordHash`, `IUserRepository.Add(User)`, `ITeacherRepository.GetAsync(TeacherId) : Task<Teacher?>` (deleted Teachers come back null), `TeacherId.Of(Guid)`, `TeacherNotFoundException(TeacherId)`, `IUnitOfWork.CommitAsync()`.
- Produces:
  - `DrivingLessons.Domain.Values.TemporaryPassword` with `string Value` and `static TemporaryPassword Of(string value)`
  - `DrivingLessons.Domain.Exceptions.TemporaryPasswordMustNotBeEmptyException()` (code `temporaryPasswordMustNotBeEmpty`)
  - `DrivingLessons.Domain.Exceptions.UserSignInEmailAlreadyInUseException()` (code `userSignInEmailAlreadyInUse`)
  - `DrivingLessons.Domain.Exceptions.TeacherAlreadyLinkedToUserException(TeacherId id)` (code `teacherAlreadyLinkedToUser`)
  - `DrivingLessons.Application.Queries.IUserQueries` with `Task<bool> ExistsWithSignInEmailAsync(Email signInEmail)` and `Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId)`. Both count Deleted Users (README decision 1). Task 2 and task 4 add methods to this interface.
  - `record CreateUserRequest(string Name, string SignInEmail, Role Role, Guid? TeacherId, string TemporaryPassword)`
  - `record CreateUserResponse(Guid Id)`
  - `CreateUserInteractor` with `Task<CreateUserResponse> ExecuteAsync(CreateUserRequest request)`, registered scoped. Task 3's controller calls it.
  - `DrivingLessons.Infrastructure.EntityFramework.Queries.UserQueries : IUserQueries`, registered scoped.

**Why:** #85 acceptance criteria 3, 4 and 6, and the interactor tests in criterion 9 (email uniqueness, Teacher already linked, Teacher Role without Teacher). README decisions 1, 4 and 5.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj --filter "FullyQualifiedName~TemporaryPasswordTest"
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateUserInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"
```

- [ ] **Step 1: Write the failing value-object test**

Create `tests\DrivingLessons.Domain.Test\Values\TemporaryPasswordTest.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class TemporaryPasswordTest
{
    [TestMethod]
    public void Password_Keeps_Its_Surrounding_Spaces()
    {
        //given
        var raw = $" {Faker.FakeString()} ";

        //when
        var password = TemporaryPassword.Of(raw);

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
        var act = () => TemporaryPassword.Of(value!);

        //then
        Should.Throw<TemporaryPasswordMustNotBeEmptyException>(act);
    }
}
```

- [ ] **Step 2: Run it and watch it fail**

Run the Domain.Test command above. Expected: build error, `TemporaryPassword` and `TemporaryPasswordMustNotBeEmptyException` don't exist.

- [ ] **Step 3: Add the value object and its exception**

Create `src\DrivingLessons.Domain\Exceptions\TemporaryPasswordMustNotBeEmptyException.cs`:

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class TemporaryPasswordMustNotBeEmptyException : DomainException
{
    public TemporaryPasswordMustNotBeEmptyException()
        : base("Temporary password must not be empty.")
    {
    }
}
```

Create `src\DrivingLessons.Domain\Values\TemporaryPassword.cs`:

```csharp
using DrivingLessons.Domain.Exceptions;

namespace DrivingLessons.Domain.Values;

public record TemporaryPassword
{
    public string Value { get; }

    private TemporaryPassword(string value)
    {
        Value = value;
    }

    public static TemporaryPassword Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TemporaryPasswordMustNotBeEmptyException();
        }

        return new TemporaryPassword(value);
    }
}
```

Run the Domain.Test command. Expected: 4 tests PASS.

- [ ] **Step 4: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Commands\CreateUserInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.CreateUser;
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
public class CreateUserInteractorTest
{
    private const string UserNameValue = "Dana Levi";
    private const string SignInEmail = "dana@school.example";
    private const string Password = "Temporary#2026";

    private IUserRepository repository = null!;
    private IUserQueries queries = null!;
    private ITeacherRepository teacherRepository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IUnitOfWork unitOfWork = null!;
    private CreateUserInteractor interactor = null!;
    private Teacher teacher = null!;
    private PasswordHash hashed = null!;
    private User? added;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        queries = A.Fake<IUserQueries>();
        teacherRepository = A.Fake<ITeacherRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new CreateUserInteractor(repository, queries, teacherRepository, passwordHasher, unitOfWork);
        teacher = Teacher.Create(TeacherName.Of("Dana Levi"), Email.Of("dana.teaches@school.example"));
        hashed = PasswordHash.Of("hashed-temporary-password");
        added = null;

        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).Returns((Teacher?)null);
        A.CallTo(() => teacherRepository.GetAsync(teacher.Id)).Returns(teacher);
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(A<Email>._)).Returns(false);
        A.CallTo(() => queries.ExistsLinkedToTeacherAsync(A<TeacherId>._)).Returns(false);
        A.CallTo(() => passwordHasher.Hash(Password)).Returns(hashed);
        A.CallTo(() => repository.Add(A<User>._)).Invokes((User user) => added = user);
    }

    [TestMethod]
    public async Task Creates_A_Teacher_Role_User_Linked_To_Its_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, teacher.Id.Value, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Name.ShouldBe(UserName.Of(UserNameValue));
        added.SignInEmail.ShouldBe(Email.Of(SignInEmail));
        added.PasswordHash.ShouldBe(hashed);
        added.Role.ShouldBe(Role.Teacher);
        added.TeacherId.ShouldBe(teacher.Id);
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Creates_An_Administrator_Without_A_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBeNull();
        A.CallTo(() => teacherRepository.GetAsync(A<TeacherId>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Creates_An_Administrator_Linked_To_A_Teacher()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, teacher.Id.Value, Password);

        //when
        await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        added.Role.ShouldBe(Role.Administrator);
        added.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public async Task Returns_The_New_User_Id()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, Password);

        //when
        var response = await interactor.ExecuteAsync(request);

        //then
        added.ShouldNotBeNull();
        response.Id.ShouldBe(added.Id.Value);
    }

    [TestMethod]
    public async Task Sign_In_Email_In_Use_Is_Rejected_Ignoring_Case_And_Spaces()
    {
        //given
        A.CallTo(() => queries.ExistsWithSignInEmailAsync(Email.Of(SignInEmail))).Returns(true);
        var request = new CreateUserRequest(UserNameValue, " Dana@School.Example ", Role.Administrator, null, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserSignInEmailAlreadyInUseException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Already_Linked_To_A_User_Is_Rejected()
    {
        //given
        A.CallTo(() => queries.ExistsLinkedToTeacherAsync(teacher.Id)).Returns(true);
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, teacher.Id.Value, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TeacherAlreadyLinkedToUserException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Teacher_Role_Without_A_Teacher_Is_Rejected()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, null, Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unknown_Teacher_Is_Not_Found()
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Teacher, Guid.NewGuid(), Password);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TeacherNotFoundException>(act);
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Temporary_Password_Is_Rejected_Before_Hashing(string blankPassword)
    {
        //given
        var request = new CreateUserRequest(UserNameValue, SignInEmail, Role.Administrator, null, blankPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<TemporaryPasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => repository.Add(A<User>._)).MustNotHaveHappened();
    }
}
```

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add these three tests after `Missing_Entity_Carries_Its_Rule_As_Code` (the existing `using` lines already cover `DrivingLessons.Domain.Exceptions` and `DrivingLessons.Domain.Values`):

```csharp
    [TestMethod]
    public void Sign_In_Email_In_Use_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserSignInEmailAlreadyInUseException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userSignInEmailAlreadyInUse");
    }

    [TestMethod]
    public void Teacher_Already_Linked_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherAlreadyLinkedToUserException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherAlreadyLinkedToUser");
    }

    [TestMethod]
    public void Blank_Temporary_Password_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TemporaryPasswordMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "temporaryPasswordMustNotBeEmpty");
    }
```

- [ ] **Step 5: Run them and watch them fail**

Run the Application.Test command above. Expected: build error, `CreateUserInteractor`, `CreateUserRequest`, `IUserQueries`, `UserSignInEmailAlreadyInUseException` and `TeacherAlreadyLinkedToUserException` don't exist.

- [ ] **Step 6: Add the two rule exceptions**

Create `src\DrivingLessons.Domain\Exceptions\UserSignInEmailAlreadyInUseException.cs` (the message never contains the email, Global Constraints):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class UserSignInEmailAlreadyInUseException : DomainException
{
    public UserSignInEmailAlreadyInUseException()
        : base("Another User already signs in with this email.")
    {
    }
}
```

Create `src\DrivingLessons.Domain\Exceptions\TeacherAlreadyLinkedToUserException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class TeacherAlreadyLinkedToUserException : DomainException
{
    public TeacherAlreadyLinkedToUserException(TeacherId id)
        : base($"Teacher {id.Value} is already linked to a User.")
    {
    }
}
```

- [ ] **Step 7: Add the query port**

Create `src\DrivingLessons.Application\Queries\IUserQueries.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IUserQueries
{
    Task<bool> ExistsWithSignInEmailAsync(Email signInEmail);

    Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId);
}
```

- [ ] **Step 8: Add the request, response and interactor**

Create `src\DrivingLessons.Application\Commands\CreateUser\CreateUserRequest.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateUser;

public record CreateUserRequest(string Name, string SignInEmail, Role Role, Guid? TeacherId, string TemporaryPassword);
```

Create `src\DrivingLessons.Application\Commands\CreateUser\CreateUserResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.CreateUser;

public record CreateUserResponse(Guid Id);
```

Create `src\DrivingLessons.Application\Commands\CreateUser\CreateUserInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateUser;

public class CreateUserInteractor
{
    private readonly IUserRepository repository;
    private readonly IUserQueries queries;
    private readonly ITeacherRepository teacherRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public CreateUserInteractor(
        IUserRepository repository,
        IUserQueries queries,
        ITeacherRepository teacherRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.queries = queries;
        this.teacherRepository = teacherRepository;
        this.passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResponse> ExecuteAsync(CreateUserRequest request)
    {
        var name = UserName.Of(request.Name);
        var signInEmail = Email.Of(request.SignInEmail);
        var temporaryPassword = TemporaryPassword.Of(request.TemporaryPassword);

        await SignInEmailMustBeFreeAsync(signInEmail);

        var teacher = await ResolveUnlinkedTeacherAsync(request.TeacherId);

        var passwordHash = passwordHasher.Hash(temporaryPassword.Value);
        var user = User.Create(name, signInEmail, passwordHash, request.Role, teacher);

        repository.Add(user);

        await unitOfWork.CommitAsync();

        return new CreateUserResponse(user.Id.Value);
    }

    private async Task SignInEmailMustBeFreeAsync(Email signInEmail)
    {
        var inUse = await queries.ExistsWithSignInEmailAsync(signInEmail);

        if (inUse)
        {
            throw new UserSignInEmailAlreadyInUseException();
        }
    }

    private async Task<Teacher?> ResolveUnlinkedTeacherAsync(Guid? id)
    {
        if (id is null)
        {
            return null;
        }

        var teacherId = TeacherId.Of(id.Value);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var alreadyLinked = await queries.ExistsLinkedToTeacherAsync(teacherId);

        if (alreadyLinked)
        {
            throw new TeacherAlreadyLinkedToUserException(teacherId);
        }

        return teacher;
    }
}
```

`User.Create` throws `UserWithTeacherRoleMustHaveLinkedTeacherException` itself, so the interactor doesn't repeat that rule.

In `src\DrivingLessons.Application\DependencyInjection.cs`, add `using DrivingLessons.Application.Commands.CreateUser;` in alphabetical order (after `using DrivingLessons.Application.Commands.CreateTeacher;`), and register the interactor right after `SeedFirstAdministratorInteractor`:

```csharp
        services.AddScoped<SeedFirstAdministratorInteractor>();
        services.AddScoped<CreateUserInteractor>();
```

- [ ] **Step 9: Implement the query port**

Create `src\DrivingLessons.Infrastructure\EntityFramework\Queries\UserQueries.cs`. `users` has no soft-delete filter, so both checks count Deleted Users (README decision 1). `Email` is written in full because inside Infrastructure it would resolve to the `DrivingLessons.Infrastructure.Email` namespace:

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class UserQueries : IUserQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public UserQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> ExistsWithSignInEmailAsync(DrivingLessons.Domain.Values.Email signInEmail)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.SignInEmail == signInEmail);
    }

    public async Task<bool> ExistsLinkedToTeacherAsync(TeacherId teacherId)
    {
        return await dbContext
                         .Users
                         .AnyAsync(x => x.TeacherId == teacherId);
    }
}
```

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, register it right after `IUserRepository`:

```csharp
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
```

- [ ] **Step 10: Run the tests and watch them pass**

Run both commands from the top of this task. Expected: `TemporaryPasswordTest` 4 PASS, `CreateUserInteractorTest` 10 PASS, `ApiExceptionFilterTest` all PASS (three new). Then:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: build clean, every test PASS (`SourceTextTest` included).

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Domain/Values/TemporaryPassword.cs src/DrivingLessons.Domain/Exceptions/TemporaryPasswordMustNotBeEmptyException.cs src/DrivingLessons.Domain/Exceptions/UserSignInEmailAlreadyInUseException.cs src/DrivingLessons.Domain/Exceptions/TeacherAlreadyLinkedToUserException.cs src/DrivingLessons.Application/Queries/IUserQueries.cs src/DrivingLessons.Application/Commands/CreateUser src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/UserQueries.cs src/DrivingLessons.Infrastructure/DependencyInjection.cs tests/DrivingLessons.Domain.Test/Values/TemporaryPasswordTest.cs tests/DrivingLessons.Application.Test/Commands/CreateUserInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs
git commit -m "feat(application): create a User with a unique sign-in email and an unlinked Teacher (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
