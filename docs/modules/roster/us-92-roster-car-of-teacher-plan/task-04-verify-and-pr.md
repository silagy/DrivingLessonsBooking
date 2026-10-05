# Task 4 of 4: Verify in the app and open the PR

> Part of [#92: Roster import stops deactivating Students and enforces that a Student's Car is one of their Teacher's Cars](README.md). Requires tasks 1 to 3 committed. Work on branch `92-roster-car-of-teacher`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 3 (`Car.IsAssignedTo`, `StudentCarMustBeAssignedToTeacherException`, `ImportRosterResponse(RosterImportId, Added, Updated, Failed)`, migration `RemoveRosterDeactivation`, `RosterRowFailureReason.CarNotAssignedToTeacher` / `carNotAssignedToTeacher`, `roster.failureReasons.carNotAssignedToTeacher`).
- Produces: the PR that closes #92.

**Why:** some acceptance criteria need the running app and a real database:
- AC 7 / Review Focus 3 and 4: the migration runs on a database that already holds a deactivating import and a Student who violates the invariant, and the Roster screen still loads afterwards.
- AC 1, 3, 6: an import with an absentee, a mismatched row and a shared-Car row; the screen shows three tiles and the new failure reason, Hebrew (RTL) first, then English.

- [ ] **Step 1: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every suite passes; no pending model changes.

- [ ] **Step 2: Build a pre-#92 database with the old code**

Use the compose Postgres, not `dl-postgres` (its migration history is stale, memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us92_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us92_verify"
```

Check out the base commit in a throwaway worktree in the scratchpad (`<scratchpad>` is this session's scratchpad directory) and start the **old** API from it in the background (Bash tool, `run_in_background: true`). It migrates the database to the pre-#92 schema and seeds `admin@local.dev` / `DevAdmin#2026`:

```bash
git worktree add "<scratchpad>/us92-base" f2a4b8f
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us92_verify;Username=app;Password=devpassword' dotnet run --project "<scratchpad>/us92-base/src/DrivingLessons.Presentation.Web" --launch-profile http
```

Seed from a folder in the scratchpad. Hebrew on the Windows `curl` command line turns into `?????` (memory note), so every Hebrew payload and CSV is written by `node` and sent by a relative `@file` path.

Two Teachers, three Cars: "טויוטה קורולה" for Yael only, "מאזדה 2" for Oren only, "יונדאי i20" shared by both. The first CSV puts three Students on the roster; the second leaves Student C out, so the old code deactivates C and the latest import carries a `Deactivated` (30) entry. Finally Oren's Car is unassigned from Oren directly in the database, making Student B a violator of the new invariant (old data; the old API allows the unassign too, but SQL keeps it independent of any later guard).

```bash
mkdir -p verify-92 && cd verify-92
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

node -e "require('fs').writeFileSync('teacher-a.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
node -e "require('fs').writeFileSync('teacher-b.json', JSON.stringify({name:'אורן ביטון',contactEmail:'oren@school.example'}))"
TEACHER_A=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher-a.json | json id)
TEACHER_B=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher-b.json | json id)

node -e "require('fs').writeFileSync('car-a.json', JSON.stringify({name:'טויוטה קורולה',type:'קורולה',transmission:'automatic'}))"
node -e "require('fs').writeFileSync('car-b.json', JSON.stringify({name:'מאזדה 2',type:'מאזדה',transmission:'manual'}))"
node -e "require('fs').writeFileSync('car-shared.json', JSON.stringify({name:'יונדאי i20',type:'i20',transmission:'manual'}))"
CAR_A=$(curl -s -X POST $API/api/cars -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @car-a.json | json id)
CAR_B=$(curl -s -X POST $API/api/cars -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @car-b.json | json id)
CAR_SHARED=$(curl -s -X POST $API/api/cars -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @car-shared.json | json id)

for PAIR in "$CAR_A:$TEACHER_A" "$CAR_B:$TEACHER_B" "$CAR_SHARED:$TEACHER_A" "$CAR_SHARED:$TEACHER_B"; do
  curl -s -o /dev/null -w "assign %{http_code}\n" -X POST "$API/api/cars/${PAIR%%:*}/teachers/${PAIR##*:}" -H "Authorization: Bearer $ADMIN"
done

node -e "
const fs=require('fs');
const head='שם מלא,תעודת זהות,טלפון,מורה,רכב\n';
const a='דנה כהן,123456782,0501234567,יעל כרמי,טויוטה קורולה\n';
const b='יוסי מזרחי,987654324,0507654321,אורן ביטון,מאזדה 2\n';
const c='רות אברהם,111111118,0521112222,יעל כרמי,יונדאי i20\n';
fs.writeFileSync('roster-1.csv', head+a+b+c);
fs.writeFileSync('roster-2.csv', head+a+b);
"
curl -s -X POST $API/api/roster-imports -H "Authorization: Bearer $ADMIN" -F "file=@roster-1.csv"; echo
curl -s -X POST $API/api/roster-imports -H "Authorization: Bearer $ADMIN" -F "file=@roster-2.csv"; echo

docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us92_verify -c "DELETE FROM car_teachers WHERE car_id = '$CAR_B' AND teacher_id = '$TEACHER_B'"

printf 'TEACHER_A=%s\nTEACHER_B=%s\nCAR_A=%s\nCAR_B=%s\nCAR_SHARED=%s\n' "$TEACHER_A" "$TEACHER_B" "$CAR_A" "$CAR_B" "$CAR_SHARED" | tee ids.txt
```

Expected: four `assign 204` (or `201`) lines; the first import `{"added":3,"updated":0,"deactivated":0,"failed":0,...}`, the second `{"added":0,"updated":2,"deactivated":1,"failed":0,...}`; `DELETE 1`; `ids.txt` with no empty value. If a column or route differs in the base code, read it there and adapt; don't change the base worktree.

Stop the old API (`TaskStop`).

- [ ] **Step 3: Migrate with the new code (Review Focus 3 and 4; AC 7)**

Start the API from the branch in the background on the same database:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us92_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Expected: it starts with no migration error. Then:

```bash
cd verify-92 && API=http://localhost:5080 && . ./ids.txt
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
curl -s $API/api/roster-imports/latest -H "Authorization: Bearer $ADMIN"; echo
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us92_verify -c "SELECT count(*) FROM roster_import_entries WHERE outcome = 30" -c "SELECT column_name FROM information_schema.columns WHERE table_name = 'roster_imports'" -c "SELECT national_id, is_active FROM students ORDER BY national_id"
```

Expected:
- `latest` → `200` with `added`, `updated`, `failed` and **no** `deactivated`; `entries` has the two `updated` entries only.
- `count` = `0`; the columns list has no `deactivated_count`.
- Students: `111111118` inactive (deactivated by the old code, untouched by the migration), `123456782` and `987654324` active. Student B (`987654324`) still sits on "מאזדה 2", which is no longer Oren's: the migration didn't touch it.

- [ ] **Step 4: Import with the new rules (AC 1, 3, 4; Review Focus 1, 2, 3)**

The third file: A unchanged (update), B **absent** (existing violator, must stay active and unchanged), C back on the shared Car with Oren (shared Car accepted; the Inactive Student is reactivated, README decision 6), a new Student D on Yael with Oren's-only Car (rejected), and a new Student E on Oren with the shared Car (added).

```bash
node -e "
const fs=require('fs');
const head='שם מלא,תעודת זהות,טלפון,מורה,רכב\n';
fs.writeFileSync('roster-3.csv', head
  + 'דנה כהן,123456782,0501234567,יעל כרמי,טויוטה קורולה\n'
  + 'רות אברהם,111111118,0521112222,אורן ביטון,יונדאי i20\n'
  + 'נועה לוי,000000018,0531234567,יעל כרמי,מאזדה 2\n'
  + 'עומר ביטון,000000026,0541234567,אורן ביטון,יונדאי i20\n');
"
curl -s -X POST $API/api/roster-imports -H "Authorization: Bearer $ADMIN" -F "file=@roster-3.csv"; echo
curl -s $API/api/roster-imports/latest -H "Authorization: Bearer $ADMIN" | json "failures"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us92_verify -c "SELECT national_id, is_active, teacher_id, car_id FROM students ORDER BY national_id"
```

Expected:
- Import `201`: `{"rosterImportId":...,"added":1,"updated":2,"failed":1}`, no `deactivated` key.
- `failures`: one entry, `rowNumber` 4, `studentName` "נועה לוי", `reason` "carNotAssignedToTeacher".
- Students: `000000026` (E) added on Oren + shared Car; `111111118` (C) active again, on Oren + shared Car; `123456782` (A) active; `987654324` (B) **active** and unchanged (Oren + "מאזדה 2"); no `000000018` row.

(`000000018` and `000000026` pass the Israeli check digit. If `NationalId.Of` rejects either, swap in any other valid ID.)

- [ ] **Step 5: The Roster screen in Hebrew (RTL)**

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200`, sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew), and open the Roster screen ("רשימת תלמידים").

Screenshots of this app can hang or come back half-rendered (memory note). Before each screenshot run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. If a screenshot still hangs, verify through `read_page`, `get_page_text` and `getComputedStyle`; the checks below never depend on a screenshot alone.

```javascript
await document.fonts.ready;
const tiles = [...document.querySelectorAll('app-roster-stat-tile')];
const stats = document.querySelector('.roster__stats');
({
  dir: document.documentElement.dir,
  tiles: tiles.map(t => t.innerText.replace(/\s+/g, ' ').trim()),
  columns: stats ? getComputedStyle(stats).gridTemplateColumns.split(' ').length : null,
  summary: document.querySelector('.import-card__summary')?.textContent.trim(),
  failures: [...document.querySelectorAll('.failed-panel__row')].map(r => r.innerText.replace(/\s+/g, ' ').trim()),
  rawKeys: document.body.innerText.match(/\broster\.[A-Za-z.]+/g),
  deactivatedCopy: /הושבתו|Deactivated/.test(document.body.innerText),
})
```

Expected:
- `dir` "rtl"; `tiles` exactly three, in order "1 נוספו", "2 עודכנו", "1 שורות שנכשלו"; `columns` 3.
- `summary` starts with "4 שורות" (added + updated + failed).
- `failures`: one row reading "שורה 4 · נועה לוי" and "הרכב לא משויך למורה הזה".
- `rawKeys` null; `deactivatedCopy` false.
- The table: "יוסי מזרחי" (B) is listed with no "לא פעיל" tag and no result badge; "רות אברהם" (C) shows "עודכן" and no "לא פעיל"; "עומר ביטון" shows "נוסף".
- RTL: the failed-rows panel sits at the inline end of the table (left in Hebrew), the reason text is right-aligned, and the name isn't reordered around the row number.

Narrow the window to 375 px wide (`resize_window` preset `mobile`, reload): the three tiles stay on one row with no horizontal page scroll (`document.documentElement.scrollWidth <= innerWidth`). Reset with preset `desktop`.

- [ ] **Step 6: English (LTR)**

Switch the language to English:
- `dir` "ltr"; tiles "Added", "Updated", "Failed rows"; no "Deactivated" anywhere.
- The failed row reads "Row 4 · נועה לוי" and "Car is not assigned to this Teacher".
- No raw translation key.

Switch back to Hebrew. Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 7: Compare against the design**

If the `claude_design` MCP is connected, read `mock/admin.jsx` (`AdminRoster`, `RosterStat`) from project `6a0ab892-caa4-49f7-baff-bba7ca38c862` with `read_file`, render it with `render_preview`, and compare with steps 5-6. Check the remaining tiles' tones (added green, updated blue, failed plum) and that the new reason sits in the failed-rows panel in the danger color like the mock's "Unknown car" line. Write down every visible difference for the PR. Expected differences:
- No grey "deactivated (absent from file)" tile: removed by this story (README decision 1).
- The mock's "DOWNLOAD ERROR REPORT" button and "Imported students" header were already absent before #92 (Roster plan decision 6).

- [ ] **Step 8: Push and open the PR**

```bash
git push -u origin 92-roster-car-of-teacher
```

Write `pr-body.md` in the scratchpad:

```markdown
Closes #92. Part of #82, slice (7) "Roster import changes" (stories 56-59).

## What changed

- **Invariant in the domain:** `Car.IsAssignedTo(Teacher)`; `Student.Create` and `Student.UpdateFromRoster` refuse a Car not assigned to the given Teacher with `StudentCarMustBeAssignedToTeacherException` (409, code `studentCarMustBeAssignedToTeacher`). A shared Car is fine for each of its Teachers.
- **No more deactivation:** the Roster import never deactivates Students missing from the file. The deactivated count is gone from the whole contract (aggregate, `RosterImportCreated`, `RosterEntryOutcome`, both API responses, the Roster screen and translations). Migration `RemoveRosterDeactivation` drops `roster_imports.deactivated_count` and deletes historical `Deactivated` entries; it touches no Student.
- **Row-level rejection:** a row whose Car isn't its Teacher's is skipped with the new failure reason `carNotAssignedToTeacher` and listed in the result; the rest of the file imports. Upsert stays by national ID; a reappearing Inactive Student is still reactivated (most recent change wins).
- **Roster screen:** three stat tiles (added, updated, failed rows); the new reason reads "הרכב לא משויך למורה הזה" / "Car is not assigned to this Teacher".

## Decisions to review

- `RosterImportCreated` loses `DeactivatedCount` (an existing event changes); nothing subscribes to it.
- Historical `Deactivated` import entries are deleted by the migration (they only fed badges, which already hid them).

## Not in this PR

- `requirements.md` §5.5 / §5.5.1, ADR 0003 and CONTEXT.md still describe the Roster as the single source of truth that deactivates absentees: slice (8) docs/ADR updates.
- Flagging existing Students whose Car isn't their Teacher's: slices (5)/(6).

## Verification

- Backend and client suites pass; no pending model changes.
- Migration on a pre-#92 database holding a deactivating import and an invariant-violating Student: <fill in from step 3>.
- Import with an absentee, a mismatched row, a shared-Car row and a reappearing Inactive Student: <fill in from step 4>.
- Roster screen, Hebrew (RTL), English and 375 px: <fill in from steps 5-6>.
- Design differences: <fill in from step 7>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the `<fill in ...>` lines with what steps 3 to 7 actually showed, then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 92-roster-car-of-teacher --title "Roster import stops deactivating Students and rejects a Car that isn't the Teacher's (#92)" --body-file <scratchpad>/pr-body.md
```

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails.

- [ ] **Step 9: Clean up**

```bash
git worktree remove "<scratchpad>/us92-base"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us92_verify"
```

Expected: the worktree is gone (`git worktree list` no longer shows it) and `DROP DATABASE`. Leave the compose Postgres running and `dl-postgres` stopped unless the user asks otherwise.
