# Task 5 of 8: Signed-in User store and the shared locked field (client)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 4 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 7, 8 and 9 first.

**Files:**
- Create: `client\src\app\core\signed-in-user\get-user.response.ts`
- Create: `client\src\app\core\signed-in-user\signed-in-user-api.service.ts`
- Create: `client\src\app\core\signed-in-user\signed-in-user.store.ts`
- Test: `client\src\app\core\signed-in-user\signed-in-user.store.spec.ts`
- Create: `client\src\app\shared\components\locked-field\locked-field.component.ts`, `.html`, `.scss`
- Test: `client\src\app\shared\components\locked-field\locked-field.component.spec.ts`
- Modify: `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.html`, `edit-user.dialog.ts`
- Delete: `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.scss` (every rule in it moves to the component)
- Modify: `.claude\rules\client-architecture.md` (the `core\` and `shared\components\` lines of the folder tree)

**Interfaces:**
- Consumes: `GET api/me` from task 4, which returns `GetUserResponse` as camelCase JSON: `{ id, name, signInEmail, role, teacherId, teacherName }`, `role` is `"administrator"` or `"teacher"`, `teacherId` and `teacherName` are `null` for a User not linked to a Teacher. `AuthService.token: Signal<string | null>` (#89).
- Produces (tasks 6 and 7 rely on these exact names):
  - `client\src\app\core\signed-in-user\get-user.response.ts`: `export interface GetUserResponse { id: string; name: string; signInEmail: string; role: Role; teacherId: string | null; teacherName: string | null; }`
  - `SignedInUserApiService` (`providedIn: 'root'`): `getMe(): Observable<GetUserResponse>`, `GET api/me`
  - `SignedInUserStore` (`providedIn: 'root'`), readonly signals:
    - `name: Signal<string | null>`: the signed-in User's name
    - `teacherName: Signal<string | null>`: the linked Teacher's name
    - both are `null` while loading, when signed out, when the User has no linked Teacher (`teacherName` only) and when `api/me` fails; the store never throws
    - loads once per token: a new token (sign in as another User, or a refreshed token after a password change) loads again; signing out clears it
  - `LockedFieldComponent`, selector `app-locked-field`: `label = input.required<string>()`, `value = input.required<string>()`; content projected into the chip after the value is shown as a muted note

**Design (frame 10a chip, `AuLockChip`):** label above the chip, 13px / 600, secondary text color. Chip 42px high, padding `0 14px`, radius 6px, grey surface (`--app-bg-muted`) with a 1px solid border (`--app-border`). Lock icon 15px in the muted text color, hidden from screen readers. Value 14.5px / 600 in ink (`--app-ink`). In rem: 0.8125rem, 0.875rem, 0.9375rem, 0.90625rem. The edit-user dialog picks up the same chip, so its border turns from dashed to solid and its value from 400 to 600: that is the 10a look, one chip everywhere. The dialog's "· can't be changed" note keeps its 0.78rem muted style.

The note is projected as plain text into a `<span class="locked-field__note">` the component owns. A projected element keeps its parent's style scope, so a span written by the dialog would not get the component's note style; owning the span keeps the style in one place with no `::ng-deep`. When nothing is projected the span is `:empty` and hidden, so the chip's flex gap doesn't leave a trailing space.

**Why:** decision 7 (the token has no Teacher name and a Teacher can't call `api/teachers/find`, so `api/me` is the only source for the 10a chip), decision 8 (in `core\`, so both features can read it without importing each other) and decision 9 (one locked field, shared by the edit-user dialog and Weekly prep in task 6).

- [ ] **Step 1: Add the response and the API service**

Create `client\src\app\core\signed-in-user\get-user.response.ts`:

```typescript
import { Role } from '../../shared/models/role.enum';

export interface GetUserResponse {
    id: string;
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    teacherName: string | null;
}
```

Create `client\src\app\core\signed-in-user\signed-in-user-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetUserResponse } from './get-user.response';

@Injectable({ providedIn: 'root' })
export class SignedInUserApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/me';

    getMe(): Observable<GetUserResponse> {
        return this.http.get<GetUserResponse>(this.baseUrl);
    }
}
```

(The admin-shell `MeApiService` for `PUT api/me/password` stays where it is; decision 8.)

- [ ] **Step 2: Write the failing store specs**

Create `client\src\app\core\signed-in-user\signed-in-user.store.spec.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Role } from '../../shared/models/role.enum';
import { AuthService } from '../auth.service';
import { SignedInUserApiService } from './signed-in-user-api.service';
import { GetUserResponse } from './get-user.response';
import { SignedInUserStore } from './signed-in-user.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;
const YAEL_TOKEN = 'token-yael';
const DANI_TOKEN = 'token-dani';

const YAEL: GetUserResponse = {
    id: 'user-yael',
    name: 'Yael Carmi',
    signInEmail: 'yael@school.example',
    role: Role.teacher,
    teacherId: 'teacher-yael',
    teacherName: 'Teacher Yael',
};

const DANI: GetUserResponse = {
    id: 'user-dani',
    name: 'Dani Levi',
    signInEmail: 'dani@school.example',
    role: Role.administrator,
    teacherId: null,
    teacherName: null,
};

function storeSignedInWith(
    token: string | null,
    getMe: ReturnType<typeof vi.fn> = vi.fn(() => of(YAEL)),
) {
    const auth = { token: signal<string | null>(token) };

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: AuthService, useValue: auth },
            { provide: SignedInUserApiService, useValue: { getMe } },
        ],
    });

    return { store: TestBed.inject(SignedInUserStore), auth, getMe };
}

async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('SignedInUserStore', () => {
    it('loads the signed-in User once signed in', async () => {
        //given
        const { store, getMe } = storeSignedInWith(YAEL_TOKEN);

        //when
        await settle();

        //then
        expect(store.name()).toBe('Yael Carmi');
        expect(store.teacherName()).toBe('Teacher Yael');
        expect(store.teacherName()).toBe('Teacher Yael');
        expect(getMe).toHaveBeenCalledTimes(1);
    });

    it('has no signed-in User when signed out', async () => {
        //given
        const { store, getMe } = storeSignedInWith(null);

        //when
        await settle();

        //then
        expect(getMe).not.toHaveBeenCalled();
        expect(store.name()).toBeNull();
        expect(store.teacherName()).toBeNull();
    });

    it('has no linked Teacher name for a User not linked to a Teacher', async () => {
        //given
        const { store } = storeSignedInWith(DANI_TOKEN, vi.fn(() => of(DANI)));

        //when
        await settle();

        //then
        expect(store.name()).toBe('Dani Levi');
        expect(store.teacherName()).toBeNull();
    });

    it('loads the other User when signed in as another User', async () => {
        //given
        const getMe = vi.fn().mockReturnValueOnce(of(YAEL)).mockReturnValue(of(DANI));
        const { store, auth } = storeSignedInWith(YAEL_TOKEN, getMe);
        await settle();

        //when
        auth.token.set(DANI_TOKEN);
        await settle();

        //then
        expect(getMe).toHaveBeenCalledTimes(2);
        expect(store.name()).toBe('Dani Levi');
        expect(store.teacherName()).toBeNull();
    });

    it('forgets the signed-in User on sign out', async () => {
        //given
        const { store, auth, getMe } = storeSignedInWith(YAEL_TOKEN);
        await settle();

        //when
        auth.token.set(null);
        await settle();

        //then
        expect(getMe).toHaveBeenCalledTimes(1);
        expect(store.name()).toBeNull();
        expect(store.teacherName()).toBeNull();
    });

    it('has no signed-in User instead of throwing when the load fails', async () => {
        //given
        const failingGetMe = vi.fn(() =>
            throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        );
        const { store } = storeSignedInWith(YAEL_TOKEN, failingGetMe);

        //when
        await settle();

        //then
        expect(store.name()).toBeNull();
        expect(store.teacherName()).toBeNull();
    });
});
```

(The first spec reads `teacherName()` twice on purpose: reading the signals again must not load again, which `toHaveBeenCalledTimes(1)` pins.)

- [ ] **Step 3: Run the specs to verify they fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/signed-in-user/signed-in-user.store.spec.ts
```

**Expected:** FAIL: the spec can't resolve `./signed-in-user.store`.

- [ ] **Step 4: Write the store**

Create `client\src\app\core\signed-in-user\signed-in-user.store.ts`:

```typescript
import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth.service';
import { SignedInUserApiService } from './signed-in-user-api.service';
import { GetUserResponse } from './get-user.response';

@Injectable({ providedIn: 'root' })
export class SignedInUserStore {
    private readonly api = inject(SignedInUserApiService);
    private readonly auth = inject(AuthService);

    private readonly meResource = resource({
        params: () => this.auth.token() ?? undefined,
        loader: () => firstValueFrom(this.api.getMe()),
    });

    private readonly signedInUser = computed<GetUserResponse | null>(() =>
        this.meResource.hasValue() ? this.meResource.value() : null,
    );

    readonly name = computed(() => this.signedInUser()?.name ?? null);
    readonly teacherName = computed(() => this.signedInUser()?.teacherName ?? null);
}
```

Keyed on the token, not on `userId`: a refreshed token for the same User (password change, #89) reloads too, and the request always carries the token the server reads.

- [ ] **Step 5: Run the specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/signed-in-user/signed-in-user.store.spec.ts
```

**Expected:** PASS, 6 specs.

- [ ] **Step 6: Write the failing locked-field specs**

Create `client\src\app\shared\components\locked-field\locked-field.component.spec.ts`:

```typescript
import { Component, provideZonelessChangeDetection, Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LockedFieldComponent } from './locked-field.component';

@Component({
    imports: [LockedFieldComponent],
    template: `<app-locked-field label="Teacher" value="Yael Carmi" />`,
})
class LockedFieldHost {}

@Component({
    imports: [LockedFieldComponent],
    template: `<app-locked-field label="Teacher" value="Yael Carmi">cannot be changed</app-locked-field>`,
})
class LockedFieldWithNoteHost {}

async function render(host: Type<unknown>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [host],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(host);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('LockedFieldComponent', () => {
    it('shows the label above the locked value', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const field = element.querySelector('app-locked-field');

        //then
        expect(field?.firstElementChild?.classList).toContain('field__label');
        expect(field?.firstElementChild?.textContent?.trim()).toBe('Teacher');
        expect(element.querySelector('.locked-field__value')?.textContent?.trim()).toBe('Yael Carmi');
    });

    it('shows a lock icon that screen readers skip', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const icon = element.querySelector('.locked-field .pi-lock');

        //then
        expect(icon).not.toBeNull();
        expect(icon?.getAttribute('aria-hidden')).toBe('true');
    });

    it('shows a projected note after the value', async () => {
        //given
        const element = await render(LockedFieldWithNoteHost);

        //when
        const note = element.querySelector('.locked-field .locked-field__value + .locked-field__note');

        //then
        expect(note?.textContent?.trim()).toBe('cannot be changed');
    });

    it('leaves the note empty when none is projected', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const note = element.querySelector('.locked-field__note');

        //then
        expect(note?.childNodes.length).toBe(0);
    });
});
```

(The last spec pins what the `:empty` rule in step 8 relies on: no text node, not even whitespace.)

- [ ] **Step 7: Run the specs to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/components/locked-field/locked-field.component.spec.ts
```

**Expected:** FAIL: the spec can't resolve `./locked-field.component`.

- [ ] **Step 8: Write the component**

Create `client\src\app\shared\components\locked-field\locked-field.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'app-locked-field',
    templateUrl: './locked-field.component.html',
    styleUrl: './locked-field.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LockedFieldComponent {
    readonly label = input.required<string>();
    readonly value = input.required<string>();
}
```

Create `client\src\app\shared\components\locked-field\locked-field.component.html` (the note span and `<ng-content />` stay on one line, with nothing between them):

```html
<span class="field__label">{{ label() }}</span>
<div class="locked-field">
  <i class="pi pi-lock locked-field__icon" aria-hidden="true"></i>
  <span class="locked-field__value">{{ value() }}</span>
  <span class="locked-field__note"><ng-content /></span>
</div>
```

Create `client\src\app\shared\components\locked-field\locked-field.component.scss`:

```scss
:host {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.field__label {
  font-weight: 600;
  font-size: 0.8125rem;
  color: var(--app-text-secondary);
}

.locked-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-block-size: 42px;
  padding-inline: 0.875rem;
  border-radius: 6px;
  background: var(--app-bg-muted);
  border: 1px solid var(--app-border);
}

.locked-field__icon {
  font-size: 0.9375rem;
  color: var(--app-text-muted);
}

.locked-field__value {
  font-size: 0.90625rem;
  font-weight: 600;
  color: var(--app-ink);
}

.locked-field__note {
  font-size: 0.78rem;
  color: var(--app-text-muted);

  &:empty {
    display: none;
  }
}
```

(`:host` takes over the `.field` column layout from `shared\dialogs\dialog-form.scss`: same direction and 0.375rem gap, so the component needs no wrapper. `--app-text-secondary` is `--p-surface-500`, the color the dialog's `.field__label` used.)

- [ ] **Step 9: Run the specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/shared/components/locked-field/locked-field.component.spec.ts
```

**Expected:** PASS, 4 specs.

- [ ] **Step 10: The edit-user dialog uses the component**

In `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.html`, replace the Teacher block:

```html
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
```

with:

```html
  @if (user.teacherName; as teacherName) {
    <app-locked-field [label]="'users.teacher' | transloco" [value]="teacherName">· {{ 'users.linkLocked' | transloco }}</app-locked-field>
  }
```

Delete `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.scss`: its five rules (`.field__label`, `.locked-field`, `.locked-field__icon`, `.locked-field__value`, `.locked-field__note`) were used only by this block and now live in the component.

```bash
git rm client/src/app/features/users/ui/dialogs/edit-user/edit-user.dialog.scss
```

In `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.ts`, add the import:

```typescript
import { LockedFieldComponent } from '../../../../../shared/components/locked-field/locked-field.component';
```

and change the `@Component` metadata's `imports` and `styleUrls` to:

```typescript
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule, DialogRefusalComponent, LockedFieldComponent],
    templateUrl: './edit-user.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss'],
```

Confirm nothing else uses the old classes outside the component:

```bash
grep -rn "locked-field\|field__label" client/src/app --include=*.html --include=*.scss --include=*.ts
```

**Expected:** hits only in `shared/components/locked-field/` and the one `<app-locked-field>` in `edit-user.dialog.html`.

Run the users specs (none renders the dialog, so they must stay green untouched):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/users/**/*.spec.ts"
```

**Expected:** PASS (`role-change.spec.ts`, `teacher-link.spec.ts`, `users.store.spec.ts`).

- [ ] **Step 11: Note the new pieces in the client architecture rule**

In `.claude\rules\client-architecture.md`, in the folder tree, replace:

```
    │   │   └── services\              language.service.ts, jerusalem-date.service.ts, toast.service.ts
```

with:

```
    │   │   ├── services\              language.service.ts, jerusalem-date.service.ts, toast.service.ts
    │   │   └── signed-in-user\        SignedInUserStore over api/me: name, linked Teacher name (#90)
```

and replace:

```
    │       ├── components\            week-grid, status-tag, empty-state, ...
```

with:

```
    │       ├── components\            week-grid, status-tag, empty-state, locked-field, ...
```

- [ ] **Step 12: Run the whole client suite and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

**Expected:** all PASS (including `source-text.spec.ts`: the `·` note separator is neither a long dash nor an ellipsis), build succeeds. Nothing uses `SignedInUserStore` yet; task 6 does.

- [ ] **Step 13: Commit**

```bash
git add client/src/app/core/signed-in-user client/src/app/shared/components/locked-field client/src/app/features/users/ui/dialogs/edit-user .claude/rules/client-architecture.md
git commit -m "feat(client): the signed-in User's linked Teacher and a shared locked field (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
