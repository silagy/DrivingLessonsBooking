# School File: Task Index (excel slice 7)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer request (1 November 2026), applied to the [Excel roadmap](../README.md).

**Goal:** Administrators can download one Excel file for a week (one Publication) with every request from every Teacher: a summary sheet that stacks every Teacher's grid, then one request-detail sheet per car across all Teachers. The per-teacher file, its download and its email do not change.

**Architecture:** `SlotRequestDetail` gains `TeacherName` (the Teacher of the Week Schedule the submission was made against). `ISubmissionQueries` gains publication-wide overloads of `GetSlotRequestCountsAsync` and `GetSlotRequestDetailsAsync`; `IWeekScheduleQueries` gains `FindByWeekAsync`, returning one `TeacherWeekGrid` (teacher id, teacher name, slots) per Week Schedule of the week. `SummarySheet.AddForTeachersTo` writes one titled grid per Teacher on `סיכום`. `RequestDetailSheet.AddWithTeacherTo` writes a car sheet with a `מורה` column. `ExcelGenerator.GenerateForSchoolAsync` puts them together. `DownloadPublicationSchoolExcelInteractor` is served by an Administrator-only `PublicationSchoolExcelQueryController` at `GET api/publications/{id}/school-excel`. The client adds a dashboard button and one "All teachers" row per closed week in History, both for Administrators only.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, ClosedXML 0.105, MSTest + Shouldly + FakeItEasy; Angular + PrimeNG, Transloco, Vitest. No migration, no new packages, no domain change.

**Branch:** `excel-school-file` (off `main`; this plan is its first commit).

## Decisions (settled with your human partner on 1 November 2026)

| # | Decision |
|---|----------|
| 1 | **Added next to the per-teacher file, not instead of it.** Per-teacher file, download and close email unchanged |
| 2 | **Administrators only.** A Teacher-role User reaches only their own teacher's data, so the endpoint carries the Administrator policy |
| 3 | **One file per week (Publication)**, generated on demand from current data like the per-teacher file; no email, no version of its own |
| 4 | **One sheet per physical car** with requests, across all Teachers. A Car shared by two Teachers gets one sheet holding both Teachers' rows. Sheet names, ordering by car name then car id, title row and safe names follow slice 6 (`CarSheetNames`) |
| 5 | **Car sheets gain a `מורה` column right after `משבצת`**: eight columns `יום · משבצת · מורה · שם התלמיד/ה · סוג שיעור · שיעור אפשרי · שיעורים מבוקשים · אילוצים`. Rows sort day → slot → teacher name → student name → submission id |
| 6 | **The teacher is the Teacher of the Week Schedule the submission was made against** (roadmap decision 3), including a soft-deleted Teacher. The car is still the student's current car (roadmap decision 5) |
| 7 | **`סיכום` stacks one grid per Teacher with a Week Schedule that week**, including Teachers with no requests, ordered by Teacher name (ordinal) then id. Each block: a bold title row with the Teacher's name, the day header row, the four slot rows (the per-teacher grid exactly, Unavailable marked), then one blank row |
| 8 | **File name `week-{yyyy-MM-dd}-school.xlsx`** on the server and in the client |
| 9 | **Download available where the per-teacher one is:** dashboard in Open and Closed, History once Closed. The backend does not check the state |
| 10 | **History gets one "All teachers" row per closed week, above that week's Teacher rows**, for Administrators only, built in the client from the existing history response (no API change) |
| 11 | **Per-teacher car sheets keep their seven columns.** The sort adds the teacher tie-break, which is constant inside one teacher's file |

## Global Constraints

- Every stored request of the Publication is listed exactly once, on its car's sheet; the summary counts across all grids add up to the rows across car sheets.
- Hebrew and right-to-left on every sheet; plain hyphen only (`ExcelTypographyTest` covers the school file too).
- No rank, national ID or phone anywhere in the workbook.
- No comments in code except `//given //when //then`; no hardcoded user-visible strings in the client.

## Tasks

### Task 1: Read models
- [x] `SlotRequestDetail` gains `string TeacherName` after `StudentName`; update the three test helpers and `SubmissionQueries` (join `Teachers` with `IgnoreQueryFilters([QueryFilters.TeacherNotDeleted])` on the Week Schedule's teacher)
- [x] `ISubmissionQueries.GetSlotRequestCountsAsync(Guid publicationId)` and `GetSlotRequestDetailsAsync(Guid publicationId)`: the per-teacher versions narrow the publication-wide query
- [x] `IWeekScheduleQueries.FindByWeekAsync(DateOnly weekStart)` → `IReadOnlyList<TeacherWeekGrid>`

### Task 2: Sheets (TDD)
- [x] `RequestDetailSheet.AddWithTeacherTo`: header, title merged A1:H1, teacher column, teacher tie-break in the sort
- [x] `SummarySheet.AddForTeachersTo`: one titled block per Teacher, seven rows apart

### Task 3: Generator, interactor, endpoint (TDD)
- [x] `IExcelGenerator.GenerateForSchoolAsync(PublicationId)`; `ExcelGenerator` builds `סיכום` + car sheets with the teacher column
- [x] `DownloadPublicationSchoolExcelInteractor` (`PublicationNotFoundException` when missing), registered in DI
- [x] `PublicationSchoolExcelQueryController.DownloadSchoolExcel`, Administrator policy; `ControllerAuthorizationTest` pins it
- [x] `ExcelTypographyTest` also checks the school file

### Task 4: Client
- [x] `PublicationsApiService.downloadSchoolExcel(id)`; `PublicationsStore.downloadSchoolExcel(publicationId?, weekStart?)` saving `week-{date}-school.xlsx`
- [x] Dashboard: "Download all teachers" button for Administrators in Open and Closed
- [x] History: `withSchoolRows` pure function in `domain\history-rows.ts` (spec'd); page renders the week row with its own action
- [x] `en.json` / `he.json` keys

### Task 5: Docs, checks, push
- [x] requirements §9 (school file section), roadmap slice 7 row + locked decision 10
- [x] Client `npm test` + build
- [ ] Backend `dotnet test`: the .NET SDK could not be installed in the session (network policy), so CI runs it
- [x] Push `excel-school-file`. No PR unless your human partner asks for one
