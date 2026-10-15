# Task 1: The Option E lockup

Part of [DriveCal logo, Option E](README.md). Read the README's Global Constraints and Decisions first.

**Files:**
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.spec.ts` (mark tests)
- Modify: `client\src\app\core\brand-assets.spec.ts` (font and token tests)
- Modify: `client\package.json`, `client\package-lock.json` (new `@fontsource-variable/rubik`, written by npm)
- Modify: `client\src\styles\_global.scss` (Rubik `@font-face`)
- Modify: `client\src\styles\_tokens.scss` (brand and wordmark tokens)
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.ts` (template)
- Replace: `client\src\app\shared\brand-logo\brand-logo.component.scss`
- Modify: `client\src\app\features\auth\login.component.scss` (backdrop override)

**Interfaces:**
- Consumes: translation keys `brand.nameLead`, `brand.nameAccent`, `brand.tagline` (unchanged); palette variables `--p-sky-300`, `--p-sky-500`, `--p-sky-700`, `--p-surface-0`; tokens `--app-bg-card`, `--app-bg-page`, `--app-on-accent`, `--app-text-secondary`, `--app-font-text`.
- Produces: `<app-brand-logo [showName] [showTagline]>` with the same inputs as today. DOM hooks for task 3: `svg.brand__mark` > `rect.brand__calendar`, `rect.brand__page`, `path.brand__rings`, `path.brand__check`, `circle.brand__halo`, `svg.brand__wheel` > `circle.brand__disc`, `path.brand__window` (3), `circle.brand__hub`; `.brand__name`, `.brand__accent`, `.brand__tagline`. CSS custom property `--brand-logo-backdrop` (default `--app-bg-card`). Global tokens `--app-brand-deep`, `--app-brand-soft`, `--app-font-wordmark`, `--app-wordmark-style`; `--app-wordmark-tracking` retuned. CSS font family `'Rubik'` (Hebrew range only).

- [ ] **Step 1: Write the failing mark tests**

In `client\src\app\shared\brand-logo\brand-logo.component.spec.ts`, replace the first test, `draws the solid calendar mark, hidden from assistive technology`, with these three tests. Leave the other five tests (`leaves every color of the mark to the stylesheet`, `names the product in two tones, as one word`, `shows no tagline unless asked`, `shows the tagline under the name in the lockup`, `keeps the mark when the name is hidden`) exactly as they are.

```typescript
    it('draws the calendar with a checked day, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(attributes(mark, ['viewBox', 'aria-hidden'])).toEqual(['0 0 40 40', 'true']);
        expect(attributes(mark?.querySelector('rect.brand__calendar'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['13', '5', '25', '24', '5']);
        expect(attributes(mark?.querySelector('rect.brand__page'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['15.5', '11', '20', '15.5', '2.5']);
        expect(attributes(mark?.querySelector('path.brand__rings'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8', '2.4', 'round']);
        expect(attributes(mark?.querySelector('path.brand__check'), ['d', 'stroke-width', 'stroke-linecap', 'stroke-linejoin']))
            .toEqual(['M26.6 17.6l2.4 2.4 4.2-4.6', '2.3', 'round', 'round']);
        expect(attributes(mark?.querySelector('circle.brand__halo'), ['cx', 'cy', 'r'])).toEqual(['15', '25.5', '14.2']);
    });

    it('draws the steering wheel in its own 40-unit box', async () => {
        //given
        const logo = await renderLogo();

        //when
        const wheel = logo.querySelector('svg.brand__mark svg.brand__wheel');

        //then
        expect(attributes(wheel, ['x', 'y', 'width', 'height', 'viewBox'])).toEqual(['2', '12.5', '26', '26', '0 0 40 40']);
        expect(attributes(wheel?.querySelector('circle.brand__disc'), ['cx', 'cy', 'r'])).toEqual(['20', '20', '19']);
        expect(Array.from(wheel?.querySelectorAll('path.brand__window') ?? [], window => window.getAttribute('d'))).toEqual([
            'M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z',
            'M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z',
            'M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z',
        ]);
        expect(attributes(wheel?.querySelector('circle.brand__hub'), ['cx', 'cy', 'r', 'stroke-width']))
            .toEqual(['20', '21.5', '4.6', '2.2']);
    });

    it('paints the halo between the calendar and the wheel', async () => {
        //given
        const logo = await renderLogo();

        //when
        const parts = Array.from(logo.querySelector('svg.brand__mark')?.children ?? [], part => part.getAttribute('class'));

        //then
        expect(parts).toEqual(['brand__calendar', 'brand__page', 'brand__rings', 'brand__check', 'brand__halo', 'brand__wheel']);
    });
```

- [ ] **Step 2: Write the failing font and token tests**

In `client\src\app\core\brand-assets.spec.ts`, change the first import line to:

```typescript
import { existsSync, readFileSync } from 'node:fs';
```

and add these two tests at the end of the `describe('brand assets', ...)` block, after `spells the wordmark halves into the product name in both languages`:

```typescript
    it('self-hosts rubik for the hebrew wordmark only', () => {
        //when
        const styles = readFileSync('src/styles/_global.scss', 'utf8');
        const face = styles.split('@font-face').find(block => block.includes("font-family: 'Rubik'")) ?? '';

        //then
        expect(face).toContain('@fontsource-variable/rubik/files/rubik-hebrew-wght-normal.woff2');
        expect(face).toContain('unicode-range: U+0307-0308, U+0590-05FF, U+200C-2010, U+20AA, U+25CC, U+FB1D-FB4F;');
        expect(existsSync('node_modules/@fontsource-variable/rubik/files/rubik-hebrew-wght-normal.woff2')).toBe(true);
    });

    it('sets the wordmark italic in english and upright in hebrew', () => {
        //when
        const [root, hebrew] = readFileSync('src/styles/_tokens.scss', 'utf8').split(":root[lang='he']");

        //then
        expect(root).toContain("--app-font-wordmark: 'Rubik',");
        expect(root).toContain('--app-wordmark-style: italic;');
        expect(hebrew).toContain('--app-wordmark-style: normal;');
    });
```

- [ ] **Step 3: Run both specs to verify they fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts --include src/app/core/brand-assets.spec.ts
```

Expected: FAIL.
- `draws the calendar with a checked day` fails: there is no `rect.brand__calendar` (the attributes come back `null`).
- `draws the steering wheel in its own 40-unit box` fails: no `svg.brand__wheel`.
- `paints the halo between the calendar and the wheel` fails: the parts are `brand__page`, `brand__rings`, `brand__hole`, `brand__hole`, `brand__lane`.
- `self-hosts rubik for the hebrew wordmark only` and `sets the wordmark italic in english and upright in hebrew` fail.
- Every other test passes.

- [ ] **Step 4: Install Rubik**

From `client\`:

```powershell
& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" install @fontsource-variable/rubik@^5.3.0
```

Expected: `package.json` `dependencies` gains `"@fontsource-variable/rubik": "^5.3.0"` next to `@fontsource-variable/noto-sans-hebrew`, and `package-lock.json` changes. Confirm the Hebrew file is there:

```powershell
Test-Path node_modules\@fontsource-variable\rubik\files\rubik-hebrew-wght-normal.woff2
```

Expected: `True`. `EBADENGINE` warnings are harmless. If `package.json` shows any other dependency change, revert it and rerun; only Rubik may be added.

- [ ] **Step 5: Register the Rubik face**

In `client\src\styles\_global.scss`, insert this block right after the existing Noto Sans Hebrew `@font-face` (after line 9), with one blank line before and after:

```scss
@font-face {
  font-family: 'Rubik';
  font-style: normal;
  font-display: swap;
  font-weight: 300 900;
  src: url('../../node_modules/@fontsource-variable/rubik/files/rubik-hebrew-wght-normal.woff2')
    format('woff2-variations');
  unicode-range: U+0307-0308, U+0590-05FF, U+200C-2010, U+20AA, U+25CC, U+FB1D-FB4F;
}
```

- [ ] **Step 6: Add the brand and wordmark tokens**

In `client\src\styles\_tokens.scss`, replace these two lines in `:root`:

```scss
  --app-brand: var(--p-sky-500);
  --app-wordmark-tracking: -0.025em;
```

with:

```scss
  --app-brand: var(--p-sky-500);
  --app-brand-deep: var(--p-sky-700);
  --app-brand-soft: var(--p-sky-300);
  --app-font-wordmark: 'Rubik', 'Helvetica Now Display', 'Helvetica Neue', Arial, 'Liberation Sans', sans-serif;
  --app-wordmark-style: italic;
  --app-wordmark-tracking: -0.02em;
```

and replace the `:root[lang='he']` block with:

```scss
:root[lang='he'] {
  --app-caps-tracking: 0;
  --app-wordmark-style: normal;
  --app-wordmark-tracking: -0.01em;
}
```

- [ ] **Step 7: Draw the Option E mark**

In `client\src\app\shared\brand-logo\brand-logo.component.ts`, replace the whole `<svg class="brand__mark" ...>...</svg>` element (the five Solid-calendar shapes) with:

```html
      <svg class="brand__mark" viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <rect class="brand__calendar" x="13" y="5" width="25" height="24" rx="5" />
        <rect class="brand__page" x="15.5" y="11" width="20" height="15.5" rx="2.5" />
        <path class="brand__rings" d="M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8" stroke-width="2.4" stroke-linecap="round" />
        <path class="brand__check" d="M26.6 17.6l2.4 2.4 4.2-4.6" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" />
        <circle class="brand__halo" cx="15" cy="25.5" r="14.2" />
        <svg class="brand__wheel" x="2" y="12.5" width="26" height="26" viewBox="0 0 40 40">
          <circle class="brand__disc" cx="20" cy="20" r="19" />
          <path class="brand__window" d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z" />
          <path class="brand__window" d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z" />
          <path class="brand__window" d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z" />
          <circle class="brand__hub" cx="20" cy="21.5" r="4.6" stroke-width="2.2" />
        </svg>
      </svg>
```

Keep the 6-space indent of the surrounding template. Nothing else in the component file changes: the wordmark markup, the `@let` lines, the inputs and the class stay.

- [ ] **Step 8: Style the mark and the wordmark**

Replace `client\src\app\shared\brand-logo\brand-logo.component.scss` with:

```scss
:host {
  display: inline-flex;
  min-inline-size: 0;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  min-inline-size: 0;
}

.brand__mark {
  flex: none;
  inline-size: 2rem;
  block-size: 2rem;
}

.brand__calendar {
  fill: var(--app-brand);
}

.brand__page {
  fill: var(--app-on-accent);
}

.brand__rings {
  fill: none;
  stroke: var(--app-brand-deep);
}

.brand__check {
  fill: none;
  stroke: var(--app-brand);
}

.brand__halo,
.brand__window {
  fill: var(--brand-logo-backdrop, var(--app-bg-card));
}

.brand__disc {
  fill: var(--app-brand-deep);
}

.brand__hub {
  fill: none;
  stroke: var(--app-brand-soft);
}

.brand__word {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  min-inline-size: 0;
}

.brand__name {
  font-family: var(--app-font-wordmark);
  font-weight: 800;
  font-style: var(--app-wordmark-style);
  font-size: 1.2rem;
  line-height: 1;
  letter-spacing: var(--app-wordmark-tracking);
  white-space: nowrap;
  color: var(--app-brand-deep);
}

.brand__accent {
  color: var(--app-brand);
}

.brand__tagline {
  font-family: var(--app-font-text);
  font-size: 0.8125rem;
  line-height: 1.3;
  color: var(--app-text-secondary);
  text-wrap: pretty;
}

.brand--lockup {
  gap: 0.75rem;
}

.brand--lockup .brand__mark {
  inline-size: 2.625rem;
  block-size: 2.625rem;
}

.brand--lockup .brand__name {
  font-size: 1.625rem;
}
```

What changed from today: `.brand` no longer sets `color` (nothing uses `currentColor` now); the mark grows to 2rem / 2.625rem (README decision 7); the five Solid-calendar rules become the Option E rules; `.brand__name` takes the wordmark font, weight 800, the style token and `--app-brand-deep`.

- [ ] **Step 9: Give the sign-in logo its backdrop**

The sign-in top bar sits on `.login` (`--app-bg-page`), not on a card. In `client\src\app\features\auth\login.component.scss`, add this rule right after the `.login__top { ... }` block, with a blank line before it:

```scss
.login__top app-brand-logo {
  --brand-logo-backdrop: var(--app-bg-page);
}
```

The admin bar and the student-form bar are `--app-bg-card`, the default, so their files don't change.

- [ ] **Step 10: Run both specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts --include src/app/core/brand-assets.spec.ts
```

Expected: PASS, all 8 `BrandLogoComponent` tests and all 7 `brand assets` tests. (`draws the solid calendar in the svg favicon` still passes; task 2 replaces it.)

- [ ] **Step 11: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS. Build succeeds, and `dist\client\browser` holds a `rubik-hebrew-wght-normal*.woff2` file, as it does for Noto (that proves the `url()` path resolved):

```powershell
Get-ChildItem dist\client\browser -Recurse -Filter "rubik-hebrew-wght-normal*.woff2" | Select-Object Name, Length
```

If the build reports `Can't resolve '../../node_modules/@fontsource-variable/rubik/...'`, the path in step 5 is wrong; compare it character by character with the Noto `src` line above it.

- [ ] **Step 12: Commit**

```bash
git add client/package.json client/package-lock.json client/src/styles/_global.scss client/src/styles/_tokens.scss client/src/app/shared/brand-logo/brand-logo.component.ts client/src/app/shared/brand-logo/brand-logo.component.scss client/src/app/shared/brand-logo/brand-logo.component.spec.ts client/src/app/core/brand-assets.spec.ts client/src/app/features/auth/login.component.scss
git commit -m "feat(brand): Option E wheel-and-calendar logo with the navy italic wordmark and Rubik for Hebrew"
```

End the commit message with the attribution line from the session's attribution reminder.
