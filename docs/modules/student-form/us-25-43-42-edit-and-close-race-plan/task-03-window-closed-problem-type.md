# Task 3 of 7: A machine-readable "window closed" problem (+ smoke: POST/PUT after close, nothing saved)

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires task 2 complete and its smoke API and shell still running. Work on branch `26-us-25-43-42-edit-and-close-race`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Presentation.Web\Filters\ProblemTypes.cs`
- Modify: `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs`

**Interfaces:**
- Consumes: `SubmissionWindowMustBeOpenException` (task 1 throws it for a closed state **and** a passed end time), `DomainException`, `NotFoundException`, `AuthenticationFailedException`.
- Produces (task 4's `isWindowClosedProblem` matches this exactly):
  - `ProblemTypes.SubmissionWindowClosed = "problems/submission-window-closed"`.
  - A 409 caused by `SubmissionWindowMustBeOpenException` answers `{ "type": "problems/submission-window-closed", "title": "Conflict", "status": 409, "detail": "Submissions are accepted only while the week's submission window is open." }`.
  - **Every other problem is unchanged**: `type` is absent (null), and status, title and detail stay as before (README decision 7).

The type is a relative URI reference (RFC 9457 allows that). It names the reason for the client, and nothing needs to resolve it. It is keyed on one exception, explicitly, in the filter that already owns status mapping (api-guidelines: "the exception filter owns it"). No per-exception catalogue and no change to `DomainException`. There is no Presentation test project (controllers and the filter are proven over HTTP, as in every slice), so this task's test cycle is the smoke in Step 4.

- [ ] **Step 1: The problem type constant**

Create `src\DrivingLessons.Presentation.Web\Filters\ProblemTypes.cs`:

```csharp
namespace DrivingLessons.Presentation.Web.Filters;

public static class ProblemTypes
{
    public const string SubmissionWindowClosed = "problems/submission-window-closed";
}
```

- [ ] **Step 2: The filter stamps it**

Replace `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` with:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        (int StatusCode, string Title, string? Type)? mapping = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", null),
            SubmissionWindowMustBeOpenException =>
                (StatusCodes.Status409Conflict, "Conflict", ProblemTypes.SubmissionWindowClosed),
            DomainException => (StatusCodes.Status409Conflict, "Conflict", null),
            _ => null
        };

        if (mapping is null)
        {
            return;
        }

        var (statusCode, title, type) = mapping.Value;
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        };

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
```

The `SubmissionWindowMustBeOpenException` arm must stay **above** `DomainException`, because switch arms match top-down and the exception is a `DomainException`. The declared tuple type makes the switch target-typed, so the `null` elements need no casts.

- [ ] **Step 3: Build and restart the smoke API**

Run: `dotnet build` then `dotnet test`
Expected: build clean with no new warnings, every test PASS.

`preview_stop` then `preview_start {name:"api-smoke"}` (same `drivinglessons_us25_smoke` database). In the task-2 shell (if it is gone, re-declare `API`, `AUTH` (log in again), `LINK`, `LINK2`, `PUB_ID`, `PUB2_ID`, `json`, `identify`, `body`, `publish` from task 2 Steps 4–5 with the values you wrote down), add:

```bash
sendt() {
  local response status content
  response=$(curl -s -w "\n%{http_code}" -X "$1" "$API/api/submissions/by-link/$2" -H "Content-Type: application/json" -d "$3")
  status=$(tail -n1 <<< "$response")
  content=$(sed '$d' <<< "$response")
  echo "$status $(node -e "const s=process.argv[1];if(!s.trim()){console.log('-');process.exit()}const o=JSON.parse(s);console.log('type='+(o.type??'none'),'|',o.detail??o.title)" "$content")"
}
```

- [ ] **Step 4: Smoke: the window closes between load and submit (US-42)**

Publish week 2 with a window that ends about 75 seconds from now. While it is open, give A a week-2 submission, load both grids and probe two ordinary 409s and a 404. Then wait for the close and submit again as A (a `PUT`, returning) and as E (a `POST`, first time).

```bash
publish $PUB2_ID "$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)" "$(date -u -d '+75 seconds' +%Y-%m-%dT%H:%M:%SZ)"
sleep 5
echo "week 2 state: $(curl -s $API/api/submissions/by-link/$LINK2 | json state)"
A2_GRID=$(identify $LINK2 000000018)
E2_GRID=$(identify $LINK2 000000067)
a2() { json "slots.$1.id" <<< "$A2_GRID"; }
e2() { json "slots.$1.id" <<< "$E2_GRID"; }

echo "1  A week-2 create (open):      $(sendt POST $LINK2 "$(body 000000018 1 "$(a2 4)|double|only after 16:00")")"
echo "2  A create again (open):       $(sendt POST $LINK2 "$(body 000000018 1 "$(a2 5)|single")")"
echo "3  A same slot twice (open):    $(sendt PUT $LINK2 "$(body 000000018 1 "$(a2 5)|single" "$(a2 5)|double")")"
echo "4  unknown link:                $(sendt POST NoSuchTokenAbc123 "$(body 000000018 1 "$(a2 5)|single")")"

for i in $(seq 1 30); do [ "$(curl -s $API/api/submissions/by-link/$LINK2 | json state)" = closed ] && break; sleep 5; done
echo "week 2 state: $(curl -s $API/api/submissions/by-link/$LINK2 | json state)"
echo "5  A revise after the close:    $(sendt PUT $LINK2 "$(body 000000018 2 "$(a2 7)|single" "$(a2 9)|single")")"
echo "6  E create after the close:    $(sendt POST $LINK2 "$(body 000000067 1 "$(e2 2)|single")")"
echo "7  A still sees the old list:   $(identify $LINK2 000000018 | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=JSON.parse(s).submission;console.log('target='+v.targetCount,'requests='+v.slotRequests.length,v.slotRequests[0].sessionType,v.slotRequests[0].constraint)})")"
```

Expected output, in order:

1. `publish …: 204`, `week 2 state: open`.
2. `1 … 204 -`.
3. `2 … 409 type=none | A submission already exists for this student and week.`
4. `3 … 409 type=none | A slot can be requested at most once per submission.` This and check 2 are **Review Focus 7**: ordinary rule violations carry no type, so the client keeps its inline rejection for them.
5. `4 … 404 type=none | No publication is available for this link.`
6. Within about two minutes, `week 2 state: closed`.
7. `5 … 409 type=problems/submission-window-closed | Submissions are accepted only while the week's submission window is open.`
8. `6 … 409 type=problems/submission-window-closed | …` (same detail).
9. `7 … target=1 requests=1 double only after 16:00`. Identify still answers after close (slice 2 open item 1), with A's pre-close list.

If the state never reaches `closed`, check the `ClosePublicationJob` lines in `preview_logs` before suspecting the filter. The seconds **between** the end time and the job are covered by task 1's `Window_That_Has_Ended_Is_Rejected_Before_The_Close_Job_Runs`. That gap is too narrow to hit reliably by hand.

- [ ] **Step 5: Nothing partial was saved**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us25_smoke -c "
select st.name, p.week_start, s.target_count, r.rank, r.session_type, r.constraint_text, s.revised_at_utc is null as never_revised
from submissions s
join students st on st.id = s.student_id
join publications p on p.id = s.publication_id
join slot_requests r on r.submission_id = s.id
order by p.week_start, st.name, r.rank;"
```

Expected: A's four week-1 rows from task 2 (target 3, `never_revised` `f`), then **exactly one** week-2 row: `Smoke Student A`, target 1, rank 1, `session_type` 20, `only after 16:00`, `never_revised` `t`. The refused `PUT` changed nothing (**Review Focus 4**, "no partial data is saved"), and E has no row at all.

- [ ] **Step 6: Scalar check**

Open `http://localhost:5080/scalar/v1` → **Submissions** → `POST /api/submissions/by-link/{token}/identify`: the 200 schema shows `submission` as a nullable `SubmissionForIdentifyStudentResponse` with `targetCount`, `lastSavedAtUtc`, `slotRequests[]` (`slotId`, `sessionType` as the `single | double` string enum, nullable `constraint`), and no `hasSubmission`.

Keep the API running. Task 7 reuses this database (A has saved lists on both weeks, E has none, week 2 is closed).

- [ ] **Step 7: Commit**

```bash
git add src/DrivingLessons.Presentation.Web/Filters/ProblemTypes.cs src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs
git commit -m "feat(api): name the window-closed conflict in the problem type

A submission refused because the window has closed answers 409 with the
problem type problems/submission-window-closed, so the student form can
tell it apart from every other rule violation. All other problems are
unchanged."
```

---

**Next:** [task-04-client-domain-data.md](task-04-client-domain-data.md)
