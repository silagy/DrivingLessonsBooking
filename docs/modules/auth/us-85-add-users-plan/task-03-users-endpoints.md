# Task 3 of 7: Administrator-only Users endpoints

> Part of [#85: Add Users from a New Users Screen](README.md). Requires tasks 1 and 2 committed. Work on branch `85-add-users-screen`.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Program.cs` (JWT bearer claim mapping and Role claim type, `Administrator` policy)
- Create: `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`
- Create: `src\DrivingLessons.Presentation.Web\Controllers\User\UserQueryController.cs`
- Nothing else. The smoke script in Step 5 lives in the session scratchpad and is not committed.

**Interfaces:**
- Consumes: from task 1, `CreateUserInteractor.ExecuteAsync(CreateUserRequest) : Task<CreateUserResponse>`; from task 2, `FindUsersInteractor.ExecuteAsync()`, `GetUserInteractor.ExecuteAsync(Guid)`. Existing `AuthClaims.Role` (`"role"`) and `AuthClaims.AdministratorRole` (`"administrator"`) in `DrivingLessons.Infrastructure.Auth`.
- Produces (the contract task 5's client consumes):
  - `POST api/users` body `{ name, signInEmail, role: "administrator" | "teacher", teacherId: string | null, temporaryPassword }` → `201 { id }`; `404` `teacherNotFound`; `409` `userSignInEmailAlreadyInUse`, `teacherAlreadyLinkedToUser`, `userWithTeacherRoleMustHaveLinkedTeacher`, `temporaryPasswordMustNotBeEmpty`, `userNameMustNotBeEmpty`, `emailMustBeValid`
  - `GET api/users/find` → `200 ItemForFindUsersResponse[]`
  - `GET api/users/{id:guid}` → `200 GetUserResponse`; `404` `userNotFound`
  - every `api/users` call: `401` without a token, `403` for a token whose `role` is not `administrator`
  - `DrivingLessons.Presentation.Web.Auth.AuthorizationPolicies.Administrator` (`"Administrator"`), which #89 makes the default

**Why:** #85 acceptance criteria 5 (no endpoint changes a User's linked Teacher) and 8 (list / get / create endpoints follow the API guidelines and are Administrator-only). README decision 6. #82 says authorization wiring is verified manually, not unit-tested, so Step 5 is the check.

- [ ] **Step 1: Add the policy name**

Create `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs`:

```csharp
namespace DrivingLessons.Presentation.Web.Auth;

public static class AuthorizationPolicies
{
    public const string Administrator = "Administrator";
}
```

- [ ] **Step 2: Read Roles from the `role` claim and add the policy**

In `src\DrivingLessons.Presentation.Web\Program.cs`, add these usings in alphabetical order with the others:

```csharp
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Presentation.Web.Auth;
```

Replace the `.AddJwtBearer(...)` block with:

```csharp
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = AuthClaims.Role
        };
    });
```

Replace the `builder.Services.AddAuthorization(...)` statement with:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(
        AuthorizationPolicies.Administrator,
        policy => policy.RequireRole(AuthClaims.AdministratorRole));
});
```

`MapInboundClaims = false` keeps the token's claim names as issued (`role`, `sub`), so `RoleClaimType = "role"` is what `RequireRole` reads. Nothing on the server reads claims today, so no other code changes.

- [ ] **Step 3: Add the controllers**

Create `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`. There is deliberately no endpoint that changes a User's linked Teacher (#85 criterion 5):

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public class UserCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a user who can sign in with a temporary password")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponse>> CreateAsync(
        [FromServices] CreateUserInteractor interactor,
        [FromBody] [Required] CreateUserRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }
}
```

Create `src\DrivingLessons.Presentation.Web\Controllers\User\UserQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.FindUsers;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public class UserQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a user")]
    [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetUserResponse> GetAsync(
        [FromServices] GetUserInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Find all users, active users first")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindUsersResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindUsersResponse>> FindAsync(
        [FromServices] FindUsersInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
```

- [ ] **Step 4: Build and run the suites**

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: build clean, every test PASS, `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Smoke-test the endpoints against a throwaway database**

README decision 15: never run this against the dev database. Use the compose Postgres:

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us85_smoke"
```

Stop anything listening on port 5080 (`preview_stop` an `api` preview if one runs). Start the API in the background against the throwaway database (Bash tool with `run_in_background: true`, from the repository root):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us85_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait until its output shows `Now listening on: http://localhost:5080`. Start-up applies every migration and seeds `admin@local.dev` / `DevAdmin#2026` (the database is empty).

Save this script as `smoke-85.sh` in the session scratchpad directory and run it with `bash smoke-85.sh`. It uses ASCII-only payloads written to files and relative `@file` paths, because Hebrew or MSYS paths on Windows `curl` command lines break:

```bash
set -u
API=http://localhost:5080/api
cd "$(dirname "$0")"
mkdir -p smoke-85 && cd smoke-85

check() {
    if [ "$3" = "$2" ]; then echo "PASS $1"; else echo "FAIL $1: expected $2, got $3"; fi
}
field() {
    node -pe "JSON.parse(require('fs').readFileSync('body.json','utf8'))$1"
}
post() {
    curl -s -o body.json -w "%{http_code}" -X POST "$API/$1" -H "Authorization: Bearer $2" -H "Content-Type: application/json" -d @"$3"
}
get() {
    curl -s -o body.json -w "%{http_code}" "$API/$1" -H "Authorization: Bearer $2"
}
login() {
    printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json
    curl -s -o body.json -X POST "$API/auth/login" -H "Content-Type: application/json" -d @login.json
    field .accessToken
}
user_json() {
    printf '{"name":"%s","signInEmail":"%s","role":"%s","teacherId":%s,"temporaryPassword":"%s"}' "$1" "$2" "$3" "$4" "$5" > "$6"
}

ADMIN=$(login admin@local.dev 'DevAdmin#2026')

printf '{"name":"Smoke Teacher One","contactEmail":"teacher.one@school.example"}' > t1.json
printf '{"name":"Smoke Teacher Two","contactEmail":"teacher.two@school.example"}' > t2.json
post teachers "$ADMIN" t1.json > /dev/null; T1=$(field .id)
post teachers "$ADMIN" t2.json > /dev/null; T2=$(field .id)

check "no token is 401" 401 "$(curl -s -o /dev/null -w "%{http_code}" "$API/users/find")"
check "admin lists users" 200 "$(get users/find "$ADMIN")"
check "only the first Administrator" 1 "$(field .length)"
check "first Administrator role" administrator "$(field '[0].role')"
check "first Administrator unlinked" null "$(field '[0].teacherId')"

user_json "Smoke User" smoke.user@school.example teacher "\"$T1\"" 'Smoke#2026' u1.json
check "create Teacher-role User" 201 "$(post users "$ADMIN" u1.json)"
U1=$(field .id)
check "list has two users" 2 "$(get users/find "$ADMIN" > /dev/null; field .length)"
check "get the new User" 200 "$(get "users/$U1" "$ADMIN")"
check "linked Teacher name" "Smoke Teacher One" "$(field .teacherName)"
check "sign-in email" smoke.user@school.example "$(field .signInEmail)"
check "unknown User is 404" 404 "$(get users/00000000-0000-0000-0000-000000000001 "$ADMIN")"
check "unknown User code" userNotFound "$(field .code)"

user_json "Other" " Smoke.User@School.Example " administrator null 'Smoke#2026' dup-email.json
check "email in use is 409" 409 "$(post users "$ADMIN" dup-email.json)"
check "email in use code" userSignInEmailAlreadyInUse "$(field .code)"
user_json "Other" other@school.example teacher "\"$T1\"" 'Smoke#2026' dup-teacher.json
check "linked Teacher is 409" 409 "$(post users "$ADMIN" dup-teacher.json)"
check "linked Teacher code" teacherAlreadyLinkedToUser "$(field .code)"
user_json "Other" other@school.example teacher null 'Smoke#2026' no-teacher.json
check "Teacher Role without Teacher is 409" 409 "$(post users "$ADMIN" no-teacher.json)"
check "Teacher Role without Teacher code" userWithTeacherRoleMustHaveLinkedTeacher "$(field .code)"
user_json "Other" other@school.example administrator null '   ' blank-password.json
check "blank password is 409" 409 "$(post users "$ADMIN" blank-password.json)"
check "blank password code" temporaryPasswordMustNotBeEmpty "$(field .code)"
user_json "Other" other@school.example teacher '"00000000-0000-0000-0000-000000000002"' 'Smoke#2026' unknown-teacher.json
check "unknown Teacher is 404" 404 "$(post users "$ADMIN" unknown-teacher.json)"
check "unknown Teacher code" teacherNotFound "$(field .code)"

TEACHER=$(login smoke.user@school.example 'Smoke#2026')
check "Teacher-role User lists users is 403" 403 "$(get users/find "$TEACHER")"
check "Teacher-role User gets a User is 403" 403 "$(get "users/$U1" "$TEACHER")"
check "Teacher-role User creates a User is 403" 403 "$(post users "$TEACHER" u1.json)"
check "Teacher-role User still reaches other endpoints" 200 "$(get teachers/find "$TEACHER")"

docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us85_smoke -c "UPDATE users SET is_deleted = true WHERE email = 'smoke.user@school.example'" > /dev/null
user_json "Other" smoke.user@school.example administrator null 'Smoke#2026' deleted-email.json
check "Deleted User's email is still taken" 409 "$(post users "$ADMIN" deleted-email.json)"
check "Deleted User's email code" userSignInEmailAlreadyInUse "$(field .code)"
check "Deleted User's Teacher is still linked" 409 "$(post users "$ADMIN" dup-teacher.json)"
check "Deleted User's Teacher code" teacherAlreadyLinkedToUser "$(field .code)"
check "list after delete" 200 "$(get users/find "$ADMIN")"
check "Deleted User sorts last" true "$(field '[1].isDeleted')"

user_json "Owner Teaches" owner.teaches@school.example administrator "\"$T2\"" 'Smoke#2026' admin-linked.json
check "Administrator linked to a Teacher" 201 "$(post users "$ADMIN" admin-linked.json)"
```

Expected: every line starts with `PASS`. If a line says `FAIL`, stop and fix the cause (with a failing test first where one can be written) before committing. When "Deleted User sorts last" runs, the list holds only the first Administrator (active) and the Deleted User, so index 1 must be the Deleted User.

Stop the API, then drop the throwaway database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE drivinglessons_us85_smoke"
```

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Auth/AuthorizationPolicies.cs src/DrivingLessons.Presentation.Web/Program.cs src/DrivingLessons.Presentation.Web/Controllers/User
git commit -m "feat(api): Administrator-only endpoints to list, get and create Users (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
