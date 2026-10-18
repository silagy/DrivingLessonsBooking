# Slot requests detail and the phone rules, finished - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Third plan in the `admin-mobile` module. [Admin on a phone](../admin-mobile-plan/README.md) built frames 01 to 05. [Students, Users and Publications on a phone](../admin-mobile-pages-plan/README.md) built frames 06 to 09. Both are merged (PR #126). This plan builds what the latest `Admin Mobile.html` export and the 2026-10-18 request add on top of them, and closes the gaps between the request's rules and what was built. Each task has its own file. Execute in order.

**Goal:**
- In the Publications dashboard grid, an Administrator or Teacher taps (or clicks) a count with 1 or more requests and sees who requested that slot, in submission order, with which of their choices it was. On a phone this is a bottom sheet; on desktop it's a centred dialog.
- The "no lessons" legend reads "אין שיעורים" / "No lessons" everywhere.
- A phone card's ⋮ menu follows rule 4, and card flags follow rule 7.
- Every admin page passes the phone checks at 360px and 390px in Hebrew and English.

**Architecture:**
- **Backend:** one read endpoint, `GET api/publications/{id}/slots/{slotId}/requests?teacherId=`. It goes through a new `FindSlotRequestsInteractor` and a new `ISubmissionQueries.FindSlotRequestsAsync`, using the same Teacher-role scoping (`MayReach`) and the same submission filter (`TeacherSubmissions`) as today's counts. That way the list always matches the count.
- **Client:**
  - `PublicationsStore` holds the opened slot and loads its requests with a `resource`.
  - `SlotCountCellComponent` becomes a button when the slot can be opened.
  - A `SlotRequestsDialog` is opened through the existing `DialogService`. Global styles under the 639px breakpoint turn that same dialog into a bottom sheet.
  - A small `SheetDragCloseDirective` closes the sheet when its handle is dragged down.
  - So no JavaScript breakpoint observer is needed, as in the earlier plans.

**Tech Stack:** .NET (ASP.NET Core, EF Core, Npgsql), MSTest + Shouldly + FakeItEasy, Angular 21 (standalone, zoneless, OnPush, signals), PrimeNG 21 (`primeng/dynamicdialog`, `primeng/menu`), Transloco, SCSS, Vitest under jsdom. Layout is measured in the browser pane (Claude Browser tools) against the dev API.

**Spec:**
- The user's request in chat on 2026-10-18 ("Implement the phone layout for the DriveCal Administrator pages ... GLOBAL RULES 1-9 ... PER PAGE ... A. NEW FEATURE: SLOT REQUESTS DETAIL ... B. COPY CHANGE").
- The Claude Design file `Admin Mobile.html` from the user's export `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` (2026-10-18 07:01), copied over `Driving Lesson Mockup\Admin Mobile.html` in this branch. Compared with the copy the earlier plans used, the export changes only:
  - the new frame **08c Slot requests sheet**, plus its rule in frame 09 ("Slot detail");
  - the tappable and selected cell styles (`.c.n:not(.z)`, `.c.n.sel`) and the sheet styles (`.sheet`, `.grab`, `.shd`, `.sl2`, `.sfoot`);
  - the legend copy: "אין שיעורים" / "No lessons", and the dashboard's "מספר בקשות - הקישו לרשימת התלמידים".
- Rules: `.claude\rules\client-architecture.md`, `client-state.md`, `client-primeng.md`, `client-i18n.md`, `code-style.md`, `ddd-architecture.md`, `api-guidelines.md`, `domain-testing.md`.

**Branch:** `admin-mobile-slot-requests`, from `origin/main` at `5806189` (no upstream set; push with `-u origin admin-mobile-slot-requests`). Commit this plan and the design snapshot on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR. Auto-merge is off, so merge by hand with `--merge` once the PR is reviewed and green.

## Current State

What the request asks for that is **already built** (merged in PR #126). Task 7 re-measures it and changes nothing here unless it finds drift:

| Request item | Built in | File |
|--------------|----------|------|
| Rule 1: 56px bar, logo, avatar, menu button, drawer with 48px nav items, active item tinted with gradient bar, language toggle, email, Sign out. The drawer opens from the right in Hebrew and from the left in English | admin-mobile-plan task 1 | `features\admin-shell\ui\components\shell-drawer\` |
| Rule 2: stacked header, 2-column 44px actions, a single action full width | both plans | each page's `.scss` |
| Rules 3, 5, 6: cards, muted Inactive / Deleted cards, Restore as a full-width row | admin-mobile-pages-plan tasks 1, 2 | `student-card\`, `user-card\` |
| Rule 8: LTR values in `<bdi dir="ltr">`, wrapping | both plans | cards, `share-link-box\` |
| Cars & teachers, Weekly prep, History, Students (filters, no-match, short Import, "חדש" tag and tint, count line), Users ("You" tag, role tags, only-you), Publications dashboard header, stats, share link, count grid | both plans | see their READMEs |

What is **not built**:

| Gap | Today | File |
|-----|-------|------|
| A: slot requests detail | Count cells are a plain `div`; no API lists who requested a slot. `ISubmissionQueries.GetSlotRequestDetailsAsync` serves the Excel: all slots, with national ID and phone, and target count rather than pick count | `slot-count-cell\`, `SubmissionQueries.cs` |
| A: legend hint | The dashboard uses the shared legend, whose first item says "פתוח" / "Open" | `shared\components\week-grid-legend\` |
| B: copy | `weekGrid.legend.outsideGrid`: "מחוץ לרשת (שישי אחה״צ/ערב, שבת)" / "Outside grid (Fri PM, Saturday)". This is the only place the copy appears (`src\` and docs have none) | `client\public\i18n\he.json:333`, `en.json:333` |
| Rule 4: ⋮ menu | One `p-menu` per page serves both the table and the cards. A car that isn't the Teacher's turns "Change car" itself into the fix item (third, muted background, red icon, "תיקון" badge). Destructive items use `--p-red-600`. Items are PrimeNG's default height (under 44px). The design (frame 06, `.pop`) shows "תיקון הרכב" first, tinted sky (`#EEF3FF` / `--skyD`), then a divider, the usual items, a divider and Plum "סימון כלא פעיל" | `students.page.ts:145-220`, `users.page.ts:75-100` |
| Rule 7: flags | `app-car-flag` and `app-deleted-flag` are 12.5px, `nowrap`, red (car) or gray (deleted), with a dotted underline, on cards too | `features\students\ui\components\car-flag\`, `deleted-flag\` |
| Plum | No Plum colour in the theme | `theme\app-preset.ts`, `styles\_tokens.scss` |

Backend facts the tasks rely on:
- A Submission has `SubmittedAtUtc` (set once), `RevisedAtUtc`, and `SlotRequests`, each with a `SlotId` and a 1-based `Rank`.
- There is one Submission per Student per Publication.
- A Submission belongs to a Teacher through `WeekScheduleId`.
- Counts don't filter Inactive Students and ignore whether a slot is Unavailable. Requests stay on a slot that is blocked later.
- There are no database-level query tests in the repo; interactor tests fake the `I*Queries`.

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **This plan builds only the gaps.** Everything in the Current State's first table stays as built. Task 7 measures it against the request's rules and fixes only real drift. It records each fix in the As built section. |
| 2 | **Endpoint: `GET api/publications/{id:guid}/slots/{slotId:guid}/requests?teacherId={guid}`** on `PublicationQueryController`. It returns `IReadOnlyList<ItemForFindSlotRequestsResponse>` (`StudentId`, `StudentName`, `Rank`, `PickCount`). It is a sub-resource of the Publication, scoped by Teacher like `/dashboard` and `/excel`. |
| 3 | **The list matches the count exactly.** It uses the same `TeacherSubmissions(publicationId, teacherId)` filter, with no Active filter, so an Inactive Student who requested the slot is listed as they are counted. `PickCount` is the number of the Student's Slot Requests, not their target session count ("choice 2 of 3" means the 2nd of the 3 they picked). |
| 4 | **Submission order is first submission:** `SubmittedAtUtc` ascending, then Student name, then Student id, so ties are stable. Revising a Submission doesn't move a Student down the list. |
| 5 | **Scoping, not existence checks.** A Teacher asking about another Teacher's slot gets `PublicationNotFoundException` (404), as `/dashboard` does. A slot id that isn't in that Teacher's grid returns an empty list; it reveals nothing. No extra query checks that the Publication exists. |
| 6 | **A cell opens only when the slot is Open and its count is 1 or more.** It then renders as a `<button type="button">`. The `aria-label` gives the count, day and window, and `aria-haspopup="dialog"` is set. While its list shows, the cell gets `aria-expanded="true"` and the selected style. A 0 cell and an Unavailable cell stay the `div` they are today, even when an Unavailable slot still has requests (the request: "blocked cells do nothing"). |
| 7 | **One dialog component, two shapes.** `DialogService.open(SlotRequestsDialog, { showHeader: false, styleClass: 'slot-requests-dialog', maskStyleClass: 'slot-requests-mask', ... })`. The component draws its own header (title, subtitle, ✕), its list and its footer Close. In `_global.scss`, at `max-width: 639px`: <ul><li>the mask aligns to the bottom;</li><li>the dialog takes the full width (`min-inline-size: 100%` beats the inline `width`, no `!important`);</li><li>it gets 20px top corners, `max-block-size: 78dvh` and a scrolling list;</li><li>the grab handle shows.</li></ul> At 640px and up it's a 28rem centred dialog with no handle. |
| 8 | **Drag to close is a directive on the sheet's top (handle + header):** `SheetDragCloseDirective` (`[appSheetDragClose]`, `dismissed` output). Pointer events move the closest `.p-dialog` down with `translate`. Letting go at 80px or more emits `dismissed`; less springs back. Upward drags are ignored. It sets `touch-action: none` only on that region, so the list still scrolls. |
| 9 | **The selected cell comes from the store:** `PublicationsStore.openedSlot()`. It's set by `openSlotRequests(slotId)` and cleared by `closeSlotRequests()` when the dialog closes, however it closes (✕, Close, scrim, Escape, drag). It also resets when the Teacher or week changes. When the dialog closes, focus goes back to the cell that opened it: the cell emits its button element and the page keeps it. |
| 10 | **The subtitle counts what the list shows.** Once loaded, "N תלמידים ביקשו" uses the list length, not the cell. If a Student revised between page load and the tap, the sheet tells the truth. While the list loads, it shows the cell's count. An empty result shows `publications.slotRequests.none` ("refresh the page"). |
| 11 | **Title and time.** The title is `publications.slotRequests.title`, with the day from `weekGrid.daysShort.*` and the date from a new shared `gridDayDate()`, extracted from `WeekGridComponent.dateFor` so the grid head and the sheet agree. The window comes from `weekGrid.windows.*`. For 2026-10-21, Afternoon, that gives "יום ד׳ 21.10 · אחה״צ" / "Wed 10/21 · Afternoon". The time is `store.windowTimes()[window]` in a `<bdi dir="ltr">`. Singular and plural are two keys (`requestedByOne` / `requestedByMany`), the project's pattern (`footerOne`). |
| 12 | **The legend's first label becomes an input.** It is `WeekGridLegendComponent.firstLabelKey`, default `weekGrid.legend.open`. The dashboard passes `publications.dashboard.legendRequests`: "מספר בקשות - הקישו לרשימת התלמידים" / "Requests - tap or click a count to see the students". Weekly prep keeps "פתוח". |
| 13 | **Copy change B keeps the key's meaning and renames it:** `weekGrid.legend.outsideGrid` becomes `weekGrid.legend.noLessons`, "אין שיעורים (שישי אחה״צ/ערב, שבת)" / "No lessons (Fri PM, Saturday)". Only the phrase changes; the parenthesis stays, so Saturday is still explained. `weekGrid.legend.unavailable` ("לא זמין" / "Unavailable") doesn't change. |
| 14 | **Rule 4 applies to the card's ⋮ only.** The cards show only below 640px, and the table only at 640px and up, so the card emits to a new `onCardActions(event, row)` that builds the card menu. The desktop table keeps `onRowActions` and today's menu ("Desktop layout stays the same"). The card menu: <ul><li>"תיקון הרכב" / "Fix the car" first (sky tint, runs Change car) when the car isn't the Teacher's, then a divider;</li><li>Edit, Change teacher, Change car (plain);</li><li>a divider, then Deactivate in Plum.</li></ul> The Users card menu is today's items with Delete in Plum. Both `p-menu`s get `styleClass="card-menu"`; its items are 44px below 640px. |
| 15 | **Plum is a theme colour:** `primitive.plum.500 = '#BA0081'` (design `--plum`), token `--app-plum`. The design's other hex values map to the nearest existing token, as the earlier plans did ("tokens only"):<ul><li>tappable border `#C9D6F5` becomes `--p-steel-200` (`#C3CFE5`);</li><li>selected fill `#EEF3FF` and the fix item's tint become `--p-sky-50` (`#EAF1FF`);</li><li>selected text `--skyD` becomes `--p-sky-700`;</li><li>the "חדש" tint `#F2F7FF` stays `--p-sky-50`, as built.</li></ul> |
| 16 | **Card flags (rule 7) through custom properties.** `app-car-flag` and `app-deleted-flag` read `--flag-color`, `--flag-font-size`, `--flag-white-space` and `--flag-underline`, with today's values as defaults, so the desktop table doesn't change. `StudentCardComponent` sets them to Plum, `0.8125rem`, `normal` and `transparent`. That's the design's `.flag`: Plum, 13px, weight 500, no underline, wrapping. |
| 17 | **Verification is in the browser pane, so execution should be native** (the session with the Browser tools). Subagents can't measure layout. Backend tasks could go to a subagent, but they're small. |

## Global Constraints

- **Client:**
  - Standalone components, OnPush, signals for state; no new store, no new dependency, no `BreakpointObserver` or `matchMedia`.
  - The phone breakpoint is `@media screen and (max-width: 639px)`; 640px and up must look as it does today, apart from items A and B.
  - Logical CSS properties for spacing, insets and sizes (`client-i18n.md`).
  - No `!important`. `::ng-deep` only under `:host`, or bare and anchored to a unique `styleClass` for overlays.
  - Tokens only, no hex in component SCSS. The one new hex is the Plum primitive in `app-preset.ts`.
  - Every new user-visible string is a Transloco key in both `client\public\i18n\he.json` and `en.json`. No long dashes or ellipsis characters in source (`client\src\app\core\source-text.spec.ts`).
  - Component style budget: 4 kB warning, 8 kB error. `students.page.scss` is at 7.11 kB, so the card menu styles go in `_global.scss`, not in that file.
- **Backend:**
  - Raw `Guid` only at the controller, the interactor's `ExecuteAsync` and the `I*Queries` boundary (as `GetSlotRequestCountsAsync` does).
  - No comments; tests use `//given //when //then`.
  - Student names are PII: never logged.
- **The phone rules the request fixes:** below 640px, no horizontal page scroll at 360px and 390px; body text ≥ 13px; tap targets ≥ 44px; nothing `nowrap` except buttons and tags; LTR values in isolated `dir="ltr"`; long emails and links wrap with `overflow-wrap: anywhere`, never truncate.

**Commands.** Run client commands from `client\` in PowerShell, with the local Angular CLI:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

- To run one spec, add `--include src/app/<path>.spec.ts`.
- If `client\node_modules` is missing, install from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.
- Backend, from the repo root: `dotnet test` (or `dotnet test --filter FullyQualifiedName~FindSlotRequestsInteractorTest`).

**Dev servers and sign-in** are as in [admin-mobile-pages-plan](../admin-mobile-pages-plan/README.md#global-constraints):
- The API runs against the compose Postgres, never `dl-postgres`.
- Ports 5013 to 5112 may be reserved on this machine. If `preview_start {name: "api"}` fails to bind, use the earlier plan's 5780 / 4210 scratch setup.
- Sign in with the dev Administrator from `appsettings.Development.json`.
- Before measuring, `await document.fonts.ready`.
- Prefer `javascript_tool` measurements over screenshots.
- Phone sizes: `resize_window {width: 390, height: 844}` and `{width: 360, height: 780}`; reload after resizing.
- Switch language with `.lang__opt[lang="he"]` / `.lang__opt[lang="en"]`.

**The phone measurement** is the script in [admin-mobile-plan § The phone measurement](../admin-mobile-plan/README.md#the-phone-measurement), with the caption list from the pages plan's As built (`.p-tag, app-transmission-tag` added). Pass:
- `pageOverflows: false` and `clippedAtInlineEdge: []`;
- `smallText: []`;
- `shortTargets` holding only the known 42px selects (parent decision 8).

## Review Focus

1. **A count that changed after the page loaded.** A Student revised their picks between page load and the tap. Expected: the sheet lists who requests the slot now, and its subtitle counts that list, not the stale cell. Task 5, `counts the students it lists, not the cell`.
2. **Two cells tapped quickly.** The first list arrives after the second was opened. Expected: the sheet shows the second slot's students only. Task 3, `shows the requests of the last slot opened`.
3. **A long Hebrew name at 360px** ("אלכסנדרה בן-דוד רוזנטל-אברהמי", frame 08c). Expected: the name wraps, "בחירה 2 מ-3" stays on one line at the row's end, and the sheet never scrolls sideways. Task 5 step 8.
4. **A blocked slot that still has requests** (marked Unavailable after Students picked it). Expected: not tappable; it shows "לא זמין" as today. Task 4, `does nothing for a blocked slot, even with requests`.
5. **Keyboard and screen reader.** Expected:
   - Tab reaches a tappable cell, and its name says the count, day and window.
   - Enter opens the list, and Escape closes it.
   - Focus returns to the same cell.
   
   Task 4, `names the slot for screen readers`; task 5, `gives focus back to the cell it opened from`.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\public\i18n\he.json`, `en.json` | `weekGrid.legend.noLessons` (replaces `outsideGrid`), `publications.dashboard.legendRequests` (1); `publications.slotRequests.*` (4, 5); `students.actions.fixCar` (6) | 1, 4, 5, 6 |
| `client\src\app\shared\components\week-grid-legend\*` + new `.spec.ts` | `firstLabelKey` input, `noLessons` key | 1 |
| `src\DrivingLessons.Application\Queries\FindSlotRequests\FindSlotRequestsInteractor.cs`, `ItemForFindSlotRequestsResponse.cs` | New query | 2 |
| `src\DrivingLessons.Application\Queries\ISubmissionQueries.cs`, `Infrastructure\EntityFramework\Queries\SubmissionQueries.cs` | `FindSlotRequestsAsync` | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs`, `Presentation.Web\Controllers\Publication\PublicationQueryController.cs` | Registration, endpoint | 2 |
| `tests\DrivingLessons.Application.Test\Queries\FindSlotRequestsInteractorTest.cs` | New | 2 |
| `client\src\app\features\publications\data\item-for-find-slot-requests.response.ts`, `publications-api.service.ts` | Response type, `findSlotRequests` | 3 |
| `client\src\app\features\publications\state\publications.store.ts` + `.spec.ts` | Opened slot, requests resource | 3 |
| `client\src\app\features\publications\ui\components\slot-count-cell\*` + new `.spec.ts` | Button when openable, selected state | 4 |
| `client\src\app\shared\dates\grid-date.ts` + `.spec.ts`; `shared\components\week-grid\week-grid.component.ts` | `gridDayDate()`, used by the grid | 5 |
| `client\src\app\shared\directives\sheet-drag-close.directive.ts` + `.spec.ts` | New | 5 |
| `client\src\app\features\publications\ui\dialogs\slot-requests\*` + `.spec.ts` | New dialog | 5 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\*` | Cell wiring, open and close the dialog, legend key | 1, 5 |
| `client\src\styles\_global.scss` | Sheet shape below 640px (5); `card-menu` items and tints (6) | 5, 6 |
| `client\src\app\theme\app-preset.ts`, `client\src\styles\_tokens.scss` | `plum.500`, `--app-plum` | 6 |
| `client\src\app\features\students\ui\pages\students\students.page.{ts,html}`, `users\ui\pages\users\users.page.{ts,html}` | `onCardActions`, `card-menu` | 6 |
| `client\src\app\features\students\ui\pages\students\student-card-actions.ts` + `.spec.ts` | Card menu order, pure | 6 |
| `client\src\app\features\students\ui\components\car-flag\*.scss`, `deleted-flag\*.scss`, `student-card\*.scss` | Flag custom properties, card values | 6 |
| `docs\requirements.md`, this README | Dashboard detail line (§ Dashboard); As built | 7 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 0 | - | Commit this plan and `Driving Lesson Mockup\Admin Mobile.html` on the branch | ✅ own commit |
| 1 | [task-01-legend-copy.md](task-01-legend-copy.md) | "No lessons", legend first-label input, dashboard hint | ✅ own commit |
| 2 | [task-02-slot-requests-api.md](task-02-slot-requests-api.md) | Query, interactor, endpoint, tests, smoke | ✅ own commit |
| 3 | [task-03-slot-requests-store.md](task-03-slot-requests-store.md) | Client response, API call, store state | ✅ own commit |
| 4 | [task-04-tappable-count-cells.md](task-04-tappable-count-cells.md) | Count cell as a button, selected state | ✅ own commit |
| 5 | [task-05-slot-requests-sheet.md](task-05-slot-requests-sheet.md) | Dialog / bottom sheet, drag to close, page wiring | ✅ own commit |
| 6 | [task-06-card-menus-and-flags.md](task-06-card-menus-and-flags.md) | Plum, card ⋮ menus (rule 4), card flags (rule 7) | ✅ own commit |
| 7 | [task-07-sweep-docs-and-pr.md](task-07-sweep-docs-and-pr.md) | Every admin page at 360/390 in he/en against rules 1-9, desktop A/B, suites, docs, PR | ✅ PR |

## Execution

Native (`superpowers:executing-plans`) is recommended. Tasks 4 to 7 end in browser-pane measurements, which only this session can take (decision 17).

## As built (after execution)

Where the code differs from the tasks below, and why:

- **The task-2 authorization test needed a line.** `ControllerAuthorizationTest` pins every endpoint's policy, so `PublicationQueryController.FindSlotRequests` joined its Teacher-or-Administrator list. The plan missed it.
- **The dialog doesn't inject the store.** `client-state.md` says dialogs never inject stores, so the page passes the store's signals in `DynamicDialogConfig.data` (`SlotRequestsDialogData`), the way `EditStudentDialog` gets its signals.
- **The sheet sits at the bottom by `margin-block: auto 0` on the dialog,** not by `align-items` on the mask. PrimeNG writes `align-items: center` inline on the mask, which CSS can't beat without `!important`. `maskStyleClass` is still passed.
- **The dialog config adds `closable: true`.** PrimeNG's Dialog binds Escape and the scrim only when it is closable (`primeng-dialog.mjs:635`, `:838`). The browser check caught a real Escape not closing the sheet. `showHeader: false` keeps PrimeNG's own ✕ hidden.
- **The drag directive ignores a press that starts on a button or link,** so pointer capture doesn't swallow the ✕. It moves the sheet with `transform: translateY()`. When the sheet closes from a drag it leaves the sheet where the finger let go, rather than snapping it back first.
- **`gridDayDate` is tested with the app's locales,** `he-IL` and `en-IL`: English dates read `21/10`, not `10/21` as the plan wrote.
- **A count cell with one request has a singular accessible name,** `publications.slotRequests.openAriaOne`, because "1 בקשות" read wrong.
- **The card flags also take `--flag-font-weight` (500, the design's `.flag`).** The card's custom properties replace its `::ng-deep` font-size override.
- **The sweep found one drift: the Students search field was 36px on a phone** (PrimeNG's default; no rule set it). It is 44px at ≤ 639px now, with the icon still centred. `students.page.scss` is 7.15 kB (was 7.11; error at 8).

Measured at the end (dev data, `pageScrollWidth`, no clipping, no small text; the only short target is the known 42px select on `/students` and `/week-schedules`):

| Page | 390 he | 390 en | 360 he | 360 en |
|------|--------|--------|--------|--------|
| `/` | 390 | 390 | 360 | 360 |
| `/teachers` | 390 | 390 | 360 | 360 |
| `/week-schedules` | 390 | 390 | 360 | 360 |
| `/publications` Draft, Closed, no Publication | 390 | 390 | 360 | 360 |
| `/publications/history` | 390 | 390 | 360 | 360 |
| `/students` and its no-match state | 390 | 390 | 360 | 360 |
| `/users` | 390 | 390 | 360 | 360 |
| `/students/import` (Roster import, not in the design) | **664** | **714** | **664** | **713** |

Rules checked by hand at 390 in Hebrew, all as the request says:
- the 56px bar and the drawer (from the right, 48px items, the active one sky with a 3px gradient bar, language, email and Sign out at the bottom);
- the header action grid (two 174px columns, 44px);
- the filter order and equal thirds;
- card `dl`s (`auto 1fr`), the ⋮ after the tag;
- muted cards (no shadow, `--g7`, 0.55 on avatar, name and details only);
- LTR isolates;
- Users role tags (Administrator sky, Teacher ocean, "-" for an Administrator);
- Weekly prep (12px × 10px card, 48px cells, short days, only "חסום" visible, sticky full-width bar);
- History cards (full-width 44px action);
- the dashboard (24px teacher select, Closed: Reopen + Excel in the 2-column row, 12px × 10px grid card, 46px label column, 4px gaps).

Not verified in the browser:
- **The Open and Published states.** No dev week is Open or Published; the page specs cover their actions.
- **A Teacher-role sign-in.** There's none in the dev data; `FindSlotRequestsInteractorTest` covers the scoping.
- **A real finger drag and a real Escape key.** The browser pane wasn't drawing, so the drag used synthetic `PointerEvent`s and Escape a synthetic `keydown` that PrimeNG's document listener handles.

**Roster import still scrolls sideways on a phone.** `/students/import` overflows at both widths, in both languages:
- its table is wider than the screen;
- stat tiles and the summary are 12.5px;
- the back link is 24px.

This branch doesn't touch it, and no design frame covers it, so it's left for a follow-up with its own design.
