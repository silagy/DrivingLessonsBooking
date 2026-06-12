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
5. **All API instants are UTC; all displayed instants are Asia/Jerusalem** — conversion happens in exactly one shared service
6. **Slot windows are wall-clock labels, never timezone-converted** — Morning is 07:00–12:00 in Asia/Jerusalem by definition (§8.3)

## Setup

ngx-translate with HTTP loader, default language, and a mandatory missing-key handler:

```typescript
provideTranslateService({
    defaultLanguage: 'en',
    loader: {
        provide: TranslateLoader,
        useFactory: (http: HttpClient) => new TranslateHttpLoader(http, './assets/i18n/', '.json'),
        deps: [HttpClient],
    },
    missingTranslationHandler: {
        provide: MissingTranslationHandler,
        useClass: AppMissingTranslationHandler,
    },
})
```

```typescript
export class AppMissingTranslationHandler implements MissingTranslationHandler {
    handle(params: MissingTranslationHandlerParams): string {
        console.warn(`[i18n] Missing translation key: ${params.key}`);
        return params.key;
    }
}
```

## LanguageService

One service owns language selection, persistence, and document direction. Initialized via `provideAppInitializer` before first render:

```typescript
@Injectable({ providedIn: 'root' })
export class LanguageService {
    private readonly translate = inject(TranslateService);

    readonly supportedLanguages = ['he', 'en'] as const;
    readonly currentLanguage = signal<AppLanguage>('he');
    readonly isRtl = computed(() => this.currentLanguage() === 'he');

    init(): void {
        const saved = localStorage.getItem('app.language') as AppLanguage | null;
        this.use(saved ?? 'he');
    }

    use(language: AppLanguage): void {
        this.translate.use(language);
        this.currentLanguage.set(language);
        localStorage.setItem('app.language', language);
        document.documentElement.lang = language;
        document.documentElement.dir = this.isRtl() ? 'rtl' : 'ltr';
    }
}
```

- Default language is **Hebrew** — the primary user base; English is the toggle
- The toggle is visible on **both** surfaces (admin shell and student form header)
- PrimeNG direction follows `document.documentElement.dir` — see `client-primeng.md`

## Translation Files

```
client\src\assets\i18n\
├── en.json
└── he.json        every key mirrored — same structure, same parameters
```

Nested JSON, feature-scoped namespaces, requirements terminology:

```json
{
    "General": {
        "save": "Save",
        "cancel": "Cancel",
        "refresh": "Refresh",
        "dataAsOf": "Data as of {{time}}"
    },
    "FormErrors": {
        "required": "This field is required",
        "emailInvalid": "Enter a valid email address"
    },
    "Publications": {
        "publish": "Publish",
        "extendDeadline": "Extend deadline",
        "windowClosed": "The submission window closed on {{closedAt}}",
        "State": {
            "draft": "Draft",
            "published": "Published",
            "open": "Open",
            "closed": "Closed"
        }
    },
    "StudentForm": {
        "submittingForTeacher": "You are submitting availability for {{teacherName}}",
        "notMyTeacher": "This is not my teacher",
        "targetCountQuestion": "How many lessons do you want this week?"
    },
    "Slots": {
        "morning": "Morning",
        "noon": "Noon",
        "afternoon": "Afternoon",
        "evening": "Evening"
    }
}
```

### Key Naming Rules

| Rule | Example |
|------|---------|
| Top-level namespace = feature or shared concern, PascalCase | `Publications`, `StudentForm`, `General`, `FormErrors` |
| Leaf keys camelCase | `extendDeadline`, `notMyTeacher` |
| Enum display values under a PascalCase subgroup keyed by the enum's camelCase values | `Publications.State.draft` |
| Interpolation params in double braces, camelCase | `{{teacherName}}`, `{{closedAt}}` |
| Key names describe meaning, not the English wording | `StudentForm.windowClosedMessage`, not `StudentForm.sorryTheWindowHasClosed` |
| Generic strings live in `General` / `FormErrors` once | `General.cancel` everywhere |

Dynamic keys are built from enum values, never free text: `'Publications.State.' + publication.state`.

## Consuming Translations

- **Templates: the `translate` pipe** — reactive, updates on language switch. Default for everything.

```html
<h2>{{ 'Publications.title' | translate }}</h2>
<p>{{ 'StudentForm.submittingForTeacher' | translate: { teacherName: teacher().name } }}</p>
```

- **TypeScript: `translate.instant()`** — only for point-in-time strings resolved at the moment of use: dialog headers, toast messages. Never store an `instant()` result in long-lived state — it goes stale on language switch.

```typescript
this.dialogService.open(PublishWeekDialog, {
    header: this.translate.instant('Publications.publish'),
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

- **Instants** (window open/close, submission timestamps) arrive as UTC ISO 8601 strings and are displayed in **Asia/Jerusalem** — one shared `JerusalemDateService` (in `core\services\`) owns the conversion using `Intl.DateTimeFormat` with `timeZone: 'Asia/Jerusalem'`; expose it as pipes for templates
- **Wall-clock definitions** (slot windows: Morning 07:00–12:00) are rendered as fixed local-time labels — they are never `Date` objects and never pass through UTC conversion; doing so shifts them across DST transitions, which is a bug
- No timezone selection UI exists in v1 — Asia/Jerusalem is the single configured zone
- Date formatting respects the active language locale (`he-IL` / `en-IL`); day names come from translation keys, not from `Date` formatting, so the grid wording matches the rest of the UI

```typescript
@Injectable({ providedIn: 'root' })
export class JerusalemDateService {
    private readonly language = inject(LanguageService);

    formatInstant(utcIso: string, options: Intl.DateTimeFormatOptions): string {
        const locale = this.language.currentLanguage() === 'he' ? 'he-IL' : 'en-IL';
        const formatter = new Intl.DateTimeFormat(locale, { ...options, timeZone: 'Asia/Jerusalem' });

        return formatter.format(new Date(utcIso));
    }
}
```

## Anti-Patterns

- **Never** hardcode a user-visible string in a template, component, store, or toast
- **Never** use physical CSS properties (`margin-left`, `right:`) for layout spacing or positioning
- **Never** add a key to `en.json` without its `he.json` mirror in the same change
- **Never** store an `instant()` result in a long-lived field or signal
- **Never** convert slot window times through `Date`/UTC — they are wall-clock labels
- **Never** format an instant without going through `JerusalemDateService` — `new Date(x).toLocaleString()` uses the browser zone and is wrong by construction
- **Never** concatenate translated fragments to build a sentence — use one key with `{{params}}`
- **Never** ship a screen that has not been checked in Hebrew/RTL
