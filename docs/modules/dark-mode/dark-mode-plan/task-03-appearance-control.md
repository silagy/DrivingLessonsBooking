# Task 3: The Appearance control

Part of [Dark mode](README.md). Read the README's Global Constraints and Decisions 11 to 14 first. Design: frames 01 / D01 (drawer footer) and 10 / D10 (desktop user menu).

**Files:**
- Create: `client\src\app\shared\appearance-toggle\appearance-toggle.component.ts`, `.scss`, `.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`shell.appearance`)
- Modify: `client\src\app\features\admin-shell\ui\components\shell-drawer\shell-drawer.component.{html,ts,scss}`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.{html,ts,scss,spec.ts}`

**Interfaces:**
- Consumes: `ThemeService.mode()`, `ThemeService.use(mode)` and `ThemeMode` (task 1); `LanguageService.isRtl()`; the tokens from task 2.
- Produces: `AppearanceToggleComponent`, selector `app-appearance-toggle`, input `fill = input(false)`. Nothing else depends on it.

- [ ] **Step 1: Add the strings**

Under `shell` in both files, add `"appearance": { "label", "light", "dark", "system" }` with the README's values: he "מראה", "בהיר", "כהה", "מערכת"; en "Appearance", "Light", "Dark", "System".

- [ ] **Step 2: Write the failing component spec**

Write `appearance-toggle.component.spec.ts` in the style of `language-toggle.component.spec.ts`: zoneless, `providePrimeNG()`, `TranslocoTestingModule` with `en: { shell: { appearance: { label: 'Appearance', light: 'Light', dark: 'Dark', system: 'System' } } }`, a fake `ThemeService` `{ mode: signal<ThemeMode>('system'), use: vi.fn() }` and a fake `LanguageService` `{ isRtl: signal(rtl) }`. Options are the `.p-togglebutton` elements.

```typescript
it('offers Light, Dark and System, in that order', ...);
// option texts ['Light', 'Dark', 'System']

it('shows the current mode as pressed', ...);
// mode 'dark' → aria-pressed ['false', 'true', 'false']

it('applies the picked mode', ...);
// click 'Light' → use called once with 'light'

it('keeps the current mode when it is picked again', ...);
// mode 'dark', click 'Dark' → use not called

it('names the group', ...);
// p-selectbutton has role 'group' and aria-label 'Appearance'

it('moves focus with the arrow keys, mirrored in Hebrew', ...);
// rtl: focus option 0, keydown ArrowLeft → option 1 focused; from option 0, ArrowRight → option 2 (wraps)
// ltr: focus option 0, ArrowRight → option 1

it('jumps to the first and last option with Home and End', ...);

it('moves focus without changing the mode', ...);
// after the arrow keys above, use not called
```

- [ ] **Step 3: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/shared/appearance-toggle/appearance-toggle.component.spec.ts`
Expected: FAIL, the component doesn't exist.

- [ ] **Step 4: Implement `AppearanceToggleComponent`**

- **Template:** one `p-selectbutton` with:
  - `[options]="options"`, where options is a static list of `{ mode: ThemeMode; labelKey: string }` for light, dark and system;
  - `optionLabel="labelKey"`, `optionValue="mode"`, `[allowEmpty]="false"`;
  - `[ngModel]="theme.mode()"` and `(ngModelChange)="theme.use($event)"`;
  - `[attr.aria-label]="'shell.appearance.label' | transloco"`, `[class.appearance--fill]="fill()"`, `(keydown)="onKeydown($event)"`;
  - an `#item` template that renders `option.labelKey | transloco`.
- **`onKeydown(event: KeyboardEvent): void`:**
  - Only act on ArrowLeft, ArrowRight, Home and End.
  - The options are the host's `[role="button"]` elements; the current one is `document.activeElement`.
  - ArrowRight goes to `+1` in LTR and `-1` in RTL, ArrowLeft the opposite, wrapping around. Home goes to index 0, End to the last.
  - `preventDefault()`, then `focus()` the target. Never call `use()`.
- **Styles** (`::ng-deep` under `:host`, the pattern of `shared\dialogs\dialog-form.scss:50-80`):
  - Track: `--app-bg-muted`, a 1px `--app-border`, radius 999px, padding 3px, gap 2px.
  - Options: transparent, `--app-text-secondary`, 0.875rem, `min-block-size: 2.25rem`, `min-inline-size: 3.5rem`.
  - Checked: `--app-bg-card` background, `--app-ink` text, `--app-border` border, `--app-shadow-card`.
  - `:focus-visible`: a 2px `--app-brand` outline.
  - `.appearance--fill`: `display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); inline-size: 100%`, options `min-block-size: 2.125rem` and 0.84375rem.

- [ ] **Step 5: Run it to verify it passes**

Run the spec. Expected: PASS, 8 tests.

- [ ] **Step 6: Write the failing shell tests**

In `admin-shell.component.spec.ts`, add a fake `ThemeService` (`{ mode: signal('system'), use: vi.fn() }`) to `render()`'s providers, then:

```typescript
it('puts Appearance in the drawer footer, under the language', ...);
// open the drawer → in .shell-drawer__footer, .shell-drawer__appearance comes right after .shell-drawer__language
// and holds app-appearance-toggle and the text 'shell.appearance.label'

it('puts Appearance between the identity and the menu items', ...);
// click '.shell__user button' → in document, .shell-user-menu__appearance follows .shell-user-menu__header
// and precedes .p-menu-list; its toggle has the class appearance--fill

it('keeps the user menu open after picking an appearance', ...);
// open the menu, click the second .p-togglebutton in .shell-user-menu__appearance, whenStable
// → theme.use called with 'dark'; the Menu component's `visible` is still true and .p-menu is in document
```

Run them: FAIL.

- [ ] **Step 7: Add the drawer row and the menu section**

- **`shell-drawer.component.html`:** right after `.shell-drawer__language`, add `<div class="shell-drawer__appearance">` holding a `<span>` with `'shell.appearance.label' | transloco` and `<app-appearance-toggle />`. Import the component. In the SCSS, add `.shell-drawer__appearance` to the two selectors that style `.shell-drawer__language` (flex row; `justify-content: space-between`).
- **`admin-shell.component.html`**, in `#start`, after `.shell-user-menu__header`:
  - a `<div class="shell-user-menu__appearance">` holding `<span class="shell-user-menu__appearance-label">` with the label, and `<app-appearance-toggle [fill]="true" />`;
  - import the component.
- **`admin-shell.component.scss`:**
  - `.shell-user-menu__appearance`: flex column, `gap: 0.5rem`, `padding: 0.5rem 0.75rem 0.75rem`, `border-block-end: 1px solid var(--app-border)`, `margin-block-end: 0.25rem`. The header's existing bottom border is the divider above.
  - `.shell-user-menu__appearance-label`: `--app-font-display`, 700, 0.8125rem, `--app-ink`.

Run the shell spec. Expected: PASS. If the `p-menu` popup doesn't render under jsdom, replace the last two tests with assertions on the `#start` template rendered through the `Menu` instance, and rely on step 8 for the open-menu behaviour. Say so under As built.

- [ ] **Step 8: Check it in the browser**

With the dev servers running and signed in, using `javascript_tool` after `await document.fonts.ready`:
- **390 × 844, Hebrew** (D01):
  - Open the drawer. The Appearance row sits directly under the language row, with the label at the start (right) and the control at the end.
  - The options are 36px tall and read בהיר, כהה, מערכת from right to left.
  - Tap כהה: `<html>` gets `app-dark`, and the drawer stays open.
- **1280 × 800, Hebrew** (D10):
  - Open the user menu. Top to bottom: the identity header, the Appearance section (label above, three options of equal width ±1px, 34px tall), then the items.
  - Click כהה: the colours change, `.p-menu` is still in the DOM, and focus is on כהה.
  - Press ArrowLeft: focus moves to מערכת and the mode doesn't change.
- **English, 390 and 1280:** the same, mirrored (Light at the left).
- `localStorage['drivecal.theme']` holds the last pick. A reload keeps it.

- [ ] **Step 9: Run all client specs and the build**

Run `ng.js test --watch=false`, then `ng.js build --project client`. Expected: all green; `translations.spec.ts` passes (same keys in he and en); no new budget warning.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/shared/appearance-toggle client/src/app/features/admin-shell client/public/i18n
git commit -m "feat(theme): Appearance control in the drawer and the user menu"
```
