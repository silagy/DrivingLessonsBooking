# US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL — Task Index (i18n slice 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 1 of the [i18n roadmap](../README.md). Execute the tasks **in order**, one per session. Each file is self-contained.

**Goal:** An admin or a student who switches between Hebrew and English sees every user-visible string in the selected language (rule-violation toasts, PrimeNG's built-in labels, the browser tab title, dates and times included), and in Hebrew every screen is mirror-correct: toasts, button icons, the date picker, the assign-teachers popover and isolated LTR values all sit where a Hebrew reader expects them, with numbers and times still readable. Covers GitHub issues [#47](https://github.com/silagy/DrivingLessonsBooking/issues/47) (US-47) and [#48](https://github.com/silagy/DrivingLessonsBooking/issues/48) (US-48).

**Architecture:** Bilingual RTL-first support already runs through the app (roadmap "What exists already"), so this slice is an acceptance check plus targeted fixes for what the planning-time audit found, not a redesign. One backend change: `ApiExceptionFilter` adds a camelCase `code` (the exception's rule name) to every 404/409 `ProblemDetails` (task 1). The client maps that code to an `errors.*` translation key and never shows the English `detail` again (task 2). `LanguageService` gains `locale` (`he-IL` / `en-IL`) and `isRtl`, hands PrimeNG a complete Hebrew/English label set, and names the browser tab in the selected language (task 3). Every date and time formatter switches to the Israeli locale, and the history page's dates, window arrow and the dashboard's "data as of" stamp are rebuilt from keys (task 4). PrimeNG's physical toast position, button-icon order and date-picker navigation are mirrored once, globally (task 5), and the assign-teachers popover is anchored to its trigger in RTL (task 6). LTR values are isolated without being pulled out of alignment, and Hebrew labels lose their letter-spacing (task 7). Task 8 runs the both-languages acceptance sweep of every screen and opens the PR.

**Tech Stack:** .NET 10 / ASP.NET Core (`ProblemDetails`, `System.Text.Json`), MSTest + Shouldly; Angular 21 (signals, standalone, zoneless) + PrimeNG 21.1 + Transloco 8; Vitest (via `ng test`). No migration, no new endpoint, no new package.

**Spec:** issues [#47](https://github.com/silagy/DrivingLessonsBooking/issues/47) and [#48](https://github.com/silagy/DrivingLessonsBooking/issues/48) · [requirements §8.4 (Hebrew and English, user-toggleable on both surfaces; RTL-first, mirror-correct, not merely translated), §8.3 (instants in Asia/Jerusalem, slot windows as wall-clock labels), decision 14](../../../requirements.md) · `.claude\rules\client-i18n.md` (no hardcoded strings, `he-IL` / `en-IL`, logical properties, no directional glyphs, `<bdi>` for LTR values) · `.claude\rules\client-primeng.md` · `.claude\rules\api-guidelines.md` "Status Codes and Error Mapping" · [i18n roadmap locked decisions](../README.md)

**Branch:** `47-us-47-48-language-and-rtl`, created from `main` (at `d271de8` when planned) in task 1 Step 1, which also commits this plan.

## User Stories

**US-47** ([#47](https://github.com/silagy/DrivingLessonsBooking/issues/47)): *As an administrator or student, I want to toggle between Hebrew and English, so that I work in my preferred language on either surface.*
- **Given** I am on any screen of the admin app or the student form **and** a visible language toggle is present **When** I switch the language **Then** every user-visible string changes to the selected language with no hardcoded leftovers.

**US-48** ([#48](https://github.com/silagy/DrivingLessonsBooking/issues/48)): *As a Hebrew-speaking user, I want layouts to be mirror-correct RTL, not merely translated, so that the UI feels native rather than awkwardly flipped.*
- **Given** Hebrew is the selected language **When** I view any screen (week grid, ranked picks, navigation, dialogs) **Then** the layout direction, alignment, icons, and ordering are mirror-correct RTL while numbers and times remain readable.
- Notes: Hebrew is RTL-first by decision 14; mirror-correctness is a requirement, not a nice-to-have.

## Planning-time audit (2 October 2026)

The planner ran the real stack (from-source API, `ng serve`) in the browser pane at 1024px, signed in as the dev admin, and on every admin screen (login, dashboard, cars & teachers with the assign popover, roster, weekly prep with the week grid, publications with the publish dialog and its date pickers, history) ran a script that lists every visible text node and every `placeholder` / `aria-label` / `title` / `alt` in the wrong script for the active language, in Hebrew and again after pressing `EN`. Two read-only code audits covered the rest: every template, store, service and both JSON files for strings (key parity checked by script), and every SCSS file, icon, bidi isolation and PrimeNG component for RTL. The student form was audited end to end at 320 and 375px in both languages by the US-22 slice on the same day ([US-22 plan](../../student-form/us-22-mobile-browser-plan/README.md)); its three defects are already fixed on `main`.

**Already passing (no task, re-checked in task 8):**
- No template contains a literal user-visible string. Every label, placeholder, button, tag, table header, empty / error / loading state and icon-only button `aria-label` goes through `| transloco`.
- `en.json` and `he.json` have the same 291 keys and the same `{{params}}`. All 235 keys referenced in code exist in both files, and every dynamic prefix (`publications.state.*`, `roster.failureReasons.*`, `weekGrid.days.*`, …) covers its enum.
- The toggle is visible on the login page, in the admin shell and in the student-form header. Switching re-renders every page without a reload, and `<html lang dir>` follows.
- Every locale-dependent label is a `computed()` that reads the language signal, so it re-formats on toggle.
- No SCSS file uses a physical layout property. Flex and grid order mirror on their own. No code reverses arrays or branches on direction.
- The week grid mirrors (Sunday at the inline-start edge, windows top to bottom), and its dates and time ranges are isolated. The ranked review list puts the grip on the right and the up/down arrows on the left in Hebrew, and those arrows are not flipped. Dialog action rows mirror. National IDs, phones, emails in teacher cards, share links, version pills and stat values are isolated.
- PrimeNG day and month names switch language.

**Defects found:**

| # | Defect | Where seen | Task |
|---|--------|------------|------|
| L1 | Every rule-violation toast is the server's English `detail`, with GUIDs inside: `Publication 3f2a… window extension 2026-10-08T… must be later than the current end.` The toast service shows `problem.detail` as is, and the server is single-language. | every admin toast raised from a 404/409 (`ToastService.apiError`) | 1, 2 |
| L2 | PrimeNG's built-in labels stay English in Hebrew: the date picker's `Choose Date`, `Previous Month`, `Next Month`, `Choose Month`, `Choose Year`, `Next Hour`, `Previous Minute` …, the dialog and toast close button `Close`, the select's `No results found`. `PRIMENG_HE` only covers day/month names, `today` and `clear`. | publish / extend / reopen dialogs, every dialog close button, teacher / week / transmission selects | 3 |
| L3 | The browser tab says `Driving Lessons` in both languages. Nothing ever sets `document.title`. | every page | 3 |
| L4 | English dates are American: the week grid and the student slot list show `10/4` for 4 October, and admin timestamps use a 12-hour clock (`Oct 4, 2026, 3:00 PM`) next to 24-hour slot windows. Every formatter receives the bare language (`'en'` = en-US). The history week column shows the raw ISO date (`2026-11-29`) in both languages, and the date picker shows `Thu, Oct 8` (month first) in both. | week grid, student slots, dashboard / history / roster timestamps, history week column, publish dialogs | 4 |
| L5 | The language toggle group has no accessible name, and its `עב` button has no `lang`, so a screen reader reads it with the English voice. | toggle on all three surfaces | 3 |
| R1 | The toast container stays at the top **right** in Hebrew (`position: fixed; right: 20px`). PrimeNG's `position` is physical. | every toast | 5 |
| R2 | PrimeNG keeps `iconPos` physical in RTL: the roster `העלאת קובץ CSV` upload icon and the student form's refresh icon come **after** their labels in Hebrew reading order, and the login `כניסה` arrow sits at the start, pointing back into the label. | roster, student-form load-failed screen, login | 5 |
| R3 | The date picker keeps `next month` on the right with a right-pointing chevron in Hebrew, while its day grid is mirrored (Sunday on the right). | publish / extend / reopen dialogs | 5 |
| R4 | The assign-teachers popover opens off its trigger in Hebrew: it is anchored to the trigger's **left** edge and grows to the right, and its arrow sits at `-6px`, outside the panel. PrimeNG's `absolutePosition` writes the trigger's physical left into `inset-inline-end`. | cars & teachers, `+ שיוך` | 6 |
| R5 | The history window column joins two dates with `→`. In Hebrew the range reads right to left, so the arrow points back at the start date. | history | 4 |
| R6 | LTR values: the shell's admin email is not isolated; the login email field lays out right-to-left while typing; the teacher card's email sits on the **left** under a right-aligned name; the roster's national ID and phone cells are left-aligned under right-aligned headers. The last two set an LTR direction on the whole block, so `text-align: start` becomes left. | admin shell, login, cars & teachers, roster | 7 |
| R7 | Eight small uppercase labels add `letter-spacing` (0.04–0.1em) that also spaces out their Hebrew letters: dashboard week eyebrow, slot-count caption, wizard step counter, review session-type pill, assign popover title, car `משותף` tag, teacher-card chip tag, cars & teachers section headings. | dashboard, student form, cars & teachers | 7 |

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Acceptance slice, not a redesign.** The audit decides the scope. Only defects a user would see or hear get a task. Everything already passing is re-checked in task 8 and listed in the PR. |
| 2 | **`code` is the exception's type name** minus `Exception`, camelCased (`WindowExtensionMustBeLaterException` → `windowExtensionMustBeLater`), added in the one filter. No per-exception code property: the naming convention `{Entity}{Rule}Exception` already makes the name the rule, and requirements terminology. Renaming an exception renames its code, and task 2's completeness check (re-run in task 8) catches the missing key. `AuthenticationFailedException` gets no code: sign-in failure is deliberately detail-free and the login page has its own message. |
| 3 | **Every one of the 56 codes gets a key**, student-only ones included, rather than a reachability analysis of which exceptions an admin can trigger. One rule ("every exception class has an `errors.*` key in both files") is checkable by script; a reachability list goes stale. Codes without a key (a future exception before its key lands) fall back to a translated generic message per status (`errors.conflict`, `errors.notFound`). |
| 4 | **The toast never shows `detail` again**, in either language. The English `detail` stays in the response for logs and developers. |
| 5 | **Snapshot translations inside modal dialogs stay as they are**: dialog headers translated at open time, the car form's transmission options and the publish dialog's week label. Every one of them sits inside a modal dialog (`modal: true, dismissableMask: true`) whose mask covers the whole page, toggle included, so a click on the toggle lands on the mask and closes the dialog before any language changes. The planner switched language under an open dialog only by script, which a user cannot do. Toasts are translated when raised and gone after ~3 s. `client-i18n.md` allows `translate()` for exactly these point-in-time strings. |
| 6 | **Tab title = `shell.title`** (`Driving Lessons Planner` / `מערכת תכנון שיעורי נהיגה`), the existing, unused key. One title for every page (YAGNI: no per-route titles). `index.html` gets the Hebrew title too, so the tab is right before the app boots. |
| 7 | **PrimeNG gets a complete label set for the components in use** (date picker, select, dialog, toast, popover), in `client\src\app\core\primeng-translations.ts`, with identical keys in both languages (a spec pins it). PrimeNG's `setTranslation` merges only the top level, so `LanguageService` merges `aria` on top of the current `aria` too. Otherwise switching to Hebrew would drop PrimeNG's other English `aria` labels instead of keeping them. |
| 8 | **Date-picker format `D, d M` in both languages** (`ה׳, 8 אוק׳ 14:30`, `Thu, 8 Oct 14:30`): day before month, matching `en-IL` / `he-IL` everywhere else (roadmap decision 2). A pattern, not a translation key, because it is the same in both. |
| 9 | **The popover is fixed in the component, not by patching PrimeNG**: on `(onShow)` in RTL, the component re-anchors the panel to its trigger's inline-start edge (a pure function, unit-tested), and a global rule moves the arrow with logical properties. Upgrading PrimeNG is not on the table for this slice. |
| 10 | **Hebrew drops tracking through one token**: each tracked label uses `calc(<em> * var(--app-caps-tracking))`, and `html[lang='he']` sets the token to `0`. Car-card's existing `.car__pill` RTL override stays as it is (it also changes size and weight). |
| 11 | **`text-align: match-parent`** for a block that isolates an LTR value (teacher email, roster ID and phone cells): it resolves `start` against the parent's direction, so the value lines up with its RTL neighbours while its own characters stay left-to-right. One property, no wrapper elements. |
| 12 | **No app initializer for translations.** The code audit suspected a race where `?publish=1` opens the dialog before the language file loads and shows the raw key as its header. A cold load of `/publications?…&publish=1` in the pane showed the Hebrew header, so there is nothing to fix (YAGNI). |
| 13 | **Out of scope** (open items, not defects a user hits in this app): a Hebrew webfont, the English `aria-label="dropdown trigger"` that PrimeNG hardcodes on every select's chevron, deleting the 27 dead keys, the admin header overflowing below ~900px in English, the direction of a numeric range inside a Hebrew sentence (`דירוגים 1–3`), and locale-aware `localeCompare` sorting. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- The client uses **Transloco** (`TranslocoPipe`, `| transloco`, `TranslocoService.translate` / `selectTranslate`), not ngx-translate. Translation files are `client\public\i18n\en.json` + `he.json`, top-level namespaces in camelCase (`general`, `errors`).
- `LanguageService` exposes the `lang` signal (not `currentLanguage`), persists under `app_lang`, and applies everything in one `effect`.
- Client specs use `TranslocoTestingModule.forRoot({ langs: {…}, translocoConfig: {…}, preloadLangs: true })`; fakes are plain objects (`{ provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } }`).
- Backend tests use MSTest + Shouldly with `//given //when //then` markers and sentence-style method names (`Disabled_Email_Needs_No_Smtp_Settings`). The filter test lives in `DrivingLessons.Application.Test`, which already tests Infrastructure code; task 1 gives it a reference to `DrivingLessons.Presentation.Web`.
- New TypeScript uses 4-space indentation. No comments anywhere except the test markers.
- Layout and rendering that jsdom cannot measure (R1–R4, R6, R7) are verified in the browser pane with `getComputedStyle` and `getBoundingClientRect`, the way earlier slices verified RTL placement.

## Global Constraints

- Hebrew and English, user-toggleable on both the admin and the student surface; Hebrew is the default and RTL-first (requirements §8.4, decision 14).
- No hardcoded user-visible string. A key added to `en.json` is added to `he.json` in the same commit with the same `{{params}}` (`client-i18n.md` rules 1 and 4).
- Logical CSS properties only (`margin-inline-start`, `inset-inline-end`, `text-align: start`). The only physical names allowed are resets of PrimeNG's own physical properties (`left: auto`, `margin-left: 0`) inside the global overrides.
- No directional glyph (`→`, `‹`) in a template or translation (`client-i18n.md` RTL rules).
- Instants display in Asia/Jerusalem; slot windows stay wall-clock labels, never converted (§8.3).
- Business rules stay in the backend (CLAUDE.md rule 12). The client only maps a server `code` to a message.
- The national ID never appears in a URL, `localStorage`, a log line or a test name. Test data uses synthetic IDs only (`000000018`, `000000026`).
- No EF migration, no new endpoint, no `package.json` change.

## Review Focus

Inputs and conditions the stories imply but a happy-path test would not exercise. Each is pinned by a step in the owning task:

1. **A 409 whose `code` has no key yet** (a new exception added after this slice) → the toast shows the translated generic conflict message in the active language, never English `detail` → task 2 spec "falls back to the generic message for a rule it has no key for", and the completeness check in task 2 Step 7 / task 8 Step 2.
2. **Switching back and forth** (Hebrew → English → Hebrew) → every PrimeNG label is in the language just chosen, none left over from the previous one (PrimeNG merges translations) → task 3 spec "restores every PrimeNG label in English after Hebrew".
3. **A returning user whose saved language is English opening a page cold** → the first render is English, left-to-right, with the English tab title and English PrimeNG labels → task 3 spec "starts in the saved language", task 8 Step 4.
4. **The assign popover opened near the viewport's edge in Hebrew** → the panel stays fully on screen and its arrow still points at the trigger → task 6 spec "keeps the panel inside the viewport when the trigger is near the edge", task 6 Step 7.3 (480px window in the pane).
5. **Mixed-script values on the Hebrew page**: an email that starts with digits, an English teacher or car name, an English constraint → each reads in its own direction and lines up with its column or label → task 7 Steps 5–6, task 8 Step 6.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-problem-code.md](task-01-problem-code.md) | Branch + plan commit; `ApiExceptionFilter` adds the rule `code` to every 404/409 (L1, server half), TDD | ✅ own commit |
| 2 | [task-02-translated-rule-violations.md](task-02-translated-rule-violations.md) | `errors.*` keys for all 56 codes + generic fallbacks; `ToastService` maps `code`, never shows `detail` (L1, client half), TDD | ✅ own commit |
| 3 | [task-03-language-service.md](task-03-language-service.md) | `LanguageService.locale` / `isRtl`, complete PrimeNG labels, translated tab title, accessible toggle (L2, L3, L5), TDD | ✅ own commit |
| 4 | [task-04-israeli-dates.md](task-04-israeli-dates.md) | Every formatter on `he-IL` / `en-IL`, date-picker format, history week + window range, "data as of" stamp (L4, R5), TDD | ✅ own commit |
| 5 | [task-05-primeng-rtl-mirroring.md](task-05-primeng-rtl-mirroring.md) | Toast position by direction, logical button-icon order, mirrored date-picker navigation (R1–R3) | ✅ own commit |
| 6 | [task-06-popover-anchor.md](task-06-popover-anchor.md) | Assign-teachers popover anchored to its trigger in RTL, arrow included (R4), TDD | ✅ own commit |
| 7 | [task-07-hebrew-text-polish.md](task-07-hebrew-text-polish.md) | LTR values isolated and aligned, no tracking on Hebrew (R6, R7) | ✅ own commit |
| 8 | [task-08-acceptance-and-pr.md](task-08-acceptance-and-pr.md) | Both-languages sweep of every screen, rule-violation toasts end to end, PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `47-us-47-48-language-and-rtl` and all earlier tasks are committed (task 1 creates the branch).
2. Open the task file and follow the steps exactly. Each step has full file contents or an anchored edit, plus exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `.claude\launch.json` gets a local-only `api-smoke` entry in task 8. Never stage it: `git add` only the paths each task lists.

Commands:
- Backend (repo root): `dotnet build` and `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`. The build's existing warnings (`NU1903`, `MSTEST0001`, `CS8618`) are not new.
- Client (in `client\`): `npm test -- --watch=false` and `npm run build`. No install is needed in this slice.

Environment notes (project memory):
- Browser-pane screenshots are flaky on this PrimeNG app: `await document.fonts.ready` first, and fall back to DOM / `getComputedStyle` evidence. When the pane is not drawn on screen, PrimeNG overlays (date-picker panel, popover) can stay at zero size because their enter animation never runs. Bring the pane forward or measure after `computer` `left_click`, not a scripted `.click()`.
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`). `dl-postgres` has a stale migration history.
- **Seeding Hebrew from Git Bash on Windows:** `curl` is the Windows binary, so Hebrew passed in an argument arrives as `?????`, and `-F file=@/tmp/…` fails with status `000`. Task 8 writes every Hebrew payload to a file and runs `curl` from that directory with relative `@file` paths.

## Open Items (non-blocking)

1. **No Hebrew webfont.** The font stack (`Helvetica Now`, `Helvetica Neue`, Arial) has no Hebrew glyphs except Arial's, so Hebrew renders in Arial / Liberation Sans / the phone's system font, and on a machine with Helvetica installed, Hebrew and Latin come from different families. A design decision, not a defect of this slice.
2. **PrimeNG hardcodes `aria-label="dropdown trigger"`** on the select's chevron (`primeng-select.mjs`). It is not in PrimeNG's translation object. The select itself is labelled, so the trigger is redundant to a screen reader. Fix upstream or with a `pt` override if an accessibility review asks.
3. **PrimeNG's RTL rules use `:dir(rtl)`** (Chrome 120+, Safari 16.4+). On older phone browsers PrimeNG's own flips do not apply, and neither do this slice's overrides of them (same selector), so those browsers get logical, unflipped behaviour, which is already mirror-correct for button icons and the date picker.
4. **27 dead translation keys** (`shell.languageToggle`, `publications.title`, `publications.dialogs.*Title`, …). Deleting them is housekeeping for its own PR.
5. **Admin header overflows below ~900px in English** (the longer English nav labels push `Sign out` off-screen). The admin surface is desktop-first (`client-primeng.md` "Responsive Sizing").
6. **No automated guard for the error-code keys.** Task 2's completeness check is a script run by hand. If exceptions keep being added, a test that reflects over `DomainException` / `NotFoundException` subclasses and reads both JSON files is the follow-up.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs      + code extension on 404/409
tests\DrivingLessons.Application.Test\
├── DrivingLessons.Application.Test.csproj                          + reference to Presentation.Web
└── Filters\ApiExceptionFilterTest.cs                                new
client\src\index.html                                                Hebrew title
client\src\styles\_tokens.scss                                       --app-caps-tracking
client\src\styles\_global.scss                                       PrimeNG RTL overrides (button icons, date picker, popover arrow)
client\public\i18n\en.json + he.json                                 errors.*, shell.language, publications.history.windowRange; − publications.dashboard.dataAsOf
client\src\app\
├── app.ts + app.spec.ts                                             toast position by direction
├── core\
│   ├── language.service.ts + .spec.ts                               locale, isRtl, PrimeNG labels, tab title
│   ├── primeng-translations.ts + .spec.ts                           new: PRIMENG_HE / PRIMENG_EN
│   └── services\toast.service.ts + .spec.ts                         code → errors.* key
├── shared\
│   ├── models\problem-details.ts                                    + code
│   ├── language-toggle\language-toggle.component.ts + .spec.ts      group label, lang attributes
│   └── components\week-grid\week-grid.component.ts + .spec.ts       locale
└── features\
    ├── admin-shell\admin-shell.component.html                       <bdi> email
    ├── auth\login.component.html                                    dir="ltr" email
    ├── publications\…                                               locale; history week + range key; dataAsOf key; date-picker format
    ├── roster\…                                                     locale; ID/phone cell alignment
    ├── student-form\state\student-form.store.ts + specs             locale
    ├── teachers\ui\components\assign-teachers-popover\…             RTL anchor (+ rtl-popover-placement.ts + .spec.ts)
    ├── teachers\ui\components\teacher-card\…                        email alignment
    └── week-schedules\state\week-schedules.store.ts                 locale
docs\modules\i18n\README.md                                          new roadmap, slice row links this plan (committed with the plan)
```

## Execution Notes (2 October 2026) — what shipped differently from the tasks

The tasks above are the plan as written. These deviations were ruled during execution; the code on the branch follows the notes, not the task text.

| Task | Plan said | Shipped | Why |
|------|-----------|---------|-----|
| 1 | No new package | `Microsoft.EntityFrameworkCore.Relational` 10.0.9 pinned in `DrivingLessons.Application.Test` | Referencing Presentation.Web raised MSB3277 (EF Core 10.0.4 transitive vs the 10.0.9 the Web app already runs through EF Design). The pin matches production. |
| 4 | `new Date(row.weekStart)` in the history page | Local parse (`split('-')`), also in `week-grid.component.ts` | `new Date('YYYY-MM-DD')` is UTC midnight; west of UTC the grid showed the previous day. The week pickers' `buildWeekOptions` still has this pattern (follow-up). |
| 6 | `insetInlineEnd`, measured against `clientWidth` | `insetInlineStart` (field and style), measured against `document.documentElement.getBoundingClientRect().right` (`containerRight`) | In RTL `inset-inline-end` is the **left** edge. An absolute panel is placed against the document's edge, which differs from `clientWidth` when the page is scrolled sideways. |
| 7 / decision 11 | `text-align: match-parent` | `:host-context([dir='rtl']) .card__email` / `.roster__ltr-cell { text-align: end; }` | Chrome computes `match-parent` to `start` and resolves it against the element's own LTR direction, so the value stayed left. |
| 8 (found) | `<p-toast [position]>` bound to the direction | One `<p-toast>` re-created per corner (`@for … track`) | PrimeNG patched the inline style with `right: false`, so switching English → Hebrew at runtime kept the toast on the right. |
| Final review | — | An unsupported saved `app_lang` falls back to Hebrew | Any other value crashed the language effect. |
