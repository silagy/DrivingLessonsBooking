# Task 2 of 6: Anonymous `SubmissionCommandController` + API smoke test

> Part of [US-50/51/27: Identity & Routing](README.md). Requires task 1 complete. Work on branch `52-us-50-51-27-identity-and-routing`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs`
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry)

**Interfaces:**
- Consumes (task 1): `IdentifyStudentInteractor.ExecuteAsync(string linkToken, IdentifyStudentRequest request)`, `IdentifyStudentRequest`, `IdentifyStudentResponse`.
- Produces: `POST api/submissions/by-link/{token}/identify` — anonymous; body `{ "nationalId": string }`.
  - **200** `{ studentName, teacherName, carName, transmission: "automatic" | "manual", slots: [{ id, day: "sunday"…"friday", window: "morning" | "noon" | "afternoon" | "evening", state: "open" | "unavailable", startLocal: "HH:mm:ss", endLocal: "HH:mm:ss" }] }` — `slots` is `[]` when the teacher has no week schedule for the week.
  - **400** ValidationProblemDetails — missing/empty `nationalId`.
  - **404** ProblemDetails — unknown/draft link, or no active roster student for the ID.
  - **409** ProblemDetails — malformed national ID (non-digits, more than 9 digits, bad check digit).
  - Task 3's client DTOs mirror this JSON exactly.

Mirror `SubmissionQueryController.cs` (same `api/submissions` base, same `[AllowAnonymous]` class attribute — `Program.cs` sets a `RequireAuthenticatedUser` fallback policy). Zero logic, `[FromServices]` on the action, `[FromBody] [Required]` request, action name ends in `Async`. `ApiExceptionFilter` already maps `NotFoundException` → 404 and `DomainException` → 409; nothing changes in `Program.cs`.

- [ ] **Step 1: Controller**

`src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Queries.IdentifyStudent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrivingLessons.Presentation.Web.Controllers.Submission;

[ApiController]
[AllowAnonymous]
[Route("api/submissions")]
[Tags("Submissions")]
public class SubmissionCommandController : ControllerBase
{
    [HttpPost("by-link/{token}/identify")]
    [EndpointSummary("Identifies a roster student by national ID and returns their teacher, car and the teacher's week grid")]
    [ProducesResponseType(typeof(IdentifyStudentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IdentifyStudentResponse> IdentifyAsync(
        [FromServices] IdentifyStudentInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] IdentifyStudentRequest request)
    {
        return await interactor.ExecuteAsync(token, request);
    }
}
```

POST (not GET) keeps the national ID out of the URL; the verb puts it in the *Command* controller per api-guidelines, although the interactor is a query (README decision 1). Slice 3 adds `POST`/`PUT` submission actions to this controller.

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: success, no new warnings.

- [ ] **Step 3: Start the API against a throwaway database**

⚠️ The smoke test uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`) — it would deactivate the real roster.

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history):
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us50_smoke
   ```
   (If `createdb` reports the database already exists from an earlier run, drop and recreate it: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us50_smoke` first. It only ever holds this smoke data.)
2. Add a **local-only** entry to `.claude\launch.json` `configurations` (the file already has unrelated local edits — never stage it):
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us50_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
   The trailing `--ConnectionStrings:Default=…` is a command-line configuration override (highest precedence over `appsettings.Development.json`).
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first — both use port 5080). The API applies all migrations to the empty database and seeds the dev admin on startup.

Keep this server running — task 6 reuses the same database and publication.

- [ ] **Step 4: Seed two teachers' grids, a roster and an open publication (bash / Git Bash)**

The national IDs below are synthetic (`0000000xx` with a valid check digit) — never use real IDs in test data.

```bash
API=http://localhost:5080
WEEK=2026-10-04
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"

new_teacher() { curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"contactEmail\":\"$2\"}" | json id; }
new_car() { curl -s -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"type\":\"Yaris\",\"transmission\":\"$2\"}" | json id; }
new_week() { curl -s -X POST $API/api/week-schedules -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"teacherId\":\"$1\",\"weekStart\":\"$WEEK\"}" | json id; }

COHEN_ID=$(new_teacher "Smoke Cohen" smoke.cohen@example.com)
LEVI_ID=$(new_teacher "Smoke Levi" smoke.levi@example.com)
NOWEEK_ID=$(new_teacher "Smoke NoWeek" smoke.noweek@example.com)
new_car "Smoke Auto" automatic > /dev/null
new_car "Smoke Manual" manual > /dev/null

new_week $COHEN_ID > /dev/null
LEVI_WS=$(new_week $LEVI_ID)
LEVI_SUNDAY_MORNING=$(curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$LEVI_ID&week=$WEEK" -H "$AUTH" | json slots.0.id)
curl -s -o /dev/null -w "block Levi's Sunday morning: %{http_code}\n" -X POST \
  "$API/api/week-schedules/$LEVI_WS/slots/$LEVI_SUNDAY_MORNING/mark-unavailable" -H "$AUTH"

ROSTER_DIR=$(mktemp -d)
cat > "$ROSTER_DIR/roster-full.csv" <<'CSV'
שם מלא,תעודת זהות,טלפון,מורה,רכב
Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
Smoke Student C,000000034,050-0000003,Smoke NoWeek,Smoke Auto
Smoke Student D,000000042,050-0000004,Smoke Cohen,Smoke Auto
CSV
head -n 4 "$ROSTER_DIR/roster-full.csv" > "$ROSTER_DIR/roster-without-d.csv"

curl -s -w "  ← import full: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster-full.csv;type=text/csv"
curl -s -w "  ← import without D: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster-without-d.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
echo "LINK=$LINK"
```

Expected:
1. `block Levi's Sunday morning: 204` (slots are ordered Sunday→Friday, Morning→Evening, so `slots.0` is Sunday morning).
2. `import full` → `201` with `"added":4,"updated":0,"deactivated":0,"failed":0`.
3. `import without D` → `201` with `"added":0,"updated":3,"deactivated":1,"failed":0`.
4. A `LINK=` line with a 43-character token. Keep `API`, `AUTH`, `PUB_ID`, `LINK` and the helpers in this shell for Step 5.

Roles in the checks below: **A** → Cohen (automatic, all open) · **B** → Levi (manual, Sunday morning blocked) · **C** → a teacher with no grid this week · **D** → deactivated by the second upload.

- [ ] **Step 5: API smoke test — routing, outcomes and PII (same shell)**

```bash
identify() { curl -s -X POST "$API/api/submissions/by-link/$1/identify" -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"$2\"}"; }
status() { curl -s -o /dev/null -w "%{http_code}" -X POST "$API/api/submissions/by-link/$1/identify" \
  -H "Content-Type: application/json" -d "$2" "${@:3}"; }
summary() { local body; body=$(identify "$LINK" "$1"); echo "$1 → $(json teacherName <<< "$body") | $(json transmission <<< "$body") | $(json carName <<< "$body") | $(json slots.length <<< "$body") slots | Sunday morning: $(json slots.0.state <<< "$body")"; }

echo "draft link: $(status $LINK '{"nationalId":"000000018"}')"

START=$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)
END=$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish \
  -H "$AUTH" -H "Content-Type: application/json" -d "{\"startUtc\":\"$START\",\"endUtc\":\"$END\"}"
sleep 5
echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"

identify $LINK 000000018 | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const o=JSON.parse(s);console.log('fields:',Object.keys(o).join(','),'| slot fields:',Object.keys(o.slots[0]).join(','))})"
summary 000000018
summary 000000026
summary 18
summary 000000034
echo "deactivated D: $(status $LINK '{"nationalId":"000000042"}')"
echo "unknown ID: $(status $LINK '{"nationalId":"000000059"}') | detail: $(identify $LINK 000000059 | json detail)"
echo "unknown ID echoed: $(identify $LINK 000000059 | grep -c 000000059)"
echo "bad check digit: $(status $LINK '{"nationalId":"000000019"}')"
echo "inner spaces: $(status $LINK '{"nationalId":"000 000 018"}')"
echo "empty body: $(status $LINK '{}')"
echo "unknown link: $(status NoSuchTokenAbc123 '{"nationalId":"000000018"}')"
echo "stale bearer: $(status $LINK '{"nationalId":"000000018"}' -H 'Authorization: Bearer not-a-real-jwt')"
echo "admin roster anonymous: $(curl -s -o /dev/null -w '%{http_code}' $API/api/students/find)"
```

Expected output, in order:

1. `draft link: 404` — the publication is still Draft (created by the first week schedule); drafts are invisible to students (slice 1 decision 2).
2. `publish: 204`, then `link state: open` (Quartz fires the past-due open job immediately; if it still says `published`, wait a few seconds and re-check before suspecting the endpoint).
3. `fields: studentName,teacherName,carName,transmission,slots | slot fields: id,day,window,state,startLocal,endLocal` — **no** national ID, phone, address, or teacher/student ids (README decision 2).
4. `000000018 → Smoke Cohen | automatic | Smoke Auto | 22 slots | Sunday morning: open`
5. `000000026 → Smoke Levi | manual | Smoke Manual | 22 slots | Sunday morning: unavailable` — **US-51**: same link, different student → a different teacher's grid, transmission from the roster car.
6. `18 → Smoke Cohen | automatic | …` — the leading zeros were dropped and the backend padded them back (**Review Focus 3**).
7. `000000034 → Smoke NoWeek | automatic | Smoke Auto | 0 slots | Sunday morning: undefined` — identified, but the teacher has no grid this week (**Review Focus 2**).
8. `deactivated D: 404` — **Review Focus 1**.
9. `unknown ID: 404 | detail: No active student on the roster matches this national ID.` and `unknown ID echoed: 0` — **Review Focus 4**.
10. `bad check digit: 409`, `inner spaces: 409` (the client strips separators before sending — task 3).
11. `empty body: 400`.
12. `unknown link: 404`.
13. `stale bearer: 200` — an expired admin token in the browser must not break the student flow.
14. `admin roster anonymous: 401` — only the student controllers are anonymous.

Keep the API running and note `LINK` — task 6 reuses this database and publication (the window stays open for 3 hours; if it has closed by then, re-run the `START`/`END` lines and reopen via `POST api/publications/{PUB_ID}/reopen` with `{"newEndUtc": …}`, or simply repeat Steps 3–4 on a fresh smoke database).

- [ ] **Step 6: Scalar check**

Open `http://localhost:5080/scalar/v1` → the **Submissions** tag lists `POST /api/submissions/by-link/{token}/identify` with 200/400/404/409 and the `IdentifyStudentRequest` body schema. (The lock icon comes from the document-wide bearer scheme — documentation only; Step 5 proved the endpoint is anonymous.)

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Controllers/Submission/SubmissionCommandController.cs
git commit -m "feat(api): anonymous student identify endpoint

POST api/submissions/by-link/{token}/identify admits active roster
students and returns their teacher, car transmission and the teacher's
week grid; unknown or deactivated IDs 404, malformed IDs 409."
```

---

**Next:** [task-03-client-domain-data.md](task-03-client-domain-data.md)
