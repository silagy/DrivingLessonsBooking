# US-22: Student Form Fully Usable on a Mobile Browser — Task Index (student-form slice 6)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 6 of the [student-form roadmap](../README.md). Execute the tasks **in order**, one per session. Each file is self-contained.

**Goal:** A student who opens the weekly link from WhatsApp on a phone (~375px, down to 320px) completes the whole flow, national ID → read-only details → target count → slot picking → ranking → submit, plus the return-to-edit path and every status screen, with no horizontal scrolling and no desktop-only interaction, in Hebrew and English. Fix the three mobile defects found by the planning-time audit and record the acceptance run. Covers GitHub issue [#23](https://github.com/silagy/DrivingLessonsBooking/issues/23) (US-22).

**Architecture:** Client only. Mobile-first was built into slices 1–5 (roadmap: "mobile-first is built into every slice, not a phase"), so this slice is an acceptance check plus three small, targeted fixes, not a redesign. Task 1 isolates the student-typed constraint's reading direction (`dir="auto"` on the pick-sheet textarea and on the review's `<q>`), pinned by page specs. Task 2 keeps time ranges on one line (`white-space: nowrap`) on the review and in the pick sheet. Task 3 adds `interactive-widget=resizes-content` to the viewport meta, so on Android the on-screen keyboard shrinks the layout and the pick sheet's Save button and the sticky wizard footer stay above it. Task 4 runs the full acceptance matrix (320 and 375px × Hebrew and English × every step and status screen) with a pasted audit script, links the plan from the roadmap and opens the PR.

**Tech Stack:** Angular 21 (signals, standalone, zoneless) + PrimeNG 21 + Transloco; Vitest (via `ng test`). No backend change, no migration, no new endpoint, no new package, no new translation key.

**Spec:** issue [#23](https://github.com/silagy/DrivingLessonsBooking/issues/23) · [student-form roadmap](../README.md) (slice 6 row "Closes last as the ~375px end-to-end acceptance check", locked decisions 5 and 6) · [requirements §3 (no native app), §7 (student flow), §10 ("Student form must be fully usable on mobile browsers")](../../../requirements.md) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) (flow starts with national ID, no teacher-confirmation step) · `.claude\rules\client-primeng.md` "Responsive Sizing" (375px design width, 40px touch targets, single column) · `.claude\rules\client-i18n.md` RTL rule "wrap with `<bdi>` or `unicode-bidi: isolate` where mixing breaks" · [slice 4 plan](../us-38-reorder-ranked-list-plan/README.md) open item 1 (real-device touch drag "worth one try … before the mobile-acceptance slice")

**Branch:** `23-us-22-mobile-browser`, created at planning time from `38-us-38-reorder-ranked-list` at `ddb7dcf`, because PR [#69](https://github.com/silagy/DrivingLessonsBooking/pull/69) (slice 4) was still open and tasks 1–2 edit the review step that slice 4 rewrote. Once #69 is merged, run `git fetch origin && git rebase origin/main` before opening the PR (task 4 Step 12). The slice-4 commits are already on `main` by then, so the rebase drops them.

## User Story

**US-22** ([#23](https://github.com/silagy/DrivingLessonsBooking/issues/23)): *As a student opening the link from WhatsApp on my phone, I want the form to be fully usable in a mobile browser, so that I can submit my preferences right where the link reached me.*
- **Given** I received the weekly link via WhatsApp **and** I open it in a mobile browser (~375px viewport) **When** I go through national ID entry, the read-only details confirmation, target count, slot picking, ranking, and submit **Then** every step is completable without horizontal scrolling or desktop-only interactions.

## Planning-time audit (2 October 2026)

The planner ran the real stack (from-source API on a throwaway database, `ng serve`) in the browser pane at **375×812** and **320×568**, in Hebrew and English, through identify (found, welcome-back, not-on-roster, invalid ID), details, target, slots, pick sheet, review (with reorder), submit, confirmation and the invalid-link screen. Seed data used deliberately long values: teacher `יהונתן בן-שמעון אברמוביץ׳`, student `אלכסנדרה מונטגומרי-רוזנברג שטיינהרט`, car `Toyota Yaris Hybrid Automatic 2024`, and a 90-character constraint. At every step a script measured the page's `scrollWidth`, every visible element's box against the viewport, every interactive element's size and every input's font size.

**Already passing (no task, re-checked in task 4):**
- No horizontal scroll at 320 or 375px in either language on any screen: `document.documentElement.scrollWidth === innerWidth`, and no visible element crosses either viewport edge.
- Every button, chip, input, radio label, grip and arrow is at least 40×40px.
- The national-ID input and the constraint textarea are 16px, so iOS Safari does not zoom on focus. The national-ID input uses `inputmode="numeric"`. The viewport meta does not disable pinch-zoom.
- Page scroll is locked while the pick sheet is open (`html:has(app-pick-sheet) { overflow: hidden }`), and the sheet scrolls itself (`max-height: 90dvh`, `overscroll-behavior: contain`).
- No desktop-only interaction: the pick sheet closes by backdrop tap or Cancel as well as Escape, reorder has Move up / Move down buttons as well as drag, and nothing depends on hover.
- The shell uses `100dvh`. All component styles use logical properties.
- Long hyphenated names and car names wrap inside their cards.
- The global `<p-toast>` container (`position: fixed`, 400px wide, `right: 20px`) sits partly off-screen at 320px, but it is empty on the student route because only admin stores raise toasts. A fixed, empty element adds no scroll width. Not a student defect (decision 6).

**Defects found (one task each):**

| # | Defect | Where seen | Task |
|---|--------|------------|------|
| D1 | A constraint typed in the other script than the page renders with its quotation marks flipped and its words reordered: on the Hebrew page `only after 16:00, and”` … `”Weizmann`, on the English page the Hebrew text breaks the same way. The `<q>` inherits the page direction and is not isolated. The textarea also lays out mixed text in the page direction while typing. | review rows, pick sheet, 320 and 375px, both languages | 1 |
| D2 | The review row's time range breaks in the middle at 320px in English: `15:00–` on one line, `18:00` on the next (`getClientRects().length === 2` on `.review__time`). | review, 320px EN, the row that carries a Double + constraint | 2 |
| D3 | On Android Chrome (108+), the default `interactive-widget=resizes-visual` leaves the layout viewport full height when the keyboard opens. The fixed pick sheet's Save button and the sticky wizard footer stay behind the keyboard until the student closes it. The pane has no on-screen keyboard, so this comes from browser behavior and measurement (the sheet spans y 491–812 at 375×812, the textarea sits around y 670, and a ~330px keyboard covers everything below y 482), not from an observed screenshot. | pick sheet (constraint), identify step | 3 |

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Acceptance slice, not a redesign.** The planning-time audit decides the scope. Only defects a student would hit on a phone get a task. Everything that already passes is re-checked in task 4 and listed in the PR. |
| 2 | **D1 is fixed with `dir="auto"`, not `<bdi>` or CSS.** `dir="auto"` on the `<q>` gives it its own base direction from its first strong character, and the UA stylesheet isolates any element with a `dir` attribute. The quote marks then sit around the text in its own direction. On the `<textarea>`, `dir="auto"` (UA style `unicode-bidi: plaintext`) aligns each paragraph to the script it is typed in. Both are attributes that a jsdom spec can pin. This follows `client-i18n.md` ("wrap with `<bdi>` or `unicode-bidi: isolate` where mixing breaks"). Roster names interpolated into translated sentences are left alone: the audit found no breakage, and isolating them would mean rewriting translation strings (YAGNI). **Changed after the final review:** an empty `dir="auto"` textarea resolves left-to-right, which put a Hebrew student's caret on the wrong side, so the textarea binds `[attr.dir]` to `'auto'` only while it holds text and otherwise follows the page. The details card's bare roster values (teacher, student, car) also got `dir="auto"`, because an admin-typed car name such as `Kia Picanto (2019)` garbled its brackets on the Hebrew card. |
| 3 | **D2: `white-space: nowrap` on `.review__time` and `.pick-sheet__time`**, not on `.slot-chip__time`. A range like `07:00–12:00` is one unit. The review row wraps the slot name at its spaces and moves the whole range to the next line. The pick-sheet header is already `display: flex`, so the range keeps its own line box. The chip time is **not** changed: at 320px a chip has 58px of content width and the range needs 50px at the chip's current 9px. With `nowrap` a slightly wider device font would overflow the chip's border instead of wrapping inside it. |
| 4 | **D3: `interactive-widget=resizes-content` in the viewport meta** (`client\src\index.html`). Chrome Android 108+ and Firefox Android 132+ then shrink the layout viewport (and `dvh`) while the keyboard is open, so the fixed sheet (`inset: 0`, `max-height: 90dvh`) and the sticky footer end above the keyboard. iOS Safari ignores the key: the student closes the keyboard (Done) and then taps Save, as today (open item 2). The meta is app-wide. The admin is desktop-first, where there is no on-screen keyboard, so this is a no-op there. |
| 5 | **No new end-to-end tooling.** The acceptance matrix runs in the browser pane with an audit script pasted into `javascript_tool` (task 4), the same way slices 1–5 were verified. A Playwright suite at 320/375px is the durable guard if the team wants one. That is its own story (open item 4), not part of US-22. |
| 6 | **The admin-wide toast container is out of scope.** It only matters on the admin surface, which is desktop-first (`client-primeng.md` "Responsive Sizing"). |
| 7 | **Chip time size stays as the mockup has it** (`0.56rem` ≈ 9px). The window name (Morning / Noon / Afternoon / Evening, ≈11.5px) is the primary label, and the hours per window are fixed. A larger time font does not fit a 320px chip without shrinking padding below the mockup (decision 3). Raised as open item 3 for the real-device check. |
| 8 | **Widths: 375px (the AC) and 320px (the smallest common phone width).** Passing both covers 360/390/412px Android and iPhone widths in between, because nothing in the student form switches layout between 320 and 480px (the shell's one breakpoint is `30rem`, where it gains side borders). |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- The client uses **Transloco** (`TranslocoPipe`, `| transloco`); translation files are `client\public\i18n\en.json` + `he.json`. **This slice adds no keys.**
- The page spec (`ui\pages\student-form\student-form.page.spec.ts`) is the component-level test harness for the whole wizard. New behavior is specified there through the DOM, with the fake `SubmissionsApiService`. `TranslocoTestingModule` has no translations, so text renders as key paths.
- Elements are found in specs by stable classes (`.review__constraint`) and ids (`#pick-constraint`).
- New TypeScript uses 4-space indentation. No comments anywhere except `//given //when //then` test markers.
- Layout and typography that jsdom cannot measure (D2, D3) are verified in the browser pane in task 4, the same way slice 4 verified 40px targets and RTL placement.

## Global Constraints

- Student surface is mobile-first: designed at ~375px, **40px minimum touch targets**, single column, **no horizontal scroll** (requirements §10, `client-primeng.md` "Responsive Sizing").
- Logical CSS properties only (`margin-inline-start`, never `margin-left`); tokens only, no hex colors in components.
- Hebrew/RTL verified before the slice is done (`client-i18n.md` rule 2). Times stay LTR (`dir="ltr"` on time spans, unchanged).
- No business rules in the client (CLAUDE.md rule 12). This slice changes presentation only.
- The national ID travels only in request bodies: never in a URL, `localStorage`, a log line or a test name. Test data uses synthetic IDs only (`000000018`, `000000026`, `000000034`).
- No backend change, no EF migration, no new endpoint, no `package.json` change, no new translation key.

## Review Focus

Inputs and conditions the story implies but a happy-path test would not exercise. Each is pinned by a step in the owning task:

1. **A constraint in the other script than the page** (English on the Hebrew page, Hebrew on the English page) → the review shows it with quotes around the text in its own direction, and the textarea aligns it as typed → task 1 "keeps a typed constraint in its own reading direction on the review", "lets the constraint follow the direction it is typed in", task 4 Steps 4.3–4.4 and 5.2, 5.5.
2. **The narrowest common phone (320px) in English**, where strings are longest → no horizontal scroll, no time range split across lines, every control at least 40px → task 4 Steps 4.5, 5 and 8.
3. **The keyboard open over the pick sheet** while typing a constraint on Android → Save stays reachable without closing the keyboard → task 3, task 4 Step 7 (real device, human-run). Pane fallback: task 4 Step 2.3 (the meta is served) and Step 4.3 (the open sheet audits clean at 320×568).
4. **A returning student editing on a phone** (welcome-back banner, review with the replaces notice, `PUT`) → same no-scroll and target checks as a first submission → task 4 Step 4.
5. **Every status screen at 320px**: invalid link, not on the roster, invalid ID, window closed mid-submit, closed → no overflow, message readable, nothing desktop-only → task 4 Step 8.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-direction-isolated-constraints.md](task-01-direction-isolated-constraints.md) | `dir="auto"` on the pick-sheet textarea and the review's constraint (D1), TDD with page specs | ✅ own commit |
| 2 | [task-02-unbroken-time-ranges.md](task-02-unbroken-time-ranges.md) | `white-space: nowrap` on the review and pick-sheet time ranges (D2) | ✅ own commit |
| 3 | [task-03-keyboard-aware-viewport.md](task-03-keyboard-aware-viewport.md) | `interactive-widget=resizes-content` in the viewport meta (D3) | ✅ own commit |
| 4 | [task-04-mobile-acceptance-and-pr.md](task-04-mobile-acceptance-and-pr.md) | Acceptance matrix (320/375px × HE/EN × every step and status screen), real-device check, roadmap link, PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `23-us-22-mobile-browser` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly. Each step has full file contents or an anchored edit, plus exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` gets a local-only `api-smoke` entry in task 4. Never stage it: `git add` only the paths each task lists.

Client commands run from `client\`: `npm test -- --watch=false` and `npm run build`. Environment notes (project memory):
- `npm test` / `npm run build` work with the default node. No install is needed in this slice.
- Browser-pane screenshots are flaky on this PrimeNG app: `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence. Screenshots sometimes come back tiled. Trust the measurements over the picture.
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`). `dl-postgres` has a stale migration history.
- **Seeding Hebrew from Git Bash on Windows:** `curl` is the Windows binary, so Hebrew passed in an argument (`-d '{"name":"…"}'`) arrives as `?????`, and an `-F file=@/tmp/…` path fails with status `000`. Task 4 writes every Hebrew payload to a file and runs `curl` from that directory with relative `@file` paths.

## Open Items (non-blocking)

1. **Real-device checks are human-run** (task 4 Step 7). There is no staging environment, so a phone reaches the dev server over the LAN. If no phone is at hand, the PR says so, and touch dragging (slice 4 open item 1) plus the Android keyboard behavior (D3) stay unverified on hardware.
2. **iOS Safari ignores `interactive-widget`.** With the keyboard open over the pick sheet, Save sits behind it until the student taps Done. That is one extra tap, not a blocker. A `visualViewport`-driven inset is the next step if iPhone students complain.
3. **Chip time legibility** (≈9px, decision 7). Worth a look on the real device. Changing it means a design change to the chip, not a bug fix.
4. **No automated mobile regression guard** (decision 5). If the student form keeps changing after v1, a Playwright suite running the task-4 audit at 320/375px in both languages is the follow-up story.

## Target Layout (new/changed this slice)

```
client\src\index.html                                        viewport meta + interactive-widget=resizes-content
client\src\app\features\student-form\ui\
├── components\pick-sheet\   .html                           dir="auto" on #pick-constraint
│                            .scss                           .pick-sheet__time nowrap
├── components\review-step\  .html                           dir="auto" on .review__constraint
│                            .scss                           .review__time nowrap
└── pages\student-form\      student-form.page.spec.ts       + two specs (picking slots, review and submit)
docs\modules\student-form\README.md    slice 6 row links this plan
```
