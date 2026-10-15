# Task 1: The DriveCal lockup

Part of [DriveCal branding](README.md). Read the README's Global Constraints first.

**Files:**
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.ts` (template)
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.scss` (whole file)
- Modify: `client\src\app\shared\brand-logo\brand-logo.component.spec.ts` (whole file)
- Modify: `client\public\i18n\en.json:2-5`, `client\public\i18n\he.json:2-5` (`brand` block)
- Modify: `client\src\app\core\brand-assets.spec.ts` (add one helper and one test)

**Interfaces:**
- Consumes: CSS vars `--app-brand`, `--app-ink`, `--app-on-accent`, `--app-font-display`, `--app-font-text`, `--app-text-secondary`, `--app-wordmark-tracking` (all exist in `client\src\styles\_tokens.scss`).
- Produces:
  - `BrandLogoComponent` (selector `app-brand-logo`), inputs unchanged: `showName = input(true)`, `showTagline = input(false)`.
  - DOM hooks used by task 3: `svg.brand__mark` containing `rect.brand__page`, `path.brand__rings`, two `circle.brand__hole`, `path.brand__lane`; `.brand__name` containing `.brand__accent`; `.brand__tagline`.
  - Translation keys `brand.nameLead`, `brand.nameAccent` (new) and `brand.appName` = `"DriveCal"` / `"דרייבקל"` (task 2 relies on this value for the tab title).

- [ ] **Step 1: Write the failing component spec**

Replace `client\src\app\shared\brand-logo\brand-logo.component.spec.ts` with:

```typescript
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { BrandLogoComponent } from './brand-logo.component';

const BRAND = {
    appName: 'DriveCal',
    nameLead: 'Drive',
    nameAccent: 'Cal',
    tagline: 'Weekly lesson demand, in one link',
};

async function renderLogo(inputs: Partial<Record<'showName' | 'showTagline', boolean>> = {}): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { brand: BRAND } },
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

function attributes(element: Element | null | undefined, names: string[]): (string | null)[] {
    return names.map(name => element?.getAttribute(name) ?? null);
}

describe('BrandLogoComponent', () => {
    it('draws the solid calendar mark, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(attributes(mark, ['viewBox', 'aria-hidden'])).toEqual(['0 0 40 40', 'true']);
        expect(attributes(mark?.querySelector('rect.brand__page'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['1', '5.5', '38', '33.5', '10']);
        expect(attributes(mark?.querySelector('path.brand__rings'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M12.5 1.8v7M27.5 1.8v7', '3.4', 'round']);
        expect(Array.from(mark?.querySelectorAll('circle.brand__hole') ?? [], hole => attributes(hole, ['cx', 'cy', 'r'])))
            .toEqual([['12.5', '9.5', '1.6'], ['27.5', '9.5', '1.6']]);
        expect(attributes(mark?.querySelector('path.brand__lane'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M20 15.5v4M20 23.5v4M20 31.5v3', '3.2', 'round']);
    });

    it('leaves every color of the mark to the stylesheet', async () => {
        //given
        const logo = await renderLogo();

        //when
        const markup = logo.querySelector('svg.brand__mark')?.outerHTML ?? '';

        //then
        expect(markup).not.toMatch(/#[0-9a-f]{3,6}\b|\bfill="|\bstroke="/i);
    });

    it('names the product in two tones, as one word', async () => {
        //given
        const logo = await renderLogo();

        //when
        const name = logo.querySelector('.brand__name');

        //then
        expect(name?.textContent).toBe('DriveCal');
        expect(name?.querySelector('.brand__accent')?.textContent).toBe('Cal');
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

`toBe('DriveCal')` on the raw `textContent` (no `trim()`) is deliberate: it is the Review Focus 1 guard against a space between the halves.

- [ ] **Step 2: Write the failing key-consistency spec**

In `client\src\app\core\brand-assets.spec.ts`, add this helper after `icoImages`:

```typescript
function brandKeys(lang: string): { appName: string; nameLead: string; nameAccent: string } {
    return JSON.parse(readFileSync(`public/i18n/${lang}.json`, 'utf8')).brand;
}
```

and this test as the last one inside `describe('brand assets', ...)`:

```typescript
    it('spells the wordmark halves into the product name in both languages', () => {
        //when
        const names = ['en', 'he'].map(brandKeys).map(brand => [brand.appName, brand.nameLead + brand.nameAccent]);

        //then
        expect(names).toEqual([['DriveCal', 'DriveCal'], ['דרייבקל', 'דרייבקל']]);
    });
```

- [ ] **Step 3: Run both specs to verify they fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts --include src/app/core/brand-assets.spec.ts
```

Expected: FAIL.
- `brand-logo`: `draws the solid calendar mark` fails (there is no `rect.brand__page`; today's rect is `x="2"`, unclassed); `leaves every color of the mark to the stylesheet` fails (today's markup has `fill="none" stroke="currentColor"`); `names the product in two tones` fails (no `.brand__accent`). The three tagline / name-hidden tests pass.
- `brand-assets`: `spells the wordmark halves` fails (`["Slotline", NaN]`). The other four tests pass for now; task 2 changes them.

- [ ] **Step 4: Add the translation keys**

`client\public\i18n\en.json`, the `brand` block becomes:

```json
  "brand": {
    "appName": "DriveCal",
    "nameLead": "Drive",
    "nameAccent": "Cal",
    "tagline": "Weekly lesson demand, in one link"
  },
```

`client\public\i18n\he.json`, the `brand` block becomes:

```json
  "brand": {
    "appName": "דרייבקל",
    "nameLead": "דרייב",
    "nameAccent": "קל",
    "tagline": "ביקוש שבועי לשיעורים, בקישור אחד"
  },
```

- [ ] **Step 5: Draw the Solid calendar and the two-tone name**

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
        <rect class="brand__page" x="1" y="5.5" width="38" height="33.5" rx="10" />
        <path class="brand__rings" d="M12.5 1.8v7M27.5 1.8v7" stroke-width="3.4" stroke-linecap="round" />
        <circle class="brand__hole" cx="12.5" cy="9.5" r="1.6" />
        <circle class="brand__hole" cx="27.5" cy="9.5" r="1.6" />
        <path class="brand__lane" d="M20 15.5v4M20 23.5v4M20 31.5v3" stroke-width="3.2" stroke-linecap="round" />
      </svg>
      @if (showName()) {
        @let lead = 'brand.nameLead' | transloco;
        @let accent = 'brand.nameAccent' | transloco;
        <span class="brand__word">
          <span class="brand__name">{{ lead }}<span class="brand__accent">{{ accent }}</span></span>
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

Keep `{{ lead }}<span class="brand__accent">` on one line with nothing between them: Angular collapses a line break there into a space, which would render "Drive Cal".

- [ ] **Step 6: Color the mark and the name**

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

.brand__page {
  fill: currentColor;
}

.brand__rings {
  fill: none;
  stroke: currentColor;
}

.brand__hole {
  fill: var(--app-on-accent);
}

.brand__lane {
  fill: none;
  stroke: var(--app-on-accent);
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
  white-space: nowrap;
  color: var(--app-ink);
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
  inline-size: 2.5rem;
  block-size: 2.5rem;
}

.brand--lockup .brand__name {
  font-size: 1.625rem;
}
```

- [ ] **Step 7: Run both specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/brand-logo/brand-logo.component.spec.ts --include src/app/core/brand-assets.spec.ts
```

Expected: PASS, all 6 `brand-logo` tests and all 5 `brand-assets` tests.

- [ ] **Step 8: Run the full client suite**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: PASS. `language.service.spec.ts` uses its own fake translations ("Slotline"), so it is unaffected; task 2 renames them. `translations.spec.ts` and `source-text.spec.ts` stay green (no dash or ellipsis characters were added).

- [ ] **Step 9: Commit**

```bash
git add client/src/app/shared/brand-logo/brand-logo.component.ts client/src/app/shared/brand-logo/brand-logo.component.scss client/src/app/shared/brand-logo/brand-logo.component.spec.ts client/src/app/core/brand-assets.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(brand): DriveCal solid-calendar logo and two-tone wordmark in every header"
```

End the commit message with the attribution line from the session's attribution reminder.
