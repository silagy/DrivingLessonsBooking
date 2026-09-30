# Task 3 of 3: Verification — admin download, the file in Excel, the close path, full check, PR

> Part of [US-45: Summary Sheet](README.md). Requires tasks 1–2 committed and task 2's smoke API, database and `$SMOKE` files still available. Work on branch `45-us-45-excel-summary-sheet`.

**Files:**
- No source change. (The roadmap row already links this plan — `docs\modules\excel\README.md` was committed with the plan.)
- Local only, never staged: `.claude\launch.json` (remove the `api-smoke` entry at the end)

**Interfaces:**
- Consumes: task 2's `api-smoke` server on `drivinglessons_us45_smoke`; `.superpowers\sdd\us-45-smoke\helpers.sh` (`SMOKE`, `API`, `json`, `AUTH`), `env.sh` (`WEEK`, `PUB_ID`, `COHEN_ID`, `LEVI_ID`) and `cohen.xlsx`.
- Produces: a verified slice and a PR closing #45.

Every bash block below starts with `. .superpowers/sdd/us-45-smoke/helpers.sh; . .superpowers/sdd/us-45-smoke/env.sh` — shell state does not survive between tool calls, and `helpers.sh` logs in again.

- [ ] **Step 1: The admin download in the browser**

1. `preview_start {name:"client"}` → `http://localhost:4200` (proxies `api/` to 5080).
2. Log in as the dev admin (`admin@local.dev`, the seeded dev password — the same one `helpers.sh` uses).
3. Open **Publications**, pick the smoke week and **Smoke Cohen**, and check the dashboard grid reads like task 2's `cohen` grid: `3` Sunday Morning, `לא זמין` Monday and Tuesday Morning, `1` Friday Noon, `1` Sunday Evening (`read_page`, not a screenshot).
4. Press **Download Excel** (`publications.downloadExcel`).
5. `read_network_requests` (`urlPattern: "excel"`) → one `GET /api/publications/{publicationId}/excel?teacherId={teacherId}` → **200**, `content-type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `content-disposition` file name `week-{yyyy-MM-dd}-{teacherId}.xlsx`.
6. The page shows the success toast `publications.excelDownloaded` and no error toast.

- [ ] **Step 2: The file, opened in Excel, by your human partner**

`SendUserFile` with `.superpowers/sdd/us-45-smoke/cohen.xlsx`, `display: "attach"`, caption: *"Smoke-week Excel for Smoke Cohen — please open the סיכום sheet: it should open first, read right-to-left with Sunday on the right, show the counts at a glance, the two blocked slots grey with לא זמין, and no cells for Friday afternoon/evening."* Ask them to reply with anything that looks wrong; continue with Steps 3–4 while waiting. This is the one check no XML inspection proves (how Excel actually renders widths, fill, borders and direction). If they report a layout problem, fix it in `SummarySheet.cs` with a failing test in `SummarySheetTest.cs` first (e.g. a width assertion), restart the server (`preview_stop` then `preview_start {name:"api-smoke"}` — `dotnet run` does not hot-reload), re-download the same file and send it again, then commit the fix separately:

```bash
. .superpowers/sdd/us-45-smoke/helpers.sh; . .superpowers/sdd/us-45-smoke/env.sh
curl -s -o "$SMOKE/cohen.xlsx" -w "cohen Excel: %{http_code}\n" \
  "$API/api/publications/$PUB_ID/excel?teacherId=$COHEN_ID" -H "$AUTH"
```

(Do not re-run `run.sh` on the same database — it creates the teachers again.)

- [ ] **Step 3: The close path attaches the same file**

The close job runs `PublicationClosedHandler`, which calls the same `ExcelGenerator` inside a Quartz scope — prove it generates without error. Give Smoke Cohen a grid for the following week and publish it with a 75-second window:

```bash
. .superpowers/sdd/us-45-smoke/helpers.sh; . .superpowers/sdd/us-45-smoke/env.sh
WEEK2=$(date -u -d "$WEEK + 7 days" +%F)
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
git diff --stat origin/main -- src
```

Expected: build succeeds with no new warnings (only the existing `NU1903` / `MSTEST0001` / `CS8618`); every test PASSES — the Application suite grows by 12 (`SummarySheetTest` 10, `ExcelGeneratorTest` +2); `No changes have been made to the model since the last migration.`; the client diff is empty; the `src` diff is exactly `ExcelGenerator.cs` and `SummarySheet.cs`.

- [ ] **Step 5: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server and the `client` server.
2. Remove the `api-smoke` entry from `.claude\launch.json`, restoring the file to its pre-task-2 local state. Never stage it.
3. Drop the throwaway database and the smoke files, which only hold this slice's smoke data:
   ```bash
   docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us45_smoke
   rm -rf .superpowers/sdd/us-45-smoke
   ```
4. `git status --short` → only `.claude/launch.json` (its unrelated local edit), nothing else.

- [ ] **Step 6: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 45-us-45-excel-summary-sheet
gh pr create --base main --title "US-45: Excel summary sheet — Hebrew day-by-slot demand grid" --body "Closes #45

## Summary
- The teacher's Excel now opens on **סיכום**: the four slots down the side (with their hours), Sunday–Friday across (with their dates), each cell the slot's request count — Hebrew and right-to-left, laid out like the admin dashboard grid.
- Open slots hold a number (\`0\` when nobody asked); Unavailable slots read **לא זמין** on grey with the count hidden, as on the dashboard; Friday afternoon and evening have no cell at all.
- A Double still counts as one (the counts query is unchanged); the detail sheet carries the Single/Double flag.
- New \`SummarySheet\` owns the sheet; \`ExcelGenerator\` calls it in place of its English grid. The admin download and the close-time email path pick it up unchanged.

## Notes
- A slot blocked after students picked it now reads לא זמין on the summary, while its rows stay on the detail sheet (plan decision 4 — settles slice 1's open item 2).
- The real email send is US-44 (#44).
- No migration, no client change, no new packages.

## Test plan
- [x] \`SummarySheetTest\` (10): RTL Hebrew sheet name, day headers with dates, slot labels with hours, counts as numbers, zero for an unpicked slot, blocked slot label and fill, count hidden on a slot blocked after picking, Friday afternoon/evening outside the grid, borders on every slot, empty grid with no slots
- [x] \`ExcelGeneratorTest\` (+2): the counts and week come from the publication and teacher asked for; an empty grid when the teacher has no week schedule
- [x] API smoke on a throwaway DB: Doubles count as one, a slot blocked before and one after picking both read לא זמין, Friday afternoon/evening absent, Levi's picks never on Cohen's file, and every cell of three downloaded files equals the dashboard
- [x] Browser: admin Download Excel → 200 with the xlsx content type
- [x] Close path: a closing week logs the versioned subject with the generated attachment
- [x] \`dotnet build\`, \`dotnet test\`, no pending EF model changes

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-45 AC "rows are the slots (Morning/Noon/Afternoon/Evening)" → task 1 `Labels_The_Rows_With_The_Four_Slots_And_Their_Hours`; "columns are Sunday–Friday" → `Heads_The_Columns_With_Sunday_To_Friday_And_Their_Dates`; "each cell holds the Slot Request count" → `Writes_The_Request_Count_Of_Each_Open_Slot`, `Writes_Zero_For_An_Open_Slot_Without_Requests`, task 2 `Summary_Sheet_Counts_…`, smoke step 4; "Unavailable slots are visually marked blocked" → `Marks_An_Unavailable_Slot_Blocked`, `Hides_The_Count_…`, smoke Monday/Tuesday Morning, human check in Step 2; "Friday Afternoon/Evening cells do not exist" → `Leaves_Friday_Afternoon_And_Evening_Out_Of_The_Grid`, `Borders_Every_Slot_Of_The_Grid`, smoke `·`.
  - Note "Double counts as one" (§8.1, decision 5) → smoke Sunday Morning `3` and Friday Noon `1`; no code change needed (README decision 6).
  - §9 "one file per teacher per publication" → task 2 `Summary_Sheet_Counts_…` (exact arguments), smoke Cohen Sunday Noon `0` vs Levi `1`. §9 Delivery (on-demand + automatic) → Steps 1 and 3. §6.3 dashboard = summary grid → smoke `matches dashboard`, Step 1.3.
  - Roadmap decision 1 (Hebrew RTL) → task 1 `Is_A_Right_To_Left_Sheet_Named_In_Hebrew`, labels from `HebrewExcelLabels`; decision 2 (on-demand, current data) → unchanged generator flow; decision 3 (scope) → unchanged `GetSlotRequestCountsAsync`; decision 6 (fixed Hebrew text in Infrastructure) → `SummarySheet` constants and `HebrewExcelLabels`.
- **Placeholder scan:** every code step has full contents or an exact anchored edit; no TBD, no "similar to".
- **Type consistency:** `SummarySheet.AddTo(XLWorkbook, DateOnly, IReadOnlyCollection<SlotForGetWeekScheduleResponse>, IReadOnlyDictionary<Guid, int>)` and `SummarySheet.Name` are used identically in task 1's tests, task 2's tests and `ExcelGenerator` (`schedule?.Slots ?? []` is `IReadOnlyCollection<SlotForGetWeekScheduleResponse>`; `counts` is `GetSlotRequestCountsAsync`'s `IReadOnlyDictionary<Guid, int>`). The grid coordinates (B–G Sunday–Friday, rows 2–5 Morning–Evening) match between `SummarySheet`, both test classes and `summary-grid.js`.
- **Checked at planning time** (then reverted, nothing but the plan committed): tasks 1 and 2's code compiled with no new warnings and all 39 Excel tests passed; task 2's smoke ran against a throwaway database and printed exactly the grids in Step 6 (`matches dashboard` three times, `cohen totalPicks: 6`). ClosedXML 0.105 facts the design relies on — `RightToLeft`, `Border.OutsideBorder`, a reloaded fill compared by ARGB, an `int` value reading back as `XLDataType.Number`, `GetValueOrDefault` on `IReadOnlyDictionary` — were exercised by those tests.
