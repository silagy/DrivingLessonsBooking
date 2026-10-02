# Task 1 of 8: Every 404/409 carries its rule as a machine-readable `code` (L1, server half, TDD)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). This task creates branch `47-us-47-48-language-and-rtl`.

**Files:**
- Modify: `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` (whole file below)
- Modify: `tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj` (one project reference)
- Create: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`
- Commit first (Step 1): `docs\modules\i18n\README.md` and `docs\modules\i18n\us-47-48-language-and-rtl-plan\*.md` (this plan)

**Interfaces:**
- Consumes: the existing `ApiExceptionFilter` mapping (401 `AuthenticationFailedException`, 404 `NotFoundException`, 409 `SubmissionWindowMustBeOpenException` with `type = ProblemTypes.SubmissionWindowClosed`, 409 `DomainException`), `ProblemTypes.SubmissionWindowClosed = "problems/submission-window-closed"`.
- Produces: every 404 and 409 `ProblemDetails` body carries a top-level `"code"` string = the exception's type name without the `Exception` suffix, camelCased (`CarNameMustNotBeEmptyException` → `"carNameMustNotBeEmpty"`, `TeacherNotFoundException` → `"teacherNotFound"`). The 401 body has no `code`. `type`, `title`, `status` and `detail` are unchanged. Task 2 reads `code` (`ProblemDetails.code` in the client) and maps it to `errors.{code}`.

**Why:** README defect L1 and decisions 2–4. The client cannot translate a free-text English sentence, but it can translate a stable rule name. The rule name is already in every exception's class name (`{Entity}{Rule}Exception`, CLAUDE.md file-location table), so the one filter derives it and no exception changes.

- [ ] **Step 1: Branch and commit the plan**

From the repo root, on an up-to-date `main` with a clean tree:

```bash
git checkout main
git pull --ff-only
git checkout -b 47-us-47-48-language-and-rtl
git add docs/modules/i18n/README.md docs/modules/i18n/us-47-48-language-and-rtl-plan
git commit -m "docs: US-47 and US-48 language toggle and RTL plan

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 2: Let the Application test project see the filter**

In `tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`, change

```xml
    <ProjectReference Include="..\..\src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj" />
  </ItemGroup>
```

to

```xml
    <ProjectReference Include="..\..\src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\DrivingLessons.Presentation.Web\DrivingLessons.Presentation.Web.csproj" />
  </ItemGroup>
```

The Web project's ASP.NET Core framework reference flows to the test project, so `DefaultHttpContext`, `ActionContext` and `ExceptionContext` resolve without a new package.

- [ ] **Step 3: Write the failing tests**

Create `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using DrivingLessons.Presentation.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shouldly;

namespace DrivingLessons.Application.Test.Filters;

[TestClass]
public class ApiExceptionFilterTest
{
    [TestMethod]
    public void Rule_Violation_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new CarNameMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "carNameMustNotBeEmpty");
    }

    [TestMethod]
    public void Missing_Entity_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherNotFoundException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherNotFound");
    }

    [TestMethod]
    public void Closed_Window_Keeps_Its_Problem_Type_And_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new SubmissionWindowMustBeOpenException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Type.ShouldBe(ProblemTypes.SubmissionWindowClosed);
        problem.Extensions.ShouldContainKeyAndValue("code", "submissionWindowMustBeOpen");
    }

    [TestMethod]
    public void Rule_Violation_Keeps_Its_Detail_For_Developers()
    {
        //given
        var context = ContextFor(new CarNameMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        ProblemOf(context).Detail.ShouldBe("Car name must not be empty.");
    }

    [TestMethod]
    public void Failed_Sign_In_Carries_No_Code()
    {
        //given
        var context = ContextFor(new AuthenticationFailedException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status401Unauthorized);
        problem.Extensions.ShouldNotContainKey("code");
    }

    private static ExceptionContext ContextFor(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        return new ExceptionContext(actionContext, []) { Exception = exception };
    }

    private static ProblemDetails ProblemOf(ExceptionContext context)
    {
        var result = context.Result.ShouldBeOfType<ObjectResult>();

        return result.Value.ShouldBeOfType<ProblemDetails>();
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest"`
Expected: the build succeeds. Exactly three tests FAIL, `Rule_Violation_Carries_Its_Rule_As_Code`, `Missing_Entity_Carries_Its_Rule_As_Code` and `Closed_Window_Keeps_Its_Problem_Type_And_Carries_Its_Rule_As_Code`, each because the dictionary does not contain the key `"code"`. `Rule_Violation_Keeps_Its_Detail_For_Developers` and `Failed_Sign_In_Carries_No_Code` PASS (they pin behaviour that must not change). If the build fails on the new project reference, stop and report the error. Do not add packages.

- [ ] **Step 5: Add the code in the filter**

Replace the whole of `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` with:

```csharp
using System.Text.Json;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private const string CodeExtension = "code";

    public void OnException(ExceptionContext context)
    {
        var code = CodeOf(context.Exception);
        (int StatusCode, string Title, string? Type, string? Code)? mapping = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", null, null),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", null, code),
            SubmissionWindowMustBeOpenException =>
                (StatusCodes.Status409Conflict, "Conflict", ProblemTypes.SubmissionWindowClosed, code),
            DomainException => (StatusCodes.Status409Conflict, "Conflict", null, code),
            _ => null
        };

        if (mapping is null)
        {
            return;
        }

        var (statusCode, title, type, problemCode) = mapping.Value;
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        };

        if (problemCode is not null)
        {
            problemDetails.Extensions[CodeExtension] = problemCode;
        }

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }

    private static string CodeOf(Exception exception)
    {
        var typeName = exception.GetType().Name;
        var rule = typeName.EndsWith(nameof(Exception), StringComparison.Ordinal)
            ? typeName[..^nameof(Exception).Length]
            : typeName;

        return JsonNamingPolicy.CamelCase.ConvertName(rule);
    }
}
```

`JsonNamingPolicy.CamelCase` is the policy the API already uses for enums (`api-guidelines.md` "Serialization"), so codes follow the same casing as every other JSON value. `ProblemDetails.Extensions` serializes as top-level properties, so the body reads `{ "type": …, "title": "Conflict", "status": 409, "detail": "…", "code": "carNameMustNotBeEmpty" }`.

- [ ] **Step 6: Run the tests, then the whole backend**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ApiExceptionFilterTest"`
Expected: PASS, all five.

Run (repo root): `dotnet build` then `dotnet test`
Expected: the build is clean apart from the existing `NU1903` / `MSTEST0001` / `CS8618` warnings, and every test in both test projects PASSES (the Application suite has five more tests than before).

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs
git commit -m "feat(api): name the broken rule in every 404 and 409 problem

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
