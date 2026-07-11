# Task 10 of 12: Teachers feature — UI, routing, navigation, i18n

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires task 9 complete (uncommitted). This task ends with **one commit covering tasks 9–10**.

## Shared Context

**Goal:** The teachers screen per the mockup (`Driving Lesson Mockup\mock\admin.jsx` → `AdminTeachers`): page header + Add-teacher button, 2-column card grid (avatar, name, email, Edit), stacked car rows with transmission pills, always-available Add-car per card, DynamicDialog forms, shell topbar navigation, and the `teachers` i18n namespace in both languages.

**Conventions:** smart page injects store + `DialogService`; dialogs are dumb (data in via config, result out via `ref.close`); dumb components use `input()`/`output()`/`computed()` only; every string is a key; logical CSS properties only; the pink `MkNote` callouts in the mockup are designer annotations — do not implement.

---

**Files:**
- Create: `client/src/app/features/teachers/ui/pages/teachers-list/teachers-list.page.ts`, `.html`, `.scss`
- Create: `client/src/app/features/teachers/ui/components/teacher-card/teacher-card.component.ts`, `.html`, `.scss`
- Create: `client/src/app/features/teachers/ui/components/car-row/car-row.component.ts`, `.html`, `.scss`
- Create: `client/src/app/features/teachers/ui/dialogs/teacher-form/teacher-form.dialog.ts`, `.html`
- Create: `client/src/app/features/teachers/ui/dialogs/car-form/car-form.dialog.ts`, `.html`
- Create: `client/src/app/features/teachers/ui/dialogs/dialog-form.scss` (shared dialog form styles)
- Create: `client/src/app/features/teachers/teachers.routes.ts`
- Modify: `client/src/app/features/admin-shell/admin.routes.ts`, `admin-shell.component.ts`, `.html`, `.scss`
- Modify: `client/public/i18n/en.json`, `client/public/i18n/he.json`

- [x] **Step 1: `ui/dialogs/teacher-form/teacher-form.dialog.ts`**

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Teacher } from '../../../domain/teacher.model';

export interface TeacherFormResult {
    name: string;
    contactEmail: string;
}

@Component({
    selector: 'app-teacher-form-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule],
    templateUrl: './teacher-form.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherFormDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<{ teacher?: Teacher }>);

    protected readonly form = this.fb.nonNullable.group({
        name: [this.config.data?.teacher?.name ?? '', Validators.required],
        contactEmail: [this.config.data?.teacher?.contactEmail ?? '', [Validators.required, Validators.email]],
    });

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: TeacherFormResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

- [x] **Step 2: `ui/dialogs/teacher-form/teacher-form.dialog.html`**

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  <div class="field">
    <label for="teacher-name">{{ 'teachers.name' | transloco }}</label>
    <input pInputText id="teacher-name" formControlName="name" />
  </div>

  <div class="field">
    <label for="teacher-email">{{ 'teachers.contactEmail' | transloco }}</label>
    <input pInputText id="teacher-email" formControlName="contactEmail" type="email" dir="ltr" />
    @if (form.controls.contactEmail.dirty && form.controls.contactEmail.invalid) {
      <small class="field__error">{{ 'formErrors.emailInvalid' | transloco }}</small>
    }
  </div>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="form.invalid" />
  </div>
</form>
```

- [x] **Step 3: `ui/dialogs/car-form/car-form.dialog.ts`**

Transmission options carry labels translated at open time (point-in-time strings, same rule as dialog headers).

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Car } from '../../../domain/car.model';
import { Transmission } from '../../../domain/transmission.enum';

export interface CarFormResult {
    name: string;
    type: string;
    transmission: Transmission;
}

@Component({
    selector: 'app-car-form-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule, SelectModule],
    templateUrl: './car-form.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarFormDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<{ car?: Car }>);
    private readonly transloco = inject(TranslocoService);

    protected readonly transmissionOptions = Object.values(Transmission).map((value) => ({
        value,
        label: this.transloco.translate(`teachers.transmissions.${value}`),
    }));

    protected readonly form = this.fb.nonNullable.group({
        name: [this.config.data?.car?.name ?? '', Validators.required],
        type: [this.config.data?.car?.type ?? '', Validators.required],
        transmission: [this.config.data?.car?.transmission ?? Transmission.automatic, Validators.required],
    });

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: CarFormResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

- [x] **Step 4: `ui/dialogs/car-form/car-form.dialog.html`**

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  <div class="field">
    <label for="car-name">{{ 'teachers.carName' | transloco }}</label>
    <input pInputText id="car-name" formControlName="name" />
  </div>

  <div class="field">
    <label for="car-type">{{ 'teachers.carType' | transloco }}</label>
    <input pInputText id="car-type" formControlName="type" />
  </div>

  <div class="field">
    <label for="car-transmission">{{ 'teachers.transmission' | transloco }}</label>
    <p-select
      inputId="car-transmission"
      formControlName="transmission"
      [options]="transmissionOptions"
      optionLabel="label"
      optionValue="value"
      appendTo="body" />
  </div>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="form.invalid" />
  </div>
</form>
```

- [x] **Step 5: `ui/dialogs/dialog-form.scss` (shared by both dialogs)**

```scss
.dialog-form {
  display: flex;
  flex-direction: column;
  gap: 1.125rem;
  padding-block-start: 0.25rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;

  label {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.8rem;
    color: var(--app-text-primary);
  }

  input {
    width: 100%;
  }
}

.field__error {
  color: var(--p-red-500);
  font-size: 0.75rem;
}

.dialog-form__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.625rem;
  margin-block-start: 0.5rem;
}
```

- [x] **Step 6: `ui/components/car-row/car-row.component.ts`**

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Car } from '../../../domain/car.model';
import { Transmission } from '../../../domain/transmission.enum';

@Component({
    selector: 'app-car-row',
    imports: [TranslocoPipe],
    templateUrl: './car-row.component.html',
    styleUrl: './car-row.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarRowComponent {
    readonly car = input.required<Car>();

    readonly edit = output<void>();

    protected readonly isManual = computed(() => this.car().transmission === Transmission.manual);
}
```

- [x] **Step 7: `ui/components/car-row/car-row.component.html`**

```html
<button type="button" class="car" (click)="edit.emit()">
  <span class="car__icon" aria-hidden="true"><i class="pi pi-car"></i></span>
  <span class="car__identity">
    <span class="car__name">{{ car().name }}</span>
    <span class="car__type">{{ car().type }}</span>
  </span>
  <span class="car__pill" [class.car__pill--manual]="isManual()">
    {{ 'teachers.transmissions.' + car().transmission | transloco }}
  </span>
</button>
```

- [x] **Step 8: `ui/components/car-row/car-row.component.scss`**

```scss
.car {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  inline-size: 100%;
  padding: 0.7rem 1rem;
  background: var(--app-bg-page);
  border: 1px solid var(--app-border);
  border-radius: 8px;
  cursor: pointer;
  text-align: start;
  font: inherit;
}

.car__icon {
  inline-size: 2.125rem;
  block-size: 2.125rem;
  border-radius: 8px;
  background: var(--app-steel-light);
  color: var(--app-whale);
  display: flex;
  align-items: center;
  justify-content: center;
}

.car__identity {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.car__name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--app-text-primary);
}

.car__type {
  font-size: 0.75rem;
  color: var(--app-text-secondary);
}

.car__pill {
  margin-inline-start: auto;
  font-family: var(--app-font-display);
  font-weight: 800;
  font-size: 0.625rem;
  letter-spacing: 0.07em;
  text-transform: uppercase;
  padding: 0.25rem 0.7rem;
  border-radius: 999px;
  background: var(--p-blue-50);
  color: var(--p-blue-800);
}

.car__pill--manual {
  background: var(--app-border);
  color: var(--app-text-secondary);
}
```

- [x] **Step 9: `ui/components/teacher-card/teacher-card.component.ts`**

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { CarRowComponent } from '../car-row/car-row.component';

@Component({
    selector: 'app-teacher-card',
    imports: [TranslocoPipe, ButtonModule, CarRowComponent],
    templateUrl: './teacher-card.component.html',
    styleUrl: './teacher-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherCardComponent {
    readonly teacher = input.required<Teacher>();

    readonly edit = output<void>();
    readonly addCar = output<void>();
    readonly editCar = output<Car>();

    protected readonly initials = computed(() => {
        const parts = this.teacher().name.split(' ');
        const first = parts[0]?.charAt(0) ?? '';
        const second = parts[1]?.charAt(0) ?? '';
        const combined = `${first}${second}` || this.teacher().name.slice(0, 2);

        return combined.toUpperCase();
    });
}
```

- [x] **Step 10: `ui/components/teacher-card/teacher-card.component.html`**

```html
<article class="card">
  <header class="card__header">
    <span class="card__avatar" aria-hidden="true">{{ initials() }}</span>
    <div class="card__identity">
      <h3 class="card__name">{{ teacher().name }}</h3>
      <bdi class="card__email">{{ teacher().contactEmail }}</bdi>
    </div>
    <p-button
      [label]="'teachers.editTeacher' | transloco"
      severity="secondary"
      size="small"
      text
      (onClick)="edit.emit()" />
  </header>

  @if (teacher().cars.length) {
    <div class="card__cars">
      @for (car of teacher().cars; track car.id) {
        <app-car-row [car]="car" (edit)="editCar.emit(car)" />
      }
    </div>
  }

  <div class="card__footer">
    <p-button
      [label]="'teachers.addCar' | transloco"
      severity="secondary"
      size="small"
      outlined
      icon="pi pi-plus"
      (onClick)="addCar.emit()" />
  </div>
</article>
```

- [x] **Step 11: `ui/components/teacher-card/teacher-card.component.scss`**

```scss
.card {
  background: var(--app-bg-card);
  border: 1px solid var(--app-border);
  border-radius: 14px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  align-self: start;
}

.card__header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.card__avatar {
  inline-size: 2.625rem;
  block-size: 2.625rem;
  border-radius: 999px;
  background: var(--app-steel-light);
  color: var(--app-whale);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--app-font-display);
  font-weight: 700;
  font-size: 0.9375rem;
}

.card__identity {
  min-inline-size: 0;
  flex: 1;
}

.card__name {
  margin: 0;
  font-family: var(--app-font-display);
  font-weight: 700;
  font-size: 1.0625rem;
  color: var(--app-text-primary);
}

.card__email {
  font-size: 0.78rem;
  color: var(--app-text-secondary);
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card__cars {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.card__footer {
  display: flex;
}
```

- [x] **Step 12: `ui/pages/teachers-list/teachers-list.page.ts`**

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogService } from 'primeng/dynamicdialog';
import { TeachersStore } from '../../../state/teachers.store';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { TeacherCardComponent } from '../../components/teacher-card/teacher-card.component';
import { CarFormDialog, CarFormResult } from '../../dialogs/car-form/car-form.dialog';
import { TeacherFormDialog, TeacherFormResult } from '../../dialogs/teacher-form/teacher-form.dialog';

@Component({
    selector: 'app-teachers-list-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, TeacherCardComponent],
    templateUrl: './teachers-list.page.html',
    styleUrl: './teachers-list.page.scss',
    providers: [DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeachersListPage {
    private static readonly dialogWidth = '28rem';

    protected readonly store = inject(TeachersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected onAddTeacher(): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.addTeacher'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref.onClose.subscribe((result?: TeacherFormResult) => {
            if (result) {
                void this.store.create(result);
            }
        });
    }

    protected onEditTeacher(teacher: Teacher): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.editTeacher'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { teacher },
        });

        ref.onClose.subscribe((result?: TeacherFormResult) => {
            if (result) {
                void this.store.changeDetails(teacher.id, result);
            }
        });
    }

    protected onAddCar(teacher: Teacher): void {
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.addCar'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.store.addCar(teacher.id, result);
            }
        });
    }

    protected onEditCar(teacher: Teacher, car: Car): void {
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.editCar'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { car },
        });

        ref.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.store.changeCarDetails(teacher.id, car.id, result);
            }
        });
    }
}
```

- [x] **Step 13: `ui/pages/teachers-list/teachers-list.page.html`**

```html
<div class="teachers">
  <header class="teachers__header">
    <div>
      <h2 class="teachers__title">{{ 'teachers.title' | transloco }}</h2>
      <p class="teachers__subtitle">{{ 'teachers.subtitle' | transloco }}</p>
    </div>
    <p-button
      [label]="'teachers.addTeacher' | transloco"
      severity="secondary"
      [disabled]="store.isMutating()"
      (onClick)="onAddTeacher()" />
  </header>

  @if (store.isLoading()) {
    <div class="teachers__loading"><p-progressSpinner /></div>
  } @else if (store.loadError(); as errorKey) {
    <p class="teachers__error">{{ errorKey | transloco }}</p>
  } @else if (store.isEmpty()) {
    <div class="teachers__empty">
      <h3>{{ 'teachers.emptyTitle' | transloco }}</h3>
      <p>{{ 'teachers.emptySubtitle' | transloco }}</p>
    </div>
  } @else {
    <div class="teachers__grid">
      @for (teacher of store.teachers(); track teacher.id) {
        <app-teacher-card
          [teacher]="teacher"
          (edit)="onEditTeacher(teacher)"
          (addCar)="onAddCar(teacher)"
          (editCar)="onEditCar(teacher, $event)" />
      }
    </div>
  }
</div>
```

- [x] **Step 14: `ui/pages/teachers-list/teachers-list.page.scss`**

```scss
.teachers__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1.5rem;
}

.teachers__title {
  margin: 0;
  font-family: var(--app-font-display);
  font-weight: 500;
  font-size: 1.625rem;
  color: var(--app-text-primary);
  letter-spacing: -0.01em;
}

.teachers__subtitle {
  margin: 0.25rem 0 0;
  font-size: 0.84rem;
  color: var(--app-text-secondary);
}

.teachers__grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
  margin-block-start: 1.375rem;

  @media (max-width: 991px) {
    grid-template-columns: 1fr;
  }
}

.teachers__loading {
  display: flex;
  justify-content: center;
  padding-block: 3rem;
}

.teachers__error {
  color: var(--p-red-500);
  margin-block-start: 1.5rem;
}

.teachers__empty {
  margin-block-start: 1.375rem;
  padding: 2.5rem;
  text-align: center;
  background: var(--app-bg-card);
  border: 1px solid var(--app-border);
  border-radius: 14px;

  h3 {
    margin: 0;
    font-family: var(--app-font-display);
    color: var(--app-text-primary);
  }

  p {
    margin: 0.5rem 0 0;
    color: var(--app-text-secondary);
    font-size: 0.85rem;
  }
}
```

- [x] **Step 15: `client/src/app/features/teachers/teachers.routes.ts`**

```typescript
import { Routes } from '@angular/router';
import { TeachersListPage } from './ui/pages/teachers-list/teachers-list.page';

export default [{ path: '', component: TeachersListPage }] satisfies Routes;
```

- [x] **Step 16: Wire the route — `client/src/app/features/admin-shell/admin.routes.ts` (full replacement)**

```typescript
import { Routes } from '@angular/router';
import { AppRoutes } from '../../shared/config/app-routes';
import { AdminShellComponent } from './admin-shell.component';
import { DashboardComponent } from './dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', component: DashboardComponent },
      {
        path: AppRoutes.teachers,
        loadChildren: () => import('../teachers/teachers.routes'),
      },
    ],
  },
];
```

- [x] **Step 17: Shell topbar navigation**

`client/src/app/features/admin-shell/admin-shell.component.ts` (full replacement):

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/auth.service';
import { AppRoutes } from '../../shared/config/app-routes';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../shared/language-toggle/language-toggle.component';

@Component({
  selector: 'app-admin-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoPipe,
    ButtonModule,
    BrandLogoComponent,
    LanguageToggleComponent,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  protected readonly auth = inject(AuthService);

  protected readonly appRoutes = AppRoutes;

  protected readonly initials = computed(() => {
    const email = this.auth.email();
    return email ? email.slice(0, 2).toUpperCase() : 'AD';
  });
}
```

`admin-shell.component.html` — insert the `<nav>` between the logo and the actions (full replacement):

```html
<div class="shell">
  <header class="shell__bar">
    <app-brand-logo />

    <nav class="shell__nav">
      <a
        routerLink="/"
        routerLinkActive="shell__nav-link--active"
        [routerLinkActiveOptions]="{ exact: true }"
        class="shell__nav-link">
        {{ 'shell.nav.dashboard' | transloco }}
      </a>
      <a
        [routerLink]="['/', appRoutes.teachers]"
        routerLinkActive="shell__nav-link--active"
        class="shell__nav-link">
        {{ 'shell.nav.teachers' | transloco }}
      </a>
    </nav>

    <div class="shell__actions">
      <app-language-toggle />

      <div class="shell__user">
        <span class="shell__avatar" aria-hidden="true">{{ initials() }}</span>
        @if (auth.email(); as email) {
          <span class="shell__email">{{ email }}</span>
        }
      </div>

      <p-button
        [label]="'shell.logout' | transloco"
        (onClick)="auth.logout()"
        severity="secondary"
        size="small"
        text />
    </div>
  </header>

  <main class="shell__main">
    <router-outlet />
  </main>
</div>
```

`admin-shell.component.scss` — append the nav styles (active underline per the mockup):

```scss
.shell__nav {
  display: flex;
  gap: 1.625rem;
  align-self: stretch;
}

.shell__nav-link {
  display: flex;
  align-items: center;
  position: relative;
  font-size: 0.9rem;
  color: var(--app-text-secondary);
  text-decoration: none;
}

.shell__nav-link--active {
  color: var(--app-text-primary);
  font-weight: 500;
}

.shell__nav-link--active::after {
  content: '';
  position: absolute;
  inset-block-end: 0;
  inset-inline: 0;
  height: 3px;
  background: linear-gradient(135deg, var(--p-primary-500) 0%, var(--app-ocean, #00bbc7) 100%);
  border-radius: 3px 3px 0 0;
}
```

If `--app-ocean` is not defined in `client/src/styles/_tokens.scss`, add it there (`--app-ocean: #00bbc7;`) rather than hardcoding twice.

- [x] **Step 18: i18n — add to BOTH files in the same change**

`client/public/i18n/en.json` — add inside `shell` and as a new top-level `teachers` namespace:

```json
  "shell": {
    "title": "Driving Lessons Planner",
    "logout": "Sign out",
    "languageToggle": "עברית",
    "nav": {
      "dashboard": "Dashboard",
      "teachers": "Teachers & cars"
    }
  },
  "teachers": {
    "title": "Teachers & cars",
    "subtitle": "Scheduling is per teacher; the roster assigns each student a car.",
    "addTeacher": "Add teacher",
    "editTeacher": "Edit",
    "addCar": "Add car",
    "editCar": "Edit car",
    "name": "Name",
    "contactEmail": "Contact email",
    "carName": "Car name",
    "carType": "Vehicle type",
    "transmission": "Transmission",
    "transmissions": {
      "automatic": "Automatic",
      "manual": "Manual"
    },
    "created": "Teacher created",
    "detailsChanged": "Teacher details updated",
    "carAdded": "Car added",
    "carChanged": "Car details updated",
    "loadFailed": "Loading teachers failed",
    "emptyTitle": "No teachers yet",
    "emptySubtitle": "Create the first teacher to start planning weeks."
  }
```

`client/public/i18n/he.json`:

```json
  "shell": {
    "title": "מערכת תכנון שיעורי נהיגה",
    "logout": "התנתקות",
    "languageToggle": "English",
    "nav": {
      "dashboard": "לוח בקרה",
      "teachers": "מורים ורכבים"
    }
  },
  "teachers": {
    "title": "מורים ורכבים",
    "subtitle": "התכנון הוא לפי מורה; קובץ התלמידים משייך לכל תלמיד רכב.",
    "addTeacher": "הוספת מורה",
    "editTeacher": "עריכה",
    "addCar": "הוספת רכב",
    "editCar": "עריכת רכב",
    "name": "שם",
    "contactEmail": "אימייל ליצירת קשר",
    "carName": "שם הרכב",
    "carType": "סוג הרכב",
    "transmission": "תיבת הילוכים",
    "transmissions": {
      "automatic": "אוטומטי",
      "manual": "ידני"
    },
    "created": "המורה נוצר",
    "detailsChanged": "פרטי המורה עודכנו",
    "carAdded": "הרכב נוסף",
    "carChanged": "פרטי הרכב עודכנו",
    "loadFailed": "טעינת המורים נכשלה",
    "emptyTitle": "אין מורים עדיין",
    "emptySubtitle": "צרו את המורה הראשון כדי להתחיל לתכנן שבועות."
  }
```

- [x] **Step 19: Verify end-to-end in the browser**

Run the API (`dotnet run --project src/DrivingLessons.Presentation.Web`) and the client (`npm start` from `client\`, proxying `/api` per the existing dev setup). Sign in, then:

1. Topbar shows Dashboard + Teachers & cars; the active item is underlined.
2. Teachers page: empty state → Add teacher → dialog → save → toast + card appears.
3. Add car → dialog with transmission select → save → car row with pill.
4. Add a second and third car — all appear (no maximum).
5. Edit teacher email and a car's transmission → values update after the automatic reload.
6. Force a 409 (e.g. temporarily submit an invalid email past the form by relaxing the client validator) → red toast shows the backend message. Revert any temporary change.
7. Toggle to Hebrew — layout mirrors, all strings translated.

- [x] **Step 20: Commit tasks 9–10 together**

```bash
git add client
git commit -m "feat(client): teachers feature with card grid, dialogs, store, and shell navigation"
```

---

**Next:** [task-11-rtl-audit.md](task-11-rtl-audit.md)
