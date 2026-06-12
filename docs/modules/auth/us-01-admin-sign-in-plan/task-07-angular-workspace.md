# Task 7 of 11: Angular workspace + PrimeNG + Transloco + RTL toggle

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–6 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`.
>
> **Commit note:** no separate commit yet — Tasks 7–9 commit together once the client compiles (see task 9, step 8). Step 9 below references `./core/auth.interceptor`, which is created in Task 8 — both land in the same commit.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Client stack:** Angular 21+ (zoneless, standalone, signals only), PrimeNG (matching major) + @primeuix/themes, Transloco (runtime he/en toggle, RTL-first). Every visible string is a translation key; Hebrew RTL-first. Dev server proxies `/api` to the backend on port 5080.

**User decisions (locked):** JWT bearer · no tests this slice.

## Risks / Gotchas for this task

- **PrimeNG major must match Angular major.** If PrimeNG lags the newest Angular, scaffold with the previous Angular major (`npx @angular/cli@<n> new`).
- **RTL:** PrimeNG styled mode respects `document.documentElement.dir`; use only logical CSS (`text-align: start`, `margin-inline`) in custom styles.
- **Zoneless + signals:** keep Transloco `reRenderOnLangChange: true` so the pipe re-renders on toggle.

---

**Files:**
- Create: `client/` workspace (`ng new`), `client/proxy.conf.json`
- Modify: `client/angular.json` (proxy), `client/src/index.html`, `client/src/styles.scss`
- Create: `client/src/app/transloco-loader.ts`, `client/public/i18n/en.json`, `client/public/i18n/he.json`
- Create: `client/src/app/core/language.service.ts`
- Replace: `client/src/app/app.config.ts`, `client/src/app/app.ts`

- [ ] **Step 1: Scaffold workspace + packages** (from repo root)

```
npx @angular/cli@latest new client --directory client --style scss --ssr false --skip-git --zoneless
cd client
npm install primeng @primeuix/themes primeicons @jsverse/transloco
```

PrimeNG major MUST match the Angular major `ng new` produced (e.g., Angular 21 ↔ `primeng@21`). If PrimeNG lags the newest Angular major, scaffold one major lower: `npx @angular/cli@21 new ...`.

- [ ] **Step 2: `client/proxy.conf.json`** + wire into `angular.json` under `projects.client.architect.serve.options`

```json
{
  "/api": {
    "target": "http://localhost:5080",
    "secure": false
  }
}
```

```json
"proxyConfig": "proxy.conf.json"
```

- [ ] **Step 3: `client/src/index.html`** — Hebrew/RTL-first defaults

```html
<!doctype html>
<html lang="he" dir="rtl">
<head>
  <meta charset="utf-8">
  <title>Driving Lessons</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

- [ ] **Step 4: `client/src/styles.scss`**

```scss
@import "primeicons/primeicons.css";

* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; height: 100%; font-family: system-ui, sans-serif; }
```

- [ ] **Step 5: `client/src/app/transloco-loader.ts`**

```ts
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(lang: string) {
    return this.http.get<Translation>(`/i18n/${lang}.json`);
  }
}
```

- [ ] **Step 6: `client/public/i18n/en.json`**

```json
{
  "auth": {
    "title": "Admin Sign In",
    "email": "Email",
    "password": "Password",
    "submit": "Sign in",
    "invalidCredentials": "Incorrect email or password"
  },
  "shell": {
    "title": "Driving Lessons Planner",
    "logout": "Sign out",
    "languageToggle": "עברית"
  },
  "dashboard": {
    "title": "Dashboard",
    "placeholder": "Welcome. Planning features arrive in the next slices."
  }
}
```

- [ ] **Step 7: `client/public/i18n/he.json`**

```json
{
  "auth": {
    "title": "כניסת מנהל",
    "email": "אימייל",
    "password": "סיסמה",
    "submit": "כניסה",
    "invalidCredentials": "אימייל או סיסמה שגויים"
  },
  "shell": {
    "title": "מערכת תכנון שיעורי נהיגה",
    "logout": "התנתקות",
    "languageToggle": "English"
  },
  "dashboard": {
    "title": "לוח בקרה",
    "placeholder": "ברוך הבא. יכולות התכנון יגיעו בפרוסות הבאות."
  }
}
```

(The toggle label is itself a translation key whose value is the other language's name — keeps the "every visible string is a key" rule intact.)

- [ ] **Step 8: `client/src/app/core/language.service.ts`** (signals only; flips `dir` for RTL)

```ts
import { effect, inject, Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

export type AppLanguage = 'he' | 'en';
const STORAGE_KEY = 'app_lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);

  readonly lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'he',
  );

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      this.transloco.setActiveLang(lang);
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'he' ? 'rtl' : 'ltr';
    });
  }

  toggle(): void {
    this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
  }
}
```

- [ ] **Step 9: `client/src/app/app.config.ts`** (complete file; `authInterceptor` file is created in Task 8 — create both in the same commit)

```ts
import { ApplicationConfig, provideZonelessChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { TranslocoHttpLoader } from './transloco-loader';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({ theme: { preset: Aura } }),
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
  ],
};
```

- [ ] **Step 10: `client/src/app/app.ts`** (root component — instantiating LanguageService makes the dir/lang effect run app-wide)

```ts
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  private readonly language = inject(LanguageService);
}
```

(No separate commit yet — Tasks 7–9 commit together once the client compiles.)

---

**Next:** [task-08-client-auth.md](task-08-client-auth.md)
