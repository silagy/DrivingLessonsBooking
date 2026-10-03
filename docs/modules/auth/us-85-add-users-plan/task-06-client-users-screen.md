# Task 6 of 7: Users screen, Add User dialog and shell navigation

> Part of [#85: Add Users from a New Users Screen](README.md). Requires tasks 1 to 5 committed. Work on branch `85-add-users-screen`.

**Files:**
- Create: `client\src\app\features\users\users.routes.ts`
- Create: `client\src\app\features\users\ui\pages\users\users.page.ts`, `users.page.html`, `users.page.scss`
- Create: `client\src\app\features\users\ui\dialogs\add-user\add-user.dialog.ts`, `add-user.dialog.html`, `add-user.dialog.scss`
- Create: `client\src\app\features\users\ui\dialogs\dialog-form.scss`
- Modify: `client\src\app\shared\config\app-routes.ts` (`users`)
- Modify: `client\src\app\features\admin-shell\admin.routes.ts` (lazy `users` route)
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html` (nav entry, last)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (`shell.nav.users`, `users.*`, six `errors.*` keys)

**Interfaces:**
- Consumes: from task 5, `UsersStore` (`users`, `linkableTeachers`, `isLoading`, `loadError`, `hasOnlyOneUser`, `isMutating`, `create(request)`), `Role`, `LinkableTeacher`, `isTeacherLinkRequired(role)`, `CreateUserRequest`. Existing `DialogService` / `DynamicDialogRef` / `DynamicDialogConfig` pattern from `features\teachers\ui\pages\cars-and-teachers\cars-and-teachers.page.ts`, PrimeNG `TableModule`, `TagModule`, `SelectModule`, `SelectButtonModule`, `PasswordModule`, `InputTextModule`, `ButtonModule`, `ProgressSpinnerModule`.
- Produces:
  - route `/users` (lazy, under the admin shell, behind the existing `authGuard`)
  - `AddUserDialog` with `AddUserDialogData { teachers: LinkableTeacher[] }` in and `AddUserResult` (same shape as `CreateUserRequest`) out
  - translation keys `shell.nav.users`, `users.*`, `errors.teacherAlreadyLinkedToUser`, `errors.temporaryPasswordMustNotBeEmpty`, `errors.userNameMustNotBeEmpty`, `errors.userNotFound`, `errors.userSignInEmailAlreadyInUse`, `errors.userWithTeacherRoleMustHaveLinkedTeacher`

**Why:** #85 acceptance criteria 1, 2, 10 and 11; the brief's sections 3 and 4 ([claude-design-prompt.md](../claude-design-prompt.md)). README decisions 9 to 13.

**What the screen looks like** (from the brief; RTL in Hebrew, so "end" is the left side):
- Header: title "Users", subtitle, and the primary **Add User** button at the end.
- A card with a PrimeNG table: Name · Sign-in email (LTR, isolated) · Role (Tag: Administrator `info`, Teacher `secondary`) · Linked teacher (name, or muted "None") · Status (Tag: Active `success`, Deleted `secondary`). Deleted rows are muted and come last (the server orders them).
- Loading: spinner. Error: inline red text. When only one User exists, a one-line hint under the table invites adding a User for each teacher (the brief's "empty" state: the first Administrator always exists).
- Add User dialog: Name · Sign-in email (LTR) · Role (SelectButton, Teacher preselected) · Linked teacher (Select; Teachers that already have a User are disabled and say so; clearable only for an Administrator; hint says required or optional and that the link can't be changed later) · Temporary password (Password with show/hide, LTR; hint that no email is sent). Footer: Cancel (text, secondary), Save (primary, disabled while the form is invalid).
- A server refusal (409) closes nothing extra: the dialog has already closed with its result, and the store shows the translated `errors.{code}` toast, as the Teachers screen does.

**Commands** (from `client\` in PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

- [ ] **Step 1: Add the route constant, the lazy route and the nav entry**

In `client\src\app\shared\config\app-routes.ts`, add `users` after `publicationsHistory`:

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
    roster: 'roster',
    weekSchedules: 'week-schedules',
    publications: 'publications',
    publicationsHistory: 'history',
    users: 'users',
    studentForm: 's',
} as const;
```

In `client\src\app\features\admin-shell\admin.routes.ts`, add after the `publications` child route:

```typescript
      {
        path: AppRoutes.users,
        loadChildren: () => import('../users/users.routes'),
      },
```

In `client\src\app\features\admin-shell\admin-shell.component.html`, add after the History link (the brief puts Users last):

```html
      <a
        [routerLink]="['/', appRoutes.users]"
        routerLinkActive="shell__nav-link--active"
        class="shell__nav-link">
        {{ 'shell.nav.users' | transloco }}
      </a>
```

Create `client\src\app\features\users\users.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { UsersPage } from './ui/pages/users/users.page';

export default [{ path: '', component: UsersPage }] satisfies Routes;
```

- [ ] **Step 2: Add the Add User dialog**

Create `client\src\app\features\users\ui\dialogs\dialog-form.scss` (the `teachers` feature's form styles plus a hint style; features don't share files):

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
    color: var(--app-ink);
  }

  input {
    width: 100%;
  }
}

.field__hint {
  font-size: 0.72rem;
  line-height: 1.4;
  color: var(--app-text-muted);
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

Create `client\src\app\features\users\ui\dialogs\add-user\add-user.dialog.scss`:

```scss
.teacher-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  inline-size: 100%;
}

.teacher-option__note {
  font-size: 0.72rem;
  color: var(--app-text-muted);
}
```

Create `client\src\app\features\users\ui\dialogs\add-user\add-user.dialog.ts`. The form-level validator duplicates the server's "Teacher required for the Teacher Role" rule for immediate feedback only (README decision 11). `toSignal` reads the Role without a manual subscription:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    FormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    ValidatorFn,
    Validators,
} from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { LinkableTeacher } from '../../../domain/linkable-teacher.model';
import { Role } from '../../../domain/role.enum';
import { isTeacherLinkRequired } from '../../../domain/teacher-link';

export interface AddUserDialogData {
    teachers: LinkableTeacher[];
}

export interface AddUserResult {
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    temporaryPassword: string;
}

const TEACHER_LINK_MISSING = 'teacherLinkMissing';

const teacherLinkValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
    const role = group.get('role')?.value as Role;
    const teacherId = group.get('teacherId')?.value as string | null;
    const missing = isTeacherLinkRequired(role) && !teacherId;

    return missing ? { [TEACHER_LINK_MISSING]: true } : null;
};

@Component({
    selector: 'app-add-user-dialog',
    imports: [
        ReactiveFormsModule,
        TranslocoPipe,
        ButtonModule,
        InputTextModule,
        PasswordModule,
        SelectModule,
        SelectButtonModule,
    ],
    templateUrl: './add-user.dialog.html',
    styleUrls: ['../dialog-form.scss', './add-user.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddUserDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<AddUserDialogData>);
    private readonly transloco = inject(TranslocoService);

    protected readonly teacherLinkMissing = TEACHER_LINK_MISSING;

    protected readonly teachers: LinkableTeacher[] = this.config.data?.teachers ?? [];

    protected readonly roleOptions = Object.values(Role).map((value) => ({
        value,
        label: this.transloco.translate(`users.roles.${value}`),
    }));

    protected readonly form = this.fb.group(
        {
            name: this.fb.nonNullable.control('', Validators.required),
            signInEmail: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
            role: this.fb.nonNullable.control(Role.teacher, Validators.required),
            teacherId: this.fb.control<string | null>(null),
            temporaryPassword: this.fb.nonNullable.control('', Validators.required),
        },
        { validators: teacherLinkValidator },
    );

    private readonly role = toSignal(this.form.controls.role.valueChanges, {
        initialValue: this.form.controls.role.value,
    });

    protected readonly teacherLinkRequired = computed(() => isTeacherLinkRequired(this.role()));

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: AddUserResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

Create `client\src\app\features\users\ui\dialogs\add-user\add-user.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  <div class="field">
    <label for="user-name">{{ 'users.name' | transloco }}</label>
    <input pInputText id="user-name" formControlName="name" />
  </div>

  <div class="field">
    <label for="user-sign-in-email">{{ 'users.signInEmail' | transloco }}</label>
    <input
      pInputText
      id="user-sign-in-email"
      formControlName="signInEmail"
      type="email"
      dir="ltr"
      autocomplete="off" />
    @if (form.controls.signInEmail.dirty && form.controls.signInEmail.invalid) {
      <small class="field__error">{{ 'formErrors.emailInvalid' | transloco }}</small>
    }
  </div>

  <div class="field">
    <label id="user-role-label">{{ 'users.role' | transloco }}</label>
    <p-selectbutton
      formControlName="role"
      [options]="roleOptions"
      optionLabel="label"
      optionValue="value"
      [allowEmpty]="false"
      ariaLabelledBy="user-role-label" />
  </div>

  <div class="field">
    <label for="user-teacher">{{ 'users.teacher' | transloco }}</label>
    <p-select
      inputId="user-teacher"
      formControlName="teacherId"
      [options]="teachers"
      optionLabel="name"
      optionValue="id"
      optionDisabled="alreadyLinked"
      [placeholder]="'users.chooseTeacher' | transloco"
      [showClear]="!teacherLinkRequired()"
      appendTo="body"
      fluid>
      <ng-template pTemplate="item" let-teacher>
        <span class="teacher-option">
          <span>{{ teacher.name }}</span>
          @if (teacher.alreadyLinked) {
            <small class="teacher-option__note">{{ 'users.alreadyHasUser' | transloco }}</small>
          }
        </span>
      </ng-template>
    </p-select>
    <small class="field__hint">
      {{ (teacherLinkRequired() ? 'users.teacherHintRequired' : 'users.teacherHintOptional') | transloco }}
      {{ 'users.teacherLinkFixed' | transloco }}
    </small>
    @if (form.hasError(teacherLinkMissing) && form.controls.teacherId.touched) {
      <small class="field__error">{{ 'users.teacherRequired' | transloco }}</small>
    }
  </div>

  <div class="field">
    <label for="user-temporary-password">{{ 'users.temporaryPassword' | transloco }}</label>
    <p-password
      inputId="user-temporary-password"
      formControlName="temporaryPassword"
      [feedback]="false"
      [toggleMask]="true"
      autocomplete="new-password"
      dir="ltr"
      fluid />
    <small class="field__hint">{{ 'users.temporaryPasswordHint' | transloco }}</small>
  </div>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="form.invalid" />
  </div>
</form>
```

If the build reports that an input doesn't exist on the installed PrimeNG version (`fluid`, `ariaLabelledBy`, `optionDisabled`, `autocomplete`), check the component's `.d.ts` under `node_modules\primeng\` and use the name it declares. Don't drop the behavior.

- [ ] **Step 3: Add the page**

Create `client\src\app\features\users\ui\pages\users\users.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Role } from '../../../domain/role.enum';
import { UsersStore } from '../../../state/users.store';
import { AddUserDialog, AddUserDialogData, AddUserResult } from '../../dialogs/add-user/add-user.dialog';

type RoleTagSeverity = 'info' | 'secondary';

const ROLE_TAG_SEVERITY: Record<Role, RoleTagSeverity> = {
    [Role.administrator]: 'info',
    [Role.teacher]: 'secondary',
};

@Component({
    selector: 'app-users-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, TableModule, TagModule],
    templateUrl: './users.page.html',
    styleUrl: './users.page.scss',
    providers: [DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
    private static readonly dialogWidth = '30rem';

    protected readonly store = inject(UsersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly roleTagSeverity = ROLE_TAG_SEVERITY;

    protected onAddUser(): void {
        const data: AddUserDialogData = { teachers: this.store.linkableTeachers() };
        const ref = this.dialogs.open(AddUserDialog, {
            header: this.transloco.translate('users.addUser'),
            width: UsersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });

        ref?.onClose.subscribe((result?: AddUserResult) => {
            if (result) {
                void this.store.create(result);
            }
        });
    }
}
```

Create `client\src\app\features\users\ui\pages\users\users.page.html`:

```html
<div class="users">
  <header class="users__header">
    <div>
      <h2 class="users__title">{{ 'users.title' | transloco }}</h2>
      <p class="users__subtitle">{{ 'users.subtitle' | transloco }}</p>
    </div>
    <p-button
      [label]="'users.addUser' | transloco"
      icon="pi pi-user-plus"
      [disabled]="store.isMutating()"
      (onClick)="onAddUser()" />
  </header>

  @if (store.isLoading()) {
    <div class="users__state"><p-progressSpinner /></div>
  } @else if (store.loadError(); as errorKey) {
    <p class="users__error">{{ errorKey | transloco }}</p>
  } @else {
    <section class="users__table-card">
      <p-table [value]="store.users()" dataKey="id">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ 'users.table.name' | transloco }}</th>
            <th>{{ 'users.table.signInEmail' | transloco }}</th>
            <th>{{ 'users.table.role' | transloco }}</th>
            <th>{{ 'users.table.teacher' | transloco }}</th>
            <th>{{ 'users.table.status' | transloco }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-user>
          <tr [class.users__row--deleted]="user.isDeleted">
            <td>{{ user.name }}</td>
            <td><bdi dir="ltr">{{ user.signInEmail }}</bdi></td>
            <td>
              <p-tag [value]="('users.roles.' + user.role) | transloco" [severity]="roleTagSeverity[user.role]" />
            </td>
            <td>
              @if (user.teacherName; as teacherName) {
                {{ teacherName }}
              } @else {
                <span class="users__muted">{{ 'users.noTeacher' | transloco }}</span>
              }
            </td>
            <td>
              @if (user.isDeleted) {
                <p-tag severity="secondary" [value]="'users.status.deleted' | transloco" />
              } @else {
                <p-tag severity="success" [value]="'users.status.active' | transloco" />
              }
            </td>
          </tr>
        </ng-template>
      </p-table>
    </section>

    @if (store.hasOnlyOneUser()) {
      <p class="users__hint">{{ 'users.onlyOneUserHint' | transloco }}</p>
    }
  }
</div>
```

Create `client\src\app\features\users\ui\pages\users\users.page.scss` (logical properties only; the card scrolls sideways on narrow screens instead of the page):

```scss
.users__header {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1.5rem;
}

.users__title {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 500;
    font-size: 1.625rem;
    color: var(--app-ink);
    letter-spacing: -0.01em;
}

.users__subtitle {
    margin: 0.25rem 0 0;
    font-size: 0.84rem;
    color: var(--app-text-secondary);
}

.users__table-card {
    margin-block-start: 1.125rem;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);
    padding: 1rem 1.25rem;
    overflow-x: auto;
}

.users__row--deleted td {
    color: var(--app-text-muted);
}

.users__muted {
    color: var(--app-text-muted);
}

.users__state {
    display: flex;
    justify-content: center;
    padding-block: 3rem;
}

.users__error {
    color: var(--p-red-500);
    margin-block-start: 1.5rem;
}

.users__hint {
    margin-block-start: 0.875rem;
    font-size: 0.85rem;
    color: var(--app-text-secondary);
}
```

- [ ] **Step 4: Add the translations**

Both files ship together (client-i18n rule 4). Keep each file valid JSON, indented like its neighbours, with no en dash, em dash or ellipsis character.

`client\public\i18n\en.json`:
- In `shell.nav`, add after `"history"`: `"users": "Users"`
- Add a top-level `"users"` object after `"publications"`:

```json
  "users": {
    "title": "Users",
    "subtitle": "Users are the people who can sign in.",
    "addUser": "Add User",
    "table": {
      "name": "Name",
      "signInEmail": "Sign-in email",
      "role": "Role",
      "teacher": "Linked teacher",
      "status": "Status"
    },
    "roles": {
      "administrator": "Administrator",
      "teacher": "Teacher"
    },
    "status": {
      "active": "Active",
      "deleted": "Deleted"
    },
    "noTeacher": "None",
    "name": "Name",
    "signInEmail": "Sign-in email",
    "role": "Role",
    "teacher": "Linked teacher",
    "chooseTeacher": "Choose a teacher",
    "alreadyHasUser": "Already has a User",
    "teacherHintRequired": "Required for the Teacher Role.",
    "teacherHintOptional": "Optional. Link a teacher if this Administrator also teaches.",
    "teacherLinkFixed": "The linked teacher can't be changed after the User is created.",
    "teacherRequired": "Choose the teacher this User signs in for.",
    "temporaryPassword": "Temporary password",
    "temporaryPasswordHint": "No email is sent. Give the password to the person yourself.",
    "created": "User added",
    "loadFailed": "Loading users failed",
    "onlyOneUserHint": "Only you can sign in so far. Add a User for each teacher who should sign in."
  },
```

- In `"errors"`, add in alphabetical order among the existing keys:

```json
    "teacherAlreadyLinkedToUser": "This teacher already has a User. Refresh the page.",
    "temporaryPasswordMustNotBeEmpty": "Enter a temporary password.",
    "userNameMustNotBeEmpty": "Enter the User's name.",
    "userNotFound": "This User no longer exists. Refresh the page.",
    "userSignInEmailAlreadyInUse": "Another User already signs in with this email.",
    "userWithTeacherRoleMustHaveLinkedTeacher": "A Teacher-role User must be linked to a teacher.",
```

`client\public\i18n\he.json`:
- In `shell.nav`, add after `"history"`: `"users": "משתמשים"`
- Add a top-level `"users"` object after `"publications"`:

```json
  "users": {
    "title": "משתמשים",
    "subtitle": "משתמשים הם האנשים שיכולים להתחבר למערכת.",
    "addUser": "הוספת משתמש",
    "table": {
      "name": "שם",
      "signInEmail": "אימייל להתחברות",
      "role": "תפקיד",
      "teacher": "מורה מקושר",
      "status": "סטטוס"
    },
    "roles": {
      "administrator": "מנהל",
      "teacher": "מורה"
    },
    "status": {
      "active": "פעיל",
      "deleted": "נמחק"
    },
    "noTeacher": "ללא",
    "name": "שם",
    "signInEmail": "אימייל להתחברות",
    "role": "תפקיד",
    "teacher": "מורה מקושר",
    "chooseTeacher": "בחירת מורה",
    "alreadyHasUser": "כבר יש לו משתמש",
    "teacherHintRequired": "חובה בתפקיד מורה.",
    "teacherHintOptional": "לא חובה. קשרו מורה אם המנהל הזה גם מלמד.",
    "teacherLinkFixed": "אי אפשר לשנות את המורה המקושר אחרי יצירת המשתמש.",
    "teacherRequired": "בחרו את המורה שהמשתמש הזה מתחבר בשמו.",
    "temporaryPassword": "סיסמה זמנית",
    "temporaryPasswordHint": "לא נשלח אימייל. מסרו את הסיסמה לאדם בעצמכם.",
    "created": "המשתמש נוסף",
    "loadFailed": "טעינת המשתמשים נכשלה",
    "onlyOneUserHint": "בינתיים רק אתם יכולים להתחבר. הוסיפו משתמש לכל מורה שצריך להתחבר."
  },
```

- In `"errors"`, add in alphabetical order:

```json
    "teacherAlreadyLinkedToUser": "למורה הזה כבר יש משתמש. רעננו את הדף.",
    "temporaryPasswordMustNotBeEmpty": "הזינו סיסמה זמנית.",
    "userNameMustNotBeEmpty": "הזינו את שם המשתמש.",
    "userNotFound": "המשתמש הזה כבר לא קיים. רעננו את הדף.",
    "userSignInEmailAlreadyInUse": "משתמש אחר כבר מתחבר עם האימייל הזה.",
    "userWithTeacherRoleMustHaveLinkedTeacher": "משתמש בתפקיד מורה חייב להיות מקושר למורה.",
```

Check that both files parse and hold the same `users` and `errors` keys:

```bash
node -e "const en=require('./client/public/i18n/en.json'),he=require('./client/public/i18n/he.json');const keys=o=>Object.entries(o).flatMap(([k,v])=>typeof v==='object'?keys(v).map(x=>k+'.'+x):[k]);const a=keys(en).filter(k=>/^(users|errors|shell)\./.test(k)).sort(),b=keys(he).filter(k=>/^(users|errors|shell)\./.test(k)).sort();console.log(JSON.stringify(a)===JSON.stringify(b)?'same keys':'DIFFERENT: '+a.filter(k=>!b.includes(k)).concat(b.filter(k=>!a.includes(k))))"
```

Run it from the repository root. Expected: `same keys`. (`errors` already holds `teacherMustNotHaveActiveUser` in both files from task 4.)

- [ ] **Step 5: Build and run the suite**

Run both commands at the top of this task from `client\`. Expected: the build succeeds with no template or type error, and every spec PASSES (`source-text.spec.ts` and `translations.spec.ts` included). The browser check is task 7.

- [ ] **Step 6: Commit**

```bash
git add client/src/app/features/users client/src/app/shared/config/app-routes.ts client/src/app/features/admin-shell/admin.routes.ts client/src/app/features/admin-shell/admin-shell.component.html client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): Users screen with an Add User dialog and a Users nav entry (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
