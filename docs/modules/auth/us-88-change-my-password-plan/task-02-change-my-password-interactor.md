# Task 2 of 6: `ChangeMyPasswordInteractor` (TDD)

> Part of [#88: Change My Own Password](README.md). Requires task 1 committed. Work on branch `88-change-my-password`.

**Files:**
- Create: `src\DrivingLessons.Domain\Exceptions\UserCurrentPasswordMustBeCorrectException.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordResponse.cs`
- Create: `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (the `errors` object only)
- Test: `tests\DrivingLessons.Application.Test\Commands\ChangeMyPasswordInteractorTest.cs` (new)
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`

**Interfaces:**
- Consumes:
  - From task 1: `Password.Of(string)`, `PasswordMustNotBeEmptyException`, `User.ChangePassword(PasswordHash)`.
  - Existing: `ICurrentUser.Id : UserId`, `IUserRepository.GetAsync(UserId) : Task<User?>`, `IPasswordHasher.Hash(string) : PasswordHash`, `IPasswordHasher.Verify(PasswordHash, string) : bool`, `IJwtTokenGenerator.Generate(User) : IssuedToken`, `IssuedToken(string AccessToken, DateTimeOffset ExpiresAtUtc)`, `IUnitOfWork.CommitAsync()`, `UserNotFoundException(UserId)`, `UserAlreadyDeletedException`.
- Produces (task 3's controller and task 4's client rely on these):
  - `ChangeMyPasswordRequest(string CurrentPassword, string NewPassword)`, JSON `{ currentPassword, newPassword }`.
  - `ChangeMyPasswordResponse(string AccessToken, DateTimeOffset ExpiresAtUtc)`, JSON `{ accessToken, expiresAtUtc }`.
  - `ChangeMyPasswordInteractor.ExecuteAsync(ChangeMyPasswordRequest request) : Task<ChangeMyPasswordResponse>`, registered as scoped.
  - `UserCurrentPasswordMustBeCorrectException(UserId id) : DomainException`, API code `userCurrentPasswordMustBeCorrect` → **409**.
  - Translations `errors.userCurrentPasswordMustBeCorrect` and `errors.passwordMustNotBeEmpty`.

**Why:**
- #88 AC 2 (part): the command works on the signed-in User only.
- #88 AC 3: "A wrong current password is rejected (interactor test with faked hasher / verifier); `ApiExceptionFilterTest` extended".
- #88 AC 5 (backend half): a fresh token after the change, so the User isn't left with a dead session.
- README decisions 1, 2 and 5; Review Focus 1, 2 and 4.

- [ ] **Step 1: Write the failing interactor tests**

Create `tests\DrivingLessons.Application.Test\Commands\ChangeMyPasswordInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.ChangeMyPassword;
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
public class ChangeMyPasswordInteractorTest
{
    private const string CurrentPassword = "Current#2026";
    private const string NewPassword = "Fresh#2027";
    private const string IssuedAccessToken = "fresh-access-token";

    private ICurrentUser currentUser = null!;
    private IUserRepository repository = null!;
    private IPasswordHasher passwordHasher = null!;
    private IJwtTokenGenerator tokenGenerator = null!;
    private IUnitOfWork unitOfWork = null!;
    private ChangeMyPasswordInteractor interactor = null!;
    private PasswordHash storedHash = null!;
    private PasswordHash hashed = null!;
    private User user = null!;
    private DateTimeOffset expiresAtUtc;

    [TestInitialize]
    public void Init()
    {
        currentUser = A.Fake<ICurrentUser>();
        repository = A.Fake<IUserRepository>();
        passwordHasher = A.Fake<IPasswordHasher>();
        tokenGenerator = A.Fake<IJwtTokenGenerator>();
        unitOfWork = A.Fake<IUnitOfWork>();
        interactor = new ChangeMyPasswordInteractor(currentUser, repository, passwordHasher, tokenGenerator, unitOfWork);

        storedHash = PasswordHash.Of("stored-hash");
        hashed = PasswordHash.Of("hashed-new-password");
        expiresAtUtc = new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero);
        user = User.Create(
            UserName.Of("Yael Carmi"),
            Email.Of("yael@school.example"),
            storedHash,
            Role.Administrator,
            null);

        A.CallTo(() => currentUser.Id).Returns(user.Id);
        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
        A.CallTo(() => passwordHasher.Verify(A<PasswordHash>._, A<string>._)).Returns(false);
        A.CallTo(() => passwordHasher.Verify(storedHash, CurrentPassword)).Returns(true);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).Returns(hashed);
        A.CallTo(() => tokenGenerator.Generate(user)).Returns(new IssuedToken(IssuedAccessToken, expiresAtUtc));
    }

    [TestMethod]
    public async Task Changes_The_Password_And_Returns_A_Fresh_Token()
    {
        //given
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var result = await interactor.ExecuteAsync(request);

        //then
        user.PasswordHash.ShouldBe(hashed);
        result.AccessToken.ShouldBe(IssuedAccessToken);
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        A.CallTo(() => passwordHasher.Hash(NewPassword)).MustHaveHappenedOnceExactly();
        A.CallTo(() => unitOfWork.CommitAsync()).MustHaveHappenedOnceExactly()
         .Then(A.CallTo(() => tokenGenerator.Generate(user)).MustHaveHappenedOnceExactly());
    }

    [TestMethod]
    public async Task The_Fresh_Token_Carries_The_New_Security_Stamp()
    {
        //given
        var stampBefore = user.SecurityStamp;
        SecurityStamp? stampAtIssue = null;
        A.CallTo(() => tokenGenerator.Generate(user))
         .ReturnsLazily(() =>
         {
             stampAtIssue = user.SecurityStamp;
             return new IssuedToken(IssuedAccessToken, expiresAtUtc);
         });
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        await interactor.ExecuteAsync(request);

        //then
        stampAtIssue.ShouldNotBeNull();
        stampAtIssue.ShouldNotBe(stampBefore);
        stampAtIssue.ShouldBe(user.SecurityStamp);
    }

    [TestMethod]
    public async Task Surrounding_Spaces_In_The_New_Password_Are_Kept()
    {
        //given
        const string spaced = "  Fresh 2027  ";
        var request = new ChangeMyPasswordRequest(CurrentPassword, spaced);

        //when
        await interactor.ExecuteAsync(request);

        //then
        A.CallTo(() => passwordHasher.Hash(spaced)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    [DataRow("Wrong#2026")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Wrong_Current_Password_Is_Rejected(string? currentPassword)
    {
        //given
        var stampBefore = user.SecurityStamp;
        var request = new ChangeMyPasswordRequest(currentPassword!, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserCurrentPasswordMustBeCorrectException>(act);
        user.PasswordHash.ShouldBe(storedHash);
        user.SecurityStamp.ShouldBe(stampBefore);
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
        A.CallTo(() => tokenGenerator.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task Blank_New_Password_Is_Rejected(string? newPassword)
    {
        //given
        var request = new ChangeMyPasswordRequest(CurrentPassword, newPassword!);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<PasswordMustNotBeEmptyException>(act);
        A.CallTo(() => passwordHasher.Verify(A<PasswordHash>._, A<string>._)).MustNotHaveHappened();
        A.CallTo(() => passwordHasher.Hash(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Rejected_As_Already_Deleted()
    {
        //given
        user.Delete();
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserAlreadyDeletedException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
        A.CallTo(() => tokenGenerator.Generate(A<User>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Missing_Signed_In_User_Is_Not_Found()
    {
        //given
        A.CallTo(() => currentUser.Id).Returns(UserId.New());
        var request = new ChangeMyPasswordRequest(CurrentPassword, NewPassword);

        //when
        var act = () => interactor.ExecuteAsync(request);

        //then
        await Should.ThrowAsync<UserNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync()).MustNotHaveHappened();
    }
}
```

Note: `Wrong_Current_Password_Is_Rejected` with `null` proves that a missing `currentPassword` in the JSON body is a refusal, not a crash.

- [ ] **Step 2: Write the failing filter tests**

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add these two tests directly after `Blank_Temporary_Password_Is_A_Conflict_With_Its_Rule_As_Code`. The existing usings cover them.

```csharp
    [TestMethod]
    public void Blank_Password_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new PasswordMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "passwordMustNotBeEmpty");
    }

    [TestMethod]
    public void Wrong_Current_Password_Is_A_Conflict_Not_An_Unauthorized()
    {
        //given
        var context = ContextFor(new UserCurrentPasswordMustBeCorrectException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userCurrentPasswordMustBeCorrect");
    }
```

- [ ] **Step 3: Run the application tests and see them fail**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: the build fails with `CS0246` for `ChangeMyPasswordInteractor`, `ChangeMyPasswordRequest` and `UserCurrentPasswordMustBeCorrectException`.

- [ ] **Step 4: Add the rule exception**

Create `src\DrivingLessons.Domain\Exceptions\UserCurrentPasswordMustBeCorrectException.cs`:

```csharp
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Exceptions;

public class UserCurrentPasswordMustBeCorrectException : DomainException
{
    public UserCurrentPasswordMustBeCorrectException(UserId id)
        : base($"The current password given for User {id.Value} is incorrect.")
    {
    }
}
```

It derives from `DomainException`, so `ApiExceptionFilter` maps it to 409 with no filter change. It must **not** derive from or be replaced by `AuthenticationFailedException` (401), because the client signs the User out on any 401 (README decision 2).

- [ ] **Step 5: Add the request, the response and the interactor**

Create `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordRequest.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);
```

Create `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordResponse.cs`:

```csharp
namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public record ChangeMyPasswordResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
```

Create `src\DrivingLessons.Application\Commands\ChangeMyPassword\ChangeMyPasswordInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeMyPassword;

public class ChangeMyPasswordInteractor
{
    private readonly ICurrentUser currentUser;
    private readonly IUserRepository repository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenGenerator tokenGenerator;
    private readonly IUnitOfWork unitOfWork;

    public ChangeMyPasswordInteractor(
        ICurrentUser currentUser,
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork)
    {
        this.currentUser = currentUser;
        this.repository = repository;
        this.passwordHasher = passwordHasher;
        this.tokenGenerator = tokenGenerator;
        this.unitOfWork = unitOfWork;
    }

    public async Task<ChangeMyPasswordResponse> ExecuteAsync(ChangeMyPasswordRequest request)
    {
        var userId = currentUser.Id;

        var user = await repository.GetAsync(userId)
                   ?? throw new UserNotFoundException(userId);

        var newPassword = Password.Of(request.NewPassword);
        var currentPassword = request.CurrentPassword ?? string.Empty;

        MustMatchCurrentPassword(user, currentPassword);

        var passwordHash = passwordHasher.Hash(newPassword.Value);

        user.ChangePassword(passwordHash);

        await unitOfWork.CommitAsync();

        var issued = tokenGenerator.Generate(user);

        return new ChangeMyPasswordResponse(issued.AccessToken, issued.ExpiresAtUtc);
    }

    private void MustMatchCurrentPassword(User user, string currentPassword)
    {
        if (!passwordHasher.Verify(user.PasswordHash, currentPassword))
        {
            throw new UserCurrentPasswordMustBeCorrectException(user.Id);
        }
    }
}
```

If the compiler warns that `request.CurrentPassword ?? string.Empty` is unnecessary because the property is non-nullable, keep it anyway: the JSON body can omit the field, and `LoginInteractor` uses the same guard. If the build treats that warning as an error, write `var currentPassword = request.CurrentPassword is null ? string.Empty : request.CurrentPassword;` as a multiline ternary instead.

- [ ] **Step 6: Register the interactor**

In `src\DrivingLessons.Application\DependencyInjection.cs`:
- Add `using DrivingLessons.Application.Commands.ChangeMyPassword;` directly before `using DrivingLessons.Application.Commands.ChangeTeacherDetails;`.
- Add `services.AddScoped<ChangeMyPasswordInteractor>();` directly after `services.AddScoped<SetUserTemporaryPasswordInteractor>();`.

- [ ] **Step 7: Add the error translations**

In `client\public\i18n\he.json`, inside the top-level `"errors"` object:
- directly after `"temporaryPasswordMustNotBeEmpty"`, add `"passwordMustNotBeEmpty": "הזינו סיסמה חדשה.",`
- directly after `"userWithTeacherRoleMustHaveLinkedTeacher"`, add `"userCurrentPasswordMustBeCorrect": "הסיסמה הנוכחית שגויה.",`

In `client\public\i18n\en.json`, at the same two places:
- `"passwordMustNotBeEmpty": "Enter a new password.",`
- `"userCurrentPasswordMustBeCorrect": "Current password is incorrect.",`

Keep the JSON valid (watch the trailing commas). The Hebrew copy for the wrong current password is the design's `err.wrongCurrent`.

- [ ] **Step 8: Run the tests and see them pass**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: everything is green, including the 11 `ChangeMyPasswordInteractorTest` cases and the 2 new filter cases.

From `client\` (PowerShell), check the translation files still parse and match:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: all specs pass, including `translations.spec.ts`.

- [ ] **Step 9: Commit**

```bash
git add src tests client/public/i18n
git commit -m "feat(application): change my password after checking the current one, and issue a fresh token (#88)"
```

End the commit message with the attribution trailer from the session's instructions.
