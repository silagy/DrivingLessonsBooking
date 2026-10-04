# Task 5 of 7: Client state for Delete, Restore and "You" (specs first)

> Part of [#86: Delete and Restore Users](README.md). Requires task 4 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Modify: `client\src\app\core\auth.service.ts` (adds `userId`, decodes base64url)
- Create: `client\src\app\core\auth.service.spec.ts`
- Modify: `client\src\app\core\services\toast.service.ts` (`success` detail, public `messageOf`)
- Modify: `client\src\app\core\services\toast.service.spec.ts`
- Modify: `client\src\app\features\users\data\users-api.service.ts`
- Modify: `client\src\app\features\users\state\users.store.ts`
- Modify: `client\src\app\features\users\state\users.store.spec.ts`

**Interfaces:**
- Consumes:
  - From task 3: `DELETE api/users/{id}` → 204 and `POST api/users/{id}/restore` → 204, with 409 problems carrying `code`.
  - Existing: `User` (`id`, `name`, `isDeleted`, ...), `UsersStore` (`usersResource`, `mutating`, `toast`), and `ToastService.resolveMessage` (private today).
- Produces (task 6 uses all of these):
  - `AuthService.userId: Signal<string | null>`, the token's `sub`.
  - `ToastService.success(key: string, detail?: ToastDetail): void`, with `export interface ToastDetail { key: string; params?: Record<string, string> }`.
  - `ToastService.messageOf(error: unknown): string`: the same translated message `apiError` shows.
  - `UsersApiService.deleteUser(userId: string): Observable<void>` and `restoreUser(userId: string): Observable<void>`.
  - `UsersStore`:
    - `currentUserId: Signal<string | null>`
    - `deleteRefusal: Signal<string | null>`
    - `clearDeleteRefusal(): void`
    - `delete(userId: string): Promise<boolean>`: true when deleted.
    - `restore(user: User): Promise<void>`
  - Translation keys used here and added in task 6: `users.deleted`, `users.restored`, `users.restoredDetail` (`{{name}}`).

**Why:**
- #86 AC 6: the Users screen offers Delete and Restore and shows 409 rule violations translated.
- README decisions 2, 8 and 12; Review Focus 4.
- `AuthService` decodes the JWT payload with plain `atob`. A real token is base64url, so `-` / `_` in the payload make `email` (and the new `userId`) silently `null`. The new spec pins the fix.

**Run the tests** (from `client\`, PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

- [ ] **Step 1: Write the failing `AuthService` spec**

`client\src\app\core\auth.service.spec.ts`. The payload segment below is the base64url of `{"sub":"user-owner","email":"owner@school.example","name":"~~~?"}`. It deliberately contains `-` and `_`.

```ts
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

const TOKEN_KEY = 'auth_token';
const BASE64URL_PAYLOAD = 'eyJzdWIiOiJ1c2VyLW93bmVyIiwiZW1haWwiOiJvd25lckBzY2hvb2wuZXhhbXBsZSIsIm5hbWUiOiJ-fn4_In0';
const TOKEN = `header.${BASE64URL_PAYLOAD}.signature`;

function createService(token: string | null): AuthService {
    if (token) {
        localStorage.setItem(TOKEN_KEY, token);
    } else {
        localStorage.removeItem(TOKEN_KEY);
    }

    TestBed.configureTestingModule({
        providers: [provideZonelessChangeDetection(), provideHttpClient(), provideRouter([])],
    });

    return TestBed.inject(AuthService);
}

describe('AuthService', () => {
    afterEach(() => localStorage.removeItem(TOKEN_KEY));

    it('reads the signed-in User id from the token', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.userId()).toBe('user-owner');
    });

    it('reads the email from a base64url token payload', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.email()).toBe('owner@school.example');
    });

    it('has no User id when signed out', () => {
        //given
        const auth = createService(null);

        //expected
        expect(auth.userId()).toBeNull();
    });

    it('has no User id when the token is not a JWT', () => {
        //given
        const auth = createService('not-a-token');

        //expected
        expect(auth.userId()).toBeNull();
    });
});
```

- [ ] **Step 2: Write the failing `ToastService` cases**

In `client\src\app\core\services\toast.service.spec.ts`:

1. Add to `EN`, next to `errors` and `general`:

```ts
    users: {
        restored: 'User restored',
        restoredDetail: '{{name}} can sign in again.',
    },
```

2. Add two new `describe` blocks after the `apiError` block, inside `describe('ToastService', ...)`:

```ts
    describe('success', () => {
        it('shows the confirmation alone', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.success('users.restored');

            //then
            expect(add).toHaveBeenCalledWith({ severity: 'success', summary: 'User restored' });
        });

        it('adds a detail line with its values filled in', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.success('users.restored', { key: 'users.restoredDetail', params: { name: 'Gil Nahum' } });

            //then
            expect(add).toHaveBeenCalledWith({
                severity: 'success',
                summary: 'User restored',
                detail: 'Gil Nahum can sign in again.',
            });
        });
    });

    describe('messageOf', () => {
        it('gives the same translated rule message without showing a toast', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'carNameMustNotBeEmpty',
            });

            //when
            const message = toast.messageOf(error);

            //then
            expect(message).toBe('Enter the car\'s name.');
            expect(add).not.toHaveBeenCalled();
        });
    });
```

- [ ] **Step 3: Write the failing store cases**

In `client\src\app\features\users\state\users.store.spec.ts`:

1. Add imports: `import { AuthService } from '../../../core/auth.service';` and `import { signal } from '@angular/core';` (merge it into the existing `@angular/core` import).
2. Add a deleted fixture after `TEACHER_USER`:

```ts
const DELETED_USER: ItemForFindUsersResponse = {
    id: 'user-nahum',
    name: 'Gil Nahum',
    signInEmail: 'gil@school.example',
    role: Role.teacher,
    teacherId: 'teacher-nahum',
    teacherName: 'Gil Nahum',
    isDeleted: true,
};
```

3. Replace `createStore` with this version. The new parameters have defaults, so the existing calls compile unchanged.

```ts
function createStore(
    findUsers: () => Observable<ItemForFindUsersResponse[]>,
    createUser: () => Observable<CreateUserResponse>,
    deleteUser: () => Observable<void> = () => of(undefined),
    restoreUser: () => Observable<void> = () => of(undefined),
): UsersStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            UsersStore,
            {
                provide: UsersApiService,
                useValue: {
                    findUsers: vi.fn(findUsers),
                    createUser: vi.fn(createUser),
                    deleteUser: vi.fn(deleteUser),
                    restoreUser: vi.fn(restoreUser),
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
```

4. Add a `SELF_REFUSAL` constant after `REQUEST`:

```ts
const SELF_REFUSAL = new HttpErrorResponse({
    status: HTTP_CONFLICT,
    error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userMustNotDeleteSelf' },
});
```

5. Append these cases inside `describe('UsersStore', ...)`:

```ts
    it('knows which User is signed in', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);

        //expected
        expect(store.currentUserId()).toBe('user-owner');
    });

    it('deletes the User, confirms it and reloads the list', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(TEACHER_USER.id);

        //then
        expect(deleted).toBe(true);
        expect(api.deleteUser).toHaveBeenCalledWith('user-levi');
        expect(toast.success).toHaveBeenCalledWith('users.deleted');
        expect(store.deleteRefusal()).toBeNull();
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('keeps the refusal for the Delete dialog instead of a toast', async () => {
        //given
        const store = createStore(
            () => of([ADMINISTRATOR]),
            () => NEVER,
            () => throwError(() => SELF_REFUSAL),
        );
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(ADMINISTRATOR.id);

        //then
        expect(deleted).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(SELF_REFUSAL);
        expect(store.deleteRefusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('clears the refusal so a reopened Delete dialog starts clean', async () => {
        //given
        const store = createStore(
            () => of([ADMINISTRATOR]),
            () => NEVER,
            () => throwError(() => SELF_REFUSAL),
        );
        await stable();
        await store.delete(ADMINISTRATOR.id);

        //when
        store.clearDeleteRefusal();

        //then
        expect(store.deleteRefusal()).toBeNull();
    });

    it('restores the User, names them in the confirmation and reloads the list', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, DELETED_USER]), () => NEVER);
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.restore(DELETED_USER);

        //then
        expect(api.restoreUser).toHaveBeenCalledWith('user-nahum');
        expect(toast.success).toHaveBeenCalledWith('users.restored', {
            key: 'users.restoredDetail',
            params: { name: 'Gil Nahum' },
        });
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('shows a Restore refusal as a toast and keeps the list', async () => {
        //given
        const refusal = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userAlreadyActive' },
        });
        const store = createStore(
            () => of([ADMINISTRATOR, DELETED_USER]),
            () => NEVER,
            () => of(undefined),
            () => throwError(() => refusal),
        );
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
```

- [ ] **Step 4: Run the specs and watch them fail**

Run the command above. Expected: compile errors or failures in `auth.service.spec.ts`, `toast.service.spec.ts` and `users.store.spec.ts`, because `userId`, `messageOf`, the `success` detail, `deleteUser`/`restoreUser` and the store members don't exist. The base64url email case fails even before compiling the rest.

- [ ] **Step 5: Implement `AuthService.userId`**

In `client\src\app\core\auth.service.ts`, replace `readEmailClaim` with:

```ts
type TokenClaim = 'email' | 'sub';

const BASE64_BLOCK = 4;

function decodePayload(token: string): Record<string, unknown> {
  const segment = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  const padded = segment.padEnd(Math.ceil(segment.length / BASE64_BLOCK) * BASE64_BLOCK, '=');

  return JSON.parse(atob(padded)) as Record<string, unknown>;
}

function readClaim(token: string | null, claim: TokenClaim): string | null {
  if (!token) {
    return null;
  }
  try {
    const value = decodePayload(token)[claim];
    return typeof value === 'string' ? value : null;
  } catch {
    return null;
  }
}
```

Then replace the `email` line and add `userId` after it:

```ts
  readonly email = computed(() => readClaim(this.token(), 'email'));
  readonly userId = computed(() => readClaim(this.token(), 'sub'));
```

The file uses 2-space indentation; keep it.

- [ ] **Step 6: Extend `ToastService`**

In `client\src\app\core\services\toast.service.ts`:

1. Export the detail type above the class:

```ts
export interface ToastDetail {
    key: string;
    params?: Record<string, string>;
}
```

2. Replace `success` and `apiError`, and make `resolveMessage` public as `messageOf`:

```ts
    success(key: string, detail?: ToastDetail): void {
        const summary = this.transloco.translate(key);

        if (!detail) {
            this.messages.add({ severity: 'success', summary });
            return;
        }

        this.messages.add({
            severity: 'success',
            summary,
            detail: this.transloco.translate(detail.key, detail.params),
        });
    }

    apiError(error: unknown): void {
        this.messages.add({ severity: 'error', summary: this.messageOf(error) });
    }

    messageOf(error: unknown): string {
```

Keep the body of the old `resolveMessage` as the body of `messageOf`, and delete the old private method name. `hasTranslation` stays private.

- [ ] **Step 7: Add the API calls**

In `client\src\app\features\users\data\users-api.service.ts`, add after `createUser`:

```ts
    deleteUser(userId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${userId}`);
    }

    restoreUser(userId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${userId}/restore`, {});
    }
```

- [ ] **Step 8: Extend `UsersStore`**

In `client\src\app\features\users\state\users.store.ts`:

1. Add `import { AuthService } from '../../../core/auth.service';`.
2. Add the injection and the refusal signal after `private readonly toast = inject(ToastService);`:

```ts
    private readonly auth = inject(AuthService);
```

and after `private readonly mutating = signal(false);`:

```ts
    private readonly refusal = signal<string | null>(null);
```

3. Add the public signals after `readonly isMutating = this.mutating.asReadonly();`:

```ts
    readonly deleteRefusal = this.refusal.asReadonly();
    readonly currentUserId = computed(() => this.auth.userId());
```

4. Add the methods after `create`:

```ts
    clearDeleteRefusal(): void {
        this.refusal.set(null);
    }

    async delete(userId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusal.set(null);

        try {
            await firstValueFrom(this.api.deleteUser(userId));
            this.toast.success('users.deleted');
            this.usersResource.reload();
            return true;
        } catch (error) {
            this.refusal.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async restore(user: User): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.restoreUser(user.id));
            this.toast.success('users.restored', { key: 'users.restoredDetail', params: { name: user.name } });
            this.usersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
```

- [ ] **Step 9: Run the specs and watch them pass**

Run the command above. Expected: the whole client suite is green, including the existing `UsersStore` and `ToastService` cases. Any other caller of `toast.success(key)` still compiles, because the detail is optional.

- [ ] **Step 10: Commit**

```bash
git add client/src
git commit -m "feat(client): Users store deletes, restores and knows the signed-in User (#86)"
```

End the commit message with the attribution trailer from the session's instructions.
