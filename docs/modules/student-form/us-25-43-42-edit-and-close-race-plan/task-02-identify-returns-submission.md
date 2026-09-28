# Task 2 of 7: Identify returns the student's saved submission (+ API smoke on a throwaway DB)

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires task 1 committed. Work on branch `26-us-25-43-42-edit-and-close-race`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs` (`HasSubmission` → `Submission` + two nested responses)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry)

**Interfaces:**
- Consumes (on `main`): `Submission.TargetCount` / `SubmittedAtUtc` / `RevisedAtUtc` / `SlotRequests`, `SlotRequest.SlotId` / `SessionType` / `Constraint` / `Rank` (owned collection `slot_requests`), `DrivingLessonsDbContext.Submissions` / `Publications`; task 1's rule (the smoke's window is three hours, well inside).
- Produces (task 4's client DTOs mirror this JSON exactly):
  - `IdentifyStudentResponse.Submission : SubmissionForIdentifyStudentResponse?`, JSON `submission`, `null` when the student has none for the publication's week. It **replaces** `hasSubmission` (README decision 1).
  - `SubmissionForIdentifyStudentResponse { TargetCount: int, LastSavedAtUtc: DateTimeOffset, SlotRequests }`, JSON `{ "targetCount": 2, "lastSavedAtUtc": "2026-…Z", "slotRequests": [...] }`; `lastSavedAtUtc = revisedAtUtc ?? submittedAtUtc`.
  - `SlotRequestForIdentifyStudentResponse { SlotId: Guid, SessionType, Constraint: string? }`, JSON `{ "slotId": uuid, "sessionType": "single" | "double", "constraint": string | null }`, **in rank order**.

The interactor is unchanged: it returns what `IStudentQueries.GetActiveByNationalIdAsync` builds, and `IdentifyStudentInteractorTest` keeps passing (`Submission` defaults to `null`). There is no EF test harness in this repo, so queries are proven over HTTP as in every earlier slice. This task's test cycle is the smoke in Steps 3–6.

- [ ] **Step 1: The response carries the saved submission**

Replace `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs` with:

```csharp
using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentResponse
{
    public string StudentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string CarName { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
    public SubmissionForIdentifyStudentResponse? Submission { get; init; }
    public IReadOnlyCollection<SlotForIdentifyStudentResponse> Slots { get; init; } = [];
}

public class SubmissionForIdentifyStudentResponse
{
    public int TargetCount { get; init; }
    public DateTimeOffset LastSavedAtUtc { get; init; }
    public IReadOnlyCollection<SlotRequestForIdentifyStudentResponse> SlotRequests { get; init; } = [];

    public static Expression<Func<Submission, SubmissionForIdentifyStudentResponse>> Selector =>
        x => new SubmissionForIdentifyStudentResponse
        {
            TargetCount = x.TargetCount.Value,
            LastSavedAtUtc = x.RevisedAtUtc ?? x.SubmittedAtUtc,
            SlotRequests = x.SlotRequests
                            .OrderBy(request => request.Rank)
                            .Select(request => new SlotRequestForIdentifyStudentResponse
                            {
                                SlotId = request.SlotId.Value,
                                SessionType = request.SessionType,
                                Constraint = request.Constraint == null ? null : request.Constraint.Value
                            })
                            .ToList()
        };
}

public class SlotRequestForIdentifyStudentResponse
{
    public Guid SlotId { get; init; }
    public SessionType SessionType { get; init; }
    public string? Constraint { get; init; }
}

public class SlotForIdentifyStudentResponse
{
    public Guid Id { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public TimeOnly StartLocal => SlotWindowTimes.StartOf(Window);
    public TimeOnly EndLocal => SlotWindowTimes.EndOf(Window);

    public static Expression<Func<Slot, SlotForIdentifyStudentResponse>> Selector =>
        x => new SlotForIdentifyStudentResponse
        {
            Id = x.Id.Value,
            Day = x.Day,
            Window = x.Window,
            State = x.State
        };
}
```

`OrderBy(request => request.Rank)` orders by the converted `rank` column in SQL. Never write `request.Rank.Value` inside the ordering, because a converted value's `.Value` is not translatable there. The `.Value` reads in the final projection are evaluated client-side, the same as `GetWeekScheduleResponse.Selector` does. No submission or slot-request id is projected (Global Constraints).

- [ ] **Step 2: Load it in `StudentQueries`**

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`, inside `GetActiveByNationalIdAsync`, replace

```csharp
        var submissionsThisWeek = from submission in dbContext.Submissions
                                  join publication in dbContext.Publications
                                      on submission.PublicationId equals publication.Id
                                  where submission.StudentId == studentId
                                        && publication.WeekStart == resolvedWeekStart
                                  select submission.Id;

        var hasSubmission = await submissionsThisWeek.AnyAsync();
```

with

```csharp
        var submissionsThisWeek = from submission in dbContext.Submissions
                                  join publication in dbContext.Publications
                                      on submission.PublicationId equals publication.Id
                                  where submission.StudentId == studentId
                                        && publication.WeekStart == resolvedWeekStart
                                  select submission;

        var savedSubmission = await submissionsThisWeek
                                        .Select(SubmissionForIdentifyStudentResponse.Selector)
                                        .FirstOrDefaultAsync();
```

and in the `return new IdentifyStudentResponse { … }` initializer replace

```csharp
            HasSubmission = hasSubmission,
```

with

```csharp
            Submission = savedSubmission,
```

Publications are unique per week (`publications.week_start` unique index) and submissions are unique per `(publication_id, student_id)`, so there is at most one row. The week join keeps the publication id off the anonymous boundary, as slice 3 did. If EF ever reports the nested `OrderBy` as untranslatable (the smoke in Step 5 fails with a translation error), map in memory instead: `var submission = await submissionsThisWeek.FirstOrDefaultAsync();` loads the entity with its owned slot requests, and you build the same response from it with `OrderBy(request => request.Rank.Value)`. `Rank` is a record without `IComparable`, so compiling the `Selector` in memory would throw. Only fall back to this if Step 5 fails.

- [ ] **Step 3: Build**

Run: `dotnet build` then `dotnet test`
Expected: build clean with no new warnings (no other code read `HasSubmission`), every test PASS.

- [ ] **Step 4: Start the API against a throwaway database and seed it (bash / Git Bash)**

⚠️ The smoke uploads a roster, and **a roster upload deactivates every student missing from the file** (decision #19). Never run it against the dev database (`drivinglessons`).

1. Start the compose Postgres (project memory: `dl-postgres` has a stale migration history) and create the throwaway database:
   ```bash
   docker stop dl-postgres; docker compose up -d postgres
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us25_smoke
   ```
   (If `createdb` reports it exists from an earlier run, drop it first with `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us25_smoke`. It only ever holds this smoke data.)
2. Add a **local-only** entry to `.claude\launch.json` `configurations`. The file already has unrelated local edits; never stage it:
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us25_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first, since both use port 5080). Startup applies every migration to the empty database and seeds the dev admin. `preview_logs` must show no migration error.
4. Seed two teachers, their grids for the next two weeks, a roster and an open week-1 publication. The national IDs are synthetic (`0000000xx` with a valid check digit). Never use real IDs in test data.

```bash
API=http://localhost:5080
WEEK=$(date -u -d 'next sunday' +%F)
WEEK2=$(date -u -d "$WEEK + 7 days" +%F)
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
COHEN_WS=$(new_week $COHEN_ID $WEEK)
new_week $LEVI_ID $WEEK > /dev/null
new_week $COHEN_ID $WEEK2 > /dev/null

ROSTER_DIR=$(mktemp -d)
cat > "$ROSTER_DIR/roster.csv" <<'CSV'
שם מלא,תעודת זהות,טלפון,מורה,רכב
Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
Smoke Student E,000000067,050-0000005,Smoke Cohen,Smoke Auto
CSV
curl -s -w "  ← import: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$ROSTER_DIR/roster.csv;type=text/csv"

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
PUB2=$(curl -s "$API/api/publications/by-week?week=$WEEK2" -H "$AUTH")
PUB2_ID=$(json id <<< "$PUB2")
LINK2=$(json linkToken <<< "$PUB2")

publish $PUB_ID "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)"
sleep 5
echo "week 1 link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
echo "WEEK=$WEEK LINK=$LINK LINK2=$LINK2 PUB_ID=$PUB_ID PUB2_ID=$PUB2_ID COHEN_ID=$COHEN_ID COHEN_WS=$COHEN_WS"
```

Expected:
1. `import: 201` with `"added":3,"updated":0,"deactivated":0,"failed":0`.
2. `publish <id>: 204`, then `week 1 link state: open`. If it still says `published`, wait a few seconds for Quartz to fire the past-due open job.
3. The `WEEK=… LINK=… …` line. **Write these values down**, because tasks 3 and 7 reuse them. Week 2 stays **Draft** until task 3.

Roles: **A** `000000018` → Cohen (automatic, all 22 slots open) · **B** `000000026` → Levi · **E** `000000067` → Cohen, a first-timer kept for the browser in task 7.

- [ ] **Step 5: Smoke: the saved submission follows every create and revise (same shell)**

```bash
identify() { curl -s -X POST "$API/api/submissions/by-link/$1/identify" -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"$2\"}"; }
body() { node -e 'const [nationalId,target,...picks]=process.argv.slice(1);console.log(JSON.stringify({nationalId,targetCount:Number(target),slotRequests:picks.map(p=>{const [slotId,sessionType,...rest]=p.split("|");return {slotId,sessionType,constraint:rest.length?rest.join("|"):null}})}))' "$@"; }
problem() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{if(!s.trim())return;const o=JSON.parse(s);console.log(o.detail??o.title)})"; }
send() {
  local response status content
  response=$(curl -s -w "\n%{http_code}" -X "$1" "$API/api/submissions/by-link/$2" -H "Content-Type: application/json" -d "$3")
  status=$(tail -n1 <<< "$response")
  content=$(sed '$d' <<< "$response")
  echo "$status $(problem <<< "$content")"
}
saved() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const o=JSON.parse(s),v=o.submission;if(!v){console.log('none');return}const at=id=>o.slots.findIndex(x=>x.id===id);console.log('target='+v.targetCount,'|',v.slotRequests.map(r=>at(r.slotId)+':'+r.sessionType+(r.constraint?':'+r.constraint:'')).join(' '))})"; }
keys() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').filter(Boolean).reduce((o,k)=>o?.[k],JSON.parse(s));console.log(Object.keys(v).join(','))})" "$1"; }

A_GRID=$(identify $LINK 000000018)
a() { json "slots.$1.id" <<< "$A_GRID"; }

echo "1  A before submitting:     $(saved <<< "$A_GRID")"
echo "   identify fields:         $(keys '' <<< "$A_GRID")"
echo "2  A create:                $(send POST $LINK "$(body 000000018 2 "$(a 2)|double|only after 16:00" "$(a 7)|single" "$(a 13)|single")")"
AFTER_CREATE=$(identify $LINK 000000018)
echo "   A saved:                 $(saved <<< "$AFTER_CREATE")"
echo "   submission fields:       $(keys submission <<< "$AFTER_CREATE")"
echo "   slot request fields:     $(keys submission.slotRequests.0 <<< "$AFTER_CREATE")"
SAVED1=$(json submission.lastSavedAtUtc <<< "$AFTER_CREATE")
sleep 1
echo "3  A revise #1:             $(send PUT $LINK "$(body 000000018 1 "$(a 5)|single" "$(a 0)|double")")"
AFTER_REVISE1=$(identify $LINK 000000018)
echo "   A saved:                 $(saved <<< "$AFTER_REVISE1")"
SAVED2=$(json submission.lastSavedAtUtc <<< "$AFTER_REVISE1")
echo "   saved time moved on:     $(node -e 'console.log(new Date(process.argv[2]) > new Date(process.argv[1]))' "$SAVED1" "$SAVED2")"
echo "4  A revise #2:             $(send PUT $LINK "$(body 000000018 3 "$(a 9)|single" "$(a 14)|single|pick me up from work" "$(a 20)|double" "$(a 3)|single")")"
echo "   A saved:                 $(identify $LINK 000000018 | saved)"
echo "5  B (never submitted):     $(identify $LINK 000000026 | saved)"
echo "6  E (never submitted):     $(identify $LINK 000000067 | saved)"
```

Expected output, in order:

1. `1  A before submitting: none` and `identify fields: studentName,teacherName,carName,transmission,submission,slots`. `hasSubmission` is gone, and there is still no national ID, phone or id.
2. `2  A create: 204`, then `A saved: target=2 | 2:double:only after 16:00 7:single 13:single`: rank order, session types and the constraint on its own pick (US-25). `submission fields: targetCount,lastSavedAtUtc,slotRequests`, `slot request fields: slotId,sessionType,constraint`, with no submission or slot-request id.
3. `3  A revise #1: 204`, `A saved: target=1 | 5:single 0:double`, `saved time moved on: true` (`lastSavedAtUtc` now reads `revisedAtUtc`).
4. `4  A revise #2: 204`, `A saved: target=3 | 9:single 14:single:pick me up from work 20:double 3:single`. This is US-43: a second revise replaces the first, and the list keeps the order sent, **not** grid order.
5. `5  B (never submitted): none`. **Review Focus 8**: A's submission never leaks to another student.
6. `6  E (never submitted): none`.

- [ ] **Step 6: What was stored, and what the admin sees**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us25_smoke -c "
select st.name, s.target_count, r.rank, r.session_type, r.constraint_text, s.revised_at_utc is not null as revised
from submissions s
join students st on st.id = s.student_id
join slot_requests r on r.submission_id = s.id
order by st.name, r.rank;"
curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$COHEN_ID" -H "$AUTH" \
  | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const o=JSON.parse(s);console.log('Cohen: submitted='+o.studentsSubmitted,'picks='+o.totalPicks)})"
```

Expected: exactly four rows, all `Smoke Student A`, `target_count` 3, ranks 1–4, `session_type` 10 / 10 / 20 / 10, `pick me up from work` on rank 2 only, `revised` `t`. Nothing is left of the create or revise #1 (full replace, US-43). Then `Cohen: submitted=1 picks=4`.

Keep the API running and the shell open (`API`, `AUTH`, `WEEK`, `LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `COHEN_ID`, `COHEN_WS`, the helpers). Task 3 continues here, and task 7 needs A's four saved picks.

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Application/Queries/IdentifyStudent/IdentifyStudentResponse.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs
git commit -m "feat(api): identify returns the student's saved submission

Instead of a yes/no, identify now answers with the week's saved target,
the slot requests in rank order with their session type and constraint,
and when it was last saved, so the student form can load it for editing.
No submission or slot-request id leaves the anonymous endpoint."
```

---

**Next:** [task-03-window-closed-problem-type.md](task-03-window-closed-problem-type.md)
