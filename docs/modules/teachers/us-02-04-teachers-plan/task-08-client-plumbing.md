# Task 8 of 12: Client plumbing — ToastService, ProblemDetails, AppRoutes

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires task 7 complete (the API exists). Work on branch `3-us-02-04-teachers-module`; client commands run from `client\`.

## Shared Context

**Goal:** The shared client infrastructure the teachers feature (and every later feature) consumes: a `ToastService` that translates keys and maps ProblemDetails, one `<p-toast />` in the root component, and `AppRoutes` constants replacing inline route strings.

**Conventions:** Transloco (`| transloco`, `translate()`), lowercase i18n namespaces, signals only, no comments, logical CSS properties.

---

**Files:**
- Create: `client/src/app/shared/models/problem-details.ts`
- Create: `client/src/app/core/services/toast.service.ts`
- Create: `client/src/app/shared/config/app-routes.ts`
- Modify: `client/src/app/app.ts`, `client/src/app/app.config.ts`, `client/src/app/app.routes.ts`, `client/src/app/core/auth.guard.ts` (only if it hardcodes `'/login'`), `client/public/i18n/en.json`, `client/public/i18n/he.json`

- [x] **Step 1: `client/src/app/shared/models/problem-details.ts`**

Mirrors what `ApiExceptionFilter` emits (`Status`, `Title`, `Detail` — the domain message lives in `detail`).

```typescript
export interface ProblemDetails {
    status: number;
    title: string;
    detail?: string;
}
```

- [x] **Step 2: `client/src/app/core/services/toast.service.ts`**

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { ProblemDetails } from '../../shared/models/problem-details';

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly messages = inject(MessageService);
    private readonly transloco = inject(TranslocoService);

    success(key: string): void {
        this.messages.add({ severity: 'success', summary: this.transloco.translate(key) });
    }

    apiError(error: unknown): void {
        this.messages.add({ severity: 'error', summary: this.resolveMessage(error) });
    }

    private resolveMessage(error: unknown): string {
        if (error instanceof HttpErrorResponse) {
            const problem = error.error as ProblemDetails | null;

            if (problem?.detail) {
                return problem.detail;
            }
        }

        return this.transloco.translate('general.unexpectedError');
    }
}
```

- [x] **Step 3: `client/src/app/shared/config/app-routes.ts`**

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
} as const;
```

- [x] **Step 4: Root component hosts the single `<p-toast />` — `client/src/app/app.ts` (full replacement)**

```typescript
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: '<router-outlet /><p-toast />',
})
export class App {
  private readonly language = inject(LanguageService);
}
```

- [x] **Step 5: Provide `MessageService` — `client/src/app/app.config.ts`**

Add the import and provider to the existing config (everything else unchanged):

```typescript
import { MessageService } from 'primeng/api';
```

```typescript
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    MessageService,
```

- [x] **Step 6: Route constants — `client/src/app/app.routes.ts` (full replacement)**

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AppRoutes } from './shared/config/app-routes';

export const routes: Routes = [
  {
    path: AppRoutes.login,
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/admin-shell/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
```

Check `client/src/app/core/auth.guard.ts` and `client/src/app/core/auth.service.ts` for hardcoded `'/login'` navigation strings; if present, import `AppRoutes` and build the path from `AppRoutes.login` (e.g. `createUrlTree(['/', AppRoutes.login])`, `router.navigate(['/', AppRoutes.login])`).

- [x] **Step 7: i18n keys — add to BOTH files in the same change**

`client/public/i18n/en.json` — add these top-level namespaces (keep existing content):

```json
  "general": {
    "save": "Save",
    "cancel": "Cancel",
    "unexpectedError": "Something went wrong. Please try again."
  },
  "formErrors": {
    "required": "This field is required",
    "emailInvalid": "Enter a valid email address"
  }
```

`client/public/i18n/he.json`:

```json
  "general": {
    "save": "שמירה",
    "cancel": "ביטול",
    "unexpectedError": "משהו השתבש. נסו שוב."
  },
  "formErrors": {
    "required": "שדה חובה",
    "emailInvalid": "יש להזין כתובת אימייל תקינה"
  }
```

- [x] **Step 8: Verify + commit**

Run from `client\`: `npm run build` — expected: success.
Run: `npm test` — expected: existing tests pass.

```bash
git add client
git commit -m "feat(client): toast service, problem details, and route constants"
```

---

**Next:** [task-09-teachers-feature-state.md](task-09-teachers-feature-state.md)
