# Task 5 of 11: Anonymous `POST`/`PUT` submission endpoints + API smoke test on a throwaway DB

> Part of [US-33…US-41: First Submission](README.md). Requires task 4 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs`
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry)

**Interfaces:**
- Consumes (task 4): `CreateSubmissionInteractor.ExecuteAsync(string, CreateSubmissionRequest)`, `ReviseSubmissionInteractor.ExecuteAsync(string, ReviseSubmissionRequest)`, `SlotRequestForSubmissionRequest`.
- Produces (task 7's client DTOs mirror this JSON exactly):
  - `POST api/submissions/by-link/{token}` — create. `PUT api/submissions/by-link/{token}` — revise (full replace). Both anonymous.
  - Body `{ "nationalId": string, "targetCount": number, "slotRequests": [{ "slotId": uuid, "sessionType": "single" | "double", "constraint": string | null }] }` — `slotRequests` in rank order.
  - **204** no body · **400** ValidationProblemDetails (missing `nationalId`/`slotRequests`, unknown `sessionType` string, malformed uuid) · **404** ProblemDetails (unknown/draft link, not (or no longer) on the roster, the teacher has no grid, slot outside the student's grid, PUT with no submission) · **409** ProblemDetails (every domain rule, incl. POST when one exists).

Mirror the existing `IdentifyAsync` action in the same controller (class-level `[AllowAnonymous]` — `Program.cs` sets a `RequireAuthenticatedUser` fallback policy; `[FromServices]` on the action; `[FromBody] [Required]`; action names end in `Async`; `[EndpointSummary]`). `ApiExceptionFilter` already maps `NotFoundException` → 404 and `DomainException` → 409; nothing changes in `Program.cs`.

- [ ] **Step 1: Controller actions**

Replace `src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs` with:

```csharp
using System.ComponentModel.DataAnnotations;
using DrivingLessons.Application.Commands.CreateSubmission;
using DrivingLessons.Application.Commands.ReviseSubmission;
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

    [HttpPost("by-link/{token}")]
    [EndpointSummary("Creates the student's submission for the week; fails if one already exists")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromServices] CreateSubmissionInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] CreateSubmissionRequest request)
    {
        await interactor.ExecuteAsync(token, request);

        return NoContent();
    }

    [HttpPut("by-link/{token}")]
    [EndpointSummary("Replaces the student's submission for the week; fails if there is none yet")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReviseAsync(
        [FromServices] ReviseSubmissionInteractor interactor,
        [FromRoute] string token,
        [FromBody] [Required] ReviseSubmissionRequest request)
    {
        await interactor.ExecuteAsync(token, request);

        return NoContent();
    }
}
```

`POST` / `PUT` on `by-link/{token}` share the path of slice 1's `GET` (the link context), split across the command and query controllers as api-guidelines require. The national ID stays in the body (Global Constraints). 204 on create is a deliberate deviation from "Create → 201 + body" (README decision 1): the only thing a body could return is an id, and no id is handed to an anonymous caller.

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: success, no new warnings.

- [ ] **Step 3: Start the API against a throwaway database**

⚠️ The smoke test uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`).

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history):
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us33_smoke
   ```
   (If `createdb` reports the database exists from an earlier run, drop it first: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us33_smoke`. It only ever holds this smoke data.)
2. Add (or re-point) a **local-only** entry in `.claude\launch.json` `configurations` — the file already has unrelated local edits; never stage it:
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us33_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first — both use port 5080). Startup applies **all** migrations to the empty database — the first real application of task 3's `AddSubmissions` — and seeds the dev admin. `preview_logs` must show no migration error.

Keep this server running — tasks 6 and 11 reuse the same database.

- [ ] **Step 4: Seed two teachers' grids for two weeks, a roster and an open publication (bash / Git Bash)**

The national IDs are synthetic (`0000000xx` with a valid check digit) — never real IDs in test data.

```bash
API=http://localhost:5080
WEEK=2026-10-04
WEEK2=2026-10-11
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }

TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"

new_teacher() { curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"contactEmail\":\"$2\"}" | json id; }
new_car() { curl -s -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"type\":\"Yaris\",\"transmission\":\"$2\"}" | json id; }
new_week() { curl -s -X POST $API/api/week-schedules -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"teacherId\":\"$1\",\"weekStart\":\"$2\"}" | json id; }
publish() { curl -s -o /dev/null -w "publish $1: %{http_code}\n" -X POST $API/api/publications/$1/publish \
  -H "$AUTH" -H "Content-Type: application/json" -d "{\"startUtc\":\"$2\",\"endUtc\":\"$3\"}"; }

COHEN_ID=$(new_teacher "Smoke Cohen" smoke.cohen@example.com)
LEVI_ID=$(new_teacher "Smoke Levi" smoke.levi@example.com)
new_car "Smoke Auto" automatic > /dev/null
new_car "Smoke Manual" manual > /dev/null

new_week $COHEN_ID $WEEK > /dev/null
LEVI_WS=$(new_week $LEVI_ID $WEEK)
new_week $COHEN_ID $WEEK2 > /dev/null
LEVI_SUNDAY_MORNING=$(curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$LEVI_ID&week=$WEEK" -H "$AUTH" | json slots.0.id)
curl -s -o /dev/null -w "block Levi's Sunday morning: %{http_code}\n" -X POST \
  "$API/api/week-schedules/$LEVI_WS/slots/$LEVI_SUNDAY_MORNING/mark-unavailable" -H "$AUTH"

ROSTER_DIR=$(mktemp -d)
cat > "$ROSTER_DIR/roster-full.csv" <<'CSV'
שם מלא,תעודת זהות,טלפון,מורה,רכב
Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
Smoke Student D,000000042,050-0000004,Smoke Cohen,Smoke Auto
CSV
head -n 3 "$ROSTER_DIR/roster-full.csv" > "$ROSTER_DIR/roster-without-d.csv"

curl -s -w "  ← import full: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster-full.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
PUB2=$(curl -s "$API/api/publications/by-week?week=$WEEK2" -H "$AUTH")
PUB2_ID=$(json id <<< "$PUB2")
LINK2=$(json linkToken <<< "$PUB2")

publish $PUB_ID "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)"
sleep 5
echo "week 1 link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
echo "LINK=$LINK LINK2=$LINK2 PUB_ID=$PUB_ID COHEN_ID=$COHEN_ID LEVI_ID=$LEVI_ID"
```

Expected:
1. `block Levi's Sunday morning: 204`.
2. `import full: 201` with `"added":3,"updated":0,"deactivated":0,"failed":0`.
3. `publish <id>: 204`, then `week 1 link state: open` (if it still says `published`, wait a few seconds — Quartz fires the past-due open job).
4. The `LINK=… LINK2=… …` line with two different 43-character tokens. **Write these five values down** — tasks 6 and 11 reuse them. Week 2 stays **Draft** for now.

Roles: **A** `000000018` → Cohen (automatic, all 22 slots open) · **B** `000000026` → Levi (manual, Sunday morning blocked) · **D** `000000042` → Cohen, deactivated mid-flow in Step 5.

- [ ] **Step 5: API smoke test — every outcome, over HTTP (same shell)**

```bash
identify() { curl -s -X POST "$API/api/submissions/by-link/$1/identify" -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"$2\"}"; }
body() { node -e 'const [nationalId,target,...picks]=process.argv.slice(1);console.log(JSON.stringify({nationalId,targetCount:Number(target),slotRequests:picks.map(p=>{const [slotId,sessionType,...rest]=p.split("|");return {slotId,sessionType,constraint:rest.length?rest.join("|"):null}})}))' "$@"; }
problem() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{if(!s.trim())return;const o=JSON.parse(s);console.log(o.detail??o.title)})"; }
send() {
  local response status content
  response=$(curl -s -w "\n%{http_code}" -X "$1" "$API/api/submissions/by-link/$2" -H "Content-Type: application/json" -d "$3" "${@:4}")
  status=$(tail -n1 <<< "$response")
  content=$(sed '$d' <<< "$response")
  echo "$status $(problem <<< "$content")"
}

A_GRID=$(identify $LINK 000000018)
B_GRID=$(identify $LINK 000000026)
a() { json "slots.$1.id" <<< "$A_GRID"; }
b() { json "slots.$1.id" <<< "$B_GRID"; }
LONG200=$(printf 'a%.0s' {1..200})
LONG201=$(printf 'a%.0s' {1..201})

echo "1  A create, target 2, 3 picks:      $(send POST $LINK "$(body 000000018 2 "$(a 2)|double|only after 16:00" "$(a 7)|single" "$(a 13)|single")")"
echo "2  A create again:                   $(send POST $LINK "$(body 000000018 1 "$(a 1)|single")")"
echo "3  A revise, target 2, 5 picks:      $(send PUT $LINK "$(body 000000018 2 "$(a 5)|double|only after 16:00" "$(a 0)|single" "$(a 9)|single" "$(a 14)|single|pick me up from work" "$(a 20)|single")")"
echo "4  B revise before creating:         $(send PUT $LINK "$(body 000000026 1 "$(b 1)|single")")"
echo "5  B target 3, only 2 picks:         $(send POST $LINK "$(body 000000026 3 "$(b 1)|single" "$(b 2)|single")")"
echo "6  B picks Levi's blocked slot:      $(send POST $LINK "$(body 000000026 1 "$(b 0)|single")")"
echo "7  B picks one of Cohen's slots:     $(send POST $LINK "$(body 000000026 1 "$(a 3)|single")")"
echo "8  B picks the same slot twice:      $(send POST $LINK "$(body 000000026 1 "$(b 1)|single" "$(b 1)|double")")"
echo "9  B target 0:                       $(send POST $LINK "$(body 000000026 0 "$(b 1)|single")")"
echo "10 B session type 99:                $(send POST $LINK "{\"nationalId\":\"000000026\",\"targetCount\":1,\"slotRequests\":[{\"slotId\":\"$(b 1)\",\"sessionType\":99,\"constraint\":null}]}")"
echo "11 B session type \"triple\":          $(send POST $LINK "$(body 000000026 1 "$(b 1)|triple")")"
echo "12 B constraint of 201 characters:   $(send POST $LINK "$(body 000000026 1 "$(b 1)|single|$LONG201")")"
echo "13 bad check digit:                  $(send POST $LINK "$(body 000000019 1 "$(b 1)|single")")"
echo "14 ID not on the roster:             $(send POST $LINK "$(body 000000059 1 "$(b 1)|single")")"
echo "15 empty body:                       $(send POST $LINK '{}')"
echo "16 unknown link:                     $(send POST NoSuchTokenAbc123 "$(body 000000026 1 "$(b 1)|single")")"
echo "17 draft link (week 2):              $(send POST $LINK2 "$(body 000000018 1 "$(a 1)|single")")"
echo "18 B constraint of 200 characters:   $(send POST $LINK "$(body 000000026 1 "$(b 1)|single|$LONG200")")"
echo "19 A revise with a stale bearer:     $(send PUT $LINK "$(body 000000018 2 "$(a 5)|double|only after 16:00" "$(a 0)|single" "$(a 9)|single" "$(a 14)|single|pick me up from work" "$(a 20)|single")" -H 'Authorization: Bearer not-a-real-jwt')"

echo "20 D identified before removal:      $(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/api/submissions/by-link/$LINK/identify" -H 'Content-Type: application/json' -d '{"nationalId":"000000042"}')"
curl -s -o /dev/null -w "   roster re-upload without D:      %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster-without-d.csv;type=text/csv"
echo "21 D submits after removal:          $(send POST $LINK "$(body 000000042 1 "$(a 1)|single")")"
```

Expected output, in order:

1. `1 … 204` — US-33 (target carried), US-35 (a Double), US-36 (a constraint), US-37 (three ranked picks).
2. `2 … 409 A submission already exists for this student and week.` — **Review Focus 5**.
3. `3 … 204` — US-39: five picks against target 2 are accepted; revise is a full replace (roadmap decision 3).
4. `4 … 404 No submission exists for this student and week yet.`
5. `5 … 409 A submission needs at least 3 slot requests to cover its target; it has 2.` — **US-40**'s exact scenario.
6. `6 … 409 Unavailable slots cannot be requested.` — **US-34** / **Review Focus 2** (a tampered client sending a blocked slot id).
7. `7 … 404 The requested slot is not in the student's week schedule.` — **Review Focus 3** (another teacher's grid).
8. `8 … 409 A slot can be requested at most once per submission.` — **Review Focus 1**.
9. `9 … 409 Target session count must be at least 1 (was 0).`
10. `10 … 409 Session type must be Single or Double.` — **Review Focus 9** (the enum converter accepts integers; the domain refuses 99).
11. `11 … 400 One or more validation errors occurred.` — an unknown enum string never reaches the interactor.
12. `12 … 409 Slot constraint must be at most 200 characters.` — **Review Focus 7**; the detail does not echo the text.
13. `13 … 409 National ID must have a valid check digit.`
14. `14 … 404 No active student on the roster matches this national ID.` — the ID is not echoed.
15. `15 … 400 One or more validation errors occurred.`
16. `16 … 404 No publication is available for this link.`
17. `17 … 404 No publication is available for this link.` — a draft link is invisible to students (slice 1).
18. `18 … 204` — exactly 200 characters is accepted.
19. `19 … 204` — an expired admin token in the browser never breaks the student flow.
20. `20 … 200`, then `roster re-upload without D: 201`.
21. `21 … 404 No active student on the roster matches this national ID.` — **Review Focus 6**: deactivated between identify and submit, D gets the same "not on file" answer identify would now give.

- [ ] **Step 6: Window closes mid-submit (same shell)**

Publish week 2 with a window that ends about a minute from now, identify while it is open, then submit after the close job has run.

```bash
publish $PUB2_ID "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+60 seconds' +%Y-%m-%dT%H:%M:%SZ)"
sleep 5
echo "week 2 state: $(curl -s $API/api/submissions/by-link/$LINK2 | json state)"
A_GRID2=$(identify $LINK2 000000018)
echo "A identified on week 2: $(json slots.length <<< "$A_GRID2") slots"
for i in $(seq 1 30); do [ "$(curl -s $API/api/submissions/by-link/$LINK2 | json state)" = closed ] && break; sleep 5; done
echo "week 2 state: $(curl -s $API/api/submissions/by-link/$LINK2 | json state)"
echo "22 A submits after the close:       $(send POST $LINK2 "$(body 000000018 1 "$(json slots.0.id <<< "$A_GRID2")|single")")"
```

Expected: `publish …: 204`, `week 2 state: open`, `A identified on week 2: 22 slots`, then (within ~2 minutes) `week 2 state: closed` and `22 A submits after the close: 409 Submissions are accepted only while the week's submission window is open.` — **Review Focus 4** (the page was loaded while open; the command is refused after close). If the state never reaches `closed`, check the `ClosePublicationJob` lines in `preview_logs` before suspecting the endpoint.

- [ ] **Step 7: What was stored**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us33_smoke -c "
select st.name, s.target_count, r.rank, r.session_type, left(r.constraint_text, 24) as constraint_text,
       s.revised_at_utc is not null as revised
from submissions s
join students st on st.id = s.student_id
join slot_requests r on r.submission_id = s.id
order by st.name, r.rank;"
```

Expected — exactly six rows, nothing for D and nothing on week 2:

| name | target_count | rank | session_type | constraint_text | revised |
|------|--------------|------|--------------|-----------------|---------|
| Smoke Student A | 2 | 1 | 20 | only after 16:00 | t |
| Smoke Student A | 2 | 2 | 10 | | t |
| Smoke Student A | 2 | 3 | 10 | | t |
| Smoke Student A | 2 | 4 | 10 | pick me up from work | t |
| Smoke Student A | 2 | 5 | 10 | | t |
| Smoke Student B | 1 | 1 | 10 | aaaaaaaaaaaaaaaaaaaaaaaa | f |

A's three original requests are gone (full replace), ranks are contiguous from 1, `session_type` 20 = Double, and the constraint lives on its own slot request (US-36, decision 11).

- [ ] **Step 8: Scalar check**

Open `http://localhost:5080/scalar/v1` → the **Submissions** tag lists `POST /api/submissions/by-link/{token}` and `PUT /api/submissions/by-link/{token}` with 204/400/404/409, and the `CreateSubmissionRequest` / `ReviseSubmissionRequest` / `SlotRequestForSubmissionRequest` schemas (`sessionType` as the `single | double` string enum).

Keep the API running and the shell open (`API`, `AUTH`, `LINK`, `LINK2`, `PUB_ID`, `COHEN_ID`, `LEVI_ID`, the helpers) — task 6 continues here.

- [ ] **Step 9: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Controllers/Submission/SubmissionCommandController.cs
git commit -m "feat(api): anonymous submission create and revise endpoints

POST and PUT api/submissions/by-link/{token} take the national ID, target
and ranked slot requests in the body and answer 204. Unknown links,
students, grids and foreign slots 404; every submission rule 409."
```

---

**Next:** [task-06-submission-read-side.md](task-06-submission-read-side.md)
