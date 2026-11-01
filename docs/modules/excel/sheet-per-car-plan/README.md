# Sheet per Car: Task Index (excel slice 6)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer request (1 November 2026), applied to the [Excel roadmap](../README.md).

**Goal:** A teacher's file keeps the teacher-wide summary grid (סיכום) as its first sheet and replaces the single request-detail sheet (פירוט בקשות) with one detail sheet per car, so the teacher books each car from its own sheet.

**Architecture:** `SlotRequestDetail` (the flat read model in `ISubmissionQueries.cs`) gains `CarId`, so two cars with the same name never share a sheet. `SubmissionQueries.GetSlotRequestDetailsAsync` projects the student's car id beside the car name it already reads. A new pure `CarSheetNames` turns car names into safe, unique Excel sheet names. `RequestDetailSheet` writes one car's sheet: a bold title row with the full car name, then the header, then that car's rows, without the car column. `ExcelGenerator` groups the requests by car, orders the groups by car name, and adds one sheet per group after the summary. The summary sheet, the download endpoint, the email, the domain, the API and the client do not change.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, ClosedXML 0.105, MSTest + Shouldly + FakeItEasy. No client change, no migration, no new packages.

**Spec:** [requirements §9](../../../requirements.md) · [Chronological Picks plan](../chronological-picks-plan/README.md) (the detail sheet as built, slice 5)

**Branch:** `excel-sheet-per-car` (off `main`; this plan is its first commit).

## Decisions (settled with your human partner on 1 November 2026; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Sheets: `סיכום`, then one detail sheet per car.** The summary grid stays teacher-wide and unchanged. The single `פירוט בקשות` sheet goes |
| 2 | **Only cars with at least one Slot Request in the file get a sheet.** Cars come from the rows the query already returns; a car with no requests has no sheet, and a soft-deleted car that still has requests keeps its sheet (roadmap decision 5) |
| 3 | **No requests at all: the workbook is `סיכום` only.** No placeholder detail sheet |
| 4 | **Car sheets are ordered by car name (ordinal), then car id**, so the order is the same in every close version |
| 5 | **Rows are grouped by `CarId`, never by name.** Two cars with the same name get two sheets |
| 6 | **Sheet name = the car name, made safe** (`CarSheetNames.For`): each of `: \ / ? * [ ]` becomes `-`; a leading or trailing `'` becomes `-`; the name is cut to 31 characters; a name already taken (ignoring case, `סיכום` and `History` reserved from the start) gets ` (2)`, ` (3)`, … with the base cut so the whole stays within 31 characters. Plain hyphen only, so `ExcelTypographyTest` stays green |
| 7 | **Row 1 is a title: the full car name, bold, merged across the seven columns**, written as text (a car named `=A1` stays text). Row 2 is the header, rows 3+ the requests. The freeze and the AutoFilter move to row 2 |
| 8 | **Seven columns, in this order:** `יום · משבצת · שם התלמיד/ה · סוג שיעור · שיעור אפשרי · שיעורים מבוקשים · אילוצים`. The `רכב` column goes (the sheet is the car). Widths `[8, 10, 24, 10, 12, 14, 40]` |
| 9 | **Row order inside a car sheet: day → slot → student name (ordinal) → submission id.** The car tie-break goes |
| 10 | **`שיעור אפשרי` numbering is unchanged:** it still counts the whole submission (a student learns on one car, so a submission never spans two sheets). A student whose roster car changed after submitting moves to the new car's sheet, because the car is the student's current roster car (roadmap decision 5) |
| 11 | **`SlotRequestDetail(Guid SubmissionId, DayOfWeek Day, SlotWindowType Window, string StudentName, Guid CarId, string CarName, SessionType SessionType, int TargetCount, int PickCount, string? Constraint)`.** `CarId` is the grouping key, never written to a cell |
| 12 | **`RequestDetailSheet.AddTo(XLWorkbook workbook, string sheetName, string carName, IReadOnlyCollection<SlotRequestDetail> requests)`.** `RequestDetailSheet.Name` goes |
| 13 | **Docs move with the code:** requirements §9 and a locked decision 9 plus a slice-6 row in the [Excel roadmap](../README.md) land with task 3 |

## Global Constraints

- One file per teacher per publication (requirements §9); every stored request is listed exactly once, on its car's sheet (Excel roadmap decision 4). The summary counts still equal the total number of rows across the car sheets.
- Hebrew and right-to-left on every sheet; every Hebrew word in the workbook comes from `HebrewExcelLabels`, `SummarySheet` or `RequestDetailSheet`.
- Plain hyphen and three dots only: `ExcelTypographyTest` must stay green.
- No rank, national ID or phone anywhere in the generated workbook.
- No migration, no client change, no API change, no new packages. No comments in code except `//given //when //then`.

## Review Focus

1. **Two cars with the same name** get two sheets, `Corolla` and `Corolla (2)`, each with only its own rows. Pinned by task 1 `Numbers_Repeated_Names_Ignoring_Case` and task 3 `Two_Cars_With_The_Same_Name_Get_A_Sheet_Each`.
2. **A car name Excel would refuse** (`A/B`, 40 characters, `סיכום`, `History`) never crashes the download or the close email. Pinned by task 1.
3. **No requests:** the file is `סיכום` only and still opens. Pinned by task 3 `Workbook_Is_The_Summary_Only_When_There_Are_No_Requests`.
4. **Every request lands on exactly one sheet:** the rows across car sheets add up to the summary total. Pinned by task 4 smoke.
5. **A soft-deleted car** keeps its sheet and its name. Pinned by task 4 smoke.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-car-sheet-names.md](task-01-car-sheet-names.md) | `CarSheetNames`: safe, unique sheet names (TDD) | own commit |
| 2 | [task-02-car-id-in-read-model.md](task-02-car-id-in-read-model.md) | `SlotRequestDetail.CarId` and the query projection | own commit |
| 3 | [task-03-one-sheet-per-car.md](task-03-one-sheet-per-car.md) | `RequestDetailSheet` per car with a title row, `ExcelGenerator` one sheet per car, docs (TDD) | own commit |
| 4 | [task-04-smoke-checks-and-push.md](task-04-smoke-checks-and-push.md) | API smoke on a throwaway database (when Postgres is available), full checks, push | none (no source change) |

No PR unless your human partner asks for one.
