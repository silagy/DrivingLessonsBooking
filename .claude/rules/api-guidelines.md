---
paths:
  - "**/Presentation.Web/**/*.cs"
---

# API Guidelines

Controllers are delivery mechanisms — **a door, nothing more**. They accept the HTTP request, delegate to an interactor, and return its result. Zero business logic, zero data access, zero mapping logic beyond passing parameters through.

## Structure

```
DrivingLessons.Presentation.Web\
├── Controllers\
│   └── {Entity}\
│       ├── {Entity}CommandController.cs
│       └── {Entity}QueryController.cs
├── Filters\
│   └── ApiExceptionFilter.cs
├── ServiceRegistration\
│   └── ServiceCollectionExtension.cs
└── Program.cs
```

## CQRS Controller Split

- Commands go in `{Entity}CommandController` — `[HttpPost]` / `[HttpPut]` / `[HttpDelete]`
- Queries go in `{Entity}QueryController` — `[HttpGet]` exclusively
- **Never mix commands and queries in the same controller**
- Both controllers for an entity share the same `[Route("...")]` base path

## Controller Rules

- Every controller: `[ApiController]`, `[Route("...")]`, `[Tags("...")]`
- Route paths are **kebab-case plural resources**: `"publications"`, `"week-schedules"`
- Interactors injected via `[FromServices]` **on the action method parameter** — controllers have no constructor and no fields
- All action methods are async and end with `Async`
- Route ids use type constraints and validation: `[FromRoute] Guid id` with `{id:guid}` in the template
- Request bodies: `[FromBody] [Required]`
- Business actions are **POST sub-resources on the entity**: `POST publications/{id}/publish`, `POST publications/{id}/close` — never `PUT` with a state field

### Command Controller Template

```csharp
[ApiController]
[Route("publications")]
[Tags("Publications")]
public class PublicationCommandController : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Create a publication for a week schedule")]
    [ProducesResponseType(typeof(CreatePublicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatePublicationResponse>> CreateAsync(
        [FromServices] CreatePublicationInteractor interactor,
        [FromBody] [Required] CreatePublicationRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPost("{id:guid}/publish")]
    [SwaggerOperation(Summary = "Publish the publication and generate its shareable link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishAsync(
        [FromServices] PublishPublicationInteractor interactor,
        [FromRoute] Guid id)
    {
        await interactor.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    [SwaggerOperation(Summary = "Reopen a closed publication with an extended window")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReopenAsync(
        [FromServices] ReopenPublicationInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ReopenPublicationRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }
}
```

### Query Controller Template

```csharp
[ApiController]
[Route("publications")]
[Tags("Publications")]
public class PublicationQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a publication")]
    [ProducesResponseType(typeof(GetPublicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetPublicationResponse> GetAsync(
        [FromServices] GetPublicationInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [SwaggerOperation(Summary = "Find publications by teacher")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindPublicationsResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindPublicationsResponse>> FindAsync(
        [FromServices] FindPublicationsInteractor interactor,
        [FromQuery] FindPublicationsRequest request)
    {
        return await interactor.ExecuteAsync(request);
    }
}
```

## Endpoint Shapes

| Operation | Route | Verb | Returns |
|-----------|-------|------|---------|
| Create | `POST {entities}` | POST | 201 + response body |
| Get by id | `GET {entities}/{id:guid}` | GET | 200 + response body |
| Find/search | `GET {entities}/find` with `[FromQuery]` filter object | GET | 200 + collection |
| Business action | `POST {entities}/{id:guid}/{action}` | POST | 204 (or 200 with body) |
| Update fields | `PUT {entities}/{id:guid}/{field-group}` | PUT | 204 |
| Delete | `DELETE {entities}/{id:guid}` | DELETE | 204 |

Anonymous student-facing endpoints are scoped by link token, not entity id: `GET submissions/by-link/{token}` — tokens are unguessable and act as the capability.

## Status Codes and Error Mapping

A single global exception filter maps exception families to `ProblemDetails` — controllers and interactors never set status codes for errors:

| Exception family | Status |
|------------------|--------|
| `AuthenticationFailedException` (login failure — deliberately detail-free) | 401 Unauthorized |
| `NotFoundException` (base of all `{Entity}NotFoundException`, in `Application\Common\Exceptions`) | 404 Not Found |
| `DomainException` (invalid state transition, broken business rule) | 409 Conflict |
| Validation attribute failures (`[Required]`, model binding) | 400 Bad Request |
| Everything else | 500 Internal Server Error |

```csharp
public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            DomainException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = context.Exception.Message
        };

        context.Result = new ObjectResult(problem)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
```

Register once in `AddControllers(options => options.Filters.Add<ApiExceptionFilter>())`.

## Auth Slice Exception (US-01)

The admin sign-in slice (`docs/modules/auth/us-01-admin-sign-in-plan.md`) is deliberately **pre-aggregate infrastructure** — no domain aggregate, and its code style deviates from these rules in places (constructor injection, `Handle(...)` naming). That deviation is accepted **for that slice only**. Every aggregate endpoint (Teacher, WeekSchedule, Publication, Student, Submission) follows these rules: `ExecuteAsync`, `[FromServices]` injection, CQRS controller split, no comments.

## Swagger

- `[SwaggerOperation(Summary = "...")]` on every action
- `[ProducesResponseType(...)]` for every status the action can return
- `[Tags("...")]` in Title Case per entity
- Response DTO names are globally unique (see naming table in ddd-architecture.md) — never two classes named `StatusResponse`

## Serialization

- `System.Text.Json` (default for new .NET apps)
- Enums serialized as camelCase strings: `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`
- DateTimes are UTC instants in ISO 8601; the client converts to Asia/Jerusalem for display

## DI Registration

One `ServiceCollectionExtension` per concern, explicit registration (the app is small — no assembly scanning needed):

```csharp
public static class ServiceCollectionExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreatePublicationInteractor>();
        services.AddScoped<PublishPublicationInteractor>();
        services.AddScoped<ClosePublicationInteractor>();
        services.AddScoped<GetPublicationInteractor>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DrivingLessons");

        services.AddDbContext<DrivingLessonsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<IPublicationQueries, PublicationQueries>();

        return services;
    }
}
```

Interactors are registered as themselves (scoped) — no interfaces for interactors.

## Auth

- Admin endpoints require the authenticated admin policy (email + password login per requirements)
- Student-facing endpoints (`by-link/{token}`) are anonymous by design — the unguessable token is the access control
- Apply `[AllowAnonymous]` explicitly on the student controllers; everything else requires authorization by default (`MapControllers().RequireAuthorization()`)

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Route path | kebab-case plural | `"week-schedules"` |
| Controller class | `{Entity}{Command\|Query}Controller` | `PublicationCommandController` |
| Swagger tag | Title Case | `"Week Schedules"` |
| Action method | PascalCase + `Async` | `PublishAsync` |
| Request DTO | `{Op}{Entity}Request` | `CreatePublicationRequest` |
| Response DTO | `{Op}{Entity}Response` | `GetPublicationResponse` |

## Anti-Patterns

- **Never** inject interactors via constructor — always `[FromServices]` on the method parameter
- **Never** mix command and query actions in one controller
- **Never** put try/catch or status-code mapping in controllers — the exception filter owns it
- **Never** put business rules, lookups, or LINQ in controllers
- **Never** model state changes as `PUT` with a `state` property — use action sub-resources
- **Never** expose sequential ids in student-facing links — link tokens only
- **Never** return domain entities from an endpoint — responses are always DTOs
