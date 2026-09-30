# US-44: Versioned Excel Email — Task Index (excel slice 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 3 of the [Excel roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** At every window close each teacher really receives the two-sheet Excel in their inbox — a Hebrew email whose subject names the week, the teacher and an incrementing version (`בקשות לשבוע 25 - משה כהן - גרסה 2`), with the file attached under that same name — and one teacher's failed send never costs another teacher their file or stops the app. Covers GitHub issue [#44](https://github.com/silagy/DrivingLessonsBooking/issues/44) (US-44).

**Architecture:** `PublicationClosedHandler` (Application) keeps its loop over the publication's `TeacherVersions`, but now writes the Hebrew subject and body, names the attachment after the subject, and isolates each teacher in a `try/catch` that logs and moves on. A static `EmailMimeMessage` (Infrastructure, MimeKit) turns an `EmailMessage` into a MIME message — plain-text part, right-to-left HTML part, the Excel attachment — and is unit-tested. `LoggingEmailSender` becomes `SmtpEmailSender` (MailKit): when `Email:Enabled` is false it logs the skip exactly as today; when true it sends over SMTP to AWS SES (ADR 0002), or to a local Mailpit container in development. `EmailOptions` grows the SMTP settings and validates them at startup.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL / Quartz; **MailKit 4.18.1** (new, Infrastructure); **Microsoft.Extensions.Logging.Abstractions 10.0.9** (new, Application); MSTest + Shouldly + FakeItEasy; Mailpit (`axllent/mailpit`, local smoke only). No client change, no migration.

**Spec:** issue [#44](https://github.com/silagy/DrivingLessonsBooking/issues/44) · [Excel roadmap](../README.md) (locked decisions 1–6; slice 3 row) · [requirements §4, §5.1, §6.4, §9 Delivery, §11 decision 8](../../../requirements.md) · [ADR 0002](../../../decisions/0002-hosting-aws-lightsail.md) (AWS SES) · [tech-stack.md — Background Processing, Hosting & Operations](../../../tech-stack.md) · slice 1 and 2 open item 3 (the file name) in [US-46](../us-46-excel-detail-sheet-plan/README.md) / [US-45](../us-45-excel-summary-sheet-plan/README.md)

**Branch:** `44-us-44-versioned-excel-email` (created from `origin/main` at planning time, after US-45 merged)

## User Story

**US-44** ([#44](https://github.com/silagy/DrivingLessonsBooking/issues/44)) — *As a teacher, I want to receive the Excel file by email at every window close with a versioned subject, so that I can start booking immediately and trust I hold the latest file.*
- **Given** my week's Publication reached its window end **and Given** my contact email is on record **When** the close event fires **Then** I receive an email with the two-sheet Excel attached and a subject like "Week 25 Requests - Teacher Cohen - v1", incrementing on every subsequent close.

Teachers have no login — email is their only interface with the system (§4).

## Context

The publications module built the whole close path around a placeholder. `ClosePublicationJob` (Quartz, at the window end) and `PublicationReconciliationHostedService` (at startup, for windows that ended while the app was down) both call `ClosePublicationInteractor`. `Publication.Close(teacherIds)` bumps a `TeacherExcelVersion` per teacher with a week schedule (v1 on the first close, +1 on every close after a reopen) and raises `PublicationClosed`. `DrivingLessonsDbContext.CommitAsync` **saves first, then dispatches** the event to `PublicationClosedHandler`, which generates each teacher's file (`ExcelGenerator`, now both Hebrew sheets after US-45/46), builds the English subject `Week {n} Requests - {name} - v{k}` and calls `IEmailSender` — `LoggingEmailSender`, which only logs.

Two consequences shape this slice:
- Because the close is already saved when the handler runs, an exception from the handler never undoes the close — it only stops the loop, so every later teacher silently gets nothing, and the close is never retried (a second close throws `PublicationMustBeOpenException`).
- The startup reconciliation runs inside `IHostedService.StartAsync`: today an exception there **stops the app from booting**. With a real SMTP server, "the mail server is down when the app restarts" becomes a boot failure.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **SMTP via MailKit to AWS SES** (human partner's choice at planning time over the AWS SDK and `System.Net.Mail`). SES stays the provider (ADR 0002); the SMTP interface keeps the code provider-agnostic and lets a local **Mailpit** container catch the mail, so the smoke checks the real message end to end. Recorded as ADR 0005 in task 2. |
| 2 | **The whole email is Hebrew** (human partner's choice over the AC's English example, which says "a subject *like*"). Subject `בקשות לשבוע {week} - {teacher name} - גרסה {version}`; body three lines: `שלום {name},` / `מצורף קובץ הבקשות לשבוע {week}, גרסה {version}.` / `קובץ עם מספר גרסה גבוה יותר מחליף את כל הקבצים הקודמים של אותו שבוע.` The wording lives in `PublicationClosedHandler` (it already builds the subject; roadmap decision 6). Task 1 updates the §6.4 example and adds decision #22 to requirements. |
| 3 | **The attachment is named after the subject** (`בקשות לשבוע 41 - משה כהן - גרסה 2.xlsx`), so the file on the teacher's disk carries its version too. `\ / : * ? " < > \|` in a teacher's name become `-` in the file name only; the subject keeps the name as entered. The admin download keeps `ExcelGenerator`'s `week-{date}-{teacherGuid}.xlsx` — `ExcelGenerator` is untouched. |
| 4 | **Each teacher's send is isolated** (human partner's choice over retries): the handler catches any exception from one teacher's generate-and-send, logs it at Error with the teacher and publication **GUIDs only** (no address, no name — code-style logging rule), and continues. No retry in v1; the admin's Download button is the fallback. This also keeps a mail outage from stopping the app at boot. `Microsoft.Extensions.Logging.Abstractions` is added to Application for `ILogger<T>`. |
| 5 | **The right-to-left HTML is built in Infrastructure** by `EmailMimeMessage.Create(EmailMessage, EmailOptions)`: the text part is the body as-is; the HTML part is `<div dir="rtl" lang="he">` with one `<p>` per body line, every line HTML-encoded. `EmailMessage` keeps its shape (`ToEmail, Subject, Body, Attachment`). Every email this system sends is Hebrew, so the sender renders every body RTL. |
| 6 | **`LoggingEmailSender` → `SmtpEmailSender`** (file, class, DI), like `PlaceholderExcelGenerator` → `ExcelGenerator` in slice 1. Disabled, it logs the existing `Email disabled. Skipped send…` line unchanged. Enabled, it connects, authenticates only when a username is set, sends, and disconnects, with a **30-second** SMTP timeout so a dead server cannot hold a close (or the boot) for MailKit's 2-minute default. |
| 7 | **`EmailOptions` grows the SMTP settings** — `Host`, `Port` (default 587, `[Range(1, 65535)]`), `Security` (MailKit `SecureSocketOptions`, default `StartTls`), `Username`, `Password`, `FromAddress`, `FromName` — and implements `IValidatableObject`: when `Enabled`, a host and a valid from address are required, and a username needs a password. The existing `ValidateOnStart()` turns a half-configured production into a boot error instead of a silent failure at the first close. |
| 8 | **Configuration:** `appsettings.json` carries the full shape with blanks (no secrets committed); `appsettings.Development.json` points at Mailpit (`localhost:1025`, `Security: None`, `noreply@local.dev`) but stays **`Enabled: false`**, so a developer without Mailpit keeps today's behavior — the smoke turns it on with `--Email:Enabled=true`. `docker-compose.yml` passes `EMAIL_*` variables (default disabled); `.env.example` documents SES SMTP; `running-the-project.md` gains an "Email locally" section. |
| 9 | **Tests:** `tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs` (beside `WeekScheduleCreatedHandlerTest`) and `tests\DrivingLessons.Application.Test\Emails\` for `EmailMimeMessageTest` and `EmailOptionsTest`. The folder is **`Emails`, not `Email`**: a `DrivingLessons.Application.Test.Email` namespace shadows the domain `Email` value object in every sibling test file (`Email.Of(…)` stops compiling — checked at planning time). |
| 10 | **The smoke runs on a throwaway database** (`drivinglessons_us44_smoke`) with a throwaway Mailpit container (`dl-mailpit`). JSON bodies containing Hebrew are sent from UTF-8 files (`--data-binary @file`): Git Bash's `curl -d "…"` turns Hebrew arguments into `???` before they leave the shell (seen at planning time). |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- DI registrations go in each project's `DependencyInjection.cs` (`AddInfrastructure`, `AddApplication`).
- Application tests build real domain objects (`Publication.Create(WeekStart.Of(…))`, `Teacher.Create(TeacherName.Of(…), Email.Of(…))`) — `DrivingLessons.Application.Test` does not reference the domain fake builders.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null** — configure `null` explicitly where a test needs it.
- Infrastructure classes are tested from `DrivingLessons.Application.Test` (it references Infrastructure), like `RosterCsvParserTest` and the Excel sheet tests.
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- Every window close emails every teacher with a week schedule for that publication, at their contact email (§5.1, §6.4, §9 Delivery).
- The subject carries a version that increments on every close of the same publication (§6.4, decision #8) — it is `TeacherExcelVersion.Version`, never recomputed.
- The attachment is the same two-sheet file `IExcelGenerator` produces for the admin download (§9, roadmap decision 2).
- No PII in new log lines: teacher and publication GUIDs only (code-style.md Logging).
- SMTP credentials come only from configuration / environment variables; nothing secret is committed.
- New packages: exactly `MailKit` 4.18.1 (Infrastructure) and `Microsoft.Extensions.Logging.Abstractions` 10.0.9 (Application). No migration, no client change.
- Layer dependencies hold: Application → Domain; Infrastructure → Application, Domain, MailKit.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **The mail server is unreachable when a window closes** → the close still stands, every other teacher is still tried, one Error line per failed teacher (GUIDs only), the app keeps serving → task 1 `A_Failed_Send_Is_Logged_And_The_Next_Teacher_Still_Gets_Their_File`, task 3 Step 5.
2. **A window ended while the app was down, and the mail server is down at restart** → the app still boots, closes the publication, logs the failed sends → task 3 Step 6.
3. **A teacher's name is Hebrew or holds a character a file name cannot** (`דנה לוי/בן דוד`) → the subject shows the name intact; the attachment name replaces the `/` → task 1 `Replaces_Characters_A_File_Name_Cannot_Hold`, task 3 Step 4.
4. **The admin reopens and the window closes again** → the next email says `גרסה 2` in both subject and attachment name → task 1 `Subject_Carries_The_Next_Version_After_A_Reopen_And_Close`, task 3 Step 4.
5. **Production is deployed with email enabled but half-configured** (no host, no from address, a username without a password) → the app refuses to start with a message naming the setting, instead of failing silently at the first close → task 2 `Enabled_Email_Must_Have_Its_Smtp_Settings`, task 3 Step 7.

Also pinned: a teacher deleted since the close → skipped, the others sent (task 1 `Skips_A_Teacher_Deleted_Since_The_Close`); the Excel failing for one teacher → the others sent (task 1 `A_Failed_Excel_Is_Logged_…`); markup in a teacher's name → encoded in the HTML part (task 2 `Encodes_Markup_In_The_Html_Body`); the attachment is the real two-sheet workbook (task 3 Step 4).

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-hebrew-versioned-email.md](task-01-hebrew-versioned-email.md) | Application — Hebrew subject/body, attachment name, per-teacher failure isolation (TDD); requirements §6.4 + decision #22 | ✅ own commit |
| 2 | [task-02-smtp-sender.md](task-02-smtp-sender.md) | Infrastructure — `EmailMimeMessage`, `SmtpEmailSender` (MailKit), `EmailOptions` validation (TDD); config, compose, `.env.example`, ADR 0005, dev docs | ✅ own commit |
| 3 | [task-03-smoke-verification-and-pr.md](task-03-smoke-verification-and-pr.md) | Mailpit smoke on a throwaway DB — v1 and v2 emails, Hebrew name, attachment, mail-server-down at close and at boot, bad config; full check; PR | — (no source change) |

## How to Run a Task

1. Confirm you are on branch `44-us-44-versioned-excel-email` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` and two US-45 plan files (`docs\modules\excel\us-45-excel-summary-sheet-plan\task-01…`, `task-02…`) carry unrelated local modifications from before this branch — **never stage them** (`git add` only the paths each task lists). Task 3 adds a local-only `api-smoke` entry to `launch.json` and removes it at the end.

Backend commands run from the repo root, in bash. Environment notes (project memory):
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`) — `dl-postgres` has a stale migration history.
- mingw `curl` cannot read MSYS `/tmp` paths from `mktemp`: keep smoke files under the repo-relative, git-ignored `.superpowers\sdd\us-44-smoke\`.
- The default npm is broken for installs (not needed in this slice); browser-pane screenshots are flaky on this PrimeNG app — prefer logs and API evidence.

## Open Items (non-blocking)

1. **No retry and no failure record** (decision 4): a send that fails is only in the logs. If it happens in practice, the next step is a `last_email_error` on `TeacherExcelVersion` shown on the dashboard with a "Resend" button — a domain change, deliberately not in v1.
2. **SES setup is operational, not code**: verify the sending domain (or address), request production access (leave the SES sandbox, which only delivers to verified recipients), create SES SMTP credentials, fill the `EMAIL_*` variables on the Lightsail host. Task 2's `.env.example` and ADR 0005 list these; nobody has done them yet.
3. **The admin download file name** (`week-{yyyy-MM-dd}-{teacherGuid}.xlsx`) is unchanged — only the email attachment is renamed (decision 3). Renaming the download would mean passing the teacher's name into `ExcelGenerator`; do it if the admin asks.
4. **No Reply-To / school branding**: the From name is configuration (`EMAIL_FROM_NAME`); there is no logo, signature or reply address.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Application\
├── DrivingLessons.Application.csproj                                    + Microsoft.Extensions.Logging.Abstractions
└── EventHandlers\PublicationClosedHandler.cs                            Hebrew subject/body, attachment name, isolation
src\DrivingLessons.Infrastructure\
├── DrivingLessons.Infrastructure.csproj                                 + MailKit
├── Email\EmailMimeMessage.cs                                            new
├── Email\SmtpEmailSender.cs                                             renamed from LoggingEmailSender.cs; sends over SMTP
├── Options\EmailOptions.cs                                              SMTP settings + validation
└── DependencyInjection.cs                                               IEmailSender → SmtpEmailSender
src\DrivingLessons.Presentation.Web\appsettings.json                     Email section shape
src\DrivingLessons.Presentation.Web\appsettings.Development.json         Email → Mailpit (disabled by default)
tests\DrivingLessons.Application.Test\EventHandlers\PublicationClosedHandlerTest.cs   new
tests\DrivingLessons.Application.Test\Emails\EmailMimeMessageTest.cs                  new
tests\DrivingLessons.Application.Test\Emails\EmailOptionsTest.cs                      new
docker-compose.yml · .env.example                                        EMAIL_* variables
docs\decisions\0005-email-over-ses-smtp.md                               new
docs\requirements.md                                                     §6.4 example, decision #22
docs\tech-stack.md · docs\development\running-the-project.md             email transport, Mailpit
docs\modules\excel\README.md                                             slice 3 row links this plan
```
