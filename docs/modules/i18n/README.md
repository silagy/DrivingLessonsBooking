# i18n Module — Roadmap

Slice roadmap for the `module:i18n` backlog (2 open issues). The slice ships as its own branch/PR with its own plan folder (`docs\modules\i18n\us-XX-...-plan\`, same format as prior modules). This README records the decomposition and the decisions locked during planning (2 October 2026), so the slice plan does not re-litigate them.

**What exists already:** bilingual, RTL-first support was built into every earlier module rather than left for a phase. `LanguageService` (`client\src\app\core\language.service.ts`) owns the active language, persists it in `localStorage`, switches Transloco and PrimeNG, and sets `lang`/`dir` on `<html>`. The `EN | עב` toggle (`shared\language-toggle`) sits in the admin shell, on the login page and in the student-form header. `en.json` and `he.json` hold 291 mirrored keys, no template has a literal string, and every SCSS file uses logical properties. What is left is the set of leaks and mirror errors the planning-time audit found ([slice plan](us-47-48-language-and-rtl-plan/README.md), "Planning-time audit").

## Slices

| Slice | Content | Issues |
|-------|---------|--------|
| 1. Language and RTL acceptance — [plan](us-47-48-language-and-rtl-plan/README.md) | Every leak the audit found fixed: rule-violation toasts in the selected language (machine-readable `code` on every 404/409), PrimeNG's built-in labels in Hebrew, translated tab title, Israeli date and time formats in English, PrimeNG toast / button icon / date picker / popover mirrored in RTL, LTR values isolated without being misaligned, no letter-spacing on Hebrew. Closes with a both-languages acceptance sweep of every screen. | [#47](https://github.com/silagy/DrivingLessonsBooking/issues/47), [#48](https://github.com/silagy/DrivingLessonsBooking/issues/48) |

One slice, because the two stories share every screen: the acceptance sweep that proves "no hardcoded leftovers" (US-47) is the same walk that proves "mirror-correct" (US-48).

## Locked decisions (2 October 2026)

| # | Decision |
|---|----------|
| 1 | **Rule-violation messages are translated in the client, not the server.** The API adds a stable `code` (the exception's rule name, camelCase) to every 404/409 `ProblemDetails`; the client maps it to an `errors.*` key. The server stays single-language, as Excel module decision 6 already chose for the workbook. `detail` stays English for logs and developers and is never shown to a user again. |
| 2 | **English means Israeli English.** Dates and times use `en-IL` / `he-IL`, never bare `en` (= en-US): day before month, 24-hour clock, matching the fixed slot windows (07:00–12:00). Rule from `client-i18n.md` "Dates, Times, and Timezone". |
| 3 | **Language names stay endonyms.** The toggle reads `EN` / `עב` in both languages, with `lang` attributes so screen readers switch voice. They are deliberately not translation keys: a language's own name must not change when the UI language changes. |
| 4 | **PrimeNG RTL quirks are fixed once, globally**, in `client\src\styles\_global.scss` (the `app` layer, which wins over the `primeng` layer). Component code never compensates per instance. |
