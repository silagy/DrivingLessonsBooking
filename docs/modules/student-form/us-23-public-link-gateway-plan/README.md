# US-23: Public Link Gateway — Task Index (student-form slice 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 1 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** A student who opens the school-wide weekly link (`/s/{token}`) on a phone lands on a public, mobile-first page that shows the week and — when the window is not yet open or already closed — a clear read-only message with the Asia/Jerusalem window dates and no form, covering GitHub issue [#24](https://github.com/silagy/DrivingLessonsBooking/issues/24) (US-23).

**Architecture:** One anonymous query endpoint (`GET api/submissions/by-link/{token}`) projects the non-draft Publication matching the link token to a student-safe DTO (week, week number, state, window instants — no ids). A new lazy `student-form` Angular feature mounted on the public route `s/:token` (outside `authGuard`) holds a per-visit signals store that maps the publication state to a view (`notYetOpen` / `open` / `closed` / `invalidLink` / `loadFailed`) and renders it inside a mobile student shell per the committed mockup (`SWinClosed`).

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco client; MSTest + Shouldly + FakeItEasy; Vitest (via `ng test`) for client pure functions. No new packages.

**Spec:** [issue #24](https://github.com/silagy/DrivingLessonsBooking/issues/24) · [student-form roadmap](../README.md) (slice 1 + locked decisions) · [requirements §7 step 2, §8.3, §8.4, §10](../../../requirements.md) · mockup `Driving Lesson Mockup\mock\student.jsx` → `SWinClosed`, `mock\shared.jsx` → `PhoneShell`

**Branch:** `24-us-23-public-link-gateway`

## User Story

**US-23** ([#24](https://github.com/silagy/DrivingLessonsBooking/issues/24)) — *As a student, I want a clear read-only message with the relevant dates when the window is not yet open or already closed, so that I know exactly when to come back instead of messaging the teacher.*

- **Given** a Publication is Published but its window has not yet opened (or it is Closed)
- **When** I open the link
- **Then** I see a read-only message stating the relevant window dates, and no form is rendered

## Context

This is the roadmap's **tracer bullet**: it proves the two scariest unknowns — an anonymous API surface against the deny-by-default fallback authorization policy (`Program.cs`), and a public mobile route in an admin-only client (`app.routes.ts` guards `''` and redirects `**` to it) — with the smallest story attached. Slice 2 (identity & routing, #52/#53/#28) plugs the national-ID step into the `open` view this slice creates.

Publications are created as **Draft** (with a link token) the moment the first week schedule for a week exists (`WeekScheduleCreatedHandler`), so a token can exist before the admin publishes. State transitions `Published → Open → Closed` are driven by Quartz jobs at the window instants; the client renders `state` exactly as the backend reports it and never compares clocks itself.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Endpoint:** `GET api/submissions/by-link/{token}` on a new `SubmissionQueryController` (`[AllowAnonymous]` at class level) — the exact shape `api-guidelines.md` prescribes for anonymous student endpoints. Slices 2/3/5 add `identify`, `POST`, `PUT` under the same `api/submissions/by-link/{token}` base (roadmap decision 2). |
| 2 | **Draft links are invisible to students:** the query excludes `Draft`, so a draft token returns **404** exactly like an unknown token. The filter lives in the query (`GetByLinkTokenExcludingDraftsAsync`), mirroring how `FindActiveAsync` filters soft-deleted rows. |
| 3 | **Student-safe DTO:** `GetPublicationByLinkResponse` carries `weekStart`, `weekNumber`, `state`, `windowStartUtc`, `windowEndUtc` — no publication id, no token echo. `PublicationLinkNotFoundException` has no payload so the capability token never appears in a response or log. |
| 4 | **Week number is domain behavior:** `WeekStart.WeekNumber` = ISO week of the week's **Monday** (Sun–Fri grid ⇒ Mon–Fri decide). Matches the mockup ("Week 25 (Jun 14–19)") and the requirements' email example. `PublicationClosedHandler` switches to it — today it takes the ISO week of the **Sunday**, which labels that week 24 in the teacher's email. One source for both surfaces. |
| 5 | **Client views:** one route `s/:token`, one page, a `StudentFormView` enum computed in the store from the resource state + `viewForPublicationState(...)`. Unknown/draft token → `invalidLink` screen (404 swallowed, no toast); network/500 → `loadFailed` with a Try-again button. |
| 6 | **Open view in slice 1** renders a minimal "Submissions are open" panel with the closing time — a true, useful screen and the insertion point where slice 2 puts the national-ID step. No placeholder copy. |
| 7 | **Store is page-provided** (`providers: [StudentFormStore]` on `StudentFormPage`) so every visit starts clean. Route-level `providers` would not do this: the router keeps a route's injector for the app's lifetime unless `withExperimentalAutoCleanupInjectors()` is enabled, so a second visit would get the first visit's store. Pinned by `student-form.routes.spec.ts`. |
| 8 | **Mockup mapping (roadmap decision 6):** `PhoneShell` → `StudentShellComponent` (brand mark + name, EN/עב toggle, optional week caption); `SWinClosed` → `StatusMessageComponent` + a window-dates card. Existing `--app-*`/`--p-*` tokens, existing `BrandLogoComponent` ("WeekDrive") instead of the mockup's "Cohen Driving School" — there is no school entity. |
| 9 | **Date helpers are duplicated into `features\student-form\domain\`** (`jerusalem-time.ts`, `week-label.ts`) because features must not import each other — the same precedent `roster` set. Consolidating into a `core` date service is logged as an open item, not done here. |
| 10 | **Share link and route stay in lockstep:** `AppRoutes.studentForm = 's'` and `PublicationsStore` builds the admin share link from it instead of its private `'/s/'` literal. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; routes are prefixed `api/`; action methods end with `Async`.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- The client uses **Transloco** (`TranslocoPipe`, `| transloco`), not ngx-translate; translation files live in `client\public\i18n\`; top-level keys are camelCase (`studentForm`), not PascalCase.
- PrimeNG is imported as modules (`ButtonModule`, `ProgressSpinnerModule`) like the existing pages.
- Component SCSS is not wrapped in `@layer app` (only the global styles are) — match existing components.
- New TypeScript files use 4-space indentation (roster/publications precedent).
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- Student URLs carry only the link token — never a sequential id (api-guidelines, client-architecture anti-patterns).
- All API calls use relative `api/...` URLs; no absolute domains.
- Every user-visible string is a translation key present in **both** `en.json` and `he.json` in the same commit.
- Instants are UTC on the wire and displayed in `Asia/Jerusalem` only (requirements §8.3); no `toLocaleString()` on instants.
- Logical CSS properties only (`margin-inline-*`, `padding-block-*`, `inset-inline-*`, `text-align: start/end`).
- Student surface is mobile-first: designed at ~375px, 40px minimum touch targets, single column (requirements §10).
- Hebrew/RTL verified before the slice is done (client-i18n rule 2).
- No business rules in the client: it renders `state`; it never decides open/closed from clocks.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **A Draft publication's token** (it exists as soon as a week schedule is created) must show the invalid-link screen, never a form or dates → Task 3 Step 4 smoke check (404) + Task 6 Step 7 browser check.
2. **An unknown, truncated, or garbage token** must show a friendly invalid-link screen — no error toast, no redirect to admin login → Task 5 Step 7 + Task 6 Step 7.
3. **Window instants across DST / on a device in another timezone** must render the Israeli wall-clock time (a 14:00 close shows 14:00 in summer and winter) → Task 4 Step 1 (`jerusalem-time.spec.ts`).
4. **Route ordering and stale admin sessions:** `/s/{token}` must never be swallowed by the guarded `''` route or the `**` redirect, and a browser holding an expired admin token in localStorage must still get the student page (the anonymous endpoint never 401s) → Task 3 Step 4 (expired bearer) + Task 5 Step 7 + Task 6 Step 7.
5. **Hebrew at 375px:** the toggle on the public page flips `dir="rtl"`, dates re-render in the Hebrew locale, and the dates card mirrors correctly without horizontal scroll → Task 6 Step 7.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-week-number.md](task-01-week-number.md) | `WeekStart.WeekNumber` (TDD) + teacher email uses it | ✅ own commit |
| 2 | [task-02-link-query.md](task-02-link-query.md) | Link query — interface, response DTO, interactor, not-found exception (TDD) + EF implementation | ✅ own commit |
| 3 | [task-03-anonymous-endpoint.md](task-03-anonymous-endpoint.md) | Anonymous `SubmissionQueryController` + API smoke test | ✅ own commit |
| 4 | [task-04-client-domain-data.md](task-04-client-domain-data.md) | Client — view mapping, week/instant helpers (TDD, Vitest), DTO, API service | ✅ own commit |
| 5 | [task-05-client-route-store.md](task-05-client-route-store.md) | Client — public route, per-visit store, page skeleton, i18n (en + he) | ✅ own commit |
| 6 | [task-06-client-ui-and-verification.md](task-06-client-ui-and-verification.md) | Client — mobile shell + status screens per mockup, end-to-end verification (both languages, 375px) + PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `24-us-23-public-link-gateway` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification — never stage it (`git add` only the paths each task lists).

Client commands (from `client\`): `npm test -- --watch=false` and `npm run build`. If `npm ci` is ever needed, see the project memory note on the broken default npm (use nvm `v22.6.0` for installs only).

## Open Items (non-blocking)

1. **Three copies of Jerusalem-time helpers** (`publications`, `roster`, `student-form` domains) — consolidate into one `core` service/pipe per `client-i18n.md` in a separate cleanup.
2. **Whitespace-only token** (`/s/%20`) hits `ShareableLinkToken.Of` → 409 instead of 404; the client shows `loadFailed`. Unreachable from a real link; accept.
3. **School display name** — the mockup shows "Cohen Driving School"; v1 has no school entity, so the shell shows the product brand. Revisit if the customer asks.
4. **Email subject week number changes** for every week (Sunday-ISO → Monday-ISO, e.g. 24 → 25). Mention in the PR so the teacher isn't surprised.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\Values\WeekStart.cs                               + WeekNumber
src\DrivingLessons.Application\
├── EventHandlers\PublicationClosedHandler.cs                               uses WeekStart.WeekNumber
├── Common\Exceptions\PublicationLinkNotFoundException.cs                   new
├── Queries\IPublicationQueries.cs                                          + GetByLinkTokenExcludingDraftsAsync
├── Queries\GetPublicationByLink\GetPublicationByLinkInteractor.cs          new
├── Queries\GetPublicationByLink\GetPublicationByLinkResponse.cs            new
└── DependencyInjection.cs                                                  + interactor
src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs   + implementation
src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionQueryController.cs   new, [AllowAnonymous]
tests\DrivingLessons.Domain.Test\Values\WeekStartTest.cs                    + week number cases
tests\DrivingLessons.Application.Test\Queries\GetPublicationByLinkInteractorTest.cs       new
client\src\app\
├── app.routes.ts                                                           + public s/:token before guarded ''
├── shared\config\app-routes.ts                                             + studentForm
├── shared\language-toggle\language-toggle.component.ts/.scss              + touch input (40px targets)
├── features\publications\state\publications.store.ts                      share link uses AppRoutes.studentForm
└── features\student-form\
    ├── student-form.routes.ts
    ├── domain\   student-form-view.enum.ts, student-form-view.ts, week-label.ts, jerusalem-time.ts (+ .spec.ts)
    ├── data\     get-publication-by-link.response.ts, submissions-api.service.ts
    ├── state\    student-form.store.ts
    └── ui\
        ├── pages\student-form\          student-form.page.ts/.html/.scss
        └── components\  student-shell\, status-message\
client\public\i18n\en.json + he.json                                        + studentForm.*
```
