# Task 4 of 8: GET api/me and the Teacher-scoping smoke (backend)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 3 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 2, 3, 5, 7 and 14 first.

**Files:**
- Create: `src\DrivingLessons.Application\Queries\GetMe\GetMeInteractor.cs`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Me\MeQueryController.cs`
- Modify: `.claude\rules\api-guidelines.md` (Auth section)
- Test: `tests\DrivingLessons.Application.Test\Queries\GetMeInteractorTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` (matrix gains `MeQueryController.GetAsync`)

**Interfaces:**
- Consumes:
  - `ICurrentUser { UserId Id; Role Role; TeacherId? TeacherId }` (task 1). This task reads only `Id`.
  - `IUserQueries.GetAsync(Guid id)` returning `GetUserResponse?` (Id, Name, SignInEmail, Role, TeacherId, TeacherName, IsDeleted) and `UserNotFoundException(UserId id)`, both existing.
  - The scoped interactors of tasks 2 and 3 (exercised by the smoke in step 8).
- Produces:
  - `public class GetMeInteractor(IUserQueries queries, ICurrentUser currentUser)` with `Task<GetUserResponse> ExecuteAsync()`, registered `services.AddScoped<GetMeInteractor>()`.
  - `GET api/me` (`MeQueryController.GetAsync`, Teacher-or-Administrator): `200` with `{ id, name, signInEmail, role, teacherId, teacherName, isDeleted }`, `role` as a camelCase string (`"administrator"` / `"teacher"`), `teacherId` / `teacherName` null for an unlinked User; `404` when the signed-in User no longer exists; `401` anonymous. Task 5's `SignedInUserApiService.getMe()` reads this.

**Why:** decision 7. The token carries `teacher_id` but no Teacher name, and a Teacher can't call `api/teachers/find`, so `api/me` is the only source for the 10a locked chip. The smoke in step 8 is the end-to-end proof of Review Focus 1, 2 and 4 against a real database.

- [ ] **Step 1: Write the failing interactor test**

Create `tests\DrivingLessons.Application.Test\Queries\GetMeInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetMe;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetMeInteractorTest
{
    private IUserQueries queries = null!;
    private ICurrentUser currentUser = null!;
    private GetMeInteractor interactor = null!;
    private UserId signedInUserId = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IUserQueries>();
        currentUser = A.Fake<ICurrentUser>();
        interactor = new GetMeInteractor(queries, currentUser);

        signedInUserId = UserId.New();

        A.CallTo(() => currentUser.Id).Returns(signedInUserId);
        A.CallTo(() => queries.GetAsync(A<Guid>._)).Returns((GetUserResponse?)null);
    }

    [TestMethod]
    public async Task Returns_The_Signed_In_User_With_The_Linked_Teacher()
    {
        //given
        var teacherId = Guid.NewGuid();
        var me = new GetUserResponse
        {
            Id = signedInUserId.Value,
            Name = "Yael Carmi",
            SignInEmail = "yael.user@school.example",
            Role = Role.Teacher,
            TeacherId = teacherId,
            TeacherName = "Yael Carmi",
            IsDeleted = false
        };
        A.CallTo(() => queries.GetAsync(signedInUserId.Value)).Returns(me);

        //when
        var result = await interactor.ExecuteAsync();

        //then
        result.ShouldBe(me);
        result.TeacherId.ShouldBe(teacherId);
        result.TeacherName.ShouldBe("Yael Carmi");
        A.CallTo(() => queries.GetAsync(signedInUserId.Value)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Missing_Signed_In_User_Is_Not_Found()
    {
        //when
        var act = () => interactor.ExecuteAsync();

        //then
        var exception = await Should.ThrowAsync<UserNotFoundException>(act);
        exception.Message.ShouldContain(signedInUserId.Value.ToString());
    }
}
```

- [ ] **Step 2: Add the endpoint to the policy matrix**

In `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`, replace the `TeacherOrAdministratorEndpoints` array and the first test method with:

```csharp
    private static readonly string[] TeacherOrAdministratorEndpoints =
    [
        "WeekScheduleQueryController.GetByTeacherAndWeek",
        "WeekScheduleCommandController.MarkSlotUnavailable",
        "WeekScheduleCommandController.MarkSlotAvailable",
        "PublicationQueryController.GetByWeek",
        "PublicationQueryController.GetDashboard",
        "PublicationQueryController.FindHistory",
        "PublicationQueryController.DownloadExcel",
        "MeCommandController.ChangePasswordAsync",
        "MeQueryController.GetAsync",
    ];
```

```csharp
    [TestMethod]
    public void Teachers_Reach_Only_Week_Schedules_Publications_And_Their_Own_User()
    {
        //when
        var endpoints = EndpointsWithRule(AuthorizationPolicies.TeacherOrAdministrator);

        //then
        endpoints.ShouldBe(TeacherOrAdministratorEndpoints, ignoreOrder: true);
    }
```

Leave the rest of the file as it is.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetMeInteractorTest|FullyQualifiedName~ControllerAuthorizationTest"`

Expected: build FAILS with `The type or namespace name 'GetMe' does not exist in the namespace 'DrivingLessons.Application.Queries'` and `The type or namespace name 'GetMeInteractor' could not be found`.

- [ ] **Step 4: Write the interactor and register it**

Create `src\DrivingLessons.Application\Queries\GetMe\GetMeInteractor.cs`:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries.GetUser;

namespace DrivingLessons.Application.Queries.GetMe;

public class GetMeInteractor
{
    private readonly IUserQueries queries;
    private readonly ICurrentUser currentUser;

    public GetMeInteractor(IUserQueries queries, ICurrentUser currentUser)
    {
        this.queries = queries;
        this.currentUser = currentUser;
    }

    public async Task<GetUserResponse> ExecuteAsync()
    {
        var userId = currentUser.Id;
        var id = userId.Value;

        var user = await queries.GetAsync(id);

        if (user is null)
        {
            throw new UserNotFoundException(userId);
        }

        return user;
    }
}
```

In `src\DrivingLessons.Application\DependencyInjection.cs`, add the using between `...Queries.GetLatestRosterImport;` and `...Queries.GetPublication;`:

```csharp
using DrivingLessons.Application.Queries.GetMe;
```

and register the interactor directly below `services.AddScoped<GetUserInteractor>();`:

```csharp
        services.AddScoped<GetMeInteractor>();
```

- [ ] **Step 5: Run the tests to see the interactor pass and the matrix still fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetMeInteractorTest|FullyQualifiedName~ControllerAuthorizationTest"`

Expected: the 2 `GetMeInteractorTest` cases PASS. `Teachers_Reach_Only_Week_Schedules_Publications_And_Their_Own_User` FAILS: the actual list is missing `"MeQueryController.GetAsync"`. The other 6 matrix cases pass.

- [ ] **Step 6: Add the controller**

Create `src\DrivingLessons.Presentation.Web\Controllers\Me\MeQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.GetMe;
using DrivingLessons.Application.Queries.GetUser;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Me;

[ApiController]
[Route("api/me")]
[Tags("Me")]
[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]
public class MeQueryController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get the signed-in user, with the linked Teacher's name")]
    [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetUserResponse> GetAsync([FromServices] GetMeInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
```

The policy sits on the class only; the action has no `[Authorize]` of its own (never stack them, api-guidelines Auth).

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~GetMeInteractorTest|FullyQualifiedName~ControllerAuthorizationTest"`

Expected: PASS (9 test cases: 2 interactor, 7 matrix).

Then run the whole backend suite:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: all PASS, build without new warnings, no pending model changes.

- [ ] **Step 8: Write down the Teacher scoping rule**

In `.claude\rules\api-guidelines.md`, section `## Auth`, insert these bullets directly below the bullet starting "Never stack a class-level and an action-level policy":

```markdown
- Teacher scoping (#90): a Teacher-role User reaches only the data of their linked Teacher. Every Teacher-reachable interactor takes `ICurrentUser` and calls `currentUser.MayReach(teacherId)` (`Application\Auth\CurrentUserExtension.cs`). When the Teacher id is an input, the check runs before anything is loaded; when only the aggregate knows its Teacher (the Slot commands), it runs right after the load and before any change or commit
- A refusal throws the entity's existing not-found exception (`WeekScheduleNotFoundException`, `PublicationNotFoundException`), so the API answers 404, never 403, and never reveals that another Teacher's data exists
- Lists that span Teachers (History) filter in the query by an optional Teacher id: the interactor passes the linked Teacher id for a Teacher and null for an Administrator. Administrators bypass scoping, linked to a Teacher or not
- `GET api/me` returns the signed-in User (`GetUserResponse`, including `TeacherName`); the token has no Teacher name
```

- [ ] **Step 9: Smoke-test Teacher scoping against the running API**

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us90_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us90_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us90_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed from a scratchpad folder. Two Teachers: A (Yael) and B (Oren). A Teacher-role User linked to A. A Teacher can be linked to only one User (`TeacherAlreadyLinkedToUserException`), so the linked Administrator is linked to **B**: reaching A then proves a linked Administrator isn't narrowed to their own Teacher. The Administrator creates both Week Schedules for the same week, which creates that week's Draft Publication (`WeekScheduleCreatedHandler`).

Hebrew payloads are written to files with `node` and posted by relative `@file` paths; Hebrew never goes on the `curl` command line, where Windows turns it into `?????` (memory note). Hebrew in responses is compared inside `node` against the payload files, never echoed.

```bash
mkdir -p smoke-90 && cd smoke-90
API=http://localhost:5080
WEEK=2026-10-11
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > admin-login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @admin-login.json | json accessToken)
node -e "require('fs').writeFileSync('t-a.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
node -e "require('fs').writeFileSync('t-b.json', JSON.stringify({name:'אורן ביטון',contactEmail:'oren@school.example'}))"
TEACHER_A=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t-a.json | json id)
TEACHER_B=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t-b.json | json id)
node -e "require('fs').writeFileSync('u-teacher.json', JSON.stringify({name:'יעל כרמי',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$TEACHER_A"
node -e "require('fs').writeFileSync('u-admin.json', JSON.stringify({name:'אורן ביטון',signInEmail:'oren.admin@school.example',role:'administrator',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$TEACHER_B"
echo "create teacher user:  $(code -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u-teacher.json)"
echo "create linked admin:  $(code -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u-admin.json)"
printf '{"email":"yael.user@school.example","password":"Temporary#2026"}' > teacher-login.json
printf '{"email":"oren.admin@school.example","password":"Temporary#2026"}' > linked-login.json
TEACHER=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @teacher-login.json | json accessToken)
LINKED=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @linked-login.json | json accessToken)
node -e "require('fs').writeFileSync('week-a.json', JSON.stringify({teacherId:process.argv[1],weekStart:process.argv[2]}))" "$TEACHER_A" "$WEEK"
node -e "require('fs').writeFileSync('week-b.json', JSON.stringify({teacherId:process.argv[1],weekStart:process.argv[2]}))" "$TEACHER_B" "$WEEK"
echo "create week A:        $(code -X POST $API/api/week-schedules -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @week-a.json)"
echo "create week B:        $(code -X POST $API/api/week-schedules -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @week-b.json)"
curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_A&week=$WEEK" -H "Authorization: Bearer $ADMIN" > schedule-a.json
curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$WEEK" -H "Authorization: Bearer $ADMIN" > schedule-b.json
SCHEDULE_A=$(json id < schedule-a.json)
SLOT_A=$(json "slots[0].id" < schedule-a.json)
SCHEDULE_B=$(json id < schedule-b.json)
SLOT_B=$(json "slots[0].id" < schedule-b.json)
PUBLICATION=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "Authorization: Bearer $ADMIN" | json id)
```

As the Teacher (linked to A):

```bash
echo "teacher me:              $(code $API/api/me -H "Authorization: Bearer $TEACHER")"
curl -s $API/api/me -H "Authorization: Bearer $TEACHER" > me-teacher.json
echo "teacher me is Yael/A:    $(node -pe "const fs=require('fs');const me=JSON.parse(fs.readFileSync('me-teacher.json','utf8'));const a=JSON.parse(fs.readFileSync('t-a.json','utf8'));me.role==='teacher'&&me.teacherId===process.argv[1]&&me.teacherName===a.name&&me.signInEmail==='yael.user@school.example'" "$TEACHER_A")"
echo "teacher week B:          $(code "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$WEEK" -H "Authorization: Bearer $TEACHER")"
echo "teacher week A:          $(code "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_A&week=$WEEK" -H "Authorization: Bearer $TEACHER")"
echo "teacher marks B slot:    $(code -X POST $API/api/week-schedules/$SCHEDULE_B/slots/$SLOT_B/mark-unavailable -H "Authorization: Bearer $TEACHER")"
echo "teacher marks A slot:    $(code -X POST $API/api/week-schedules/$SCHEDULE_A/slots/$SLOT_A/mark-unavailable -H "Authorization: Bearer $TEACHER")"
curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$WEEK" -H "Authorization: Bearer $ADMIN" > schedule-b-after.json
echo "B slot still open:       $(json "slots[0].state==='open'" < schedule-b-after.json)"
echo "teacher dashboard B:     $(code "$API/api/publications/$PUBLICATION/dashboard?teacherId=$TEACHER_B" -H "Authorization: Bearer $TEACHER")"
echo "teacher dashboard A:     $(code "$API/api/publications/$PUBLICATION/dashboard?teacherId=$TEACHER_A" -H "Authorization: Bearer $TEACHER")"
echo "teacher excel B:         $(code "$API/api/publications/$PUBLICATION/excel?teacherId=$TEACHER_B" -H "Authorization: Bearer $TEACHER")"
echo "teacher excel A:         $(code "$API/api/publications/$PUBLICATION/excel?teacherId=$TEACHER_A" -H "Authorization: Bearer $TEACHER")"
curl -s $API/api/publications/history -H "Authorization: Bearer $TEACHER" > history-teacher.json
echo "teacher history only A:  $(node -pe "const rows=JSON.parse(require('fs').readFileSync('history-teacher.json','utf8'));rows.length===1&&rows.every(row=>row.teacherId===process.argv[1])" "$TEACHER_A")"
echo "anonymous me:            $(code $API/api/me)"
```

As the Administrator linked to B:

```bash
curl -s $API/api/me -H "Authorization: Bearer $LINKED" > me-linked.json
echo "linked me is Oren/B:     $(node -pe "const fs=require('fs');const me=JSON.parse(fs.readFileSync('me-linked.json','utf8'));const b=JSON.parse(fs.readFileSync('t-b.json','utf8'));me.role==='administrator'&&me.teacherId===process.argv[1]&&me.teacherName===b.name" "$TEACHER_B")"
echo "linked week A:           $(code "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_A&week=$WEEK" -H "Authorization: Bearer $LINKED")"
echo "linked week B:           $(code "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$WEEK" -H "Authorization: Bearer $LINKED")"
echo "linked restores A slot:  $(code -X POST $API/api/week-schedules/$SCHEDULE_A/slots/$SLOT_A/mark-available -H "Authorization: Bearer $LINKED")"
echo "linked dashboard A:      $(code "$API/api/publications/$PUBLICATION/dashboard?teacherId=$TEACHER_A" -H "Authorization: Bearer $LINKED")"
echo "linked excel A:          $(code "$API/api/publications/$PUBLICATION/excel?teacherId=$TEACHER_A" -H "Authorization: Bearer $LINKED")"
curl -s $API/api/publications/history -H "Authorization: Bearer $LINKED" > history-linked.json
echo "linked history A and B:  $(node -pe "const rows=JSON.parse(require('fs').readFileSync('history-linked.json','utf8'));const ids=rows.map(row=>row.teacherId);rows.length===2&&ids.includes(process.argv[1])&&ids.includes(process.argv[2])" "$TEACHER_A" "$TEACHER_B")"
curl -s $API/api/me -H "Authorization: Bearer $ADMIN" > me-admin.json
echo "unlinked admin me:       $(node -pe "const me=JSON.parse(require('fs').readFileSync('me-admin.json','utf8'));me.role==='administrator'&&me.teacherId===null&&me.teacherName===null")"
```

Expected:

| Line | Result |
|------|--------|
| create teacher user, create linked admin | `201` each |
| create week A, create week B | `201` |
| teacher me | `200` |
| teacher me is Yael/A | `true` |
| teacher week B | `404` |
| teacher week A | `200` |
| teacher marks B slot | `404` |
| teacher marks A slot | `204` |
| B slot still open | `true` (the refused command changed nothing) |
| teacher dashboard B | `404` |
| teacher dashboard A | `200` |
| teacher excel B | `404` |
| teacher excel A | `200` |
| teacher history only A | `true` |
| anonymous me | `401` |
| linked me is Oren/B | `true` |
| linked week A, linked week B | `200` each |
| linked restores A slot | `204` |
| linked dashboard A, linked excel A | `200` each |
| linked history A and B | `true` |
| unlinked admin me | `true` |

A `200` on any `B` line for the Teacher, or `false` on a check line, is a defect in tasks 1 to 4: fix it with a failing test first, before the commit below.

Stop the API (`TaskStop` on the background task), then drop the smoke database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE drivinglessons_us90_smoke WITH (FORCE)"
```

Expected: `DROP DATABASE`.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/Queries/GetMe/GetMeInteractor.cs src/DrivingLessons.Application/DependencyInjection.cs src/DrivingLessons.Presentation.Web/Controllers/Me/MeQueryController.cs tests/DrivingLessons.Application.Test/Queries/GetMeInteractorTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs .claude/rules/api-guidelines.md
git commit -m "feat(api): GET api/me returns the signed-in User with the linked Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
