# Task 3 of 6: The `PUT api/me/password` endpoint

> Part of [#88: Change My Own Password](README.md). Requires task 2 committed. Work on branch `88-change-my-password`.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Me\MeCommandController.cs`

**Interfaces:**
- Consumes:
  - From task 2: `ChangeMyPasswordInteractor.ExecuteAsync(ChangeMyPasswordRequest) : Task<ChangeMyPasswordResponse>`.
  - Existing: the `FallbackPolicy` in `Program.cs` (any authenticated User); `HttpCurrentUser` (reads `sub`); the per-request stamp check `SignedInUserJwtBearerEvents`; `ApiExceptionFilter`.
- Produces (task 4's client calls this):
  - `PUT api/me/password` with body `{ currentPassword, newPassword }` returns **200** with `{ accessToken, expiresAtUtc }`.
  - Errors: 401 with no or a stale token; 404 `userNotFound`; 409 `userCurrentPasswordMustBeCorrect`, `passwordMustNotBeEmpty` or `userAlreadyDeleted`.
  - Open to **both** Roles: no Administrator policy.

**Why:**
- #88 AC 2: "A 'Me' command endpoint changes the signed-in User's password; it is available to every authenticated User".
- #88 AC 5 (backend half): the fresh token works and the old one is dead. This is checked here against the real API.
- README decisions 1, 2 and 6; Review Focus 1 to 4.

Controllers contain no logic (CLAUDE.md rule 7), so this task has no unit test. The interactor is covered by task 2, and step 3's smoke test exercises the HTTP surface.

- [ ] **Step 1: Add the controller**

Create `src\DrivingLessons.Presentation.Web\Controllers\Me\MeCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ChangeMyPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Me;

[ApiController]
[Route("api/me")]
[Tags("Me")]
[Authorize]
public class MeCommandController : ControllerBase
{
    [HttpPut("password")]
    [EndpointSummary("Change the signed-in user's password; returns a fresh token so they stay signed in")]
    [ProducesResponseType(typeof(ChangeMyPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChangeMyPasswordResponse>> ChangePasswordAsync(
        [FromServices] ChangeMyPasswordInteractor interactor,
        [FromBody] [Required] ChangeMyPasswordRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return Ok(result);
    }
}
```

`[Authorize]` with no policy states the intent explicitly: any authenticated User. It matches the fallback policy, so a Teacher-role token is accepted. Do **not** add `Policy = AuthorizationPolicies.Administrator`.

- [ ] **Step 2: Build and run every backend suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: everything is green, and the last command reports no pending changes (this slice adds no column).

- [ ] **Step 3: Smoke-test the API on a throwaway database**

From the repository root:

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us88_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us88_smoke"
```

Stop anything listening on port 5080. Then start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us88_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait until the output shows `Now listening on: http://localhost:5080`. Then, from a folder in the session scratchpad, run the setup below. Payloads are ASCII-only and written to files, because Hebrew on a Windows `curl` command line breaks (memory note).

```bash
mkdir -p smoke-88 && cd smoke-88
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
login() { printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json; curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken; }
login_status() { printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json; curl -s -o /dev/null -w "%{http_code}\n" -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json; }
call() { curl -s -o body.json -w "%{http_code}" -X "$1" "$API$2" ${3:+-H "Authorization: Bearer $3"} -H "Content-Type: application/json" ${4:+-d @$4}; echo " $(cat body.json)"; }
change() { node -e "require('fs').writeFileSync('change.json', JSON.stringify({currentPassword:process.argv[1],newPassword:process.argv[2]}))" "$1" "$2"; }
ADMIN=$(login admin@local.dev 'DevAdmin#2026')
printf '{"name":"Dana Levi","contactEmail":"dana@school.example"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher.json | json id)
printf '{"name":"Dana User","signInEmail":"dana.user@school.example","role":"teacher","teacherId":"%s","temporaryPassword":"Temporary#2026"}' "$TEACHER_ID" > dana.json
DANA_ID=$(curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @dana.json | json id)
DANA=$(login dana.user@school.example 'Temporary#2026')
echo "teacher=$TEACHER_ID dana=$DANA_ID"
```

If `POST api/teachers` doesn't return `{ id }`, read the id from `GET api/teachers/find` instead.

**Refusals keep the User signed in** (AC 3, Review Focus 1):
- [ ] `change 'Wrong#2026' 'Fresh#2027'; call PUT /api/me/password $DANA change.json` returns `409` with `"code":"userCurrentPasswordMustBeCorrect"`. It is **not** `401`.
- [ ] `call GET /api/publications/find $DANA` (or any other authenticated GET Dana can reach) does **not** return `401`: her token survived the refusal. A `403` or `200` is fine.
- [ ] `change 'Temporary#2026' '   '; call PUT /api/me/password $DANA change.json` returns `409` with `"code":"passwordMustNotBeEmpty"`.
- [ ] `call PUT /api/me/password "" change.json` (no token) returns `401`.

**A Teacher-role User changes their own password** (AC 2, AC 5, Review Focus 2 and 3):
- [ ] `change 'Temporary#2026' 'Fresh#2027'; call PUT /api/me/password $DANA change.json` returns `200` with an `accessToken` and an `expiresAtUtc`.
- [ ] `DANA_NEW=$(cat body.json | json accessToken)`.
- [ ] `call GET /api/users/find $DANA` returns `401`: the old token died with the stamp change.
- [ ] `call GET /api/users/find $DANA_NEW` returns `403`, not `401`: the fresh token is valid, and Dana is still a Teacher.
- [ ] `login_status dana.user@school.example 'Temporary#2026'` returns `401`, and `login_status dana.user@school.example 'Fresh#2027'` returns `200`.

**An Administrator changes their own password, with spaces kept** (Review Focus 4):
- [ ] `change 'DevAdmin#2026' ' Spaced Admin 2027 '; call PUT /api/me/password $ADMIN change.json` returns `200`.
- [ ] `ADMIN_NEW=$(cat body.json | json accessToken); call GET /api/users/find $ADMIN_NEW` returns `200`, and `call GET /api/users/find $ADMIN` returns `401`.
- [ ] `login_status admin@local.dev ' Spaced Admin 2027 '` returns `200`, and `login_status admin@local.dev 'Spaced Admin 2027'` (trimmed) returns `401`.
- [ ] Put the seeded password back so later tasks can sign in: `change ' Spaced Admin 2027 ' 'DevAdmin#2026'; call PUT /api/me/password $ADMIN_NEW change.json` returns `200`.

**The User's other sessions end** (README decision 1):
- [ ] `D1=$(login dana.user@school.example 'Fresh#2027'); D2=$(login dana.user@school.example 'Fresh#2027')`. Then `change 'Fresh#2027' 'Fresh#2028'; call PUT /api/me/password $D1 change.json` returns `200`, and `call GET /api/users/find $D2` returns `401`.

Stop the API. Leave the database in place; task 6 drops it.

- [ ] **Step 4: Commit**

```bash
git add src
git commit -m "feat(api): change my own password through PUT api/me for every signed-in User (#88)"
```

End the commit message with the attribution trailer from the session's instructions.
