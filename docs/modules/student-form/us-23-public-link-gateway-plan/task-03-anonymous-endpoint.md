# Task 3 of 6: Anonymous `SubmissionQueryController` + API smoke test

> Part of [US-23: Public Link Gateway](README.md). Requires tasks 1–2 complete. Work on branch `24-us-23-public-link-gateway`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionQueryController.cs`

**Interfaces:**
- Consumes: `GetPublicationByLinkInteractor.ExecuteAsync(string linkToken)` and `GetPublicationByLinkResponse` (task 2).
- Produces: `GET api/submissions/by-link/{token}` — anonymous; **200** `{ weekStart: "yyyy-MM-dd", weekNumber: number, state: "published" | "open" | "closed", windowStartUtc: ISO-8601, windowEndUtc: ISO-8601 }`; **404** ProblemDetails for unknown **and** draft tokens. Task 4's client DTO mirrors this JSON exactly.

Mirror `PublicationQueryController.cs` / `StudentQueryController.cs`: zero logic, `[FromServices]` on the action, `[EndpointSummary]`, `[ProducesResponseType]`, action name ends in `Async`. The one difference: **`[AllowAnonymous]` on the class** — `Program.cs` sets a fallback policy (`RequireAuthenticatedUser`) for every endpoint, and `[AllowAnonymous]` is what lets the student in (api-guidelines "Auth"). The unguessable 32-byte token is the access control. `ApiExceptionFilter` already maps `NotFoundException` → 404, so nothing else changes in `Program.cs`. Future student endpoints (identify, create/update submission) will live in a sibling `SubmissionCommandController` on the same `api/submissions` base.

- [x] **Step 1: Controller**

`src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionQueryController.cs`:

```csharp
using DrivingLessons.Application.Queries.GetPublicationByLink;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Submission;

[ApiController]
[AllowAnonymous]
[Route("api/submissions")]
[Tags("Submissions")]
public class SubmissionQueryController : ControllerBase
{
    [HttpGet("by-link/{token}")]
    [EndpointSummary("Gets the week and submission window behind a student link; draft and unknown links are not found")]
    [ProducesResponseType(typeof(GetPublicationByLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<GetPublicationByLinkResponse> GetPublicationByLinkAsync(
        [FromServices] GetPublicationByLinkInteractor interactor,
        [FromRoute] string token)
    {
        return await interactor.ExecuteAsync(token);
    }
}
```

No 401 response type — the endpoint is anonymous by design.

- [x] **Step 2: Build**

Run: `dotnet build`
Expected: success, no new warnings.

- [x] **Step 3: Start the stack**

Postgres must be up (`docker start dl-postgres`, or see `docs\development\running-the-project.md` Option B). Start the API from source (`dotnet run --project src\DrivingLessons.Presentation.Web --launch-profile http`, or `preview_start {name:"api"}`) — it listens on `http://localhost:5080` and auto-applies migrations. No migration is added in this slice.

- [x] **Step 4: API smoke test (bash / Git Bash)**

The script below exercises every Review Focus item that lives on the server: draft token → 404, unknown token → 404 without echoing it, a stale bearer header does not turn the anonymous call into a 401, and admin endpoints are still protected. Pick an **unused** Sunday for `WEEK` (a week with no existing week schedule — e.g. a Sunday a few months ahead).

```bash
API=http://localhost:5080
WEEK=2026-11-15

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | sed -E 's/.*"accessToken":"([^"]+)".*/\1/')

TEACHER_ID=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Smoke Teacher US23","contactEmail":"smoke.us23@example.com"}' | sed -E 's/.*"id":"([^"]+)".*/\1/')

curl -s -o /dev/null -w "create week schedule: %{http_code}\n" -X POST $API/api/week-schedules \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$WEEK\"}"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "Authorization: Bearer $TOKEN")
echo "$PUB"
PUB_ID=$(echo "$PUB" | sed -E 's/.*"id":"([^"]+)".*/\1/')
LINK=$(echo "$PUB" | sed -E 's/.*"linkToken":"([^"]+)".*/\1/')

curl -s -w "\ndraft link: %{http_code}\n" $API/api/submissions/by-link/$LINK

curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"startUtc":"2026-11-11T16:00:00Z","endUtc":"2026-11-13T12:00:00Z"}'

curl -s -w "\npublished link: %{http_code}\n" $API/api/submissions/by-link/$LINK
curl -s -w "\nstale bearer: %{http_code}\n" -H "Authorization: Bearer not-a-real-jwt" $API/api/submissions/by-link/$LINK
curl -s -w "\nunknown link: %{http_code}\n" $API/api/submissions/by-link/NoSuchTokenAbc123
curl -s -o /dev/null -w "admin endpoint anonymous: %{http_code}\n" $API/api/publications/history
```

Expected output, in order:

1. `create week schedule: 201`; the `by-week` JSON shows `"state":"draft"` and a `linkToken`.
2. `draft link: 404` with a ProblemDetails body — **Review Focus 1**.
3. `publish: 204`.
4. `published link: 200` with exactly these fields: `{"weekStart":"2026-11-15","weekNumber":47,"state":"published","windowStartUtc":"2026-11-11T16:00:00+00:00","windowEndUtc":"2026-11-13T12:00:00+00:00"}` — no `id`, no `linkToken`. (Week of Sun 15 Nov 2026 → Monday 16 Nov → ISO week 47. If you chose a different `WEEK`, expect the ISO week of the Monday after it.)
5. `stale bearer: 200` — **Review Focus 4** (an expired admin token in the browser must not break the student page).
6. `unknown link: 404`; the body's `detail` is `No publication is available for this link.` and does **not** contain `NoSuchTokenAbc123` — **Review Focus 2**.
7. `admin endpoint anonymous: 401` — the fallback policy is intact; only the student controller is anonymous.

Keep `WEEK`, `PUB_ID` and `LINK` handy — task 6 reuses this publication for the browser check. (If you are running PowerShell, run the script from Git Bash; the `Bash` tool in this environment is Git Bash.)

- [x] **Step 5: Scalar check**

Open `http://localhost:5080/scalar/v1` → a **Submissions** tag lists `GET /api/submissions/by-link/{token}` with the 200/404 responses. (Scalar may still draw a lock icon — `BearerSecuritySchemeTransformer` adds a document-wide security requirement. That is documentation only; Step 4 proved the endpoint is anonymous.)

- [ ] **Step 6: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Controllers/Submission
git commit -m "feat(api): anonymous student link endpoint

GET api/submissions/by-link/{token} returns the week and window for a
published, open or closed publication; drafts and unknown links 404."
```

---

**Next:** [task-04-client-domain-data.md](task-04-client-domain-data.md)
