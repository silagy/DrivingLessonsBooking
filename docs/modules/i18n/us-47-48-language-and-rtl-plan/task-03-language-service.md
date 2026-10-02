# Task 3 of 8: `LanguageService` — Israeli locale, direction flag, complete PrimeNG labels, translated tab title, accessible toggle (L2, L3, L5, TDD)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–2 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Create: `client\src\app\core\primeng-translations.ts`, `client\src\app\core\primeng-translations.spec.ts`
- Modify: `client\src\app\core\language.service.ts` (whole file below)
- Create: `client\src\app\core\language.service.spec.ts`
- Modify: `client\src\app\app.config.ts` (one import path)
- Modify: `client\src\app\shared\language-toggle\language-toggle.component.ts` (whole file below)
- Create: `client\src\app\shared\language-toggle\language-toggle.component.spec.ts`
- Modify: `client\src\index.html` (the `<title>`)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (one key each: `shell.language`)

**Interfaces:**
- Consumes: the existing `LanguageService` (`lang` signal, `use()`, `toggle()`, storage key `app_lang`), the existing key `shell.title` (`Driving Lessons Planner` / `מערכת תכנון שיעורי נהיגה`).
- Produces, relied on by later tasks:
  - `type AppLocale = 'he-IL' | 'en-IL'` and `LanguageService.locale: Signal<AppLocale>` (task 4 passes it to every formatter).
  - `LanguageService.isRtl: Signal<boolean>` (tasks 5 and 6).
  - `PRIMENG_HE` / `PRIMENG_EN` now live in `core\primeng-translations.ts`; `language.service.ts` no longer exports them.
  - New key `shell.language` (`Language` / `שפה`).
  - Test fakes of `LanguageService` must now carry `locale` as well as `lang` (task 4 updates the three existing fakes).

**Why:** README defects L2, L3 and L5, decisions 6 and 7. PrimeNG renders its own labels (date-picker buttons, dialog close, empty select) from its translation object, which today only has day/month names in Hebrew. `PrimeNG.setTranslation` merges only the top level, so a nested `aria` object would replace PrimeNG's whole default `aria` set instead of adding to it: the service merges `aria` itself. Nothing names the tab, so it shows `index.html`'s English title forever. The toggle's group has no name and its Hebrew option has no `lang`.

- [ ] **Step 1: Write the failing PrimeNG-labels spec**

Create `client\src\app\core\primeng-translations.spec.ts`:

```ts
import { PRIMENG_EN, PRIMENG_HE } from './primeng-translations';

function keyPaths(value: object, prefix = ''): string[] {
    return Object.entries(value).flatMap(([key, child]) =>
        child && typeof child === 'object' && !Array.isArray(child)
            ? keyPaths(child, `${prefix}${key}.`)
            : [`${prefix}${key}`],
    );
}

function textValues(value: object): string[] {
    return Object.values(value).flatMap(child => {
        if (typeof child === 'string') {
            return [child];
        }

        return child && typeof child === 'object' ? textValues(child) : [];
    });
}

describe('PrimeNG translations', () => {
    it('gives Hebrew and English the same labels', () => {
        expect(keyPaths(PRIMENG_HE).sort()).toEqual(keyPaths(PRIMENG_EN).sort());
    });

    it('writes every Hebrew label in Hebrew', () => {
        expect(textValues(PRIMENG_HE).filter(text => /[A-Za-z]/.test(text))).toEqual([]);
    });

    it('covers the labels the date picker, select and dialogs show', () => {
        expect(keyPaths(PRIMENG_HE)).toEqual(expect.arrayContaining([
            'chooseDate',
            'prevMonth',
            'nextMonth',
            'nextHour',
            'prevMinute',
            'emptyMessage',
            'aria.close',
            'aria.listLabel',
        ]));
    });
});
```

- [ ] **Step 2: Write the failing `LanguageService` spec**

Create `client\src\app\core\language.service.spec.ts`:

```ts
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { PrimeNG } from 'primeng/config';
import { LanguageService } from './language.service';

const TRANSLATIONS = {
    en: { shell: { title: 'Driving Lessons Planner' } },
    he: { shell: { title: 'מערכת תכנון שיעורי נהיגה' } },
};

function withSavedLanguage(saved: string | null): void {
    const storage = new Map<string, string>(saved ? [['app_lang', saved]] : []);
    Object.defineProperty(globalThis, 'localStorage', {
        configurable: true,
        value: {
            getItem: (key: string) => storage.get(key) ?? null,
            setItem: (key: string, value: string) => void storage.set(key, value),
            removeItem: (key: string) => void storage.delete(key),
            clear: () => storage.clear(),
        } as Storage,
    });
}

function startService(): LanguageService {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: TRANSLATIONS,
                translocoConfig: { availableLangs: ['he', 'en'], defaultLang: 'he' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });
    const language = TestBed.inject(LanguageService);
    TestBed.tick();

    return language;
}

function switchTo(language: LanguageService, lang: 'he' | 'en'): void {
    language.use(lang);
    TestBed.tick();
}

function primeNgLabels() {
    return TestBed.inject(PrimeNG).translation;
}

describe('LanguageService', () => {
    it('starts in Hebrew, right to left, when nothing is saved', () => {
        //given
        withSavedLanguage(null);

        //when
        const language = startService();

        //then
        expect([language.lang(), language.locale(), language.isRtl()]).toEqual(['he', 'he-IL', true]);
        expect([document.documentElement.lang, document.documentElement.dir]).toEqual(['he', 'rtl']);
    });

    it('starts in the saved language', () => {
        //given
        withSavedLanguage('en');

        //when
        const language = startService();

        //then
        expect([language.locale(), language.isRtl(), document.documentElement.dir]).toEqual(['en-IL', false, 'ltr']);
        expect(document.title).toBe('Driving Lessons Planner');
        expect(primeNgLabels().aria?.close).toBe('Close');
    });

    it('switches to Israeli English, left to right', () => {
        //given
        withSavedLanguage(null);
        const language = startService();

        //when
        switchTo(language, 'en');

        //then
        expect([language.lang(), language.locale(), language.isRtl()]).toEqual(['en', 'en-IL', false]);
        expect([document.documentElement.lang, document.documentElement.dir]).toEqual(['en', 'ltr']);
    });

    it('gives PrimeNG its labels in Hebrew', () => {
        //given
        withSavedLanguage(null);

        //when
        startService();

        //then
        const labels = primeNgLabels();
        expect([labels.chooseDate, labels.nextMonth, labels.emptyMessage, labels.aria?.close])
            .toEqual(['בחירת תאריך', 'החודש הבא', 'לא נמצאו תוצאות', 'סגירה']);
    });

    it('restores every PrimeNG label in English after Hebrew', () => {
        //given
        withSavedLanguage(null);
        const language = startService();

        //when
        switchTo(language, 'en');

        //then
        const labels = primeNgLabels();
        expect([labels.chooseDate, labels.nextMonth, labels.emptyMessage, labels.aria?.close])
            .toEqual(['Choose Date', 'Next Month', 'No results found', 'Close']);
        expect(labels.aria?.moveUp).toBe('Move Up');
    });

    it('names the browser tab in the selected language', () => {
        //given
        withSavedLanguage(null);
        const language = startService();
        const hebrewTitle = document.title;

        //when
        switchTo(language, 'en');

        //then
        expect([hebrewTitle, document.title]).toEqual(['מערכת תכנון שיעורי נהיגה', 'Driving Lessons Planner']);
    });
});
```

`aria.moveUp` is a PrimeNG default this slice does not translate. It must survive the switch, which pins decision 7's `aria` merge (Review Focus 2).

- [ ] **Step 3: Write the failing toggle spec**

Create `client\src\app\shared\language-toggle\language-toggle.component.spec.ts`:

```ts
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { AppLanguage, LanguageService } from '../../core/language.service';
import { LanguageToggleComponent } from './language-toggle.component';

async function renderToggle(): Promise<ComponentFixture<LanguageToggleComponent>> {
    const lang = signal<AppLanguage>('he');
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { shell: { language: 'Language' } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang, use: (next: AppLanguage) => lang.set(next) } },
        ],
    });
    const fixture = TestBed.createComponent(LanguageToggleComponent);
    await fixture.whenStable();

    return fixture;
}

function options(fixture: ComponentFixture<LanguageToggleComponent>): HTMLButtonElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.lang__opt')];
}

describe('LanguageToggleComponent', () => {
    it('names the toggle group in the active language', async () => {
        //given
        const fixture = await renderToggle();

        //when
        const group = (fixture.nativeElement as HTMLElement).querySelector('[role=group]');

        //then
        expect(group?.getAttribute('aria-label')).toBe('Language');
    });

    it('marks each option with its own language and name', async () => {
        //given
        const fixture = await renderToggle();

        //when
        const buttons = options(fixture);

        //then
        expect(buttons.map(button => button.getAttribute('lang'))).toEqual(['en', 'he']);
        expect(buttons.map(button => button.getAttribute('aria-label'))).toEqual(['English', 'עברית']);
    });

    it('switches to the pressed language', async () => {
        //given
        const fixture = await renderToggle();

        //when
        options(fixture)[0].click();
        await fixture.whenStable();

        //then
        expect(options(fixture).map(button => button.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
    });
});
```

- [ ] **Step 4: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL.
- `primeng-translations.spec.ts` fails to load: `Failed to resolve import "./primeng-translations"`.
- In `language.service.spec.ts`, five specs fail:
  - `starts in Hebrew…`, `starts in the saved language` and `switches to Israeli English…` with `language.locale is not a function`.
  - `gives PrimeNG its labels in Hebrew` with `expected [ 'Choose Date', 'Next Month', … ] to deeply equal [ 'בחירת תאריך', … ]`.
  - `names the browser tab…` because nothing sets the title (`expected [ '', '' ] …`).
- `restores every PrimeNG label in English after Hebrew` **passes** already: today `PRIMENG_HE` never overwrites those labels, so PrimeNG's English defaults survive. It is a regression pin for Step 6's merge, which does overwrite them in Hebrew.
- In the toggle spec, `names the toggle group…` and `marks each option…` fail (`expected null to be 'Language'`); `switches to the pressed language` passes.
Every pre-existing spec still passes.

- [ ] **Step 5: Create the PrimeNG label sets**

Create `client\src\app\core\primeng-translations.ts`:

```ts
import { Translation } from 'primeng/api';

export const PRIMENG_HE: Partial<Translation> = {
    firstDayOfWeek: 0,
    dayNames: ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'],
    dayNamesShort: ['א׳', 'ב׳', 'ג׳', 'ד׳', 'ה׳', 'ו׳', 'ש׳'],
    dayNamesMin: ['א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ש'],
    monthNames: [
        'ינואר', 'פברואר', 'מרץ', 'אפריל', 'מאי', 'יוני',
        'יולי', 'אוגוסט', 'ספטמבר', 'אוקטובר', 'נובמבר', 'דצמבר',
    ],
    monthNamesShort: [
        'ינו', 'פבר', 'מרץ', 'אפר', 'מאי', 'יונ',
        'יול', 'אוג', 'ספט', 'אוק', 'נוב', 'דצמ',
    ],
    today: 'היום',
    clear: 'נקה',
    chooseYear: 'בחירת שנה',
    chooseMonth: 'בחירת חודש',
    chooseDate: 'בחירת תאריך',
    prevDecade: 'העשור הקודם',
    nextDecade: 'העשור הבא',
    prevYear: 'השנה הקודמת',
    nextYear: 'השנה הבאה',
    prevMonth: 'החודש הקודם',
    nextMonth: 'החודש הבא',
    prevHour: 'השעה הקודמת',
    nextHour: 'השעה הבאה',
    prevMinute: 'הדקה הקודמת',
    nextMinute: 'הדקה הבאה',
    prevSecond: 'השנייה הקודמת',
    nextSecond: 'השנייה הבאה',
    am: 'לפנה״צ',
    pm: 'אחה״צ',
    weekHeader: 'שבוע',
    emptyMessage: 'לא נמצאו תוצאות',
    emptyFilterMessage: 'לא נמצאו תוצאות',
    emptySearchMessage: 'לא נמצאו תוצאות',
    searchMessage: 'יש תוצאות חיפוש',
    selectionMessage: '{0} פריטים נבחרו',
    emptySelectionMessage: 'לא נבחר פריט',
    accept: 'כן',
    reject: 'לא',
    cancel: 'ביטול',
    aria: {
        close: 'סגירה',
        previous: 'הקודם',
        next: 'הבא',
        navigation: 'ניווט',
        listLabel: 'רשימת אפשרויות',
        selectAll: 'כל הפריטים נבחרו',
        unselectAll: 'הבחירה בוטלה בכל הפריטים',
        removeLabel: 'הסרה',
    },
};

export const PRIMENG_EN: Partial<Translation> = {
    firstDayOfWeek: 0,
    dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
    dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
    dayNamesMin: ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'],
    monthNames: [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December',
    ],
    monthNamesShort: [
        'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
        'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ],
    today: 'Today',
    clear: 'Clear',
    chooseYear: 'Choose Year',
    chooseMonth: 'Choose Month',
    chooseDate: 'Choose Date',
    prevDecade: 'Previous Decade',
    nextDecade: 'Next Decade',
    prevYear: 'Previous Year',
    nextYear: 'Next Year',
    prevMonth: 'Previous Month',
    nextMonth: 'Next Month',
    prevHour: 'Previous Hour',
    nextHour: 'Next Hour',
    prevMinute: 'Previous Minute',
    nextMinute: 'Next Minute',
    prevSecond: 'Previous Second',
    nextSecond: 'Next Second',
    am: 'am',
    pm: 'pm',
    weekHeader: 'Wk',
    emptyMessage: 'No results found',
    emptyFilterMessage: 'No results found',
    emptySearchMessage: 'No results found',
    searchMessage: 'Search results are available',
    selectionMessage: '{0} items selected',
    emptySelectionMessage: 'No selected item',
    accept: 'Yes',
    reject: 'No',
    cancel: 'Cancel',
    aria: {
        close: 'Close',
        previous: 'Previous',
        next: 'Next',
        navigation: 'Navigation',
        listLabel: 'Option List',
        selectAll: 'All items selected',
        unselectAll: 'All items unselected',
        removeLabel: 'Remove',
    },
};
```

The English values are PrimeNG 21's own defaults (`primeng-config.mjs`), so switching back to English restores exactly what PrimeNG shipped.

- [ ] **Step 6: Rewrite `LanguageService`**

Replace the whole of `client\src\app\core\language.service.ts` with:

```ts
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { PrimeNG } from 'primeng/config';
import { Translation } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { PRIMENG_EN, PRIMENG_HE } from './primeng-translations';

export type AppLanguage = 'he' | 'en';
export type AppLocale = 'he-IL' | 'en-IL';

const STORAGE_KEY = 'app_lang';
const DOCUMENT_TITLE_KEY = 'shell.title';

const LOCALES: Record<AppLanguage, AppLocale> = {
  he: 'he-IL',
  en: 'en-IL',
};

const PRIMENG_TRANSLATIONS: Record<AppLanguage, Partial<Translation>> = {
  he: PRIMENG_HE,
  en: PRIMENG_EN,
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly primeng = inject(PrimeNG);
  private readonly title = inject(Title);

  readonly lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'he',
  );

  readonly locale = computed<AppLocale>(() => LOCALES[this.lang()]);

  readonly isRtl = computed(() => this.lang() === 'he');

  private readonly documentTitle = toSignal(this.transloco.selectTranslate(DOCUMENT_TITLE_KEY), {
    initialValue: '',
  });

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      this.transloco.setActiveLang(lang);
      this.applyPrimeNgTranslation(PRIMENG_TRANSLATIONS[lang]);
      document.documentElement.lang = lang;
      document.documentElement.dir = this.isRtl() ? 'rtl' : 'ltr';
    });

    effect(() => {
      const title = this.documentTitle();

      if (title) {
        this.title.setTitle(title);
      }
    });
  }

  toggle(): void {
    this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
  }

  use(lang: AppLanguage): void {
    this.lang.set(lang);
  }

  private applyPrimeNgTranslation(translation: Partial<Translation>): void {
    this.primeng.setTranslation({
      ...translation,
      aria: { ...this.primeng.translation.aria, ...translation.aria },
    });
  }
}
```

The file keeps its existing 2-space indentation. `selectTranslate` re-emits on every `setActiveLang` once that language's file has loaded, so the title never shows a raw key.

- [ ] **Step 7: Point `app.config.ts` at the new file**

In `client\src\app\app.config.ts`, change

```ts
import { PRIMENG_HE } from './core/language.service';
```

to

```ts
import { PRIMENG_HE } from './core/primeng-translations';
```

Then confirm nothing else imported the constants from the service. Run (repo root): `grep -rn "PRIMENG_HE\|PRIMENG_EN" client/src --include=*.ts`
Expected: only `app.config.ts`, `core/language.service.ts`, `core/primeng-translations.ts` and `core/primeng-translations.spec.ts`.

- [ ] **Step 8: Make the toggle accessible**

Replace the whole of `client\src\app\shared\language-toggle\language-toggle.component.ts` with:

```ts
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { AppLanguage, LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-language-toggle',
  imports: [TranslocoPipe],
  template: `
    <div
      class="lang"
      [class.lang--touch]="touch()"
      role="group"
      [attr.aria-label]="'shell.language' | transloco"
    >
      @for (option of options; track option.lang) {
        <button
          type="button"
          class="lang__opt"
          [class.lang__opt--active]="language.lang() === option.lang"
          [attr.aria-pressed]="language.lang() === option.lang"
          [attr.lang]="option.lang"
          [attr.aria-label]="option.name"
          (click)="language.use(option.lang)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styleUrl: './language-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageToggleComponent {
  protected readonly language = inject(LanguageService);

  readonly touch = input(false);

  protected readonly options: ReadonlyArray<{ lang: AppLanguage; label: string; name: string }> = [
    { lang: 'en', label: 'EN', name: 'English' },
    { lang: 'he', label: 'עב', name: 'עברית' },
  ];
}
```

`label` and `name` are endonyms by design (roadmap decision 3): a language's own name does not change with the UI language. `lang` on each button makes a screen reader read `עברית` with the Hebrew voice.

- [ ] **Step 9: Add `shell.language` and the Hebrew boot title**

1. In `client\public\i18n\en.json`, change

```json
    "languageToggle": "עברית",
```

to

```json
    "languageToggle": "עברית",
    "language": "Language",
```

2. In `client\public\i18n\he.json`, change

```json
    "languageToggle": "English",
```

to

```json
    "languageToggle": "English",
    "language": "שפה",
```

3. In `client\src\index.html`, change

```html
  <title>Driving Lessons</title>
```

to

```html
  <title>מערכת תכנון שיעורי נהיגה</title>
```

`index.html` already boots as `lang="he" dir="rtl"`, the default language, so its static title now matches until `LanguageService` takes over.

- [ ] **Step 10: Run the specs and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the three PrimeNG-label specs, the six `LanguageService` specs and the three toggle specs.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/core/primeng-translations.ts client/src/app/core/primeng-translations.spec.ts client/src/app/core/language.service.ts client/src/app/core/language.service.spec.ts client/src/app/app.config.ts client/src/app/shared/language-toggle/language-toggle.component.ts client/src/app/shared/language-toggle/language-toggle.component.spec.ts client/src/index.html client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(i18n): Hebrew PrimeNG labels, translated tab title, accessible language toggle

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
