# US-33 / 34 / 35 / 36 / 37 / 39 / 40 / 41: First Submission — Task Index (student-form slice 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 3 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** After confirming their roster details, a student declares how many lessons they want this week, picks slots from their teacher's grid day by day — each pick Single or Double with an optional constraint, ranked in the order picked, Unavailable slots untouchable — reviews the ranked list, is stopped with a clear message if it is shorter than their target, submits, and sees a confirmation that the submission can be edited through the same link and national ID until the window closes. The admin dashboard counts the real requests. Covers GitHub issues [#33](https://github.com/silagy/DrivingLessonsBooking/issues/33) (US-33), [#34](https://github.com/silagy/DrivingLessonsBooking/issues/34) (US-34), [#35](https://github.com/silagy/DrivingLessonsBooking/issues/35) (US-35), [#36](https://github.com/silagy/DrivingLessonsBooking/issues/36) (US-36), [#37](https://github.com/silagy/DrivingLessonsBooking/issues/37) (US-37), [#39](https://github.com/silagy/DrivingLessonsBooking/issues/39) (US-39), [#40](https://github.com/silagy/DrivingLessonsBooking/issues/40) (US-40) and [#41](https://github.com/silagy/DrivingLessonsBooking/issues/41) (US-41).

**Architecture:** A new `Submission` aggregate (per student per publication) owns an ordered list of `SlotRequest` children; `Submission.Create(publication, student, weekSchedule, target, picks, now)` and the full-replace `Revise(…)` enforce every rule — window open, active student, the student's own teacher's grid for the publication's week, open slots, one request per slot, picks ≥ target — and raise `SubmissionCreated` / `SubmissionRevised`. Two anonymous commands on the existing `SubmissionCommandController` (`POST` create, `PUT` revise, `api/submissions/by-link/{token}`, national ID in the body) resolve link → active roster student → their teacher's week schedule → slots **inside that grid** through one shared `SubmissionContextResolver`, then call the domain. The read side gains `hasSubmission` on identify (so the client knows POST vs PUT) and a real `SubmissionQueries` behind the admin dashboard. The client's store-held wizard grows from 3 to 5 steps (target stepper, pickable chips + a bottom sheet per pick, ranked review) plus a confirmation.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco client; MSTest + Shouldly + FakeItEasy; Vitest (via `ng test`). No new packages. **One EF migration** (`AddSubmissions`).

**Spec:** issues [#33](https://github.com/silagy/DrivingLessonsBooking/issues/33), [#34](https://github.com/silagy/DrivingLessonsBooking/issues/34), [#35](https://github.com/silagy/DrivingLessonsBooking/issues/35), [#36](https://github.com/silagy/DrivingLessonsBooking/issues/36), [#37](https://github.com/silagy/DrivingLessonsBooking/issues/37), [#39](https://github.com/silagy/DrivingLessonsBooking/issues/39), [#40](https://github.com/silagy/DrivingLessonsBooking/issues/40), [#41](https://github.com/silagy/DrivingLessonsBooking/issues/41) · [student-form roadmap](../README.md) (slice 3 + locked decisions 1–8) · [slice 2 plan](../us-50-51-27-identity-routing-plan/README.md) (open item 1, Review Focus, conventions) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) · [requirements §5.6, §5.7, §7 steps 5–9 + validation rules, §8.1, §8.3, §10, decisions 4, 5, 11](../../../requirements.md) · mockup `Driving Lesson Mockup\mock\student.jsx` → `STarget`, `SlotDayList` / `SlotChip`, `SSlots`, `SSlotSheet`, `SReview` / `PickRow`, `SReviewErr`, `SDone`; `mock\shared.jsx` → `PhoneShell`, `MStep`, `MFoot`, `MkField`, `MkBtn`

**Branch:** `33-us-33-34-35-36-37-39-40-41-first-submission` (created from `origin/main` at planning time)

## User Stories

**US-33** ([#33](https://github.com/silagy/DrivingLessonsBooking/issues/33)) — *As a student, I want to declare my target session count for the week, so that the teacher knows how many lessons I actually want, independent of how many slots I mark.*
- **Given** I passed the confirmation step **When** I enter a target session count of 1 or more (no upper limit) **Then** the target is accepted and carried on my Submission for the teacher to interpret against my ranked picks.

**US-34** ([#34](https://github.com/silagy/DrivingLessonsBooking/issues/34)) — *As a student, I want the week grid to show Unavailable slots as visibly blocked and unselectable, so that I only pick times that can actually happen.*
- **Given** the admin marked some Slots Unavailable before publishing **When** I view the week grid **Then** those Slots are visibly blocked and cannot be picked, while Open slots remain selectable.

**US-35** ([#35](https://github.com/silagy/DrivingLessonsBooking/issues/35)) — *As a student, I want to specify Single or Double for each Slot I pick, so that the teacher knows the session length I want at that time.*
- **Given** I am picking slots in the week grid **When** I pick a Slot and choose Double **Then** that Slot Request carries the Double session type, which will appear in the Excel detail sheet. *(A Double still counts as one request in summary counts — decision 5, §8.1.)*

**US-36** ([#36](https://github.com/silagy/DrivingLessonsBooking/issues/36)) — *As a student, I want an optional free-text constraint on each Slot Request, so that I can communicate nuance (e.g., "only after 16:00") without a phone call.*
- **Given** I picked a Slot **When** I type a free-text constraint on that pick **Then** the constraint is saved on that Slot Request only — not on the whole Submission — and reaches the Excel detail sheet. *(Decision 11.)*

**US-37** ([#37](https://github.com/silagy/DrivingLessonsBooking/issues/37)) — *As a student, I want my picks to accumulate as a ranked list in selection order, so that my preference order is captured without extra effort.*
- **Given** I am picking slots in the week grid **When** I pick several Slots one after another **Then** each pick is appended to a single ranked list in selection order (rank 1, 2, 3, …). *(No primary/alternative types — decision 4.)*

**US-39** ([#39](https://github.com/silagy/DrivingLessonsBooking/issues/39)) — *As a student, I want to pick more slots than my target count, so that lower-ranked picks serve as backups when my top choices are taken.*
- **Given** I declared a target of 2 sessions **When** I pick 5 Slots and submit **Then** the Submission is accepted with ranks 1–5 against target 2, and the teacher interprets rank against target during booking.

**US-40** ([#40](https://github.com/silagy/DrivingLessonsBooking/issues/40)) — *As a student, I want validation to block my submission when it cannot be honored, so that the teacher never receives an unusable submission.*
- **Given** I declared a target of 3 sessions **and** I picked only 2 Slots **When** I attempt to submit **Then** the submission is blocked with a clear message explaining I must pick at least as many slots as my target. *(Sibling rules enforced the same way: target ≥ 1; a Slot at most once per Submission; the national ID must be on the roster — ADR 0003.)*

**US-41** ([#41](https://github.com/silagy/DrivingLessonsBooking/issues/41)) — *As a student, I want a confirmation screen telling me I can edit via the same link and national ID until the window closes, so that I know corrections are self-service and never require messaging the teacher.*
- **Given** my Submission passed validation **When** I submit **Then** I see a confirmation stating the submission was received and can be edited via the same link and national ID until the window closes.

## Context

Slice 2 shipped the anonymous `SubmissionCommandController` with `POST …/identify` (national ID in the body → the active roster student's teacher, car, transmission and that teacher's week-grid slots, **with slot ids** for exactly this slice) and a three-step wizard (`identify → details → slots`) held in the per-visit `StudentFormStore`, whose slots step renders the teacher's grid read-only as a day-by-day chip list. Identify is deliberately not gated by window state (slice 2 open item 1): this slice enforces "window must be open" in the Submission commands. The publications module left `ISubmissionQueries` as a stub returning zeros; both the admin dashboard (`GetPublicationDashboardInteractor`) and the placeholder Excel generator already call it.

No `Submission` code exists yet. Slice 4 adds drag-reorder of the ranked list; slice 5 loads a returning student's submission into the form (welcome-back screen), handles editing any number of times and the dedicated window-closed-mid-submit screen (#42). This slice's picks are append, change in place, and remove.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Endpoints:** `POST api/submissions/by-link/{token}` (create — 409 if one exists) and `PUT api/submissions/by-link/{token}` (revise — 404 if none; full replace), body `{ nationalId, targetCount, slotRequests: [{ slotId, sessionType, constraint }] }` with list order = rank. Both answer **204 No Content** — a deliberate deviation from "Create → 201 + body": the only useful body would be an id, and no id is handed to an anonymous caller (slice-2 decision 2); the client already holds what it sent. |
| 2 | **Identify gains `hasSubmission: bool`** (roadmap decision 2: "the client knows which to call from the identify response"). A returning student in this slice starts from an **empty** list and the review warns that submitting replaces the earlier one; slice 5 grows the flag into the loaded submission. Without it, the confirmation's promise ("edit via the same link and ID") would 409. |
| 3 | **"Window must be open" is state-based** (`Publication.IsOpen`), not a clock check: the Quartz close job is the authority for "the window ended", exactly as the rest of the publication lifecycle. The seconds between `EndUtc` and the job running are accepted (open item 2). |
| 4 | **Domain shape** (roadmap decision 3): `Submission.Create(publication, student, weekSchedule, targetCount, picks, submittedAtUtc)` and `Revise(…same…, revisedAtUtc)`, one event each. A pick is a `SlotPick` value object (resolved `Slot` + `SessionType` + `SlotConstraint?`); rank is the pick's position (`Rank` value object, 1…n) — never sent by the client. `SlotRequest` references its slot by id and has no mutators (reorder is slice 4). |
| 5 | **A submission records the `WeekScheduleId` it was made against.** Per-teacher counts (dashboard, Excel) join through it, so a student rebound to another teacher by a later roster upload never moves their already-submitted slots onto a grid they do not belong to; `Revise` re-points it to the student's current teacher's grid. |
| 6 | **Anonymous error messages are payload-free:** eight new `Submission…` domain exceptions with fixed text; `SlotNotFoundException()` and `WeekScheduleNotFoundException()` gain parameterless overloads for these paths (their existing overloads carry internal ids). |
| 7 | **Status map:** 404 = something the student's request points at does not exist for them (unknown/draft link, not — or no longer — an **active** roster student, their teacher has no grid, a slot id outside their grid, PUT with no submission); 409 = a rule (malformed ID, target < 1, constraint too long/blank, undefined session type, window not open, unavailable slot, duplicate slot, picks < target, POST when one exists). The commands load **active** students only (`IStudentRepository.GetActiveByNationalIdAsync`), mirroring identify (slice-2 decision 4), so a student removed between identify and submit gets the client's "contact your school" message; the aggregate still guards `IsActive` itself. |
| 8 | **Shared `SubmissionContextResolver`** (`Application\Commands\Common\`) does the loading both commands share — no rule lives in it. Slot ids are resolved **inside the student's own week schedule**, which is what makes another teacher's slot a 404 before the domain runs. A draft link is "not found", as in slice 1. |
| 9 | **Client outcomes by status only:** 204 → confirmation; 409 → "couldn't save — the window may have closed, or this week's slots changed" + **Check the form again** (reloads the link; a closed window then shows slice 1's closed screen); 404 → "couldn't find your roster details or this week's slots — contact your school"; anything else → retry. 409/404 lock Submit (a retry cannot change the answer). The dedicated window-closed screen (`SDoneErr`) is slice 5. |
| 10 | **Client-side feedback duplicates exactly four backend rules** and decides none: `−` disabled at target 1; picks ≥ target on the review (the mockup's "Not enough picks" + disabled Submit); `maxlength="200"` on the constraint; Unavailable chips `disabled`. |
| 11 | **Five numbered steps** `identify → details → target → slots → review` (mockup "Step n of 5"), confirmation `done` unnumbered and caption-free. **Back** on target, slots and review — not in the mockup, but with one route the browser Back leaves the form, and it is the only way to change the target after picking. None on details (the ID field is not re-bound). |
| 12 | **The bottom sheet is a custom dumb component rendered with `@if`**, not a PrimeNG `Drawer` or a `DialogService` dialog: its open slot is wizard state in the store (roadmap decision 4), its choices are `linkedSignal`s of its input, and it is destroyed on close, so no leave animation can show one slot's content on the next (slice-2 `p-message` lesson). PrimeNG provides `pFocusTrap`, `pTextarea`, `p-button`; Escape and the backdrop cancel; focus lands on the first option. |
| 13 | **Picking:** tap an open chip → sheet for a new pick (next rank, Single, no constraint) → "Add as pick #n"; tap a picked chip → the same sheet with its values → Save (rank kept) or Remove (later picks move up). A slot is never two picks. |
| 14 | **Constraint:** the client trims and sends `null` for blank text; `SlotConstraint` trims, refuses blank and > 200 characters; stored as `constraint_text varchar(200)` (`constraint` is reserved in PostgreSQL). |
| 15 | **`hasAvailability` = at least one Open slot** (was: any slot). A grid that is entirely Unavailable now gets slice 2's "no lesson slots — contact your school" notice instead of a grid with nothing to pick. |
| 16 | **`SubmissionQueries` becomes real:** per slot, one count per Slot Request — **a Double counts one** (§8.1); students submitted, total picks, latest submitted/revised instant. Both existing consumers (dashboard, placeholder Excel summary) pick it up unchanged; the Excel detail sheet stays the Excel module's. |
| 17 | **In-visit edit:** the confirmation's "Edit my submission" returns to the review with the list intact; the next submit is a `PUT`. |
| 18 | **Timestamps:** `SubmittedAtUtc` + nullable `RevisedAtUtc` (`DateTimeOffset`, `timestamptz`), passed into the domain like `Publication.ExtendWindow`'s instant; read from the injected `TimeProvider`. |
| 19 | **`(publication_id, student_id)` is unique** — one submission per student per week, and the database's last word if two POSTs race. |
| 20 | **The API smoke tests run against a throwaway database** (`drivinglessons_us33_smoke`) — roster uploads deactivate students, so never against the dev database. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; routes are prefixed `api/`; action methods end with `Async`.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- Repositories expose no `UnitOfWork`; command interactors inject `IUnitOfWork` directly (`CreateWeekScheduleInteractor`, `ImportRosterInteractor`).
- Domain methods take instants as `DateTimeOffset` (`Publication.ExtendWindow/Reopen` precedent) rather than an instant value object.
- "Already exists" is checked by the create interactor and thrown as a domain exception (`WeekScheduleAlreadyExistsException` precedent); the aggregate has no knowledge of other submissions.
- Application tests build real domain objects (`Teacher.Create`, `Publication.Create` → `Publish` → `Open`, …) — `DrivingLessons.Application.Test` does not reference the domain fake builders.
- The pick sheet is not opened through `DialogService` (client-primeng "all modals open through DialogService") — see decision 12. The target stepper uses native `<button>`s, not a PrimeNG input.
- The client uses **Transloco** (`TranslocoPipe`, `| transloco`), not ngx-translate; translation files live in `client\public\i18n\`; top-level keys are camelCase (`studentForm`).
- PrimeNG is imported as modules (`ButtonModule`, `MessageModule`, `TextareaModule`, `FocusTrapModule`); full-width buttons use the `fluid` attribute.
- Component SCSS is not wrapped in `@layer app` (only the global styles are) — match existing components; new color tokens go in `client\src\styles\_tokens.scss`.
- New TypeScript files use 4-space indentation (student-form precedent).
- No comments anywhere except `//given //when //then` test markers.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null** — configure `null` explicitly in not-found tests.

## Global Constraints

- The national ID travels **only in request bodies**: never in a URL, query string, route, `localStorage`, log line, exception message, or response body.
- Student URLs carry only the link token — never an id; no id (submission, slot request, student, teacher, week schedule) is returned to the anonymous endpoints beyond the slot ids slice 2 already returns.
- All API calls use relative `api/...` URLs; no absolute domains.
- Every user-visible string is a translation key present in **both** `en.json` and `he.json` in the same commit.
- Logical CSS properties only; tokens only, no hex colors in components.
- Student surface is mobile-first: designed at ~375px, 40px minimum touch targets, single column (requirements §10).
- Hebrew/RTL verified before the slice is done (client-i18n rule 2).
- No business rules in the client: it renders the backend's outcome and mirrors only the four rules in decision 10 for immediate feedback.
- No new NuGet/npm packages; the only schema change is the `AddSubmissions` migration.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise — each is pinned by a step in the owning task:

1. **The same slot twice in one submission** (tampered body, or a double-tap race) → 409, never two requests for one slot → task 2 `Create__/Revise__Slot_Must_Be_Requested_Once`, task 5 check 8; client: a picked chip re-opens for editing — task 9 "changes a pick in place…".
2. **An Unavailable slot id sent by a tampered client** → 409 even though the UI never offers it → task 2 `Create__Slot_Must_Be_Open`, task 4 `Unavailable_Slot_Is_Rejected`, task 5 check 6; UI side task 9 "never opens an unavailable slot".
3. **A slot id from another teacher's grid** → 404 (resolved only inside the student's own grid), and the aggregate refuses a foreign slot or grid itself → task 4 `Slot_Must_Be_In_The_Students_Week_Schedule`, task 2 `Create__Slot_Must_Be_In_The_Week_Schedule` / `…Week_Schedule_Must_Be_The_Students_Teachers`, task 5 check 7.
4. **The window closes between page load and submit** → 409, the list stays on screen, "check the form again" shows the closed screen → task 2 `Create__Window_Must_Be_Open` / `Revise__Window_Must_Be_Open`, task 4 `Closed_Window_Is_Rejected`, task 5 Step 6, task 10 "checks the form again…", task 11 Step 3.6.
5. **POST when a submission already exists** (returning student, second tab, confirmation → edit) → 409 from the API; the client sends `PUT` whenever identify says one exists or this visit already submitted → task 4 `Submission_Must_Not_Already_Exist`, task 5 check 2, task 10 "replaces the earlier submission…" / "edits from the confirmation…", task 11 Steps 3.1, 3.4.
6. **A student removed from the roster between identify and submit** → 404 and "contact your school", never a submission → task 3 `GetActiveByNationalIdAsync`, task 4 `Student_Must_Be_Active_On_The_Roster`, task 2 `…Student_Must_Be_Active` (aggregate guard), task 5 checks 20–21, task 10 error `it.each` (404), task 11 Step 3.5.
7. **A very long constraint** (pasted essay, no spaces) → capped at 200 by the textarea, refused above 200 by the API, wrapped inside the review card → task 1 `SlotConstraintTest`, task 4 `Too_Long_Constraint_Is_Rejected`, task 5 checks 12 and 18, task 9 "caps the constraint at 200 characters", task 11 Step 3.8.
8. **A target far above what was (or can be) picked** (e.g. 30 against a 22-slot week) → accepted as a target, blocked at review with the exact shortfall and a one-tap "Change my target" → task 1 `Target_Is_Not_Covered_By_Fewer_Picks (30, 22)`, task 2 `Create__Picks_Must_Cover_Target (10, 3)`, task 8 "raises the target without an upper limit…", task 10 "blocks…" / "lets the student lower the target…", task 11 Step 3.3.
9. **An undefined session type** (`"sessionType": 99` binds through the integer-tolerant enum converter; `"triple"` does not) → 409 / 400, never stored → task 1 `SlotPickTest.Session_Type_Must_Be_Single_Or_Double`, task 5 checks 10–11.
10. **Re-opening the pick sheet on another slot** (after Escape, backdrop or Cancel) → the new sheet shows only that slot's day, time and choices — never the previous slot's typed constraint → task 9 "closes on Escape and never shows the previous slot…", task 11 Step 3.7.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-submission-values.md](task-01-submission-values.md) | Domain — typed IDs, `TargetSessionCount`, `SessionType`, `SlotConstraint`, `Rank`, `SlotPick` (TDD) | ✅ own commit |
| 2 | [task-02-submission-aggregate.md](task-02-submission-aggregate.md) | Domain — `Submission` + `SlotRequest`, events, eight rule exceptions (TDD) | ✅ own commit |
| 3 | [task-03-submission-persistence.md](task-03-submission-persistence.md) | Infrastructure — converters, `SubmissionConfiguration`, repositories, `AddSubmissions` migration | ✅ own commit |
| 4 | [task-04-submission-commands.md](task-04-submission-commands.md) | Application — `SubmissionContextResolver`, create + revise interactors (TDD) | ✅ own commit |
| 5 | [task-05-submission-endpoints.md](task-05-submission-endpoints.md) | API — anonymous `POST`/`PUT` + smoke test on a throwaway DB (every outcome, window closing mid-submit) | ✅ own commit |
| 6 | [task-06-submission-read-side.md](task-06-submission-read-side.md) | API — identify `hasSubmission`; real `SubmissionQueries` (dashboard + Excel counts), verified over HTTP | ✅ own commit |
| 7 | [task-07-client-domain-data.md](task-07-client-domain-data.md) | Client — pick / constraint / target / sheet / review models (TDD, Vitest), DTOs, API methods | ✅ own commit |
| 8 | [task-08-client-target-step.md](task-08-client-target-step.md) | Client — five-step wizard, target stepper, Back, all-unavailable notice (US-33) | ✅ own commit |
| 9 | [task-09-client-slot-picking.md](task-09-client-slot-picking.md) | Client — pickable chips, rank badges, bottom sheet, Target/Picked footer (US-34/35/36/37/39) | ✅ own commit |
| 10 | [task-10-client-review-and-confirmation.md](task-10-client-review-and-confirmation.md) | Client — ranked review, not-enough message, submit POST/PUT, errors, confirmation (US-39/40/41) | ✅ own commit |
| 11 | [task-11-verification-and-pr.md](task-11-verification-and-pr.md) | End-to-end browser verification (HE + EN, 375px, every edge path) + roadmap link + PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `33-us-33-34-35-36-37-39-40-41-first-submission` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification (and task 5 adds a local-only `api-smoke` entry) — never stage it (`git add` only the paths each task lists).

Backend commands run from the repo root. Client commands run from `client\`: `npm test -- --watch=false` and `npm run build`. Environment notes (project memory):
- The default npm is broken for installs: if `npm ci` is ever needed, use nvm **v22.6.0** for the install only; build and test with node **v26.4.0** and the local `ng`.
- Browser-pane screenshots are flaky on this PrimeNG app: `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence.
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`) — `dl-postgres` has a stale migration history.

## Open Items (non-blocking)

1. **Two POSTs racing for the same student** (two tabs submitting at the same instant): the unique `(publication_id, student_id)` index keeps the data right, but the loser gets a 500 (`DbUpdateException`) rather than a 409. Map unique-violation to `SubmissionAlreadyExistsException` if it is ever seen. Two concurrent `PUT`s can likewise surface a concurrency 500.
2. **Close-job lag:** between `EndUtc` and the Quartz close job firing (seconds; the reconciliation service covers restarts) a submission is still accepted — the publication is authoritatively open until it is closed.
3. **409 reasons are indistinguishable to the client** (by design — it maps statuses). Slice 5's dedicated window-closed screen (#42) needs a machine-readable reason, e.g. a ProblemDetails `type` per exception family.
4. **A returning student starts from an empty list** in this slice (the review says it replaces the earlier one); slice 5 loads the existing picks and the welcome-back copy (`SIdEditing`).
5. **Browser/Android Back leaves the single-route wizard and loses the picks** (roadmap decision 4 accepts refresh-restarts). A `beforeunload` guard or history-state steps could follow if students complain.
6. **The Excel detail sheet stays the placeholder** (Excel module); the summary sheet now shows real counts.
7. **No rate limiting on the anonymous commands** (carried from slice 2 open item 2) — the accepted-risk posture of requirements §8.2 / ADR 0003.
8. **A student rebound to another teacher mid-week** keeps their submission on the old grid until they revise; revising moves it to the new teacher's grid (decision 5), so the old teacher's counts drop at that moment.
9. **An all-zero slot id (`00000000-…`) answers 500**: `EntityId` rejects `Guid.Empty` with an `ArgumentException`, the same as every other id route in the API. Tampering only.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\
├── Entities\Submission.cs, SlotRequest.cs                                   new
├── Values\SubmissionId.cs, SlotRequestId.cs, TargetSessionCount.cs, SessionType.cs,
│          SlotConstraint.cs, Rank.cs, SlotPick.cs                            new
├── Events\SubmissionCreated.cs, SubmissionRevised.cs                        new
├── Exceptions\  TargetSessionCountMustBePositive, SlotConstraintMustNotBeEmpty,
│                SlotConstraintMustNotExceedMaxLength, RankMustBePositive, SessionTypeMustBeSingleOrDouble,
│                SubmissionWindowMustBeOpen, SubmissionStudentMustBeActive, SubmissionWeekScheduleMustMatchStudentWeek,
│                SubmissionSlotMustBeInWeekSchedule, SubmissionSlotMustBeOpen, SubmissionSlotMustBeRequestedOnce,
│                SubmissionPicksMustCoverTarget, SubmissionMustBeForPublicationAndStudent,
│                SubmissionAlreadyExists  (…Exception.cs)                     new
└── Repositories\ISubmissionRepository.cs                                    new;  IStudentRepository + GetActiveByNationalIdAsync
src\DrivingLessons.Application\
├── Commands\Common\SlotRequestForSubmissionRequest.cs, SubmissionContext.cs, SubmissionContextResolver.cs   new
├── Commands\CreateSubmission\CreateSubmissionRequest.cs, CreateSubmissionInteractor.cs                    new
├── Commands\ReviseSubmission\ReviseSubmissionRequest.cs, ReviseSubmissionInteractor.cs                    new
├── Common\Exceptions\SubmissionNotFoundException.cs                         new;  SlotNotFound / WeekScheduleNotFound + () overloads
├── Queries\IdentifyStudent\IdentifyStudentResponse.cs                       + HasSubmission
└── DependencyInjection.cs                                                   + resolver, 2 interactors
src\DrivingLessons.Infrastructure\
├── EntityFramework\EntityConfigurations\SubmissionConfiguration.cs          new
├── EntityFramework\EntityConfigurations\Converters\ SubmissionId, SlotRequestId, TargetSessionCount,
│                                                    SlotConstraint, Rank (…Converter.cs)   new
├── EntityFramework\Repositories\SubmissionRepository.cs                     new;  StudentRepository + GetActiveByNationalIdAsync
├── EntityFramework\Queries\SubmissionQueries.cs                             stub → real;  StudentQueries + hasSubmission
├── EntityFramework\Migrations\<timestamp>_AddSubmissions.cs (+ Designer, snapshot)   new
├── EntityFramework\DrivingLessonsDbContext.cs                               + Submissions
└── DependencyInjection.cs                                                   + ISubmissionRepository
src\DrivingLessons.Presentation.Web\Controllers\Submission\SubmissionCommandController.cs   + CreateAsync (POST), ReviseAsync (PUT)
tests\DrivingLessons.Domain.Test\
├── Values\TargetSessionCountTest.cs, SlotConstraintTest.cs, RankTest.cs, SlotPickTest.cs   new
├── Entities\SubmissionTest.cs                                               new
├── Entities\Fake\SubmissionScenario.cs, SubmissionFakeBuilder.cs            new
└── Common\Faker.cs                                                          + FakeUtcInstant
tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs, ReviseSubmissionInteractorTest.cs   new
client\src\app\features\student-form\
├── domain\   session-type.enum.ts, submit-status.enum.ts, slot-pick.ts, slot-constraint.ts, target-count.ts,
│             pick-sheet.ts, review-item.ts (+ .spec.ts each)                new
│             slot-day.ts (+ rank, picks, slotTimeLabel, hasOpenSlot), student-form-step(.enum).ts (5 steps + done, previousStepOf)
├── data\     slot-request-for-submission.request.ts, create-submission.request.ts, revise-submission.request.ts   new
│             identify-student.response.ts (+ hasSubmission), submissions-api.service.ts (+ create/revise, + spec)
├── state\    student-form.store.ts                                          + target, picks, sheet, review, submit, recheck
└── ui\
    ├── pages\student-form\          student-form.page.ts/.html/.scss/.spec.ts   target, review, done
    └── components\  target-step\, pick-sheet\, review-step\                 new
                     wizard-step\ (+ Back), slot-day-list\ (buttons + badges), slots-step\ (+ footer, sheet)
client\src\styles\_tokens.scss                                               + --app-scrim
client\public\i18n\en.json + he.json                                         studentForm: + back, slotLabel, target.*, pick.*,
                                                                             review.*, submitted.*; slots.* reworded + extended
docs\modules\student-form\README.md                                          slice 3 row links this plan
```
