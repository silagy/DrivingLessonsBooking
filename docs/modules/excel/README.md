# Excel Module — Roadmap

Slice roadmap for the `module:excel` backlog (3 open issues). Each slice ships as its own branch/PR with its own plan folder (`docs\modules\excel\us-XX-...-plan\`, same format as prior modules), authored **just-in-time** when the slice starts so it absorbs what earlier slices taught. This README records the agreed decomposition and the design decisions locked during planning (29 September 2026), so slice plans don't re-litigate them.

**Prerequisite:** the [first-submission slice](../student-form/us-33-first-submission-plan/README.md) (student-form slice 3) — the Excel reads `Submission` / `SlotRequest` rows, and its summary sheet already counts them through `ISubmissionQueries`.

**What exists already:** the publications module shipped the whole delivery path around a placeholder. `IExcelGenerator` (`PlaceholderExcelGenerator`, ClosedXML) is called by the admin download (`GET api/publications/{id}/excel?teacherId=…`, `DownloadPublicationExcelInteractor`) and by `PublicationClosedHandler` at every close, which builds the versioned subject and hands the file to `IEmailSender` (`LoggingEmailSender` — logs, never sends). The summary sheet already shows real per-slot counts (slice-3 decision 16); the detail sheet is a header row only.

## Slices

| Slice | Content | Issues |
|-------|---------|--------|
| 1. Request detail sheet — [plan](us-46-excel-detail-sheet-plan/README.md) | Sheet 2 lists one row per Slot Request with Day, Slot, Student name, National ID, Phone, Transmission, Session type, Rank, Target count, Constraints — sorted by day, slot, rank; Hebrew, right-to-left | [#46](https://github.com/silagy/DrivingLessonsBooking/issues/46) |
| 2. Summary sheet — [plan](us-45-excel-summary-sheet-plan/README.md) | Sheet 1 in Hebrew/RTL: slots × Sunday–Friday grid of request counts, Unavailable slots visibly blocked, Friday Afternoon/Evening cells absent | [#45](https://github.com/silagy/DrivingLessonsBooking/issues/45) |
| 3. Versioned email | A real `IEmailSender` (provider chosen when the slice starts — SES fits the Lightsail hosting, ADR 0002) sending the two-sheet file to each teacher's contact email at every close, subject "Week N Requests - Teacher X - vK" | [#44](https://github.com/silagy/DrivingLessonsBooking/issues/44) |

Slice 1 goes first because the detail sheet is what a teacher books from; slice 3 goes last because it only changes delivery of a file the first two slices finish.

## Locked decisions (29 September 2026)

| # | Decision |
|---|----------|
| 1 | **The workbook is Hebrew and right-to-left.** Headers and enum values (days, slots, transmission, session type) are Hebrew; student names and constraints stay as entered. Wording follows the client's `he.json` (admin wording where admin and student differ: אוטומטי / ידני). Slice 1 does the detail sheet; slice 2 converts the summary sheet. |
| 2 | **One file per teacher per publication** (requirements §9, ADR 0003), generated on demand from current data at download and at every close — no stored snapshot. |
| 3 | **A teacher's file covers the submissions made against that teacher's week schedule** for the publication — the same scope as the dashboard and summary counts (slice-3 decision 5). A student rebound to another teacher keeps their already-submitted rows on the old teacher's file until they revise. |
| 4 | **Every stored request is listed**, including those of students deactivated by a later roster upload and of slots the admin marked Unavailable after the student submitted. The file agrees with the summary counts; the teacher decides what to book. |
| 5 | **Transmission comes from the student's current roster car**, including a car that was soft-deleted since (the Excel must never drop a row because a car was removed from the pool). |
| 6 | **Excel wording is fixed Hebrew text in Infrastructure** (`HebrewExcelLabels`), not a backend i18n system; the email subject stays as `PublicationClosedHandler` builds it until slice 3. |
