# Students, Users and Publications on a phone - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Follow-up to [Admin on a phone](../admin-mobile-plan/README.md). That plan made the shell, Cars & teachers, Weekly prep and Publications history phone-friendly, and recorded three Administrator pages that still overflow at 390px. This plan gives them their phone layouts. Each task has its own file. Execute in order.

**Goal:** Below 640px, in Hebrew (RTL) and in English, `/students` and `/users` show their rows as cards, the Publications dashboard's header, stats, share link and grid card follow the design, and none of the three pages scrolls sideways or clips at 390px or 360px. Desktop (≥ 640px) doesn't change.

**Architecture:** Client only. The conventions are the parent plan's: one layout breakpoint, `@media screen and (max-width: 639px)`; both variants in the template with CSS picking one (no `BreakpointObserver`); phone-only and wide-only `p-button`s hidden on the host. New markup:
- `StudentCardComponent` and `UserCardComponent`, one card per row, next to each page's `p-table`. They're components, not inline markup, because `students.page.scss` is already 6.68 kB (budget: 4 kB warning, 8 kB error) and the cards add about 2 kB of styles.
- The dashboard gets a phone-only "Extend deadline" next to the window status.
- One new key, `students.importRosterShort`.

Business rules, stores and API calls don't change.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), PrimeNG 21, Transloco, SCSS, Vitest under jsdom. Layout is measured in the browser pane against the dev API, because jsdom can't lay out CSS.

**Spec:** Claude Design file `Admin Mobile.html`, from the user's export `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` (2026-10-17), copied over `Driving Lesson Mockup\Admin Mobile.html` in this branch. New frames: **06 Students**, **06b Students - no match**, **07 Users**, **07b Users - only you**, **08 Publications - Open**, **08b Publications - Closed header**, **08 English LTR at 360px**, and **09 Rules for 06 to 08** (rules 8 to 18). Rules 1 to 7 (frame 05) still apply. Rules: `.claude\rules\client-architecture.md`, `client-primeng.md`, `client-i18n.md`, `client-state.md`.

**Branch:** `admin-mobile-more-pages`, from the local `admin-mobile` at `82895ce`. `admin-mobile` isn't merged or pushed yet, so this branch carries its commits. The PR waits for `admin-mobile` to merge into `main`; then this branch is rebased or merged onto `main` and its PR targets `main`.

## Current State (measured at 390 × 844, Hebrew, dev data)

| Page | Today | File |
|------|-------|------|
| `/students` | Page `scrollWidth` 408. The `p-table` is 602px wide; below 992px a compact table already merges ID and phone under the name and car under the teacher, but it still doesn't fit. Row ⋮ buttons are 40px. Filters already stack below 992px | `features\students\ui\pages\students\` |
| `/users` | Page fits (390), but the `p-table` is 714px inside a 358px card (`overflow: hidden` on the card, `overflow-x: auto` on the table): Role, Teacher, Status and the ⋮ are cut off. ⋮ buttons are 40px | `features\users\ui\pages\users\` |
| `/publications` | Page `scrollWidth` 453 in Hebrew (Closed week), 522 in English. `.dashboard__header` is one flex row; the actions sit beside the title block. The inline week select is 20px tall. Grid cells are 38px wide at 390 (33px at 360, per the parent plan's sweep); the grid card keeps `padding: 22px 24px` | `features\publications\ui\pages\publications-dashboard\` |
| Stat chips | `display: flex; align-items: baseline`, value 1.5rem, label 0.78rem (12.5px) | `features\publications\ui\components\publication-stat-chip\` |
| Share link | Value `nowrap` + ellipsis, title 0.78rem, hint 0.72rem, copy `p-button size="small"` | `features\publications\ui\components\share-link-box\` |
| Style budget | `students.page.scss` 6.68 kB (warning already); other pages under 4 kB | `client\angular.json` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Cards are components.** `StudentCardComponent` (`features\students\ui\components\student-card\`) and `UserCardComponent` (`features\users\ui\components\user-card\`). Inputs are the row and `disabled`; the ⋮ emits the click `Event` (`actionsRequested`) so the page opens its existing row menu with `onRowActions($event, row)`. Restore emits `restoreRequested`. The pages keep the menus, dialogs and store calls. |
| 2 | **Cards follow rules 8 to 12**: avatar, name, tags in the top row, status tag at its end, then a 44px ⋮; details as a `dl`; a muted card (Inactive Student, Deleted User) loses its shadow and fades avatar, name and details to 55%, but not the status tag or the action; Restore is a full-width 44px text row at the bottom. |
| 3 | **Labels stay as today** (rule 18). The `dt`s reuse `students.table.*` and `users.table.*` (so "תעודת זהות", not the design's "ת״ז", and "מורה מקושר"). The one phone-only label is `students.importRosterShort`: "ייבוא רשימה" / "Import Roster". |
| 4 | **The phone list isn't inside the white table card.** At ≤ 639px the page's card section drops its background, border and shadow while it holds the list (`students__card--list`, `users__card--list`); loading, error and empty states keep the card look. The count line is centered, 13px. |
| 5 | **No-match on a phone** (frame 06b) is the same empty state as the table's, rendered from one `ng-template` in both places; its Clear filters button is full width, 44px. |
| 6 | **The status segmented control is 44px** (rule 7), not the design's 38px; equal thirds (rule 13). Selects keep 42px (parent decision 8). |
| 7 | **Users "only you"** (frame 07b): on a phone the header's Add User is hidden while the empty state shows its own. Your own card stays above the empty state, as your row does on desktop; frame 07b leaves it out, but hiding a row on one width only would make the two layouts disagree. |
| 8 | **Dashboard title on a phone** (frames 08, 08b): the teacher select on its own 44px line at 1.5rem, then "Week 42" (1.0625rem) with the state tag right after it. The title row becomes a grid and the `h2` a subgrid, so the tag (outside the `h2`, as today) lines up after the week title without moving it into the heading. |
| 9 | **The week select becomes a 44px bordered pill** on its own line; the window status goes on the next line with the phone-only "Extend deadline" text button at its end (rule 15). The wide-only Extend stays in `.dashboard__actions`. The remaining actions take the 2-column row; a single action (Publish week...) spans both columns; no actions, no row. |
| 10 | **Stats are 3 equal columns** (rule 16): value over label, label 13px and allowed to wrap; the "data as of" stamp on its own line. The design's "· refresh to update" is new copy and isn't added. |
| 11 | **Share link on a phone:** the link wraps (`overflow-wrap: anywhere`) in a 44px box, the copy button is 44px, title and hint 13px. |
| 12 | **Count grid** (rule 17): the dashboard's grid cards get `padding: 0.75rem 0.625rem`, and the dashboard narrows the shared grid to a `2.875rem` label column and 4px gaps through `:host ::ng-deep`, so Weekly prep's grid is untouched. Short window names must fit 46px (measured in task 3). |
| 13 | **Flags on a card are 13px** (rule 11): the card sets the `app-car-flag` / `app-deleted-flag` font size; the flag components don't change. |
| 14 | **Verification is in the browser pane, so execution is native.** |

## Global Constraints

The parent plan's apply unchanged: standalone, OnPush, signals; logical CSS properties; no `!important`; `::ng-deep` only under `:host`; tokens only, no hex; every new string a key in `he.json` and `en.json`; no long dashes or ellipsis characters in source (`source-text.spec.ts`); desktop re-measured at 1280px after each task.

Commands (from `client\`, PowerShell), the dev servers and sign-in are as in the parent README, with one local difference: Windows reserves ports 5013 to 5112 and 5413 to 5512 on this machine, so the API runs on 5780 against the compose Postgres on 55432, and the client on 4210 with a scratch proxy to 5780 (scratch `launch.json` entries, not committed).

**The phone measurement** is the parent README's script (compare against `screen.width`). Pass: `pageOverflows: false`, `clippedAtInlineEdge: []`, `smallText: []`, and `shortTargets` holding only the known 42px selects.

## Review Focus

1. **A long name, a long email, a flagged car** on a card at 360px. Expected: they wrap inside the card; the page never scrolls sideways. Task 1 step 7, task 2 step 7.
2. **Muted cards still act.** An Inactive Student's ⋮ and a Deleted User's Restore are full strength and work. Task 1 step 7, task 2 step 7.
3. **The dashboard in every state** (Draft, Published, Open, Closed, no publication) and for a Teacher-role User (no teacher select). Task 3 step 7.
4. **English at 360px**: "Extend deadline", "Reopen window...", "Download Excel" on one line in half-width buttons. Task 3 step 7, task 4.
5. **Desktop unchanged** on all three pages at 1280px. Every task's last measuring step.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\features\students\ui\components\student-card\*` + `.spec.ts` | New card | 1 |
| `client\src\app\features\students\ui\pages\students\students.page.html` / `.scss` / `.ts` | Card list, short import label, no-match template, phone header and filters | 1 |
| `client\src\app\features\users\ui\components\user-card\*` + `.spec.ts` | New card | 2 |
| `client\src\app\features\users\ui\pages\users\users.page.html` / `.scss` | Card list, full-width header action, hidden when only you | 2 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\*` | Phone header, phone Extend, stats grid, grid card | 3 |
| `client\src\app\features\publications\ui\components\publication-stat-chip\*.scss` | Stacked chip on a phone | 3 |
| `client\src\app\features\publications\ui\components\share-link-box\*.scss` | Wrapping link, 44px copy | 3 |
| `client\public\i18n\he.json`, `en.json` | `students.importRosterShort` | 1 |
| `docs\modules\admin-mobile\admin-mobile-plan\README.md` | "Still overflowing" line points here | 4 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-students-cards.md](task-01-students-cards.md) | Student card, page switch, header, filters, no-match | ✅ own commit |
| 2 | [task-02-users-cards.md](task-02-users-cards.md) | User card, page switch, header | ✅ own commit |
| 3 | [task-03-publications-dashboard.md](task-03-publications-dashboard.md) | Header, Extend, stats, share link, grid card | ✅ own commit |
| 4 | [task-04-sweep-and-suites.md](task-04-sweep-and-suites.md) | Three pages at 390/360 in he/en, suites, build | ✅ own commit |
