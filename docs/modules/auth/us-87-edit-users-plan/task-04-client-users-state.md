# Task 4 of 6: Client API and store for Edit details, Change Role and Set Temporary Password

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Requires task 3 committed. Work on branch `87-edit-users`.

**Files:**
- Create: `client\src\app\features\users\data\change-user-details.request.ts`
- Create: `client\src\app\features\users\data\change-user-role.request.ts`
- Create: `client\src\app\features\users\data\set-user-temporary-password.request.ts`
- Modify: `client\src\app\features\users\data\users-api.service.ts`
- Modify: `client\src\app\features\users\state\users.store.ts`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.ts` (rename only)
- Test: `client\src\app\features\users\state\users.store.spec.ts` (rewritten)

**Interfaces:**
- Consumes: task 3's three routes; the existing `ToastService.success(key)` and `ToastService.messageOf(error)`; `UsersApiService`.
- Produces:
  - DTOs whose names are identical to the backend's (`ChangeUserDetailsRequest`, `ChangeUserRoleRequest`, `SetUserTemporaryPasswordRequest`):
    - `ChangeUserDetailsRequest { name: string; signInEmail: string }`
    - `ChangeUserRoleRequest { role: Role }`
    - `SetUserTemporaryPasswordRequest { temporaryPassword: string }`
  - `UsersApiService` methods:
    - `changeUserDetails(userId: string, request: ChangeUserDetailsRequest): Observable<void>`, which calls `PUT api/users/{id}/details`
    - `changeUserRole(userId: string, request: ChangeUserRoleRequest): Observable<void>`, which calls `PUT api/users/{id}/role`
    - `setUserTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Observable<void>`, which calls `PUT api/users/{id}/temporary-password`
  - `UsersStore`:
    - `refusal: Signal<string | null>` and `clearRefusal(): void` replace `deleteRefusal` and `clearDeleteRefusal`.
    - `changeDetails(userId: string, request: ChangeUserDetailsRequest): Promise<boolean>`
    - `changeRole(userId: string, request: ChangeUserRoleRequest): Promise<boolean>`
    - `setTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Promise<boolean>`
    - `delete(userId: string): Promise<boolean>` is unchanged.
  - Every dialog command returns `true` on success, after a success toast and a list reload. On failure it returns `false`, and the translated message is in `refusal()`; no error toast is shown.
  - Success toast keys: `users.detailsChanged`, `users.roleChanged`, `users.temporaryPasswordSet`. Task 5 adds them to the translation files.

**Why:**
- #87 AC 7: violations appear translated, inside the dialog (design 5b, 6c-6f).
- README decision 7; Review Focus 5.

**Run the tests** (from `client\`, in PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/users/state/users.store.spec.ts
```

If `--include` isn't accepted, run the whole suite without it.

- [ ] **Step 1: Add the request DTOs**

`client\src\app\features\users\data\change-user-details.request.ts`:

```ts
export interface ChangeUserDetailsRequest {
    name: string;
    signInEmail: string;
}
```

`client\src\app\features\users\data\change-user-role.request.ts`:

```ts
import { Role } from '../domain/role.enum';

export interface ChangeUserRoleRequest {
    role: Role;
}
```

`client\src\app\features\users\data\set-user-temporary-password.request.ts`:

```ts
export interface SetUserTemporaryPasswordRequest {
    temporaryPassword: string;
}
```

- [ ] **Step 2: Rewrite the store spec (failing)**

Replace the whole of `client\src\app\features\users\state\users.store.spec.ts` with the code below. It does three things:
- `createStore` now takes one object of API stub overrides instead of four positional functions, so the three new API methods don't push it to seven arguments.
- Every existing test keeps its name and assertions. The one exception is the rename from `deleteRefusal` / `clearDeleteRefusal` to `refusal` / `clearRefusal`.
- New tests cover the three commands.

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { ChangeUserDetailsRequest } from '../data/change-user-details.request';
import { ChangeUserRoleRequest } from '../data/change-user-role.request';
import { CreateUserRequest } from '../data/create-user.request';
import { CreateUserResponse } from '../data/create-user.response';
import { ItemForFindTeachersResponse } from '../data/item-for-find-teachers.response';
import { ItemForFindUsersResponse } from '../data/item-for-find-users.response';
import { SetUserTemporaryPasswordRequest } from '../data/set-user-temporary-password.request';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { UsersApiService } from '../data/users-api.service';
import { Role } from '../domain/role.enum';
import { UsersStore } from './users.store';

const HTTP_CONFLICT = 409;
const HTTP_INTERNAL_SERVER_ERROR = 500;

const ADMINISTRATOR: ItemForFindUsersResponse = {
    id: 'user-owner',
    name: 'School Owner',
    signInEmail: 'owner@school.example',
    role: Role.administrator,
    teacherId: null,
    teacherName: null,
    isDeleted: false,
};

const TEACHER_USER: ItemForFindUsersResponse = {
    id: 'user-levi',
    name: 'Dana Levi',
    signInEmail: 'dana@school.example',
    role: Role.teacher,
    teacherId: 'teacher-levi',
    teacherName: 'Dana Levi',
    isDeleted: false,
};

const DELETED_USER: ItemForFindUsersResponse = {
    id: 'user-nahum',
    name: 'Gil Nahum',
    signInEmail: 'gil@school.example',
    role: Role.teacher,
    teacherId: 'teacher-nahum',
    teacherName: 'Gil Nahum',
    isDeleted: true,
};

const TEACHERS: ItemForFindTeachersResponse[] = [
    { id: 'teacher-levi', name: 'Dana Levi', contactEmail: 'dana.teaches@school.example' },
    { id: 'teacher-cohen', name: 'Avi Cohen', contactEmail: 'avi@school.example' },
];

const REQUEST: CreateUserRequest = {
    name: 'Avi Cohen',
    signInEmail: 'avi.signin@school.example',
    role: Role.teacher,
    teacherId: 'teacher-cohen',
    temporaryPassword: 'Temporary#2026',
};

const DETAILS: ChangeUserDetailsRequest = { name: 'Dana Levi-Cohen', signInEmail: 'dana.new@school.example' };
const TO_ADMINISTRATOR: ChangeUserRoleRequest = { role: Role.administrator };
const TEMPORARY_PASSWORD: SetUserTemporaryPasswordRequest = { temporaryPassword: 'Fresh#2027' };

function conflict(code: string): HttpErrorResponse {
    return new HttpErrorResponse({
        status: HTTP_CONFLICT,
        error: { status: HTTP_CONFLICT, title: 'Conflict', code },
    });
}

const SELF_REFUSAL = conflict('userMustNotDeleteSelf');

interface ApiStubs {
    findUsers: () => Observable<ItemForFindUsersResponse[]>;
    createUser: () => Observable<CreateUserResponse>;
    deleteUser: () => Observable<void>;
    restoreUser: () => Observable<void>;
    changeUserDetails: () => Observable<void>;
    changeUserRole: () => Observable<void>;
    setUserTemporaryPassword: () => Observable<void>;
}

function createStore(overrides: Partial<ApiStubs> = {}): UsersStore {
    const stubs: ApiStubs = {
        findUsers: () => of([ADMINISTRATOR]),
        createUser: () => NEVER,
        deleteUser: () => of(undefined),
        restoreUser: () => of(undefined),
        changeUserDetails: () => of(undefined),
        changeUserRole: () => of(undefined),
        setUserTemporaryPassword: () => of(undefined),
        ...overrides,
    };

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            UsersStore,
            {
                provide: UsersApiService,
                useValue: {
                    findUsers: vi.fn(stubs.findUsers),
                    createUser: vi.fn(stubs.createUser),
                    deleteUser: vi.fn(stubs.deleteUser),
                    restoreUser: vi.fn(stubs.restoreUser),
                    changeUserDetails: vi.fn(stubs.changeUserDetails),
                    changeUserRole: vi.fn(stubs.changeUserRole),
                    setUserTemporaryPassword: vi.fn(stubs.setUserTemporaryPassword),
                },
            },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(() => of(TEACHERS)) } },
            {
                provide: ToastService,
                useValue: { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') },
            },
            { provide: AuthService, useValue: { userId: signal('user-owner') } },
        ],
    });

    return TestBed.inject(UsersStore);
}

async function stable(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('UsersStore', () => {
    it('lists the Users', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });

        //when
        await stable();

        //then
        expect(store.users()).toEqual([ADMINISTRATOR, TEACHER_USER]);
        expect(store.isLoading()).toBe(false);
    });

    it('reports the load error instead of throwing when the Users fail to load', async () => {
        //given
        const store = createStore({
            findUsers: () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        });

        //when
        await stable();

        //then
        expect(store.users()).toEqual([]);
        expect(store.isLoading()).toBe(false);
        expect(store.loadError()).toBe('users.loadFailed');
    });

    it('loads the Users again when asked to retry', async () => {
        //given
        const store = createStore();
        await stable();
        const api = TestBed.inject(UsersApiService);

        //when
        store.reload();

        //then
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
    });

    it('marks the Teachers that already have a User, ordered by name', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });

        //when
        await stable();

        //then
        expect(store.linkableTeachers()).toEqual([
            { id: 'teacher-cohen', name: 'Avi Cohen', alreadyLinked: false },
            { id: 'teacher-levi', name: 'Dana Levi', alreadyLinked: true },
        ]);
    });

    it('knows when the first Administrator is the only User', async () => {
        //given
        const store = createStore();

        //when
        await stable();

        //then
        expect(store.hasOnlyOneUser()).toBe(true);
    });

    it('creates the User, confirms it and reloads the list', async () => {
        //given
        const store = createStore({ createUser: () => of({ id: 'user-new' }) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.create(REQUEST);

        //then
        expect(api.createUser).toHaveBeenCalledWith(REQUEST);
        expect(toast.success).toHaveBeenCalledWith('users.created');
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('loads the Teachers again for a new store instance', async () => {
        //given
        createStore();
        await stable();
        const teacherOptionsApi = TestBed.inject(TeacherOptionsApiService);

        //when
        TestBed.runInInjectionContext(() => new UsersStore());
        await stable();

        //then
        expect(teacherOptionsApi.findTeachers).toHaveBeenCalledTimes(2);
    });

    it('shows the refusal and keeps the list when the server rejects the User', async () => {
        //given
        const refusal = conflict('userSignInEmailAlreadyInUse');
        const store = createStore({ createUser: () => throwError(() => refusal) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.create(REQUEST);

        //then
        expect(toast.apiError).toHaveBeenCalledWith(refusal);
        expect(toast.success).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('knows which User is signed in', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });

        //expected
        expect(store.currentUserId()).toBe('user-owner');
    });

    it('deletes the User, confirms it and reloads the list', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(TEACHER_USER.id);

        //then
        expect(deleted).toBe(true);
        expect(api.deleteUser).toHaveBeenCalledWith('user-levi');
        expect(toast.success).toHaveBeenCalledWith('users.deleted');
        expect(store.refusal()).toBeNull();
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('keeps the refusal for the Delete dialog instead of a toast', async () => {
        //given
        const store = createStore({ deleteUser: () => throwError(() => SELF_REFUSAL) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(ADMINISTRATOR.id);

        //then
        expect(deleted).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(SELF_REFUSAL);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('clears the refusal so a reopened dialog starts clean', async () => {
        //given
        const store = createStore({ deleteUser: () => throwError(() => SELF_REFUSAL) });
        await stable();
        await store.delete(ADMINISTRATOR.id);

        //when
        store.clearRefusal();

        //then
        expect(store.refusal()).toBeNull();
    });

    it('restores the User, names them in the confirmation and reloads the list', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, DELETED_USER]) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.restore(DELETED_USER);

        //then
        expect(api.restoreUser).toHaveBeenCalledWith('user-nahum');
        expect(toast.success).toHaveBeenCalledWith('users.restored', {
            key: 'users.restoredDetail',
            params: { name: isolateDirection('Gil Nahum') },
        });
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('shows a Restore refusal as a toast and keeps the list', async () => {
        //given
        const refusal = conflict('userAlreadyActive');
        const store = createStore({
            findUsers: () => of([ADMINISTRATOR, DELETED_USER]),
            restoreUser: () => throwError(() => refusal),
        });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.restore(DELETED_USER);

        //then
        expect(toast.apiError).toHaveBeenCalledWith(refusal);
        expect(toast.success).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('changes the details, confirms it and reloads the list', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.changeDetails(TEACHER_USER.id, DETAILS);

        //then
        expect(changed).toBe(true);
        expect(api.changeUserDetails).toHaveBeenCalledWith('user-levi', DETAILS);
        expect(toast.success).toHaveBeenCalledWith('users.detailsChanged');
        expect(store.refusal()).toBeNull();
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('keeps an Edit details refusal for the dialog', async () => {
        //given
        const refusal = conflict('userSignInEmailAlreadyInUse');
        const store = createStore({ changeUserDetails: () => throwError(() => refusal) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.changeDetails(TEACHER_USER.id, DETAILS);

        //then
        expect(changed).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(refusal);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('changes the Role, confirms it and reloads the list', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.changeRole(TEACHER_USER.id, TO_ADMINISTRATOR);

        //then
        expect(changed).toBe(true);
        expect(api.changeUserRole).toHaveBeenCalledWith('user-levi', TO_ADMINISTRATOR);
        expect(toast.success).toHaveBeenCalledWith('users.roleChanged');
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('keeps a Change Role refusal for the dialog', async () => {
        //given
        const refusal = conflict('userMustNotChangeOwnRole');
        const store = createStore({ changeUserRole: () => throwError(() => refusal) });
        await stable();
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.changeRole(ADMINISTRATOR.id, { role: Role.teacher });

        //then
        expect(changed).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(refusal);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(store.isMutating()).toBe(false);
    });

    it('sets the Temporary Password and confirms it', async () => {
        //given
        const store = createStore({ findUsers: () => of([ADMINISTRATOR, TEACHER_USER]) });
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const set = await store.setTemporaryPassword(TEACHER_USER.id, TEMPORARY_PASSWORD);

        //then
        expect(set).toBe(true);
        expect(api.setUserTemporaryPassword).toHaveBeenCalledWith('user-levi', TEMPORARY_PASSWORD);
        expect(toast.success).toHaveBeenCalledWith('users.temporaryPasswordSet');
        expect(store.isMutating()).toBe(false);
    });

    it('keeps a Set Temporary Password refusal for the dialog', async () => {
        //given
        const refusal = conflict('userAlreadyDeleted');
        const store = createStore({ setUserTemporaryPassword: () => throwError(() => refusal) });
        await stable();
        const toast = TestBed.inject(ToastService);

        //when
        const set = await store.setTemporaryPassword(TEACHER_USER.id, TEMPORARY_PASSWORD);

        //then
        expect(set).toBe(false);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(store.isMutating()).toBe(false);
    });

    it('starts a new command without the previous refusal', async () => {
        //given
        let fail = true;
        const store = createStore({
            changeUserRole: () => (fail ? throwError(() => conflict('userAlreadyHasRole')) : of(undefined)),
        });
        await stable();
        await store.changeRole(TEACHER_USER.id, TO_ADMINISTRATOR);
        fail = false;

        //when
        const changed = await store.changeRole(TEACHER_USER.id, TO_ADMINISTRATOR);

        //then
        expect(changed).toBe(true);
        expect(store.refusal()).toBeNull();
    });
});
```

- [ ] **Step 3: Run the spec and watch it fail**

Run the command above. Expected: compile errors for the missing store members (`refusal`, `clearRefusal`, `changeDetails`, `changeRole`, `setTemporaryPassword`).

- [ ] **Step 4: Add the API methods**

In `client\src\app\features\users\data\users-api.service.ts`, add the three imports in alphabetical order with the other `./` imports:

```ts
import { ChangeUserDetailsRequest } from './change-user-details.request';
import { ChangeUserRoleRequest } from './change-user-role.request';
import { SetUserTemporaryPasswordRequest } from './set-user-temporary-password.request';
```

Then add these methods after `restoreUser`:

```ts
    changeUserDetails(userId: string, request: ChangeUserDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/details`, request);
    }

    changeUserRole(userId: string, request: ChangeUserRoleRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/role`, request);
    }

    setUserTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/temporary-password`, request);
    }
```

- [ ] **Step 5: Update the store**

In `client\src\app\features\users\state\users.store.ts`:

1. Add `Observable` to the `rxjs` import: `import { Observable, firstValueFrom } from 'rxjs';`.
2. Add the three request imports next to `CreateUserRequest`:

```ts
import { ChangeUserDetailsRequest } from '../data/change-user-details.request';
import { ChangeUserRoleRequest } from '../data/change-user-role.request';
import { SetUserTemporaryPasswordRequest } from '../data/set-user-temporary-password.request';
```

3. Rename the refusal signals. The public name becomes `refusal`, so the private field, which is already called `refusal`, becomes `refusalMessage`. Replace `private readonly refusal = signal<string | null>(null);` with:

```ts
    private readonly refusalMessage = signal<string | null>(null);
```

and replace `readonly deleteRefusal = this.refusal.asReadonly();` with:

```ts
    readonly refusal = this.refusalMessage.asReadonly();
```

4. Replace `clearDeleteRefusal()` and `delete()` with the block below. It also adds the three new commands and the shared helper:

```ts
    clearRefusal(): void {
        this.refusalMessage.set(null);
    }

    delete(userId: string): Promise<boolean> {
        return this.runDialogCommand(() => this.api.deleteUser(userId), 'users.deleted');
    }

    changeDetails(userId: string, request: ChangeUserDetailsRequest): Promise<boolean> {
        return this.runDialogCommand(() => this.api.changeUserDetails(userId, request), 'users.detailsChanged');
    }

    changeRole(userId: string, request: ChangeUserRoleRequest): Promise<boolean> {
        return this.runDialogCommand(() => this.api.changeUserRole(userId, request), 'users.roleChanged');
    }

    setTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Promise<boolean> {
        return this.runDialogCommand(
            () => this.api.setUserTemporaryPassword(userId, request),
            'users.temporaryPasswordSet',
        );
    }
```

Keep `restore()` where it is. Then add this private method at the end of the class:

```ts
    private async runDialogCommand(command: () => Observable<void>, successKey: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalMessage.set(null);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.usersResource.reload();
            return true;
        } catch (error) {
            this.refusalMessage.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }
```

`firstValueFrom` on a `void` Observable that completes after one emission resolves just as `delete` did before. Setting a Temporary Password also reloads the list: it's cheap, and it keeps one code path for every command.

- [ ] **Step 6: Follow the rename in the page**

In `client\src\app\features\users\ui\pages\users\users.page.ts`, `onDeleteUser`:
- `this.store.clearDeleteRefusal();` becomes `this.store.clearRefusal();`
- `refusal: this.store.deleteRefusal,` becomes `refusal: this.store.refusal,`

`DeleteUserDialogData` keeps its own field names (`refusal`, `isDeleting`, `confirm`), so `delete-user.dialog.ts` needs no change. Search the client for `deleteRefusal` and `clearDeleteRefusal`; after this step, nothing should match:

```bash
grep -rn "deleteRefusal\|clearDeleteRefusal" client/src
```

- [ ] **Step 7: Run the spec and watch it pass, then run the whole client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: everything is green and the build succeeds. The success keys `users.detailsChanged`, `users.roleChanged` and `users.temporaryPasswordSet` are only used through the mocked `ToastService` here; task 5 adds them to the translation files.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/users
git commit -m "feat(client): Users store edits details and Role and sets a Temporary Password (#87)"
```

End the commit message with the attribution trailer from the session's instructions.
