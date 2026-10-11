# #106: The admin header fits at tablet width - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of bug [#106](https://github.com/silagy/DrivingLessonsBooking/issues/106), found while verifying #95 on the spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) epic branch. Each task has its own file. Execute in order. Every commit references #106.

**Goal:** At 768px and wider, no Administrator page scrolls sideways, in Hebrew (RTL) and in English. Every nav link and the user menu stay visible and reachable.

**Architecture:** CSS only, in `admin-shell.component.scss`. No template, TypeScript or translation change.
- **At ≤ 992px the bar becomes two rows.** Row 1 holds the logo at inline-start and the language toggle plus user button at inline-end. Row 2 holds the nav, across the full width (`flex-wrap: wrap`; the nav gets `order: 1; flex-basis: 100%`). The 992px breakpoint is the repo's one mobile/desktop breakpoint.
- **At every width:**
  - The sign-in email in the user button is capped (`max-inline-size: 12rem`, with an ellipsis). A long email can no longer widen the bar, and the full email stays in the user menu's header.
  - The nav can shrink (`min-inline-size: 0`) and scrolls inside the bar with a thin visible scrollbar (`overflow-x: auto`). If its links ever don't fit, the nav scrolls, never the page.
- **A measured fallback.** If the single-row bar at 993-1024px still makes the nav scroll in either language, the email is hidden from 993 to 1199px as well. Task 1 measures this and applies the fallback only when the measurement calls for it.

**Tech Stack:** Angular 21 (standalone, OnPush), PrimeNG 21, SCSS, Transloco. Verified in the browser pane (Claude Browser tools) against the dev API. Vitest runs under jsdom, which can't measure layout, so no unit test can.

**Spec:** issue [#106](https://github.com/silagy/DrivingLessonsBooking/issues/106): Expected, possible approaches, How to check · `.claude\rules\client-i18n.md` (logical properties) · `.claude\rules\client-primeng.md` ("One breakpoint convention: `992px` separates mobile/desktop"; "Never use `!important`, unscoped `::ng-deep`, or physical CSS properties in overrides") · precedent `client\src\app\features\students\ui\pages\students\students.page.scss:324` (`@media screen and (max-width: 992px)` inside a component).

**Branch:** `106-admin-header-tablet`, from `82-users-and-roles` at `2195dc9` (contains #109). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Bar | `.shell__bar`: one flex row, `height: 3.875rem`, `gap: 2.25rem`, `padding-inline: 2.25rem`. No `@media` rule anywhere in the file | `client\src\app\features\admin-shell\admin-shell.component.scss` |
| Children | `<app-brand-logo />` (mark + "WeekDrive"), `nav.shell__nav` (`gap: 1.625rem`, `align-self: stretch`), and `div.shell__actions` (`margin-inline-start: auto`) holding `<app-language-toggle />` and the user `p-button` (avatar, `<bdi class="shell__email" dir="ltr">`, chevron) | `admin-shell.component.html` |
| Nav | An Administrator gets 7 links (dashboard, teachers, students, weeklyPrep, publications, history, users); a Teacher gets 3. Hebrew labels "לוח בקרה ... משתמשים", English "Dashboard ... Users" | `domain\navigation.ts`, `client\public\i18n\*.json` `shell.nav` |
| Measured | `/students`, Hebrew, 768×1024: `scrollWidth` 1022 > `innerWidth` 768 (issue #106) | |
| Active link | `.shell__nav-link--active::after` draws a 3px bar at `inset-block-end: 0` of the link; the link stretches to the nav's height | same scss |
| User menu | `p-menu` popup, `appendTo="body"`, `styleClass="shell-user-menu"`; its header already shows the full email and the Role tag | same html |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Two rows at ≤ 992px, rather than collapsing the nav into a menu.** Two rows keep every link one tap away, need no TypeScript, and keep the active-link underline. At 768px the nav row has about 736px; the 7 links need roughly 540px in Hebrew and 600px in English. A hamburger menu would hide the screens behind a second click, and #106 lists it only as one possibility. |
| 2 | **The breakpoint is 992px**, the repo's single mobile/desktop breakpoint (`client-primeng.md`). Task 1 adds one more media query only if measurement shows the single-row bar still overflows at 993-1024px (decision 4). |
| 3 | **The email stays visible in the user button, capped at 12rem with an ellipsis.** A short email (the dev `admin@local.dev`) shows in full. A long one can't widen the bar. The full email is always in the user menu's header, and the button keeps its `aria-label` ("תפריט משתמש"). |
| 4 | **The fallback is measured, not guessed.** Estimating label widths is unreliable, so task 1 measures the nav at 993, 1024 and 1100px in both languages. If the nav overflows at 993 or 1024px in either language, task 1 adds `@media screen and (min-width: 993px) and (max-width: 1199px) { .shell__email { display: none; } }` and measures again. Otherwise there is no extra breakpoint. |
| 5 | **The nav keeps a visible thin scrollbar when it overflows** (`scrollbar-width: thin`), never `none`. It is a safety net that should not trigger at ≥ 768px after decisions 1 and 4. If it ever does, a mouse user can still see that there is more and reach it. Keyboard focus scrolls a link into view by itself. |
| 6 | **`overflow-x` is the one non-logical property**, and it's allowed: it names an axis, not a side, so it mirrors correctly in RTL. The logical `overflow-inline` isn't supported in Chromium. Every spacing, inset and sizing property in the change is logical (`padding-inline`, `padding-block-start`, `block-size`, `max-inline-size`, `min-inline-size`). |
| 7 | **`.shell__main` padding stays as it is.** #106 confirms the page content already fits at 768px; this fix is the bar only. |
| 8 | **Verification is in the browser pane, so execution should be native** (in the session that has the Browser tools), not a subagent per task. It's two tasks, one SCSS file, and the proof is a set of measurements. |

## Global Constraints

- Standalone components, OnPush; no new component, service or dependency.
- Logical CSS properties for every spacing, inset and size (`client-i18n.md` rule 3). Decision 6 is the only physical-axis property.
- No `!important`; `::ng-deep` only under `:host` (the file's existing pattern).
- No hardcoded user-visible strings; this change adds none.
- No long dashes or ellipsis characters in source (`client\src\app\core\source-text.spec.ts` scans `.ts`, `.html` and `.scss`). The CSS value `text-overflow: ellipsis` is a keyword, not the `…` character.
- Admin is desktop-first; the student form is not touched.

**Commands** (client commands from `client\` in PowerShell, with the local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Dev servers: `preview_start {name: "api"}` (port 5080) and `preview_start {name: "client"}` (port 4200, proxies `/api`), from `.claude\launch.json`. The API runs against the compose Postgres, never `dl-postgres`: `docker stop dl-postgres`, then `docker compose up -d postgres` (memory). Sign in with the dev Administrator from `appsettings.Development.json`. Before measuring, `await document.fonts.ready`. Prefer `javascript_tool` measurements over screenshots, which hang on this app (memory).

## Review Focus

1. **A long sign-in email widens the bar again.** Expected: the email truncates with an ellipsis, and the page still doesn't scroll. Covered by task 1 step 6, which swaps in a 60-character email in the DOM and re-measures.
2. **The band just above the breakpoint (993-1024px), in English**, where the bar is a single row again with longer labels. Expected: no page scroll and no nav scroll. Covered by task 1 step 5, which measures 993, 1024 and 1100 in both languages and applies decision 4's fallback if needed.
3. **RTL mirroring of the two-row bar.** Expected: in Hebrew the logo sits at the right, the toggle and user button at the left, and the nav row starts at the right. Covered by task 1 step 5's `firstLinkAtInlineStart` and `logoAtInlineStart` checks in both languages.
4. **The user menu at tablet width.** Expected: it opens fully inside the viewport and shows the full email. Covered by task 1 step 7.
5. **A Teacher-role User's shorter nav (3 links)** in the two-row bar. Expected: the links sit at inline-start of row 2 and nothing stretches oddly. Covered by task 1 step 8, a spot check at 768px if a Teacher-role User exists in the dev database (skipped and noted if not).

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\features\admin-shell\admin-shell.component.scss` | Email cap, nav shrink/scroll, `@media (max-width: 992px)` two-row bar, conditional 993-1199px rule | 1 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-two-row-tablet-header.md](task-01-two-row-tablet-header.md) | Measure (RED), SCSS change, measure (GREEN) at 768 to 1280 in he and en, long email, user menu | ✅ own commit |
| 2 | [task-02-suites-and-pr.md](task-02-suites-and-pr.md) | Client suite and build, PR | ✅ PR |

The PR targets `82-users-and-roles` and says `Closes #106`. It won't auto-close on that base, so close #106 by hand after the merge.
