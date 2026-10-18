# Task 2: Dark preset and tokens

Part of [Dark mode](README.md). Read the README's Global Constraints and Decisions 4 to 7 first.

**Files:**
- Modify: `client\src\app\theme\app-preset.ts`
- Modify: `client\src\styles\_tokens.scss`
- Create: `client\src\app\theme\dark-tokens.spec.ts`
- Modify: `client\src\app\features\students\ui\components\student-card\student-card.component.scss:113`, `client\src\styles\_global.scss:97` (`--app-plum` → `--app-danger`)

**Interfaces:**
- Consumes: `html.app-dark` and `darkModeSelector: '.app-dark'` from task 1.
- Produces, for tasks 3 to 5, these tokens with a value in `:root` and in `:root.app-dark`:
  - existing: `--app-ink`, `--app-text-secondary`, `--app-text-muted`, `--app-bg-page`, `--app-bg-muted`, `--app-bg-card`, `--app-border`, `--app-slot-unavailable`, `--app-on-accent`, `--app-brand`, `--app-brand-deep`, `--app-brand-soft`, `--app-steel-light`, `--app-whale`, `--app-shadow-card`, `--app-shadow-lift`, `--app-scrim`;
  - renamed: `--app-danger` (was `--app-plum`);
  - new: `--app-border-interactive`, `--app-tag-sky-bg/-fg/-border`, `--app-tag-ocean-bg/-fg/-border`, `--app-tag-green-bg/-fg`, `--app-row-new-bg/-border`, `--app-row-muted-bg`, `--app-selected-bg/-border`, `--app-error`, `--app-error-strong`, `--app-error-bg`, `--app-error-bg-strong`, `--app-error-border`, `--app-success`, `--app-info`.

- [ ] **Step 1: Write the failing token spec**

Write `dark-tokens.spec.ts`. It reads `src/styles/_tokens.scss` and `src/index.html` with `readFileSync`, and parses the `:root { ... }` and `:root.app-dark { ... }` blocks into `Map<string, string>` (declarations match `/(--app-[\w-]+)\s*:\s*([^;]+);/g`; values may span lines). A small `contrast(fg: string, bg: string): number` helper implements the WCAG relative-luminance ratio for `#RRGGBB`.

```typescript
const NOT_COLOURS = ['--app-font-display', '--app-font-text', '--app-font-wordmark', '--app-wordmark-style',
    '--app-wordmark-tracking', '--app-radius-card', '--app-radius-field', '--app-caps-tracking', '--app-grad-sky'];

it('gives every colour token a dark value', ...);
// every key of light minus NOT_COLOURS is a key of dark; report the missing ones

it('keeps dark body text readable', ...);
// ≥ 4.5 for each fg of [ink, text-secondary, text-muted, brand, danger] on [bg-page, bg-card],
// text-muted on bg-muted, brand-deep on selected-bg and on row-new-bg, whale on steel-light

it('keeps dark tags readable', ...);
// ≥ 3 for tag-sky-fg on tag-sky-bg, tag-ocean-fg on tag-ocean-bg, tag-green-fg on tag-green-bg

it('paints the boot background with the dark page colour', ...);
// the hex in index.html's `html.app-dark { ... background: #...; }` equals dark --app-bg-page, ignoring case

it('no longer has a Plum alias', ...);
// light has no '--app-plum'; light '--app-danger' is 'var(--p-plum-500)'
```

Report failures with the token names and ratios in the message, so a failing run says what to fix.

- [ ] **Step 2: Run it to verify it fails**

Run: `ng.js test --watch=false --include src/app/theme/dark-tokens.spec.ts`
Expected: FAIL. There is no `:root.app-dark` block.

- [ ] **Step 3: Add the light tokens to `:root`**

In `_tokens.scss`, replace `--app-plum: var(--p-plum-500);` with `--app-danger: var(--p-plum-500);` and add (Decisions 6 and 7):

```scss
  --app-border-interactive: var(--p-steel-200);

  --app-tag-sky-bg: var(--p-sky-50);
  --app-tag-sky-fg: var(--p-sky-700);
  --app-tag-sky-border: var(--p-sky-100);
  --app-tag-ocean-bg: var(--p-ocean-50);
  --app-tag-ocean-fg: var(--p-ocean-700);
  --app-tag-ocean-border: var(--p-ocean-100);
  --app-tag-green-bg: var(--p-green-100);
  --app-tag-green-fg: var(--p-green-700);

  --app-row-new-bg: var(--p-sky-50);
  --app-row-new-border: var(--p-sky-100);
  --app-row-muted-bg: var(--app-bg-page);
  --app-selected-bg: var(--p-sky-50);
  --app-selected-border: var(--p-sky-500);

  --app-error: var(--p-red-600);
  --app-error-strong: var(--p-red-700);
  --app-error-bg: var(--p-red-50);
  --app-error-bg-strong: var(--p-red-100);
  --app-error-border: var(--p-red-200);
  --app-success: var(--p-green-600);
  --app-info: var(--p-blue-600);
```

- [ ] **Step 4: Add the `:root.app-dark` block**

After `:root[lang='he']`, the request's values verbatim, plus Decision 6's:

```scss
:root.app-dark {
  --app-ink: #F5F6FF;
  --app-text-secondary: #A9B1C2;
  --app-text-muted: #8A93A6;

  --app-bg-page: #121418;
  --app-bg-muted: #1F2228;
  --app-bg-card: #1B1D22;
  --app-border: #2C2F37;
  --app-border-interactive: #3A4A6E;

  --app-slot-unavailable: repeating-linear-gradient(45deg, #1F2228, #1F2228 6px, #2A2D35 6px, #2A2D35 12px);

  --app-on-accent: #FFFFFF;
  --app-brand: #5C95FF;
  --app-brand-deep: #A9C6FF;
  --app-brand-soft: #5C95FF;
  --app-steel-light: #22304A;
  --app-whale: #A9C6FF;
  --app-danger: #FF8AD8;

  --app-tag-sky-bg: #132447;
  --app-tag-sky-fg: #A9C6FF;
  --app-tag-sky-border: #1E3566;
  --app-tag-ocean-bg: #0A2E31;
  --app-tag-ocean-fg: #6FE6EC;
  --app-tag-ocean-border: #12474B;
  --app-tag-green-bg: #12301D;
  --app-tag-green-fg: #86DCA0;

  --app-row-new-bg: #14213A;
  --app-row-new-border: #24395F;
  --app-row-muted-bg: #16181C;
  --app-selected-bg: #14213A;
  --app-selected-border: #5C95FF;

  --app-error: var(--p-red-400);
  --app-error-strong: var(--p-red-300);
  --app-error-bg: color-mix(in srgb, var(--p-red-500), transparent 84%);
  --app-error-bg-strong: color-mix(in srgb, var(--p-red-500), transparent 76%);
  --app-error-border: color-mix(in srgb, var(--p-red-400), transparent 60%);
  --app-success: var(--p-green-400);
  --app-info: var(--p-blue-400);

  --app-shadow-card: 0 1px 2px rgba(0, 0, 0, 0.35);
  --app-shadow-lift: 0 20px 48px rgba(0, 0, 0, 0.55);
  --app-scrim: rgba(0, 0, 0, 0.6);
}
```

Rename the two `--app-plum` consumers to `--app-danger` (`student-card.component.scss:113`, `_global.scss:97`).

- [ ] **Step 5: Run the token spec**

Expected: PASS, 5 tests.

- [ ] **Step 6: Add the dark preset**

In `app-preset.ts`:
- Extract the light primary-button object to `const gradientPrimaryButton = { background: skyGradient, ... }` and use it in `button.colorScheme.light.root.primary` and a new `button.colorScheme.dark.root.primary`.
- Add `semantic.colorScheme.dark`:
  - `primary`: `color '{sky.400}'`, `contrastColor '#ffffff'`, `hoverColor '{sky.300}'`, `activeColor '{sky.200}'`;
  - `surface`: `0 '#ffffff'`, `50 '#F5F6FF'`, `100 '#D5DAE6'`, `200 '#A9B1C2'`, `300 '#8A93A6'`, `400 '#6B7385'`, `500 '#43464D'`, `600 '#2C2F37'`, `700 '#23262C'`, `800 '#1B1D22'`, `900 '#121418'`, `950 '#0B0C0E'`;
  - `text`, `content`, `overlay` (`select`, `popover`, `modal`, plus `navigation.shadow`), `formField`, `navigation.item.focusBackground`, `list.option.focusBackground`: exactly as Decision 4.
- Add `components.tag.colorScheme`: `light.success` and `dark.success` = `{ background: 'var(--app-tag-green-bg)', color: 'var(--app-tag-green-fg)' }`; `dark.info` = `{ background: 'var(--app-tag-sky-bg)', color: 'var(--app-tag-sky-fg)' }`; `dark.secondary` = `{ background: '{surface.600}', color: '{surface.100}' }`.
- Add `components.togglebutton.colorScheme.dark` as Decision 5.

- [ ] **Step 7: Build and check the computed values**

Run `ng.js build --project client`. Expected: no errors.

Then start the dev servers (README, Dev servers), sign in, and run `localStorage.setItem('drivecal.theme','dark')` and reload. On `/publications`, with `javascript_tool`:
- `getComputedStyle(document.body).backgroundColor` is `rgb(18, 20, 24)`;
- `getComputedStyle(document.documentElement)` gives `--p-content-background` `#1b1d22`, `--p-overlay-modal-border-color` `#2c2f37`, `--p-text-muted-color` `#8a93a6`, `--p-primary-color` `#3d82ff`;
- `--p-overlay-modal-shadow` resolves to `var(--app-shadow-lift)`. If the semantic `colorScheme.dark.overlay.*.shadow` keys don't emit, set `colorScheme.dark.root.shadow` on the `dialog`, `drawer`, `menu`, `popover` and `select` components instead, and note it under As built;
- open the user menu: `.p-menu` background `rgb(27, 29, 34)`, border `rgb(44, 47, 55)`;
- a primary `p-button` still has the Sky gradient as its `background-image`.

Then `localStorage.setItem('drivecal.theme','light')` and reload. Expected: the page looks as it did before this task, including the Active tag (Aura's green-100 / green-700, now through the token).

- [ ] **Step 8: Run all client specs**

Run: `ng.js test --watch=false`. Expected: all green.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/theme client/src/styles client/src/app/features/students/ui/components/student-card/student-card.component.scss
git commit -m "feat(theme): dark preset and a dark value for every colour token"
```
