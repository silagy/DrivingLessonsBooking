# Task 1: The Slotline lockup

Part of [Slotline branding](README.md). Read the README's Global Constraints first.

**Files:**
- Create: `client\src\app\shared\brand-logo\brand-logo.component.spec.ts`
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.ts` (whole file)
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.scss` (whole file)
- Modify: `client\src\styles\_tokens.scss` (two new tokens, one reset)
- Modify: `client\public\i18n\en.json:2-4`, `client\public\i18n\he.json:2-4` (`brand` block)
- Modify: `client\src\app\features\auth\login.component.html:3`, `client\src\app\features\auth\login.component.scss` (`.login__top`)

**Interfaces:**
- Consumes: Transloco keys `brand.appName`, `brand.tagline`; CSS vars `--p-sky-500`, `--app-font-display`, `--app-font-text`, `--app-text-secondary`.
- Produces:
  - `BrandLogoComponent` (selector `app-brand-logo`), inputs `showName: InputSignal<boolean>` (default `true`, unchanged) and `showTagline: InputSignal<boolean>` (default `false`, new).
  - DOM: `svg.brand__mark` (the Lane tile), `.brand__name`, `.brand__tagline` (only when `showTagline` is true), `.brand--lockup` on `.brand` when `showTagline` is true.
  - CSS tokens `--app-brand` and `--app-wordmark-tracking`.
  - Translation key `brand.tagline` (both files). Task 2 makes `brand.appName` the tab title too.

- [ ] **Step 1: Write the failing spec**

Create `client\src\app\shared\brand-logo\brand-logo.component.spec.ts`:

```typescript
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { BrandLogoComponent } from './brand-logo.component';

async function renderLogo(inputs: Partial<Record<'showName' | 'showTagline', boolean>> = {}): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { brand: { appName: 'Slotline', tagline: 'Weekly lesson demand, in one link' } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });
    const fixture = TestBed.createComponent(BrandLogoComponent);
    Object.entries(inputs).forEach(([name, value]) => fixture.componentRef.setInput(name, value));
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('BrandLogoComponent', () => {
    it('draws the lane tile mark, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(mark?.getAttribute('viewBox')).toBe('0 0 40 40');
        expect(mark?.getAttribute('aria-hidden')).toBe('true');
        expect(mark?.querySelector('rect')?.getAttribute('rx')).toBe('10');
        expect(mark?.querySelector('path')?.getAttribute('d')).toBe('M20 9v5M20 17.5v5M20 26v5');
    });

    it('names the product in the active language', async () => {
        //given
        const logo = await renderLogo();

        //when
        const name = logo.querySelector('.brand__name');

        //then
        expect(name?.textContent?.trim()).toBe('Slotline');
    });

    it('shows no tagline unless asked', async () => {
        //given
        const logo = await renderLogo();

        //when
        const tagline = logo.querySelector('.brand__tagline');

        //then
        expect(tagline).toBeNull();
        expect(logo.querySelector('.brand--lockup')).toBeNull();
    });

    it('shows the tagline under the name in the lockup', async () => {
        //given
        const logo = await renderLogo({ showTagline: true });

        //when
        const tagline = logo.querySelector('.brand--lockup .brand__tagline');

        //then
        expect(tagline?.textContent?.trim()).toBe('Weekly lesson demand, in one link');
    });

    it('keeps the mark when the name is hidden', async () => {
        //given
        const logo = await renderLogo({ showName: false, showTagline: true });

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(mark).not.toBeNull();
        expect([logo.querySelector('.brand__name'), logo.querySelector('.brand__tagline')]).toEqual([null, null]);
    });
});
```

- [ ] **Step 2: Run it to verify it fails**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts
```

Expected: FAIL. `draws the lane tile mark` fails because there is no `svg.brand__mark` (`mark` is `null`). The two `showTagline` tests fail with `NG0303` (the component has no `showTagline` input). `names the product` and `shows no tagline unless asked` pass already.

- [ ] **Step 3: Add the tokens**

In `client\src\styles\_tokens.scss`, after the `--app-on-accent` line add:

```scss
  --app-brand: var(--p-sky-500);
  --app-wordmark-tracking: -0.025em;
```

and change the Hebrew reset at the end of the file to:

```scss
:root[lang='he'] {
  --app-caps-tracking: 0;
  --app-wordmark-tracking: 0;
}
```

- [ ] **Step 4: Replace the component**

`client\src\app\shared\brand-logo\brand-logo.component.ts`, whole file:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand-logo',
  imports: [TranslocoPipe],
  template: `
    <span class="brand" [class.brand--lockup]="showTagline()">
      <svg class="brand__mark" viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <rect x="2" y="2" width="36" height="36" rx="10" fill="none" stroke="currentColor" stroke-width="3" />
        <path d="M20 9v5M20 17.5v5M20 26v5" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
      </svg>
      @if (showName()) {
        <span class="brand__word">
          <span class="brand__name">{{ 'brand.appName' | transloco }}</span>
          @if (showTagline()) {
            <span class="brand__tagline">{{ 'brand.tagline' | transloco }}</span>
          }
        </span>
      }
    </span>
  `,
  styleUrl: './brand-logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandLogoComponent {
  readonly showName = input(true);
  readonly showTagline = input(false);
}
```

`client\src\app\shared\brand-logo\brand-logo.component.scss`, whole file:

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
  color: var(--app-brand);
}

.brand__mark {
  flex: none;
  inline-size: 1.75rem;
  block-size: 1.75rem;
}

.brand__word {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  min-inline-size: 0;
}

.brand__name {
  font-family: var(--app-font-display);
  font-weight: 700;
  font-size: 1.2rem;
  line-height: 1;
  letter-spacing: var(--app-wordmark-tracking);
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
  inline-size: 2.5rem;
  block-size: 2.5rem;
}

.brand--lockup .brand__name {
  font-size: 1.625rem;
}
```

- [ ] **Step 5: Add the translations**

`client\public\i18n\en.json`, replace the `brand` block:

```json
  "brand": {
    "appName": "Slotline",
    "tagline": "Weekly lesson demand, in one link"
  },
```

`client\public\i18n\he.json`, replace the `brand` block:

```json
  "brand": {
    "appName": "סלוטליין",
    "tagline": "ביקוש שבועי לשיעורים, בקישור אחד"
  },
```

Write these with the Edit tool, not through a shell, so the Hebrew stays UTF-8.

- [ ] **Step 6: Turn the tagline on for sign-in**

`client\src\app\features\auth\login.component.html:3`:

```html
    <app-brand-logo [showTagline]="true" />
```

In `client\src\app\features\auth\login.component.scss`, add a gap to `.login__top` so a wrapped tagline never touches the language toggle:

```scss
.login__top {
  position: absolute;
  inset-block-start: 1.75rem;
  inset-inline: 2.25rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}
```

- [ ] **Step 7: Run the spec to verify it passes**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts
```

Expected: PASS, 5 tests.

- [ ] **Step 8: Run the full client suite**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: PASS. `translations.spec.ts` and `source-text.spec.ts` stay green (the Hebrew tagline uses a plain comma). If a login or shell spec asserted "WeekDrive", update its expected text to "Slotline".

- [ ] **Step 9: Commit**

```bash
git add client/src/app/shared/brand-logo client/src/styles/_tokens.scss client/public/i18n/en.json client/public/i18n/he.json client/src/app/features/auth/login.component.html client/src/app/features/auth/login.component.scss
git commit -m "feat(brand): Slotline lane-tile logo and wordmark in every header, tagline on sign-in"
```
