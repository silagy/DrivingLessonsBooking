# Task 9 of 11: Login page + admin shell

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–8 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`.
>
> **Commit note:** step 8 commits the combined work of tasks 7–9.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Client stack:** Angular 21+ (zoneless, standalone, signals only), PrimeNG, Transloco (he/en, RTL-first). Every visible string is a translation key (keys created in task 7); layouts must be mirror-correct under `dir="rtl"` — use only logical CSS (`text-align: start`, `margin-inline`) in custom styles.

---

**Files:**
- Create: `client/src/app/features/auth/login.component.ts`, `.html`, `.scss`
- Create: `client/src/app/features/admin-shell/admin.routes.ts`, `admin-shell.component.ts`, `.html`, `dashboard.component.ts`

- [ ] **Step 1: `login.component.ts`**

```ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TranslocoPipe, InputTextModule, PasswordModule, ButtonModule, MessageModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly loading = signal(false);
  protected readonly error = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      return;
    }
    this.loading.set(true);
    this.error.set(false);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      },
    });
  }
}
```

- [ ] **Step 2: `login.component.html`** (logical CSS + PrimeNG, mirror-correct under `dir="rtl"`)

```html
<div class="login-page">
  <form class="login-card" [formGroup]="form" (ngSubmit)="submit()">
    <h1>{{ 'auth.title' | transloco }}</h1>

    <label for="email">{{ 'auth.email' | transloco }}</label>
    <input pInputText id="email" type="email" formControlName="email" autocomplete="username" />

    <label for="password">{{ 'auth.password' | transloco }}</label>
    <p-password
      inputId="password"
      formControlName="password"
      [feedback]="false"
      [toggleMask]="true"
      styleClass="login-password" />

    @if (error()) {
      <p-message severity="error" [text]="'auth.invalidCredentials' | transloco" />
    }

    <p-button
      type="submit"
      [label]="'auth.submit' | transloco"
      [loading]="loading()"
      [disabled]="form.invalid" />
  </form>
</div>
```

- [ ] **Step 3: `login.component.scss`**

```scss
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-card {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  width: min(22rem, 90vw);

  label {
    text-align: start; // logical property: correct in both LTR and RTL
  }

  :is(input, .login-password) {
    width: 100%;
  }
}
```

- [ ] **Step 4: `admin.routes.ts`**

```ts
import { Routes } from '@angular/router';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [{ path: '', component: DashboardComponent }],
  },
];
```

- [ ] **Step 5: `admin-shell.component.ts`**

```ts
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, TranslocoPipe, ToolbarModule, ButtonModule],
  templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly language = inject(LanguageService);
}
```

- [ ] **Step 6: `admin-shell.component.html`**

```html
<p-toolbar>
  <ng-template #start>
    <strong>{{ 'shell.title' | transloco }}</strong>
  </ng-template>
  <ng-template #end>
    <p-button [label]="'shell.languageToggle' | transloco" (onClick)="language.toggle()" text />
    <p-button [label]="'shell.logout' | transloco" (onClick)="auth.logout()" severity="secondary" text />
  </ng-template>
</p-toolbar>
<main style="padding: 1rem">
  <router-outlet />
</main>
```

- [ ] **Step 7: `dashboard.component.ts`**

```ts
import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-dashboard',
  imports: [TranslocoPipe],
  template: `
    <h2>{{ 'dashboard.title' | transloco }}</h2>
    <p>{{ 'dashboard.placeholder' | transloco }}</p>
  `,
})
export class DashboardComponent {}
```

- [ ] **Step 8: Verify client builds, then commit Tasks 7–9**

Run (in `client/`): `npm run build` — expected: success. If the initial-bundle budget fails, raise `budgets` in `angular.json` (warning 1.5MB / error 2MB).

```bash
git add client
git commit -m "feat(client): angular workspace with primeng/transloco, jwt auth flow, login page, admin shell"
```

---

**Next:** [task-10-docker.md](task-10-docker.md)
