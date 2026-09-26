# US-50 / US-51 / US-27: Identity & Routing — Task Index (student-form slice 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 2 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** On the single school-wide weekly link, a student types their national ID; a roster member is admitted with no password and shown — read-only — the teacher, car and transmission from their roster record, then that teacher's own week grid; an ID that is not (or no longer) on the roster gets a clear "contact your school" message and no form. Covers GitHub issues [#52](https://github.com/silagy/DrivingLessonsBooking/issues/52) (US-50), [#53](https://github.com/silagy/DrivingLessonsBooking/issues/53) (US-51) and [#28](https://github.com/silagy/DrivingLessonsBooking/issues/28) (US-27).

**Architecture:** One anonymous endpoint (`POST api/submissions/by-link/{token}/identify`, national ID in the body) runs an Application **query** interactor: resolve the non-draft publication by link token (slice 1's query) → normalize the ID through `NationalId.Of` → project the active roster student joined to their teacher and car, plus that teacher's week-schedule slots for the publication's week, into one student-safe DTO. The client's `open` view becomes a three-step wizard held in the existing per-visit `StudentFormStore`: an ID step (lookup is a `resource()` keyed by token + ID), a read-only details step, and a read-only day-by-day slot list of the teacher's grid.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco client; MSTest + Shouldly + FakeItEasy; Vitest (via `ng test`). No new packages, no migration.

**Spec:** issues [#52](https://github.com/silagy/DrivingLessonsBooking/issues/52), [#53](https://github.com/silagy/DrivingLessonsBooking/issues/53), [#28](https://github.com/silagy/DrivingLessonsBooking/issues/28) · [student-form roadmap](../README.md) (slice 2 + locked decisions 1, 2, 4, 5, 6) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) · [requirements §5.4, §5.5, §7 steps 3–4 & 6, §8.2, §8.4, §10, decisions #16–#19](../../../requirements.md) · mockup `Driving Lesson Mockup\mock\student.jsx` → `SIdKnown`, `SIdUnknown`, `SDetails`, `SlotDayList` / `SlotChip`; `mock\shared.jsx` → `PhoneShell`, `MStep`, `MFoot`, `MkField`

**Branch:** `52-us-50-51-27-identity-and-routing`

## User Stories

**US-50** ([#52](https://github.com/silagy/DrivingLessonsBooking/issues/52)) — *As a student, I want to identify by my national ID and be admitted only if I am in the roster, so that I can submit quickly while non-members cannot submit at all.*
- **Given** a published, open weekly link **When** I enter my national ID **Then** if my ID is in the roster I proceed into the flow with no password or verification; if it is not, I see a clear "please contact your school" message and no form is shown.

**US-51** ([#53](https://github.com/silagy/DrivingLessonsBooking/issues/53)) — *As a student, I want the single weekly link to send me to my own teacher's availability grid based on my roster record, so that I never pick the wrong teacher and am never asked who my teacher is.*
- **Given** a single weekly Publication and a roster binding student A to Teacher Cohen and student B to Teacher Levi **When** each enters their national ID on the same link **Then** A sees Cohen's grid and B sees Levi's grid, each with their teacher and transmission shown read-only.

**US-27** ([#28](https://github.com/silagy/DrivingLessonsBooking/issues/28)) — *As a student, I want the form to display which teacher I am submitting for, so that I see my teacher confirmed before submitting.*
- **Given** I passed the national ID step **When** I reach the confirmation step **Then** the form shows, read-only, the teacher resolved from my roster record ("You are submitting availability for Teacher Cohen") along with my transmission. No teacher selection, no mismatch warning, no "not my teacher" abort, no transmission question.

## Context

Slice 1 shipped the public `/s/:token` route, the anonymous `SubmissionQueryController`, and a store that maps the publication state to a view; its `open` view was a placeholder-free "Submissions are open" panel and the documented insertion point for this slice. The roster module (US-49) already persists `Student` (national ID unique index, `IsActive`, `TeacherId`, `CarId`); transmission lives on the `Car`, never on the student (decision #18). A `WeekSchedule` exists per teacher per week — the admin may have prepared only some teachers, so a roster student's teacher can lack a grid for the published week. `Publication` is school-wide (one per week, decision #16).

The Submission aggregate does not exist yet (slice 3). This slice therefore identifies and routes only; the "welcome back — your submission is loaded" screen (`SIdEditing`) belongs to slice 5, when the identify response grows an existing-submission field (roadmap decision 2).

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Endpoint:** `POST api/submissions/by-link/{token}/identify` with body `{ "nationalId": "..." }` on a new anonymous `SubmissionCommandController`. POST because the national ID is PII and must never ride in a URL (access logs, proxies, browser history). It sits in the *Command* controller because api-guidelines split controllers by verb (GET = query controller) — slice 1 pre-announced this sibling — while the interactor itself is an Application **query** (`Queries\IdentifyStudent\`): it changes nothing. |
| 2 | **One response** — `IdentifyStudentResponse { studentName, teacherName, carName, transmission, slots[] }`, slots = the teacher's week-schedule slots for the publication's week (`id, day, window, state, startLocal, endLocal`, Sunday→Friday, morning→evening). Slot ids are included because slice 3 references them in picks. **Never echoed:** national ID, phone, address, student/teacher/car/week-schedule ids. |
| 3 | **Outcomes by status only:** 200 found · **404** `StudentNotFoundException` (payload-free) when no *active* roster student matches · **409** the existing `NationalId` domain exceptions (non-digits, >9 digits, bad check digit) · 404 `PublicationLinkNotFoundException` for unknown/draft links (slice 1's query, reused). The client maps 404 → "not on file", 409 → "invalid ID", anything else → "couldn't check, retry"; it never renders backend text. A link cannot become unknown after the page loaded it (publications are never deleted or un-published), so 404 on identify means "not on roster" in practice. |
| 4 | **Deactivated students are "not on file."** Deactivation means "absent from the latest roster upload" (decision #19); the roster is the single source of truth, so the query filters `IsActive`. |
| 5 | **Teacher without a week schedule → 200 with `slots: []`.** Identification succeeded; the details step shows the teacher and a "no availability yet — contact your school" notice, and offers no Continue. Not an error status. |
| 6 | **ID normalization is split by responsibility:** the client strips spaces, dashes and bidi marks as an input mask (people type `312 456 789`; WhatsApp pastes carry U+200E); the backend `NationalId.Of` stays the only validator (NBSP/bidi strip, zero-pad to 9, check digit). The client has **no check-digit logic** — no domain rule is duplicated. |
| 7 | **Lookup trigger (mockup `SIdKnown`/`SIdUnknown`):** a lookup fires automatically once 9 digits are typed; Continue/Enter looks up a shorter ID (leading zero omitted). Continue advances only after "Found you". The lookup is a `resource()` keyed by `{ token, nationalId }`, so a stale response can never render the previous ID's name, and any edit drops the previous result. |
| 8 | **Wizard state in the store** (roadmap decision 4): `StudentFormStep` + `WIZARD_STEPS = [identify, details, slots]` drive the "Step n of N" counter; slice 3 inserts `target` and `review` by editing that array. Refresh restarts at the ID step. Caption bar per mockup: none on the ID step, week on details, **week · teacher** on the slots step (US-27 "prominently"). |
| 9 | **Slots step is read-only in this slice:** the teacher's grid as the mockup's day-by-day chip list (open vs striped/unavailable), with a truthful intro ("These are {teacher}'s slots for this week. Striped slots are unavailable."). It proves US-51 on screen and is the exact component slice 3 makes pickable (roadmap decision 5). No footer action yet. |
| 10 | **Components (all dumb, page composes):** `WizardStepComponent` (counter, heading, intro, sticky footer slot), `IdentifyStepComponent`, `DetailsStepComponent`, `SlotsStepComponent`, `SlotDayListComponent`. |
| 11 | **The ID field is one signal-backed input, not a reactive `FormGroup`** — a single field whose error state comes from the backend outcome (`pInputText [invalid]`). Deliberate deviation from client-primeng "typed reactive forms". |
| 12 | **Transmission labels live under `studentForm.details.transmissions.*`** instead of reusing `teachers.transmissions.*` — features never depend on each other (same precedent as slice 1's date helpers). Day/window labels reuse the shared-component keys `weekGrid.days.*` / `weekGrid.windows.*`. |
| 13 | **Slice 1's "Submissions are open" panel is replaced** by the ID step; its `studentForm.open.*` keys are deleted. |
| 14 | **The API smoke test runs against a throwaway database** (`drivinglessons_us50_smoke`) — a roster upload deactivates every student missing from the file, so it must never run against the dev database. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; routes are prefixed `api/`; action methods end with `Async`.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- Query interfaces normally take primitives; `IStudentQueries.GetActiveByNationalIdAsync` takes the `NationalId` value object so the interactor validates once at the boundary and nothing re-validates downstream (ddd-architecture "the type is the proof").
- The client uses **Transloco** (`TranslocoPipe`, `| transloco`), not ngx-translate; translation files live in `client\public\i18n\`; top-level keys are camelCase (`studentForm`).
- PrimeNG is imported as modules (`ButtonModule`, `InputTextModule`, `MessageModule`) like the existing pages; full-width buttons use the `fluid` attribute (login precedent).
- Component SCSS is not wrapped in `@layer app` (only the global styles are) — match existing components.
- New TypeScript files use 4-space indentation (student-form/roster precedent).
- No comments anywhere except `//given //when //then` test markers.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null** — configure `null` explicitly in not-found tests.

## Global Constraints

- The national ID travels **only in the POST body**: never in a URL, query string, route, `localStorage`, log line, exception message, or response body.
- Student URLs carry only the link token — never a sequential id (api-guidelines, client-architecture anti-patterns).
- All API calls use relative `api/...` URLs; no absolute domains.
- Every user-visible string is a translation key present in **both** `en.json` and `he.json` in the same commit.
- Logical CSS properties only (`margin-inline-*`, `padding-block-*`, `inset-block-*`, `text-align: start/end`); tokens only, no hex colors.
- Student surface is mobile-first: designed at ~375px, 40px minimum touch targets, single column (requirements §10).
- Hebrew/RTL verified before the slice is done (client-i18n rule 2).
- No business rules in the client: it renders the backend's outcome; it never decides roster membership, ID validity, or which teacher a student belongs to.
- No new NuGet/npm packages; no EF migration (the `national_id` unique index already exists).

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **A student removed by a later roster upload (deactivated)** must get the "not on file" message — never their old teacher's grid → Task 1 query filters `IsActive` + Task 2 Step 5 smoke (student D) + Task 6 Step 3.6.
2. **The student's teacher has no week schedule for the published week** (admin prepared only some teachers) → details step shows the teacher plus "contact your school", no Continue, no crash → Task 1 (`slots: []`) + Task 2 Step 5 (student C) + Task 4 page spec + Task 5 page spec + Task 6 Step 3.4.
3. **IDs typed the way people type them** — with spaces or dashes, pasted with bidi marks, or 8 digits with the leading zero dropped — must still find the student → Task 3 `national-id-input.spec.ts` + Task 2 Step 5 (`18` → student A) + Task 4 page spec + Task 6 Steps 2.2, 3.2–3.3.
4. **The national ID never leaves the POST body** (URL, 404 detail, localStorage) → Task 1 message test + Task 3 API-service spec + Task 2 Step 5 (404 detail) + Task 6 Step 3.10.
5. **Editing the ID after a result, or while a lookup is in flight,** must drop the previous result — the screen must never show one student's name next to another student's ID → Task 4 page spec ("drops the previous result") + Task 6 Step 3.9.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-identify-query.md](task-01-identify-query.md) | Identify query — request/response DTOs, not-found exception, interactor (TDD) + EF implementation | ✅ own commit |
| 2 | [task-02-identify-endpoint.md](task-02-identify-endpoint.md) | Anonymous `SubmissionCommandController` + API smoke test on a throwaway DB (routing per teacher proven over HTTP) | ✅ own commit |
| 3 | [task-03-client-domain-data.md](task-03-client-domain-data.md) | Client — ID input mask, initials, wizard steps, slot-day grouping (TDD, Vitest), DTOs, API method | ✅ own commit |
| 4 | [task-04-client-identify-and-details.md](task-04-client-identify-and-details.md) | Client — store identify flow, wizard/ID/details step components, page wiring, i18n (US-50 + US-27) | ✅ own commit |
| 5 | [task-05-client-teacher-grid.md](task-05-client-teacher-grid.md) | Client — slot day list + slots step, details Continue, i18n (US-51) | ✅ own commit |
| 6 | [task-06-verification-and-pr.md](task-06-verification-and-pr.md) | End-to-end browser verification (two students, one link, HE + EN, 375px) + roadmap link + PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `52-us-50-51-27-identity-and-routing` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification (and task 2 adds a local-only `api-smoke` entry) — never stage it (`git add` only the paths each task lists).

Backend commands run from the repo root. Client commands run from `client\`: `npm test -- --watch=false` and `npm run build`. If `npm ci` is ever needed, see the project memory note on the broken default npm (use nvm `v22.6.0` for installs only). For from-source API runs use the compose Postgres container (`docker stop dl-postgres; docker compose up -d postgres`), per the project memory note.

## Open Items (non-blocking)

1. **Identify is not gated by window state** — a Published or Closed link still answers identify. The client only shows the ID step while Open; slice 3's `Submission` commands are where "window must be open" is enforced (domain rule, requirements §7 validation).
2. **No rate limiting on identify** — with the link, the endpoint is a roster-membership oracle for semi-guessable IDs. This is the accepted risk of requirements §8.2 / ADR 0003; an ASP.NET Core rate limiter on the anonymous endpoints is the natural mitigation if abuse appears.
3. **A student bound to a soft-deleted teacher** still identifies and sees that teacher's name (and, having no grid, the "contact your school" notice). A roster re-upload rebinds them. Revisit if the admin workflow makes this common.
4. **Welcome-back screen (`SIdEditing`)** — slice 5 adds the existing submission to the identify response and the "edit my submission" copy.
5. **School display name** — the mockup says "Cohen Driving School's roster"; v1 has no school entity, so the copy says "the school's roster" (same call as slice 1).
6. **Continue arrow glyph** from the mockup is omitted — a directional icon needs RTL flipping; cosmetic, revisit with the design pass.
7. **Step counter reads "of 3"** in this slice; slice 3 makes it "of 5" by extending `WIZARD_STEPS`.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Application\
├── Common\Exceptions\StudentNotFoundException.cs                         new
├── Queries\IStudentQueries.cs                                            + GetActiveByNationalIdAsync
├── Queries\IdentifyStudent\IdentifyStudentRequest.cs                     new
├── Queries\IdentifyStudent\IdentifyStudentResponse.cs                    new (+ SlotForIdentifyStudentResponse)
├── Queries\IdentifyStudent\IdentifyStudentInteractor.cs                  new
└── DependencyInjection.cs                                                + interactor
src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs      + implementation
src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs   new, [AllowAnonymous]
tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs  new
client\src\app\features\student-form\
├── domain\   transmission.enum.ts, identify-status.enum.ts, student-form-step.enum.ts, student-form-step.ts,
│             national-id-input.ts, name-initials.ts, slot-day.ts (+ .spec.ts each), week-label.ts (+ dateInWeek)
├── data\     identify-student.request.ts, identify-student.response.ts,
│             submissions-api.service.ts (+ identifyStudent, + .spec.ts)
├── state\    student-form.store.ts                                       + identify resource, steps, captions
└── ui\
    ├── pages\student-form\          student-form.page.ts/.html/.spec.ts  open view = wizard
    └── components\  wizard-step\, identify-step\, details-step\, slots-step\, slot-day-list\
client\public\i18n\en.json + he.json                                      studentForm: − open, + step, continue,
                                                                          weekTeacherCaption, identify.*, details.*, slots.*
docs\modules\student-form\README.md                                       slice 2 row links this plan
```
