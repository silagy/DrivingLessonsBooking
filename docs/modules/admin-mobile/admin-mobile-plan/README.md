# Admin on a phone - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of the **Admin Mobile** design: the Administrator screens become usable on a phone (390px) without sideways scrolling. Each task has its own file. Execute in order.

**Goal:** Below 640px, in Hebrew (RTL) and in English, the admin shell is one 56px bar with a menu drawer, page headers stack, the Cars & teachers cards, the Weekly prep grid and the Publications history list follow the design, dialogs fit the screen, and no admin page scrolls sideways at 390px or 360px.

**Architecture:** Client only, mostly SCSS. One new layout breakpoint, `@media screen and (max-width: 639px)`, written in each component that changes (the design's "below 640px"). Where the phone layout needs different markup, the template renders both variants and CSS shows one (the existing `students__wide-only` / `students__compact-only` pattern), so there is no JavaScript breakpoint observer. New markup:
- the shell gets a menu button and a PrimeNG `p-drawer` holding the nav, the language toggle, the email and sign out;
- the week grid gets short day names and a compact window time;
- Weekly prep gets a sticky bottom publish bar;
- Publications history gets a card list next to its table.

Business rules, stores and API calls don't change.

**Tech Stack:** Angular 21 (standalone, OnPush, signals), PrimeNG 21 (`primeng/drawer`), Transloco, SCSS, Vitest under jsdom. Layout is measured in the browser pane (Claude Browser tools) against the dev API, because jsdom can't lay out CSS.

**Spec:** Claude Design file `Admin Mobile.html` from the user's export `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip`, copied to `Driving Lesson Mockup\Admin Mobile.html` in this branch. Frames: **01 App shell - menu open**, **02 Cars & teachers**, **03 Weekly prep**, **04 Publications history**, and **05 Responsive rules** ("What changes below 640px", rules 1 to 7). Today's phone rendering, the problem the design fixes, is in the same export (`uploads\pasted-*.png`): the nav tabs cut off, the history table squeezed into one-word lines, the grid running off screen, and the car card's assign button pushed outside the card. Rules: `.claude\rules\client-architecture.md`, `client-primeng.md`, `client-i18n.md`, `client-state.md`.

**Branch:** `admin-mobile`, from `origin/main` at `7f34fcf`. This plan and the design snapshot are committed on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR; auto-merge is off, so merge by hand with `--merge` once it's reviewed and green (memory).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Shell bar | One row at ≥ 1280px; two rows (nav on its own full-width, scrollable row) at `max-width: 1279px` (#106). No phone layout: at 390px the nav row shows 4 of 7 links and cuts the rest | `client\src\app\features\admin-shell\admin-shell.component.scss:183` |
| User button | `p-button.shell__user`: avatar, email (`<bdi class="shell__email">`, capped at 9rem), chevron; opens the `p-menu` popup (Change my password, Sign out) | `admin-shell.component.html` |
| Main padding | `.shell__main { padding: 1.625rem 2.25rem 2.25rem }` at every width | `admin-shell.component.scss` |
| Dialogs | Opened by `DialogService` with inline widths 28rem to 35rem (448 to 560px); nothing caps them on a phone. `client-primeng.md` says "On mobile widths the dialog goes full-screen (global override)", but no such override exists | `cars-and-teachers.page.ts:28`, `users.page.ts:33`, `students.page.ts:76`, `admin-shell.component.ts:33`; `client\src\styles\_global.scss` |
| Cars & teachers header | `.fleet__header` flex row (title block, then the two buttons at inline-end) | `features\teachers\ui\pages\cars-and-teachers\` |
| Car card | One flex row: icon, name/type, transmission pill, teacher chips + `app-assign-teachers-popover`, then actions ("Shared" tag, "Edit" text button, trash icon). Nothing wraps | `features\teachers\ui\components\car-card\` |
| Teacher card | Header row: avatar, name/email, "Edit" text button, trash icon. Grid `repeat(auto-fill, minmax(22rem, 1fr))` | `features\teachers\ui\components\teacher-card\`, `cars-and-teachers.page.scss` |
| Weekly prep | Header row (title block, then state tag + "Publish week..." link); selectors in a row; grid card `padding: 22px 24px`; each slot is a `button.slot-cell` 64px tall showing "פתוח"/"חסום"; footer row with the legend and the unavailable count | `features\week-schedules\ui\pages\weekly-prep\` |
| Week grid | Shared: `grid-template-columns: 118px repeat(6, 1fr)`, `gap: 8px`, full day names (`weekGrid.days.*`), window name + time (`07:00-12:00`, from `windowTimesOf`), cells `min-height: 64px`. Used by Weekly prep and the Publications dashboard | `client\src\app\shared\components\week-grid\`, `shared\dates\window-times.ts` |
| History | One `p-table` (week, teacher, window, status, last Excel, action). At 390px every cell wraps to one word per line | `features\publications\ui\pages\publications-history\` |
| Breakpoints | `client-primeng.md`: `992px` is the sizing breakpoint; a component may add its own layout breakpoint when measured content needs one, with the reason written in its plan | `.claude\rules\client-primeng.md:166-167` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **The phone breakpoint is `max-width: 639px`**, a layout breakpoint in each component that changes, as `client-primeng.md` allows. The design names 640px. 992px stays the sizing breakpoint (inputs are already 40px below it). 640 to 1279px keeps today's two-row tablet bar. Task 1 adds the 639px breakpoint to `client-primeng.md`'s list. |
| 2 | **Both variants live in the template; CSS picks one.** No `BreakpointObserver` or `matchMedia`. That matches `students.page` (`__wide-only` / `__compact-only`), keeps the components OnPush and pure, and needs no resize handling. A hidden variant is `display: none`, so it's out of the accessibility tree. |
| 3 | **The bar's avatar button keeps today's user menu** (Change my password, Sign out). On a phone it shows the avatar only (no email, no chevron). The drawer footer adds the language toggle, the email and Sign out, as frame 01 shows. Change my password stays reachable through the avatar; the design doesn't place it, and dropping it would remove a feature. |
| 4 | **The drawer opens from inline-start**: from the right in Hebrew, from the left in English (`position` from `LanguageService.isRtl()`). Frame 01 opens it from the right in RTL. PrimeNG's `position` is physical, so it's computed. It closes on a link tap, on the close button, on the scrim and on Escape (PrimeNG). |
| 5 | **Dialogs fit the phone, not full-screen.** A global `max-inline-size: calc(100vw - 2rem)` on `.p-dialog` at ≤ 639px caps the inline `width` without `!important` (max-width beats width). True full-screen would need `!important` over PrimeNG's inline width, which the rules ban. Task 2 rewrites the `client-primeng.md` line to match. |
| 6 | **Grid cells hide "פתוח"/"Open" visually, not from screen readers.** The design shows only "חסום". The open label gets a visually-hidden style at ≤ 639px, so the button keeps its accessible name. |
| 7 | **Short day names are new keys**, `weekGrid.daysShort.*` (`א׳ ... ו׳`, `Sun ... Fri`). The compact window time is computed: `compactWindowTime('07:00-12:00')` is `07-12`, and minutes stay when they aren't `:00` (`07:30-12:00` is `07:30-12`). Both are shown only at ≤ 639px; the desktop grid doesn't change. |
| 8 | **Inputs keep the global 40px**, not the design's 48px selects. Sizing lives in one place (`client-primeng.md`); the selects become full width. |
| 9 | **History cards reuse today's labels and actions.** The design labels a draft's link "המשך הכנה"; today's action is "צפייה בלוח" (View dashboard), and the Excel version stays the `v2` pill. Changing what a row's action does is out of scope. |
| 10 | **Car chips gain initials on the phone only**, from the shared `initials()` in `shared\text\initials.ts`, as frame 02 shows. Desktop chips don't change. |
| 11 | **Only the designed screens get bespoke layouts.** Dashboard, Students, Publications (dashboard) and Users inherit the shell, the dialogs and the week grid, and task 7 measures them at 390px. A page that still scrolls sideways is recorded in the PR and proposed as a follow-up story, not fixed here. Students already has a 992px compact layout. |
| 12 | **Verification is in the browser pane, so execution should be native** (the session with the Browser tools). Subagents can't measure layout. |

## As built (after execution)

Where the code differs from the tasks below, and why:

- **The drawer is its own component**, `features\admin-shell\ui\components\shell-drawer\` (`ShellDrawerComponent`: `open` model, `navigation`, `position`, `email`, `initials` inputs, `signOut` output). Inline in the shell, its styles pushed `admin-shell.component.scss` to 5.6 kB, over the 4 kB component style budget. The shell spec drives it unchanged.
- **The drawer is a named modal dialog and gives focus back.** It carries `role="dialog"`, `aria-modal` and the `shell.menu` label (`pt` root). When it closes from its X, its scrim or Escape, focus returns to the menu button.
- **Phone-only and wide-only buttons are hidden on the `p-button` host**, not on the inner button, so they don't leave an empty flex item that still takes a gap. The menu button sits before the user menu's `p-menu` host, which gives the design's 4px gap.
- **The grid has short window names on the phone too** (`weekGrid.windowsShort.*`; `Morn`, `Noon`, `Aft`, `Eve` in English). Bold "Afternoon" is 62px wide, more than the 52px label column.
- **The car card's actions row is `flex: 1 1 100%`** so its negative margins reach both edges of the card.
- **The drawer's email row puts the avatar first**, so in RTL it sits at the inline-start edge, as frame 01 shows.
- **Weekly prep cells show a short label on the phone.** English "Unavailable" is 77px wide in a 42px cell. Both full labels become visually hidden on the phone, and an unavailable cell shows `weekSchedules.cellUnavailableShort` (`חסום` / `Blocked`, `aria-hidden`). At 360px, English "Blocked" ellipsizes.
- **The dashboard's count cells got task 4's fallback**: a 1rem number, 48px cells, and the "requests" caption and "Unavailable" label visually hidden on the phone. They were 58px wide in 38px columns.
- **Header buttons, the dock link and the history card action use `block-size: 2.75rem`**, not `min-block-size`; PrimeNG's padding made them 46 to 48px.
- **Selects stay 42px tall** (decision 8), the one known `shortTargets` entry on Weekly prep.
- **Still overflowing at 390px, not designed yet:** `/students` (408px in Hebrew, table), `/publications` (453px in Hebrew, 522px in English, the action buttons), and `/users` (table clipped inside its card). Designed in frames 06 to 09 and fixed by [Students, Users and Publications on a phone](../admin-mobile-pages-plan/README.md).

## Global Constraints

- Standalone components, OnPush, signals for state; no new service, store or dependency (`primeng/drawer` ships with PrimeNG 21).
- Logical CSS properties for every spacing, inset and size (`client-i18n.md`). `overflow-x` is allowed (it names an axis), as in #106.
- No `!important`; `::ng-deep` only under `:host`, or bare and anchored to a unique `styleClass` for overlays (the drawer, dialogs).
- Tokens only, no hex values in SCSS (`client-primeng.md` anti-patterns).
- Every new user-visible string is a Transloco key in both `client\public\i18n\he.json` and `en.json`.
- No long dashes or ellipsis characters in source; `client\src\app\core\source-text.spec.ts` scans `.ts`, `.html` and `.scss`.
- Desktop (≥ 640px) must look as it does today. Each task re-measures 1280px after its change.
- The student form is not touched.

**Commands** (client commands from `client\` in PowerShell, with the local Angular CLI, because the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec, add `--include src/app/<path>.spec.ts`. If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Dev servers: `preview_start {name: "api"}` (port 5080) and `preview_start {name: "client"}` (port 4200, proxies `/api`), from `.claude\launch.json`. The API runs against the compose Postgres, never `dl-postgres`: `docker stop dl-postgres`, then `docker compose up -d postgres` (memory). Sign in with the dev Administrator from `src\DrivingLessons.Presentation.Web\appsettings.Development.json` (`Admin:Email`, `Admin:Password`). Before measuring, `await document.fonts.ready`. Prefer `javascript_tool` measurements over screenshots, which can hang on this app (memory). Phone sizes: `resize_window {width: 390, height: 844}` and `{width: 360, height: 780}`; reload after resizing. Switch language with `.lang__opt[lang="he"]` / `.lang__opt[lang="en"]`.

## The phone measurement

Tasks 1 to 7 run this with `javascript_tool` on the page under test. It's the shared GREEN check; each task adds its own element checks.

```javascript
await document.fonts.ready;
const visible = (element) => element.offsetParent !== null || getComputedStyle(element).position === 'fixed';
const box = (element) => element.getBoundingClientRect();
const targets = [...document.querySelectorAll('.shell__bar button, main button, main a[href], main .p-select')]
    .filter(visible)
    .filter((element) => !element.closest('.week-grid'))
    .map((element) => ({ element, rect: box(element) }))
    .filter(({ rect }) => rect.height < 44)
    .map(({ element, rect }) => `${element.className || element.tagName}:${Math.round(rect.height)}`);
const captions = '.week-grid, .week-grid-legend, app-publication-state-tag, [class*="pill"], [class*="avatar"], [class*="initials"], [class*="chip-tag"], .car__shared, .fleet__section';
const smallText = [...document.querySelectorAll('main *')]
    .filter(visible)
    .filter((element) => !element.closest(captions))
    .filter((element) => [...element.childNodes].some((node) => node.nodeType === Node.TEXT_NODE && node.textContent.trim()))
    .filter((element) => parseFloat(getComputedStyle(element).fontSize) < 13)
    .map((element) => `${element.className || element.tagName}:${getComputedStyle(element).fontSize}`);
const viewportWidth = screen.width;
({
    page: location.pathname,
    lang: document.documentElement.lang,
    viewportWidth,
    pageScrollWidth: document.documentElement.scrollWidth,
    pageOverflows: document.documentElement.scrollWidth > viewportWidth,
    clippedAtInlineEdge: [...document.querySelectorAll('main *')]
        .filter(visible)
        .filter((element) => { const rect = box(element); return rect.right > viewportWidth + 1 || rect.left < -1; })
        .slice(0, 5)
        .map((element) => element.className || element.tagName),
    shortTargets: targets.slice(0, 10),
    smallText: smallText.slice(0, 10),
});
```

It compares against `screen.width`, not `innerWidth`: under the browser pane's phone emulation an overflowing page widens `innerWidth` (748 on `/teachers` at 390 before task 3), which would hide the overflow.

Pass: `pageOverflows: false` and `clippedAtInlineEdge: []`. Two lists check rule 7 ("Body text at least 13px, tap targets at least 44px"); each task says which entries it must remove:
- `shortTargets` lists anything under 44px tall.
- `smallText` lists body text under 13px. It skips the captions the design itself sets smaller: grid, legend, tags, pills, avatars, initials and section eyebrows.

## Review Focus

1. **Long names and emails on a phone** (a 40-character teacher email, a long car name, a car shared by four teachers). Expected: text truncates or wraps inside the card; the page never scrolls sideways. Covered by task 3 step 7.
2. **A Teacher-role User on a phone.** Expected: the drawer lists only Weekly prep, Publications and History; Weekly prep shows the locked teacher field and no publish bar when the User can't publish. Covered by task 1's spec (`lists only the Teacher links`) and task 5 step 7.
3. **English (LTR) on a phone.** Expected: the logo at the left, the menu button at the right, the drawer sliding in from the left, the grid's Sunday column at the left. Covered by task 1's spec (`opens from the left in English`) and the English pass in every task's measurement step.
4. **The narrowest common phone, 360px.** Expected: the six grid columns still fit (cells about 37px wide), no page overflow anywhere. Covered by task 4 step 6 and task 7 step 2.
5. **The Publications dashboard's count cells in the narrower grid.** Expected: the shared grid change doesn't push the dashboard's counts out of their cells. Covered by task 4 step 6, which measures `/publications` at 390px.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\features\admin-shell\admin-shell.component.html` / `.scss` / `.ts` | Menu button, `p-drawer` with nav + language + email + sign out; `max-width: 639px` compact bar and main padding | 1 |
| `client\src\app\features\admin-shell\admin-shell.component.spec.ts` | New: drawer opens, lists the Role's links, closes on a link, signs out, side by language | 1 |
| `client\src\app\features\admin-shell\ui\components\shell-drawer\*` | As built: the drawer, extracted from the shell for the style budget | 7 |
| `client\src\app\features\publications\ui\components\slot-count-cell\slot-count-cell.component.scss` | As built: task 4's fallback for the dashboard's count cells | 4 |
| `client\public\i18n\he.json`, `en.json` | `shell.openMenu`, `shell.closeMenu` (1); `weekGrid.daysShort.*` (4); `teachers.editTeacherAria` (3) | 1, 3, 4 |
| `.claude\rules\client-primeng.md` | 639px layout breakpoint (1); dialogs fit the phone (2) | 1, 2 |
| `client\src\styles\_global.scss` | `.p-dialog` capped at ≤ 639px | 2 |
| `client\src\app\features\teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.scss` | Stacked header, 2-column action buttons, one-column teacher grid | 3 |
| `client\src\app\features\teachers\ui\components\car-card\*` | Three-row card, chip initials, icon-only edit | 3 |
| `client\src\app\features\teachers\ui\components\teacher-card\*` | Icon-only edit and delete, 44px | 3 |
| `client\src\app\features\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.scss` | 44px trigger on the phone | 3 |
| `client\src\app\shared\dates\window-times.ts` + `.spec.ts` | `compactWindowTime()` | 4 |
| `client\src\app\shared\components\week-grid\*` | Short day names, compact time, 52px label column, 48px cells at ≤ 639px | 4 |
| `client\src\app\features\week-schedules\ui\pages\weekly-prep\*` | Stacked header and selectors, visually-hidden open label, sticky publish bar | 5 |
| `client\src\app\features\publications\ui\pages\publications-history\*` + new `.spec.ts` | Card list at ≤ 639px | 6 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-shell-drawer.md](task-01-shell-drawer.md) | Compact bar + menu drawer, spec, measured | ✅ own commit |
| 2 | [task-02-dialogs-fit-phone.md](task-02-dialogs-fit-phone.md) | Global dialog cap, measured on four dialogs | ✅ own commit |
| 3 | [task-03-cars-and-teachers.md](task-03-cars-and-teachers.md) | Header, car cards, teacher cards | ✅ own commit |
| 4 | [task-04-compact-week-grid.md](task-04-compact-week-grid.md) | `compactWindowTime`, short days, grid sizing | ✅ own commit |
| 5 | [task-05-weekly-prep.md](task-05-weekly-prep.md) | Header, selectors, cells, sticky publish bar | ✅ own commit |
| 6 | [task-06-history-cards.md](task-06-history-cards.md) | Card list, spec | ✅ own commit |
| 7 | [task-07-sweep-suites-and-pr.md](task-07-sweep-suites-and-pr.md) | All admin pages at 390/360 in he/en, suites, build, PR | ✅ PR |
