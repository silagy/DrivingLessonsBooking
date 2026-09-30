# Task 3 of 3: Verification — admin download, the file in Excel, the close path, full check, PR

> Part of [US-46: Request Detail Sheet](README.md). Requires tasks 1–2 committed and task 2's smoke API, database and shell still available. Work on branch `46-us-46-excel-detail-sheet`.

**Files:**
- No source change. (The roadmap row already links this plan — `docs\modules\excel\README.md` was committed with the plan.)
- Local only, never staged: `.claude\launch.json` (remove the `api-smoke` entry at the end)

**Interfaces:**
- Consumes: task 2's `api-smoke` server on `drivinglessons_us46_smoke`, the shell values (`API`, `AUTH`, `PUB_ID`, `COHEN_ID`, `LEVI_ID`, `SMOKE`, the `json` / `download` / `rows` helpers) and `$SMOKE/cohen.xlsx`.
- Produces: a verified slice and a PR closing #46.

If the task-2 shell is gone: re-declare `API=http://localhost:5080`, `SMOKE=.superpowers/sdd/us-46-smoke`, `json` and the login (`TOKEN` / `AUTH`) from task 2 Step 8.5; `WEEK=$(date -u -d 'next sunday' +%F)`; `PUB_ID=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH" | json id)`; teacher ids from `curl -s "$API/api/teachers/find" -H "$AUTH"`.

- [ ] **Step 1: The admin download in the browser**

1. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080).
2. Log in as the dev admin (`admin@local.dev`, the seeded dev password — the same one task 2's `curl` login uses).
3. Open **Publications**, pick the smoke week and **Smoke Cohen**, press **Download Excel** (`publications.downloadExcel`).
4. `read_network_requests` (`urlPattern: "excel"`) → one `GET /api/publications/{publicationId}/excel?teacherId={teacherId}` → **200**. Open its response headers: `content-type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` and a `content-disposition` file name `week-{yyyy-MM-dd}-{teacherId}.xlsx`. The URL and file name carry only GUIDs and the week — no national ID.
5. The page shows the success toast `publications.excelDownloaded` and no error toast.

- [ ] **Step 2: The file, opened in Excel, by your human partner**

`SendUserFile` with `$SMOKE/cohen.xlsx`, `display: "attach"`, caption: *"Smoke-week Excel for Smoke Cohen — please open the פירוט בקשות sheet and check it reads right-to-left, the columns are readable without resizing, and the constraints wrap."* Ask them to reply with anything that looks wrong; continue with Steps 3–4 while waiting. This is the one check no DOM or XML inspection proves (how Excel actually renders widths, wrap and direction). If they report a layout problem, fix it in `RequestDetailSheet.cs` with a failing test in `RequestDetailSheetTest.cs` first (e.g. a width assertion), re-run task 2 Step 9's `download cohen … && rows cohen`, and commit the fix separately.

- [ ] **Step 3: The close path attaches the same file (same shell)**

The close job runs `PublicationClosedHandler`, which calls the same `ExcelGenerator` inside a Quartz scope — prove it generates without error. Give Smoke Cohen a grid for the following week and publish it with a 75-second window:

```bash
WEEK2=$(date -u -d "$(date -u -d 'next sunday' +%F) + 7 days" +%F)
curl -s -o /dev/null -w "week 2 schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"teacherId\":\"$COHEN_ID\",\"weekStart\":\"$WEEK2\"}"
PUB2=$(curl -s "$API/api/publications/by-week?week=$WEEK2" -H "$AUTH")
PUB2_ID=$(json id <<< "$PUB2")
LINK2=$(json linkToken <<< "$PUB2")
curl -s -o /dev/null -w "publish week 2: %{http_code}\n" -X POST $API/api/publications/$PUB2_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+75 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
for i in $(seq 1 36); do [ "$(curl -s $API/api/submissions/by-link/$LINK2 | json state)" = closed ] && break; sleep 5; done
echo "week 2 link state: $(curl -s $API/api/submissions/by-link/$LINK2 | json state)"
```

Expected: `week 2 schedule: 201`, `publish week 2: 204`, `week 2 link state: closed` (within about two minutes). Then `preview_logs` (`search: "Skipped send"`) on the `api-smoke` server shows one line:

```
Email disabled. Skipped send to [smoke.cohen@example.com] subject [Week <n> Requests - Smoke Cohen - v1] attachment [<bytes>] bytes.
```

with `<n>` the ISO week number of `$WEEK2` and `<bytes>` a few thousand. `preview_logs` (`level: "error"`) → no errors. (Only Smoke Cohen gets a line: Smoke Levi has no grid for that week. The real send is US-44.)

- [ ] **Step 4: Full check**

```bash
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
git diff --stat origin/main -- client
```

Expected: build succeeds with no new warnings (only the existing `NU1903` / `MSTEST0001` / `CS8618`); every test PASSES — the Application suite grows by 27 (`RequestDetailSheetTest` 24, `ExcelGeneratorTest` 3); `No changes have been made to the model since the last migration.`; the client diff is empty (this slice has no client change).

- [ ] **Step 5: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server and the `client` server.
2. Remove the `api-smoke` entry from `.claude\launch.json`, restoring the file to its pre-task-2 local state. Never stage it.
3. Drop the throwaway database and the smoke files, which only hold this slice's smoke data:
   ```bash
   docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us46_smoke
   rm -rf .superpowers/sdd/us-46-smoke
   ```
4. `git status --short` → only `.claude/launch.json` (its unrelated local edit), nothing else.

- [ ] **Step 6: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 46-us-46-excel-detail-sheet
gh pr create --base main --title "US-46: Excel request detail sheet — one sorted row per slot request, in Hebrew" --body "Closes #46

## Summary
- The teacher's Excel now has a real second sheet, **פירוט בקשות**: one row per Slot Request with Day, Slot, Student name, National ID, Phone, Transmission, Session type, Rank, Target count and Constraints, sorted by day, then slot, then rank (equal ranks on a slot by student name).
- Hebrew and right-to-left, bold frozen header with a filter, fixed column widths, wrapped constraints. National IDs and phones are text cells (leading zeros kept); constraints are always text, never formulas.
- New \`ISubmissionQueries.GetSlotRequestDetailsAsync\`, scoped exactly like the summary counts (submissions made against the teacher's week schedule). Deactivated students, slots marked Unavailable after picking, and soft-deleted cars never drop a row.
- \`PlaceholderExcelGenerator\` → \`ExcelGenerator\`. The admin download and the close-time email path pick the sheet up unchanged.

## Notes
- The summary sheet is still English until US-45 (#45); the real email send is US-44 (#44).
- New module roadmap: \`docs/modules/excel/README.md\`.
- No migration, no client change, no new packages.

## Test plan
- [x] \`RequestDetailSheetTest\` (24): Hebrew header, header-only when empty, every field, day → slot → rank order, same-rank tie by name, IDs and phones as text, blank / formula-like / 200-character constraints, RTL + frozen filtered header, every Hebrew label
- [x] \`ExcelGeneratorTest\` (3): the detail rows come from the publication and teacher asked for, sheet order, file name and content type unchanged
- [x] API smoke on a throwaway DB: six Cohen rows in spec order after three submissions, a roster re-upload deactivating one student, a slot blocked after picking and a car deleted; Levi's file holds only Levi's student; header only before any submission; dashboard total matches; no national ID in URLs or logs
- [x] Browser: admin Download Excel → 200 with the xlsx content type
- [x] Close path: a closing week logs the versioned subject with the generated attachment
- [x] \`dotnet build\`, \`dotnet test\`, no pending EF model changes

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-46 AC "one row per Slot Request" → task 2 query (one record per `SlotRequest`, `from request in submission.SlotRequests`), task 1 `Writes_Every_Booking_Field_Of_A_Slot_Request`, task 2 Step 9 check 1 (six requests → six rows, dashboard total 6).
  - The ten columns, in order (issue #46, requirements §9, ADR 0003's National ID + Phone) → task 1 `Writes_The_Hebrew_Header_Row` and `Writes_Every_Booking_Field_Of_A_Slot_Request`; values from the roster (§5.5: name, phone; transmission via the car) → task 2 query joins `Students` and `Cars`.
  - "Sorted by day, then slot, then student rank" → task 1 `Sorts_By_Day_Then_Slot_Then_Rank`, `Orders_Students_With_The_Same_Slot_And_Rank_By_Name`; task 2 Step 9 rows 2–7.
  - "So I can book directly from the file" → text-typed IDs/phones, readable widths, wrap, filter, frozen header (task 1), and the human check in task 3 Step 2.
  - §9 "one file per teacher per publication" → task 2 `Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication`, Step 9 check 6. §9 Delivery (on-demand + automatic) → task 3 Steps 1 and 3. §8.1 standing flag (the detail sheet carries Single/Double) → task 1 `Names_The_Session_Type_In_Hebrew`.
  - Roadmap decisions 1 (Hebrew RTL) → task 1; 3 (scope) → task 2 `TeacherSubmissions` reuse; 4 (deactivated, blocked slots listed) → task 2 Step 9 check 4; 5 (deleted car) → task 2 `IgnoreQueryFilters`, check 5; 6 (labels in Infrastructure) → task 1 `HebrewExcelLabels`.
- **Placeholder scan:** every code step has full contents or an exact anchored edit; the only conditional instruction is task 2 Step 4's named fallback (if EF cannot translate the owned-collection join).
- **Type consistency:** `SlotRequestDetail(DayOfWeek Day, SlotWindowType Window, string StudentName, string NationalId, string Phone, Transmission Transmission, SessionType SessionType, int Rank, int TargetCount, string? Constraint)` is defined in task 1 and constructed with the same argument order in task 1's `Request(…)` helper, task 2's test and task 2's query mapping. `RequestDetailSheet.AddTo(XLWorkbook, IReadOnlyCollection<SlotRequestDetail>)` and `RequestDetailSheet.Name` are used identically in tasks 1 and 2. `GetSlotRequestDetailsAsync(Guid, Guid) : Task<IReadOnlyList<SlotRequestDetail>>` matches between the interface, the implementation and the FakeItEasy configuration. `ExcelGenerator`'s constructor keeps the placeholder's three dependencies, so DI needs only the type swap.
- **Checked at planning time** (then reverted, nothing committed): tasks 1 and 2's code compiled and their 27 tests passed; task 2's smoke script ran against a throwaway database and produced exactly the expected rows in Step 9; the close path in Step 3 logged `subject [Week 42 Requests - Smoke Cohen - v1] attachment [7895] bytes`. ClosedXML 0.105 facts the design relies on — strings stay text, `Blank.Value`, `RightToLeft`, `FreezeRows`, `SetAutoFilter`, and `AdjustToContents` not widening Hebrew-only or wrapped columns — were confirmed in a scratch program.
