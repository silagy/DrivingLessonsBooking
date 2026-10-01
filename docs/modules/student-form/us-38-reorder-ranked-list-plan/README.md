# US-38: Reorder the Ranked List — Task Index (student-form slice 4)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 4 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session. Each file is self-contained.

**Goal:** On the review step, a student can move any pick to a different position in their ranked list, by dragging it by a handle or with Move up / Move down buttons. The ranks renumber to the new order, the preferred/backup split follows the new positions, and that order is what the Submission carries, for a first submission (`POST`) and for an edit (`PUT`). Covers GitHub issue [#38](https://github.com/silagy/DrivingLessonsBooking/issues/38) (US-38).

**Architecture:** Client only. The server already ranks slot requests by their position in the request (`Submission.Create/Revise` → `Rank.Of(position)`, pinned by `SubmissionTest.Create__Ranks_Slot_Requests_In_Selection_Order` and `Revise`). Identify already returns the saved list ordered by rank. A new pure domain function `movePick(picks, { slotId, toIndex })` in `domain\slot-pick.ts` returns the reordered list. The store gets one mutation, `reorderPick(move)`, plus `canReorder` and `movedPickRank` signals. The review step renders Move up / Move down icon buttons per pick (task 2) and an Angular CDK drag-drop list with a grip handle per pick (task 3). Both emit the same `PickMove` output. Ranks, the preferred markers, the slot-chip badges and the submit payload are already derived from the order of `picks`, so they follow without further changes.

**Tech Stack:** Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco; `@angular/cdk/drag-drop` 21.2.14 (already installed as PrimeNG's peer dependency, declared explicitly in task 3); Vitest (via `ng test`). No backend change, no migration, no new endpoint.

**Spec:** issue [#38](https://github.com/silagy/DrivingLessonsBooking/issues/38) · [student-form roadmap](../README.md) (slice 4 row "Drag-reorder of the ranked list on the review screen", locked decisions 4–7) · [requirements §5.6 (Rank = position in the student's ordered list), §7 step 8 ("The student may reorder before submitting"), §10 (mobile)](../../../requirements.md) · [slice 3 plan](../us-33-first-submission-plan/README.md) decisions 4 and 13 (rank is the pick's position, never sent by the client) · [slice 5 plan](../us-25-43-42-edit-and-close-race-plan/README.md) open item 1 (until this ships, a rank changes only by removing and re-adding) · mockup `Driving Lesson Mockup\mock\student.jsx` → `SReview` / `PickRow` (the mockup has no reorder affordance; slice 3 task 10 notes "No drag handle — reorder is slice 4")

**Branch:** `38-us-38-reorder-ranked-list` (created from `main` at `d6eaa15` at planning time, after slice 5 / PR #65 and the excel module merged)

## User Story

**US-38** ([#38](https://github.com/silagy/DrivingLessonsBooking/issues/38)): *As a student, I want to reorder my ranked list before submitting, so that my final ranking reflects my real preferences, not just the order I clicked in.*
- **Given** I have picked multiple Slots into a ranked list **When** I move a pick to a different position before submitting **Then** the ranks are renumbered to the new order and that order is what the Submission carries.

## Context

Slices 1, 2, 3 and 5 are on `main`. The wizard is identify → details → target → slots → review, with `done` and `windowClosed` after it. Picks live in the store as `chosenPicks: SlotPick[]`, and the `picks` computed filters them to slots still open. Everything rank-shaped is derived from the order of `picks`:

- `reviewItemsOf(picks, slots, target)` → `rank = index + 1`, `isPreferred = rank <= target` (`domain\review-item.ts`)
- `groupSlotsByDay(…, picks)` → slot-chip rank badges via `rankOf` (`domain\slot-day.ts`)
- `submit()` → `slotRequests: picks.map(…)` in list order, for both `POST` and `PUT`

So reordering is one list operation plus UI. Nothing on the server changes: rank is never sent, it is the position.

`@angular/cdk` 21.2.14 is in `client\node_modules` and `package-lock.json` (`"peer": true`), pulled in as PrimeNG 21's peer dependency, but `client\package.json` does not declare it.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Client only.** No backend, API or migration change. Rank is the position in the `slotRequests` array (slice 3 decision 4). The domain tests listed under Architecture already pin "ranks follow the list order" for create and revise. Task 4 checks the stored `rank` column after a reordered `POST` and a reordered `PUT`. |
| 2 | **Two ways to reorder, one path through the store.** A **drag handle** per pick (the roadmap's "drag-reorder") and **Move up / Move down** buttons per pick. The buttons are the single-pointer alternative to dragging (WCAG 2.2 SC 2.5.7) and the keyboard and screen-reader path. They are also more reliable than dragging on a 375px touch screen. Both emit `PickMove { slotId, toIndex }` → `store.reorderPick(move)` → `movePick(picks, move)`. |
| 3 | **Drag uses `@angular/cdk/drag-drop`**, declared in `client\package.json` at the already-locked 21.2.14 (task 3). It is not a new package: it is already installed and locked as PrimeNG's peer, so the lockfile only loses `"peer": true`. PrimeNG's `p-orderList` was rejected because it is a selectable list box with its own chrome, not the mockup's ranked cards. A hand-rolled pointer-event drag was rejected because CDK already solves touch, auto-scroll and the drop animation. If a reviewer rejects the dependency, drop task 3: task 2 alone satisfies the acceptance criterion. |
| 4 | **Dragging starts only from the grip handle** (`cdkDragHandle`), never from the whole card. On a phone, a touch elsewhere on the card scrolls the page as before. The grip sits at the inline-start edge (the right in Hebrew). It is `aria-hidden` because the buttons are the accessible path. The list is locked to the vertical axis and the drag preview stays inside the list (`cdkDragPreviewContainer="parent"`), so it inherits the page direction and tokens. |
| 5 | **Reorder lives on the review step only** (roadmap slice 4). The slots step keeps tap-to-add, edit and remove. Its chip badges show the new ranks because they are derived from the same list. A pick added after a reorder goes to the end, as every new pick does. |
| 6 | **Rank follows position, everything else travels with the pick.** Session type and constraint move with it. The preferred/backup boundary stays at rank = target, so promoting a backup into the top ranks makes it preferred and pushes the last preferred pick to backup. No new rule: `reviewItemsOf` already does this. |
| 7 | **The controls appear only when there are at least two picks** (`canReorder = pickCount > 1`). Move up is disabled on rank 1 and Move down on the last rank. While a submission is in flight, the buttons are disabled, the drop list is disabled and `reorderPick` ignores calls, so the list on screen is always the list that was sent. After a failed or rejected submit, reordering works again. |
| 8 | **Focus and announcement.** After a button move, focus stays on the same button of the moved pick. If that button has just become disabled (the pick reached the top or bottom), focus goes to the pick's other move button, so keyboard focus is never dropped onto `<body>`. A visually hidden `aria-live="polite"` region announces "Moved to rank {n}." (`movedPickRank`). It is cleared whenever the review step is entered. The buttons' accessible names carry the rank and slot ("Move pick 3 (Wednesday · Afternoon) up"). |
| 9 | **A one-line hint** under the summary explains the controls while reordering is possible: task 2 "Use the arrows to move a pick up or down.", replaced in task 3 by "Drag a pick by its handle, or use the arrows, to change its rank." |
| 10 | **`movePick` clamps and never throws.** A `toIndex` before the start or past the end clamps to the first or last position. An unknown `slotId` returns the list unchanged. It always returns a new array. CDK and the buttons only produce valid indexes, so clamping is a safety net, not a rule. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- The client uses **Transloco** (`TranslocoPipe`, `| transloco`); translation files are `client\public\i18n\en.json` + `he.json` (2-space indented); top-level key `studentForm`. Hebrew copy uses the plural imperative (הוסיפו, בדקו) used across the student form.
- Client `domain\` files never import from `data\`.
- PrimeNG is imported as modules (`ButtonModule`, `MessageModule`). Icon-only buttons use `icon="pi pi-…"` + `[ariaLabel]` (precedent `car-card.component.html`).
- The page spec (`ui\pages\student-form\student-form.page.spec.ts`) is the component-level test harness for the whole wizard. The review step has no spec of its own. New behavior is specified there through the DOM, with the fake `SubmissionsApiService`.
- Elements are found in specs by stable classes (`.review__item`, `.review__rank`) and data attributes (`data-slot-id` on chips; this slice adds `data-pick-id` on review rows, so a chip and a row are never confused).
- New TypeScript uses 4-space indentation. No comments anywhere except `//given //when //then` test markers.
- Named constants instead of magic numbers where a number carries meaning (`SINGLE_PREFERRED_TARGET` precedent in `review-step.component.ts`). ±1 index arithmetic is not a magic number.

## Global Constraints

- Every user-visible string is a translation key present in **both** `en.json` and `he.json` in the same commit.
- Logical CSS properties only (`margin-inline-start`, never `margin-left`); tokens only, no hex colors in components.
- Student surface is mobile-first: designed at ~375px, **40px minimum touch targets**, single column, no horizontal scroll (requirements §10, client-primeng "Responsive Sizing").
- Hebrew/RTL verified before the slice is done (client-i18n rule 2). Up/down arrows are vertical and are **not** mirrored.
- No business rules in the client (CLAUDE.md rule 12): the client never sends a rank. The order of `slotRequests` is the ranking, and the server numbers it.
- The national ID travels only in request bodies: never in a URL, `localStorage`, a log line or a test name.
- No backend change, no EF migration, no new endpoint. The only `package.json` change is declaring the already-locked `@angular/cdk` (decision 3).

## Review Focus

Inputs and conditions the story implies but a happy-path test would not exercise. Each is pinned by a step in the owning task:

1. **A backup promoted into the preferred ranks** (target 2, rank 3 moved to rank 1) → it gets the preferred styling and the old rank 2 becomes a backup. The request carries the new order with each pick's own session type and constraint → task 2 "promotes a later pick and submits the new order", "moves the preferred boundary with the order", task 4 Step 2.
2. **A moved pick's session type and constraint** (a Double with "only after 16:00" moved down two ranks) → they stay on that pick, never on whatever pick now holds its old rank → task 1 "keeps the session type and constraint with the moved pick", task 2 "promotes a later pick and submits the new order".
3. **Back to the grid after a reorder, then add a pick** → chip badges show the new ranks, the new pick is appended after the reordered list, never re-sorted to selection order → task 2 "shows the new ranks on the grid and appends a new pick after them", task 4 Step 3.
4. **Reordering while a submission is in flight** → impossible: buttons disabled, drop list disabled, store ignores the call. The confirmation reflects exactly what was sent → task 2 "locks reordering while the list is being sent", task 3 "locks dragging while the list is being sent".
5. **A returning student reorders their saved list** → the `PUT` carries the new order and the stored ranks match it. Re-entering shows the new order → task 2 "reorders a saved list and replaces it in the new order", task 4 Step 4.
6. **Keyboard focus when a pick reaches the top or bottom** → focus moves to the pick's other arrow instead of being dropped → task 2 "keeps focus on a pick that reaches the top".
7. **A drop on the pick's own position, or a single-pick list** → nothing changes and nothing is announced. A single pick shows no handle, no arrows and no hint → task 2 "offers no reordering for a single pick", task 3 "changes nothing when a pick is dropped where it started", "offers a drag handle on every pick when there is more than one".
8. **Hebrew/RTL at 375px** → the grip sits on the right, the arrows are not mirrored, the drag preview stays inside the list with `dir="rtl"`, there is no horizontal scroll and every control is at least 40×40 → task 4 Steps 2 and 6.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-move-pick.md](task-01-move-pick.md) | Client domain: `PickMove` + `movePick` (TDD, Vitest) | ✅ own commit |
| 2 | [task-02-move-buttons.md](task-02-move-buttons.md) | Store `reorderPick` / `canReorder` / `movedPickRank`; Move up / Move down buttons, focus, live announcement, hint (US-38 AC) | ✅ own commit |
| 3 | [task-03-drag-handle.md](task-03-drag-handle.md) | Declare `@angular/cdk`; drag-reorder by a grip handle on the review step | ✅ own commit |
| 4 | [task-04-verification-and-pr.md](task-04-verification-and-pr.md) | End-to-end browser verification (HE + EN, 375px, POST and PUT, stored ranks), roadmap link, PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `38-us-38-reorder-ranked-list` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly. Each step has full file contents or an anchored edit, plus exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` gets a local-only `api-smoke` entry in task 4. Never stage it: `git add` only the paths each task lists.

Client commands run from `client\`: `npm test -- --watch=false` and `npm run build`. Environment notes (project memory):
- The default npm is broken for installs: task 3's `npm install` uses nvm **v22.6.0** (`& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" install`). `npm test` / `npm run build` (`npm run` only) are unaffected.
- Browser-pane screenshots are flaky on this PrimeNG app: `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence.
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`). `dl-postgres` has a stale migration history.

## Open Items (non-blocking)

1. **Touch dragging on a real phone is not exercised** by the specs (jsdom) or the browser pane (mouse events, even with the mobile preset). CDK supports touch and the grip has `touch-action: none`. The arrows are the guaranteed path. Worth one try on a real device before the mobile-acceptance slice ([#23](https://github.com/silagy/DrivingLessonsBooking/issues/23)).
2. **No reorder on the slots step.** The grid is a day-by-day chip list, not a ranked list. If students ask for it there, it is a new story.
3. **Long lists:** CDK auto-scrolls the page while a pick is dragged near the viewport edge. With 10+ picks, the arrows may still be quicker. No "move to top" control (YAGNI until asked).

## Target Layout (new/changed this slice)

```
client\package.json, package-lock.json                       + "@angular/cdk": "^21.2.0" (already locked at 21.2.14)
client\src\app\features\student-form\
├── domain\   slot-pick.ts (+ spec)                          + PickMove, movePick
├── state\    student-form.store.ts                          + canReorder, movedPickRank, reorderPick; reset on entering review
└── ui\
    ├── pages\student-form\  student-form.page.html          binds canReorder / movedPickRank / (moved)
    │                        student-form.page.spec.ts       + "reordering the ranked list" describe
    └── components\review-step\  .ts/.html/.scss             move buttons, focus, live region, hint, cdkDropList + grip
client\public\i18n\en.json + he.json   studentForm.review: moveUp, moveDown, movedTo, reorderHint
docs\modules\student-form\README.md    slice 4 row links this plan
```
