# WhatsApp Reminders for Students Who Haven't Submitted - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Each task has its own file. Execute in order.

**Goal:**
- While a Publication is `Open`, the dashboard shows a section under the week grid that lists the active Students who have no Submission in this Publication.
  - An Administrator sees every Teacher's Students, filtered to one Teacher or all Teachers (the default).
  - A Teacher-role User sees only their own Teacher's Students.
- Each Student with an Israeli mobile number has a WhatsApp button. It opens `https://wa.me/{number}?text={message}` in the sender's own WhatsApp, with a fixed Hebrew reminder already written.

**Architecture:**
- **Domain:** `PhoneNumber.ToWhatsAppNumber()` returns a new `WhatsAppNumber` value, or `null` when the phone isn't an Israeli mobile.
- **Backend:** one query, `IStudentQueries.FindWithoutSubmissionAsync(publicationId, teacherId?)`, serves two read endpoints:
  - `GET api/publications/{id}/students-without-submission?teacherId=` on `PublicationQueryController`, for a Teacher or an Administrator, scoped with `MayReach` like `/dashboard`;
  - `GET api/publications/{id}/school-students-without-submission`, for Administrators only. It lives on today's `PublicationSchoolExcelQueryController`, renamed `PublicationSchoolQueryController`.
- **Client:**
  - `PublicationsStore` loads the list while the Publication is `Open`: the school list for an Administrator, the Teacher's list for a Teacher. It holds the Administrator's teacher filter and builds each row's WhatsApp link from the Hebrew translation, whatever language the UI is in.
  - A new `StudentsWithoutSubmissionComponent` renders the section.
  - Nothing is written anywhere: no command, no domain event, no migration.

**Tech Stack:** .NET (ASP.NET Core, EF Core, Npgsql), MSTest + Shouldly + FakeItEasy, Angular 21 (standalone, zoneless, OnPush, signals), PrimeNG 21, Transloco, SCSS, Vitest under jsdom.

**Spec:** [WhatsApp Reminders - Intent](../whatsapp-reminders-intent.md) (3 November 2026), decisions 1 to 5. Rules: `.claude\rules\code-style.md`, `ddd-architecture.md`, `domain-building-blocks.md`, `api-guidelines.md`, `domain-testing.md`, `client-architecture.md`, `client-state.md`, `client-primeng.md`, `client-i18n.md`. Design: the canvas [DriveCal WhatsApp Reminders](https://claude.ai/artifact/CwEh4rfo2W18iEqJPTXMFT) (3 November 2026; private until shared). Its frames:
- **Dashboard (desktop) - Administrator, Open**: the section under the grid, with a working teacher filter;
- **Phone - Administrator**;
- **Phone - Teacher-role User**: no filter, no teacher name;
- **What opens in WhatsApp**: the chat with the message already written;
- **Section states**: loading, everyone submitted, load failed.

The design uses DriveCal's own tokens. The org's Comply365 design system serves no token files, and the app's palette already comes from it. Where the design shows a hex value, the code uses the matching `--app-*` token.

**Branch:** `whatsapp-reminders`, from `origin/main` at `bac6d1c` (no upstream set; push with `-u origin whatsapp-reminders`). The intent and this plan are committed on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR. Auto-merge is off, so merge by hand with `--merge`.

## Task files - execute in order

| # | File | What it does |
|---|------|--------------|
| 0 | [task-00-baseline.md](task-00-baseline.md) | Build and run every suite; record the counts later tasks compare against |
| 1 | [task-01-whatsapp-number.md](task-01-whatsapp-number.md) | `WhatsAppNumber` and `PhoneNumber.ToWhatsAppNumber()`, with domain tests |
| 2 | [task-02-students-without-submission-api.md](task-02-students-without-submission-api.md) | Query, two interactors, two endpoints, authorization test, API smoke |
| 3 | [task-03-reminders-store.md](task-03-reminders-store.md) | Client response and API calls, the shared week range, the reminder link, store state |
| 4 | [task-04-reminders-section.md](task-04-reminders-section.md) | The section component, translation keys, dashboard wiring |
| 5 | [task-05-docs-verification-and-pr.md](task-05-docs-verification-and-pr.md) | Requirements, browser verification, full suites, PR |

Each task file is self-contained and ends with a verification and a commit. Do not start a task before its predecessor is committed.

## Current State

Backend facts the tasks rely on:
- `Student` has `Name` (`StudentName`), `Phone` (`PhoneNumber`), `TeacherId`, `CarId` and `IsActive`.
- `PhoneNumber.Of` keeps the text as entered: bidi marks removed, non-breaking spaces made plain, trimmed, at least 7 digits.
- One Submission per Student per Publication (`PublicationId`, `StudentId`, `WeekScheduleId`).
- A Publication and its Week Schedules share a `WeekStart`, and a Teacher has at most one Week Schedule per week. `IStudentQueries.GetActiveByNationalIdAsync(nationalId, weekStart)` already joins a Student to their Teacher's Week Schedule this way.
- `CurrentUserExtension.MayReach(TeacherId)` is true for an Administrator, or for the Teacher-role User linked to that Teacher.
- `ControllerAuthorizationTest` pins every endpoint's policy by `Controller.Action` name.
- There are no database-level query tests. Interactor tests fake `I*Queries`, and EF queries are checked by an API smoke.

Client facts:
- `PublicationsStore` loads the dashboard with a `resource` keyed on the Publication and the selected Teacher. `refresh()` reloads the Publication, and every dependent resource follows.
- `shareLink()` builds the weekly link from `document.location.origin`. The backend has no public URL setting, so the message must be built on the client.
- `formatWindowInstant(utcIso, locale)` is in `shared\dates\jerusalem-time.ts`. The student form's `weekRangeLabel(weekStart: string, locale)` lives in `features\student-form\domain\week-label.ts`, where another feature can't import it. Publications has its own, different `weekRangeLabel(Date, locale)` in `domain\week-options.ts`.

## Decisions

| # | Decision |
|---|----------|
| 1 | **Placement: a card under the week grid**, shown only in the `Open` case of the dashboard, after the grid card (grill, 3 November 2026). Its title carries the count. `Draft`, `Published` and `Closed` don't show it (intent §4.2). |
| 2 | **Administrator filter: a select above the list**, "All teachers" (the default, `null`) or one Teacher. It is independent of the page's teacher picker (intent decision 5). Rows show the Teacher's name only in the Administrator's view. The filter resets to all when the Publication changes. |
| 3 | **Phones: Israeli mobiles only** (grill). `ToWhatsAppNumber()` accepts a number made only of digits, spaces, `-`, `(`, `)`, `.` and one leading `+`, whose digits are one of: <ul><li>`05` + 8 digits (10 digits), which becomes `9725` + the last 8;</li><li>`9725` + 8 digits (12);</li><li>`009725` + 8 digits (14), which drops the `00`.</li></ul> A leading `+` is allowed only before `9725`. Anything else returns `null`: landlines, foreign numbers, two numbers in one field, letters. |
| 4 | **The message is the intent §4.4 text as written** (grill). It is the Transloco key `publications.reminders.message`. The store always translates it with the `he` language, loading `he` first if the UI is English. `en.json` holds an English translation for key parity (`client-i18n.md` rule 4); the app never sends it. |
| 5 | **Message values** use the Hebrew locale `he-IL`, whatever the UI language: <ul><li>`weekRange` comes from the student form's `weekRangeLabel`, moved to `shared\dates\week-label.ts` (with its spec, `weekDates` and `dateInWeek`), so the message reads the week the way the form shows it;</li><li>`closesAt` is `formatWindowInstant(windowEndUtc, 'he-IL')`;</li><li>`link` is `shareLink()`;</li><li>`teacherName` comes from the row.</li></ul> |
| 6 | **The button is a link, not a script:** `<a pButton [href]="row.whatsAppUrl" target="_blank" rel="noopener">` with `pi pi-whatsapp`. That way no popup blocker can stop it, and it works with a keyboard and on a phone. Its `aria-label` names the Student. A row without a WhatsApp number shows the phone and `publications.reminders.noWhatsApp`, with no button. |
| 7 | **Two endpoints, one query.** Teacher-scoped reads stay on `PublicationQueryController` (`TeacherOrAdministrator`), and school-wide reads go on the Administrator-only controller, the same split as `/excel` and `/school-excel`. `PublicationSchoolExcelQueryController` is renamed `PublicationSchoolQueryController` so it can hold both. |
| 8 | **The backend doesn't check the Publication's state.** The list is a read; showing it only while `Open` is presentation (decision 1). A missing Publication is a 404. A Teacher with no Week Schedule that week gets `[]`. Another Teacher's list is a 404, as `/dashboard` does. |
| 9 | **Order: Teacher name, then Student name, then Student id** in the query. The client keeps that order and only filters. |
| 10 | **No write and no tracking** (intent decision 2). Pressing a button changes nothing. A Student leaves the list when they submit and the page reloads. |

**To confirm:** none.

## Global Constraints

- **Backend:**
  - Raw `Guid` only at the controller, the interactor's `ExecuteAsync` and the `I*Queries` boundary.
  - Value objects with private constructors. No comments; tests use `//given //when //then`.
  - Student names and phones are PII: never logged.
- **Client:**
  - Standalone, OnPush, signals; stores expose readonly signals and components never subscribe; no new dependency.
  - Every new user-visible string is a Transloco key in both `client\public\i18n\he.json` and `en.json`.
  - No en dash, em dash or ellipsis character in source or translations (`source-text.spec.ts`, `translations.spec.ts`).
  - Logical CSS properties; tokens only, no hex in component SCSS; no `!important`.
  - Component style budget: 4 kB warning, 8 kB error.
- **Phone rules** (admin-mobile): below 640px, no horizontal page scroll at 360px and 390px; body text ≥ 13px; tap targets ≥ 44px; nothing `nowrap` except buttons and tags; phones in `<bdi dir="ltr">`.
- **Commands:**
  - Client, from `client\` in PowerShell, with the local CLI: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false` and `... ng.js build --project client`. To run one spec, add `--include src/app/<path>.spec.ts`.
  - If `client\node_modules` is missing, run `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci` from `client\` (memory: default npm is broken for install).
  - Backend, from the repo root: `dotnet build`, `dotnet test`.
- **Dev servers:**
  - The API runs against the compose Postgres (`drivinglessonsbooking-postgres-1`), never `dl-postgres`.
  - Hebrew request bodies go in files, passed as relative `@paths` (memory: Hebrew in curl argv becomes `?????`).
  - Before measuring in the browser pane, `await document.fonts.ready`. Prefer `javascript_tool` over screenshots.
- **Commits:** stage by explicit path, never `git add -A`.

## Naming contract

| Name | Kind | Task |
|------|------|------|
| `WhatsAppNumber` (`Value`: digits only, e.g. `972501234567`) | Domain value object | 1 |
| `PhoneNumber.ToWhatsAppNumber(): WhatsAppNumber?` | Domain method | 1 |
| `ItemForFindStudentsWithoutSubmissionResponse { Guid StudentId; string StudentName; Guid TeacherId; string TeacherName; string Phone; string? WhatsAppNumber }` | Application DTO | 2 |
| `IStudentQueries.FindWithoutSubmissionAsync(Guid publicationId, Guid? teacherId): Task<IReadOnlyList<ItemForFindStudentsWithoutSubmissionResponse>?>` | Query | 2 |
| `FindStudentsWithoutSubmissionInteractor.ExecuteAsync(Guid publicationId, Guid teacherId)` | Interactor | 2 |
| `FindSchoolStudentsWithoutSubmissionInteractor.ExecuteAsync(Guid publicationId)` | Interactor | 2 |
| `PublicationQueryController.FindStudentsWithoutSubmission`, `GET {id}/students-without-submission?teacherId=` | Endpoint | 2 |
| `PublicationSchoolQueryController.FindSchoolStudentsWithoutSubmission`, `GET {id}/school-students-without-submission` | Endpoint | 2 |
| `ItemForFindStudentsWithoutSubmissionResponse` (`studentId`, `studentName`, `teacherId`, `teacherName`, `phone`, `whatsAppNumber: string \| null`) | Client type | 3 |
| `PublicationsApiService.findStudentsWithoutSubmission(id, teacherId)`, `findSchoolStudentsWithoutSubmission(id)` | Client API | 3 |
| `shared\dates\week-label.ts` (`weekDates`, `dateInWeek`, `weekRangeLabel`) | Moved module | 3 |
| `whatsAppChatUrl(whatsAppNumber, message): string`, `reminderMessageParams(input): ReminderMessageParams`, `REMINDER_LOCALE = 'he-IL'` | Client domain, `domain\whatsapp-reminder.ts` | 3 |
| `ReminderRow { studentId; studentName; teacherName; phone; whatsAppUrl: string \| null }` | Client model | 3 |
| `PublicationsStore.reminderRows`, `reminderTeacherId`, `remindersAreLoading`, `remindersError`, `selectReminderTeacher(id \| null)` | Store | 3 |
| `publications.reminders.*` (`title`, `hint`, `teacherFilter`, `allTeachers`, `send`, `sendAria`, `noWhatsApp`, `empty`, `loadFailed`, `message`) | Translation keys | 3 (`message`), 4 (rest) |
| `StudentsWithoutSubmissionComponent` (`app-students-without-submission`) | Client component | 4 |

## Review Focus

1. **A Student who submitted and then had Change Teacher applied.** Expected: listed under no Teacher, in either view. Task 2 smoke, step 2.6.
2. **The UI in English.** Expected: the WhatsApp text is still Hebrew, with Hebrew dates, even when `he.json` wasn't loaded before. Task 3, `writes the message in Hebrew when the UI is English`.
3. **A phone the Roster wrote with bidi marks, spaces or `+972`.** Expected: a working number. Two numbers in one field, a landline, or a foreign number: no button and no exception. Task 1 data rows.
4. **A Teacher-role User asking for the school list or another Teacher's list.** Expected: 403 for the school list (policy), 404 for another Teacher. Task 2, `Other_Teachers_List_Is_Not_Found` and `ControllerAuthorizationTest`.
5. **A long Hebrew name and a long teacher name at 360px.** Expected: they wrap, the 44px button stays on the row, and there's no sideways scroll. Task 5 step 5.4.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Values\WhatsAppNumber.cs` | New | 1 |
| `src\DrivingLessons.Domain\Values\PhoneNumber.cs` | `ToWhatsAppNumber()` | 1 |
| `tests\DrivingLessons.Domain.Test\Values\PhoneNumberTest.cs` | Conversion tests | 1 |
| `src\DrivingLessons.Application\Queries\FindStudentsWithoutSubmission\*` | Response, two interactors | 2 |
| `src\DrivingLessons.Application\Queries\IStudentQueries.cs`, `Infrastructure\EntityFramework\Queries\StudentQueries.cs` | `FindWithoutSubmissionAsync` | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Two registrations | 2 |
| `src\DrivingLessons.Presentation.Web\Controllers\Publication\PublicationQueryController.cs` | Teacher endpoint | 2 |
| `...\Controllers\Publication\PublicationSchoolExcelQueryController.cs` → `PublicationSchoolQueryController.cs` | Rename, school endpoint | 2 |
| `tests\DrivingLessons.Application.Test\Queries\FindStudentsWithoutSubmissionInteractorTest.cs`, `FindSchoolStudentsWithoutSubmissionInteractorTest.cs` | New | 2 |
| `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` | New endpoint names, renamed controller | 2 |
| `client\src\app\features\student-form\domain\week-label.ts` + `.spec.ts` → `client\src\app\shared\dates\` | Moved; imports updated | 3 |
| `client\src\app\features\publications\data\item-for-find-students-without-submission.response.ts`, `publications-api.service.ts` | Type, two calls | 3 |
| `client\src\app\features\publications\domain\whatsapp-reminder.ts` + `.spec.ts`, `reminder-row.model.ts` | New | 3 |
| `client\src\app\features\publications\state\publications.store.ts` + `.spec.ts` | Reminder resource, filter, rows | 3 |
| `client\public\i18n\he.json`, `en.json` | `publications.reminders.*` | 3, 4 |
| `client\src\app\features\publications\ui\components\students-without-submission\*` | New component + spec | 4 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\*` | Section in the `Open` case | 4 |
| `docs\requirements.md`, `docs\modules\publications\whatsapp-reminders-intent.md`, this README | §3, §6.3, §6.5, §11 #27; intent status; Manual verification | 5 |

## Out of scope

- Remembering who was reminded, and any "remind everyone" action (intent decision 2, §3 non-goals).
- WhatsApp Cloud API, templates, outbox (the `docs/notifications-intent` branch).
- The list on a `Closed` Publication (intent §3 Later).
- Editable message text.

## Manual verification

Task 5, 3 November 2026, against the running app (API on port 5480, the client dev server proxied to it, the compose Postgres) in the built-in browser. Publication `8c1e95c9` (week 2026-10-25) is the Open week from Task 2. The dev data holds real-looking names and phones, so the walk-through compared through the DOM and the API and recorded counts only. Test variations, all fake and all reverted afterwards: one Student's phone set to a landline (`03-6123456`), another renamed to a 40-character Hebrew name with a long unbroken phone (three, then five, numbers joined without spaces), one Teacher's name made 36 characters long, a throwaway Teacher with no Week Schedule (soft-deleted after), and Task 2's smoke Teacher-role User restored with a temporary password (soft-deleted again after).

| # | Check | Result |
|---|-------|--------|
| 1 | Administrator, Hebrew: the list against `school-students-without-submission` | 54 rows = 54 from the API, same order, names, teachers and phones; 2 Teachers; the hrefs are `https://wa.me/{whatsAppNumber}?text=` exactly where the API has a number |
| 1 | Page teacher picker set to another Teacher | still 54 rows, 2 Teachers, filter on "כל המורים" |
| 1 | Filter to one Teacher, then back | 29 rows (= SQL), 1 Teacher; back to all: 54 |
| 1 | Filter to the Teacher with no Week Schedule | offered; "עוד לא שלחו (0)" and "כל התלמידים שלחו.", filter still shown (the deferred minor, as reviewed) |
| 2 | Submit for a listed Student (API, the weekly link) while the page is open, then Refresh | 53 → 52, the Student gone |
| 3 | Decode a row's `text` | "שלום {name}, עוד לא קיבלנו מכם את השיעורים האפשריים לשבוע 44 (25-30 באוק׳) אצל {teacher}." then "אפשר לשלוח אותם עד יום חמישי, 5 בנוב׳, 11:21 בקישור הזה:", the weekly link (same as the share box), and the closing line: intent §4.4. 11:21 is the window end 09:21Z in Asia/Jerusalem |
| 4 | English UI | every section string English ("Haven't submitted yet (54)", "Teacher", "All teachers", "WhatsApp", "Not a WhatsApp number", aria "Send {name} a WhatsApp reminder"); the decoded text is the same Hebrew |
| 4 | English UI with `/i18n/he.json` unavailable (file moved aside for one page load, then put back) | "Haven't submitted yet" with "Couldn't load the list. Refresh the page." (`role="alert"`), no rows, no "everyone submitted" |
| 5 | Landline and the unbroken multi-number phone | phone shown, "המספר לא מתאים לוואטסאפ", no button |
| 6 | Teacher-role User (smoke User, linked to a Teacher with 29 missing) | Hebrew and English: 29 rows = own API list, same order, no teacher column, no filter, Hebrew message; the school list 403, another Teacher's list 404 |
| 7 | Closed week (2026-10-18) and Draft week (2026-09-27); the current week with no Publication | no section. No Published-state Publication exists in the dev data; that state is covered by the store test |
| 8 | Writes | `pg_stat_user_tables` insert+update+delete total 13 before and after two list loads; the API log's only `UPDATE`s are the test setup's |
| 9 | Desktop (1024) with the stress phone | phone column 150px, wraps inside it, no overlap with name or button |

Phone measurement (admin-mobile) on `/publications?week=2026-10-25`, Administrator, after the fix below:

| Width | Lang | Scheme | `scrollWidth` | `pageOverflows` | `clippedAtInlineEdge` | `shortTargets` | `smallText` | Stress row | Buttons |
|-------|------|--------|---------------|-----------------|-----------------------|----------------|-------------|------------|---------|
| 390 | he | light | 390 | false | [] | [] | [] | name 2 lines, phone 2 lines, inside row | 52 of 52 at 44px, on their rows |
| 390 | en | dark | 390 | false | [] | [] | [] | name 2, phone 2, inside | 52/52 |
| 360 | he | dark | 360 | false | [] | [] | [] | name 2, phone 2, inside | 52/52 |
| 360 | en | dark | 360 | false | [] | [] | [] | name 2, phone 2, inside | 52/52 |

Teacher-role User at 360 he: `scrollWidth` 360, nothing clipped, no short targets, every button on its row. Below 640px the avatar is hidden, as the Phone frames draw. With the 36-character Teacher name (he, 390 and 360) the teacher line wraps to 2 lines and its row's button stays 44px on the row.

Code fix made by this task: a 54-character unbroken phone overflowed its row at 360px (376px in a 298px row) and widened the page to 407px. `.reminders__phone` now has `overflow-wrap: anywhere`; after it the page is 360 wide and the phone wraps to 2 lines.

Not verified:
- Opening a `wa.me` link. The dev phones may belong to real people, and the browser pane has no WhatsApp sign-in, so no link was opened; the href and its decoded text were checked instead.
- Screenshots. The session's window wasn't visible, so the pane rendered in the background and a capture came back stale (memory note); every check above is a DOM or API measurement.
- Found in passing, outside this feature: with a 36-character Teacher name, the page's teacher picker in `app-publication-week-heading` (`nowrap` label) widens the page to 459px at 390 and 360. That component is unchanged on this branch.
