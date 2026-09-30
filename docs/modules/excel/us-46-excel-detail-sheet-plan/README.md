# US-46: Request Detail Sheet — Task Index (excel slice 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 1 of the [Excel roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** The teacher's Excel file gains a real second sheet — one row per Slot Request made against that teacher's week, with Day, Slot, Student name, National ID, Phone, Transmission, Session type, Rank, Target count and Constraints, sorted by day, then slot, then rank, in Hebrew and right-to-left — so the teacher can book straight from the file. The same file is what the admin downloads and what every close hands to the email sender. Covers GitHub issue [#46](https://github.com/silagy/DrivingLessonsBooking/issues/46) (US-46).

**Architecture:** A new read-model member `ISubmissionQueries.GetSlotRequestDetailsAsync(publicationId, teacherId)` returns flat `SlotRequestDetail` records (enums + primitives), scoped exactly like the existing slot counts (submissions made against the teacher's week schedule). A static `RequestDetailSheet` (Infrastructure, ClosedXML) owns every rule of the sheet — order, Hebrew labels via `HebrewExcelLabels`, text-typed identifiers, layout — and is unit-tested by writing a workbook and reading it back. `PlaceholderExcelGenerator` becomes `ExcelGenerator` and calls the new query and sheet; the summary sheet is untouched (US-45). Both existing consumers (admin download, `PublicationClosedHandler`) pick the sheet up with no change.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL; ClosedXML 0.105 (already referenced by Infrastructure); MSTest + Shouldly + FakeItEasy. No client change, no new packages, no migration.

**Spec:** issue [#46](https://github.com/silagy/DrivingLessonsBooking/issues/46) · [Excel roadmap](../README.md) (locked decisions 1–6) · [requirements §9 Sheet 2, §5.5, §5.7, §6.4, §8.1](../../../requirements.md) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) (National ID + Phone replace Email; one file per teacher) · [first-submission plan](../../student-form/us-33-first-submission-plan/README.md) decisions 5 and 16 (per-teacher scope through `WeekScheduleId`, `SubmissionQueries`)

**Branch:** `46-us-46-excel-detail-sheet` (created from `origin/main` at planning time)

## User Story

**US-46** ([#46](https://github.com/silagy/DrivingLessonsBooking/issues/46)) — *As a teacher, I want the Excel detail sheet to list one row per Slot Request with all booking-relevant fields, so that I can book directly from the file without asking anyone anything.*
- **Given** the Excel was generated for my Publication **When** I open the Request Detail sheet **Then** I see one row per Slot Request with **Day, Slot, Student name, National ID, Phone, Transmission, Session type, Rank, Target count, and Constraints** — sorted by day, then slot, then student rank.

## Context

The publications module built the whole Excel path around a placeholder: `IExcelGenerator` → `PlaceholderExcelGenerator` (ClosedXML) serves `GET api/publications/{id}/excel?teacherId=…` (the admin dashboard and history pages already have Download buttons) and `PublicationClosedHandler`, which runs at every close, versions the subject and passes the file to `IEmailSender` (`LoggingEmailSender`, which only logs). Student-form slice 3 made `ISubmissionQueries` real: the summary sheet counts slot requests per slot for the teacher's week schedule. The detail sheet is still a header row in English with the wrong columns (`Student, Day, Window, Session Type, Rank, Constraint`).

A `Submission` (per student per publication) records the `WeekScheduleId` it was made against, its `TargetCount` and an owned list of `SlotRequest`s (`SlotId`, `SessionType`, `Constraint?`, `Rank`). The student's name, national ID and phone live on `Student`; transmission lives on the student's roster `Car`. `Car` and `Teacher` have soft-delete query filters; `Student`, `Submission` and `WeekSchedule` do not.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Hebrew, right-to-left sheet named `פירוט בקשות`** (roadmap decision 1). Headers: `יום · משבצת · שם התלמיד/ה · תעודת זהות · טלפון · תיבת הילוכים · סוג שיעור · דירוג · יעד שיעורים · אילוצים`. Values: `ראשון…שישי`, `בוקר · צהריים · אחה״צ · ערב`, `אוטומטי · ידני`, `יחיד · כפול` (`he.json` wording; admin wording for transmission). |
| 2 | **New query member, flat record**: `ISubmissionQueries.GetSlotRequestDetailsAsync(Guid publicationId, Guid teacherId) : Task<IReadOnlyList<SlotRequestDetail>>`; `SlotRequestDetail` lives beside `SubmissionStats` in `ISubmissionQueries.cs` (its precedent). The query returns rows **unsorted**; it reuses `TeacherSubmissions(…)` so its scope is provably the summary counts' scope (roadmap decision 3). |
| 3 | **Every sheet rule lives in `RequestDetailSheet`** (Infrastructure, static, ClosedXML) — the order (day → slot → rank, then student name, then national ID, ordinal, so equal ranks on one slot are stable), the labels, the cell types, the layout. It is the unit under test; the EF query is proven over HTTP (there is no EF test harness in this repo). |
| 4 | **National ID and phone are text cells** in a text-formatted (`@`) column, so `000000018` keeps its leading zeros and `050-0000001` is never read as a number or a formula; rank and target count are numbers. A missing constraint is a blank cell. Constraints are written as values, never formulas (`=1+1` stays text). |
| 5 | **Fixed column widths**, constraints wrapped in a 40-wide column. ClosedXML's `AdjustToContents` did not widen a Hebrew-only column and skips wrapped cells (both checked at planning time with ClosedXML 0.105), so widths are explicit. Bold header row, frozen, with an AutoFilter over the header and rows. |
| 6 | **`PlaceholderExcelGenerator` → `ExcelGenerator`** (file, class, DI). Its summary sheet is moved over unchanged — English, sheet `Summary` — and is US-45's to convert; its detail-sheet stub is replaced by `RequestDetailSheet.AddTo`. File name and content type are unchanged. |
| 7 | **Rows include deactivated students, slots marked Unavailable after submitting, and students whose car was soft-deleted** (roadmap decisions 4–5): the query joins `dbContext.Cars.IgnoreQueryFilters()` so the car filter can never drop a row. |
| 8 | **Tests live in `tests\DrivingLessons.Application.Test\Excel\`** — that project already references Infrastructure and tests `RosterCsvParser` the same way (`Csv\`). Workbooks are round-tripped through a `MemoryStream`, so the tests read what Excel would read. |
| 9 | **The API smoke runs against a throwaway database** (`drivinglessons_us46_smoke`): it uploads rosters (which deactivate students) and deletes a car, so never against the dev database. The downloaded `.xlsx` is unpacked with Windows `tar.exe` and read by a small Node script — no new tooling. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- DI registrations go in each project's `DependencyInjection.cs` (`AddInfrastructure`).
- Query interfaces take and return raw `Guid`s and primitives (`ISubmissionQueries` precedent); value objects are resolved inside the implementation (`PublicationId.Of`, `TeacherId.Of`).
- Application tests build real domain objects (`Publication.Create(WeekStart.Of(…))`) — `DrivingLessons.Application.Test` does not reference the domain fake builders.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null** — configure `null` explicitly where a test needs it.
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- One file per teacher per publication; sheet 2 is one row per Slot Request (requirements §9, ADR 0003).
- Columns exactly, in order: Day, Slot, Student name, National ID, Phone, Transmission, Session type, Rank, Target count, Constraints (issue #46).
- Sorted by day (Sunday → Friday), then slot (Morning → Noon → Afternoon → Evening), then student rank (issue #46, requirements §9).
- Hebrew, right-to-left (roadmap decision 1).
- The Excel is served only to the authenticated admin (download endpoint) and to the teacher's email; the national ID never appears in a URL, a file name, or a log line.
- No new NuGet/npm packages, no migration, no client change.
- Layer dependencies hold: Application → Domain; Infrastructure → Application, Domain, ClosedXML.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **A national ID with leading zeros or a phone with dashes** (`000000018`, `050-0000001`) → stays text exactly as stored, never a number that drops zeros → task 1 `Keeps_National_Id_And_Phone_As_Text`, task 2 smoke row check.
2. **The student's car was soft-deleted after they submitted** → the row is still listed with that car's transmission (the `Car` query filter must not drop it) → task 2 Step 5 check 5.
3. **The student was deactivated by a later roster upload** → their rows are still listed (matching the summary counts) → task 2 Step 5 check 4.
4. **Another teacher's students** → never on this teacher's file; the query takes the teacher from the download/close, scoped like the counts → task 2 `Detail_Sheet_Lists_The_Requests_Of_The_Teacher_And_Publication`, Step 5 check 6 (Levi's file).
5. **A constraint that looks like a formula or is 200 characters long** (`=1+1`, a pasted paragraph) → plain text, wrapped inside a fixed-width column → task 1 `Writes_A_Formula_Like_Constraint_As_Plain_Text`, `Wraps_Long_Constraints_In_A_Fixed_Width_Column`, task 2 Step 5 check 3.

Also pinned: two students with the same rank on the same slot → a stable order by name (task 1 `Orders_Students_With_The_Same_Slot_And_Rank_By_Name`, task 2 Step 5 check 2); a teacher with no submissions → header only (task 1 `Lists_Only_The_Header_When_There_Are_No_Requests`, task 2 Step 5 check 7); a slot marked Unavailable after submitting → still listed (task 2 Step 5 check 4).

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-request-detail-sheet.md](task-01-request-detail-sheet.md) | Infrastructure — `SlotRequestDetail`, `HebrewExcelLabels`, `RequestDetailSheet` (TDD, workbook round-trip) | ✅ own commit |
| 2 | [task-02-detail-query-and-generator.md](task-02-detail-query-and-generator.md) | Query + generator — `GetSlotRequestDetailsAsync`, `ExcelGenerator` wiring (TDD), API smoke on a throwaway DB | ✅ own commit |
| 3 | [task-03-verification-and-pr.md](task-03-verification-and-pr.md) | Admin download in the browser, the file handed to the human partner, the close path, full check, PR | — (no source change) |

## How to Run a Task

1. Confirm you are on branch `46-us-46-excel-detail-sheet` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification (and task 2 adds a local-only `api-smoke` entry) — never stage it (`git add` only the paths each task lists).

Backend commands run from the repo root, in bash. Environment notes (project memory):
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`) — `dl-postgres` has a stale migration history.
- mingw `curl` cannot read MSYS `/tmp` paths from `mktemp`: keep smoke files under the repo-relative, git-ignored `.superpowers\sdd\us-46-smoke\`.
- The default npm is broken for installs (not needed in this slice); browser-pane screenshots are flaky on this PrimeNG app — prefer DOM and network evidence.

## Open Items (non-blocking)

1. **The summary sheet is still English** (`Summary`, `Sunday…`, `Morning…`) next to a Hebrew detail sheet until US-45 ([#45](https://github.com/silagy/DrivingLessonsBooking/issues/45)) converts it.
2. **A slot marked Unavailable after a student picked it** is listed on the detail sheet but shown blocked with no count on the summary sheet. US-45 decides whether the summary shows such counts.
3. **The file name** (`week-{yyyy-MM-dd}-{teacherGuid}.xlsx`) is not human-friendly; US-44 (the email attachment) is the natural place to rename it to the teacher and week number.
4. **No EF test harness**: `GetSlotRequestDetailsAsync` is proven by the task 2 smoke only, like every earlier query implementation.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Application\Queries\ISubmissionQueries.cs             + SlotRequestDetail, + GetSlotRequestDetailsAsync
src\DrivingLessons.Infrastructure\
├── Excel\HebrewExcelLabels.cs                                            new
├── Excel\RequestDetailSheet.cs                                           new
├── Excel\ExcelGenerator.cs                                               renamed from PlaceholderExcelGenerator.cs; detail sheet real
├── EntityFramework\Queries\SubmissionQueries.cs                          + GetSlotRequestDetailsAsync
└── DependencyInjection.cs                                                IExcelGenerator → ExcelGenerator
tests\DrivingLessons.Application.Test\Excel\RequestDetailSheetTest.cs     new
tests\DrivingLessons.Application.Test\Excel\ExcelGeneratorTest.cs         new
docs\modules\excel\README.md                                              new (roadmap); slice 1 row links this plan
```
