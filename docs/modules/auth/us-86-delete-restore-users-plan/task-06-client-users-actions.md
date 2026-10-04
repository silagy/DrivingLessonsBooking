# Task 6 of 7: Row actions, "You" marker and the Delete dialog

> Part of [#86: Delete and Restore Users](README.md). Requires task 5 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Create: `client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.ts`
- Create: `client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.html`
- Create: `client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.scss`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.ts`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.html`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.scss`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (twelve `users.*` keys)

**Interfaces:**
- Consumes:
  - From task 5: `UsersStore.currentUserId`, `deleteRefusal`, `isMutating`, `clearDeleteRefusal()`, `delete(userId): Promise<boolean>` and `restore(user)`.
  - From task 2: the `errors.*` keys the refusals resolve to.
  - Existing: `User`, `Role`, `InitialsPipe` (`shared\pipes\initials.pipe`), `isolateDirection` (`shared\text\isolate-direction`), `DialogService`, and the tag classes in `users.page.scss`.
- Produces: the finished Users screen for #86, which task 7 verifies in the browser. `DeleteUserDialogData` is the dialog's input.

**Why:** #86 AC 6: the Users screen offers Delete (with confirmation) and Restore, shows the deleted state and displays 409 rule violations translated. AC 8: every string is a translation key, Hebrew first, RTL-correct. README decisions 1, 2, 8, 9 and 10; Review Focus 4.

**Design to match** (Claude Design `Users and Roles.html`):
- **3a/3b. Rows.**
  - Every row ends with an actions cell. Its header is visually hidden ("Actions").
  - Active rows have a 32px round kebab button. It opens a popup menu that, for now, holds one item: a trash icon plus "Delete", in the danger color.
  - Deleted rows show a small text button "Restore" instead of the kebab.
  - The signed-in User's row has a "You" tag next to the name: transparent background, ink text and a 1px ink border.
- **3f. Restore succeeded.** A success toast "User restored" with the detail "{name} can sign in again."
- **3g. Restore refused.** An error toast with the translated rule.
- **8a. The Delete dialog** (500px wide) has the header "Delete {name}?" and, in order:
  1. A who-card: a 34px initials avatar, the name (bold), the email (LTR, secondary) and the Role tag at the inline end. It has a light page-colored background, a 1px border and a 10px radius.
  2. Three bullet lines with 16px muted icons:
     - "They're signed out immediately." (sign-out icon, mirrored in RTL)
     - "Their Teacher record, Week Schedules and Publications are not affected." (shield icon)
     - "You can undo this any time with Restore." (restore icon, mirrored in RTL)
  3. A footer with a text "Cancel" and a danger "Delete" button.
- **8b-8d. The Delete dialog after a refusal.** The same dialog stays open, with an error message as the first body item: bold title "That didn't go through", then the translated rule.

**Run the checks** (from `client\`, PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

- [ ] **Step 1: Add the translations**

In `client\public\i18n\he.json`, inside the top-level `"users"` object, add after `"emptyHint"`. Mind the comma after `"emptyHint"`'s value.

```json
    "actions": "פעולות",
    "you": "את/ה",
    "delete": "מחיקה",
    "restore": "שחזור",
    "deleteTitle": "למחוק את {{name}}?",
    "deleteSignedOut": "הם יוצאו מהמערכת מיד.",
    "deleteKeepsTeacher": "רשומת המורה, המערכות השבועיות והפרסומים לא מושפעים.",
    "deleteUndo": "אפשר לבטל בכל עת עם \"שחזור\".",
    "deleted": "המשתמש נמחק",
    "restored": "המשתמש שוחזר",
    "restoredDetail": "{{name}} יכול/ה להיכנס שוב.",
    "refusedTitle": "הפעולה לא בוצעה"
```

In `client\public\i18n\en.json`, at the same place:

```json
    "actions": "Actions",
    "you": "You",
    "delete": "Delete",
    "restore": "Restore",
    "deleteTitle": "Delete {{name}}?",
    "deleteSignedOut": "They're signed out immediately.",
    "deleteKeepsTeacher": "Their Teacher record, Week Schedules and Publications are not affected.",
    "deleteUndo": "You can undo this any time with Restore.",
    "deleted": "User deleted",
    "restored": "User restored",
    "restoredDetail": "{{name}} can sign in again.",
    "refusedTitle": "That didn't go through"
```

Run `ng test`. Expected: green. `translations.spec.ts` checks that both files have the same keys and no long dashes or ellipsis characters.

- [ ] **Step 2: Create the Delete dialog**

`client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';

export interface DeleteUserDialogData {
    user: User;
    refusal: Signal<string | null>;
    isDeleting: Signal<boolean>;
    confirm: () => Promise<boolean>;
}

@Component({
    selector: 'app-delete-user-dialog',
    imports: [TranslocoPipe, InitialsPipe, ButtonModule, MessageModule, TagModule],
    templateUrl: './delete-user.dialog.html',
    styleUrl: './delete-user.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteUserDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<DeleteUserDialogData>).data as DeleteUserDialogData;

    protected readonly roles = Role;
    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isDeleting = this.data.isDeleting;

    protected async confirm(): Promise<void> {
        const deleted = await this.data.confirm();

        if (deleted) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

Check the relative import depth against `add-user.dialog.ts`, which sits at the same depth. Copy its `InitialsPipe` path if it differs (the page imports it as `../../../../../shared/pipes/initials.pipe`).

`client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.html`:

```html
<div class="delete-user">
  @if (refusal(); as message) {
    <p-message severity="error" class="delete-user__refusal">
      <span class="delete-user__refusal-text">
        <strong>{{ 'users.refusedTitle' | transloco }}</strong>
        <span>{{ message }}</span>
      </span>
    </p-message>
  }

  <div class="delete-user__who">
    <span class="delete-user__avatar" aria-hidden="true">{{ user.name | initials }}</span>
    <span class="delete-user__identity">
      <span class="delete-user__name">{{ user.name }}</span>
      <bdi dir="ltr" class="delete-user__email">{{ user.signInEmail }}</bdi>
    </span>
    <p-tag
      class="delete-user__role"
      [class.delete-user__role--administrator]="user.role === roles.administrator"
      [class.delete-user__role--teacher]="user.role === roles.teacher"
      [value]="('users.roles.' + user.role) | transloco" />
  </div>

  <ul class="delete-user__effects">
    <li class="delete-user__effect">
      <i class="pi pi-sign-out delete-user__icon delete-user__icon--mirrored" aria-hidden="true"></i>
      {{ 'users.deleteSignedOut' | transloco }}
    </li>
    <li class="delete-user__effect">
      <i class="pi pi-shield delete-user__icon" aria-hidden="true"></i>
      {{ 'users.deleteKeepsTeacher' | transloco }}
    </li>
    <li class="delete-user__effect">
      <i class="pi pi-replay delete-user__icon delete-user__icon--mirrored" aria-hidden="true"></i>
      {{ 'users.deleteUndo' | transloco }}
    </li>
  </ul>

  <div class="delete-user__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button
      [label]="'users.delete' | transloco"
      severity="danger"
      type="button"
      [disabled]="isDeleting()"
      (onClick)="confirm()" />
  </div>
</div>
```

`client\src\app\features\users\ui\dialogs\delete-user\delete-user.dialog.scss`. It uses logical properties only, and the tag tokens are the same ones `users.page.scss` uses.

```scss
.delete-user {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.delete-user__refusal-text {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.delete-user__who {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.625rem 0.75rem;
    border-radius: 10px;
    background: var(--app-bg-page);
    border: 1px solid var(--app-border);
}

.delete-user__avatar {
    flex: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    inline-size: 34px;
    block-size: 34px;
    border-radius: 999px;
    background: var(--app-steel-light);
    color: var(--app-whale);
    font-size: 0.8rem;
    font-weight: 700;
}

.delete-user__identity {
    display: flex;
    flex-direction: column;
    min-inline-size: 0;
}

.delete-user__name {
    font-size: 0.9rem;
    font-weight: 600;
    color: var(--app-ink);
}

.delete-user__email {
    font-size: 0.8rem;
    color: var(--app-text-secondary);
}

.delete-user__role {
    margin-inline-start: auto;
    border: 1px solid transparent;
    border-radius: 999px;
    padding: 3px 10px;
    font-size: 0.75rem;
    font-weight: 600;
    white-space: nowrap;
}

.delete-user__role--administrator {
    background: var(--p-sky-50);
    color: var(--p-sky-700);
    border-color: var(--p-sky-100);
}

.delete-user__role--teacher {
    background: var(--p-ocean-50);
    color: var(--p-ocean-700);
    border-color: var(--p-ocean-100);
}

.delete-user__effects {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 0;
    padding: 0;
    list-style: none;
}

.delete-user__effect {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    font-size: 0.875rem;
    line-height: 1.5;
    color: var(--app-text-secondary);
}

.delete-user__icon {
    margin-block-start: 0.2rem;
    font-size: 1rem;
    color: var(--app-text-muted);
}

:host-context([dir='rtl']) .delete-user__icon--mirrored {
    transform: scaleX(-1);
}

.delete-user__actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.625rem;
    padding-block-start: 0.5rem;
}
```

If `p-tag` doesn't take the classes on its host the way `users.page.scss` relies on, copy that file's selector approach (it styles `.users__tag` on the `p-tag` host). Keep the two files consistent.

- [ ] **Step 3: Wire the actions into the page component**

In `client\src\app\features\users\ui\pages\users\users.page.ts`:

1. Update the imports:

```ts
import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
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
import { DeleteUserDialog, DeleteUserDialogData } from '../../dialogs/delete-user/delete-user.dialog';
```

2. Add `MenuModule` to the component's `imports` array.
3. Inside the class, add after `private static readonly dialogWidth = '33.75rem';`:

```ts
    private static readonly deleteDialogWidth = '31.25rem';
```

and after `protected readonly roles = Role;`:

```ts
    protected readonly rowActions = signal<MenuItem[]>([]);

    private readonly rowMenu = viewChild.required<Menu>('rowMenu');
```

4. Add the methods after `onAddUser`:

```ts
    protected onRowActions(event: Event, user: User): void {
        this.rowActions.set([
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

    private onDeleteUser(user: User): void {
        this.store.clearDeleteRefusal();

        const data: DeleteUserDialogData = {
            user,
            refusal: this.store.deleteRefusal,
            isDeleting: this.store.isMutating,
            confirm: () => this.store.delete(user.id),
        };

        this.dialogs.open(DeleteUserDialog, {
            header: this.transloco.translate('users.deleteTitle', { name: isolateDirection(user.name) }),
            width: UsersPage.deleteDialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
```

`clearDeleteRefusal()` before `open` is Review Focus 4: a dialog reopened after a refusal starts clean.

- [ ] **Step 4: Add the column, the "You" tag, the actions and the menu to the template**

In `client\src\app\features\users\ui\pages\users\users.page.html`:

1. In the header row, add after the Status `<th>`:

```html
            <th class="users__actions-header">
              <span class="users__sr-only">{{ 'users.actions' | transloco }}</span>
            </th>
```

2. In the name cell, replace `<span class="users__name-text">{{ user.name }}</span>` with:

```html
                <span class="users__name-text">{{ user.name }}</span>
                @if (user.id === store.currentUserId()) {
                  <p-tag class="users__tag users__tag--you" [value]="'users.you' | transloco" />
                }
```

3. In the body row, add after the Status `<td>`:

```html
            <td class="users__actions">
              @if (user.isDeleted) {
                <p-button
                  [label]="'users.restore' | transloco"
                  text
                  size="small"
                  [disabled]="store.isMutating()"
                  (onClick)="onRestoreUser(user)" />
              } @else {
                <p-button
                  icon="pi pi-ellipsis-v"
                  rounded
                  text
                  severity="secondary"
                  [ariaLabel]="'users.actions' | transloco"
                  (onClick)="onRowActions($event, user)" />
              }
            </td>
```

4. Right after the closing `</p-table>`, add the shared popup menu:

```html
      <p-menu #rowMenu [model]="rowActions()" [popup]="true" appendTo="body" />
```

- [ ] **Step 5: Style the new pieces**

Append to `client\src\app\features\users\ui\pages\users\users.page.scss`:

```scss
.users__tag--you {
    background: transparent;
    color: var(--app-ink);
    border-color: var(--app-ink);
}

.users__actions-header {
    inline-size: 90px;
}

.users__actions {
    text-align: end;
    white-space: nowrap;
}

.users__sr-only {
    position: absolute;
    inline-size: 1px;
    block-size: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
}

::ng-deep .users-menu__item--danger .p-menu-item-link,
::ng-deep .users-menu__item--danger .p-menu-item-icon,
::ng-deep .users-menu__item--danger .p-menu-item-label {
    color: var(--p-red-600);
}
```

The menu is appended to `body`, outside the component's host, so its rule is unscoped `::ng-deep`. If the PrimeNG 21 class names differ, inspect the rendered menu in task 7 and adjust. The `.users__name` flex container already spaces the "You" tag with its `gap`.

- [ ] **Step 6: Run the checks**

Run both commands above. Expected: `ng test` is green and `ng build` succeeds with no new warnings about unused imports.

- [ ] **Step 7: Quick look in the browser**

With the API running against a disposable database (task 3 step 8) and the client started (`preview_start {name: "client"}`), sign in as `admin@local.dev`. Check all of the following:
- The signed-in row has "את/ה".
- The kebab opens a menu with "מחיקה" in red.
- Delete opens the dialog with the who-card and three lines.
- Deleting your own row shows "הפעולה לא בוצעה" / "אי אפשר למחוק את עצמך." inside the dialog, and the dialog stays open.
- Cancel and reopen: the refusal is gone.

Task 7 does the full check.

- [ ] **Step 8: Commit**

```bash
git add client
git commit -m "feat(client): delete Users from a row menu and restore Deleted Users (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
