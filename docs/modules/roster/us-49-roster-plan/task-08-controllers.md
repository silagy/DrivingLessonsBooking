# Task 8 of 10: Controllers + API smoke test

> Part of [US-49: Roster Module](README.md). Requires tasks 1–7 complete. Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\RosterImport\RosterImportCommandController.cs`, `RosterImportQueryController.cs`
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentQueryController.cs`

Mirror `PublicationCommandController.cs` / `TeacherQueryController.cs` exactly: zero logic, `[FromServices]` on every action, `[EndpointSummary]`, `[ProducesResponseType]`, `CreatedAtAction(null, result)` for creates. There is **no** `StudentCommandController` — students mutate only via roster import. All three controllers inherit the global fallback authorization policy (admin JWT) — nothing to add. The `ApiExceptionFilter` already maps `DomainException` → 409 and `NotFoundException` → 404.

- [ ] **Step 1: Roster import command controller**

`IFormFile` stays here — only the file name and opened stream cross into Application via `ImportRosterRequest`.

`Controllers\RosterImport\RosterImportCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.ImportRoster;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.RosterImport;

[ApiController]
[Route("api/roster-imports")]
[Tags("Roster Imports")]
public class RosterImportCommandController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1_048_576)]
    [EndpointSummary("Imports the Berosh roster CSV, upserting students and recording the import result")]
    [ProducesResponseType(typeof(ImportRosterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportRosterResponse>> ImportAsync(
        [FromServices] ImportRosterInteractor interactor,
        [Required] IFormFile file)
    {
        var content = file.OpenReadStream();
        var request = new ImportRosterRequest(file.FileName, content);

        var result = await interactor.ExecuteAsync(request);

        return CreatedAtAction(null, result);
    }
}
```

- [ ] **Step 2: Roster import query controller**

`Controllers\RosterImport\RosterImportQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.GetLatestRosterImport;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.RosterImport;

[ApiController]
[Route("api/roster-imports")]
[Tags("Roster Imports")]
public class RosterImportQueryController : ControllerBase
{
    [HttpGet("latest")]
    [EndpointSummary("Gets the most recent roster import with its entries and failures")]
    [ProducesResponseType(typeof(GetLatestRosterImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetLatestRosterImportResponse> GetLatestAsync(
        [FromServices] GetLatestRosterImportInteractor interactor)
    {
        return await interactor.ExecuteAsync();
    }
}
```

- [ ] **Step 3: Student query controller**

`Controllers\Student\StudentQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.FindStudents;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Student;

[ApiController]
[Route("api/students")]
[Tags("Students")]
public class StudentQueryController : ControllerBase
{
    [HttpGet("find")]
    [EndpointSummary("Finds all students, optionally filtered by teacher")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItemForFindStudentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(
        [FromServices] FindStudentsInteractor interactor,
        [FromQuery] Guid? teacherId)
    {
        return await interactor.ExecuteAsync(teacherId);
    }
}
```

- [ ] **Step 4: Build and smoke-test the API**

Run: `dotnet build` — success.

Start the app (Postgres via Docker Compose must be up, same as previous modules; migrations auto-apply). Get a bearer token from `POST /api/auth/login`, then drive the flow with curl or the Scalar UI (Development only).

**Setup** — the roster resolves teachers and cars by exact name, so create them first:
1. `POST /api/teachers` body `{"name":"משה לוי","contactEmail":"moshe@school.co.il"}` → **201**
2. `POST /api/cars` with name `טויוטה יאריס 123` (body per the cars API) → **201**

**Sample file** — save as `roster.csv` in UTF-8 (with or without BOM — Excel writes a BOM and the parser consumes it). It contains a quoted field with an embedded comma (row 2), a bad-checksum ID (row 4: `123456789`), and an unknown car (row 5):

```csv
שם מלא,תעודת זהות,טלפון,מורה,רכב,כתובת,תאריך התחלה,סוג רישיון
דנה כהן,123456782,0501234567,משה לוי,טויוטה יאריס 123,"הרצל 5, תל אביב",01/09/2025,B
יוסי מזרחי,987654324,0521234567,משה לוי,טויוטה יאריס 123,,15/8/2025,B
רות אברהם,123456789,0531234567,משה לוי,טויוטה יאריס 123,,,B
אבי שלום,335588125,0541234567,משה לוי,רכב לא קיים,,,B
```

**Flow:**

1. Upload:
   ```bash
   curl -X POST https://localhost:{port}/api/roster-imports \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@roster.csv"
   ```
   → **201** `{ "rosterImportId": "...", "added": 2, "updated": 0, "deactivated": 0, "failed": 2 }`
2. `GET /api/roster-imports/latest` → **200**; `entries` has two `added` national IDs; `failures` has row 4 (`רות אברהם`, `invalidNationalId`) and row 5 (`אבי שלום`, `unknownCar`)
3. `GET /api/students/find` → **200**, two rows, both `"isActive": true`, teacher and car names resolved; `GET /api/students/find?teacherId={teacherGuid}` → same two rows; a random teacher GUID → empty array
4. Re-upload the same file with the `דנה כהן` row deleted → **201** `{ "added": 0, "updated": 1, "deactivated": 1, "failed": 2 }`; `GET /api/students/find` now shows `דנה כהן` with `"isActive": false`
5. Upload a file whose header is missing `טלפון` → **409** ProblemDetails (required columns); upload an empty file → **409** (must not be empty)
6. `GET /api/roster-imports/latest` before any upload on a fresh database → **404**

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Presentation.Web
git commit -m "feat(api): add roster import and student endpoints"
```

---

**Next:** [task-09-client-feature.md](task-09-client-feature.md)
