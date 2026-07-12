# Task 5 of 10: Controllers + API smoke test

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–4 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\WeekSchedule\WeekScheduleCommandController.cs`, `WeekScheduleQueryController.cs`

Mirror `TeacherCommandController.cs` / `TeacherQueryController.cs` exactly: `[FromServices]` on action parameters, `[EndpointSummary]`, `[ProducesResponseType]`, `CreatedAtAction(null, result)` for creates, `NoContent()` for state changes. Business actions are POST sub-resources; the alternate-key lookup follows the `by-*` convention from api-guidelines.

- [ ] **Step 1: Command controller**

`Controllers\WeekSchedule\WeekScheduleCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateWeekSchedule;
using DrivingLessons.Application.Commands.MarkSlotUnavailable;
using DrivingLessons.Application.Commands.MarkSlotAvailable;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.WeekSchedule;

[ApiController]
[Route("api/week-schedules")]
[Tags("Week Schedules")]
public class WeekScheduleCommandController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Creates a week schedule for a teacher and week with all slots open")]
    [ProducesResponseType(typeof(CreateWeekScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromServices] CreateWeekScheduleInteractor interactor,
        [FromBody] [Required] CreateWeekScheduleRequest request)
    {
        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }

    [HttpPost("{id:guid}/slots/{slotId:guid}/mark-unavailable")]
    [EndpointSummary("Marks an open slot unavailable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkSlotUnavailable(
        [FromServices] MarkSlotUnavailableInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid slotId)
    {
        await interactor.ExecuteAsync(id, slotId);

        return NoContent();
    }

    [HttpPost("{id:guid}/slots/{slotId:guid}/mark-available")]
    [EndpointSummary("Marks an unavailable slot as available")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkSlotAvailable(
        [FromServices] MarkSlotAvailableInteractor interactor,
        [FromRoute] Guid id,
        [FromRoute] Guid slotId)
    {
        await interactor.ExecuteAsync(id, slotId);

        return NoContent();
    }
}
```

- [ ] **Step 2: Query controller**

`Controllers\WeekSchedule\WeekScheduleQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.GetWeekSchedule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.WeekSchedule;

[ApiController]
[Route("api/week-schedules")]
[Tags("Week Schedules")]
public class WeekScheduleQueryController : ControllerBase
{
    [HttpGet("by-teacher-and-week")]
    [EndpointSummary("Gets the week schedule for a teacher and week")]
    [ProducesResponseType(typeof(GetWeekScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetWeekScheduleResponse> GetByTeacherAndWeek(
        [FromServices] GetWeekScheduleInteractor interactor,
        [FromQuery] Guid teacherId,
        [FromQuery] DateOnly week)
    {
        return await interactor.ExecuteAsync(teacherId, week);
    }
}
```

- [ ] **Step 3: Build and smoke-test the API**

Run: `dotnet build` — success.

Start the app (Postgres via Docker Compose must be up — same as teachers verification; migrations auto-apply). Then, with a bearer token from `POST /api/auth/login`, verify with curl (or Scalar UI in dev):

1. `GET /api/week-schedules/by-teacher-and-week?teacherId={existing}&week=2026-07-19` → **404**
2. `POST /api/week-schedules` body `{"teacherId":"{existing}","weekStart":"2026-07-19"}` → **201** `{ "id": "..." }`
3. Repeat the GET → **200**, 22 slots, all `"state":"open"`, Friday has only `morning`+`noon`, days `sunday..friday`, each slot has `startLocal`/`endLocal`
4. `POST /api/week-schedules/{id}/slots/{slotId}/mark-unavailable` → **204**; repeat → **409** ProblemDetails "must be open"
5. `POST /api/week-schedules/{id}/slots/{slotId}/mark-available` → **204**; repeat → **409**
6. `POST /api/week-schedules` same body again → **409** "already exists"
7. `POST /api/week-schedules` body with `"weekStart":"2026-07-20"` (a Monday) → **409** "must be a Sunday"

- [ ] **Step 4: Commit**

```bash
git add src/DrivingLessons.Presentation.Web
git commit -m "feat(api): week schedule command and query endpoints"
```

---

**Next:** [task-06-client-domain-data.md](task-06-client-domain-data.md)
