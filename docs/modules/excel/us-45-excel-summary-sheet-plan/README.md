# US-45: Summary Sheet — Task Index (excel slice 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 2 of the [Excel roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** The teacher's Excel file opens on a Hebrew, right-to-left summary sheet — the week's slots (Morning / Noon / Afternoon / Evening) down the side, Sunday–Friday across the top, each cell holding that slot's request count, Unavailable slots visibly blocked and the Friday Afternoon/Evening cells absent — so the teacher sees the week's demand shape at a glance, exactly as the admin dashboard shows it. Covers GitHub issue [#45](https://github.com/silagy/DrivingLessonsBooking/issues/45) (US-45).

**Architecture:** A static `SummarySheet` (Infrastructure, ClosedXML) owns every rule of sheet 1 — layout, Hebrew labels (reusing `HebrewExcelLabels.DayOf` / `WindowOf`), the blocked and absent cells, widths — and is unit-tested by writing a workbook and reading it back, the way slice 1 tests `RequestDetailSheet`. `ExcelGenerator` drops its private English `BuildSummarySheet` and calls `SummarySheet.AddTo` with the inputs it already loads (the week schedule's slots and `GetSlotRequestCountsAsync`). No new query, no new record. Both consumers (admin download, `PublicationClosedHandler`) pick the sheet up with no change.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL; ClosedXML 0.105 (already referenced by Infrastructure); MSTest + Shouldly + FakeItEasy. No client change, no new packages, no migration.

**Spec:** issue [#45](https://github.com/silagy/DrivingLessonsBooking/issues/45) · [Excel roadmap](../README.md) (locked decisions 1–6) · [requirements §9 Sheet 1, §5.3, §6.3, §8.1, §11 decisions 5 and 7](../../../requirements.md) · [slice 1 plan](../us-46-excel-detail-sheet-plan/README.md) (decisions 3, 5, 6 and open items 1–2) · the admin dashboard grid it mirrors: `client\src\app\shared\components\week-grid\` and `client\src\app\features\publications\ui\components\slot-count-cell\`

**Branch:** `45-us-45-excel-summary-sheet` (created from `origin/main` at planning time)

## User Story

**US-45** ([#45](https://github.com/silagy/DrivingLessonsBooking/issues/45)) — *As a teacher, I want the Excel summary sheet to show request counts in the day-by-slot grid with Unavailable slots marked blocked, so that I see the week's demand shape at a glance.*
- **Given** the Excel was generated for my Publication **When** I open the Summary sheet **Then** rows are the slots (Morning/Noon/Afternoon/Evening), columns are Sunday–Friday, each cell holds the Slot Request count, Unavailable slots are visually marked blocked, and Friday Afternoon/Evening cells do not exist.
- **Notes:** Double counts as one in these cells; the detail sheet carries the Single/Double flag (§8.1 standing flag).

## Context

Slice 1 ([#46](https://github.com/silagy/DrivingLessonsBooking/issues/46), merged) renamed `PlaceholderExcelGenerator` to `ExcelGenerator` and gave it a real Hebrew detail sheet (`RequestDetailSheet`, `פירוט בקשות`). The summary sheet was moved over unchanged: sheet `Summary`, English day and slot names (`Sunday…`, `Morning…`), real counts from `ISubmissionQueries.GetSlotRequestCountsAsync` (one per `SlotRequest`, so a Double already counts as one), Unavailable slots light-grey with **no text**, missing cells blank, and `AdjustToContents` widths — which slice 1 found do not widen Hebrew text.

`ExcelGenerator.GenerateAsync` already loads everything the sheet needs: the publication's `WeekStart`, the teacher's `GetWeekScheduleResponse?` (`Slots`: `Id`, `Day`, `Window`, `State`) and the counts dictionary (`slotId → count`). The domain's `WeekGridDefinition` (`Days`, `WindowsFor(day)` — Friday has Morning and Noon only) and `SlotWindowTimes` (`StartOf` / `EndOf`) define the grid. The admin dashboard renders the same grid: day name + date across, slot name + hours down, `לא זמין` (`weekGrid.legend.unavailable`) on a blocked cell with the count hidden, an empty "void" cell for Friday Afternoon/Evening.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Hebrew, right-to-left sheet named `סיכום`**, first in the workbook, replacing the English `Summary` (roadmap decision 1). Sunday is therefore the right-most day column, as on the dashboard. |
| 2 | **The layout mirrors the dashboard grid.** Cell A1 is empty. Row 1, columns B–G: `ראשון 4.10 … שישי 9.10` — the Hebrew day and its date as `d.M` (the dashboard's Hebrew date format). Column A, rows 2–5: `בוקר 07:00–12:00`, `צהריים 12:00–15:00`, `אחה״צ 15:00–18:00`, `ערב 18:00–22:00` (en dash, hours from `SlotWindowTimes`). Header row and label column bold. |
| 3 | **An open slot holds its count as a number, `0` when nobody asked** — so "no demand" (`0`) is never confused with "no slot" (blank). |
| 4 | **An Unavailable slot shows the text `לא זמין` on a light-grey fill, and never a count** — exactly the dashboard's `slot-count-cell`. This also settles slice 1 open item 2: a slot the admin marks Unavailable after students picked it shows blocked here (the admin's decision is the teacher's headline), while its rows stay on the detail sheet (roadmap decision 4) so the teacher can still see who asked. |
| 5 | **Friday Afternoon/Evening do not exist**: no value, no border, no fill — outside the bordered grid, like the dashboard's void cell. Cells are driven by `WeekGridDefinition.WindowsFor(day)` and filled from the schedule's slots; every real slot cell has a thin border. A teacher with no week schedule for that week gets the day headers and slot labels over an empty grid, never an error. |
| 6 | **Double counts as one** with no new code: the counts are `GetSlotRequestCountsAsync` (one per `SlotRequest`), unchanged. The task 2 smoke pins it with real Doubles. |
| 7 | **Every sheet rule lives in `SummarySheet`** (Infrastructure, static, ClosedXML): `AddTo(XLWorkbook, DateOnly weekStart, IReadOnlyCollection<SlotForGetWeekScheduleResponse> slots, IReadOnlyDictionary<Guid, int> counts)`. It takes the generator's existing inputs, so there is no new query, record or DI change. It is the unit under test (slice 1 decision 3). |
| 8 | **Fixed column widths** (label column 18, each day 12), no `AdjustToContents` — it does not widen Hebrew text (slice 1 decision 5). Labels are one line, so nothing wraps. |
| 9 | **No totals row or column, no title row.** The spec asks for the grid only; the week is in the day headers and the teacher in the email subject. |
| 10 | **Tests live in `tests\DrivingLessons.Application.Test\Excel\`** beside `RequestDetailSheetTest` (slice 1 decision 8); workbooks are round-tripped through a `MemoryStream`. |
| 11 | **The API smoke runs against a throwaway database** (`drivinglessons_us45_smoke`), never the dev database. The downloaded `.xlsx` is unpacked with Windows `tar.exe` and read by a small Node script that checks **every cell against the dashboard's `slotCounts`** for the same teacher. No new tooling. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Query interfaces take and return raw `Guid`s and primitives (`ISubmissionQueries`, `IWeekScheduleQueries` precedent); Infrastructure sheet builders consume Application read models directly (`RequestDetailSheet` takes `SlotRequestDetail`).
- Application tests build real domain objects (`Publication.Create(WeekStart.Of(…))`) — `DrivingLessons.Application.Test` does not reference the domain fake builders.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null** — configure `null` explicitly where a test needs it (`ExcelGeneratorTest.Init` already does for the week schedule).
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- One file per teacher per publication; sheet 1 is the summary grid, sheet 2 the request detail (requirements §9).
- Rows: slots (Morning / Noon / Afternoon / Evening). Columns: days (Sunday through Friday). Cell value: count of slot requests (requirements §9 Sheet 1).
- `Unavailable` slots visually marked as blocked; Friday Afternoon/Evening cells do not exist (requirements §9 Sheet 1, §5.3, decision 7).
- A `Double` session counts as one request in summary counts (requirements §8.1, decision 5).
- Hebrew, right-to-left (roadmap decision 1); wording from `client\public\i18n\he.json` (`weekGrid.days`, `weekGrid.windows`, `weekGrid.legend.unavailable`).
- The summary sheet agrees with the admin dashboard for the same publication and teacher (requirements §6.3 calls the dashboard "the summary grid").
- No new NuGet/npm packages, no migration, no client change.
- Layer dependencies hold: Application → Domain; Infrastructure → Application, Domain, ClosedXML.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **A slot marked Unavailable after students picked it** → the cell shows `לא זמין`, not the count (matching the dashboard), while the detail sheet keeps the rows → task 1 `Hides_The_Count_Of_A_Slot_Marked_Unavailable_After_It_Was_Requested`, task 2 smoke (Cohen Monday Morning).
2. **Double sessions** → count one each, never two → task 2 smoke (Cohen Sunday Morning `3` with Dana's Double; Friday Noon `1` from A's Double).
3. **Another teacher's students picking the same day and slot** → never counted on this teacher's file → task 2 `Summary_Sheet_Counts_The_Requests_Of_The_Teacher_And_Publication` (exact publication/teacher/week arguments), smoke (Cohen Sunday Noon `0` while Levi's student picked Levi's Sunday Noon).
4. **A teacher with no week schedule for the week** (admin download) → day headers and slot labels over an empty grid, no crash → task 1 `Leaves_The_Grid_Empty_When_There_Are_No_Slots`, task 2 `Summary_Grid_Is_Empty_When_The_Teacher_Has_No_Week_Schedule`.
5. **An open slot nobody picked next to a Friday cell that does not exist** → `0` versus blank-and-borderless, never confused → task 1 `Writes_Zero_For_An_Open_Slot_Without_Requests`, `Leaves_Friday_Afternoon_And_Evening_Out_Of_The_Grid`, `Borders_Every_Slot_Of_The_Grid`.

Also pinned: a slot blocked before anyone could pick it (task 1 `Marks_An_Unavailable_Slot_Blocked`, smoke Cohen Tuesday Morning); a week before any submission → every open cell `0` (smoke `levi-before`); every downloaded cell equals the dashboard (smoke `matches dashboard`, three times).

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-summary-sheet.md](task-01-summary-sheet.md) | Infrastructure — `SummarySheet` (TDD, workbook round-trip) | ✅ own commit |
| 2 | [task-02-generator-and-smoke.md](task-02-generator-and-smoke.md) | `ExcelGenerator` writes `SummarySheet` (TDD), API smoke on a throwaway DB checked cell-by-cell against the dashboard | ✅ own commit |
| 3 | [task-03-verification-and-pr.md](task-03-verification-and-pr.md) | Admin download in the browser, the file handed to the human partner, the close path, full check, PR | — (no source change) |

## How to Run a Task

1. Confirm you are on branch `45-us-45-excel-summary-sheet` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification (and task 2 adds a local-only `api-smoke` entry) — never stage it (`git add` only the paths each task lists).

Backend commands run from the repo root, in bash. Environment notes (project memory):
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`) — `dl-postgres` has a stale migration history.
- mingw `curl` cannot read MSYS `/tmp` paths from `mktemp`: keep smoke files under the repo-relative, git-ignored `.superpowers\sdd\us-45-smoke\`.
- Shell state does not survive between tool calls: the smoke is written as script files (`helpers.sh`, `run.sh`) and `run.sh` saves the ids task 3 needs to `env.sh`.
- The default npm is broken for installs (not needed in this slice); browser-pane screenshots are flaky on this PrimeNG app — prefer DOM and network evidence.

## Open Items (non-blocking)

1. **A slot blocked after picking** now reads `לא זמין` on the summary while its rows stay on the detail sheet (decision 4). If teachers find that confusing, the cell could read `לא זמין (2)` — a one-line change in `SummarySheet.WriteSlotCell` and the dashboard's `slot-count-cell` together.
2. **No totals** (decision 9). The dashboard's "students submitted / total picks" chips are not on the sheet; add a totals row only if teachers ask.
3. **The file name** (`week-{yyyy-MM-dd}-{teacherGuid}.xlsx`) is still not human-friendly — US-44 ([#44](https://github.com/silagy/DrivingLessonsBooking/issues/44)) is the natural place to rename it (slice 1 open item 3).
4. **No EF test harness**: the counts query is unchanged and proven by the task 2 smoke, like every earlier query implementation.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Infrastructure\Excel\SummarySheet.cs                   new
src\DrivingLessons.Infrastructure\Excel\ExcelGenerator.cs                 BuildSummarySheet + English labels removed; calls SummarySheet.AddTo
tests\DrivingLessons.Application.Test\Excel\SummarySheetTest.cs           new
tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs         sheet name; + 2 summary tests
docs\modules\excel\README.md                                              slice 2 row links this plan
```
