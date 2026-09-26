# Task 5 of 6: Client — public route, per-visit store, page skeleton, i18n

> Part of [US-23: Public Link Gateway](README.md). Requires task 4 complete. Work on branch `24-us-23-public-link-gateway`; client commands run from `client\`.

**Files:**
- Modify: `client\src\app\shared\config\app-routes.ts`
- Modify: `client\src\app\app.routes.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts` (share-link prefix)
- Create: `client\src\app\features\student-form\student-form.routes.ts`
- Create: `client\src\app\features\student-form\state\student-form.store.ts`
- Create: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.ts`, `.html`, `.scss`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 4): `StudentFormView`, `viewForPublicationState`, `weekRangeLabel`, `formatWindowInstant`, `GetPublicationByLinkResponse`, `SubmissionsApiService.getPublicationByLink`; existing `LanguageService.lang` signal.
- Produces (task 6 relies on these exact names):
  - `AppRoutes.studentForm = 's'`
  - `StudentFormStore` (provided by `StudentFormPage`, so page children can inject it) with readonly signals `view: Signal<StudentFormView>`, `isOpen: Signal<boolean>`, `weekParams: Signal<{ weekNumber: number; weekRange: string }>`, `opensAt: Signal<string>`, `closesAt: Signal<string>`, and methods `open(token: string): void`, `retry(): void`
  - `StudentFormPage` (selector `app-student-form-page`) with `token = input.required<string>()` bound from the `:token` route param
  - Translation keys under `studentForm.*` (full list in Step 6)

Precedents to open before coding: `features\publications\state\publications.store.ts` (`resource()` + 404-swallowing loader + `isStatus`), `features\roster\roster.routes.ts` (default-exported routes), `features\roster\ui\pages\roster\roster.page.ts` (page shape, PrimeNG module imports).

- [x] **Step 1: Route constant + public route (before the guarded shell)**

`client\src\app\shared\config\app-routes.ts` — add `studentForm` at the end of the object:

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
    roster: 'roster',
    weekSchedules: 'week-schedules',
    publications: 'publications',
    publicationsHistory: 'history',
    studentForm: 's',
} as const;
```

`client\src\app\app.routes.ts` — insert the student route **between** the `login` route and the guarded `''` route (this file uses 2-space indentation — keep it). Order matters: the router matches top-down, and the guarded `''` shell plus the `**` redirect would otherwise send an anonymous student to `/login` (**Review Focus 4**). Full file:

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
    path: AppRoutes.studentForm,
    loadChildren: () => import('./features/student-form/student-form.routes'),
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

- [x] **Step 2: Keep the admin share link in lockstep with the route**

In `client\src\app\features\publications\state\publications.store.ts`:

1. Add the import (with the other `shared` imports):
   ```typescript
   import { AppRoutes } from '../../../shared/config/app-routes';
   ```
2. Delete the line `const STUDENT_LINK_PREFIX = '/s/';`
3. Replace the body of `buildShareLink`:
   ```typescript
   private buildShareLink(token: string): string {
       return `${this.document.location.origin}/${AppRoutes.studentForm}/${token}`;
   }
   ```

The generated link is byte-for-byte the same as before (`{origin}/s/{token}`); only the source of the `s` changes.

- [x] **Step 3: Feature routes (store provided per visit)**

`client\src\app\features\student-form\student-form.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { StudentFormPage } from './ui/pages/student-form/student-form.page';

export default [{ path: ':token', component: StudentFormPage }] satisfies Routes;
```

The store is provided on `StudentFormPage` (Step 5), not on the route. The page is destroyed on leave, so each visit gets a fresh store (README decision 7). `withComponentInputBinding()` is already enabled in `app.config.ts`, so `:token` binds to the page's `token` input.

> **Changed on review:** the original plan put `providers: [StudentFormStore]` on this route. Route injectors are never destroyed without `withExperimentalAutoCleanupInjectors()`, so the store survived between visits. `student-form.routes.spec.ts` (navigate `/s/a` → elsewhere → `/s/a`, expect a different store instance) failed against the route provider and passes against the page provider.

- [x] **Step 4: Signals store**

`client\src\app\features\student-form\state\student-form.store.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { GetPublicationByLinkResponse } from '../data/get-publication-by-link.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { formatWindowInstant } from '../domain/jerusalem-time';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };

@Injectable()
export class StudentFormStore {
    private readonly api = inject(SubmissionsApiService);
    private readonly language = inject(LanguageService);

    private readonly linkToken = signal<string | null>(null);

    private readonly publicationResource = resource({
        params: () => this.linkToken() ?? undefined,
        loader: ({ params: token }) => this.loadByLink(token),
    });

    private readonly publication = computed<GetPublicationByLinkResponse | null>(() =>
        this.publicationResource.hasValue() ? this.publicationResource.value() : null,
    );

    readonly view = computed<StudentFormView>(() => {
        if (!this.linkToken() || this.publicationResource.isLoading()) {
            return StudentFormView.loading;
        }

        if (this.publicationResource.error()) {
            return StudentFormView.loadFailed;
        }

        const publication = this.publication();

        return publication ? viewForPublicationState(publication.state) : StudentFormView.invalidLink;
    });

    readonly isOpen = computed(() => this.view() === StudentFormView.open);

    readonly weekParams = computed(() => {
        const publication = this.publication();

        if (!publication) {
            return EMPTY_WEEK_PARAMS;
        }

        return {
            weekNumber: publication.weekNumber,
            weekRange: weekRangeLabel(publication.weekStart, this.language.lang()),
        };
    });

    readonly opensAt = computed(() => this.formatInstant(this.publication()?.windowStartUtc));
    readonly closesAt = computed(() => this.formatInstant(this.publication()?.windowEndUtc));

    open(token: string): void {
        this.linkToken.set(token);
    }

    retry(): void {
        this.publicationResource.reload();
    }

    private formatInstant(utcIso: string | undefined): string {
        return utcIso ? formatWindowInstant(utcIso, this.language.lang()) : '';
    }

    private async loadByLink(token: string): Promise<GetPublicationByLinkResponse | null> {
        try {
            return await firstValueFrom(this.api.getPublicationByLink(token));
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return null;
            }

            throw error;
        }
    }
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
```

Notes for the implementer:
- A **404 is a normal outcome** (unknown or draft link) → `null` → `invalidLink`. Anything else (network, 500) stays an error → `loadFailed` with retry. No toast either way — the page itself is the message (**Review Focus 2**).
- `hasValue()` guards the read: in Angular 20+ reading `value()` while the resource is in an error state throws.
- `weekParams`, `opensAt` and `closesAt` read `language.lang()`, so toggling EN/עב re-formats dates without reloading.
- `view` shows `loading` until the page has handed over the token (before the first `open()`).

- [x] **Step 5: Page skeleton**

Task 6 replaces the template and styles with the full mockup UI; this skeleton only proves routing, store and translations end to end.

`client\src\app\features\student-form\ui\pages\student-form\student-form.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';

@Component({
    selector: 'app-student-form-page',
    imports: [TranslocoPipe, ProgressSpinnerModule],
    providers: [StudentFormStore],
    templateUrl: './student-form.page.html',
    styleUrl: './student-form.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
    protected readonly store = inject(StudentFormStore);
    protected readonly views = StudentFormView;

    readonly token = input.required<string>();

    constructor() {
        effect(() => this.store.open(this.token()));
    }
}
```

`client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`:

```html
<main class="student-form">
    @switch (store.view()) {
        @case (views.loading) {
            <p-progressSpinner [ariaLabel]="'studentForm.loading' | transloco" />
        }
        @case (views.notYetOpen) {
            <h1>{{ 'studentForm.notYetOpen.title' | transloco }}</h1>
            <p>{{ 'studentForm.notYetOpen.body' | transloco: store.weekParams() }}</p>
        }
        @case (views.open) {
            <h1>{{ 'studentForm.open.title' | transloco }}</h1>
            <p>{{ 'studentForm.open.body' | transloco: store.weekParams() }}</p>
        }
        @case (views.closed) {
            <h1>{{ 'studentForm.closed.title' | transloco }}</h1>
            <p>{{ 'studentForm.closed.body' | transloco: store.weekParams() }}</p>
        }
        @case (views.invalidLink) {
            <h1>{{ 'studentForm.invalidLink.title' | transloco }}</h1>
        }
        @case (views.loadFailed) {
            <h1>{{ 'studentForm.loadFailed.title' | transloco }}</h1>
        }
    }
</main>
```

`client\src\app\features\student-form\ui\pages\student-form\student-form.page.scss`:

```scss
.student-form {
    padding: 1rem;
}
```

- [x] **Step 6: Translations (en + he, same commit)**

Add a new top-level `"studentForm"` object to **both** files, immediately **before** the `"general": {` line (line ~242; JSON files use 2-space indentation). Every key exists in both files with identical parameters.

`client\public\i18n\en.json`:

```json
  "studentForm": {
    "loading": "Loading the form…",
    "weekCaption": "Week {{weekNumber}} · {{weekRange}}",
    "window": {
      "opens": "Opens",
      "closes": "Closes",
      "closed": "Closed"
    },
    "notYetOpen": {
      "title": "Submissions aren't open yet",
      "body": "The form for week {{weekNumber}} ({{weekRange}}) isn't open yet.",
      "hint": "Come back through the same link — no need for a new one."
    },
    "open": {
      "title": "Submissions are open",
      "body": "The form for week {{weekNumber}} ({{weekRange}}) is open."
    },
    "closed": {
      "title": "Submissions are closed",
      "body": "The form for week {{weekNumber}} ({{weekRange}}) has closed.",
      "hint": "If the school reopens the window, this link will work again."
    },
    "invalidLink": {
      "title": "This link isn't valid",
      "body": "We couldn't find a form for this link. Ask your school for the current link."
    },
    "loadFailed": {
      "title": "Something went wrong",
      "body": "We couldn't load the form. Check your connection and try again.",
      "retry": "Try again"
    }
  },
```

`client\public\i18n\he.json`:

```json
  "studentForm": {
    "loading": "טוען את הטופס…",
    "weekCaption": "שבוע {{weekNumber}} · {{weekRange}}",
    "window": {
      "opens": "פתיחה",
      "closes": "סגירה",
      "closed": "נסגר"
    },
    "notYetOpen": {
      "title": "ההגשה עוד לא נפתחה",
      "body": "הטופס לשבוע {{weekNumber}} ({{weekRange}}) עוד לא פתוח.",
      "hint": "חזרו דרך אותו הקישור — אין צורך בקישור חדש."
    },
    "open": {
      "title": "ההגשה פתוחה",
      "body": "הטופס לשבוע {{weekNumber}} ({{weekRange}}) פתוח."
    },
    "closed": {
      "title": "ההגשה נסגרה",
      "body": "הטופס לשבוע {{weekNumber}} ({{weekRange}}) נסגר.",
      "hint": "אם בית הספר יפתח את החלון מחדש, הקישור יעבוד שוב."
    },
    "invalidLink": {
      "title": "הקישור אינו תקין",
      "body": "לא מצאנו טופס עבור הקישור הזה. בקשו מבית הספר את הקישור העדכני."
    },
    "loadFailed": {
      "title": "משהו השתבש",
      "body": "לא הצלחנו לטעון את הטופס. בדקו את החיבור ונסו שוב.",
      "retry": "נסו שוב"
    }
  },
```

Validate both files parse: `node -e "require('./public/i18n/en.json'); require('./public/i18n/he.json'); console.log('ok')"` (from `client\`) → `ok`.

- [x] **Step 7: Build, test, and check the route in the browser**

Run (in `client\`): `npm test -- --watch=false` → all PASS; `npm run build` → success.

With Postgres + API running (task 3 Step 3) start the client (`preview_start {name:"client"}` → `http://localhost:4200`). Verify with `get_page_text` / `read_page` (project memory: screenshots can hang on this PrimeNG app):

1. **Logged out** (clear `localStorage` first), open `http://localhost:4200/s/NoSuchTokenAbc123` → URL stays `/s/NoSuchTokenAbc123` (no redirect to `/login`), page shows `הקישור אינו תקין`, no error toast — **Review Focus 2 + 4**.
2. Open `http://localhost:4200/s/{LINK}` with the published link from task 3 → `ההגשה עוד לא נפתחה` and the body with week 47 and the Nov 15–20 range.
3. Run `localStorage.setItem('auth_token', 'not-a-real-jwt')` in the page (the `AuthService` key), reload the student URL → still the student page, no bounce to `/login`, the interceptor's bearer header is ignored by the anonymous endpoint — **Review Focus 4**. Remove the key afterwards.
4. Sign in as admin → Publications → the week from task 3 → the share-link box still shows `http://localhost:4200/s/{LINK}` (Step 2 did not change the link).

- [ ] **Step 8: Commit**

```bash
git add client/src/app/app.routes.ts client/src/app/shared/config/app-routes.ts client/src/app/features/publications/state/publications.store.ts client/src/app/features/student-form client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): public student link route with per-visit store

s/:token sits outside the auth guard; the store maps the link's
publication state to a view and swallows 404 as an invalid link."
```

---

**Next:** [task-06-client-ui-and-verification.md](task-06-client-ui-and-verification.md)
