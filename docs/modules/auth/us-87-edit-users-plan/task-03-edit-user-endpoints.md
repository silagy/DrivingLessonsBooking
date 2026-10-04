# Task 3 of 6: The Edit details, Change Role and Set Temporary Password endpoints

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Requires task 2 committed. Work on branch `87-edit-users`.

**Files:**
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`

**Interfaces:**
- Consumes:
  - From task 2: `ChangeUserDetailsInteractor.ExecuteAsync(Guid, ChangeUserDetailsRequest)`, `ChangeUserRoleInteractor.ExecuteAsync(Guid, ChangeUserRoleRequest)`, `SetUserTemporaryPasswordInteractor.ExecuteAsync(Guid, SetUserTemporaryPasswordRequest)`.
  - Existing: `AuthorizationPolicies.Administrator` on the controller class; `HttpCurrentUser` registered in `Program.cs`; the per-request stamp check from #86 (`SignedInUserJwtBearerEvents`); the camelCase string-enum JSON converter in `Program.cs`, so `"role":"teacher"` binds to `Role.Teacher`.
- Produces (task 4's client calls these):
  - `PUT api/users/{id}/details` with body `{ name, signInEmail }` returns 204. Errors: 404 `userNotFound`; 409 `userSignInEmailAlreadyInUse`, `userAlreadyDeleted`, `userNameMustNotBeEmpty` or `emailMustBeValid`.
  - `PUT api/users/{id}/role` with body `{ role }` returns 204. Errors: 404; 409 `userMustNotChangeOwnRole`, `userMustNotDemoteLastActiveAdministrator`, `userAlreadyHasRole`, `userWithTeacherRoleMustHaveLinkedTeacher` or `userAlreadyDeleted`.
  - `PUT api/users/{id}/temporary-password` with body `{ temporaryPassword }` returns 204. Errors: 404; 409 `temporaryPasswordMustNotBeEmpty` or `userAlreadyDeleted`.
  - All three are Administrator only, so a Teacher-role token gets 403.

**Why:**
- #87 AC 6: a demoted User loses Administrator access on their next request. This is checked here against the real API.
- #87 AC 7: the screen needs these endpoints.
- README decision 6; Review Focus 1 and 4.

Controllers contain no logic (CLAUDE.md rule 7), so this task has no unit test. The interactors are covered by task 2, and step 3's smoke test exercises the HTTP surface.

- [ ] **Step 1: Add the endpoints**

In `src\DrivingLessons.Presentation.Web\Controllers\User\UserCommandController.cs`, add these usings in alphabetical order with the other `Commands` usings:

```csharp
using DrivingLessons.Application.Commands.ChangeUserDetails;
using DrivingLessons.Application.Commands.ChangeUserRole;
using DrivingLessons.Application.Commands.SetUserTemporaryPassword;
```

Then add after `RestoreAsync`:

```csharp
    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the user's name and sign-in email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeUserDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeUserDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    [EndpointSummary("Change the user's role; they are signed out on their next request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRoleAsync(
        [FromServices] ChangeUserRoleInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeUserRoleRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/temporary-password")]
    [EndpointSummary("Set a new temporary password; the user is signed out on their next request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetTemporaryPasswordAsync(
        [FromServices] SetUserTemporaryPasswordInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] SetUserTemporaryPasswordRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
```

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
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us87_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us87_smoke"
```

Stop anything listening on port 5080. Then start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us87_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait until the output shows `Now listening on: http://localhost:5080`. Then, from a folder in the session scratchpad, run the setup below. Payloads are ASCII-only and written to files, because Hebrew on a Windows `curl` command line breaks (memory note).

```bash
mkdir -p smoke-87 && cd smoke-87
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
login() { printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json; curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken; }
login_status() { printf '{"email":"%s","password":"%s"}' "$1" "$2" > login.json; curl -s -o /dev/null -w "%{http_code}\n" -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json; }
call() { curl -s -o body.json -w "%{http_code}" -X "$1" "$API$2" -H "Authorization: Bearer $3" -H "Content-Type: application/json" ${4:+-d @$4}; echo " $(cat body.json)"; }
ADMIN=$(login admin@local.dev 'DevAdmin#2026')
printf '{"name":"Dana Levi","contactEmail":"dana@school.example"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher.json | json id)
printf '{"name":"Dana User","signInEmail":"dana.user@school.example","role":"teacher","teacherId":"%s","temporaryPassword":"Temporary#2026"}' "$TEACHER_ID" > dana.json
DANA_ID=$(curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @dana.json | json id)
printf '{"name":"Amit Rozen","signInEmail":"amit@school.example","role":"administrator","teacherId":null,"temporaryPassword":"Temporary#2026"}' > amit.json
AMIT_ID=$(curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @amit.json | json id)
SELF_ID=$(curl -s $API/api/users/find -H "Authorization: Bearer $ADMIN" | node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).find(u => u.signInEmail === 'admin@local.dev').id")
DANA=$(login dana.user@school.example 'Temporary#2026')
printf '{"role":"administrator"}' > to-admin.json
printf '{"role":"teacher"}' > to-teacher.json
echo "teacher=$TEACHER_ID dana=$DANA_ID amit=$AMIT_ID self=$SELF_ID"
```

If `POST api/teachers` doesn't return `{ id }`, read the id from `GET api/teachers/find` instead.

**Edit details** (AC 3, Review Focus 1):
- [ ] `printf '{"name":"Dana Levi-Cohen","signInEmail":"dana.user@school.example"}' > same-email.json; call PUT /api/users/$DANA_ID/details $ADMIN same-email.json` returns `204`. Saving with your own email is not refused.
- [ ] `printf '{"name":"Dana","signInEmail":"AMIT@school.example"}' > taken.json; call PUT /api/users/$DANA_ID/details $ADMIN taken.json` returns `409` with `"code":"userSignInEmailAlreadyInUse"`.
- [ ] `call PUT /api/users/00000000-0000-0000-0000-000000000001/details $ADMIN same-email.json` returns `404` with `"code":"userNotFound"`.
- [ ] `call PUT /api/users/$DANA_ID/details $DANA same-email.json` returns `403` (a Teacher-role token).
- [ ] `call GET /api/users/find $ADMIN` shows Dana as `"Dana Levi-Cohen"`, still with her `teacherId`.

**Change Role** (AC 2, AC 4, AC 6):
- [ ] `call PUT /api/users/$SELF_ID/role $ADMIN to-teacher.json` returns `409` with `"code":"userMustNotChangeOwnRole"`. The signed-in Administrator has no linked Teacher, so this also checks the self rule wins (Review Focus 3).
- [ ] `call PUT /api/users/$AMIT_ID/role $ADMIN to-teacher.json` returns `409` with `"code":"userWithTeacherRoleMustHaveLinkedTeacher"`.
- [ ] `call PUT /api/users/$DANA_ID/role $ADMIN to-teacher.json` returns `409` with `"code":"userAlreadyHasRole"`.
- [ ] `call GET /api/users/find $DANA` returns `403`: Dana's token is valid, but she isn't an Administrator yet.
- [ ] `call PUT /api/users/$DANA_ID/role $ADMIN to-admin.json` returns `204`.
- [ ] `call GET /api/users/find $DANA` now returns `401`. Her old token died with the stamp change.
- [ ] `DANA_ADMIN=$(login dana.user@school.example 'Temporary#2026'); call GET /api/users/find $DANA_ADMIN` returns `200`, and Dana is listed with `"role":"administrator"` and her `teacherId` (AC 5).
- [ ] AC 6 / Review Focus 4: `call PUT /api/users/$DANA_ID/role $ADMIN to-teacher.json` returns `204`, then `call GET /api/users/find $DANA_ADMIN` returns `401` on the very next request.

**Set Temporary Password:**
- [ ] `DANA=$(login dana.user@school.example 'Temporary#2026'); call GET /api/users/find $DANA` returns `403` (a valid token).
- [ ] `printf '{"temporaryPassword":"Fresh#2027"}' > password.json; call PUT /api/users/$DANA_ID/temporary-password $ADMIN password.json` returns `204`.
- [ ] `call GET /api/users/find $DANA` returns `401`.
- [ ] `login_status dana.user@school.example 'Temporary#2026'` returns `401`, and `login_status dana.user@school.example 'Fresh#2027'` returns `200`.
- [ ] `printf '{"temporaryPassword":"  "}' > blank.json; call PUT /api/users/$DANA_ID/temporary-password $ADMIN blank.json` returns `409` with `"code":"temporaryPasswordMustNotBeEmpty"`.

**Deleted User** (stale screen):
- [ ] `call DELETE /api/users/$AMIT_ID $ADMIN` returns `204`. After that, `call PUT /api/users/$AMIT_ID/temporary-password $ADMIN password.json` returns `409` with `"code":"userAlreadyDeleted"`.

Stop the API. Leave the database in place; task 6 drops it.

If `login_status` for a wrong password returns something other than `401`, write down what it returns and move on. That behavior belongs to the sign-in endpoint, not this slice.

- [ ] **Step 4: Commit**

```bash
git add src
git commit -m "feat(api): edit a User's details and Role and set a Temporary Password through api/users (#87)"
```

End the commit message with the attribution trailer from the session's instructions.
