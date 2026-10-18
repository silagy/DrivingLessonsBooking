# Dark mode - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans (recommended here, see Execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

First plan in the `dark-mode` module. It adds a dark colour scheme to the whole client (admin pages, sign-in and the public Student form) and an Appearance control (Light / Dark / System) in the phone drawer and the desktop user menu. Layouts, behaviour and copy stay as they are: only colours change, plus the new control. Each task has its own file. Execute in order.

**Goal:**
- Every screen renders in dark when the User picks Dark, or picks System (the default) on a device set to dark.
- The choice is saved per device and applied before the first paint, so a reload in dark never flashes white.
- In light, every screen looks as it does today, apart from the few one-step shifts listed in Decision 7.

**Architecture:**
- **Switch:** a signal-based `ThemeService` in `core\` holds the mode, resolves System through `matchMedia`, and toggles the `app-dark` class and `color-scheme` on `<html>`. PrimeNG's `darkModeSelector: '.app-dark'` makes the preset's `colorScheme.dark` apply under the same class. An inline script in `index.html` sets the class before Angular boots.
- **Colours:** `_tokens.scss` gets a `:root.app-dark` block with an explicit value for every colour token, plus new tag, row, selected, error and danger tokens. Component SCSS reads only `--app-*` tokens (and PrimeNG's scheme-aware semantic tokens such as `--p-primary-color`), never a palette step such as `--p-sky-50` or a hex. A spec enforces that.
- **Control:** one shared `AppearanceToggleComponent` (PrimeNG `SelectButton`) bound to `ThemeService.mode`, used in the drawer footer and in the user menu's `#start` template.

**Tech Stack:** Angular 21 (standalone, zoneless, OnPush, signals), PrimeNG 21 with the Aura preset via `@primeuix/themes` (`definePreset`), Transloco, SCSS in `@layer app`, Vitest under jsdom. Measured in the browser pane (Claude Browser tools) against the dev API.

**Spec:**
- The user's request in chat on 2026-10-18 ("Add dark mode to the DriveCal client ... 1. THEME SWITCH MECHANISM ... 7. CHECKS").
- The Claude Design file `Admin Mobile.html` from the user's export `C:\Users\AdiSilagy\Downloads\Driving Lesson Mockup.zip` (2026-10-18 16:25), copied over `Driving Lesson Mockup\Admin Mobile.html` in this branch. Compared with the copy on `main`, the export adds:
  - row 2, frames **D01 to D08c** (every phone frame in dark, under the `.dk` class);
  - **D09 Dark tokens** (the token map and the five rules: switch, mechanism, override only `--app-*`, elevation, accent);
  - **10 / D10 Desktop user menu** (the Appearance section in light and dark);
  - an "מראה" row in frame 01's drawer footer.
- Rules: `.claude\rules\client-architecture.md`, `client-state.md`, `client-primeng.md`, `client-i18n.md`.

**Branch:** `dark-mode`, from `origin/main` at `44e4e10` (no upstream set; push with `-u origin dark-mode`). Commit this plan and the design snapshot on the branch before task 1. The PR targets `main`. CI (`ci.yml`) runs on the PR. Auto-merge is off, so merge by hand with `--merge` once the PR is reviewed and green.

## Current State

| Area | Today | File |
|------|-------|------|
| PrimeNG theme | `darkModeSelector: false`; the preset has only `colorScheme.light` (primary, surface) and a light-only gradient primary button | `client\src\app\app.config.ts:26-34`, `client\src\app\theme\app-preset.ts` |
| Tokens | One `:root` block. Every colour is an alias of a `--p-*` variable (`--app-bg-page: var(--p-surface-50)`, ...). `--app-plum` holds Plum | `client\src\styles\_tokens.scss` |
| Boot | `index.html` has `<meta name="theme-color" content="#ffffff">` and no script | `client\src\index.html` |
| Language toggle | `LanguageService` (signal, `localStorage` key `app_lang`, effect sets `lang`/`dir` on `<html>`) and a hand-built segmented `LanguageToggleComponent`. `App` injects `LanguageService` so it starts at boot | `core\language.service.ts`, `shared\language-toggle\`, `app.ts` |
| Drawer footer | Language row (label at start, toggle at end), email, Sign out (`--p-red-600`) | `features\admin-shell\ui\components\shell-drawer\` |
| User menu | `p-menu` popup; `#start` template holds the identity header (avatar, email, role tag, bottom border); items are Change my password and Sign out | `features\admin-shell\admin-shell.component.{html,ts,scss}` |
| Segmented controls | `p-selectbutton` styled through `::ng-deep .x.p-selectbutton .p-togglebutton` with `--app-*` tokens (Students status filter, role switch in dialogs) | `students.page.scss:69`, `shared\dialogs\dialog-form.scss:50` |
| Hard-coded colours | No hex, `white` or `rgba(18, 20, 24` in component SCSS except `#f2f7ff` in `change-car.dialog.scss:28`. Palette steps are used directly in about 30 files: `--p-sky-50/100/500/700`, `--p-ocean-50/100/700`, `--p-primary-50/100/700/800`, `--p-steel-200`, `--p-surface-100/400/500/600`, `--p-red-50..700`, `--p-green-600`, `--p-blue-600` | `grep -rnE -- "--p-(sky\|ocean\|steel\|plum\|primary\|surface\|red\|green\|blue)-[0-9]+\|#[0-9a-fA-F]{3,8}" client/src --include=*.scss` |
| Tags | Status and state tags are `p-tag` with `severity` (`success`, `secondary`, `info`, ...), not custom classes | `student-card`, `students.page`, `users.page`, `user-card`, `publication-state-tag` |
| Brand logo | Mark and wordmark read `--app-brand`, `--app-brand-deep`, `--app-brand-soft`, `--app-on-accent` and a backdrop | `shared\brand-logo\brand-logo.component.scss` |
| CSP | None (Caddy sets only HSTS), so an inline script in `index.html` runs | `deploy\Caddyfile` |

Contrast of the requested dark tokens, computed while planning (all pass): ink on card 15.66, text-secondary on card 7.83, text-muted on card 5.46 and on `--app-bg-muted` 5.16, brand on card 5.78, brand-deep on card 9.80, danger on card 7.93, tag sky 8.90, ocean 9.81, green 8.70, whale on steel-light 7.68, brand on row-new 5.50. Two values that are **not** in the request fail and are fixed by Decisions 4 and 6: Aura's dark muted text (`surface.400` = `#6B7385`, 3.55 on card) and `--p-red-600` on a dark card.

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **`ThemeService` mirrors `LanguageService`:** `core\theme.service.ts`, `providedIn: 'root'`, `mode = signal<ThemeMode>(...)` read from `localStorage['drivecal.theme']` (anything other than `light`/`dark`/`system` is `system`), `isDark = computed(...)`, `use(mode)`, and one `effect` that persists the mode and applies it. `App` injects it next to `LanguageService`, so the sign-in page and the public Student form follow the saved choice too. |
| 2 | **Applying a mode** sets, on `document.documentElement`: the `app-dark` class, `style.colorScheme` (`'dark'` / `'light'`), and the `content` of `meta[name="theme-color"]` (`#1B1D22` in dark, the bar colour; `#ffffff` in light, today's value). The status-bar colour isn't in the request; without it a phone in dark shows a white status bar over a dark bar. |
| 3 | **No flash, two parts in `index.html` `<head>`:** <ul><li>an inline `<script>` after the `theme-color` meta that repeats Decision 1's resolution and Decision 2's three writes (wrapped in `try` for `localStorage`);</li><li>an inline `<style>`: `html.app-dark { color-scheme: dark; background: #121418; }`.</li></ul> The style is needed because the production build inlines only critical CSS and loads `styles.css` without blocking, and `:root.app-dark` rules can't be "critical" when the static HTML has no `app-dark` class. A spec pins the boot background to the dark `--app-bg-page`. |
| 4 | **Dark preset (`colorScheme.dark`)**, beyond the request's primary and surface: <ul><li>`text`: `color '{surface.50}'`, `hoverColor '{surface.0}'`, `mutedColor '{surface.300}'`, `hoverMutedColor '{surface.200}'` (Aura's `surface.400` muted fails 4.5:1);</li><li>`content`: background `'{surface.800}'`, hoverBackground `'{surface.700}'`, borderColor `'{surface.600}'` (menus, cards, tables at `#1B1D22` / `#2C2F37`);</li><li>`overlay.select`, `overlay.popover`, `overlay.modal`: background `'{surface.800}'`, borderColor `'{surface.600}'`; and `shadow: 'var(--app-shadow-lift)'` on those three and `overlay.navigation` (Elevation rule);</li><li>`formField`: background `'{surface.800}'`, borderColor `'{surface.600}'`, hoverBorderColor `'{surface.500}'` (design `.sel`);</li><li>`navigation.item` and `list.option`: focusBackground `'{surface.700}'` (Aura's `surface.800` equals the menu itself, so hover would vanish).</li></ul> Aura's dark mask is already `rgba(0,0,0,0.6)`, the request's scrim. |
| 5 | **Component tokens in the preset:** <ul><li>`button.colorScheme.dark.root.primary` is the same object as light's, extracted to one `const gradientPrimaryButton`;</li><li>`tag`: `success` in **both** schemes reads `var(--app-tag-green-bg)` / `var(--app-tag-green-fg)`; in dark, `info` reads `var(--app-tag-sky-bg)` / `var(--app-tag-sky-fg)` and `secondary` is `'{surface.600}'` / `'{surface.100}'` (design `.tag.m`; Aura's `surface.800` would vanish on a card);</li><li>`togglebutton.colorScheme.dark`: root background, hoverBackground and checkedBackground `'{surface.800}'`, borderColor and checkedBorderColor `'{surface.600}'`, color `'{surface.200}'`, hoverColor `'{surface.100}'`, checkedColor `'{surface.50}'`, `content.checkedBackground '{surface.600}'` (Aura's `surface.950` is near black; only the unstyled Roster filter uses these).</li></ul> Preset tokens may hold `var(--app-*)`: the tag and shadow colours then live in one place, `_tokens.scss`. |
| 6 | **Token set.** The request's dark values, verbatim, plus tokens for colours the request's list missed. Every colour token has a value in both blocks: <ul><li>`--app-plum` is renamed `--app-danger` (same light value, Plum; dark `#FF8AD8`). Two consumers: `student-card` flags and the card menu's destructive item;</li><li>new error family, because 15 rules use `--p-red-*` and red-600 fails on a dark card: `--app-error` (light `var(--p-red-600)`, dark `var(--p-red-400)`, 6.10:1), `--app-error-strong` (`red-700` / `red-300`), `--app-error-bg` (`red-50` / `color-mix(in srgb, var(--p-red-500), transparent 84%)`), `--app-error-bg-strong` (`red-100` / `color-mix(in srgb, var(--p-red-500), transparent 76%)`), `--app-error-border` (`red-200` / `color-mix(in srgb, var(--p-red-400), transparent 60%)`);</li><li>`--app-success` (`var(--p-green-600)` / `var(--p-green-400)`) and `--app-info` (`var(--p-blue-600)` / `var(--p-blue-400)`) for the Roster tiles and the Student form's slot count;</li><li>`--app-dot-green` is **not** added: no component draws the design's green dot, so it would have no consumer.</li></ul> |
| 7 | **Light keeps today's rendering.** The request calls the light values "current", but some of its hex values come from the design and differ from what the app renders. Where they differ, the light value is today's: <ul><li>`--app-tag-green-bg/-fg` = `var(--p-green-100)` / `var(--p-green-700)` (Aura's success tag today), not `#E9F5EC` / `#1E7A39`;</li><li>`--app-row-new-bg` = `var(--p-sky-50)`, not `#F2F7FF`;</li><li>`--app-selected-bg` = `var(--p-sky-50)`, not `#EEF3FF`.</li></ul> Removing literals still shifts four light colours by one step, accepted: <ul><li>`week-grid-legend` text `--p-surface-600` becomes `--app-text-secondary` (`surface-500`);</li><li>the required-field marks in `teachers\ui\dialogs\dialog-form.scss:26` and `cars-and-teachers.page.scss:56`, `--p-red-500`, become `--app-error` (`red-600`);</li><li>the car flag's hover underline `--p-red-300` becomes `--app-error-border` (`red-200`);</li><li>Change car's selected option `#f2f7ff` becomes `--app-selected-bg` (`#EAF1FF`).</li></ul> |
| 8 | **The mapping rule for literals** (tasks 4 and 5 apply it file by file): <ul><li>a tag or chip's `sky-50 / sky-700 / sky-100` (or `primary-50 / primary-700 or 800 / primary-100`) → `--app-tag-sky-bg / -fg / -border`; `ocean-50 / 700 / 100` → `--app-tag-ocean-*`;</li><li>the new-Student tint `sky-50` + `sky-100` border → `--app-row-new-bg / -border`; an Inactive row or card background → `--app-row-muted-bg`;</li><li>a selected or active fill `sky-50` → `--app-selected-bg`, its `sky-500` border or outline → `--app-selected-border`, its `sky-700` text → `--app-brand-deep`;</li><li>`sky-500` as a focus outline, icon or text colour → `--app-brand` (dark `#5C95FF`; `sky-500` itself is 2.6:1 on a dark card, under the 3:1 a focus ring needs); `sky-700` text → `--app-brand-deep`;</li><li>the tappable count-cell border `steel-200` → `--app-border-interactive`;</li><li>`surface-100` fill → `--app-bg-muted`; `surface-500 / 600` text → `--app-text-secondary`; `surface-400` → `--app-text-muted`;</li><li>`red-500 / 600` → `--app-error`; `red-700` → `--app-error-strong`; `red-50` → `--app-error-bg`; `red-100` → `--app-error-bg-strong`; `red-200 / 300` → `--app-error-border`; `green-600` → `--app-success`; `blue-600` → `--app-info`.</li></ul> Red stays red and Plum stays Plum, so the drawer's Sign out becomes `--app-error`, not Plum, though frames 01 / D01 draw it Plum. |
| 9 | **Car and deleted flags.** The request says "car-flag / deleted-flag (→ `--app-danger`)". That is what the Student card already sets through `--flag-color` (Plum), so `student-card` sets `--flag-color: var(--app-danger)`. The flags' own defaults, used by the desktop table, follow Decision 8 (red → `--app-error`; gray → `--app-text-secondary` / `--app-text-muted`), so the table keeps its colours in light. |
| 10 | **The brand mark keeps its colours in dark** (D09 rule 5: "The logo is unchanged"; the design's dark bar draws the light `#cal` symbol). `brand-logo.component.scss` declares `:host` custom properties `--brand-mark-sky: var(--p-sky-500)`, `--brand-mark-deep: var(--p-sky-700)`, `--brand-mark-soft: var(--p-sky-300)` and `--brand-mark-page: var(--p-surface-0)` (`#ffffff` in both schemes), and the mark's parts read those. The wheel's windows read `--brand-mark-page` too, so they stay white; the halo keeps the backdrop (it's the gap that has to match the surface behind). The wordmark keeps `--app-brand-deep` / `--app-brand`, which lift to `#A9C6FF` / `#5C95FF` in dark. Declarations of `--brand-mark-*` are the one exception the colour guard spec allows. |
| 11 | **The Appearance control is one component,** `shared\appearance-toggle\AppearanceToggleComponent` (`app-appearance-toggle`), beside `language-toggle`. It wraps `p-selectbutton` with `[allowEmpty]="false"`, options in DOM order Light, Dark, System (they follow the page direction; the language toggle forces LTR, this one doesn't). Labels are translated in an `#item` template, so they change with the language. Input `fill = input(false)`: `false` is the drawer's inline control with 36px options; `true` is the menu's full-width, three equal columns, 34px options (frame 10 `.ap .segf`). |
| 12 | **Keyboard.** PrimeNG's `SelectButton` makes each option a tab stop and selects on Enter / Space, with no arrow keys. The component adds a `keydown` handler. ArrowRight / ArrowLeft move focus to the visually next / previous option: in RTL, ArrowLeft is next in DOM order. Home and End go to the first and last option, and moving wraps around. Arrows move **focus only**; Enter / Space select, as PrimeNG does. That way browsing the options doesn't flip the whole app's colours on each key press. The group's `aria-label` is `shell.appearance.label`. |
| 13 | **The desktop section doesn't close the menu.** The control sits in the `p-menu` `#start` template, which is inside the popup container, so PrimeNG's outside-click check doesn't fire. It isn't a menu item, so no `command` hides the popup. Task 3 proves both in a spec and in the browser. Change my password stays in the menu: the design shows only Sign out, but the request says not to change behaviour. |
| 14 | **No Appearance control on sign-in or the Student form.** The request places it only in the drawer and the user menu. Those pages follow the device's saved choice, or System. |
| 15 | **Guard specs keep it honest** (both in `client\src\app\theme\`): <ul><li>`dark-tokens.spec.ts` reads `_tokens.scss` and checks that every colour token in `:root` has a `:root.app-dark` value, that the dark text and tag pairs meet 4.5:1 / 3:1, and that the boot background equals the dark page colour;</li><li>`no-hardcoded-colours.spec.ts` scans SCSS for hex, `rgb(a)(` and palette steps (`--p-<palette>-<n>`).</li></ul> |
| 16 | **Verification is in the browser pane, so execution should be native** (the session with the Browser tools). `resize_window {colorScheme: 'dark'}` emulates the OS setting, so System can be tested live. |

## Global Constraints

- Standalone components, OnPush, signals for state; no new dependency; the only new store-like class is `ThemeService` (a `core` singleton, like `LanguageService`).
- Layout, behaviour and copy don't change. The only new UI is the Appearance control. Only colours change.
- Component SCSS uses `--app-*` tokens and PrimeNG semantic tokens without a palette step (`--p-primary-color`, `--p-border-radius-*`). No hex, no `rgb(a)(`, no `--p-<palette>-<n>`. Hex appears only in `app-preset.ts`, `_tokens.scss` and the boot code (`index.html`, `theme.service.ts`).
- Every custom style stays in `@layer app`. No `!important`. `::ng-deep` only under `:host`, or bare and anchored to a unique `styleClass` for overlays.
- Logical CSS properties (`client-i18n.md`). Every new string is a Transloco key in both `client\public\i18n\he.json` and `en.json`. No long dashes or ellipsis characters in source (`core\source-text.spec.ts`).
- Strings, verbatim: `shell.appearance.label` "מראה" / "Appearance"; `shell.appearance.light` "בהיר" / "Light"; `shell.appearance.dark` "כהה" / "Dark"; `shell.appearance.system` "מערכת" / "System".
- `localStorage` key `drivecal.theme`; values `light`, `dark`, `system`; default `system`.
- Contrast in dark: body text ≥ 4.5:1, tags and large text ≥ 3:1, against their own background.
- Component style budget: 4 kB warning, 8 kB error. `students.page.scss` is about 7.15 kB, so token renames there must not add rules.
- Backend, Excel export and emails don't change: `git diff origin/main -- src tests` stays empty.

**Commands.** Run client commands from `client\` in PowerShell, with the local Angular CLI:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

- To run one spec, add `--include src/app/<path>.spec.ts`.
- If `client\node_modules` is missing, install from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

**Dev servers and sign-in** are as in [admin-mobile-pages-plan](../../admin-mobile/admin-mobile-pages-plan/README.md#global-constraints):
- The API runs against the compose Postgres, never `dl-postgres`.
- Ports 5013 to 5112 may be reserved on this machine. If `preview_start {name: "api"}` fails to bind, use the earlier plan's 5780 / 4210 scratch setup.
- Sign in with the dev Administrator from `appsettings.Development.json`.
- Before measuring, `await document.fonts.ready`. Prefer `javascript_tool` measurements over screenshots.
- Sizes: `resize_window {width: 390, height: 844}`, `{width: 360, height: 780}`, `{width: 1280, height: 800}`; reload after resizing.
- Language: `.lang__opt[lang="he"]` / `.lang__opt[lang="en"]`. Theme: the new control, or `localStorage.setItem('drivecal.theme', 'dark')` and reload.

## Review Focus

1. **Reloading in dark on a slow connection.** Expected: the first paint is already dark (no white frame), including on a production build where `styles.css` loads after first paint. Task 1, `turns the page dark before the app boots`; task 2, `paints the boot background with the dark page colour`; task 6 step 4 (the built `index.html`).
2. **The OS switches to dark while the app is open in System.** Expected: the app turns dark at once, with no reload; a User who picked Light or Dark is not affected. Task 1, `follows the device when it changes, in System only`.
3. **A value in `localStorage` the app didn't write** (`"Dark"`, `"auto"`, empty string), or storage unavailable. Expected: treated as System; the boot script doesn't throw, so the app still starts. Task 1, `treats an unknown saved value as System` and `starts in System when storage throws`.
4. **Picking an option in the desktop user menu.** Expected: the colours change at once, the menu stays open, and focus stays on the option. Task 3, `keeps the user menu open after picking an appearance`.
5. **A PrimeNG surface nobody restyled** (Toast, `p-message`, DatePicker panel, Select panel, table hover, Roster's unstyled select button) on a dark page. Expected: `#1B1D22` surfaces, `#2C2F37` borders, readable text. Task 6 step 2, the contrast script on every page, with the overlays opened.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\core\theme.service.ts` + `.spec.ts` | New | 1 |
| `client\src\app\app.ts`, `app.config.ts`, `app.config.spec.ts` | Inject `ThemeService`; `darkModeSelector: '.app-dark'` | 1 |
| `client\src\index.html`; `client\src\app\core\theme-boot.spec.ts` | Boot script and style; their spec | 1 |
| `client\src\app\theme\app-preset.ts` | `colorScheme.dark`, button / tag / togglebutton tokens | 2 |
| `client\src\styles\_tokens.scss` | New tokens, `--app-danger`, `:root.app-dark` | 2 |
| `client\src\app\theme\dark-tokens.spec.ts` | New | 2 |
| `client\src\app\features\students\ui\components\student-card\student-card.component.scss`, `client\src\styles\_global.scss` | `--app-plum` → `--app-danger` | 2 |
| `client\src\app\shared\appearance-toggle\appearance-toggle.component.{ts,scss,spec.ts}` | New | 3 |
| `client\public\i18n\he.json`, `en.json` | `shell.appearance.*` | 3 |
| `features\admin-shell\ui\components\shell-drawer\shell-drawer.component.{html,ts,scss}` | Appearance row | 3 |
| `features\admin-shell\admin-shell.component.{html,ts,scss,spec.ts}` | Menu section; role tags (4) | 3, 4 |
| `client\src\app\theme\no-hardcoded-colours.spec.ts` | New (4), widened (5) | 4, 5 |
| SCSS under `features\{admin-shell,users,students,publications,teachers,roster}` and `client\src\styles\_global.scss` | Literals → tokens | 4 |
| SCSS under `shared\` (`brand-logo`, `components\week-grid`, `week-grid-legend`, `dialogs\dialog-form.scss`), `features\student-form\`, `features\auth\` | Literals → tokens, brand mark | 5 |
| `.claude\rules\client-primeng.md`, `CLAUDE.md`, `docs\requirements.md` (§ 8.4), `docs\decisions\0009-dark-mode.md`, this README | Docs, As built | 6 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 0 | - | Commit this plan and `Driving Lesson Mockup\Admin Mobile.html` on the branch | ✅ own commit |
| 1 | [task-01-theme-switch.md](task-01-theme-switch.md) | `ThemeService`, PrimeNG selector, boot script and style | ✅ own commit |
| 2 | [task-02-dark-preset-and-tokens.md](task-02-dark-preset-and-tokens.md) | Dark preset, token set, `--app-danger`, token guard spec | ✅ own commit |
| 3 | [task-03-appearance-control.md](task-03-appearance-control.md) | Appearance control, drawer row, user menu section | ✅ own commit |
| 4 | [task-04-admin-pages-tokens.md](task-04-admin-pages-tokens.md) | Literals → tokens in the admin features and `_global.scss`; colour guard spec | ✅ own commit |
| 5 | [task-05-shared-and-student-form-tokens.md](task-05-shared-and-student-form-tokens.md) | Literals → tokens in `shared\`, Student form, sign-in; brand mark; guard covers all of `src` | ✅ own commit |
| 6 | [task-06-sweep-docs-and-pr.md](task-06-sweep-docs-and-pr.md) | Every screen in light/dark × he/en × 360/390/1280, contrast, System live, no flash, suites, docs, PR | ✅ PR |

## Execution

Native (`superpowers:executing-plans`) is recommended. Tasks 3 to 6 end in browser-pane checks, which only this session can take (Decision 16).

## As built (after execution)

Where the code differs from the tasks below, and why: *(filled in by task 6)*
