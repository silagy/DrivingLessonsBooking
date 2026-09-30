# Task 2 of 3: The detail query and the generator — real rows in every downloaded and emailed file

> Part of [US-46: Request Detail Sheet](README.md). Requires task 1 committed. Work on branch `46-us-46-excel-detail-sheet`, commands from the repo root in bash.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs` (+ `GetSlotRequestDetailsAsync`)
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` (+ implementation)
- Rename + modify: `src\DrivingLessons.Infrastructure\Excel\PlaceholderExcelGenerator.cs` → `ExcelGenerator.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs`
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry)

**Interfaces:**
- Consumes (task 1): `SlotRequestDetail` (`DrivingLessons.Application.Queries`), `RequestDetailSheet.AddTo(XLWorkbook, IReadOnlyCollection<SlotRequestDetail>)`, `RequestDetailSheet.Name` (`"פירוט בקשות"`). (On `main`): `ISubmissionQueries.GetSlotRequestCountsAsync` and its private `TeacherSubmissions(publicationId, teacherId)` scope; `IExcelGenerator.GenerateAsync(PublicationId, TeacherId) : Task<ExcelFile>`; `IPublicationRepository.GetAsync(PublicationId) : Task<Publication?>`; `IWeekScheduleQueries.GetByTeacherAndWeekAsync(Guid, DateOnly) : Task<GetWeekScheduleResponse?>`.
- Produces:
  - `ISubmissionQueries.GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId) : Task<IReadOnlyList<SlotRequestDetail>>` — one record per slot request of the submissions made against that teacher's week schedule for the publication; unsorted (the sheet sorts).
  - `ExcelGenerator : IExcelGenerator` (replaces `PlaceholderExcelGenerator`; same constructor dependencies) — slice 2 (US-45) rewrites its summary sheet, slice 3 (US-44) attaches its file.

Both consumers of `IExcelGenerator` — `DownloadPublicationExcelInteractor` (admin download) and `PublicationClosedHandler` (every close) — are unchanged: they get the new sheet through DI.

- [ ] **Step 1: Write the failing generator tests**

Create `tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs`:

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Excel;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class ExcelGeneratorTest
{
    private IPublicationRepository publicationRepository = null!;
    private IWeekScheduleQueries weekScheduleQueries = null!;
    private ISubmissionQueries submissionQueries = null!;
    private ExcelGenerator generator = null!;
    private Publication publication = null!;
    private TeacherId teacherId = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        weekScheduleQueries = A.Fake<IWeekScheduleQueries>();
        submissionQueries = A.Fake<ISubmissionQueries>();
        generator = new ExcelGenerator(publicationRepository, weekScheduleQueries, submissionQueries);
        publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        teacherId = TeacherId.New();

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);
        A.CallTo(() => weekScheduleQueries.GetByTeacherAndWeekAsync(A<Guid>._, A<DateOnly>._))
            .Returns((GetWeekScheduleResponse?)null);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(A<Guid>._, A<Guid>._))
            .Returns(new Dictionary<Guid, int>());
        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(A<Guid>._, A<Guid>._))
            .Returns(Array.Empty<SlotRequestDetail>());
    }

    [TestMethod]
    public async Task Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication()
    {
        //given
        var request = new SlotRequestDetail(
            DayOfWeek.Sunday,
            SlotWindowType.Morning,
            "Smoke Student A",
            "000000018",
            "050-0000001",
            Transmission.Automatic,
            SessionType.Single,
            1,
            2,
            null);

        A.CallTo(() => submissionQueries.GetSlotRequestDetailsAsync(publication.Id.Value, teacherId.Value))
            .Returns([request]);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var detail = WorkbookOf(excel).Worksheet(RequestDetailSheet.Name);
        detail.LastRowUsed()!.RowNumber().ShouldBe(2);
        detail.Cell(2, 3).GetText().ShouldBe("Smoke Student A");
        detail.Cell(2, 4).GetText().ShouldBe("000000018");
    }

    [TestMethod]
    public async Task Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        WorkbookOf(excel).Worksheets.Select(x => x.Name).ShouldBe(
            [
                "Summary",
                RequestDetailSheet.Name
            ]);
    }

    [TestMethod]
    public async Task Keeps_The_File_Name_And_Content_Type()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        excel.FileName.ShouldBe($"week-2026-10-04-{teacherId.Value}.xlsx");
        excel.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static XLWorkbook WorkbookOf(ExcelFile excel)
    {
        var stream = new MemoryStream(excel.Content);

        return new XLWorkbook(stream);
    }
}
```

The `Init` defaults answer every other publication/teacher pair with **no** rows, so the first test only passes if the generator asks for exactly the publication and teacher it was given — the scope that keeps another teacher's students off this file (README Review Focus 4). `Keeps_The_File_Name_And_Content_Type` pins what the rename must not change: the admin download and the email attachment both use them.

- [ ] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ExcelGeneratorTest"`
Expected: FAIL — the build breaks with `CS0246: The type or namespace name 'ExcelGenerator' could not be found` and `CS1061: 'ISubmissionQueries' does not contain a definition for 'GetSlotRequestDetailsAsync'`.

- [ ] **Step 3: The query member**

In `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs`, replace the interface with:

```csharp
public interface ISubmissionQueries
{
    Task<IReadOnlyDictionary<Guid, int>> GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId);

    Task<SubmissionStats> GetStatsAsync(Guid publicationId, Guid teacherId);

    Task<IReadOnlyList<SlotRequestDetail>> GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId);
}
```

(`SubmissionStats` and `SlotRequestDetail` below it stay as they are.)

- [ ] **Step 4: Implement it in `SubmissionQueries`**

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\SubmissionQueries.cs`, add this method between `GetStatsAsync` and the private `TeacherSubmissions`:

```csharp
    public async Task<IReadOnlyList<SlotRequestDetail>> GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId)
    {
        var submissions = TeacherSubmissions(publicationId, teacherId);

        var query = from submission in submissions
                    join weekSchedule in dbContext.WeekSchedules
                        on submission.WeekScheduleId equals weekSchedule.Id
                    join student in dbContext.Students
                        on submission.StudentId equals student.Id
                    join car in dbContext.Cars.IgnoreQueryFilters()
                        on student.CarId equals car.Id
                    from request in submission.SlotRequests
                    from slot in weekSchedule.Slots
                    where slot.Id == request.SlotId
                    select new
                    {
                        slot.Day,
                        slot.Window,
                        StudentName = student.Name,
                        student.NationalId,
                        student.Phone,
                        car.Transmission,
                        request.SessionType,
                        request.Rank,
                        submission.TargetCount,
                        request.Constraint
                    };

        var rows = await query.ToListAsync();

        return rows
                   .Select(x => new SlotRequestDetail(
                       x.Day,
                       x.Window,
                       x.StudentName.Value,
                       x.NationalId.Value,
                       x.Phone.Value,
                       x.Transmission,
                       x.SessionType,
                       x.Rank.Value,
                       x.TargetCount.Value,
                       x.Constraint?.Value))
                   .ToList();
    }
```

Why it is shaped this way:
- `TeacherSubmissions(…)` is the scope the summary counts and the dashboard already use (submissions made **against this teacher's week schedule** for the publication), so the two sheets can never disagree about whose requests they hold (roadmap decision 3).
- `Students` has no query filter, so deactivated students stay (roadmap decision 4). `Cars` filters soft-deleted cars; `IgnoreQueryFilters()` applies to the whole query, and no other entity in it has a filter, so it only lifts the car filter (roadmap decision 5) — a plain `join car in dbContext.Cars` would silently drop the rows of a student whose car was deleted.
- The value objects (`StudentName`, `NationalId`, `PhoneNumber`, `Rank`, `TargetSessionCount`, `SlotConstraint?`) are projected whole — EF materializes them through their converters — and unwrapped in memory, so nothing depends on EF translating `.Value` through a converter.
- There is no `orderby`: the order is a sheet rule, pinned by task 1's tests (README decision 3).

If EF refuses to translate `from slot in weekSchedule.Slots where slot.Id == request.SlotId` (it translates it to an inner join on `slots` in EF Core 10), fall back to loading the week schedule's slots separately with `dbContext.WeekSchedules.Where(x => x.TeacherId == …).SelectMany(x => x.Slots)` and matching `SlotId` in memory — the result must be the same rows.

- [ ] **Step 5: Rename the generator and give it the real detail sheet**

```bash
git mv src/DrivingLessons.Infrastructure/Excel/PlaceholderExcelGenerator.cs src/DrivingLessons.Infrastructure/Excel/ExcelGenerator.cs
```

Replace the contents of `src\DrivingLessons.Infrastructure\Excel\ExcelGenerator.cs` with:

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public class ExcelGenerator : IExcelGenerator
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly DayOfWeek[] Days =
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    private static readonly SlotWindowType[] Windows =
    [
        SlotWindowType.Morning,
        SlotWindowType.Noon,
        SlotWindowType.Afternoon,
        SlotWindowType.Evening
    ];

    private readonly IPublicationRepository publicationRepository;
    private readonly IWeekScheduleQueries weekScheduleQueries;
    private readonly ISubmissionQueries submissionQueries;

    public ExcelGenerator(
        IPublicationRepository publicationRepository,
        IWeekScheduleQueries weekScheduleQueries,
        ISubmissionQueries submissionQueries)
    {
        this.publicationRepository = publicationRepository;
        this.weekScheduleQueries = weekScheduleQueries;
        this.submissionQueries = submissionQueries;
    }

    public async Task<ExcelFile> GenerateAsync(PublicationId publicationId, TeacherId teacherId)
    {
        var publication = await publicationRepository.GetAsync(publicationId)
                          ?? throw new PublicationNotFoundException(publicationId);

        var weekStart = publication.WeekStart.Value;
        var teacherGuid = teacherId.Value;

        var schedule = await weekScheduleQueries.GetByTeacherAndWeekAsync(teacherGuid, weekStart);
        var counts = await submissionQueries.GetSlotRequestCountsAsync(publicationId.Value, teacherGuid);
        var requests = await submissionQueries.GetSlotRequestDetailsAsync(publicationId.Value, teacherGuid);

        using var workbook = new XLWorkbook();

        BuildSummarySheet(workbook, schedule, counts);
        RequestDetailSheet.AddTo(workbook, requests);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"week-{weekStart:yyyy-MM-dd}-{teacherGuid}.xlsx";

        return new ExcelFile(fileName, content, ContentType);
    }

    private static void BuildSummarySheet(
        XLWorkbook workbook,
        GetWeekScheduleResponse? schedule,
        IReadOnlyDictionary<Guid, int> counts)
    {
        var sheet = workbook.Worksheets.Add("Summary");

        for (var column = 0; column < Days.Length; column++)
        {
            sheet.Cell(1, column + 2).Value = Days[column].ToString();
        }

        var slotsByCell = schedule?
                              .Slots
                              .ToDictionary(slot => (slot.Day, slot.Window))
                          ?? [];

        for (var row = 0; row < Windows.Length; row++)
        {
            var window = Windows[row];
            sheet.Cell(row + 2, 1).Value = window.ToString();

            for (var column = 0; column < Days.Length; column++)
            {
                var day = Days[column];
                var cell = sheet.Cell(row + 2, column + 2);

                if (!slotsByCell.TryGetValue((day, window), out var slot))
                {
                    continue;
                }

                if (slot.State == SlotState.Unavailable)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    continue;
                }

                cell.Value = counts.TryGetValue(slot.Id, out var count) ? count : 0;
            }
        }

        sheet.Columns().AdjustToContents();
    }
}
```

The summary sheet is copied over unchanged (README decision 6 — US-45 converts it); the old `BuildDetailSheet` stub is gone, replaced by `RequestDetailSheet.AddTo`. The workbook is now disposed (`using var`), which the placeholder never did.

- [ ] **Step 6: Register it**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, replace

```csharp
        services.AddScoped<IExcelGenerator, PlaceholderExcelGenerator>();
```

with

```csharp
        services.AddScoped<IExcelGenerator, ExcelGenerator>();
```

Then confirm nothing else names the old class: `grep -rn "PlaceholderExcelGenerator" src tests --include=*.cs` → no output.

- [ ] **Step 7: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ExcelGeneratorTest|FullyQualifiedName~RequestDetailSheetTest"`
Expected: PASS — 27 tests (3 generator + 24 sheet).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS. `dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web` → `No changes have been made to the model since the last migration.`

- [ ] **Step 8: Smoke API on a throwaway database**

1. Create the database (the compose container must be running — `docker ps` shows `drivinglessonsbooking-postgres-1`):
   ```bash
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us46_smoke
   ```
   (If it reports the database exists from an earlier run, drop it first: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us46_smoke`. It only ever holds this smoke data.)
2. Add a **local-only** entry to `.claude\launch.json` `configurations` (the file has unrelated local edits — never stage it):
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us46_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first — both use port 5080). Startup applies every migration to the empty database and seeds the dev admin; `preview_logs` must show no migration error.
4. Write the xlsx reader (Windows `tar.exe` unpacks the file, Node reads the sheet XML — no new tooling):
   ```bash
   SMOKE=.superpowers/sdd/us-46-smoke
   mkdir -p "$SMOKE"
   cat > "$SMOKE/detail-rows.js" <<'JS'
   const fs = require('fs');
   const path = require('path');
   const dir = process.argv[2];
   const read = file => fs.readFileSync(path.join(dir, file), 'utf8');
   const decode = text => text
       .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, '&');
   const texts = xml => [...xml.matchAll(/<(?:\w+:)?t(?:\s[^>]*)?>([\s\S]*?)<\/(?:\w+:)?t>/g)].map(m => decode(m[1])).join('');
   const shared = fs.existsSync(path.join(dir, 'xl/sharedStrings.xml'))
       ? [...read('xl/sharedStrings.xml').matchAll(/<(?:\w+:)?si>([\s\S]*?)<\/(?:\w+:)?si>/g)].map(m => texts(m[1]))
       : [];
   const workbook = read('xl/workbook.xml');
   const rels = read('xl/_rels/workbook.xml.rels');
   const sheetTag = [...workbook.matchAll(/<(?:\w+:)?sheet\s[^>]*>/g)].map(m => m[0]).find(tag => tag.includes('name="פירוט בקשות"'));
   const relId = sheetTag.match(/r:id="([^"]+)"/)[1];
   const target = [...rels.matchAll(/<Relationship\s[^>]*>/g)].map(m => m[0]).find(tag => tag.includes(`Id="${relId}"`)).match(/Target="([^"]+)"/)[1];
   const sheet = read(path.posix.join('xl', target.replace(/^\/?xl\//, '')));
   console.log(`rightToLeft=${/rightToLeft="(1|true)"/.test(sheet)}`);
   const rows = new Map();
   for (const m of sheet.matchAll(/<(?:\w+:)?c r="([A-Z]+)(\d+)"([^>]*?)(?:\/>|>([\s\S]*?)<\/(?:\w+:)?c>)/g)) {
       const [, column, row, attributes, body = ''] = m;
       const type = (attributes.match(/t="(\w+)"/) || [])[1];
       const value = (body.match(/<(?:\w+:)?v>([\s\S]*?)<\/(?:\w+:)?v>/) || [])[1];
       const text = type === 's' ? shared[Number(value)] : type === 'inlineStr' ? texts(body) : decode(value ?? '');
       if (!rows.has(row)) rows.set(row, {});
       rows.get(row)[column] = text;
   }
   for (const [row, cells] of [...rows].sort((a, b) => a[0] - b[0])) {
       console.log(row, '|', 'ABCDEFGHIJ'.split('').map(column => cells[column] ?? '').join(' | '));
   }
   JS
   ```
5. In a bash shell, set up the week (two teachers, three cars, four roster students, one open publication):
   ```bash
   API=http://localhost:5080
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
   import() { curl -s -w "  ← import $1: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
     -F "file=@$SMOKE/$1;type=text/csv"; }

   WEEK=$(date -u -d 'next sunday' +%F)
   COHEN_ID=$(new_teacher "Smoke Cohen" smoke.cohen@example.com)
   LEVI_ID=$(new_teacher "Smoke Levi" smoke.levi@example.com)
   new_car "Smoke Auto" automatic > /dev/null
   new_car "Smoke Manual" manual > /dev/null
   OLD_CAR_ID=$(new_car "Smoke Old" manual)
   COHEN_WS=$(new_week $COHEN_ID $WEEK)
   new_week $LEVI_ID $WEEK > /dev/null

   cat > "$SMOKE/roster.csv" <<'CSV'
   שם מלא,תעודת זהות,טלפון,מורה,רכב
   Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
   Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
   דנה כהן,000000034,050-0000003,Smoke Cohen,Smoke Old
   Smoke Student E,000000067,050-0000005,Smoke Cohen,Smoke Auto
   CSV
   grep -v '^Smoke Student E,' "$SMOKE/roster.csv" > "$SMOKE/roster-without-e.csv"
   import roster.csv

   PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
   PUB_ID=$(json id <<< "$PUB")
   LINK=$(json linkToken <<< "$PUB")
   curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
     -H "Content-Type: application/json" \
     -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
   sleep 5
   echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"
   ```
   Expected: `import roster.csv: 201` with `"added":4,"updated":0,"deactivated":0,"failed":0`; `publish: 204`; `link state: open` (if it still says `published`, wait a few seconds — Quartz fires the past-due open job).

- [ ] **Step 9: Submit, change the roster, read both teachers' files (same shell)**

```bash
identify() { curl -s -X POST "$API/api/submissions/by-link/$LINK/identify" -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"$1\"}"; }
body() { node -e 'const [nationalId,target,...picks]=process.argv.slice(1);console.log(JSON.stringify({nationalId,targetCount:Number(target),slotRequests:picks.map(p=>{const [slotId,sessionType,...rest]=p.split("|");return {slotId,sessionType,constraint:rest.length?rest.join("|"):null}})}))' "$@"; }
submit() { curl -s -o /dev/null -w "%{http_code}" -X POST "$API/api/submissions/by-link/$LINK" \
  -H "Content-Type: application/json" -d "$1"; }
download() { curl -s -o "$SMOKE/$1.xlsx" -w "$1 Excel: %{http_code} %{size_download} bytes\n" \
  "$API/api/publications/$PUB_ID/excel?teacherId=$2" -H "$AUTH"; }
rows() { rm -rf "$SMOKE/$1" && mkdir -p "$SMOKE/$1" && /c/Windows/System32/tar.exe -xf "$SMOKE/$1.xlsx" -C "$SMOKE/$1" \
  && node "$SMOKE/detail-rows.js" "$SMOKE/$1"; }

COHEN_GRID=$(identify 000000018)
cohen() { json "slots.$1.id" <<< "$COHEN_GRID"; }
LEVI_GRID=$(identify 000000026)
levi() { json "slots.$1.id" <<< "$LEVI_GRID"; }

echo "A:    $(submit "$(body 000000018 2 "$(cohen 0)|single" "$(cohen 21)|double|רק אחרי 16:00" "$(cohen 4)|single")")"
echo "E:    $(submit "$(body 000000067 1 "$(cohen 0)|single|=1+1" "$(cohen 3)|single")")"
echo "Dana: $(submit "$(body 000000034 1 "$(cohen 0)|double")")"

download levi-before $LEVI_ID
rows levi-before

echo "B:    $(submit "$(body 000000026 1 "$(levi 1)|single")")"
import roster-without-e.csv
curl -s -o /dev/null -w "block A's Friday noon: %{http_code}\n" -X POST \
  "$API/api/week-schedules/$COHEN_WS/slots/$(cohen 21)/mark-unavailable" -H "$AUTH"
curl -s -o /dev/null -w "delete Smoke Old: %{http_code}\n" -X DELETE "$API/api/cars/$OLD_CAR_ID" -H "$AUTH"

download cohen $COHEN_ID
rows cohen
download levi $LEVI_ID
rows levi
curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$COHEN_ID" -H "$AUTH" | json totalPicks
```

Identify lists the grid Sunday → Friday, Morning → Evening, so `cohen 0` is Sunday Morning, `cohen 3` Sunday Evening, `cohen 4` Monday Morning and `cohen 21` Friday Noon.

Expected, in order:
1. `A:    204`, `E:    204`, `Dana: 204`.
2. `levi-before Excel: 200 …`, then `rightToLeft=true` and **only** row `1` — the Hebrew header `יום | משבצת | שם התלמיד/ה | תעודת זהות | טלפון | תיבת הילוכים | סוג שיעור | דירוג | יעד שיעורים | אילוצים` (check 7: a teacher with no submissions gets the header only).
3. `B:    204`; `import roster-without-e.csv: 201` with `"deactivated":1`; `block A's Friday noon: 204`; `delete Smoke Old: 204`.
4. `cohen Excel: 200 …`, `rightToLeft=true`, the header row, then exactly:
   ```
   2 | ראשון | בוקר | Smoke Student A | 000000018 | 050-0000001 | אוטומטי | יחיד | 1 | 2 |
   3 | ראשון | בוקר | Smoke Student E | 000000067 | 050-0000005 | אוטומטי | יחיד | 1 | 1 | =1+1
   4 | ראשון | בוקר | דנה כהן | 000000034 | 050-0000003 | ידני | כפול | 1 | 1 |
   5 | ראשון | ערב | Smoke Student E | 000000067 | 050-0000005 | אוטומטי | יחיד | 2 | 1 |
   6 | שני | בוקר | Smoke Student A | 000000018 | 050-0000001 | אוטומטי | יחיד | 3 | 2 |
   7 | שישי | צהריים | Smoke Student A | 000000018 | 050-0000001 | אוטומטי | כפול | 2 | 2 | רק אחרי 16:00
   ```
5. `levi Excel: 200 …`, the header, then exactly one row: `2 | ראשון | צהריים | Smoke Student B | 000000026 | 050-0000002 | ידני | יחיד | 1 | 1 |`.
6. The dashboard's `totalPicks` is `6` — the same six requests as rows 2–7.

What each part of step 4 proves:
- **Check 1** (whole block) — every requested field, the spec's order: day, then slot, then rank — not student and not submission order.
- **Check 2** (rows 2–4) — three students at rank 1 on the same slot, in the stable ordinal name order (Latin before Hebrew).
- **Check 3** (rows 3 and 7) — `=1+1` arrives as text, not `2`; the Hebrew constraint arrives intact.
- **Check 4** (rows 3, 5 and 7) — E was deactivated by the second upload and is still listed; A's Friday Noon was marked Unavailable after A picked it and is still listed (roadmap decision 4).
- **Check 5** (row 4) — Dana's car was soft-deleted and her row still says `ידני` (roadmap decision 5).
- **Check 6** (step 5) — Levi's file holds Levi's student only; no Cohen student leaks across.
- Leading zeros (`000000018`) and dashes (`050-0000001`) survive in every row (README Review Focus 1).

Finally, the national ID never reached a URL or a log line: `preview_logs` with `search: "0000000"` → no line.

Keep the API running, the smoke database, and the shell (`API`, `AUTH`, `PUB_ID`, `COHEN_ID`, `LEVI_ID`, `SMOKE`, the helpers) — task 3 continues here. `$SMOKE/cohen.xlsx` is the file task 3 hands to your human partner.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/Queries/ISubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/EntityFramework/Queries/SubmissionQueries.cs \
  src/DrivingLessons.Infrastructure/Excel/ExcelGenerator.cs \
  src/DrivingLessons.Infrastructure/DependencyInjection.cs \
  tests/DrivingLessons.Application.Test/Excel/ExcelGeneratorTest.cs
git commit -m "feat(excel): detail sheet lists the teacher's slot requests

GetSlotRequestDetailsAsync reads every slot request made against the
teacher's week schedule, with the student's roster details and car
transmission, including deactivated students and deleted cars.
PlaceholderExcelGenerator becomes ExcelGenerator and writes the sheet
into every downloaded and emailed file."
```

(Step 5's `git mv` already staged the old path's removal; before committing, `git status` must show `renamed: …PlaceholderExcelGenerator.cs -> …ExcelGenerator.cs` and nothing under `.claude\`.)

---

**Next:** [task-03-verification-and-pr.md](task-03-verification-and-pr.md)
