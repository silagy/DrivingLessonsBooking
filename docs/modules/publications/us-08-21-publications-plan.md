# US-08–21: Publications Module — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Per-task files with full code live in [us-08-21-publications-plan/](us-08-21-publications-plan/README.md).

**Goal:** Implement the Publications module — the school-wide weekly Publication lifecycle (`Draft → Published → Open → Closed`, reopenable), the unguessable shareable link, automatic window open/close, the live admin dashboard of per-slot request counts, per-teacher versioned Excel emailed at every close, on-demand Excel download, and the publications history view — covering GitHub issues [#9](https://github.com/silagy/DrivingLessonsBooking/issues/9) (US-08) through [#22](https://github.com/silagy/DrivingLessonsBooking/issues/22) (US-21).

**Architecture:** New `Publication` `AggregateRoot` (one per calendar week, school-wide, per [ADR 0003](../../decisions/0003-roster-csv-and-weekly-link-model.md)) through all four onion layers. This slice also builds the three cross-cutting mechanisms the teachers/week-schedules slices deliberately deferred: **domain-event dispatch**, **background scheduling** (Quartz), and **outbound email** — plus stub seams for the not-yet-built Excel content (`module:excel`) and Submission data (`module:student-form`). A new Angular `publications` feature adds the dashboard + history pages and a Publish entry-point on the existing weekly-prep page.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL + Quartz.NET + ClosedXML backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco 8 client.

**Branch:** `9-us-08-21-publications-module` (off `main`, after week-schedules merge PR #57).

---

## Context

The weekly demand-collection flow is: admin prepares each teacher's grid (week-schedules, done) → **publishes the week** → students submit via one link → window closes → Excel lands in each teacher's inbox. This module is the spine that connects preparation to delivery. It is the first slice that needs three things no prior slice built:

1. **Domain-event dispatch** — the teachers slice set `CommitAsync() = SaveChangesAsync()` only and explicitly deferred dispatch "to the Publication slice." This is that slice. Creating a teacher's WeekSchedule must now spawn a Draft Publication for the week, and closing a window must generate + email the Excel — both via handlers.
2. **Background scheduling** — US-16/17 require the window to open and close at its datetime with no user present. Quartz jobs are registered at publish time; a startup reconciliation service catches up after downtime.
3. **Outbound email** — US-17/18 email a versioned Excel to each teacher at every close.

Two dependencies are not yet built and are isolated behind seams so this module ships and is testable in full:
- **Excel content** (`module:excel`, US-44/45/46) — `IExcelGenerator` is defined here with a ClosedXML placeholder (real two-sheet structure, summary counts wired, detail sheet header-only until submissions exist). The excel module fills the detail rows later without touching the close pipeline.
- **Submission data** (`module:student-form`) — `ISubmissionQueries` is defined here; its infrastructure implementation returns **zero counts / empty stats** until the student-form module creates the submission tables. The dashboard UI is fully built and lights up automatically when real submissions land.

The committed mockups (`Driving Lesson Mockup\mock\admin.jsx` → `AdminPublish`, `AdminDashboard`, `AdminClosed`, `AdminHistory`, `DeskGrid mode="counts"`) are the UX source of truth.

### User Stories

| Issue | Story | Summary |
|---|---|---|
| [#9](https://github.com/silagy/DrivingLessonsBooking/issues/9) | US-08 | Admin publishes the prepared week by setting one window start + end; Publication → Published, window stored (UTC), covers all teachers |
| [#10](https://github.com/silagy/DrivingLessonsBooking/issues/10) | US-09 | Publish generates a single unguessable link scoped to the week |
| [#11](https://github.com/silagy/DrivingLessonsBooking/issues/11) | US-10 | Admin copies the link with one click |
| [#12](https://github.com/silagy/DrivingLessonsBooking/issues/12) | US-11 | Each Publication's lifecycle state is clearly displayed |
| [#13](https://github.com/silagy/DrivingLessonsBooking/issues/13) | US-12 | Dashboard shows the day×slot grid with request count per slot, per teacher; Unavailable marked blocked |
| [#14](https://github.com/silagy/DrivingLessonsBooking/issues/14) | US-13 | Dashboard reflects data as of page load; manual refresh only, no push |
| [#15](https://github.com/silagy/DrivingLessonsBooking/issues/15) | US-14 | Admin downloads the Excel on demand at any time |
| [#16](https://github.com/silagy/DrivingLessonsBooking/issues/16) | US-15 | Admin extends the window end before close |
| [#17](https://github.com/silagy/DrivingLessonsBooking/issues/17) | US-16 | Window closes automatically at end time; link becomes read-only |
| [#18](https://github.com/silagy/DrivingLessonsBooking/issues/18) | US-17 | Excel automatically emailed to each teacher at close |
| [#19](https://github.com/silagy/DrivingLessonsBooking/issues/19) | US-18 | Every close increments a per-teacher version carried in the subject |
| [#20](https://github.com/silagy/DrivingLessonsBooking/issues/20) | US-19 | Admin reopens a closed window with a new end datetime |
| [#21](https://github.com/silagy/DrivingLessonsBooking/issues/21) | US-20 | Admin sees history of past publications per teacher with state + latest version |
| [#22](https://github.com/silagy/DrivingLessonsBooking/issues/22) | US-21 | Admin re-downloads the Excel of any past publication |

## Locked Decisions (resolved via grilling)

| # | Decision | Choice |
|---|---|---|
| 1 | **Publication cardinality** | One `Publication` per calendar week, school-wide (ADR 0003 #16). Identified by `WeekStart` (Sunday `DateOnly`), unique index on `week_start`. |
| 2 | **Draft creation trigger** | A `WeekScheduleCreated` domain-event handler ensures a Draft Publication for that week exists (idempotent — first teacher's schedule creates it, later teachers find it). This module builds the domain-event dispatcher deferred by the teachers slice. |
| 3 | **State machine** | `Draft(10) → Published(20) → Open(30) → Closed(40)`; `Reopen` returns Closed → Open (US-19: "returns to Open … until the next close"). Every transition is non-idempotent → guard → `DomainException` → 409. |
| 4 | **Auto open/close** | **Quartz.NET**: per-publication `OpenPublicationJob` + `ClosePublicationJob` scheduled at publish; rescheduled on extend/reopen; a startup `IHostedService` reconciles missed boundaries after downtime. |
| 5 | **Versioning** | **Per-teacher** counters: `Publication` owns `TeacherExcelVersion { TeacherId, Version }` children. `Close(teacherIds)` increments each; the email subject carries that teacher's version. Teachers set = those with a WeekSchedule that week. |
| 6 | **Teacher scope of a Publication** | Exactly the teachers who prepared a `WeekSchedule` for the week (drives dashboard selector, Excel set, version set). Resolved in the Close/query interactors, never inside the aggregate. |
| 7 | **Excel** | `IExcelGenerator` seam + ClosedXML `PlaceholderExcelGenerator` (real two-sheet workbook: summary grid from the week's slots with counts via `ISubmissionQueries`; detail sheet headers only). `module:excel` (US-44/45/46) fills detail rows later. |
| 8 | **Email** | `IEmailSender` seam + `LoggingEmailSender` (logs recipient/subject/attachment size), gated by `Email:Enabled=false`. Real SMTP/SES drops in behind the interface later. |
| 9 | **Dashboard counts** | `ISubmissionQueries` seam; infrastructure impl returns zeros/empty until `module:student-form` builds submission tables. UI fully built now. No polling — one query per Refresh click (US-13). |
| 10 | **Link token** | `ShareableLinkToken` value object generated at `Publication.Create` from `RandomNumberGenerator` (32-byte base64url, no sequential ids); `GetByLinkTokenAsync` on the repo for the future student route. |
| 11 | **Open/Close are internal** | No HTTP endpoints for open/close — Quartz jobs + reconciliation call the interactors directly. Only `publish`, `extend-window`, `reopen`, and the read/download endpoints are exposed. |
| 12 | **Cross-feature rule** | `publications` may not import from `features/teachers` or `features/week-schedules`; it gets its own `TeacherOptionsApiService`. `SlotState` moves to `shared/models` (two features now need it); `PublicationState` lives in `shared/models` (prep page renders it too). |

## Conventions that OVERRIDE the rules docs (follow the code, per prior slices)

- Repositories expose **no `UnitOfWork` property** — interactors inject `IUnitOfWork` separately.
- Controllers use `[EndpointSummary]`/`[Tags]`/`[ProducesResponseType]` (native AddOpenApi), not `[SwaggerOperation]`; `[FromServices]` per-action interactor injection, no controller constructors.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- No comments anywhere except `//given //when //then` test markers.
- `FirstOrDefaultAsync` for predicate lookups, `FindAsync` for PK; never `SingleOrDefaultAsync`.
- Transloco lowercase namespaces in `client\public\i18n\{en,he}.json`, keys always mirrored; RTL-first; strings never hardcoded.

## Domain Design

```
Domain\Entities\Publication.cs        AggregateRoot<PublicationId>
    WeekStart WeekStart
    PublicationState State
    ShareableLinkToken LinkToken
    SubmissionWindow? Window                    (null in Draft; set on Publish)
    IReadOnlyCollection<TeacherExcelVersion> TeacherVersions   (private List, per-teacher counters)

    static Create(WeekStart)                     → Draft, generates LinkToken → PublicationCreated
    Publish(SubmissionWindow)                    → MustBeDraft → Published    → PublicationPublished
    Open()                                        → MustBePublished → Open     → PublicationOpened
    Close(IEnumerable<TeacherId>)                 → MustBeOpen → Closed, bump each teacher version → PublicationClosed
    ExtendWindow(DateTimeOffset newEndUtc)        → MustBeOpen/Published, newEnd>currentEnd → PublicationWindowExtended
    Reopen(DateTimeOffset newEndUtc)              → MustBeClosed → Open, window end = newEnd  → PublicationReopened

Domain\Entities\TeacherExcelVersion.cs   Entity<TeacherExcelVersionId> — no events; internal Create + internal Increment
    TeacherId TeacherId, int Version
```

The interactor/domain split for teacher versions: the **Close interactor** resolves which teachers have a WeekSchedule for the week (`IWeekScheduleRepository`/query) and passes their ids to `Close(teacherIds)`; the **domain** find-or-creates a `TeacherExcelVersion` per id and increments. The aggregate never queries other aggregates.

| Kind | Types |
|---|---|
| Typed IDs | `PublicationId`, `TeacherExcelVersionId` |
| Value objects | `ShareableLinkToken` (RNG base64url, non-empty), `SubmissionWindow` (`StartUtc`,`EndUtc` — end must be after start), enum `PublicationState { Draft=10, Published=20, Open=30, Closed=40 }` |
| Events | `PublicationCreated`, `PublicationPublished`, `PublicationOpened`, `PublicationClosed`, `PublicationWindowExtended`, `PublicationReopened` |
| Domain exceptions | `PublicationMustBeDraftException`, `PublicationMustBePublishedException`, `PublicationMustBeOpenException`, `PublicationMustBeClosedException`, `SubmissionWindowEndMustBeAfterStartException`, `WindowExtensionMustBeLaterException` |
| App exceptions | `PublicationNotFoundException`, `PublicationAlreadyExistsException` : `NotFoundException`/base |

### Domain-event dispatch (new infrastructure)

- `Application\Common\IDomainEventHandler<TEvent>` and `IDomainEventDispatcher`.
- `DrivingLessonsDbContext.CommitAsync()` becomes: gather `UncommittedEvents` from tracked aggregates → `SaveChangesAsync` → dispatch → `CommitEvents()`, looping until no new events are raised (bounded), saving between cycles so handler-created aggregates persist in the same unit of work.
- Handlers (Application):
  - `WeekScheduleCreatedHandler` → `IPublicationRepository.GetByWeekAsync`; if null, `Publication.Create(weekStart)` + `Add` (idempotent Draft creation, decision #2).
  - `PublicationClosedHandler` → for each teacher in the closed publication's version set: `IExcelGenerator.GenerateAsync` → `IEmailSender.SendAsync` with subject `Week {n} Requests - {teacherName} - v{version}` (US-17/18).

## API Surface

| Endpoint | Interactor | Returns |
|---|---|---|
| `POST api/publications/{id:guid}/publish` | `PublishPublicationInteractor` | 204 (schedules Quartz jobs) |
| `POST api/publications/{id:guid}/extend-window` | `ExtendPublicationWindowInteractor` | 204 (reschedules close) |
| `POST api/publications/{id:guid}/reopen` | `ReopenPublicationInteractor` | 204 (reschedules close) |
| `GET api/publications/by-week?week=YYYY-MM-DD` | `GetPublicationInteractor` | 200 `GetPublicationResponse` / 404 |
| `GET api/publications/{id:guid}/dashboard?teacherId=` | `GetPublicationDashboardInteractor` | 200 `GetPublicationDashboardResponse` / 404 |
| `GET api/publications/history` | `FindPublicationHistoryInteractor` | 200 `ItemForFindPublicationHistoryResponse[]` |
| `GET api/publications/{id:guid}/excel?teacherId=` | `DownloadPublicationExcelInteractor` | 200 `FileContentResult` (.xlsx) / 404 |

`Open`/`Close` interactors are **not** exposed (decision #11). `PublicationCommandController` + `PublicationQueryController` in `Presentation.Web\Controllers\Publication\`, route `api/publications`. Errors flow through the existing `ApiExceptionFilter` (domain guards → 409, not-found → 404) — no filter changes.

## Task Breakdown

See [us-08-21-publications-plan/README.md](us-08-21-publications-plan/README.md) for the execution index. Inside-out backend (domain → dispatch → application → infrastructure → scheduling → controllers), then client plumbing before the pages that consume it. One commit per task; client store+page tasks may commit together (a store with no consumer is dead code).

| # | Task | Layer | Commit |
|---|------|-------|--------|
| 01 | Domain values (IDs, state, token, window) — TDD | Domain | ✅ own |
| 02 | Publication aggregate + TeacherExcelVersion + events + repo iface — TDD | Domain | ✅ own |
| 03 | Domain-event dispatch + WeekScheduleCreated→Draft handler — TDD | Application/Infra | ✅ own |
| 04 | Application commands + seams (excel/email/submissions) + DI | Application | ✅ own |
| 05 | Application queries + DTOs + PublicationClosed handler | Application | ✅ own |
| 06 | Infrastructure persistence + migration | Infrastructure | ✅ own |
| 07 | Infrastructure services (token/excel/email) | Infrastructure | ✅ own |
| 08 | Quartz scheduling + reconciliation | Infrastructure | ✅ own |
| 09 | Controllers + Scalar smoke test | Presentation | ✅ own |
| 10 | Client core services + feature domain/data | Client | ✅ own |
| 11 | Client signal store + routing + i18n | Client | ⏳ with 12 |
| 12 | Client dashboard page + dialogs + components | Client | ✅ 11–12 |
| 13 | Client history page + weekly-prep integration | Client | ✅ own |
| 14 | End-to-end verification (both languages) + PR | — | — |

## Risks / Gotchas

1. **Domain-event dispatch is greenfield and load-bearing.** The `WeekScheduleCreated` handler adds a *new* aggregate mid-commit; `CommitAsync` must drain handler-raised events across save cycles (bounded loop) and `CommitEvents()` on every aggregate to avoid re-dispatch. Unit-test the dispatcher in isolation.
2. **Nullable owned `SubmissionWindow`.** An optional owned value object (null in Draft) is the one EF mapping shape this repo hasn't used — verify constructor binding on first migration run; fall back to a private parameterless ctor on the record if needed.
3. **Per-teacher versions as owned collection.** Mirror the `OwnsMany(Slots)` pattern (shadow FK `publication_id`, explicit `HasKey`, `UsePropertyAccessMode(Field)`); never add a `PublicationId` CLR property to `TeacherExcelVersion`.
4. **Quartz ↔ extend/reopen races.** Rescheduling the close job must be idempotent; the `Close` interactor re-checks state (`MustBeOpen`) so a stale job firing after an extend/reopen no-ops via 409 (swallowed by the job). Manually test restart + extend + reopen (task 14).
5. **History query translation.** The per-teacher history rows join Publications × WeekSchedules on value-converted `week_start`; if LINQ won't translate, join via `EF.Property`/raw week keys. Verify the generated SQL.
6. **ClosedXML placeholder scope.** Keep the placeholder to summary grid + detail headers only — do not implement US-45/46 detail content here; that is `module:excel`. Note the boundary in the file so the excel module knows where to plug in.
7. **`SlotState` relocation** to `shared/models` touches ~4 week-schedules imports — mechanical, keep it in its own commit within task 10 if it grows.
8. **Concurrent draft creation race** — two teachers' first schedules for the same week could both miss `GetByWeekAsync`; the unique index on `week_start` is the backstop, loser swallows the 409. Accepted for a single-admin system.

## Verification

- `dotnet test tests\DrivingLessons.Domain.Test` — all domain tests green (aggregate transitions, guards with `[DataRow]`, event assertions, dispatcher).
- `dotnet build` + `dotnet ef migrations has-pending-model-changes` after task 06 (reports none).
- **Scalar (task 09):** authorize via bearer; publish a Draft → 204 + link in `by-week`; extend → 204; reopen a Closed → 204; dashboard returns the week's slots with zero counts + blocked Unavailable; download excel → .xlsx bytes; unauthenticated → 401; wrong-state transition → 409.
- **E2E (task 14)** with `Email:Enabled=false` and short windows: prep two teachers for a week → prep page shows **Draft** → publish (start +2m, end +5m) → state **Published**, link copyable (US-08/09/10/11) → auto **Open** (US-16), dashboard chips + zero-count grid, refresh updates the stamp (US-12/13) → extend +2m (US-15), close fires at the new time only → auto **Close** → logs show one email per teacher, subject `… - v1` (US-16/17/18) → on-demand download mid-flow (US-14) → reopen +2m → close again → `v2` in logs + history (US-19/18) → history lists per-teacher rows with state + version, re-download returns the file (US-20/21) → restart the app with a window already past its end → reconciliation closes + emails on startup.
- **Hebrew/RTL audit (task 14):** every new screen (dashboard's 4 state variants, 3 dialogs, history, prep chip) toggled to Hebrew — day columns mirror (Sunday inline-start), numbers/dates/link isolated in `<bdi>`, no physical CSS properties in new SCSS.

## Follow-ups (not this slice)

- `module:excel` (US-44/45/46) fills the Excel detail sheet + summary formatting behind the existing `IExcelGenerator`.
- `module:student-form` implements `ISubmissionQueries` against real submission tables (dashboard counts light up with no publications-module change) and consumes `GetByLinkTokenAsync` for the `/s/{token}` route.
- Real SMTP/SES `IEmailSender` implementation + verified identities for production.
- Record any rules-doc deviations (event dispatch now implemented; Quartz added) in `us-08-21-publications-changes.md` when the slice lands.
