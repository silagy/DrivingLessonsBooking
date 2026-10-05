# Task 3 of 7: Route guards by Role, with the "no access" toast

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires task 2 committed. Work on branch `89-teacher-role-navigation`. Read README decisions 5-7 first.

**Files:**
- Modify: `client\src\app\core\services\toast.service.ts`
- Test: `client\src\app\core\services\toast.service.spec.ts`
- Create: `client\src\app\core\role.guards.ts`
- Test: `client\src\app\core\role.guards.spec.ts` (**new**)
- Modify: `client\src\app\features\admin-shell\admin.routes.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Modify: `.claude\rules\client-architecture.md` (Routing section)

**Interfaces:**
- Consumes: `AuthService.isAuthenticated` (exists) and `AuthService.isAdministrator` (task 2); `AppRoutes.login`, `AppRoutes.weekSchedules`.
- Produces:
  - `ToastService.info(key: string, detail: ToastDetail): Promise<void>`: waits until the active language's translations are loaded, then adds `{ severity: 'info', summary, detail }`.
  - `ToastService.apiError` / `messageOf`: a 403 without a translated rule code shows `errors.forbidden`.
  - `administratorGuard: CanMatchFn` and `homeGuard: CanActivateFn` exported from `client\src\app\core\role.guards.ts`.
  - Translation keys `access.refusedTitle`, `access.refusedDetail`, `errors.forbidden`.

**Design (frame 10d):** a Teacher who opens an Administrator-only URL lands on Weekly prep, and an **info** toast (blue info icon, white card, close X) appears at the top inline end (top-left in Hebrew, top-right in English): title "אין לך גישה לדף הזה", detail "הועברת למערכת השבועית שלך.".

**Why:** AC 5 ("Route guards gain a Role check; typing an admin-only URL as a Teacher redirects away").

- [ ] **Step 1: Write the failing toast specs**

In `client\src\app\core\services\toast.service.spec.ts`:

Add `HTTP_FORBIDDEN` next to the other status constants:

```typescript
const HTTP_FORBIDDEN = 403;
```

Extend the `EN` fixture with these entries (keep the existing ones):

```typescript
    access: {
        refusedTitle: 'You don\'t have access to that page',
        refusedDetail: 'We\'ve taken you to your Week Schedule.',
    },
```

and, inside `errors`:

```typescript
        forbidden: 'You don\'t have permission to do that.',
```

Change `setUp` so it can skip preloading the language (the default stays preloaded, so every existing spec is unchanged):

```typescript
function setUp(preloadLangs = true) {
```

and inside it pass the flag: `preloadLangs,` instead of `preloadLangs: true,`.

Add a new `describe` block inside `describe('ToastService', ...)`:

```typescript
    describe('info', () => {
        it('shows an info toast with a title and a detail', async () => {
            //given
            const { toast, add } = setUp();

            //when
            await toast.info('access.refusedTitle', { key: 'access.refusedDetail' });

            //then
            expect(add).toHaveBeenCalledWith({
                severity: 'info',
                summary: 'You don\'t have access to that page',
                detail: 'We\'ve taken you to your Week Schedule.',
            });
        });

        it('waits for the translations before showing the info toast', async () => {
            //given
            const { toast, add } = setUp(false);

            //when
            await toast.info('access.refusedTitle', { key: 'access.refusedDetail' });

            //then
            expect(shownSummary(add)).toBe('You don\'t have access to that page');
        });
    });
```

Add inside the existing `describe('apiError', ...)` block:

```typescript
        it('says the User may not do that when the server refuses with 403', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.apiError(new HttpErrorResponse({ status: HTTP_FORBIDDEN }));

            //then
            expect(shownSummary(add)).toBe('You don\'t have permission to do that.');
        });
```

- [ ] **Step 2: Run them to verify they fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/services/toast.service.spec.ts
```

Expected: FAIL: `Property 'info' does not exist on type 'ToastService'`.

- [ ] **Step 3: Implement `info` and the 403 key**

In `client\src\app\core\services\toast.service.ts`:

Add `import { firstValueFrom } from 'rxjs';`.

Add the 403 entry to `GENERIC_ERROR_KEYS`:

```typescript
const GENERIC_ERROR_KEYS: Partial<Record<number, string>> = {
    [HttpStatusCode.Forbidden]: 'errors.forbidden',
    [HttpStatusCode.NotFound]: 'errors.notFound',
    [HttpStatusCode.Conflict]: 'errors.conflict',
};
```

Add the method after `success`:

```typescript
    async info(key: string, detail: ToastDetail): Promise<void> {
        const activeLang = this.transloco.getActiveLang();
        await firstValueFrom(this.transloco.load(activeLang));

        this.messages.add({
            severity: 'info',
            summary: this.transloco.translate(key),
            detail: this.transloco.translate(detail.key, detail.params),
        });
    }
```

Run the spec again (same command). Expected: PASS.

- [ ] **Step 4: Write the failing guard specs**

Create `client\src\app\core\role.guards.spec.ts`:

```typescript
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Route, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './services/toast.service';
import { administratorGuard, homeGuard } from './role.guards';

interface SignedInAs {
    isAuthenticated: boolean;
    isAdministrator: boolean;
}

function setUp(user: SignedInAs) {
    const info = vi.fn().mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            {
                provide: AuthService,
                useValue: { isAuthenticated: signal(user.isAuthenticated), isAdministrator: signal(user.isAdministrator) },
            },
            { provide: ToastService, useValue: { info } },
        ],
    });

    return { info, router: TestBed.inject(Router) };
}

function urlOf(router: Router, result: unknown): string {
    return router.serializeUrl(result as UrlTree);
}

function runAdministratorGuard(): unknown {
    return TestBed.runInInjectionContext(() => administratorGuard({} as Route, []));
}

function runHomeGuard(): unknown {
    return TestBed.runInInjectionContext(() => homeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
}

describe('administratorGuard', () => {
    it('lets an Administrator into an Administrator-only screen', () => {
        //given
        const { info } = setUp({ isAuthenticated: true, isAdministrator: true });

        //when
        const result = runAdministratorGuard();

        //then
        expect(result).toBe(true);
        expect(info).not.toHaveBeenCalled();
    });

    it('sends a Teacher to weekly prep with the no-access toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: true, isAdministrator: false });

        //when
        const result = runAdministratorGuard();

        //then
        expect(urlOf(router, result)).toBe('/week-schedules');
        expect(info).toHaveBeenCalledWith('access.refusedTitle', { key: 'access.refusedDetail' });
    });

    it('sends a signed-out visitor to sign in without the no-access toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: false, isAdministrator: false });

        //when
        const result = runAdministratorGuard();

        //then
        expect(urlOf(router, result)).toBe('/login');
        expect(info).not.toHaveBeenCalled();
    });
});

describe('homeGuard', () => {
    it('keeps an Administrator on the dashboard', () => {
        //given
        setUp({ isAuthenticated: true, isAdministrator: true });

        //when
        const result = runHomeGuard();

        //then
        expect(result).toBe(true);
    });

    it('sends a Teacher from the dashboard to weekly prep without a toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: true, isAdministrator: false });

        //when
        const result = runHomeGuard();

        //then
        expect(urlOf(router, result)).toBe('/week-schedules');
        expect(info).not.toHaveBeenCalled();
    });
});
```

Why the signed-out case matters: `canMatch` on a child route runs during route recognition, **before** the parent's `authGuard` (`canActivate`). Without it, a signed-out visitor opening `/users` would see "you don't have access" on the way to the login page.

- [ ] **Step 5: Run them to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/role.guards.spec.ts
```

Expected: FAIL: `Cannot find module './role.guards'`.

- [ ] **Step 6: Implement the guards**

Create `client\src\app\core\role.guards.ts` (core files use 2-space indentation, like `auth.guard.ts`):

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AppRoutes } from '../shared/config/app-routes';
import { AuthService } from './auth.service';
import { ToastService } from './services/toast.service';

export const administratorGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/', AppRoutes.login]);
  }

  if (auth.isAdministrator()) {
    return true;
  }

  void inject(ToastService).info('access.refusedTitle', { key: 'access.refusedDetail' });

  return router.createUrlTree(['/', AppRoutes.weekSchedules]);
};

export const homeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);

  return auth.isAdministrator() ? true : inject(Router).createUrlTree(['/', AppRoutes.weekSchedules]);
};
```

Run the spec again (same command). Expected: PASS.

- [ ] **Step 7: Guard the routes**

Replace `client\src\app\features\admin-shell\admin.routes.ts` with:

```typescript
import { Routes } from '@angular/router';
import { administratorGuard, homeGuard } from '../../core/role.guards';
import { AppRoutes } from '../../shared/config/app-routes';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', pathMatch: 'full', component: DashboardComponent, canActivate: [homeGuard] },
      {
        path: AppRoutes.teachers,
        canMatch: [administratorGuard],
        loadChildren: () => import('../teachers/teachers.routes'),
      },
      {
        path: AppRoutes.roster,
        canMatch: [administratorGuard],
        loadChildren: () => import('../roster/roster.routes'),
      },
      {
        path: AppRoutes.weekSchedules,
        loadChildren: () => import('../week-schedules/week-schedules.routes'),
      },
      {
        path: AppRoutes.publications,
        loadChildren: () => import('../publications/publications.routes'),
      },
      {
        path: AppRoutes.users,
        canMatch: [administratorGuard],
        loadChildren: () => import('../users/users.routes'),
      },
    ],
  },
];
```

Two details matter here:
- `homeGuard` is `canActivate` with `pathMatch: 'full'`, **not** `canMatch`. An empty-path child matches every URL as a prefix during recognition; a `canMatch` there would run for `/week-schedules` too, and redirecting a Teacher to `/week-schedules` from it would loop forever.
- `administratorGuard` is `canMatch`, so a Teacher never downloads the Teachers, Roster or Users chunks.

- [ ] **Step 8: Add the translations**

`client\public\i18n\he.json`: add a top-level `access` object right before `"general"`:

```json
  "access": {
    "refusedTitle": "אין לך גישה לדף הזה",
    "refusedDetail": "הועברת למערכת השבועית שלך."
  },
```

and inside the top-level `errors` object, right after `"notFound"`:

```json
    "forbidden": "אין לך הרשאה לבצע את הפעולה הזו.",
```

`client\public\i18n\en.json`: the same places:

```json
  "access": {
    "refusedTitle": "You don't have access to that page",
    "refusedDetail": "We've taken you to your Week Schedule."
  },
```

```json
    "forbidden": "You don't have permission to do that.",
```

- [ ] **Step 9: Update the client architecture rule**

In `.claude\rules\client-architecture.md`, replace the bullet that starts "Admin routes are guarded by `authGuard`" with:

```markdown
- Admin routes are guarded by `authGuard` (functional, `inject()`); the student form route is anonymous — the unguessable link token is the access control, mirroring the backend's `[AllowAnonymous]` posture
- Role checks live in `core\role.guards.ts` (#89): Administrator-only feature routes add `canMatch: [administratorGuard]` (a Teacher-role User is sent to Weekly prep with an info toast, and never downloads the chunk); the dashboard uses `canActivate: [homeGuard]` with `pathMatch: 'full'` (a Teacher-role User lands on Weekly prep silently). They are UX only — the server's policies are the security boundary
```

- [ ] **Step 10: Run the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (including `translations.spec.ts`, which checks that both files have the same keys and no long dashes), build succeeds.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/core/services/toast.service.ts client/src/app/core/services/toast.service.spec.ts client/src/app/core/role.guards.ts client/src/app/core/role.guards.spec.ts client/src/app/features/admin-shell/admin.routes.ts client/public/i18n/he.json client/public/i18n/en.json .claude/rules/client-architecture.md
git commit -m "feat(client): Administrator-only routes send a Teacher to Weekly prep with an info toast (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
