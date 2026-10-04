# Task 5 of 6: Row menu and the Edit details, Change Role and Set Temporary Password dialogs

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Requires task 4 committed. Work on branch `87-edit-users`.

**Files** (all paths under `client\src\app\features\users\`):
- Create: `domain\role-change.ts`, `domain\role-change.spec.ts`
- Create: `ui\components\user-who-card\user-who-card.component.{ts,html,scss}`
- Create: `ui\dialogs\edit-user\edit-user.dialog.{ts,html,scss}`
- Create: `ui\dialogs\change-user-role\change-user-role.dialog.{ts,html}`
- Create: `ui\dialogs\set-temporary-password\set-temporary-password.dialog.{ts,html}`
- Modify: `ui\dialogs\dialog-form.scss` (gets the shared `.role-switch`, refusal and note styles)
- Modify: `ui\dialogs\add-user\add-user.dialog.scss` (loses `.role-switch`)
- Modify: `ui\dialogs\delete-user\delete-user.dialog.{ts,html,scss}` (uses the who-card)
- Modify: `ui\pages\users\users.page.ts` (row menu, three openers)
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (`users.*`)

**Interfaces:**
- Consumes:
  - From task 4: `UsersStore.refusal`, `clearRefusal()`, `isMutating`, `changeDetails(userId, ChangeUserDetailsRequest)`, `changeRole(userId, ChangeUserRoleRequest)`, `setTemporaryPassword(userId, SetUserTemporaryPasswordRequest)` and `delete(userId)`, each returning `Promise<boolean>`.
  - From task 2: the `errors.*` keys.
  - Existing: `User`, `Role`, `InitialsPipe`, `isolateDirection(text)`, `DialogService`, `DynamicDialogConfig` / `DynamicDialogRef`.
- Produces:
  - `otherRole(role: Role): Role` and `roleChangeNoteKey(newRole: Role, teacherName: string | null): string | null`
  - `<app-user-who-card [user]="user" />`
  - The three dialogs. Each one's data is `{ user: User; refusal: Signal<string | null>; isSaving: Signal<boolean>; confirm: (request) => Promise<boolean> }`, and it closes only when `confirm` resolves to `true`.
  - Translation keys `users.editDetails`, `users.changeRole`, `users.setTemporaryPassword`, `users.linkLocked`, `users.newRole`, `users.currentRole`, `users.promoteNote`, `users.demoteNote`, `users.roleEffect`, `users.temporaryPasswordNote`, `users.temporaryPasswordSelfNote`, `users.setPassword`, `users.detailsChanged`, `users.roleChanged`, `users.temporaryPasswordSet`.

**Why:**
- #87 AC 7: the Users screen gets Edit, Change Role and Set Temporary Password actions, and 409 violations appear translated.
- #87 AC 8: every string is a translation key, Hebrew first and RTL-correct.
- Design frames 3b, 5a/5b, 6a-6f and 7a.
- README decisions 7-12; Review Focus 5.

**Design reference** (`Users and Roles.html`), so this task can be built without the `claude_design` MCP:
- **3b, row menu:** a 230px popup with Edit details (pencil), Change Role (shield), Set Temporary Password (key), a divider, then Delete in the danger color.
- **5a, Edit details:** Name, then Email (`dir="ltr"`). When linked, a read-only "Linked Teacher" row follows: a dashed border, a lock icon, the Teacher's name and "· can't be changed". The footer has Cancel and Save.
- **5b:** an error message titled "That didn't go through" is the first item in the body.
- **6a-6f, Change Role:**
  - An optional error message comes first, then the who-card (avatar, name, LTR email, Role tag).
  - The "New Role" pill switch has the current Role disabled and labelled "(current)".
  - Then comes an info message: the promote note when the new Role is Administrator, or the demote note when it's Teacher and the User is linked.
  - Last comes a muted line with an info icon: "The change takes effect on their next action, and they'll need to sign in again."
  - The footer has Cancel and Change Role. The notes are hidden while a refusal shows.
- **7a, Set Temporary Password:** the who-card, then the "Temporary Password" password field (shown in the frame) and the info message "Their current sessions end immediately. Tell them the new password yourself." The footer has Cancel and Set password.
- Copy comes from the design's copy deck: `act.*`, `edit.*`, `role.*` and `temp.*`.

**Run the tests** (from `client\`, in PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

- [ ] **Step 1: Write the failing spec for the role-change helpers**

`domain\role-change.spec.ts`:

```ts
import { Role } from './role.enum';
import { otherRole, roleChangeNoteKey } from './role-change';

describe('role change', () => {
    it('offers the other Role', () => {
        //expected
        expect(otherRole(Role.administrator)).toBe(Role.teacher);
        expect(otherRole(Role.teacher)).toBe(Role.administrator);
    });

    it('explains that the Teacher link is kept when the new Role is Administrator', () => {
        //expected
        expect(roleChangeNoteKey(Role.administrator, 'Dana Levi')).toBe('users.promoteNote');
    });

    it('explains what a linked User sees with the Teacher Role', () => {
        //expected
        expect(roleChangeNoteKey(Role.teacher, 'Ronit Avraham')).toBe('users.demoteNote');
    });

    it('has no note for a User without a linked Teacher', () => {
        //expected
        expect(roleChangeNoteKey(Role.teacher, null)).toBeNull();
        expect(roleChangeNoteKey(Role.administrator, null)).toBeNull();
    });
});
```

Run the tests. Expected: they fail because `./role-change` doesn't exist.

- [ ] **Step 2: Implement the helpers**

`domain\role-change.ts`:

```ts
import { Role } from './role.enum';

export function otherRole(role: Role): Role {
    return role === Role.administrator ? Role.teacher : Role.administrator;
}

export function roleChangeNoteKey(newRole: Role, teacherName: string | null): string | null {
    if (!teacherName) {
        return null;
    }

    return newRole === Role.administrator ? 'users.promoteNote' : 'users.demoteNote';
}
```

These helpers only pick copy. Every rule (same Role, missing link, self, last Administrator) stays on the server (CLAUDE.md rule 12). Run the tests: the four cases pass.

- [ ] **Step 3: Extract the who-card**

`ui\components\user-who-card\user-who-card.component.ts`:

```ts
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';

@Component({
    selector: 'app-user-who-card',
    imports: [TranslocoPipe, InitialsPipe, TagModule],
    templateUrl: './user-who-card.component.html',
    styleUrl: './user-who-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserWhoCardComponent {
    readonly user = input.required<User>();

    protected readonly roles = Role;
}
```

`ui\components\user-who-card\user-who-card.component.html`:

```html
<div class="who-card">
  <span class="who-card__avatar" aria-hidden="true">{{ user().name | initials }}</span>
  <span class="who-card__identity">
    <span class="who-card__name">{{ user().name }}</span>
    <bdi dir="ltr" class="who-card__email">{{ user().signInEmail }}</bdi>
  </span>
  <p-tag
    class="who-card__role"
    [class.who-card__role--administrator]="user().role === roles.administrator"
    [class.who-card__role--teacher]="user().role === roles.teacher"
    [value]="('users.roles.' + user().role) | transloco" />
</div>
```

`ui\components\user-who-card\user-who-card.component.scss`: move the rules `.delete-user__who`, `__avatar`, `__identity`, `__name`, `__email`, `__role`, `__role--administrator` and `__role--teacher` from `delete-user.dialog.scss` into this file unchanged, renaming the prefix `delete-user__` to `who-card__` (`.delete-user__who` becomes `.who-card`). Delete those rules from `delete-user.dialog.scss`.

Check the relative import depth of `InitialsPipe` against `delete-user.dialog.ts`, which imports it from `'../../../../../shared/pipes/initials.pipe'`. The who-card sits at the same depth (`ui\components\x\` mirrors `ui\dialogs\x\`), so the path is identical.

- [ ] **Step 4: Use the who-card in the Delete dialog**

In `delete-user.dialog.html`, replace the whole `<div class="delete-user__who">...</div>` block with:

```html
  <app-user-who-card [user]="user" />
```

In `delete-user.dialog.ts`:
- Add `import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';`.
- Replace `InitialsPipe` and `TagModule` in `imports` with `UserWhoCardComponent`, and delete their import lines.
- Delete `protected readonly roles = Role;` and the now-unused `Role` import.

- [ ] **Step 5: Share the dialog styles**

Move the whole `:host ::ng-deep .role-switch.p-selectbutton { ... }` block from `add-user.dialog.scss` to the end of `dialog-form.scss`, unchanged. Then append to `dialog-form.scss` (two-space indent, like the rest of that file):

```scss
.dialog-form__refusal-text {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.dialog-form__note {
  display: flex;
  align-items: flex-start;
  margin: 0;
  gap: 0.5rem;
  font-size: 0.8125rem;
  line-height: 1.5;
  color: var(--app-text-secondary);

  .pi {
    margin-block-start: 0.2rem;
  }
}
```

`add-user.dialog.ts` already lists `'../dialog-form.scss'` first in `styleUrls`, so the Add User role switch looks exactly as before.

- [ ] **Step 6: The Edit details dialog**

`ui\dialogs\edit-user\edit-user.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { ChangeUserDetailsRequest } from '../../../data/change-user-details.request';
import { User } from '../../../domain/user.model';

export interface EditUserDialogData {
    user: User;
    refusal: Signal<string | null>;
    isSaving: Signal<boolean>;
    confirm: (request: ChangeUserDetailsRequest) => Promise<boolean>;
}

@Component({
    selector: 'app-edit-user-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule, MessageModule],
    templateUrl: './edit-user.dialog.html',
    styleUrls: ['../dialog-form.scss', './edit-user.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditUserDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<EditUserDialogData>).data as EditUserDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;

    protected readonly form = this.fb.nonNullable.group({
        name: [this.user.name, Validators.required],
        signInEmail: [this.user.signInEmail, [Validators.required, Validators.email]],
    });

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const request: ChangeUserDetailsRequest = this.form.getRawValue();
        const saved = await this.data.confirm(request);

        if (saved) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

`ui\dialogs\edit-user\edit-user.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  @if (refusal(); as message) {
    <p-message severity="error">
      <span class="dialog-form__refusal-text">
        <strong>{{ 'users.refusedTitle' | transloco }}</strong>
        <span>{{ message }}</span>
      </span>
    </p-message>
  }

  <div class="field">
    <label for="edit-user-name">{{ 'users.name' | transloco }}</label>
    <input pInputText id="edit-user-name" formControlName="name" />
  </div>

  <div class="field">
    <label for="edit-user-sign-in-email">{{ 'users.signInEmail' | transloco }}</label>
    <input
      pInputText
      id="edit-user-sign-in-email"
      formControlName="signInEmail"
      type="email"
      dir="ltr"
      autocomplete="off" />
    @if (form.controls.signInEmail.dirty && form.controls.signInEmail.invalid) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ 'formErrors.emailInvalid' | transloco }}</small>
    }
  </div>

  @if (user.teacherName; as teacherName) {
    <div class="field">
      <span class="field__label">{{ 'users.teacher' | transloco }}</span>
      <div class="locked-field">
        <i class="pi pi-lock locked-field__icon" aria-hidden="true"></i>
        <span class="locked-field__value">{{ teacherName }}</span>
        <span class="locked-field__note">· {{ 'users.linkLocked' | transloco }}</span>
      </div>
    </div>
  }

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="form.invalid || isSaving()" />
  </div>
</form>
```

`ui\dialogs\edit-user\edit-user.dialog.scss`:

```scss
.field__label {
  font-weight: 600;
  font-size: 0.8125rem;
  color: var(--p-surface-500);
}

.locked-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  block-size: 42px;
  padding-inline: 0.75rem;
  border-radius: 6px;
  background: var(--app-bg-muted);
  border: 1px dashed var(--app-border);
}

.locked-field__icon {
  font-size: 0.9rem;
  color: var(--app-text-secondary);
}

.locked-field__value {
  font-size: 0.9rem;
  color: var(--app-ink);
}

.locked-field__note {
  font-size: 0.78rem;
  color: var(--app-text-muted);
}
```

The read-only Teacher is a `<span>` label, not a `<label>`, because there's no control to label.

- [ ] **Step 7: The Change Role dialog**

`ui\dialogs\change-user-role\change-user-role.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { SelectButtonModule } from 'primeng/selectbutton';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { ChangeUserRoleRequest } from '../../../data/change-user-role.request';
import { otherRole, roleChangeNoteKey } from '../../../domain/role-change';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';
import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';

export interface ChangeUserRoleDialogData {
    user: User;
    refusal: Signal<string | null>;
    isSaving: Signal<boolean>;
    confirm: (request: ChangeUserRoleRequest) => Promise<boolean>;
}

@Component({
    selector: 'app-change-user-role-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, MessageModule, SelectButtonModule, UserWhoCardComponent],
    templateUrl: './change-user-role.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeUserRoleDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly transloco = inject(TranslocoService);
    private readonly data = inject(DynamicDialogConfig<ChangeUserRoleDialogData>).data as ChangeUserRoleDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly teacher = this.user.teacherName ? isolateDirection(this.user.teacherName) : '';

    protected readonly roleOptions = Object.values(Role).map((value) => ({
        value,
        label: this.roleLabel(value),
        current: value === this.user.role,
    }));

    protected readonly form = this.fb.nonNullable.group({
        role: this.fb.nonNullable.control(otherRole(this.user.role)),
    });

    private readonly role = toSignal(this.form.controls.role.valueChanges, {
        initialValue: this.form.controls.role.value,
    });

    protected readonly noteKey = computed(() => roleChangeNoteKey(this.role(), this.user.teacherName));

    protected async submit(): Promise<void> {
        const request: ChangeUserRoleRequest = this.form.getRawValue();
        const changed = await this.data.confirm(request);

        if (changed) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }

    private roleLabel(role: Role): string {
        const label = this.transloco.translate(`users.roles.${role}`);

        if (role !== this.user.role) {
            return label;
        }

        return `${label} ${this.transloco.translate('users.currentRole')}`;
    }
}
```

`ui\dialogs\change-user-role\change-user-role.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  @if (refusal(); as message) {
    <p-message severity="error">
      <span class="dialog-form__refusal-text">
        <strong>{{ 'users.refusedTitle' | transloco }}</strong>
        <span>{{ message }}</span>
      </span>
    </p-message>
  }

  <app-user-who-card [user]="user" />

  <div class="field">
    <label id="change-role-label">{{ 'users.newRole' | transloco }}</label>
    <p-selectbutton
      class="role-switch"
      formControlName="role"
      [options]="roleOptions"
      optionLabel="label"
      optionValue="value"
      optionDisabled="current"
      [allowEmpty]="false"
      ariaLabelledBy="change-role-label" />
  </div>

  @if (!refusal()) {
    @if (noteKey(); as key) {
      <p-message severity="info">{{ key | transloco: { teacher } }}</p-message>
    }
  }

  <p class="dialog-form__note">
    <i class="pi pi-info-circle" aria-hidden="true"></i>
    <span>{{ 'users.roleEffect' | transloco }}</span>
  </p>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'users.changeRole' | transloco" type="submit" [disabled]="isSaving()" />
  </div>
</form>
```

With two Roles and the current one disabled, the selected Role is always the other one, so the submit button needs no "same Role" check. If the screen is stale and the User already has the target Role, the server answers `userAlreadyHasRole` (design 6f).
- [ ] **Step 8: The Set Temporary Password dialog**

`ui\dialogs\set-temporary-password\set-temporary-password.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { SetUserTemporaryPasswordRequest } from '../../../data/set-user-temporary-password.request';
import { User } from '../../../domain/user.model';
import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';

export interface SetTemporaryPasswordDialogData {
    user: User;
    refusal: Signal<string | null>;
    isSaving: Signal<boolean>;
    confirm: (request: SetUserTemporaryPasswordRequest) => Promise<boolean>;
}

@Component({
    selector: 'app-set-temporary-password-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, MessageModule, PasswordModule, UserWhoCardComponent],
    templateUrl: './set-temporary-password.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetTemporaryPasswordDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<SetTemporaryPasswordDialogData>).data as SetTemporaryPasswordDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;

    protected readonly form = this.fb.nonNullable.group({
        temporaryPassword: ['', Validators.required],
    });

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const request: SetUserTemporaryPasswordRequest = this.form.getRawValue();
        const set = await this.data.confirm(request);

        if (set) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

`ui\dialogs\set-temporary-password\set-temporary-password.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  @if (refusal(); as message) {
    <p-message severity="error">
      <span class="dialog-form__refusal-text">
        <strong>{{ 'users.refusedTitle' | transloco }}</strong>
        <span>{{ message }}</span>
      </span>
    </p-message>
  }

  <app-user-who-card [user]="user" />

  <div class="field">
    <label for="set-temporary-password">{{ 'users.temporaryPassword' | transloco }}</label>
    <p-password
      inputId="set-temporary-password"
      formControlName="temporaryPassword"
      [feedback]="false"
      [toggleMask]="true"
      autocomplete="new-password"
      dir="ltr"
      fluid />
  </div>

  <p-message severity="info">{{ 'users.temporaryPasswordNote' | transloco }}</p-message>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'users.setPassword' | transloco" type="submit" [disabled]="form.invalid || isSaving()" />
  </div>
</form>
```

- [ ] **Step 9: Wire the row menu and the openers**

Replace `ui\pages\users\users.page.ts` with:

```ts
import { ChangeDetectionStrategy, Component, Type, inject, signal, viewChild } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { Menu, MenuModule } from 'primeng/menu';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';
import { UsersStore } from '../../../state/users.store';
import { AddUserDialog, AddUserDialogData, AddUserResult } from '../../dialogs/add-user/add-user.dialog';
import {
    ChangeUserRoleDialog,
    ChangeUserRoleDialogData,
} from '../../dialogs/change-user-role/change-user-role.dialog';
import { DeleteUserDialog, DeleteUserDialogData } from '../../dialogs/delete-user/delete-user.dialog';
import { EditUserDialog, EditUserDialogData } from '../../dialogs/edit-user/edit-user.dialog';
import {
    SetTemporaryPasswordDialog,
    SetTemporaryPasswordDialogData,
} from '../../dialogs/set-temporary-password/set-temporary-password.dialog';

@Component({
    selector: 'app-users-page',
    imports: [TranslocoPipe, InitialsPipe, ButtonModule, MenuModule, ProgressSpinnerModule, TableModule, TagModule],
    templateUrl: './users.page.html',
    styleUrl: './users.page.scss',
    providers: [UsersStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
    private static readonly dialogWidth = '33.75rem';
    private static readonly userDialogWidth = '32.5rem';
    private static readonly deleteDialogWidth = '31.25rem';

    protected readonly store = inject(UsersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly roles = Role;
    protected readonly rowActions = signal<MenuItem[]>([]);

    private readonly rowMenu = viewChild.required<Menu>('rowMenu');

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

    protected onRowActions(event: Event, user: User): void {
        this.rowActions.set([
            {
                label: this.transloco.translate('users.editDetails'),
                icon: 'pi pi-pencil',
                command: () => this.onEditUser(user),
            },
            {
                label: this.transloco.translate('users.changeRole'),
                icon: 'pi pi-shield',
                command: () => this.onChangeRole(user),
            },
            {
                label: this.transloco.translate('users.setTemporaryPassword'),
                icon: 'pi pi-key',
                command: () => this.onSetTemporaryPassword(user),
            },
            { separator: true },
            {
                label: this.transloco.translate('users.delete'),
                icon: 'pi pi-trash',
                styleClass: 'users-menu__item--danger',
                command: () => this.onDeleteUser(user),
            },
        ]);
        this.rowMenu().toggle(event);
    }

    protected onRestoreUser(user: User): void {
        void this.store.restore(user);
    }

    private onEditUser(user: User): void {
        const data: EditUserDialogData = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.changeDetails(user.id, request),
        };

        this.openUserDialog(
            EditUserDialog,
            this.transloco.translate('users.editDetails'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onChangeRole(user: User): void {
        const data: ChangeUserRoleDialogData = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.changeRole(user.id, request),
        };

        this.openUserDialog(
            ChangeUserRoleDialog,
            this.transloco.translate('users.changeRole'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onSetTemporaryPassword(user: User): void {
        const data: SetTemporaryPasswordDialogData = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.setTemporaryPassword(user.id, request),
        };

        this.openUserDialog(
            SetTemporaryPasswordDialog,
            this.transloco.translate('users.setTemporaryPassword'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onDeleteUser(user: User): void {
        const data: DeleteUserDialogData = {
            user,
            refusal: this.store.refusal,
            isDeleting: this.store.isMutating,
            confirm: () => this.store.delete(user.id),
        };

        this.openUserDialog(
            DeleteUserDialog,
            this.transloco.translate('users.deleteTitle', { name: isolateDirection(user.name) }),
            UsersPage.deleteDialogWidth,
            data,
        );
    }

    private openUserDialog(component: Type<unknown>, header: string, width: string, data: unknown): void {
        this.store.clearRefusal();

        this.dialogs.open(component, {
            header,
            width,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
```

Each caller passes `openUserDialog` a header it has already translated, because the Delete title carries the User's name as a parameter.

Every opener calls `clearRefusal()` first, so a dialog never opens showing an earlier refusal (Review Focus 5). The self row keeps every action (README decision 9, CLAUDE.md rule 12). `users.page.html` needs no change: the menu is the same shared popup `p-menu`, and the danger style for Delete already exists.

- [ ] **Step 10: Add the translations (Hebrew first)**

In `client\public\i18n\he.json`, inside `"users"`, add after `"refusedTitle"` (put a comma after `"refusedTitle"`'s value):

```json
    "editDetails": "עריכת פרטים",
    "changeRole": "שינוי תפקיד",
    "setTemporaryPassword": "הגדרת סיסמה זמנית",
    "linkLocked": "אי אפשר לשנות",
    "newRole": "תפקיד חדש",
    "currentRole": "(נוכחי)",
    "promoteNote": "הקישור למורה {{teacher}} נשמר. מעכשיו תהיה גישה לכל המסכים.",
    "demoteNote": "מעכשיו תהיה גישה רק למערכת השבועית ולפרסומים של {{teacher}}.",
    "roleEffect": "השינוי ייכנס לתוקף בפעולה הבאה שלהם, והם יצטרכו להיכנס שוב.",
    "temporaryPasswordNote": "כל החיבורים הפעילים שלהם יסתיימו מיד. מסרו להם את הסיסמה החדשה בעצמכם.",
    "temporaryPasswordSelfNote": "זו הסיסמה שלך. מיד אחרי השמירה תצאו מהמערכת - היכנסו שוב עם הסיסמה החדשה.",
    "setPassword": "הגדרת סיסמה",
    "detailsChanged": "הפרטים נשמרו",
    "roleChanged": "התפקיד שונה",
    "temporaryPasswordSet": "הסיסמה הזמנית הוגדרה"
```

In `client\public\i18n\en.json`, at the same place:

```json
    "editDetails": "Edit details",
    "changeRole": "Change Role",
    "setTemporaryPassword": "Set Temporary Password",
    "linkLocked": "can't be changed",
    "newRole": "New Role",
    "currentRole": "(current)",
    "promoteNote": "The link to {{teacher}} is kept. They get access to every screen.",
    "demoteNote": "From now on they only see {{teacher}}'s Week Schedules and Publications.",
    "roleEffect": "The change takes effect on their next action, and they'll need to sign in again.",
    "temporaryPasswordNote": "Their current sessions end immediately. Tell them the new password yourself.",
    "temporaryPasswordSelfNote": "This is your own password. You're signed out right after saving - sign in again with the new password.",
    "setPassword": "Set password",
    "detailsChanged": "Details saved",
    "roleChanged": "Role changed",
    "temporaryPasswordSet": "Temporary Password set"
```

The copy comes from the design's copy deck (`act.edit`, `act.role`, `act.tempPw`, `edit.locked`, `role.new`, `role.currentOpt`, `role.promoteNote`, `role.demoteNote`, `role.effect`, `temp.note`, `temp.save`). The three success toasts are new because the design shows none. The English buttons use the app's existing title case, not the mock's capitals (README decision 12).

- [ ] **Step 11: Run every client check**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected:
- Everything is green: `role-change.spec.ts`, `translations.spec.ts` (same keys in he and en, no long dashes or ellipsis characters) and `source-text.spec.ts` included.
- The build succeeds with no new warnings about unused imports in `delete-user.dialog.ts`.
- `grep -rn "delete-user__who\|delete-user__avatar\|delete-user__role" client/src` finds nothing.

- [ ] **Step 12: Commit**

```bash
git add client/src/app/features/users client/public/i18n
git commit -m "feat(client): edit details, change Role and set a Temporary Password from the Users row menu (#87)"
```

End the commit message with the attribution trailer from the session's instructions.
