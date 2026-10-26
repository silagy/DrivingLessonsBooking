# Chronological Picks: Task Index (excel slice 5)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer feedback (26 October 2026), applied to sheet 2 of the [Excel roadmap](../README.md).

**Goal:** Sheet 2 (פירוט בקשות) drops the student's rank, numbers each student's slots `{position} מתוך {picks}` from Sunday morning to Friday evening, and shows the target count as a number in its own column.

**Architecture:** `SlotRequestDetail` (the flat read model in `ISubmissionQueries.cs`) loses `Rank` and gains `SubmissionId`, so the sheet can tell two students with the same name apart. `SubmissionQueries.GetSlotRequestDetailsAsync` projects the submission's id instead of the rank. `RequestDetailSheet` numbers each submission's rows by day, then slot, writes the position and the target in two columns, and stops sorting by rank. The position is how the sheet presents rows the query already returns, so it lives in the sheet, where the unit tests reach it. The summary sheet, the generator, the download endpoint, the email, the domain, the API and the client do not change.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, ClosedXML 0.105, MSTest + Shouldly + FakeItEasy. No client change, no migration, no new packages.

**Spec:** [Chronological Picks on the Detail Sheet - Intent](../chronological-picks-intent.md) · [requirements §9 Sheet 2](../../../requirements.md) · [Detail Sheet Customer Feedback plan](../detail-sheet-feedback-plan/README.md) (the sheet as built, slice 4)

**Branch:** `excel-chronological-picks` (off `main`; the intent and this plan are its first commits).

## Decisions (made while planning; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Eight columns, in this order:** `יום · משבצת · שם התלמיד/ה · רכב · סוג שיעור · שיעור אפשרי · שיעורים מבוקשים · אילוצים` (intent §4.1). Widths `[8, 10, 24, 18, 10, 12, 14, 40]` |
| 2 | **`שיעור אפשרי` is `{position} מתוך {PickCount}`, written as text.** Position counts the submission's rows by `Day`, then `Window` (both enums already sort Sunday → Friday and Morning → Evening), starting at 1. `PickCount` still comes from the query (every Slot Request in the submission), so a slot marked Unavailable after picking keeps its number and its share of the count |
| 3 | **`שיעורים מבוקשים` is `TargetCount`, written as a number** (it was `{target} מתוך {picks}` text) |
| 4 | **`SlotRequestDetail(Guid SubmissionId, DayOfWeek Day, SlotWindowType Window, string StudentName, string CarName, SessionType SessionType, int TargetCount, int PickCount, string? Constraint)`.** `Rank` goes: the Excel is its only consumer. `SubmissionId` is the grouping key, never written to a cell |
| 5 | **Row order: day → slot → student name (ordinal) → car name (ordinal) → submission id.** The submission id only makes two same-named students with the same car in the same slot come out the same way every time |
| 6 | **`HebrewExcelLabels.RequestedLessonsOf` becomes `PickPositionOf(int position, int pickCount)`**, returning `$"{position} מתוך {pickCount}"`. No caller of the old name is left |
| 7 | **Rank stays everywhere else** (intent decision 2): `SlotRequest.Rank`, the student form, `FindSlotRequestsAsync` and the admin slot-requests dialog are untouched |
| 8 | **Docs move with the code:** requirements §9 Sheet 2 and locked decision 8 in the [Excel roadmap](../README.md) land with task 1. The roadmap's slice-5 row and the intent already link this plan |

## Global Constraints

- One file per teacher per publication; sheet 2 is one row per Slot Request (requirements §9); every stored request is listed (Excel roadmap decision 4).
- Hebrew and right-to-left; every Hebrew word in the workbook comes from `HebrewExcelLabels` or `RequestDetailSheet`'s headers.
- Plain hyphen and three dots only: `ExcelTypographyTest` must stay green.
- No rank, national ID or phone anywhere in the generated workbook.
- No migration, no client change, no API change, no new packages. No comments in code except `//given //when //then`.

## Review Focus

1. **The student ranked their slots against the calendar** (favourite on Friday): numbering ignores rank. Pinned by task 1 `Numbers_A_Students_Slots_From_Sunday_Morning_To_Friday_Evening` (rows handed in out of order) and task 2 smoke student A (rank 1 on Friday reads `3 מתוך 3`).
2. **Two students with the same name in one file** each count from 1. Pinned by task 1 `Numbers_Each_Submission_On_Its_Own_Even_With_The_Same_Name`.
3. **A slot marked Unavailable after the student picked it** keeps its number and still counts in *picks*. Pinned by task 2 smoke row 7.
4. **A Double** takes one number, like a Single. Pinned by task 2 smoke row 7 (A's Friday Double is `3 מתוך 3`).
5. **The target column is a number Excel can sum and filter**, not text. Pinned by task 1 `Writes_The_Target_Count_As_A_Number`.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-position-and-target-columns.md](task-01-position-and-target-columns.md) | Read model, query, sheet, labels and docs (TDD, workbook round-trip) | own commit |
| 2 | [task-02-download-smoke-and-pr.md](task-02-download-smoke-and-pr.md) | API smoke on a throwaway database, the file handed to your human partner, full checks, PR | none (no source change) |

Environment notes (project memory): run the API against the compose Postgres `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`); write Hebrew payloads to files and pass them to `curl` as relative `@file` paths (Hebrew in argv becomes `?????`). Auto-merge is disabled: your human partner merges by hand with `--merge`.
