# Detail Sheet Customer Feedback: Task Index (excel slice 4)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer feedback on the final report (18 October 2026), applied to sheet 2 of the [Excel roadmap](../README.md). This plan is the first half of one branch; the second half is the [Double-counts-once plan](../../student-form/double-counts-once-plan/README.md) (customer feedback 2), whose last task verifies both and opens one PR.

**Goal:** The Request Detail sheet drops National ID, Phone and Transmission, and gains the student's car name and a "requested lessons" column that reads `{target} מתוך {picks}` (for example `1 מתוך 3`).

**Architecture:** `SlotRequestDetail` (the flat read model in `ISubmissionQueries.cs`) loses `NationalId`, `Phone` and `Transmission` and gains `CarName` and `PickCount`. `SubmissionQueries.GetSlotRequestDetailsAsync` projects the car's name and the submission's slot request count. `RequestDetailSheet` writes eight columns, and `HebrewExcelLabels` owns the new `מתוך` wording. The summary sheet, the generator, the download endpoint and the close-time email do not change.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, ClosedXML 0.105, MSTest + Shouldly + FakeItEasy. No client change, no migration, no new packages.

**Spec:** the customer's feedback 1 (quoted below) · [requirements §9 Sheet 2](../../../requirements.md) · [US-46 plan](../us-46-excel-detail-sheet-plan/README.md) (the sheet as built) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) (its Excel identifier line is amended here)

> מקצה שיפורים לדו״ח הסופי: להסיר: תעודת זהות, טלפון, תיבת הילוכים. להוסיף: עמודה עם שם הרכב. עמודה עם כמות השיעורים שהתלמיד ביקש, וזה שיעור אחד מתוך אחד, או אחד מתוך שלושה, וכן הלאה.

**Branch:** `customer-feedback-excel-and-target` (from `origin/main` at planning time). Both plans run on it.

## Decisions (made while planning; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Eight columns, in this order:** `יום · משבצת · שם התלמיד/ה · רכב · סוג שיעור · דירוג · שיעורים מבוקשים · אילוצים`. The car sits next to the student's name, because the teacher books a student into a car. |
| 2 | **"Requested lessons" is `{TargetCount} מתוך {PickCount}`**, written as text (confirmed with your human partner: the target over the number of slots the student offered). It replaces the numeric `יעד שיעורים` column. `PickCount` is every Slot Request in the submission, the same rows the sheet lists (roadmap decision 4), so a slot marked Unavailable after the student picked it still counts. |
| 3 | **`SlotRequestDetail(DayOfWeek Day, SlotWindowType Window, string StudentName, string CarName, SessionType SessionType, int Rank, int TargetCount, int PickCount, string? Constraint)`.** The query stops reading national IDs and phones. The Excel no longer carries them. |
| 4 | **The car name is the roster car's `Name`, including a car soft-deleted since** (the existing `IgnoreQueryFilters([QueryFilters.CarNotDeleted])` join stays). It is written as a text value, so `12-345-67` or `=A1` stays as typed. |
| 5 | **Order: day → slot → rank → student name → car name** (ordinal). The national ID tie-breaker goes with the column. |
| 6 | **`HebrewExcelLabels.TransmissionOf` is deleted** (no caller is left). **`HebrewExcelLabels.RequestedLessonsOf(int targetCount, int pickCount)`** returns `$"{targetCount} מתוך {pickCount}"`. |
| 7 | **Column widths** `[8, 10, 24, 18, 10, 7, 14, 40]`. The text-format (`@`) columns go with National ID and Phone. The 40-wide wrapped constraints column, bold frozen header, AutoFilter and right-to-left stay. |
| 8 | **Docs move with the code:** requirements §9 Sheet 2 table, a 7th locked decision and a slice-4 row in the [Excel roadmap](../README.md), and a short amendment to ADR 0003 ("the Excel no longer carries National ID or Phone"). |

## Global Constraints

- One file per teacher per publication; sheet 2 is one row per Slot Request, sorted by day, then slot, then rank (requirements §9).
- Hebrew and right-to-left; every Hebrew word in the workbook comes from `HebrewExcelLabels` or `RequestDetailSheet`'s headers.
- Plain hyphen and three dots only: `ExcelTypographyTest` must stay green.
- No national ID or phone anywhere in the generated workbook.
- No migration, no client change, no new packages. No comments in code except `//given //when //then`.

## Review Focus

1. **A slot marked Unavailable after the student picked it**: the row is still listed and still counts in `{picks}`, so `2 מתוך 3` matches the three rows. Pinned by task 2 smoke row 7.
2. **The student's car was soft-deleted after they submitted**: the row still shows that car's name. Pinned by task 2 smoke row 4.
3. **A car name that looks like a number or a formula** (`12-345-67`, `=A1`) stays text exactly. Pinned by task 1 `Keeps_A_Car_Name_That_Looks_Like_A_Number_Or_A_Formula_As_Text`.
4. **Two students with the same name, slot and rank** keep a stable order by car name. Pinned by task 1 `Orders_Students_With_The_Same_Slot_And_Rank_By_Name_Then_Car`.
5. **The national ID and phone are really gone**: not hidden in an unused column or the query. Pinned by task 1 `Writes_The_Hebrew_Header_Row` (exactly eight headers) and task 2 Step 3 (no `0000000` in any cell).

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-detail-sheet-columns.md](task-01-detail-sheet-columns.md) | Read model, query, sheet, labels and docs (TDD, workbook round-trip) | own commit |
| 2 | [task-02-download-smoke.md](task-02-download-smoke.md) | API smoke on a throwaway database: real rows, real counts, the file handed to your human partner | none (no source change) |

Then continue with the [Double-counts-once plan](../../student-form/double-counts-once-plan/README.md).

Environment notes (project memory): run the API against the compose Postgres `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`); write Hebrew payloads to files and pass them to `curl` as relative `@file` paths (Hebrew in argv becomes `?????`); browser-pane screenshots are flaky on this app, so prefer DOM and network evidence.
