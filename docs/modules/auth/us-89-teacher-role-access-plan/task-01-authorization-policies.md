# Task 1 of 7: Authorization policies (backend)

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Work on branch `89-teacher-role-navigation`. Read README decisions 1-3 first.

**Files:**
- Modify: `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Program.cs:58-64`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\WeekSchedule\WeekScheduleQueryController.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\WeekSchedule\WeekScheduleCommandController.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Me\MeCommandController.cs`
- Modify: `.claude\rules\api-guidelines.md` (Auth section)
- Test: `tests\DrivingLessons.Application.Test\Auth\AuthorizationPoliciesTest.cs` (**new**)
- Test: `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` (**new**)

**Interfaces:**
- Consumes: `DrivingLessons.Infrastructure.Auth.AuthClaims` (`Role = "role"`, `AdministratorRole = "administrator"`, `TeacherRole = "teacher"`). JWT bearer already runs with `RoleClaimType = AuthClaims.Role` and `MapInboundClaims = false`.
- Produces:
  - `AuthorizationPolicies.Administrator = "Administrator"` (exists), `AuthorizationPolicies.TeacherOrAdministrator = "TeacherOrAdministrator"` (new)
  - `public static void AuthorizationPolicies.Configure(AuthorizationOptions options)`: sets `DefaultPolicy` and `FallbackPolicy` to the Administrator policy and registers both named policies.
  - HTTP contract the client relies on (tasks 3-7): a signed-in Teacher-role User gets **403** from every Administrator-only endpoint and **200 / 404 / 409** (normal behavior) from the eight Teacher-or-Administrator endpoints. Anonymous callers still get **401**.

**Why:** AC 1-3. The policy matrix is the security boundary of #89; everything on the client only hides what this task refuses.

- [ ] **Step 1: Write the failing policy test**

Create `tests\DrivingLessons.Application.Test\Auth\AuthorizationPoliciesTest.cs`:

```csharp
using System.Security.Claims;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class AuthorizationPoliciesTest
{
    [TestMethod]
    [DataRow(AuthClaims.AdministratorRole, AuthorizationPolicies.Administrator, true)]
    [DataRow(AuthClaims.TeacherRole, AuthorizationPolicies.Administrator, false)]
    [DataRow(AuthClaims.AdministratorRole, AuthorizationPolicies.TeacherOrAdministrator, true)]
    [DataRow(AuthClaims.TeacherRole, AuthorizationPolicies.TeacherOrAdministrator, true)]
    [DataRow("student", AuthorizationPolicies.Administrator, false)]
    [DataRow("student", AuthorizationPolicies.TeacherOrAdministrator, false)]
    public async Task Policy_Admits_Only_Its_Roles(string role, string policy, bool expected)
    {
        //given
        var authorization = Services().GetRequiredService<IAuthorizationService>();
        var user = SignedIn(role);

        //when
        var result = await authorization.AuthorizeAsync(user, policy);

        //then
        result.Succeeded.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(AuthClaims.AdministratorRole, true)]
    [DataRow(AuthClaims.TeacherRole, false)]
    public async Task Endpoints_Without_A_Policy_Are_Administrator_Only(string role, bool expected)
    {
        //given
        var services = Services();
        var options = services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var user = SignedIn(role);

        //when
        var fallback = await authorization.AuthorizeAsync(user, options.FallbackPolicy!);
        var byDefault = await authorization.AuthorizeAsync(user, options.DefaultPolicy);

        //then
        fallback.Succeeded.ShouldBe(expected);
        byDefault.Succeeded.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Anonymous_Caller_Passes_No_Policy()
    {
        //given
        var authorization = Services().GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        //when
        var result = await authorization.AuthorizeAsync(anonymous, AuthorizationPolicies.TeacherOrAdministrator);

        //then
        result.Succeeded.ShouldBeFalse();
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal SignedIn(string role)
    {
        Claim[] claims = [new(AuthClaims.Role, role)];
        var identity = new ClaimsIdentity(claims, "Bearer", "sub", AuthClaims.Role);

        return new ClaimsPrincipal(identity);
    }
}
```

- [ ] **Step 2: Write the failing endpoint-matrix test**

Create `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs`. It reads the attributes on every controller action in `Presentation.Web` and names the effective rule: `Anonymous`, a policy name, `Administrator` when there is no attribute (the fallback) or a bare `[Authorize]` (the default), or several names joined with `+` when class and action attributes stack (ASP.NET requires all of them).

```csharp
using System.Reflection;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class ControllerAuthorizationTest
{
    private const string Anonymous = "Anonymous";

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
    ];

    private static readonly string[] AnonymousEndpoints =
    [
        "AuthController.Login",
        "SubmissionCommandController.IdentifyAsync",
        "SubmissionCommandController.CreateAsync",
        "SubmissionCommandController.ReviseAsync",
        "SubmissionQueryController.GetPublicationByLinkAsync",
    ];

    [TestMethod]
    public void Teachers_Reach_Only_Week_Schedules_Publications_And_Their_Own_Password()
    {
        //when
        var endpoints = EndpointsWithRule(AuthorizationPolicies.TeacherOrAdministrator);

        //then
        endpoints.ShouldBe(TeacherOrAdministratorEndpoints, ignoreOrder: true);
    }

    [TestMethod]
    public void Only_Sign_In_And_The_Student_Form_Are_Anonymous()
    {
        //when
        var endpoints = EndpointsWithRule(Anonymous);

        //then
        endpoints.ShouldBe(AnonymousEndpoints, ignoreOrder: true);
    }

    [TestMethod]
    public void Every_Other_Endpoint_Is_Administrator_Only()
    {
        //when
        var others = Endpoints()
            .Where(endpoint => !TeacherOrAdministratorEndpoints.Contains(endpoint.Key))
            .Where(endpoint => !AnonymousEndpoints.Contains(endpoint.Key))
            .ToList();

        //then
        others.ShouldNotBeEmpty();
        others.ShouldAllBe(endpoint => endpoint.Value == AuthorizationPolicies.Administrator);
    }

    [TestMethod]
    [DataRow("WeekScheduleCommandController.Create")]
    [DataRow("PublicationCommandController.Publish")]
    [DataRow("PublicationCommandController.ExtendWindow")]
    [DataRow("PublicationCommandController.Reopen")]
    public void Week_Schedule_Creation_And_The_Publication_Lifecycle_Stay_Administrator_Only(string endpoint)
    {
        //when
        var rule = Endpoints()[endpoint];

        //then
        rule.ShouldBe(AuthorizationPolicies.Administrator);
    }

    private static List<string> EndpointsWithRule(string rule)
    {
        return Endpoints()
            .Where(endpoint => endpoint.Value == rule)
            .Select(endpoint => endpoint.Key)
            .ToList();
    }

    private static Dictionary<string, string> Endpoints()
    {
        return typeof(AuthorizationPolicies).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(action => action.IsDefined(typeof(HttpMethodAttribute), true))
            .ToDictionary(action => $"{action.DeclaringType!.Name}.{action.Name}", RuleOf);
    }

    private static string RuleOf(MethodInfo action)
    {
        var controller = action.DeclaringType!;

        if (action.IsDefined(typeof(AllowAnonymousAttribute), true) ||
            controller.IsDefined(typeof(AllowAnonymousAttribute), true))
        {
            return Anonymous;
        }

        var policies = controller.GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(action.GetCustomAttributes<AuthorizeAttribute>(true))
            .Select(attribute => attribute.Policy ?? AuthorizationPolicies.Administrator)
            .DefaultIfEmpty(AuthorizationPolicies.Administrator)
            .Distinct()
            .Order();

        return string.Join("+", policies);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~AuthorizationPoliciesTest|FullyQualifiedName~ControllerAuthorizationTest"`

Expected: build FAILS with `'AuthorizationPolicies' does not contain a definition for 'TeacherOrAdministrator'` and `... 'Configure'`.

- [ ] **Step 4: Add the policies**

Replace `src\DrivingLessons.Presentation.Web\Auth\AuthorizationPolicies.cs` with:

```csharp
using DrivingLessons.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace DrivingLessons.Presentation.Web.Auth;

public static class AuthorizationPolicies
{
    public const string Administrator = "Administrator";
    public const string TeacherOrAdministrator = "TeacherOrAdministrator";

    public static void Configure(AuthorizationOptions options)
    {
        var administrator = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(AuthClaims.AdministratorRole)
            .Build();
        var teacherOrAdministrator = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(AuthClaims.AdministratorRole, AuthClaims.TeacherRole)
            .Build();

        options.DefaultPolicy = administrator;
        options.FallbackPolicy = administrator;
        options.AddPolicy(Administrator, administrator);
        options.AddPolicy(TeacherOrAdministrator, teacherOrAdministrator);
    }
}
```

In `src\DrivingLessons.Presentation.Web\Program.cs`, replace the whole `builder.Services.AddAuthorization(options => { ... });` block (lines 58-64) with:

```csharp
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
```

Then delete `using Microsoft.AspNetCore.Authorization;` from `Program.cs` if nothing else in the file uses it (`AuthorizationPolicyBuilder` was its only use; `.AllowAnonymous()` on the endpoint builders comes from `Microsoft.AspNetCore.Builder`).

- [ ] **Step 5: Opt the eight actions in**

`WeekScheduleQueryController.cs`: add the usings and a class-level attribute.

```csharp
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.WeekSchedule;

[ApiController]
[Route("api/week-schedules")]
[Tags("Week Schedules")]
[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]
public class WeekScheduleQueryController : ControllerBase
```

(Keep the existing attribute order of the file; only the `[Authorize]` line and the two usings are new.)

`PublicationQueryController.cs`: the same two usings and the same class-level `[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]` after `[Tags("Publications")]`.

`WeekScheduleCommandController.cs`: add the two usings, and put `[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]` on the **actions** `MarkSlotUnavailable` and `MarkSlotAvailable` only (directly above each `[HttpPost(...)]`). `Create` gets **no** attribute: it stays on the Administrator fallback.

`MeCommandController.cs`: add `using DrivingLessons.Presentation.Web.Auth;` and replace the bare `[Authorize]` with `[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]`. A bare `[Authorize]` now means Administrator (decision 1), which would lock Teachers out of changing their own password.

Do **not** touch `PublicationCommandController`, `UserCommandController`, `UserQueryController` or any other controller.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~AuthorizationPoliciesTest|FullyQualifiedName~ControllerAuthorizationTest"`

Expected: PASS (16 test cases: 9 policy, 7 matrix). If `Every_Other_Endpoint_Is_Administrator_Only` fails, its message names the endpoint and its rule; an endpoint showing `Administrator+TeacherOrAdministrator` means a class-level and an action-level attribute stacked.

Then run the whole backend suite:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: all PASS, build without new warnings.

- [ ] **Step 7: Update the API guidelines**

In `.claude\rules\api-guidelines.md`, section `## Auth`, replace the second and fourth bullets (the one starting "Administrator-only controllers carry" and the one starting "Apply `[AllowAnonymous]` explicitly") with:

```markdown
- Two policies live in `Presentation.Web\Auth\AuthorizationPolicies.cs`: `Administrator` (token `role` claim is `administrator`) and `TeacherOrAdministrator` (`administrator` or `teacher`). JWT bearer runs with `MapInboundClaims = false` and `RoleClaimType = "role"`
- `Administrator` is both the fallback policy (no attribute) and the default policy (bare `[Authorize]`): every endpoint is Administrator-only unless it opts in (#89). Opt in with `[Authorize(Policy = AuthorizationPolicies.TeacherOrAdministrator)]` only for screens a Teacher-role User uses: Week Schedule queries, mark Slot Open / Unavailable, Publication queries (including the dashboard and Excel download) and `api/me`
- Never stack a class-level and an action-level policy: ASP.NET requires both. `ControllerAuthorizationTest` pins the endpoint-to-policy matrix; update its lists when an endpoint opts in or out
- Apply `[AllowAnonymous]` explicitly on the student controllers and the login action
```

- [ ] **Step 8: Smoke-test both Roles against the running API**

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us89_smoke"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us89_smoke"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us89_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

From a scratchpad folder (ASCII names here, so no Hebrew-on-the-command-line problem):

```bash
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > admin-login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @admin-login.json | json accessToken)
printf '{"name":"Yael Carmi","contactEmail":"yael@school.example"}' > teacher.json
TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher.json | json id)
node -e "require('fs').writeFileSync('user.json', JSON.stringify({name:'Yael Carmi',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$TEACHER_ID"
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @user.json; echo
printf '{"email":"yael.user@school.example","password":"Temporary#2026"}' > teacher-login.json
TEACHER=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @teacher-login.json | json accessToken)
WEEK=2026-10-11
node -e "require('fs').writeFileSync('week.json', JSON.stringify({teacherId:process.argv[1],weekStart:process.argv[2]}))" "$TEACHER_ID" "$WEEK"
echo "teacher creates week: $(code -X POST $API/api/week-schedules -H "Authorization: Bearer $TEACHER" -H "Content-Type: application/json" -d @week.json)"
echo "admin creates week:   $(code -X POST $API/api/week-schedules -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @week.json)"
SCHEDULE=$(curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_ID&week=$WEEK" -H "Authorization: Bearer $TEACHER")
SCHEDULE_ID=$(echo "$SCHEDULE" | json id)
SLOT_ID=$(echo "$SCHEDULE" | json "slots[0].id")
PUBLICATION_ID=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "Authorization: Bearer $TEACHER" | json id)
echo "teacher marks slot:    $(code -X POST $API/api/week-schedules/$SCHEDULE_ID/slots/$SLOT_ID/mark-unavailable -H "Authorization: Bearer $TEACHER")"
echo "teacher dashboard:     $(code "$API/api/publications/$PUBLICATION_ID/dashboard?teacherId=$TEACHER_ID" -H "Authorization: Bearer $TEACHER")"
echo "teacher history:       $(code $API/api/publications/history -H "Authorization: Bearer $TEACHER")"
printf '{"windowStartUtc":"2026-10-07T16:00:00Z","windowEndUtc":"2026-10-09T11:00:00Z"}' > publish.json
echo "teacher publishes:     $(code -X POST $API/api/publications/$PUBLICATION_ID/publish -H "Authorization: Bearer $TEACHER" -H "Content-Type: application/json" -d @publish.json)"
echo "teacher teachers/find: $(code $API/api/teachers/find -H "Authorization: Bearer $TEACHER")"
echo "teacher students/find: $(code $API/api/students/find -H "Authorization: Bearer $TEACHER")"
echo "teacher roster latest: $(code $API/api/roster-imports/latest -H "Authorization: Bearer $TEACHER")"
echo "teacher users/find:    $(code $API/api/users/find -H "Authorization: Bearer $TEACHER")"
printf '{"currentPassword":"Wrong#2026","newPassword":"Yael#2027"}' > my-password.json
echo "teacher my password:   $(code -X PUT $API/api/me/password -H "Authorization: Bearer $TEACHER" -H "Content-Type: application/json" -d @my-password.json)"
echo "admin teachers/find:   $(code $API/api/teachers/find -H "Authorization: Bearer $ADMIN")"
echo "admin users/find:      $(code $API/api/users/find -H "Authorization: Bearer $ADMIN")"
echo "anonymous history:     $(code $API/api/publications/history)"
```

Expected:

| Line | Code |
|------|------|
| teacher creates week | `403` |
| admin creates week | `201` (or `200`, whatever `Create` returns today) |
| teacher marks slot | `204` (or `200`) |
| teacher dashboard | `200` |
| teacher history | `200` |
| teacher publishes | `403` (the body shape doesn't matter: authorization runs before model binding) |
| teacher teachers/find, students/find, roster latest, users/find | `403` each |
| teacher my password | `409` (wrong current password: the Teacher passed authorization) |
| admin teachers/find, users/find | `200` |
| anonymous history | `401` |

Stop the API (`TaskStop` on the background task). Leave the smoke database; task 7 uses its own.

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Auth/AuthorizationPolicies.cs src/DrivingLessons.Presentation.Web/Program.cs src/DrivingLessons.Presentation.Web/Controllers tests/DrivingLessons.Application.Test/Auth/AuthorizationPoliciesTest.cs tests/DrivingLessons.Application.Test/Auth/ControllerAuthorizationTest.cs .claude/rules/api-guidelines.md
git commit -m "feat(auth): Administrator is the default policy; Teachers reach only Week Schedules, Publications and their password (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
