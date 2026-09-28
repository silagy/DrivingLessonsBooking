# Task 6 of 11: Read side — identify reports an existing submission; real dashboard counts replace the stub

> Part of [US-33…US-41: First Submission](README.md). Requires task 5 complete and its smoke API + shell still running. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs` (+ `HasSubmission`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` (stub → real)

**Interfaces:**
- Consumes (task 3): `DrivingLessonsDbContext.Submissions`, `Submission.PublicationId` / `StudentId` / `WeekScheduleId` / `SubmittedAtUtc` / `RevisedAtUtc` / `SlotRequests`, `SlotRequest.SlotId`. (On `main`): `ISubmissionQueries` (`GetSlotRequestCountsAsync`, `GetStatsAsync`, `SubmissionStats`), consumed unchanged by `GetPublicationDashboardInteractor` and `PlaceholderExcelGenerator`.
- Produces:
  - `IdentifyStudentResponse.HasSubmission : bool` — JSON `hasSubmission`. Task 7's client DTO mirrors it; task 10's store uses it to choose `POST` (create) or `PUT` (revise) — roadmap decision 2 ("the client knows which to call from the identify response"). Slice 5 grows this into the loaded submission.
  - `SubmissionQueries.GetSlotRequestCountsAsync(publicationId, teacherId)` — slot id → number of slot requests, over submissions made against that teacher's week schedule. **A Double counts as one** (requirements §8.1, decision 5): one row per slot request, whatever its session type.
  - `SubmissionQueries.GetStatsAsync(publicationId, teacherId)` — `StudentsSubmitted` (submissions), `TotalPicks` (slot requests), `LastSubmissionAtUtc` (latest of submitted/revised, `null` when none).

No new interface members and no new DTOs for the dashboard: `ISubmissionQueries` was shaped in the publications module for exactly this and has two consumers already wired (the admin dashboard and the placeholder Excel summary sheet), so both start showing real numbers with no further change. There is no EF test harness in this repo (queries are proven over HTTP, like every earlier query implementation), so this task's test cycle is the smoke in Step 4.

- [ ] **Step 1: `hasSubmission` on the identify response**

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
    public bool HasSubmission { get; init; }
    public IReadOnlyCollection<SlotForIdentifyStudentResponse> Slots { get; init; } = [];
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

(Only `HasSubmission` is new.) It is a yes/no — no submission id, count or timestamp leaves this anonymous endpoint.

- [ ] **Step 2: Compute it in `StudentQueries`**

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`, replace the whole `GetActiveByNationalIdAsync` method with:

```csharp
    public async Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        var query = from student in dbContext.Students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    where student.NationalId == nationalId && student.IsActive
                    select new
                    {
                        StudentId = student.Id,
                        TeacherId = teacher.Id,
                        StudentName = student.Name.Value,
                        TeacherName = teacher.Name.Value,
                        CarName = car.Name.Value,
                        car.Transmission
                    };

        var identified = await query.FirstOrDefaultAsync();

        if (identified is null)
        {
            return null;
        }

        var studentId = identified.StudentId;

        var submissionsThisWeek = from submission in dbContext.Submissions
                                  join publication in dbContext.Publications
                                      on submission.PublicationId equals publication.Id
                                  where submission.StudentId == studentId
                                        && publication.WeekStart == resolvedWeekStart
                                  select submission.Id;

        var hasSubmission = await submissionsThisWeek.AnyAsync();

        var slots = await dbContext
                            .WeekSchedules
                            .Where(x => x.TeacherId == identified.TeacherId && x.WeekStart == resolvedWeekStart)
                            .SelectMany(x => x.Slots)
                            .OrderBy(slot => slot.Day)
                            .ThenBy(slot => slot.Window)
                            .Select(SlotForIdentifyStudentResponse.Selector)
                            .ToListAsync();

        return new IdentifyStudentResponse
        {
            StudentName = identified.StudentName,
            TeacherName = identified.TeacherName,
            CarName = identified.CarName,
            Transmission = identified.Transmission,
            HasSubmission = hasSubmission,
            Slots = slots
        };
    }
```

The query keeps its `DateOnly weekStart` parameter: publications are unique per week (`publications.week_start` unique index), so "a submission for this week" is a join on the week — no publication id has to cross the anonymous boundary. `FindAsync` and the usings are unchanged.

- [ ] **Step 3: Replace the `SubmissionQueries` stub**

Replace `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class SubmissionQueries : ISubmissionQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public SubmissionQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var counts = await submissions
                               .SelectMany(x => x.SlotRequests)
                               .GroupBy(x => x.SlotId)
                               .Select(x => new { SlotId = x.Key, Count = x.Count() })
                               .ToListAsync();

        return counts.ToDictionary(x => x.SlotId.Value, x => x.Count);
    }

    public async Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var studentsSubmitted = await submissions.CountAsync();

        var totalPicks = await submissions
                                   .SelectMany(x => x.SlotRequests)
                                   .CountAsync();

        var lastSubmissionAtUtc = await submissions.MaxAsync(x => (DateTimeOffset?)(x.RevisedAtUtc ?? x.SubmittedAtUtc));

        return new SubmissionStats(studentsSubmitted, totalPicks, lastSubmissionAtUtc);
    }

    private IQueryable<Submission> TeacherSubmissions(Guid publicationId, Guid teacherId)
    {
        var resolvedPublicationId = PublicationId.Of(publicationId);
        var resolvedTeacherId = TeacherId.Of(teacherId);

        return from submission in dbContext.Submissions
               join weekSchedule in dbContext.WeekSchedules
                   on submission.WeekScheduleId equals weekSchedule.Id
               where submission.PublicationId == resolvedPublicationId
                     && weekSchedule.TeacherId == resolvedTeacherId
               select submission;
    }
}
```

Notes for the implementer:
- "Per teacher" goes through the submission's **week schedule** (the grid the student picked from), not the student's current teacher — a roster re-upload that rebinds a student must not move their already-submitted slots to a grid they do not belong to (task 2 decision on `WeekScheduleId`).
- `.Value` is unwrapped **after** `ToListAsync()` — in memory, never inside the SQL translation (a converted key's `.Value` is not translatable inside `GroupBy`). If EF ever reports the `GroupBy` over the owned collection as untranslatable, select `x => x.SlotId` into a list and group in memory instead — the per-teacher volume is tens of rows.
- `MaxAsync` over a **nullable** projection returns `null` for a teacher with no submissions instead of throwing on an empty set.
- Deactivated students' submissions still count: deactivation preserves history (requirements §5.5.1), and their slot requests are real demand for that week.

- [ ] **Step 4: Build, restart the smoke API, verify over HTTP**

Run: `dotnet build` and `dotnet test`
Expected: build clean (no new warnings), every test PASS (`IdentifyStudentInteractorTest` is unaffected — `HasSubmission` defaults to `false`).

`preview_stop` then `preview_start {name:"api-smoke"}` (same `drivinglessons_us33_smoke` database, no migration this time). In the task-5 shell (if it is gone, re-declare `API`, `AUTH` (log in again), `LINK`, `LINK2`, `PUB_ID`, `COHEN_ID`, `LEVI_ID`, and the `json` / `identify` helpers from task 5 Steps 4–5 with the values you wrote down):

```bash
counts() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const o=JSON.parse(s);console.log('submitted='+o.studentsSubmitted,'picks='+o.totalPicks,'last='+(o.lastSubmissionAtUtc?'set':'null'),'|',o.slotCounts.filter(c=>c.requestCount).map(c=>c.day+'-'+c.window+':'+c.requestCount).join(' ')||'-')})"; }

identify $LINK 000000018 | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>console.log('fields:',Object.keys(JSON.parse(s)).join(',')))"
echo "A week 1 hasSubmission: $(identify $LINK 000000018 | json hasSubmission)"
echo "B week 1 hasSubmission: $(identify $LINK 000000026 | json hasSubmission)"
echo "A week 2 hasSubmission: $(identify $LINK2 000000018 | json hasSubmission)"
echo "Cohen week 1: $(curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$COHEN_ID" -H "$AUTH" | counts)"
echo "Levi week 1:  $(curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$LEVI_ID" -H "$AUTH" | counts)"
curl -s -o "$ROSTER_DIR/cohen.xlsx" -w "Cohen Excel: %{http_code} %{size_download} bytes\n" \
  "$API/api/publications/$PUB_ID/excel?teacherId=$COHEN_ID" -H "$AUTH"
```

Expected:
1. `fields: studentName,teacherName,carName,transmission,hasSubmission,slots` — still no national ID, phone or ids (slice-2 decision 2).
2. `A week 1 hasSubmission: true`, `B week 1 hasSubmission: true`, `A week 2 hasSubmission: false` (the week-2 window closed before A could submit — identify is still answered, slice 2 open item 1).
3. `Cohen week 1: submitted=1 picks=5 last=set | sunday-morning:1 monday-noon:1 tuesday-noon:1 wednesday-afternoon:1 friday-morning:1` — A's revised list (task 5 check 3); the Monday-noon pick is a **Double and counts as 1** (§8.1). D contributed nothing (refused). A's three pre-revision picks are gone.
4. `Levi week 1: submitted=1 picks=1 | sunday-noon:1`.
5. `Cohen Excel: 200 …` — optionally open `cohen.xlsx`: the *Summary* sheet shows `1` in those five cells and `0` elsewhere (the detail sheet stays the Excel module's placeholder — README open item 6).

Keep the API running — task 11 reuses this database (A and B already have submissions; that is what the "replaces your earlier list" path needs).

- [ ] **Step 5: Commit**

```bash
git add src/DrivingLessons.Application/Queries/IdentifyStudent/IdentifyStudentResponse.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/SubmissionQueries.cs
git commit -m "feat(api): real submission counts and hasSubmission on identify

The admin dashboard and Excel summary now count slot requests per slot
for each teacher's grid (a double counts once), with submitted students,
total picks and the latest submission time. Identify tells the student
form whether this week's submission already exists."
```

---

**Next:** [task-07-client-domain-data.md](task-07-client-domain-data.md)
