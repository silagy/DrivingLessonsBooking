# Task 5 of 6: The Change my password dialog and the shell's user menu

> Part of [#88: Change My Own Password](README.md). Requires task 4 committed. Work on branch `88-change-my-password`.

**Files:**
- Create: `client\src\app\features\users\ui\dialogs\change-my-password\change-my-password.dialog.ts`
- Create: `client\src\app\features\users\ui\dialogs\change-my-password\change-my-password.dialog.html`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.ts`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes:
  - From task 4: `MyPasswordStore` (`refusal`, `isSaving`, `change(request): Promise<boolean>`), `ChangeMyPasswordRequest`, `passwordsMatch(newPassword, confirmation)`.
  - Existing: `dialog-form.scss` (`.dialog-form`, `.field`, `.field__error`, `.dialog-form__actions`), `DialogRefusalComponent` (`[message]`), `AuthService` (`email`, `logout()`), `DynamicDialog`, `p-password`, `p-menu`.
- Produces:
  - `ChangeMyPasswordDialog`, a standalone dialog that provides its own `MyPasswordStore` and takes no `data`.
  - The shell's user menu: an avatar button (`aria-label` "User menu") opens a popup with a header (avatar, email) and two items, "Change my password" and "Sign out".
  - Translation keys `shell.userMenu`, `shell.changeMyPassword`, `myPassword.*`.

**Why:**
- #88 AC 4: "A 'Change my password' dialog is reachable from the admin shell for every User".
- #88 AC 5 (UI half): success keeps the User signed in and says so explicitly with the toast "Password changed" / "You're still signed in." (design 9d).
- #88 AC 6: all strings are translation keys, Hebrew first, RTL-correct.
- README decisions 7 to 11; Review Focus 5.

## Design reference

Translate the mock into PrimeNG; don't port its JSX (see [users-and-roles-design.md](../users-and-roles-design.md)).

**User menu (2c / 2d / 2e).** In the top bar, at the inline end, after the language toggle: a pill button holding the 30px avatar initials, the user label and a chevron that turns 180° while the menu is open. The popup is about 300px wide, opens under the button and aligns to the inline end. It holds:
1. A header: a 38px avatar, then the sign-in email (LTR-isolated). Name, Role tag and linked Teacher are out of scope (README decision 10).
2. A divider.
3. "Change my password" with a key icon.
4. "Sign out" with a sign-out icon, mirrored in RTL.

**Dialog (9a / 9b / 9c),** about 520px wide, titled "Change my password":
1. The server refusal, if any, comes first: an error message titled "That didn't go through" (9c: "Current password is incorrect.").
2. "Current password": a password field with an eye toggle, `dir="ltr"`.
3. "New password": the same.
4. "Confirm new password": the same. While it doesn't match, an error line under it reads "The passwords don't match." (alert icon) and the field is outlined as invalid (9b).
5. Footer: a text "Cancel", then the primary "Change password", disabled while the form is invalid (9b) or saving.

**Copy deck** (Hebrew first):

| Key | Hebrew | English | Design key |
|-----|--------|---------|------------|
| `shell.userMenu` | תפריט משתמש | User menu | `menu.open` |
| `shell.changeMyPassword` | שינוי הסיסמה שלי | Change my password | `menu.changePw` |
| `myPassword.title` | שינוי הסיסמה שלי | Change my password | `my.title` |
| `myPassword.current` | סיסמה נוכחית | Current password | `my.current` |
| `myPassword.new` | סיסמה חדשה | New password | `my.new` |
| `myPassword.confirm` | אימות הסיסמה החדשה | Confirm new password | `my.confirm` |
| `myPassword.save` | שינוי סיסמה | Change password | `my.save` |
| `myPassword.mismatch` | הסיסמאות לא תואמות. | The passwords don't match. | `err.mismatch` |
| `myPassword.changed` | הסיסמה שונתה | Password changed | `toast.pwChanged` |
| `myPassword.changedDetail` | נשארת מחובר/ת. | You're still signed in. | `toast.pwChangedDetail` |

The refusal title reuses `users.refusedTitle` (inside `DialogRefusalComponent`), Cancel reuses `general.cancel`, and Sign out reuses `shell.logout`.

- [ ] **Step 1: Add the translations**

In `client\public\i18n\he.json`, inside `"shell"`, add after `"logout"`:

```json
    "userMenu": "תפריט משתמש",
    "changeMyPassword": "שינוי הסיסמה שלי",
```

Then add a new top-level block directly after the closing brace of `"users"` (before `"studentForm"`):

```json
  "myPassword": {
    "title": "שינוי הסיסמה שלי",
    "current": "סיסמה נוכחית",
    "new": "סיסמה חדשה",
    "confirm": "אימות הסיסמה החדשה",
    "save": "שינוי סיסמה",
    "mismatch": "הסיסמאות לא תואמות.",
    "changed": "הסיסמה שונתה",
    "changedDetail": "נשארת מחובר/ת."
  },
```

In `client\public\i18n\en.json`, at the same places:

```json
    "userMenu": "User menu",
    "changeMyPassword": "Change my password",
```

```json
  "myPassword": {
    "title": "Change my password",
    "current": "Current password",
    "new": "New password",
    "confirm": "Confirm new password",
    "save": "Change password",
    "mismatch": "The passwords don't match.",
    "changed": "Password changed",
    "changedDetail": "You're still signed in."
  },
```

- [ ] **Step 2: Add the dialog**

Create `client\src\app\features\users\ui\dialogs\change-my-password\change-my-password.dialog.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { PasswordModule } from 'primeng/password';
import { ChangeMyPasswordRequest } from '../../../data/change-my-password.request';
import { passwordsMatch } from '../../../domain/passwords-match';
import { MyPasswordStore } from '../../../state/my-password.store';
import { DialogRefusalComponent } from '../../components/dialog-refusal/dialog-refusal.component';

const PASSWORDS_MISMATCH = 'passwordsMismatch';

function matchingPasswords(group: AbstractControl): ValidationErrors | null {
    const newPassword = group.get('newPassword')?.value as string;
    const confirmation = group.get('confirmation')?.value as string;

    return passwordsMatch(newPassword, confirmation) ? null : { [PASSWORDS_MISMATCH]: true };
}

@Component({
    selector: 'app-change-my-password-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, PasswordModule, DialogRefusalComponent],
    templateUrl: './change-my-password.dialog.html',
    styleUrl: '../dialog-form.scss',
    providers: [MyPasswordStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeMyPasswordDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly store = inject(MyPasswordStore);

    protected readonly refusal = this.store.refusal;
    protected readonly isSaving = this.store.isSaving;
    protected readonly passwordsMismatch = PASSWORDS_MISMATCH;

    protected readonly form = this.fb.nonNullable.group(
        {
            currentPassword: ['', Validators.required],
            newPassword: ['', Validators.required],
            confirmation: ['', Validators.required],
        },
        { validators: matchingPasswords },
    );

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const { currentPassword, newPassword } = this.form.getRawValue();
        const request: ChangeMyPasswordRequest = { currentPassword, newPassword };
        const changed = await this.store.change(request);

        if (changed) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

Create `client\src\app\features\users\ui\dialogs\change-my-password\change-my-password.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  @if (refusal(); as message) {
    <app-dialog-refusal [message]="message" />
  }

  <div class="field">
    <label for="my-current-password">{{ 'myPassword.current' | transloco }}</label>
    <p-password
      inputId="my-current-password"
      formControlName="currentPassword"
      [feedback]="false"
      [toggleMask]="true"
      autocomplete="current-password"
      dir="ltr"
      fluid />
  </div>

  <div class="field">
    <label for="my-new-password">{{ 'myPassword.new' | transloco }}</label>
    <p-password
      inputId="my-new-password"
      formControlName="newPassword"
      [feedback]="false"
      [toggleMask]="true"
      autocomplete="new-password"
      dir="ltr"
      fluid />
  </div>

  @let showsMismatch = form.hasError(passwordsMismatch) && form.controls.confirmation.dirty;

  <div class="field">
    <label for="my-password-confirmation">{{ 'myPassword.confirm' | transloco }}</label>
    <p-password
      inputId="my-password-confirmation"
      formControlName="confirmation"
      [feedback]="false"
      [toggleMask]="true"
      [invalid]="showsMismatch"
      autocomplete="new-password"
      dir="ltr"
      fluid />
    @if (showsMismatch) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ 'myPassword.mismatch' | transloco }}</small>
    }
  </div>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button [label]="'myPassword.save' | transloco" type="submit" [disabled]="form.invalid || isSaving()" />
  </div>
</form>
```

If the build reports that `invalid` isn't a known input of `p-password`, drop `[invalid]` and add `[class.ng-invalid]="showsMismatch" [class.ng-dirty]="showsMismatch"` on the `p-password` element instead. PrimeNG styles the invalid border from those two classes.

- [ ] **Step 3: Add the user menu to the shell**

Replace `client\src\app\features\admin-shell\admin-shell.component.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { Menu, MenuModule } from 'primeng/menu';
import { AuthService } from '../../core/auth.service';
import { AppRoutes } from '../../shared/config/app-routes';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../shared/language-toggle/language-toggle.component';
import { ChangeMyPasswordDialog } from '../users/ui/dialogs/change-my-password/change-my-password.dialog';

@Component({
  selector: 'app-admin-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoPipe,
    ButtonModule,
    MenuModule,
    BrandLogoComponent,
    LanguageToggleComponent,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  providers: [DialogService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private static readonly dialogWidth = '32.5rem';

  protected readonly auth = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly transloco = inject(TranslocoService);

  protected readonly appRoutes = AppRoutes;
  protected readonly userMenuItems = signal<MenuItem[]>([]);
  protected readonly isUserMenuOpen = signal(false);

  private readonly userMenu = viewChild.required<Menu>('userMenu');

  protected readonly initials = computed(() => {
    const email = this.auth.email();
    return email ? email.slice(0, 2).toUpperCase() : 'AD';
  });

  protected onUserMenu(event: Event): void {
    this.userMenuItems.set([
      {
        label: this.transloco.translate('shell.changeMyPassword'),
        icon: 'pi pi-key',
        command: () => this.onChangeMyPassword(),
      },
      {
        label: this.transloco.translate('shell.logout'),
        icon: 'pi pi-sign-out',
        styleClass: 'shell-user-menu__item--sign-out',
        command: () => this.auth.logout(),
      },
    ]);
    this.userMenu().toggle(event);
  }

  private onChangeMyPassword(): void {
    this.dialogs.open(ChangeMyPasswordDialog, {
      header: this.transloco.translate('myPassword.title'),
      width: AdminShellComponent.dialogWidth,
      modal: true,
      dismissableMask: true,
    });
  }
}
```

The menu items are built when the menu opens, like `UsersPage.onRowActions`, so they pick up the current language.

In `client\src\app\features\admin-shell\admin-shell.component.html`, replace everything inside `<div class="shell__actions">` (the `.shell__user` div and the "Sign out" `p-button`) with:

```html
      <app-language-toggle />

      <p-button
        [styleClass]="isUserMenuOpen() ? 'shell__user shell__user--open' : 'shell__user'"
        [ariaLabel]="'shell.userMenu' | transloco"
        severity="secondary"
        text
        (onClick)="onUserMenu($event)">
        <span class="shell__avatar" aria-hidden="true">{{ initials() }}</span>
        @if (auth.email(); as email) {
          <bdi class="shell__email" dir="ltr">{{ email }}</bdi>
        }
        <i class="pi pi-chevron-down shell__chevron" [class.shell__chevron--open]="isUserMenuOpen()" aria-hidden="true"></i>
      </p-button>

      <p-menu
        #userMenu
        [model]="userMenuItems()"
        [popup]="true"
        appendTo="body"
        styleClass="shell-user-menu"
        (onShow)="isUserMenuOpen.set(true)"
        (onHide)="isUserMenuOpen.set(false)">
        <ng-template #start>
          <div class="shell-user-menu__header">
            <span class="shell__avatar shell__avatar--large" aria-hidden="true">{{ initials() }}</span>
            @if (auth.email(); as email) {
              <bdi class="shell-user-menu__email" dir="ltr">{{ email }}</bdi>
            }
          </div>
        </ng-template>
      </p-menu>
```

The "Sign out" button leaves the bar and becomes a menu item, as in the design.

- [ ] **Step 4: Style the user menu**

In `client\src\app\features\admin-shell\admin-shell.component.scss`, replace the `.shell__user { ... }` rule with:

```scss
:host ::ng-deep .shell__user.p-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding-block: 0.25rem;
  padding-inline: 0.25rem 0.625rem;
  border: 1px solid transparent;
  border-radius: 999px;
  color: var(--app-text-secondary);
}

:host ::ng-deep .shell__user--open.p-button {
  background: var(--app-bg-muted);
  border-color: var(--app-border);
}

.shell__chevron {
  font-size: 0.7rem;
  color: var(--app-text-muted);
  transition: transform 0.15s ease;
}

.shell__chevron--open {
  transform: rotate(180deg);
}

.shell__avatar--large {
  width: 2.375rem;
  height: 2.375rem;
  font-size: 0.85rem;
}

.shell-user-menu__header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 0.75rem 0.875rem;
  margin-block-end: 0.25rem;
  border-block-end: 1px solid var(--app-border);
}

.shell-user-menu__email {
  font-size: 0.8125rem;
  color: var(--app-text-secondary);
}

::ng-deep .p-menu.shell-user-menu {
  min-inline-size: 18.75rem;
}

::ng-deep [dir='rtl'] .shell-user-menu__item--sign-out .p-menu-item-icon {
  transform: scaleX(-1);
}
```

Keep `.shell__avatar` and `.shell__email` as they are. The header elements are declared in this component's template, so their plain selectors still apply after `appendTo="body"`. Only the PrimeNG-generated menu parts need `::ng-deep`, as in `users.page.scss`.

- [ ] **Step 5: Build and run the client specs**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: the build succeeds with no template errors, and every spec passes (`translations.spec.ts`, `source-text.spec.ts` included).

- [ ] **Step 6: Quick look in the browser**

Start the API against the task 3 smoke database (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us88_smoke;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Start the client with `preview_start {name: "client"}`, sign in as `admin@local.dev` / `DevAdmin#2026`, then check:
- The avatar button opens the menu. Its header shows the email, followed by "שינוי הסיסמה שלי" and "התנתקות".
- "שינוי הסיסמה שלי" opens the dialog. Typing a different confirmation shows "הסיסמאות לא תואמות." and disables "שינוי סיסמה".
- `read_console_messages` with `onlyErrors: true` shows nothing new.

Task 6 runs the full verification. Stop the API and the client.

- [ ] **Step 7: Commit**

```bash
git add client
git commit -m "feat(client): change my password from the shell's user menu (#88)"
```

End the commit message with the attribution trailer from the session's instructions.
