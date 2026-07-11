# Task 7 of 12: Controllers + enum serialization

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires tasks 1–6 complete. Work on branch `3-us-02-04-teachers-module`, commands from the repo root.

## Shared Context

**Goal:** CQRS controller pair for the Teacher aggregate plus camelCase string-enum JSON. Controllers are doors: `[FromServices]` per-action injection, no constructors, no fields, no logic. Errors flow through the existing `ApiExceptionFilter`.

**Locked decision:** use the native `[EndpointSummary]` attribute (Microsoft.AspNetCore.OpenApi + Scalar), **not** `[SwaggerOperation]` — the api-guidelines doc predates the AddOpenApi choice.

---

**Files:**
- Create: `src/DrivingLessons.Presentation.Web/Controllers/Teacher/TeacherCommandController.cs`
- Create: `src/DrivingLessons.Presentation.Web/Controllers/Teacher/TeacherQueryController.cs`
- Modify: `src/DrivingLessons.Presentation.Web/Program.cs` (JSON enum converter)

- [ ] **Step 1: `Controllers/Teacher/TeacherCommandController.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.AddCar;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.ChangeTeacherDetails;
using DrivingLessons.Application.Commands.CreateTeacher;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Teacher;

[ApiController]
[Route("api/teachers")]
[Tags("Teachers")]
public class TeacherCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a teacher")]
    [ProducesResponseType(typeof(CreateTeacherResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTeacherResponse>> CreateAsync(
        [FromServices] CreateTeacherInteractor interactor,
        [FromBody] [Required] CreateTeacherRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPost("{id:guid}/cars")]
    [EndpointSummary("Add a car to the teacher")]
    [ProducesResponseType(typeof(AddCarResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddCarResponse>> AddCarAsync(
        [FromServices] AddCarInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] AddCarRequest request)
    {
        var result = await interactor.ExecuteAsync(id, request);

        return CreatedAtAction(null, result);
    }

    [HttpPut("{id:guid}/details")]
    [EndpointSummary("Change the teacher's name and contact email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDetailsAsync(
        [FromServices] ChangeTeacherDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ChangeTeacherDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, request);

        return NoContent();
    }

    [HttpPut("{id:guid}/cars/{carId:guid}")]
    [EndpointSummary("Change a car's name, type, and transmission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeCarDetailsAsync(
        [FromServices] ChangeCarDetailsInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid carId,
        [FromBody] [Required] ChangeCarDetailsRequest request)
    {
        await interactor.ExecuteAsync(id, carId, request);

        return NoContent();
    }
}
```

- [ ] **Step 2: `Controllers/Teacher/TeacherQueryController.cs`**

```csharp
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Teacher;

[ApiController]
[Route("api/teachers")]
[Tags("Teachers")]
public class TeacherQueryController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a teacher with their cars")]
    [ProducesResponseType(typeof(GetTeacherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetTeacherResponse> GetAsync(
        [FromServices] GetTeacherInteractor interactor,
        [FromRoute] Guid id)
    {
        return await interactor.ExecuteAsync(id);
    }

    [HttpGet("find")]
    [EndpointSummary("Find all teachers with their cars")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindTeachersResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync(
        [FromServices] FindTeachersInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
```

Note: the controller class name collides with the `Teacher` entity only via namespace — the `DrivingLessons.Presentation.Web.Controllers.Teacher` namespace never imports `DrivingLessons.Domain.Entities`, so no conflict arises. If an analyzer complains, do not rename the namespace; alias the entity where needed.

- [ ] **Step 3: Enum serialization — `src/DrivingLessons.Presentation.Web/Program.cs`**

Add `using System.Text.Json;` and `using System.Text.Json.Serialization;` to the top, then extend the existing `AddControllers` call:

```csharp
builder.Services
    .AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
```

- [ ] **Step 4: Verify via Scalar**

Start Postgres (per `docs/development/running-the-project.md`), then run the API in Development and open `/scalar/v1`:

```
dotnet run --project src/DrivingLessons.Presentation.Web
```

Walkthrough (authorize with a bearer token from `POST api/auth/login` first):

1. `GET api/teachers/find` without a token → **401**.
2. `POST api/teachers` `{ "name": "Teacher Cohen", "contactEmail": "avi.cohen@example.com" }` → **201** with an id.
3. `POST api/teachers/{id}/cars` `{ "name": "Corolla White", "type": "Sedan", "transmission": "automatic" }` → **201**; repeat twice more → **201** each (no maximum).
4. `PUT api/teachers/{id}/details` with a new email → **204**; `GET api/teachers/{id}` shows it.
5. `PUT api/teachers/{id}/cars/{carId}` switching `"transmission": "manual"` → **204**.
6. `PUT api/teachers/{id}/cars/{carId}` with a `carId` belonging to another teacher → **404**.
7. `POST api/teachers` with `"contactEmail": "not-an-email"` → **409** (EmailMustBeValid).
8. `GET api/teachers/find` → **200**, cars nested, transmission rendered as `"automatic"`/`"manual"`.

- [ ] **Step 5: Commit**

```bash
git add src
git commit -m "feat(api): teacher command and query controllers with camelcase enum json"
```

---

**Next:** [task-08-client-plumbing.md](task-08-client-plumbing.md)
