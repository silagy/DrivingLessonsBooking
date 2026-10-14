# Task 1 of 8: Backend — `/api/health` and per-IP rate limits

> Part of [#74: Production Deployment](README.md). Work on branch `74-production-deployment`, commands from the repo root in bash.

**Why:** Decisions 7 and 8. The deploy workflow needs a URL that proves the new version started *and* reaches its database. And once the app is public, the two anonymous doors that check a secret — admin sign-in (a password) and student identify (a national ID behind the weekly link) — must not accept unlimited guesses from one IP.

**Files:**
- Create: `src/DrivingLessons.Presentation.Web/RateLimiting/RateLimitPolicies.cs`
- Create: `src/DrivingLessons.Presentation.Web/RateLimiting/RateLimitingExtension.cs`
- Create: `tests/DrivingLessons.Application.Test/RateLimiting/RateLimitingExtensionTest.cs`
- Modify: `src/DrivingLessons.Presentation.Web/Program.cs`
- Modify: `src/DrivingLessons.Presentation.Web/Controllers/AuthController.cs`
- Modify: `src/DrivingLessons.Presentation.Web/Controllers/Submission/SubmissionCommandController.cs`
- Modify: `src/DrivingLessons.Infrastructure/DrivingLessons.Infrastructure.csproj`
- Modify: `src/DrivingLessons.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: (on `main`) `DrivingLessonsDbContext`, `AddInfrastructure`, `AuthController.Login` (`POST api/auth/login`), `SubmissionCommandController.IdentifyAsync` (`POST api/submissions/by-link/{token}/identify`).
- Produces (later tasks rely on these exact values):
  - `GET /api/health` → `200` body `Healthy` / `503` body `Unhealthy`, anonymous. Used by task 7's deploy gate and task 8.
  - HTTP `429` with header `Retry-After: <seconds>` and JSON body `{"title":"Too Many Requests","status":429,"code":"tooManyRequests"}` (content type `application/problem+json`). Task 2's client keys off the status `429`.
  - `RateLimitPolicies.AdminSignIn = "admin-sign-in"`, `RateLimitPolicies.StudentIdentify = "student-identify"`; `RateLimitingExtension.AdminSignInPermitsPerMinute = 5`, `StudentIdentifyPermitsPerMinute = 30`.

The `DrivingLessons.Application.Test` project already references `Presentation.Web` (see `Filters/ApiExceptionFilterTest.cs`) and `Microsoft.Extensions.DependencyInjection`, so the new test needs no project change.

- [ ] **Step 1: Write the failing tests**

Create `tests/DrivingLessons.Application.Test/RateLimiting/RateLimitingExtensionTest.cs`:

```csharp
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.RateLimiting;

[TestClass]
public class RateLimitingExtensionTest
{
    [TestMethod]
    public async Task Rejection_Is_A_Too_Many_Requests_Problem_With_A_Code()
    {
        //given
        var context = RejectedContext(new RejectedLease(null));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        var response = context.HttpContext.Response;
        response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        response.ContentType.ShouldBe("application/problem+json");
        var body = await BodyOf(response);
        body.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status429TooManyRequests);
        body.GetProperty("code").GetString().ShouldBe("tooManyRequests");
    }

    [TestMethod]
    public async Task Rejection_Tells_The_Client_When_To_Retry()
    {
        //given
        var context = RejectedContext(new RejectedLease(TimeSpan.FromSeconds(41.2)));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        context.HttpContext.Response.Headers.RetryAfter.ToString().ShouldBe("42");
    }

    [TestMethod]
    public async Task Rejection_Without_A_Known_Wait_Sends_No_Retry_After()
    {
        //given
        var context = RejectedContext(new RejectedLease(null));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        context.HttpContext.Response.Headers.ShouldNotContainKey("Retry-After");
    }

    [TestMethod]
    public void Each_Client_Address_Has_Its_Own_Limit()
    {
        //given
        var first = ContextFrom(IPAddress.Parse("203.0.113.10"));
        var second = ContextFrom(IPAddress.Parse("203.0.113.11"));

        //when
        var firstKey = RateLimitingExtension.PartitionKeyOf(first);
        var secondKey = RateLimitingExtension.PartitionKeyOf(second);

        //then
        firstKey.ShouldBe("203.0.113.10");
        secondKey.ShouldNotBe(firstKey);
    }

    [TestMethod]
    public void Clients_Without_An_Address_Share_One_Limit()
    {
        //given
        var context = ContextFrom(null);

        //when
        var key = RateLimitingExtension.PartitionKeyOf(context);

        //then
        key.ShouldBe("unknown");
    }

    private static OnRejectedContext RejectedContext(RateLimitLease lease)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        return new OnRejectedContext { HttpContext = httpContext, Lease = lease };
    }

    private static DefaultHttpContext ContextFrom(IPAddress? address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address;

        return context;
    }

    private static async Task<JsonElement> BodyOf(HttpResponse response)
    {
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);

        return document.RootElement.Clone();
    }

    private sealed class RejectedLease(TimeSpan? retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames =>
            retryAfter is null
                ? []
                : [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            var isRetryAfter = metadataName == MetadataName.RetryAfter.Name;
            metadata = isRetryAfter
                ? retryAfter
                : null;

            return isRetryAfter && retryAfter is not null;
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/DrivingLessons.Application.Test --filter "FullyQualifiedName~RateLimitingExtensionTest"
```

Expected: build FAIL — `The type or namespace name 'RateLimiting' does not exist in the namespace 'DrivingLessons.Presentation.Web'`.

- [ ] **Step 3: The policy names**

Create `src/DrivingLessons.Presentation.Web/RateLimiting/RateLimitPolicies.cs`:

```csharp
namespace DrivingLessons.Presentation.Web.RateLimiting;

public static class RateLimitPolicies
{
    public const string AdminSignIn = "admin-sign-in";
    public const string StudentIdentify = "student-identify";
}
```

- [ ] **Step 4: The limiter and its rejection response**

Create `src/DrivingLessons.Presentation.Web/RateLimiting/RateLimitingExtension.cs`:

```csharp
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrivingLessons.Presentation.Web.RateLimiting;

public static class RateLimitingExtension
{
    public const int AdminSignInPermitsPerMinute = 5;
    public const int StudentIdentifyPermitsPerMinute = 30;
    public const string TooManyRequestsCode = "tooManyRequests";

    private const string CodeExtension = "code";
    private const string ProblemContentType = "application/problem+json";
    private const string TooManyRequestsTitle = "Too Many Requests";
    private const string UnknownClient = "unknown";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(
                RateLimitPolicies.AdminSignIn,
                context => PerClientFixedWindow(context, AdminSignInPermitsPerMinute));
            options.AddPolicy(
                RateLimitPolicies.StudentIdentify,
                context => PerClientFixedWindow(context, StudentIdentifyPermitsPerMinute));
        });
    }

    public static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = Math.Ceiling(retryAfter.TotalSeconds);
            response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = TooManyRequestsTitle
        };
        problem.Extensions[CodeExtension] = TooManyRequestsCode;

        await response.WriteAsJsonAsync(problem, options: null, ProblemContentType, cancellationToken);
    }

    public static string PartitionKeyOf(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;

        return address?.ToString() ?? UnknownClient;
    }

    private static RateLimitPartition<string> PerClientFixedWindow(HttpContext context, int permitsPerMinute)
    {
        var clientKey = PartitionKeyOf(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = Window,
                QueueLimit = 0
            });
    }
}
```

(`Microsoft.AspNetCore.RateLimiting` and `System.Threading.RateLimiting` ship with the ASP.NET Core shared framework — no package.)

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/DrivingLessons.Application.Test --filter "FullyQualifiedName~RateLimitingExtensionTest"
```

Expected: 5 passed.

- [ ] **Step 6: Apply the policies to the two endpoints**

In `src/DrivingLessons.Presentation.Web/Controllers/AuthController.cs`, add the using and the attribute:

```csharp
using DrivingLessons.Application.Auth;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrivingLessons.Presentation.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginInteractor login) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AdminSignIn)]
    public Task<LoginResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) =>
        login.Handle(command, cancellationToken);
}
```

In `src/DrivingLessons.Presentation.Web/Controllers/Submission/SubmissionCommandController.cs`, add `using DrivingLessons.Presentation.Web.RateLimiting;` and `using Microsoft.AspNetCore.RateLimiting;` to the usings (alphabetical, as in the file), then put the attribute and a `429` response type on `IdentifyAsync` only:

```csharp
    [HttpPost("by-link/{token}/identify")]
    [EnableRateLimiting(RateLimitPolicies.StudentIdentify)]
    [EndpointSummary("Identifies a roster student by national ID and returns their teacher, car and the teacher's week grid")]
    [ProducesResponseType(typeof(IdentifyStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IdentifyStudentResponse> IdentifyAsync(
```

`Login` gets no `ProducesResponseType`: the auth slice has none at all (its accepted deviation, api-guidelines "pre-aggregate infrastructure" note).

- [ ] **Step 7: The health check, and wiring in `Program.cs`**

Add the package to Infrastructure:

```bash
dotnet add src/DrivingLessons.Infrastructure package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore --version 10.0.9
```

In `src/DrivingLessons.Infrastructure/DependencyInjection.cs`, directly after the `services.AddScoped<IUnitOfWork>(...)` line, add:

```csharp
        services.AddHealthChecks()
                .AddDbContextCheck<DrivingLessonsDbContext>();
```

In `src/DrivingLessons.Presentation.Web/Program.cs`:

1. Add `using DrivingLessons.Presentation.Web.RateLimiting;` to the usings (alphabetical, after `using DrivingLessons.Presentation.Web.OpenApi;`).
2. After `builder.Services.AddInfrastructure(builder.Configuration);` add:

```csharp
builder.Services.AddApiRateLimiting();
```

3. Replace the block from `app.UseDefaultFiles();` to `app.MapFallbackToFile(...)` with:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapControllers();

app.MapFallbackToFile("index.html").AllowAnonymous();
```

(`WebApplication` adds routing at the start of the pipeline, so `UseRateLimiter` sees each endpoint's `[EnableRateLimiting]` metadata. There is no global limiter, so `/api/health`, static files and every other endpoint are never limited.)

- [ ] **Step 8: Build, full test run, and a live check of the limits**

```bash
dotnet build DrivingLessons.sln
dotnet test DrivingLessons.sln
```

Expected: build succeeds with no new warnings; every test passes.

Start the API: make sure the compose Postgres is up (`docker stop dl-postgres 2>/dev/null; docker compose up -d postgres`), then `preview_start {name:"api"}` and wait for `Now listening on: http://localhost:5080` in `preview_logs`.

```bash
API=http://localhost:5080
curl -s -w "  <- %{http_code}\n" $API/api/health
for i in 1 2 3 4 5 6; do
  curl -s -o /dev/null -w "login $i: %{http_code}\n" -X POST $API/api/auth/login \
    -H "Content-Type: application/json" -d '{"email":"admin@local.dev","password":"wrong-password"}'
done
curl -s -i -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"wrong-password"}' | grep -iE "^HTTP|^retry-after|^content-type|tooManyRequests"
for i in $(seq 1 31); do
  curl -s -o /dev/null -w "%{http_code} " -X POST $API/api/submissions/by-link/no-such-token/identify \
    -H "Content-Type: application/json" -d '{"nationalId":"000000018"}'
done; echo
curl -s -o /dev/null -w "health after limits: %{http_code}\n" $API/api/health
```

Expected:
- `Healthy  <- 200`
- `login 1`…`login 5`: `401`; `login 6`: `429`
- the `-i` call: `HTTP/1.1 429 Too Many Requests`, a `Retry-After:` header with a number ≤ 60, `Content-Type: application/problem+json`, and a body containing `"code":"tooManyRequests"`
- identify: thirty non-`429` answers (`404` — the token is unknown; the limiter runs before the interactor either way) then `429` on the 31st request
- `health after limits: 200` (health is never limited)

- [ ] **Step 9: Health reports a dead database as unhealthy**

```bash
docker compose stop postgres
curl -s -w "  <- %{http_code}\n" http://localhost:5080/api/health
docker compose start postgres
sleep 5
curl -s -w "  <- %{http_code}\n" http://localhost:5080/api/health
```

Expected: `Unhealthy  <- 503`, then `Healthy  <- 200` once Postgres is back. `preview_stop` the API.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/RateLimiting \
        src/DrivingLessons.Presentation.Web/Program.cs \
        src/DrivingLessons.Presentation.Web/Controllers/AuthController.cs \
        src/DrivingLessons.Presentation.Web/Controllers/Submission/SubmissionCommandController.cs \
        src/DrivingLessons.Infrastructure/DrivingLessons.Infrastructure.csproj \
        src/DrivingLessons.Infrastructure/DependencyInjection.cs \
        tests/DrivingLessons.Application.Test/RateLimiting
git commit -m "feat(ops): health endpoint with a database check and per-IP limits on sign-in and identify

- GET /api/health (anonymous) answers 200 Healthy or 503 Unhealthy
- admin sign-in: 5 per minute per IP; student identify: 30 per minute per IP
- a rejection is 429 with Retry-After and a problem body coded tooManyRequests

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
