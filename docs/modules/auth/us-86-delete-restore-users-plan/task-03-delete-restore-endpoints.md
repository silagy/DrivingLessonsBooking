# Task 3 of 7: Current User and the Delete / Restore endpoints

> Part of [#86: Delete and Restore Users](README.md). Requires task 2 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Program.cs` (register `ICurrentUser`)
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`
- Test: `tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs`

**Interfaces:**
- Consumes: from task 2, `ICurrentUser`, `DeleteUserInteractor.ExecuteAsync(Guid)` and `RestoreUserInteractor.ExecuteAsync(Guid)`. Existing: `AuthorizationPolicies.Administrator` and the token's `sub` claim (the User id, written by `JwtTokenGenerator` with `JwtRegisteredClaimNames.Sub`; `MapInboundClaims = false`, so it stays `sub`).
- Produces:
  - `HttpCurrentUser : ICurrentUser`, registered scoped. It throws `InvalidOperationException` when the request has no valid `sub`; that can't happen behind the Administrator policy.
  - `DELETE api/users/{id}`: 204, or 404 `userNotFound`, or 409 `userAlreadyDeleted` / `userMustNotDeleteSelf` / `userMustNotBeLastActiveAdministrator`.
  - `POST api/users/{id}/restore`: 204, or 404, or 409 `userAlreadyActive` / `userLinkedTeacherMustNotBeDeleted`.
  - Both are Administrator only (403 for a Teacher-role token). Task 5's client calls them.

**Why:** #86 AC 2, 6 and 7 need the endpoints. README decisions 6, 11 and 13; Review Focus 3, checked against the real database here.

**Run the tests:**

```bash
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~HttpCurrentUserTest"
```

- [ ] **Step 1: Write the failing test**

`tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs`. Use the same `JwtRegisteredClaimNames` namespace as `src\DrivingLessons.Infrastructure\Auth\JwtTokenGenerator.cs`; the code below assumes `Microsoft.IdentityModel.JsonWebTokens`.

```csharp
using System.Security.Claims;
using DrivingLessons.Domain.Values;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class HttpCurrentUserTest
{
    [TestMethod]
    public void Reads_The_Signed_In_User_From_The_Subject_Claim()
    {
        //given
        var id = Guid.NewGuid();
        var accessor = AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        //when
        var currentUserId = new HttpCurrentUser(accessor).Id;

        //then
        currentUserId.ShouldBe(UserId.Of(id));
    }

    [TestMethod]
    public void Request_Without_A_Signed_In_User_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith();

        //when
        var act = () => new HttpCurrentUser(accessor).Id;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    private static HttpContextAccessor AccessorWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run the command above. Expected: the build fails because `HttpCurrentUser` doesn't exist.

- [ ] **Step 3: Implement `HttpCurrentUser`**

`src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DrivingLessons.Presentation.Web.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId Id => SignedInUserId();

    private UserId SignedInUserId()
    {
        var subject = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(subject, out var id))
        {
            throw new InvalidOperationException("The request has no signed-in User.");
        }

        return UserId.Of(id);
    }
}
```

Presentation.Web can see `DrivingLessons.Domain.Values` through its Application reference. If the compiler disagrees, check `DrivingLessons.Presentation.Web.csproj` before adding any reference.

- [ ] **Step 4: Register it**

In `src\DrivingLessons.Presentation.Web\Program.cs`, add `using DrivingLessons.Application.Auth;` with the other usings. Then add right after `builder.Services.AddInfrastructure(builder.Configuration);`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
```

- [ ] **Step 5: Run the test and watch it pass**

Run the command above. Expected: both cases pass.

- [ ] **Step 6: Add the endpoints**

In `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`, add `using DrivingLessons.Application.Commands.DeleteUser;` and `using DrivingLessons.Application.Commands.RestoreUser;`. Then add after `CreateAsync`:

```csharp
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete the user so they can no longer sign in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromServices] DeleteUserInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [EndpointSummary("Restore a deleted user so they can sign in again")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreAsync(
        [FromServices] RestoreUserInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }
```

- [ ] **Step 7: Build and run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: green. The last command reports no pending changes; this slice adds no migration.

- [ ] **Step 8: Smoke-test the API on a throwaway database**

README decision 13. From the repository root:

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us86_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us86_smoke"
```

Stop anything on port 5080. Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us86_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait for `Now listening on: http://localhost:5080`. Then, from a folder in the session scratchpad, use ASCII-only payloads written to files (memory note: Hebrew on a Windows `curl` command line breaks):

```bash
mkdir -p smoke-86 && cd smoke-86
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
login() { printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json; curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken; }
call() { curl -s -o body.json -w "%{http_code}" -X "$1" "$API$2" -H "Authorization: Bearer $3" -H "Content-Type: application/json" ${4:+-d @$4}; echo " $(cat body.json)"; }

ADMIN=$(login admin@local.dev 'DevAdmin#2026')
printf '{"name":"Dana Levi","contactEmail":"dana@school.example"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher.json | json id)
printf '{"name":"Dana User","signInEmail":"dana.user@school.example","role":"teacher","teacherId":"%s","temporaryPassword":"Temporary#2026"}' "$TEACHER_ID" > teacher-user.json
DANA_ID=$(curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher-user.json | json id)
SELF_ID=$(curl -s $API/api/users/find -H "Authorization: Bearer $ADMIN" | node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).find(u => u.signInEmail === 'admin@local.dev').id")
DANA=$(login dana.user@school.example 'Temporary#2026')
echo "teacher=$TEACHER_ID dana=$DANA_ID self=$SELF_ID"
```

Run these and check each result. If `POST api/teachers` doesn't return `{ id }`, read the id from `GET api/teachers/find` instead.

- [ ] `call DELETE /api/users/$DANA_ID $DANA` → `403` (Teacher-role token; run this before Dana is deleted)
- [ ] `call DELETE /api/users/$SELF_ID $ADMIN` → `409`, `"code":"userMustNotDeleteSelf"`
- [ ] `call DELETE /api/users/00000000-0000-0000-0000-000000000001 $ADMIN` → `404`, `"code":"userNotFound"`
- [ ] `call DELETE /api/users/$DANA_ID $ADMIN` → `204`
- [ ] `call DELETE /api/users/$DANA_ID $ADMIN` → `409`, `"code":"userAlreadyDeleted"`
- [ ] `call GET /api/teachers/find $ADMIN` still lists "Dana Levi" (AC 3: the Teacher record is untouched)
- [ ] `call POST /api/users/$DANA_ID/restore $ADMIN` → `204`; `GET /api/users/find` shows Dana with `"isDeleted":false`
- [ ] `call POST /api/users/$DANA_ID/restore $ADMIN` → `409`, `"code":"userAlreadyActive"`
- [ ] Review Focus 3: `call DELETE /api/users/$DANA_ID $ADMIN` → `204`, then `call DELETE /api/teachers/$TEACHER_ID $ADMIN` → `204` (allowed, since the User is deleted), then `call POST /api/users/$DANA_ID/restore $ADMIN` → `409`, `"code":"userLinkedTeacherMustNotBeDeleted"`

Stop the API. Leave the database in place; task 4 recreates it.

- [ ] **Step 9: Commit**

```bash
git add src tests
git commit -m "feat(api): delete and restore Users through api/users (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
