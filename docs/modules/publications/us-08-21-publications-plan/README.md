# US-08–21: Publications Module — Task Index

Per-task breakdown of the approved implementation plan (the source of truth is [../us-08-21-publications-plan.md](../us-08-21-publications-plan.md)). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** Implement the Publications module — school-wide weekly Publication lifecycle (`Draft → Published → Open → Closed`, reopenable), unguessable shareable link, automatic window open/close, live per-slot dashboard, per-teacher versioned Excel emailed at every close, on-demand download, and history — covering GitHub issues [#9](https://github.com/silagy/DrivingLessonsBooking/issues/9)–[#22](https://github.com/silagy/DrivingLessonsBooking/issues/22) (US-08…US-21).

**Architecture:** New `Publication` aggregate (one per calendar week, school-wide, [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md)) through all four onion layers, plus the three cross-cutting mechanisms deferred by earlier slices — **domain-event dispatch**, **Quartz scheduling**, **outbound email** — and seams (`IExcelGenerator`, `IEmailSender`, `ISubmissionQueries`) for the not-yet-built Excel and Submission data. A new Angular `publications` feature adds the dashboard + history pages and a Publish entry-point on the weekly-prep page.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL + Quartz.NET + ClosedXML backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco 8 client.

**Branch:** `9-us-08-21-publications-module`

## User Stories

- **US-08** ([#9](https://github.com/silagy/DrivingLessonsBooking/issues/9)) — admin publishes the prepared week by setting one window start + end datetime; the school-wide Publication moves Draft → Published, the window is stored (UTC).
- **US-09** ([#10](https://github.com/silagy/DrivingLessonsBooking/issues/10)) — publishing generates a single unguessable link scoped to the week.
- **US-10** ([#11](https://github.com/silagy/DrivingLessonsBooking/issues/11)) — admin copies the link with one click.
- **US-11** ([#12](https://github.com/silagy/DrivingLessonsBooking/issues/12)) — each Publication's lifecycle state is clearly displayed.
- **US-12** ([#13](https://github.com/silagy/DrivingLessonsBooking/issues/13)) — dashboard shows the day×slot grid with request count per slot, per teacher; Unavailable slots marked blocked.
- **US-13** ([#14](https://github.com/silagy/DrivingLessonsBooking/issues/14)) — dashboard reflects data as of page load; manual refresh only, no push.
- **US-14** ([#15](https://github.com/silagy/DrivingLessonsBooking/issues/15)) — admin downloads the Excel on demand at any time.
- **US-15** ([#16](https://github.com/silagy/DrivingLessonsBooking/issues/16)) — admin extends the window end before close.
- **US-16** ([#17](https://github.com/silagy/DrivingLessonsBooking/issues/17)) — window closes automatically at end time; link becomes read-only.
- **US-17** ([#18](https://github.com/silagy/DrivingLessonsBooking/issues/18)) — Excel automatically emailed to each teacher at close.
- **US-18** ([#19](https://github.com/silagy/DrivingLessonsBooking/issues/19)) — every close increments a per-teacher version carried in the email subject.
- **US-19** ([#20](https://github.com/silagy/DrivingLessonsBooking/issues/20)) — admin reopens a closed window with a new end datetime; returns to Open.
- **US-20** ([#21](https://github.com/silagy/DrivingLessonsBooking/issues/21)) — admin sees the history of past publications per teacher with state + latest version.
- **US-21** ([#22](https://github.com/silagy/DrivingLessonsBooking/issues/22)) — admin re-downloads the Excel of any past publication.

## Decisions (resolved via grilling)

| # | Decision |
|---|----------|
| 1 | **One Publication per calendar week, school-wide** (ADR 0003 #16). Identified by `WeekStart` (Sunday `DateOnly`), unique index on `week_start`. |
| 2 | **Draft is created by a `WeekScheduleCreated` domain-event handler** (idempotent): the first teacher's schedule for a week creates the Draft Publication, later teachers find it. This module builds the domain-event dispatcher the teachers slice deferred. |
| 3 | **State machine** `Draft(10) → Published(20) → Open(30) → Closed(40)`; `Reopen` returns Closed → Open. Non-idempotent transitions → domain guard → 409. |
| 4 | **Quartz.NET** schedules per-publication open/close jobs at publish, reschedules on extend/reopen; a startup hosted service reconciles missed boundaries after downtime. |
| 5 | **Per-teacher version counters** — `Publication` owns `TeacherExcelVersion { TeacherId, Version }`; `Close(teacherIds)` bumps each; the subject carries that teacher's version. |
| 6 | **A Publication covers exactly the teachers with a WeekSchedule that week** — resolved in the Close/query interactors, never inside the aggregate. |
| 7 | **Excel** behind `IExcelGenerator` + ClosedXML placeholder (summary grid wired to counts, detail sheet headers only); `module:excel` fills detail rows later. |
| 8 | **Email** behind `IEmailSender` + `LoggingEmailSender`, gated by `Email:Enabled=false`; real SMTP/SES later. |
| 9 | **Dashboard counts** behind `ISubmissionQueries`; infra impl returns zeros until `module:student-form` builds submission tables. No polling. |
| 10 | **`ShareableLinkToken`** generated at `Publication.Create` from `RandomNumberGenerator` (32-byte base64url); `GetByLinkTokenAsync` for the future student route. |
| 11 | **Open/Close have no HTTP endpoints** — Quartz + reconciliation call the interactors directly. |
| 12 | **Cross-feature rule** — `publications` gets its own `TeacherOptionsApiService`; `SlotState` and `PublicationState` live in `shared/models`. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior slices)

- Repositories have **no `UnitOfWork` property** — interactors inject `IUnitOfWork` separately.
- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; `[FromServices]` per-action injection, no constructors.
- DI registrations go in each project's `DependencyInjection.cs`.
- No comments anywhere except `//given //when //then` test markers.
- `FirstOrDefaultAsync` for predicate lookups, `FindAsync` for PK; never `SingleOrDefaultAsync`.
- Transloco lowercase namespaces, keys always mirrored; RTL-first.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-domain-values.md](task-01-domain-values.md) | Domain values — IDs, `PublicationState`, `ShareableLinkToken`, `SubmissionWindow` (TDD) | ✅ own commit |
| 2 | [task-02-publication-aggregate.md](task-02-publication-aggregate.md) | `Publication` aggregate + `TeacherExcelVersion` + events + exceptions + repo iface (TDD) | ✅ own commit |
| 3 | [task-03-domain-event-dispatch.md](task-03-domain-event-dispatch.md) | Domain-event dispatch + `WeekScheduleCreated`→Draft handler (TDD) | ✅ own commit |
| 4 | [task-04-application-commands.md](task-04-application-commands.md) | Application commands + seams (`IExcelGenerator`/`IEmailSender`/`ISubmissionQueries`) + DI | ✅ own commit |
| 5 | [task-05-application-queries-handlers.md](task-05-application-queries-handlers.md) | Application queries + DTOs + `PublicationClosed` handler | ✅ own commit |
| 6 | [task-06-infrastructure-persistence.md](task-06-infrastructure-persistence.md) | Infrastructure — persistence, queries, `ISubmissionQueries` stub, migration | ✅ own commit |
| 7 | [task-07-infrastructure-services.md](task-07-infrastructure-services.md) | Infrastructure — token generator, ClosedXML placeholder Excel, logging email | ✅ own commit |
| 8 | [task-08-scheduling.md](task-08-scheduling.md) | Quartz jobs, scheduler, reconciliation hosted service | ✅ own commit |
| 9 | [task-09-controllers.md](task-09-controllers.md) | Controllers + Scalar smoke test | ✅ own commit |
| 10 | [task-10-client-domain-data.md](task-10-client-domain-data.md) | Client — core services, shared enums, feature domain + data | ✅ own commit |
| 11 | [task-11-client-store-routing.md](task-11-client-store-routing.md) | Client — signal store, routes, nav, i18n | ⏳ with 12 |
| 12 | [task-12-client-dashboard.md](task-12-client-dashboard.md) | Client — dashboard page, dumb components, dialogs | ✅ 11–12 |
| 13 | [task-13-client-history-prep.md](task-13-client-history-prep.md) | Client — history page + weekly-prep integration | ✅ own commit |
| 14 | [task-14-verification.md](task-14-verification.md) | End-to-end verification (both languages) + PR | — |

## How to Run a Task

1. Confirm you are on branch `9-us-08-21-publications-module` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\
├── Values\        PublicationId, TeacherExcelVersionId, PublicationState, ShareableLinkToken, SubmissionWindow
├── Entities\      Publication, TeacherExcelVersion
├── Events\        PublicationCreated, PublicationPublished, PublicationOpened, PublicationClosed, PublicationWindowExtended, PublicationReopened
├── Exceptions\    PublicationMustBeDraftException, PublicationMustBePublishedException, PublicationMustBeOpenException, PublicationMustBeClosedException, SubmissionWindowEndMustBeAfterStartException, WindowExtensionMustBeLaterException
└── Repositories\  IPublicationRepository
src\DrivingLessons.Application\
├── Common\             IDomainEventHandler, IDomainEventDispatcher
├── Common\Exceptions\  PublicationNotFoundException, PublicationAlreadyExistsException
├── Abstractions\       IExcelGenerator, IEmailSender, ExcelFile, EmailMessage
├── Commands\           PublishPublication, ExtendPublicationWindow, ReopenPublication, OpenPublication, ClosePublication
├── EventHandlers\      WeekScheduleCreatedHandler, PublicationClosedHandler
└── Queries\            IPublicationQueries, ISubmissionQueries, GetPublication, GetPublicationDashboard, FindPublicationHistory
src\DrivingLessons.Infrastructure\
├── EntityFramework\EntityConfigurations\  PublicationConfiguration, Converters\ (PublicationIdConverter, ShareableLinkTokenConverter)
├── EntityFramework\Migrations\            new AddPublications
├── EntityFramework\Queries\               PublicationQueries, SubmissionQueries (stub)
├── EntityFramework\Repositories\          PublicationRepository
├── DomainEvents\                          DomainEventDispatcher
├── Excel\                                 PlaceholderExcelGenerator (ClosedXML)
├── Email\                                 LoggingEmailSender, EmailOptions
└── Scheduling\                            OpenPublicationJob, ClosePublicationJob, PublicationScheduler, PublicationReconciliationHostedService
src\DrivingLessons.Presentation.Web\Controllers\Publication\   PublicationCommandController, PublicationQueryController
tests\DrivingLessons.Domain.Test\                              Values\, Entities\ + Fake\ (PublicationFakeBuilder + StateBuilders)
client\src\app\
├── core\services\      file-download.service.ts, clipboard.service.ts
├── shared\models\      publication-state.enum.ts, slot-state.enum.ts (relocated)
├── shared\components\  publication-state-tag\
└── features\publications\{domain, data, state, ui}\
```
