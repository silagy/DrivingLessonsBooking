# Task 8 of 11: Client auth — service, interceptor, guard, routes

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–7 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`.
>
> **Commit note:** no separate commit — Tasks 7–9 commit together once the client compiles (see task 9, step 8). Task 7's `app.config.ts` already imports `./core/auth.interceptor`, created here.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Client stack:** Angular 21+ (zoneless, standalone, signals only), PrimeNG, Transloco (he/en, RTL-first). The API issues JWT bearer tokens via `POST /api/auth/login` (built in tasks 4–6).

**Token storage decision: `localStorage`** — the single admin survives page refreshes and tab reopens without re-login; XSS exposure is an accepted v1 risk for a one-user internal tool (same posture as the PRD's email-only student identity).

---

**Files:**
- Create: `client/src/app/core/auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts`
- Replace: `client/src/app/app.routes.ts`

- [ ] **Step 1: `auth.service.ts`**

```ts
import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

const TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly isAuthenticated = computed(() => this.token() !== null);

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(TOKEN_KEY, response.accessToken);
          this.token.set(response.accessToken);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
    void this.router.navigate(['/login']);
  }
}
```

- [ ] **Step 2: `auth.interceptor.ts`**

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Expired/invalid token on any API call (except the login attempt itself): drop session, go to login.
      if (error.status === 401 && !req.url.includes('/api/auth/login')) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
```

- [ ] **Step 3: `auth.guard.ts`**

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};
```

- [ ] **Step 4: `app.routes.ts`**

```ts
import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
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

---

**Next:** [task-09-login-and-shell.md](task-09-login-and-shell.md)
