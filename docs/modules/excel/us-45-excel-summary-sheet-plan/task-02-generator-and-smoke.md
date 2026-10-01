# Task 2 of 3: The generator writes the summary sheet — every downloaded and emailed file, checked against the dashboard

> Part of [US-45: Summary Sheet](README.md). Requires task 1 committed. Work on branch `45-us-45-excel-summary-sheet`, commands from the repo root in bash.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\Excel\ExcelGenerator.cs` (English `BuildSummarySheet` → `SummarySheet.AddTo`)
- Test: `tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs` (sheet name, + 2 tests)
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry)

**Interfaces:**
- Consumes (task 1): `SummarySheet.AddTo(XLWorkbook, DateOnly, IReadOnlyCollection<SlotForGetWeekScheduleResponse>, IReadOnlyDictionary<Guid, int>)`, `SummarySheet.Name` (`"סיכום"`). (On `main`): `ExcelGenerator(IPublicationRepository, IWeekScheduleQueries, ISubmissionQueries)`; `IWeekScheduleQueries.GetByTeacherAndWeekAsync(Guid teacherId, DateOnly weekStart) : Task<GetWeekScheduleResponse?>`; `ISubmissionQueries.GetSlotRequestCountsAsync(Guid publicationId, Guid teacherId) : Task<IReadOnlyDictionary<Guid, int>>`; `RequestDetailSheet.AddTo` / `RequestDetailSheet.Name` (slice 1).
- Produces: `ExcelGenerator` whose workbook is `[סיכום, פירוט בקשות]`; constructor, file name and content type unchanged — so `DownloadPublicationExcelInteractor`, `PublicationClosedHandler` and DI need no change. Slice 3 (US-44) attaches this file.

- [x] **Step 1: Write the failing generator tests**

In `tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs`:

1. In `Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet`, replace

   ```csharp
                   "Summary",
   ```

   with

   ```csharp
                   SummarySheet.Name,
   ```

2. Insert these two tests directly above `[TestMethod] public async Task Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet()`:

```csharp
    [TestMethod]
    public async Task Summary_Sheet_Counts_The_Requests_Of_The_Teacher_And_Publication()
    {
        //given
        var slot = new SlotForGetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            Day = DayOfWeek.Sunday,
            Window = SlotWindowType.Morning,
            State = SlotState.Open
        };
        var schedule = new GetWeekScheduleResponse
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId.Value,
            WeekStart = publication.WeekStart.Value,
            Slots = [slot]
        };
        var counts = new Dictionary<Guid, int>
        {
            [slot.Id] = 2
        };

        A.CallTo(() => weekScheduleQueries.GetByTeacherAndWeekAsync(teacherId.Value, publication.WeekStart.Value))
            .Returns(schedule);
        A.CallTo(() => submissionQueries.GetSlotRequestCountsAsync(publication.Id.Value, teacherId.Value))
            .Returns(counts);

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var summary = WorkbookOf(excel).Worksheet(SummarySheet.Name);
        summary.Cell(1, 2).GetText().ShouldBe("ראשון 4.10");
        summary.Cell(2, 2).GetValue<int>().ShouldBe(2);
    }

    [TestMethod]
    public async Task Summary_Grid_Is_Empty_When_The_Teacher_Has_No_Week_Schedule()
    {
        //given

        //when
        var excel = await generator.GenerateAsync(publication.Id, teacherId);

        //then
        var summary = WorkbookOf(excel).Worksheet(SummarySheet.Name);
        summary.Range(2, 2, 5, 7).IsEmpty().ShouldBeTrue();
    }

```

The existing `using`s already cover `SlotForGetWeekScheduleResponse` / `GetWeekScheduleResponse` (`DrivingLessons.Application.Queries.GetWeekSchedule`), `SlotState` / `SlotWindowType` (`DrivingLessons.Domain.Values`) and `SummarySheet` (`DrivingLessons.Infrastructure.Excel`).

`Init` answers every other teacher/week with **no** schedule and every other publication/teacher with **no** counts, so `Summary_Sheet_Counts_…` only passes if the generator asks for exactly the teacher, the publication's week and the publication it was given — the scope that keeps another teacher's picks off this file (README Review Focus 3). Its header assertion proves the week's date reaches the sheet. `Summary_Grid_Is_Empty_…` runs on those defaults: a teacher with no week schedule gets an empty grid, not an exception (Review Focus 4).

- [x] **Step 2: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~ExcelGeneratorTest"`
Expected: FAIL — 3 of 5: `Summary_Sheet_Counts_The_Requests_Of_The_Teacher_And_Publication` and `Summary_Grid_Is_Empty_When_The_Teacher_Has_No_Week_Schedule` throw because there is no worksheet named `סיכום`; `Workbook_Has_The_Summary_Then_The_Request_Detail_Sheet` fails because the first sheet is still `Summary`. The other two pass.

- [x] **Step 3: The generator writes `SummarySheet`**

Replace the contents of `src\DrivingLessons.Infrastructure\Excel\ExcelGenerator.cs` with:

```csharp
using ClosedXML.Excel;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public class ExcelGenerator : IExcelGenerator
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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

        SummarySheet.AddTo(workbook, weekStart, schedule?.Slots ?? [], counts);
        RequestDetailSheet.AddTo(workbook, requests);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"week-{weekStart:yyyy-MM-dd}-{teacherGuid}.xlsx";

        return new ExcelFile(fileName, content, ContentType);
    }
}
```

What changed: the private `Days` / `Windows` arrays and `BuildSummarySheet` (English labels, grey fill with no text, `AdjustToContents`) are gone; the `DrivingLessons.Application.Queries.GetWeekSchedule` using went with them. The loads, their order, the file name and the content type are untouched.

Then confirm the English sheet is gone everywhere: `grep -rn '"Summary"' src tests --include=*.cs` → no output.

- [x] **Step 4: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Excel"`
Expected: PASS — 39 tests (5 generator + 10 summary + 24 detail).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS.

- [x] **Step 5: Smoke API on a throwaway database — setup**

1. Create the database (the compose container must be running — `docker ps` shows `drivinglessonsbooking-postgres-1`):
   ```bash
   docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us45_smoke
   ```
   (If it reports the database exists from an earlier run, drop it first: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us45_smoke`. It only ever holds this smoke data.)
2. Add a **local-only** entry to `.claude\launch.json` `configurations` (the file has unrelated local edits — never stage it):
   ```json
   {
     "name": "api-smoke",
     "runtimeExecutable": "dotnet",
     "runtimeArgs": [
       "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
       "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us45_smoke;Username=app;Password=devpassword"
     ],
     "port": 5080
   }
   ```
3. `preview_start {name:"api-smoke"}` (stop any running `api` server first — both use port 5080). Startup applies every migration to the empty database and seeds the dev admin; `preview_logs` must show no migration error.
4. Write the smoke helpers (sourced by `run.sh` here and by task 3):
   ```bash
   SMOKE=.superpowers/sdd/us-45-smoke
   mkdir -p "$SMOKE"
   cat > "$SMOKE/helpers.sh" <<'SH'
   SMOKE=.superpowers/sdd/us-45-smoke
   API=http://localhost:5080
   json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }
   TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
     -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
   AUTH="Authorization: Bearer $TOKEN"
   SH
   ```
5. Write the summary-grid reader (Windows `tar.exe` unpacks the file, Node reads the sheet XML — no new tooling). It prints the sheet names, the direction and the grid (`·` = no cell), and, given the dashboard JSON for the same teacher, compares every one of the 24 grid positions with it (`לא זמין` for an unavailable slot, the count for an open one, nothing where the dashboard has no slot):
   ```bash
   cat > "$SMOKE/summary-grid.js" <<'JS'
   const fs = require('fs');
   const path = require('path');
   const [dir, dashboardFile] = process.argv.slice(2);
   const read = file => fs.readFileSync(path.join(dir, file), 'utf8');
   const decode = text => text
       .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, '&');
   const texts = xml => [...xml.matchAll(/<(?:\w+:)?t(?:\s[^>]*)?>([\s\S]*?)<\/(?:\w+:)?t>/g)].map(m => decode(m[1])).join('');
   const shared = fs.existsSync(path.join(dir, 'xl/sharedStrings.xml'))
       ? [...read('xl/sharedStrings.xml').matchAll(/<(?:\w+:)?si>([\s\S]*?)<\/(?:\w+:)?si>/g)].map(m => texts(m[1]))
       : [];
   const workbook = read('xl/workbook.xml');
   const rels = read('xl/_rels/workbook.xml.rels');
   const sheetTags = [...workbook.matchAll(/<(?:\w+:)?sheet\s[^>]*>/g)].map(m => m[0]);
   console.log('sheets=' + sheetTags.map(tag => tag.match(/name="([^"]+)"/)[1]).join(','));
   const relId = sheetTags.find(tag => tag.includes('name="סיכום"')).match(/r:id="([^"]+)"/)[1];
   const target = [...rels.matchAll(/<Relationship\s[^>]*>/g)].map(m => m[0]).find(tag => tag.includes(`Id="${relId}"`)).match(/Target="([^"]+)"/)[1];
   const sheet = read(path.posix.join('xl', target.replace(/^\/?xl\//, '')));
   console.log(`rightToLeft=${/rightToLeft="(1|true)"/.test(sheet)}`);
   const cells = {};
   for (const m of sheet.matchAll(/<(?:\w+:)?c r="([A-Z]+\d+)"([^>]*?)(?:\/>|>([\s\S]*?)<\/(?:\w+:)?c>)/g)) {
       const [, ref, attributes, body = ''] = m;
       const type = (attributes.match(/t="(\w+)"/) || [])[1];
       const value = (body.match(/<(?:\w+:)?v>([\s\S]*?)<\/(?:\w+:)?v>/) || [])[1];
       cells[ref] = type === 's' ? shared[Number(value)] : type === 'inlineStr' ? texts(body) : decode(value ?? '');
   }
   const columns = 'ABCDEFG'.split('');
   for (let row = 1; row <= 5; row++) {
       console.log(row, '|', columns.map(column => cells[column + row] || '·').join(' | '));
   }
   if (dashboardFile) {
       const days = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday'];
       const windows = ['morning', 'noon', 'afternoon', 'evening'];
       const dashboard = JSON.parse(fs.readFileSync(dashboardFile, 'utf8'));
       const mismatches = [];
       days.forEach((day, d) => windows.forEach((window, w) => {
           const slot = dashboard.slotCounts.find(x => x.day === day && x.window === window);
           const expected = !slot ? '' : slot.state === 'unavailable' ? 'לא זמין' : String(slot.requestCount);
           const actual = cells[columns[d + 1] + (w + 2)] ?? '';
           if (expected !== actual) mismatches.push(`${day}-${window}: sheet [${actual}] dashboard [${expected}]`);
       }));
       console.log(mismatches.length ? 'dashboard MISMATCH\n' + mismatches.join('\n') : 'matches dashboard');
   }
   JS
   ```

- [x] **Step 6: Smoke — submit, block, read both teachers' files against the dashboard**

Write and run the scenario (two teachers, two cars, four roster students, one open publication):

```bash
cat > "$SMOKE/run.sh" <<'SH'
. .superpowers/sdd/us-45-smoke/helpers.sh
new_teacher() { curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"contactEmail\":\"$2\"}" | json id; }
new_car() { curl -s -X POST $API/api/cars -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"name\":\"$1\",\"type\":\"Yaris\",\"transmission\":\"$2\"}" | json id; }
new_week() { curl -s -X POST $API/api/week-schedules -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"teacherId\":\"$1\",\"weekStart\":\"$2\"}" | json id; }
import() { curl -s -w "  <- import $1: %{http_code}\n" -X POST $API/api/roster-imports -H "$AUTH" \
  -F "file=@$SMOKE/$1;type=text/csv"; }

WEEK=$(date -u -d 'next sunday' +%F)
COHEN_ID=$(new_teacher "Smoke Cohen" smoke.cohen@example.com)
LEVI_ID=$(new_teacher "Smoke Levi" smoke.levi@example.com)
new_car "Smoke Auto" automatic > /dev/null
new_car "Smoke Manual" manual > /dev/null
COHEN_WS=$(new_week $COHEN_ID $WEEK)
new_week $LEVI_ID $WEEK > /dev/null

cat > "$SMOKE/roster.csv" <<'CSV'
שם מלא,תעודת זהות,טלפון,מורה,רכב
Smoke Student A,000000018,050-0000001,Smoke Cohen,Smoke Auto
Smoke Student B,000000026,050-0000002,Smoke Levi,Smoke Manual
דנה כהן,000000034,050-0000003,Smoke Cohen,Smoke Manual
Smoke Student E,000000067,050-0000005,Smoke Cohen,Smoke Auto
CSV
import roster.csv

PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+3 hours' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 5
echo "link state: $(curl -s $API/api/submissions/by-link/$LINK | json state)"

identify() { curl -s -X POST "$API/api/submissions/by-link/$LINK/identify" -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"$1\"}"; }
body() { node -e 'const [nationalId,target,...picks]=process.argv.slice(1);console.log(JSON.stringify({nationalId,targetCount:Number(target),slotRequests:picks.map(p=>{const [slotId,sessionType,...rest]=p.split("|");return {slotId,sessionType,constraint:rest.length?rest.join("|"):null}})}))' "$@"; }
submit() { curl -s -o /dev/null -w "%{http_code}" -X POST "$API/api/submissions/by-link/$LINK" \
  -H "Content-Type: application/json" -d "$1"; }
block() { curl -s -o /dev/null -w "block $1: %{http_code}\n" -X POST \
  "$API/api/week-schedules/$COHEN_WS/slots/$2/mark-unavailable" -H "$AUTH"; }
download() { curl -s -o "$SMOKE/$1.xlsx" -w "$1 Excel: %{http_code}\n" \
  "$API/api/publications/$PUB_ID/excel?teacherId=$2" -H "$AUTH"; }
dashboard() { curl -s "$API/api/publications/$PUB_ID/dashboard?teacherId=$2" -H "$AUTH" > "$SMOKE/$1-dashboard.json"; }
grid() { rm -rf "$SMOKE/$1" && mkdir -p "$SMOKE/$1" && /c/Windows/System32/tar.exe -xf "$SMOKE/$1.xlsx" -C "$SMOKE/$1" \
  && node "$SMOKE/summary-grid.js" "$SMOKE/$1" "$SMOKE/$1-dashboard.json"; }

COHEN_GRID=$(identify 000000018)
cohen() { json "slots.$1.id" <<< "$COHEN_GRID"; }
LEVI_GRID=$(identify 000000026)
levi() { json "slots.$1.id" <<< "$LEVI_GRID"; }

block "Cohen Tuesday morning" "$(cohen 8)"
download levi-before $LEVI_ID
dashboard levi-before $LEVI_ID
grid levi-before

echo "A:    $(submit "$(body 000000018 2 "$(cohen 0)|single" "$(cohen 21)|double" "$(cohen 4)|single")")"
echo "E:    $(submit "$(body 000000067 1 "$(cohen 0)|single" "$(cohen 3)|single")")"
echo "Dana: $(submit "$(body 000000034 1 "$(cohen 0)|double")")"
echo "B:    $(submit "$(body 000000026 1 "$(levi 1)|single")")"
block "Cohen Monday morning" "$(cohen 4)"

download cohen $COHEN_ID
dashboard cohen $COHEN_ID
grid cohen
download levi $LEVI_ID
dashboard levi $LEVI_ID
grid levi
echo "cohen totalPicks: $(json totalPicks < "$SMOKE/cohen-dashboard.json")"

printf 'WEEK=%s\nPUB_ID=%s\nCOHEN_ID=%s\nLEVI_ID=%s\n' "$WEEK" "$PUB_ID" "$COHEN_ID" "$LEVI_ID" > "$SMOKE/env.sh"
SH
bash "$SMOKE/run.sh"
```

Identify lists the grid Sunday → Friday, Morning → Evening, so `cohen 0` is Sunday Morning, `cohen 3` Sunday Evening, `cohen 4` Monday Morning, `cohen 8` Tuesday Morning and `cohen 21` Friday Noon; `levi 1` is Levi's Sunday Noon.

Expected, in order (the dates are `$WEEK`'s — shown here for `WEEK=2026-10-04`):
1. `import roster.csv: 201` with `"added":4,…,"failed":0`; `publish: 204`; `link state: open` (if it still says `published`, wait a few seconds and re-check — Quartz fires the past-due open job); `block Cohen Tuesday morning: 204`.
2. `levi-before Excel: 200`, then `sheets=סיכום,פירוט בקשות`, `rightToLeft=true` and — before any submission — every open cell `0`:
   ```
   1 | · | ראשון 4.10 | שני 5.10 | שלישי 6.10 | רביעי 7.10 | חמישי 8.10 | שישי 9.10
   2 | בוקר 07:00–12:00 | 0 | 0 | 0 | 0 | 0 | 0
   3 | צהריים 12:00–15:00 | 0 | 0 | 0 | 0 | 0 | 0
   4 | אחה״צ 15:00–18:00 | 0 | 0 | 0 | 0 | 0 | ·
   5 | ערב 18:00–22:00 | 0 | 0 | 0 | 0 | 0 | ·
   matches dashboard
   ```
3. `A:    204`, `E:    204`, `Dana: 204`, `B:    204`; `block Cohen Monday morning: 204`.
4. `cohen Excel: 200`, `sheets=סיכום,פירוט בקשות`, `rightToLeft=true`, then exactly:
   ```
   1 | · | ראשון 4.10 | שני 5.10 | שלישי 6.10 | רביעי 7.10 | חמישי 8.10 | שישי 9.10
   2 | בוקר 07:00–12:00 | 3 | לא זמין | לא זמין | 0 | 0 | 0
   3 | צהריים 12:00–15:00 | 0 | 0 | 0 | 0 | 0 | 1
   4 | אחה״צ 15:00–18:00 | 0 | 0 | 0 | 0 | 0 | ·
   5 | ערב 18:00–22:00 | 1 | 0 | 0 | 0 | 0 | ·
   matches dashboard
   ```
5. `levi Excel: 200`, the same headers and labels, `1` at Sunday Noon (row 3, column B), `0` in every other open cell, `·` for Friday Afternoon/Evening, and `matches dashboard`.
6. `cohen totalPicks: 6` — the six requests the detail sheet lists; the summary shows five of them, because A's Monday Morning pick sits on a slot now blocked.

What step 4 proves:
- **Sunday Morning `3`** — A (Single), E (Single) and Dana (**Double**) — a Double counts as one (README Review Focus 2).
- **Friday Noon `1`** — A's **Double** again counts as one; Friday Afternoon/Evening are `·`: no cell at all (Review Focus 5).
- **Monday Morning `לא זמין`** — A picked it, then the admin blocked it: the cell is blocked and the count hidden, as on the dashboard (Review Focus 1). **Tuesday Morning `לא זמין`** — blocked before anyone could pick it.
- **Sunday Noon `0`** — Levi's student B picked Levi's Sunday Noon; it never reaches Cohen's file (Review Focus 3), and step 5 shows it on Levi's.
- **`matches dashboard`** three times — every cell of every file equals the admin dashboard for the same teacher (Global Constraints).

Keep the API running, the smoke database and `$SMOKE` (`helpers.sh`, `env.sh`, `cohen.xlsx`) — task 3 continues here. `$SMOKE/cohen.xlsx` is the file task 3 hands to your human partner.

- [x] **Step 7: Commit**

```bash
git add src/DrivingLessons.Infrastructure/Excel/ExcelGenerator.cs \
  tests/DrivingLessons.Application.Test/Excel/ExcelGeneratorTest.cs
git commit -m "feat(excel): generated files open on the Hebrew summary sheet

ExcelGenerator writes SummarySheet in place of its English grid, from the
same week schedule and slot request counts, so the admin download and
every close-time email carry it.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(`git status` must show only these two files staged and nothing under `.claude\`.)

---

**Next:** [task-03-verification-and-pr.md](task-03-verification-and-pr.md)
