# Task 4 of 7: Immediate lockout on every authenticated request (TDD)

> Part of [#86: Delete and Restore Users](README.md). Requires task 3 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Create: `src\DrivingLessons.Application\Auth\CheckSignedInUserInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Create: `src\DrivingLessons.Presentation.Web\Auth\SignedInUserJwtBearerEvents.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Program.cs` (wire the events)
- Test: `tests\DrivingLessons.Application.Test\Auth\CheckSignedInUserInteractorTest.cs`

**Interfaces:**
- Consumes:
  - From task 2: `IUserRepository.GetAsync(UserId) : Task<User?>`.
  - From task 1: `User.Restore()`, used in a test.
  - Existing: `User.IsDeleted`, `User.SecurityStamp`, `SecurityStamp.Of(string)` (throws on blank), `UserId.Of(Guid)`, `AuthClaims.SecurityStamp` (`"security_stamp"`), and `JwtRegisteredClaimNames.Sub`.
- Produces:
  - `CheckSignedInUserInteractor.ExecuteAsync(string? userIdClaim, string? securityStampClaim) : Task<bool>`, registered scoped. It never throws on bad claims.
  - `SignedInUserJwtBearerEvents : JwtBearerEvents`, registered scoped and set as `options.EventsType`. On `false` it calls `context.Fail(...)`, so the request gets a bare 401 and the client's `authInterceptor` signs out.

**Why:**
- #86 AC 4: a missing User, a Deleted User or a security-stamp mismatch gives a 401.
- #86 AC 5: a deleted User is signed out on their next request, not when the token expires.
- README decisions 5 and 7; Review Focus 1, 2 and 5.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CheckSignedInUserInteractorTest"
```

- [ ] **Step 1: Write the failing tests**

`tests\DrivingLessons.Application.Test\Auth\CheckSignedInUserInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class CheckSignedInUserInteractorTest
{
    private IUserRepository repository = null!;
    private CheckSignedInUserInteractor interactor = null!;
    private User user = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IUserRepository>();
        interactor = new CheckSignedInUserInteractor(repository);
        user = User.Create(
            UserName.Of("Dana User"),
            Email.Of("dana.user@school.example"),
            PasswordHash.Of("hash"),
            Role.Administrator,
            null);

        A.CallTo(() => repository.GetAsync(A<UserId>._)).Returns((User?)null);
        A.CallTo(() => repository.GetAsync(user.Id)).Returns(user);
    }

    [TestMethod]
    public async Task Active_User_With_The_Current_Stamp_Is_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Not_Signed_In()
    {
        //given
        var stampInToken = user.SecurityStamp.Value;
        user.Delete();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), stampInToken);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Deleted_User_Is_Not_Signed_In_Even_With_The_New_Stamp()
    {
        //given
        user.Delete();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Restored_User_Is_Not_Signed_In_With_A_Token_From_Before_The_Delete()
    {
        //given
        var stampInToken = user.SecurityStamp.Value;
        user.Delete();
        user.Restore();

        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), stampInToken);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_User_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(Guid.NewGuid().ToString(), user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    [DataRow("")]
    [DataRow("00000000-0000-0000-0000-000000000000")]
    public async Task Malformed_User_Id_Is_Not_Signed_In(string userIdClaim)
    {
        //when
        var signedIn = await interactor.ExecuteAsync(userIdClaim, user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_User_Id_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(null, user.SecurityStamp.Value);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task Blank_Security_Stamp_Is_Not_Signed_In(string securityStampClaim)
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), securityStampClaim);

        //then
        signedIn.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Missing_Security_Stamp_Is_Not_Signed_In()
    {
        //when
        var signedIn = await interactor.ExecuteAsync(user.Id.Value.ToString(), null);

        //then
        signedIn.ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run the tests and watch them fail**

Run the command above. Expected: the build fails because `CheckSignedInUserInteractor` doesn't exist.

- [ ] **Step 3: Implement the interactor**

`src\DrivingLessons.Application\Auth\CheckSignedInUserInteractor.cs`. It uses a primary constructor like its neighbour `LoginInteractor`.

```csharp
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Auth;

public class CheckSignedInUserInteractor(IUserRepository users)
{
    public async Task<bool> ExecuteAsync(string? userIdClaim, string? securityStampClaim)
    {
        if (!Guid.TryParse(userIdClaim, out var id)
            || id == Guid.Empty
            || string.IsNullOrWhiteSpace(securityStampClaim))
        {
            return false;
        }

        var userId = UserId.Of(id);
        var user = await users.GetAsync(userId);

        if (user is null
            || user.IsDeleted)
        {
            return false;
        }

        var securityStamp = SecurityStamp.Of(securityStampClaim);

        return user.SecurityStamp == securityStamp;
    }
}
```

Register it in `src\DrivingLessons.Application\DependencyInjection.cs`, after `services.AddScoped<LoginInteractor>();`:

```csharp
        services.AddScoped<CheckSignedInUserInteractor>();
```

- [ ] **Step 4: Run the tests and watch them pass**

Run the command above. Expected: every case passes.

- [ ] **Step 5: Wire the check into JWT bearer authentication**

`src\DrivingLessons.Presentation.Web\Auth\SignedInUserJwtBearerEvents.cs` (same `JwtRegisteredClaimNames` namespace as `HttpCurrentUser`):

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class SignedInUserJwtBearerEvents(CheckSignedInUserInteractor checkSignedInUser) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userIdClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var securityStampClaim = context.Principal?.FindFirst(AuthClaims.SecurityStamp)?.Value;

        var signedIn = await checkSignedInUser.ExecuteAsync(userIdClaim, securityStampClaim);

        if (!signedIn)
        {
            context.Fail("The User is deleted or their sign-in was revoked.");
        }
    }
}
```

In `src\DrivingLessons.Presentation.Web\Program.cs`:

1. Add `builder.Services.AddScoped<SignedInUserJwtBearerEvents>();` right after the `ICurrentUser` registration from task 3.
2. Inside `.AddJwtBearer(options => { ... })`, add right after `options.MapInboundClaims = false;`:

```csharp
        options.EventsType = typeof(SignedInUserJwtBearerEvents);
```

JWT bearer resolves `EventsType` from the request's services on every request, so the scoped interactor and `DbContext` are per request.

- [ ] **Step 6: Build and run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: all green.

- [ ] **Step 7: Smoke-test the lockout on a throwaway database**

Recreate the database and start the API in the background, exactly as in task 3 step 8 (`DROP DATABASE IF EXISTS drivinglessons_us86_smoke`, `CREATE DATABASE ...`, `dotnet run ...`). Then run task 3's setup block (the helpers `API`, `json`, `login`, `call`, plus `ADMIN`, `TEACHER_ID`, `DANA_ID`, `DANA`).

Check each result:

- [ ] 1. `call GET /api/teachers/find $DANA` → `200` (any signed-in User may read this under the fallback policy)
- [ ] 2. `call DELETE /api/users/$DANA_ID $ADMIN` → `204`
- [ ] 3. **Review Focus 1:** `call GET /api/teachers/find $DANA` → `401`, immediately, with the same token that was `200` a moment ago
- [ ] 4. `login dana.user@school.example 'Temporary#2026'` prints nothing / `undefined` (sign-in refused, existing #84 behaviour)
- [ ] 5. `call POST /api/users/$DANA_ID/restore $ADMIN` → `204`. Then `call GET /api/teachers/find $DANA` with the **old** token → still `401` (decision 7). `DANA2=$(login dana.user@school.example 'Temporary#2026')`, then `call GET /api/teachers/find $DANA2` → `200`.
- [ ] 6. **Review Focus 5:** with the revoked `$DANA` token attached, anonymous endpoints still work:
  - `curl -s -o /dev/null -w "%{http_code}" -X POST $API/api/auth/login -H "Authorization: Bearer $DANA" -H "Content-Type: application/json" -d @login.json` → `200`
  - Find the anonymous by-link read route (`grep -rn "by-link" src/DrivingLessons.Presentation.Web/Controllers`). Call it with a made-up token and the `$DANA` header: expect `404`, not `401`.
- [ ] 7. `call GET /api/users/find $ADMIN` → `200` (the Administrator is unaffected)

Stop the API.

- [ ] **Step 8: Commit**

```bash
git add src tests
git commit -m "feat(auth): sign a deleted or revoked User out on their next request (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
