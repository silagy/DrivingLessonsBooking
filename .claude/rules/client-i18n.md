---
paths:
  - "client/**"
---

# Client i18n Guide — Hebrew + English, RTL-First

Localization rules per requirements §8.4 and decision 14: **Hebrew and English, user-toggleable on both surfaces (admin and student form). Hebrew is RTL-first — layouts must be mirror-correct, not merely translated.** Timezone presentation rules (§8.3) also live here because they are display concerns.

## Critical Rules

1. **Never hardcode a user-visible string** — every label, button, tooltip, toast, dialog header, validation message, and empty state comes from a translation key
2. **Hebrew is a first-class layout** — every screen must be correct in RTL, not just readable
3. **Logical CSS properties only** — `margin-inline-start`, never `margin-left`; `padding-inline-end`, never `padding-right`; `inset-inline-start`, never `left` (for layout positioning)
4. **`en.json` and `he.json` ship together** — a key added to one file in a PR is added to both in the same PR
5. **All API instants are UTC; all displayed instants are Asia/Jerusalem** — conversion happens only in `shared\dates\jerusalem-time.ts` functions that pin `timeZone: 'Asia/Jerusalem'`
6. **Slot windows are wall-clock labels, never timezone-converted** — Morning is 07:00–12:00 in Asia/Jerusalem by definition (§8.3)

## Setup

Transloco, registered in `app.config.ts`, with an HTTP loader (`app\transloco-loader.ts`) that fetches `public\i18n\{lang}.json` from `/i18n/`:

```typescript
provideTransloco({
    config: {
        availableLangs: ['he', 'en'],
        defaultLang: 'he',
        fallbackLang: 'en',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
    },
    loader: TranslocoHttpLoader,
}),
```

```typescript
@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
    private readonly http = inject(HttpClient);

    getTranslation(lang: string) {
        return this.http.get<Translation>(`/i18n/${lang}.json`);
    }
}
```

- There is no custom missing-key handler: Transloco's default warns about a missing key outside prod mode and renders the key itself

## LanguageService

One service (`core\language.service.ts`) owns language selection, persistence, document direction, and PrimeNG's own strings. The root `App` component injects it, so it is live from startup:

```typescript
@Injectable({ providedIn: 'root' })
export class LanguageService {
    private readonly transloco = inject(TranslocoService);
    private readonly primeng = inject(PrimeNG);

    readonly lang = signal<AppLanguage>(localStorage.getItem(STORAGE_KEY) === 'en' ? 'en' : 'he');
    readonly locale = computed<AppLocale>(() => LOCALES[this.lang()]);
    readonly isRtl = computed(() => this.lang() === 'he');

    constructor() {
        effect(() => {
            const lang = this.lang();
            localStorage.setItem(STORAGE_KEY, lang);
            this.transloco.setActiveLang(lang);
            this.applyPrimeNgTranslation(PRIMENG_TRANSLATIONS[lang]);
            document.documentElement.lang = lang;
            document.documentElement.dir = this.isRtl() ? 'rtl' : 'ltr';
        });
    }

    toggle(): void {
        this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
    }
}
```

- Default language is **Hebrew** — the primary user base; English is the toggle
- The toggle (`shared\language-toggle\`) is visible on **both** surfaces (admin shell and student form header)
- `locale()` (`he-IL` / `en-IL`) is the locale passed to date formatters
- PrimeNG's built-in strings (calendar day names, aria labels) come from `core\primeng-translations.ts` and switch with the language
- PrimeNG direction follows `document.documentElement.dir` — see `client-primeng.md`
- The document title is the translated `brand.appName` key (the product name) and follows the language; `index.html` carries the Hebrew name for first paint

## Translation Files

```
client\public\i18n\
├── en.json
└── he.json        every key mirrored — same structure, same parameters
```

Nested JSON, feature-scoped namespaces, requirements terminology:

```json
{
    "general": {
        "save": "Save",
        "cancel": "Cancel",
        "refresh": "Refresh"
    },
    "formErrors": {
        "required": "This field is required",
        "emailInvalid": "Enter a valid email address"
    },
    "publications": {
        "title": "Publications",
        "extendWindow": "Extend deadline",
        "dataAsOf": "Data as of {{time}}",
        "state": {
            "draft": "Draft",
            "published": "Published",
            "open": "Open",
            "closed": "Closed"
        }
    },
    "studentForm": {
        "weekTeacherCaption": "Week {{weekNumber}} · {{weekRange}} · {{teacherName}}"
    },
    "weekGrid": {
        "windows": {
            "morning": "Morning",
            "noon": "Noon",
            "afternoon": "Afternoon",
            "evening": "Evening"
        }
    }
}
```

### Key Naming Rules

| Rule | Example |
|------|---------|
| Top-level namespace = feature or shared concern, camelCase | `publications`, `studentForm`, `general`, `formErrors` |
| Leaf keys camelCase | `extendWindow`, `dataAsOf` |
| Enum display values under a camelCase subgroup keyed by the enum's camelCase values | `publications.state.draft` |
| Interpolation params in double braces, camelCase | `{{teacherName}}`, `{{closedAt}}` |
| Key names describe meaning, not the English wording | `studentForm.windowClosedMessage`, not `studentForm.sorryTheWindowHasClosed` |
| Generic strings live in `general` / `formErrors` once | `general.cancel` everywhere |

Dynamic keys are built from enum values, never free text: `'publications.state.' + publication.state`.

## Consuming Translations

- **Templates: the `transloco` pipe** (import `TranslocoPipe`) — reactive, updates on language switch. Default for everything.

```html
<h2>{{ 'publications.title' | transloco }}</h2>
<p>{{ 'publications.dataAsOf' | transloco: { time: dataAsOf() } }}</p>
```

- **TypeScript: `TranslocoService.translate()`** — only for point-in-time strings resolved at the moment of use: dialog headers, toast messages. Never store a `translate()` result in long-lived state — it goes stale on language switch. Toasts go through `ToastService`, which translates keys for you.

```typescript
this.dialogService.open(PublishWeekDialog, {
    header: this.transloco.translate('publications.publish'),
});
```

- Plural-sensitive strings get explicit singular/plural keys selected in code; do not hand-roll language-specific plural rule logic.

## RTL Layout Rules

- `LanguageService` sets `dir` on `<html>`; nothing else touches direction
- **Logical properties** in all SCSS — `margin-inline-*`, `padding-inline-*`, `inset-inline-*`, `border-start-start-radius`, `text-align: start`
- Flexbox and Grid mirror automatically — never compensate with `row-reverse` hacks
- No hardcoded directional glyphs in strings or templates (`→`, `‹`); use icons that PrimeNG flips or rotate via `[dir="rtl"]` scoped CSS
- Numbers, times, and email addresses stay LTR inside RTL text — wrap with `<bdi>` or `unicode-bidi: isolate` where mixing breaks
- The week grid keeps **Sunday first** in both languages; in RTL the day columns render mirrored (Sunday at the inline-start edge). Friday has only Morning and Noon; Saturday never renders (requirements §5.3)
- A feature is not done until verified in Hebrew — mirror-correct layout is part of acceptance, not a follow-up

## Dates, Times, and Timezone (§8.3)

- **Instants** (window open/close, submission timestamps) arrive as UTC ISO 8601 strings and are displayed in **Asia/Jerusalem** — pure functions in `shared\dates\jerusalem-time.ts` do the conversion using `Intl.DateTimeFormat` with `timeZone: 'Asia/Jerusalem'`; stores and pages call them with `LanguageService.locale()`
- Admin-entered wall times (publish, extend, reopen dialogs) go back to the API as UTC through `jerusalemWallTimeToUtcIso` in `shared\dates\jerusalem-time.ts`, never through the browser's zone
- **Wall-clock definitions** (slot windows: Morning 07:00–12:00) are rendered as fixed local-time labels — they are never `Date` objects and never pass through UTC conversion; doing so shifts them across DST transitions, which is a bug
- No timezone selection UI exists in v1 — Asia/Jerusalem is the single configured zone
- Date formatting respects the active language locale (`he-IL` / `en-IL`); day names come from translation keys, not from `Date` formatting, so the grid wording matches the rest of the UI

```typescript
export function formatInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: 'Asia/Jerusalem',
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}
```

```typescript
readonly dataAsOf = computed<string>(() => {
    const loadedAt = this.loadedAtState();

    return loadedAt ? formatInstantInJerusalem(loadedAt, this.language.locale()) : '';
});
```

## Anti-Patterns

- **Never** hardcode a user-visible string in a template, component, store, or toast
- **Never** use physical CSS properties (`margin-left`, `right:`) for layout spacing or positioning
- **Never** add a key to `en.json` without its `he.json` mirror in the same change
- **Never** store a `TranslocoService.translate()` result in a long-lived field or signal
- **Never** convert slot window times through `Date`/UTC — they are wall-clock labels
- **Never** format an instant without going through a `shared\dates\jerusalem-time.ts` function — `new Date(x).toLocaleString()` uses the browser zone and is wrong by construction
- **Never** concatenate translated fragments to build a sentence — use one key with `{{params}}`
- **Never** ship a screen that has not been checked in Hebrew/RTL
