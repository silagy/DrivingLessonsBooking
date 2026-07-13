# Task 9 of 14: Controllers + Scalar smoke test

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–8 complete (domain, dispatch, application commands + queries, infrastructure persistence + services + scheduling). Work on branch `9-us-08-21-publications-module`, commands from the repo root.

The two controllers are thin HTTP adapters over interactors that already exist and are DI-registered by tasks 04/05. `[FromServices]` per-action injection, no constructor, `[EndpointSummary]`/`[Tags]`/`[ProducesResponseType]` — exactly like `WeekScheduleCommandController`/`WeekScheduleQueryController`. Open/Close are **not** exposed (decision #11 — Quartz + reconciliation call those interactors directly). Errors flow through the existing `ApiExceptionFilter` (domain guard → 409, `PublicationNotFoundException` → 404, `AuthenticationFailedException` → 401) — **no filter changes**.

Endpoints (plan API-surface table):

| Endpoint | Interactor | Returns |
|---|---|---|
| `POST api/publications/{id:guid}/publish` | `PublishPublicationInteractor` | 204 |
| `POST api/publications/{id:guid}/extend-window` | `ExtendPublicationWindowInteractor` | 204 |
| `POST api/publications/{id:guid}/reopen` | `ReopenPublicationInteractor` | 204 |
| `GET api/publications/by-week?week=YYYY-MM-DD` | `GetPublicationInteractor` | 200 `GetPublicationResponse` / 404 |
| `GET api/publications/{id:guid}/dashboard?teacherId=` | `GetPublicationDashboardInteractor` | 200 `GetPublicationDashboardResponse` / 404 |
| `GET api/publications/history` | `FindPublicationHistoryInteractor` | 200 `ItemForFindPublicationHistoryResponse[]` |
| `GET api/publications/{id:guid}/excel?teacherId=` | `DownloadPublicationExcelInteractor` | 200 `.xlsx` / 404 |

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationCommandController.cs`
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs`

---

- [ ] **Step 1: Command controller**

`Controllers\Publication\PublicationCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ExtendPublicationWindow;
using DrivingLessons.Application.Commands.PublishPublication;
using DrivingLessons.Application.Commands.ReopenPublication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Publication;

[ApiController]
[Route("api/publications")]
[Tags("Publications")]
public class PublicationCommandController : ControllerBase
{
    [HttpPost("{id:guid}/publish")]
    [EndpointSummary("Publishes a draft publication with a submission window and schedules its open/close jobs")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(
        [FromServices] PublishPublicationInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] PublishPublicationRequest request)
    {
        await interactor.ExecuteAsync(id, request.StartUtc, request.EndUtc);

        return NoContent();
    }

    [HttpPost("{id:guid}/extend-window")]
    [EndpointSummary("Extends the submission window end of an open publication and reschedules its close job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExtendWindow(
        [FromServices] ExtendPublicationWindowInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ExtendPublicationWindowRequest request)
    {
        await interactor.ExecuteAsync(id, request.NewEndUtc);

        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    [EndpointSummary("Reopens a closed publication with a new window end and reschedules its close job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(
        [FromServices] ReopenPublicationInteractor interactor,
        [FromRoute] Guid id,
        [FromBody] [Required] ReopenPublicationRequest request)
    {
        await interactor.ExecuteAsync(id, request.NewEndUtc);

        return NoContent();
    }
}
```

> The three request records (`PublishPublicationRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc)`, `ExtendPublicationWindowRequest(DateTimeOffset NewEndUtc)`, `ReopenPublicationRequest(DateTimeOffset NewEndUtc)`) were created with their interactors in task 04. This controller only unpacks them onto the interactor's primitive `ExecuteAsync` parameters — no logic.

- [ ] **Step 2: Query controller**

`Controllers\Publication\PublicationQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Publication;

[ApiController]
[Route("api/publications")]
[Tags("Publications")]
public class PublicationQueryController : ControllerBase
{
    [HttpGet("by-week")]
    [EndpointSummary("Gets the publication for a calendar week")]
    [ProducesResponseType(typeof(GetPublicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetPublicationResponse> GetByWeek(
        [FromServices] GetPublicationInteractor interactor,
        [FromQuery] DateOnly week)
    {
        return await interactor.ExecuteAsync(week);
    }

    [HttpGet("{id:guid}/dashboard")]
    [EndpointSummary("Gets the per-teacher dashboard of slot request counts for a publication")]
    [ProducesResponseType(typeof(GetPublicationDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<GetPublicationDashboardResponse> GetDashboard(
        [FromServices] GetPublicationDashboardInteractor interactor,
        [FromRoute] Guid id,
        [FromQuery] Guid teacherId)
    {
        return await interactor.ExecuteAsync(id, teacherId);
    }

    [HttpGet("history")]
    [EndpointSummary("Gets the history of past publications per teacher with state and latest Excel version")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemForFindPublicationHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<ItemForFindPublicationHistoryResponse>> FindHistory(
        [FromServices] FindPublicationHistoryInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }

    [HttpGet("{id:guid}/excel")]
    [EndpointSummary("Downloads the teacher's Excel workbook for a publication")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadExcel(
        [FromServices] DownloadPublicationExcelInteractor interactor,
        [FromRoute] Guid id,
        [FromQuery] Guid teacherId)
    {
        var excel = await interactor.ExecuteAsync(id, teacherId);

        return File(excel.Content, excel.ContentType, excel.FileName);
    }
}
```

> `DownloadPublicationExcelInteractor.ExecuteAsync(Guid id, Guid teacherId)` returns the `ExcelFile(string FileName, byte[] Content, string ContentType)` record from `DrivingLessons.Application.Abstractions`; the controller streams it via `File(...)`. `GetPublicationInteractor.ExecuteAsync(DateOnly week)` returns the non-null `GetPublicationResponse` (it wraps `IPublicationQueries.GetByWeekAsync` and throws `PublicationNotFoundException` on null → the filter maps it to 404).

- [ ] **Step 3: Verify DI registration**

All seven interactors are registered by tasks 04/05 in `src\DrivingLessons.Application\DependencyInjection.cs`. Confirm each resolves before smoke-testing (a missing registration surfaces as `InvalidOperationException: Unable to resolve service` at the first request):

```
PublishPublicationInteractor
ExtendPublicationWindowInteractor
ReopenPublicationInteractor
GetPublicationInteractor
GetPublicationDashboardInteractor
FindPublicationHistoryInteractor
DownloadPublicationExcelInteractor
```

If any is missing, add it in `AddApplication` (do not register in the Presentation project) and re-run the build.

- [ ] **Step 4: Build**

Run: `dotnet build`

Expected output: `Build succeeded.` with 0 errors. The new controllers are discovered by convention (`[ApiController]` + `[Route("api/publications")]`); no startup wiring is needed.

- [ ] **Step 5: Scalar / HTTP smoke test**

Postgres must be up (Docker Compose, same as the week-schedules verification; migrations auto-apply on startup). Run the app and open the Scalar UI in dev (or use curl). A Draft publication already exists for any week whose teachers have a WeekSchedule (created by the `WeekScheduleCreated` handler from task 03) — prepare a teacher's week first if none exists, then read its id from `by-week`.

1. Call any endpoint **without** a bearer token → **401** ProblemDetails (`AuthenticationFailedException`).
2. Authorize with a bearer token from `POST /api/auth/login`.
3. `GET /api/publications/by-week?week=2026-07-19` → **200** `{ "id": "...", "weekStart": "2026-07-19", "state": "draft", "linkToken": "...", "windowStartUtc": null, "windowEndUtc": null }`. Copy the `id`.
4. `POST /api/publications/{id}/publish` body `{"startUtc":"2026-07-19T06:00:00Z","endUtc":"2026-07-19T18:00:00Z"}` → **204**; repeat the same POST → **409** ProblemDetails "must be draft" (state moved past Draft).
5. `GET /api/publications/by-week?week=2026-07-19` again → **200** with `state` now `published` (or `open` if the open job already fired) and `windowStartUtc`/`windowEndUtc` populated; `linkToken` is a stable unguessable string.
6. `GET /api/publications/{id}/dashboard?teacherId={teacher}` → **200** with the week's slots, every `requestCount` = 0 (submission stub) and Unavailable slots flagged; stats fields zero/null.
7. `POST /api/publications/{id}/extend-window` body `{"newEndUtc":"2026-07-19T20:00:00Z"}` → **204** while Open; sending an `newEndUtc` earlier than the current end → **409** "must be later".
8. `GET /api/publications/{id}/excel?teacherId={teacher}` → **200**, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, a downloadable `.xlsx` body.
9. `GET /api/publications/history` → **200** array with one row per teacher for the week (state + `latestExcelVersion`).
10. `POST /api/publications/{id}/reopen` body `{"newEndUtc":"2026-07-19T22:00:00Z"}` on a publication that is **not** Closed → **409** "must be closed"; after the close job fires (or a manual reconcile), the same call → **204** back to Open.

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Controllers/Publication
git commit -m "feat(api): publication controllers"
```

---

**Next:** [task-10-client-domain-data.md](task-10-client-domain-data.md)
