# US-25 / 43 / 42: Edit & Close Race — Task Index (student-form slice 5)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 5 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session. Each file is self-contained.

**Goal:** A returning student who enters their national ID on the weekly link sees a welcome-back message and gets their saved Submission loaded for editing: target, ranked picks, session types and constraints, with the roster profile still read-only. They can change it and resubmit any number of times until the window closes, and every resubmit replaces the previous version. If the window's end time passes while the form is open, the submit is refused, nothing is saved, and a dedicated "the window just closed" screen says so. Covers GitHub issues [#26](https://github.com/silagy/DrivingLessonsBooking/issues/26) (US-25), [#43](https://github.com/silagy/DrivingLessonsBooking/issues/43) (US-43) and [#42](https://github.com/silagy/DrivingLessonsBooking/issues/42) (US-42).

**Architecture:** No new endpoint, aggregate or migration. On the backend, (1) `Submission.Create/Revise` also refuse once the submission instant reaches the window's `EndUtc`, through a new `Publication.IsOpenAt(instant)` built on `SubmissionWindow.HasEndedBy(instant)`. The close job no longer decides the exact cut-off. (2) The anonymous identify response replaces `hasSubmission: bool` with the saved `submission` (target, last-saved instant, slot requests in rank order). (3) `ApiExceptionFilter` stamps a ProblemDetails `type` of `problems/submission-window-closed` on `SubmissionWindowMustBeOpenException`, the machine-readable reason slice 3 left open. On the client, the store seeds the wizard from the saved submission when the student leaves the ID step, dropping picks whose slot is no longer open and saying how many. The ID step shows the mockup's welcome-back banner with "Edit my submission". The confirmation after a replace says "Changes saved". A window-closed problem routes to a new unnumbered `windowClosed` step (mockup `SDoneErr`).

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco client; MSTest + Shouldly + FakeItEasy; Vitest (via `ng test`). No new packages, no migration.

**Spec:** issues [#26](https://github.com/silagy/DrivingLessonsBooking/issues/26), [#43](https://github.com/silagy/DrivingLessonsBooking/issues/43), [#42](https://github.com/silagy/DrivingLessonsBooking/issues/42) · [student-form roadmap](../README.md) (slice 5 + locked decisions 1–4) · [slice 3 plan](../us-33-first-submission-plan/README.md) (decisions 2, 3, 9, 17; open items 2, 3, 4) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) · [requirements §5.6, §7 validation rules, §8.2, §8.3, §10](../../../requirements.md) · mockup `Driving Lesson Mockup\mock\student.jsx` → `SIdEditing` (welcome back), `SDone` (confirmation), `SDoneErr` (window closed mid-submit)

**Branch:** `26-us-25-43-42-edit-and-close-race` (created from `origin/main` at planning time, after slice 3 / PR #64 merged)

## User Stories

**US-25** ([#26](https://github.com/silagy/DrivingLessonsBooking/issues/26)): *As a returning student, I want my profile (from the roster) and my existing submission for this publication loaded for editing, so that I never repeat myself and corrections stay self-service.*
- **Given** I have an existing Submission for this Publication **When** I enter my national ID on the same link **Then** my roster profile (full name, teacher, transmission) is shown read-only and my existing Submission (target count, ranked picks, constraints) is loaded for editing.

**US-43** ([#43](https://github.com/silagy/DrivingLessonsBooking/issues/43)): *As a returning student, I want to edit my Submission any number of times until the window closes, so that corrections never go back through WhatsApp.*
- **Given** I have an existing Submission for an Open Publication **When** I re-enter my national ID on the link, change my target, picks, ranks, or constraints, and resubmit **Then** my Submission is updated to the new content, replacing the previous version as the single submission for this Publication.

**US-42** ([#42](https://github.com/silagy/DrivingLessonsBooking/issues/42)): *As a student, I want a clear rejection message if the window closed between my page load and my submit, so that I am never silently dropped.*
- **Given** I loaded the form while the window was open **and Given** the window end time passed before I finished **When** I submit **Then** the submission is rejected with a clear message that the window has closed, and no partial data is saved.

## Context

Slices 1–3 are on `main`. The anonymous API has `GET api/submissions/by-link/{token}` (link context: state, week, window), `POST …/identify` (national ID in the body → roster student, teacher's grid with slot ids, `hasSubmission`), `POST …/by-link/{token}` (create) and `PUT …/by-link/{token}` (revise, a full replace raising `SubmissionRevised`). `Submission.Revise` already runs every guard before it mutates anything, inside one unit of work. So "edit any number of times" and "no partial data" already hold on the server. This slice pins both with tests and exposes them through the UI.

What is missing: identify only says *whether* a submission exists, so a returning student starts from an empty list (slice 3 open item 4). The window rule is state-only (`Publication.IsOpen`), which accepts submissions in the seconds between `EndUtc` and the close job (slice 3 open item 2). The client maps every 409 to one generic message because the reasons are indistinguishable (slice 3 open item 3).

Slice 4 (reorder, [#38](https://github.com/silagy/DrivingLessonsBooking/issues/38)) is not shipped. In this slice a rank changes only by removing a pick and adding it again. Reorder lands independently and needs nothing from this slice.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Identify returns the saved submission** in place of `hasSubmission`: `submission: { targetCount, lastSavedAtUtc, slotRequests: [{ slotId, sessionType, constraint }] } \| null`, slot requests in rank order, `lastSavedAtUtc = revisedAtUtc ?? submittedAtUtc`. No submission or slot-request id leaves the endpoint. Anyone holding the link and the ID can see the list, which is the accepted posture of requirements §8.2 and the mockup's `SIdEditing` note 5. The client derives "replaces an earlier submission" from `submission !== null` (roadmap decision 2). |
| 2 | **The server returns the submission as stored. The client drops picks that no longer fit** the student's current grid: a slot marked Unavailable after the student submitted, or a grid that changed because a roster upload rebound them to another teacher. The review step says how many were dropped. Sending them back would 409 or 404, so the client never does. The loaded target is kept even if fewer picks survive, so the existing "Not enough picks" message takes over. |
| 3 | **The wizard is seeded when the student leaves the ID step** (`continueToDetails`), from the identify response that is current at that moment: target, picks and dropped count. It is never re-seeded by "Check the form again", because that reload must not overwrite edits in progress. A student without a submission is always reset to target 1 and no picks, so a replaced ID never carries the previous student's list. |
| 4 | **Same five steps for a returning student.** The ID step shows the welcome-back banner ("Welcome back, {name}. You already sent {n} picks on {savedAt}, so we've loaded them. You can edit your submission until the window closes ({closesAt}).") and labels the button **Edit my submission** (mockup `SIdEditing`). The details step stays read-only exactly as slice 2 built it, which is US-25's "profile shown read-only". |
| 5 | **After a `PUT`, the confirmation heading reads "Changes saved"**, not "Submitted". The body (count, week, teacher, edit-until) is unchanged. This is the US-43 signal that the new content replaced the old. |
| 6 | **The window closes at `EndUtc`, not when the job runs.** `Submission.Create/Revise` refuse when the publication is not Open **or** the submission instant is at or after `Window.EndUtc`, through `Publication.IsOpenAt(instant)` → `SubmissionWindow.HasEndedBy(instant)`. This supersedes slice 3 decision 3 / open item 2: US-42's AC is phrased by end time, the instant is already passed into both methods, and the rule no longer races the Quartz close job. The same exception type is thrown. `StartUtc` is not checked: `Open` is only reached at the start (Quartz open job), and every existing test instant lies before the fake windows' end. |
| 7 | **Machine-readable reason = ProblemDetails `type`.** `ApiExceptionFilter` sets `type: "problems/submission-window-closed"` for `SubmissionWindowMustBeOpenException` only; the status stays 409 (api-guidelines: `DomainException` → 409), `title`/`detail` unchanged, every other problem keeps `type` null. The constant lives in `Presentation.Web\Filters\ProblemTypes.cs`. This resolves slice 3 open item 3 without a per-exception catalogue. |
| 8 | **Client: that `type` → the `windowClosed` step** (mockup `SDoneErr`). The screen is unnumbered, has no caption and no action. Its body depends on the command that was refused: a `POST` gives "your list was not saved", a `PUT` gives "your changes were not saved — the list you sent earlier stays as it was". It shows **no time**, because the `EndUtc` loaded with the page may be stale after an admin extension. Any other 409 keeps slice 3's inline "couldn't save" message and **Check the form again**, reworded without "the window may have closed". |
| 9 | **"No partial data is saved"** holds because every guard runs before any mutation and each command commits once. This slice pins it: a domain test asserts that a refused revision leaves target, slot requests, `RevisedAtUtc` and events untouched; interactor tests assert no `Add`/`CommitAsync`; the smoke checks the stored rows after a refused `PUT`. |
| 10 | **The smoke tests run against a throwaway database** (`drivinglessons_us25_smoke`), because roster uploads deactivate students. Never run them against the dev database. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; routes are prefixed `api/`; action methods end with `Async`.
- Domain methods take instants as `DateTimeOffset` (`Publication.ExtendWindow/Reopen`, `Submission.Create/Revise` precedent).
- Application tests build real domain objects (`Teacher.Create`, `Publication.Create` → `Publish` → `Open`, …). `DrivingLessons.Application.Test` does not reference the domain fake builders.
- Query responses map entities through a static `Selector` expression (`GetWeekScheduleResponse`, `SlotForIdentifyStudentResponse`), nested collections included.
- The client uses **Transloco** (`TranslocoPipe`, `| transloco`); translation files are `client\public\i18n\en.json` + `he.json`; top-level key `studentForm`. Singular/plural pairs are separate keys (`…One` / `…Many`), chosen by a `computed` (`submittedBodyKey` precedent).
- Client `domain\` files never import from `data\`; they declare their own structural shapes (`StudentSlot` precedent), which the DTOs satisfy.
- PrimeNG is imported as modules (`ButtonModule`, `MessageModule`); `p-message`s inside the identify and review steps already have their leave animation disabled in the component SCSS. New messages must sit inside `.identify__result` / `.review` to inherit that.
- New TypeScript files use 4-space indentation. No comments anywhere except `//given //when //then` test markers.
- FakeItEasy: the most recent `A.CallTo(...).Returns(...)` wins, so a test can re-point `timeProvider.GetUtcNow()` after `Init`.

## Global Constraints

- The national ID travels **only in request bodies**: never in a URL, query string, route, `localStorage`, log line, exception message, or response body.
- No id beyond the slot ids slice 2 already returns reaches the anonymous endpoints. The saved submission carries slot ids, session types, constraints, target and one instant, and nothing else.
- All API calls use relative `api/...` URLs; no absolute domains.
- Every user-visible string is a translation key present in **both** `en.json` and `he.json` in the same commit.
- Logical CSS properties only; tokens only, no hex colors in components.
- Student surface is mobile-first: designed at ~375px, 40px minimum touch targets, single column (requirements §10).
- Hebrew/RTL verified before the slice is done (client-i18n rule 2).
- No business rules in the client: it renders the backend's outcome. Dropping picks whose slot is not open is not a new rule. It is slice 3's existing `picks` filter applied to loaded data, and the backend rejects those slots regardless.
- No new NuGet/npm packages, no EF migration, no new endpoint.

## Review Focus

Inputs the stories imply but a happy-path test would not exercise. Each is pinned by a step in the owning task:

1. **A saved pick whose slot is no longer open** (admin marked it Unavailable after the student submitted, or a roster rebind moved the student to another teacher's grid) → dropped from the loaded list, the review says "1 of your earlier picks is no longer offered…", and it is never sent back → task 4 `loadedSubmissionOf` specs, task 5 "tells the student when saved picks are no longer offered", task 7 Step 4.
2. **A saved target the surviving picks no longer cover** (target 2, two of three picks dropped) → the loaded target stays, the review blocks with the existing "Not enough picks" message and the student can add a slot or lower the target → task 5 same test, task 7 Step 4.4.
3. **The end time passes before the close job flips the state** → the submit is refused at that instant (409 `submission-window-closed`) even though the publication still reads Open → task 1 `Create__/Revise__Window_Must_Not_Have_Ended`, `Window_That_Has_Ended_Is_Rejected_Before_The_Close_Job_Runs`.
4. **A refused revision** → the earlier version stays exactly as it was: target, ranks, constraints, `revised_at_utc` → task 1 `Revise__Rejected_Revision_Keeps_The_Previous_Version` + `Window_That_Has_Ended_Is_Rejected_And_Keeps_The_Previous_Version`, task 3 Step 5 psql, task 7 Step 6.8.
5. **"Check the form again" after a rejection** reloads identify, which now carries the saved submission, and must **not** overwrite the student's in-progress edits → task 5 "keeps the edits in progress when the form is checked again", task 7 Step 5.2.
6. **Typing a returning student's ID, then replacing it with another student's ID** before continuing → the second student starts fresh (target 1, no picks), never with the first student's list → task 5 "starts fresh when a returning ID is replaced by a first-timer's".
7. **Any other 409** (duplicate slot, unavailable slot, POST-when-exists) → no `type`, still the inline "couldn't save" + Check the form again, never the window-closed screen → task 3 Step 4 checks 2–3, task 4 `isWindowClosedProblem` specs, task 6 "still offers a recheck for any other rejection", task 7 Step 5.2.
8. **Another student's submission** → never returned: identify for B answers `submission: null` while A has one → task 2 Step 5 check 5.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-window-ends-at-end-time.md](task-01-window-ends-at-end-time.md) | Domain + interactors: the window ends at `EndUtc`; refused revisions keep the previous version; revise any number of times (TDD) | ✅ own commit |
| 2 | [task-02-identify-returns-submission.md](task-02-identify-returns-submission.md) | API: identify returns the saved submission, with a smoke test on a throwaway DB (create, revise twice, other students) | ✅ own commit |
| 3 | [task-03-window-closed-problem-type.md](task-03-window-closed-problem-type.md) | API: `problems/submission-window-closed` ProblemDetails type, with a smoke test of POST/PUT after close and the stored rows unchanged | ✅ own commit |
| 4 | [task-04-client-domain-data.md](task-04-client-domain-data.md) | Client: saved-submission DTOs, `loadedSubmissionOf`, `isWindowClosedProblem`, `windowClosed` step (TDD, Vitest) | ✅ own commit |
| 5 | [task-05-client-returning-student.md](task-05-client-returning-student.md) | Client: seed the wizard, welcome-back banner, dropped-picks notice, "Changes saved" (US-25, US-43) | ✅ own commit |
| 6 | [task-06-client-window-closed-screen.md](task-06-client-window-closed-screen.md) | Client: the window-closed-mid-submit screen; generic rejection reworded (US-42) | ✅ own commit |
| 7 | [task-07-verification-and-pr.md](task-07-verification-and-pr.md) | End-to-end browser verification (HE + EN, 375px, every edge path), roadmap link, PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `26-us-25-43-42-edit-and-close-race` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly. Each step has full file contents or an anchored edit, plus exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` has an unrelated local modification (and task 2 adds a local-only `api-smoke` entry). Never stage it: `git add` only the paths each task lists.

Backend commands run from the repo root. Client commands run from `client\`: `npm test -- --watch=false` and `npm run build`. Environment notes (project memory):
- The default npm is broken for installs: if `npm ci` is ever needed, use nvm **v22.6.0** for the install only; build and test with node **v26.4.0** and the local `ng`.
- Browser-pane screenshots are flaky on this PrimeNG app: `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence.
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`). `dl-postgres` has a stale migration history.

## Open Items (non-blocking)

1. **Reorder is slice 4** ([#38](https://github.com/silagy/DrivingLessonsBooking/issues/38)). Until it ships, US-43's "change … ranks" means remove a pick and add it again (it goes to the end).
2. **Two concurrent `PUT`s** for the same student (two tabs) can still surface a concurrency 500 (carried from slice 3 open item 1).
3. **Close-job lag, now visible rather than silent:** for the seconds between `EndUtc` and the close job, the link GET still says `open`, so a student can load and identify, and the submit then lands on the window-closed screen. That is the correct outcome.
4. **The window-closed screen has no action.** If the school reopens, the student reopens the link (mockup `SDoneErr`, "this link will work again").
5. **No rate limiting on the anonymous endpoints** (carried from slice 2 open item 2). The saved list is now readable by ID, which is the same accepted-risk posture (requirements §8.2 / ADR 0003).

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Domain\
├── Values\SubmissionWindow.cs                                               + HasEndedBy(instant)
└── Entities\Publication.cs, Submission.cs                                   + IsOpenAt(instant); window guard takes the instant
src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentResponse.cs   HasSubmission → Submission (+ 2 nested responses)
src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs  loads the saved submission
src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs            + problem type;  ProblemTypes.cs  new
tests\DrivingLessons.Domain.Test\
├── Values\SubmissionWindowTest.cs                                           + Has_Ended_By
└── Entities\PublicationTest.cs, SubmissionTest.cs                           + Is_Open_At…, window-end / keeps-previous / replaces-every-version
tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs, ReviseSubmissionInteractorTest.cs   + window-end, any-number-of-times
client\src\app\features\student-form\
├── data\     identify-student.response.ts (hasSubmission → submission), problem-types.ts (+ spec)   new/changed
├── domain\   loaded-submission.ts (+ spec), welcome-back.ts                 new
│             student-form-step.enum.ts (+ windowClosed), student-form-step.spec.ts
├── state\    student-form.store.ts                                          seed, welcomeBack, droppedPickCount, titles, window-closed
│             student-form.store.spec.ts                                     fixture
└── ui\
    ├── pages\student-form\          student-form.page.html/.scss/.spec.ts   welcomeBack, droppedPickCount, titles, windowClosed step
    └── components\  identify-step\ (welcome back + button label), review-step\ (dropped notice)
client\public\i18n\en.json + he.json   studentForm: identify.welcomeBack*, identify.editSubmission, review.dropped*,
                                       review.errors.rejected (reworded), submitted.revisedTitle, closedMidSubmit.*
docs\modules\student-form\README.md    slice 5 row links this plan
```
