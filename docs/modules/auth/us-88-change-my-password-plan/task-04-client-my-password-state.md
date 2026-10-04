# Task 4 of 6: Client state for Change my password (specs first)

> Part of [#88: Change My Own Password](README.md). Requires task 3 committed. Work on branch `88-change-my-password`.

**Files:**
- Modify: `client\src\app\core\auth.service.ts`
- Test: `client\src\app\core\auth.service.spec.ts`
- Create: `client\src\app\features\users\data\change-my-password.request.ts`
- Create: `client\src\app\features\users\data\change-my-password.response.ts`
- Create: `client\src\app\features\users\data\me-api.service.ts`
- Create: `client\src\app\features\users\domain\passwords-match.ts`
- Test: `client\src\app\features\users\domain\passwords-match.spec.ts` (new)
- Create: `client\src\app\features\users\state\my-password.store.ts`
- Test: `client\src\app\features\users\state\my-password.store.spec.ts` (new)

**Interfaces:**
- Consumes:
  - From task 3: `PUT api/me/password` with `{ currentPassword, newPassword }` → `200 { accessToken, expiresAtUtc }`; 409 with `code` `userCurrentPasswordMustBeCorrect` or `passwordMustNotBeEmpty`.
  - Existing: `AuthService` (`token` signal, `localStorage['auth_token']`), `ToastService.success(key, detail?)` / `messageOf(error)`.
- Produces (task 5's dialog and shell rely on these):
  - `AuthService.useToken(accessToken: string): void`. Stores the token and updates the `token` signal. `login()` now uses it too.
  - `ChangeMyPasswordRequest { currentPassword: string; newPassword: string }` and `ChangeMyPasswordResponse { accessToken: string; expiresAtUtc: string }`, named like the backend DTOs.
  - `MeApiService.changeMyPassword(request): Observable<ChangeMyPasswordResponse>`, `providedIn: 'root'`.
  - `passwordsMatch(newPassword: string, confirmation: string): boolean`, an exact comparison.
  - `MyPasswordStore` (`@Injectable()`, not root, provided by the dialog): `refusal: Signal<string | null>`, `isSaving: Signal<boolean>`, `change(request: ChangeMyPasswordRequest): Promise<boolean>`.
  - Translation keys used here, added in task 5: `myPassword.changed`, `myPassword.changedDetail`.

**Why:**
- #88 AC 5: "After a successful change the User is not left with a dead session". The store swaps in the fresh token before anything else can send a request with the old one.
- README decisions 1, 7, 8 and 9; Review Focus 1, 2 and 5.

- [ ] **Step 1: Write the failing `AuthService` spec**

In `client\src\app\core\auth.service.spec.ts`, add this constant after `const TOKEN = ...`:

```typescript
const FRESH_PAYLOAD = 'eyJzdWIiOiJ1c2VyLXlhZWwiLCJlbWFpbCI6InlhZWxAc2Nob29sLmV4YW1wbGUifQ';
const FRESH_TOKEN = `header.${FRESH_PAYLOAD}.signature`;
```

`FRESH_PAYLOAD` is the base64url of `{"sub":"user-yael","email":"yael@school.example"}`.

Then add this test as the last one inside `describe('AuthService', ...)`:

```typescript
    it('uses a fresh token for the next requests and keeps it across reloads', () => {
        //given
        const auth = createService(TOKEN);

        //when
        auth.useToken(FRESH_TOKEN);

        //then
        expect(auth.token()).toBe(FRESH_TOKEN);
        expect(localStorage.getItem(TOKEN_KEY)).toBe(FRESH_TOKEN);
        expect(auth.userId()).toBe('user-yael');
        expect(auth.isAuthenticated()).toBe(true);
    });
```

- [ ] **Step 2: Write the failing `passwordsMatch` spec**

Create `client\src\app\features\users\domain\passwords-match.spec.ts`:

```typescript
import { passwordsMatch } from './passwords-match';

describe('passwords match', () => {
    it('matches the same password typed twice', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2027')).toBe(true);
    });

    it('does not match a different confirmation', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2026')).toBe(false);
    });

    it('does not match when only the case differs', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'fresh#2027')).toBe(false);
    });

    it('does not match when only surrounding spaces differ, because the password is stored as typed', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2027 ')).toBe(false);
    });
});
```

- [ ] **Step 3: Write the failing `MyPasswordStore` spec**

Create `client\src\app\features\users\state\my-password.store.spec.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ChangeMyPasswordRequest } from '../data/change-my-password.request';
import { ChangeMyPasswordResponse } from '../data/change-my-password.response';
import { MeApiService } from '../data/me-api.service';
import { MyPasswordStore } from './my-password.store';

const HTTP_CONFLICT = 409;

const REQUEST: ChangeMyPasswordRequest = { currentPassword: 'Current#2026', newPassword: 'Fresh#2027' };

const RESPONSE: ChangeMyPasswordResponse = {
    accessToken: 'fresh-access-token',
    expiresAtUtc: '2026-10-04T22:00:00Z',
};

const WRONG_CURRENT_PASSWORD = new HttpErrorResponse({
    status: HTTP_CONFLICT,
    error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userCurrentPasswordMustBeCorrect' },
});

function createStore(changeMyPassword: () => Observable<ChangeMyPasswordResponse>): MyPasswordStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            MyPasswordStore,
            { provide: MeApiService, useValue: { changeMyPassword: vi.fn(changeMyPassword) } },
            {
                provide: ToastService,
                useValue: { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') },
            },
            { provide: AuthService, useValue: { useToken: vi.fn(), logout: vi.fn() } },
        ],
    });

    return TestBed.inject(MyPasswordStore);
}

describe('MyPasswordStore', () => {
    it('changes the password, keeps the User signed in with the fresh token and confirms it', async () => {
        //given
        const store = createStore(() => of(RESPONSE));
        const api = TestBed.inject(MeApiService);
        const auth = TestBed.inject(AuthService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.change(REQUEST);

        //then
        expect(changed).toBe(true);
        expect(api.changeMyPassword).toHaveBeenCalledWith(REQUEST);
        expect(auth.useToken).toHaveBeenCalledWith('fresh-access-token');
        expect(toast.success).toHaveBeenCalledWith('myPassword.changed', { key: 'myPassword.changedDetail' });
        expect(store.refusal()).toBeNull();
        expect(store.isSaving()).toBe(false);
    });

    it('keeps the refusal and the current token when the server refuses', async () => {
        //given
        const store = createStore(() => throwError(() => WRONG_CURRENT_PASSWORD));
        const auth = TestBed.inject(AuthService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.change(REQUEST);

        //then
        expect(changed).toBe(false);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.messageOf).toHaveBeenCalledWith(WRONG_CURRENT_PASSWORD);
        expect(toast.success).not.toHaveBeenCalled();
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(auth.useToken).not.toHaveBeenCalled();
        expect(auth.logout).not.toHaveBeenCalled();
        expect(store.isSaving()).toBe(false);
    });

    it('is saving while the change is in flight', () => {
        //given
        const store = createStore(() => NEVER);

        //when
        void store.change(REQUEST);

        //then
        expect(store.isSaving()).toBe(true);
    });

    it('starts a new change without the previous refusal', async () => {
        //given
        let calls = 0;
        const store = createStore(() => {
            calls++;
            return calls === 1 ? throwError(() => WRONG_CURRENT_PASSWORD) : NEVER;
        });
        await store.change(REQUEST);

        //when
        void store.change(REQUEST);

        //then
        expect(store.refusal()).toBeNull();
        expect(store.isSaving()).toBe(true);
    });
});
```

- [ ] **Step 4: Run the client specs and see them fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: the run fails to compile the new specs. `useToken` doesn't exist on `AuthService`, and `./passwords-match`, `../data/change-my-password.request`, `../data/me-api.service` and `./my-password.store` can't be resolved.

- [ ] **Step 5: Add `AuthService.useToken`**

In `client\src\app\core\auth.service.ts`, replace the `login` method with:

```typescript
  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.useToken(response.accessToken)));
  }

  useToken(accessToken: string): void {
    localStorage.setItem(TOKEN_KEY, accessToken);
    this.token.set(accessToken);
  }
```

Keep `logout()` as it is. The file uses 2-space indentation; keep it.

- [ ] **Step 6: Add the DTOs and the API service**

Create `client\src\app\features\users\data\change-my-password.request.ts`:

```typescript
export interface ChangeMyPasswordRequest {
    currentPassword: string;
    newPassword: string;
}
```

Create `client\src\app\features\users\data\change-my-password.response.ts`:

```typescript
export interface ChangeMyPasswordResponse {
    accessToken: string;
    expiresAtUtc: string;
}
```

Create `client\src\app\features\users\data\me-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeMyPasswordRequest } from './change-my-password.request';
import { ChangeMyPasswordResponse } from './change-my-password.response';

@Injectable({ providedIn: 'root' })
export class MeApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/me';

    changeMyPassword(request: ChangeMyPasswordRequest): Observable<ChangeMyPasswordResponse> {
        return this.http.put<ChangeMyPasswordResponse>(`${this.baseUrl}/password`, request);
    }
}
```

- [ ] **Step 7: Add `passwordsMatch`**

Create `client\src\app\features\users\domain\passwords-match.ts`:

```typescript
export function passwordsMatch(newPassword: string, confirmation: string): boolean {
    return newPassword === confirmation;
}
```

- [ ] **Step 8: Add `MyPasswordStore`**

Create `client\src\app\features\users\state\my-password.store.ts`:

```typescript
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ChangeMyPasswordRequest } from '../data/change-my-password.request';
import { MeApiService } from '../data/me-api.service';

@Injectable()
export class MyPasswordStore {
    private readonly api = inject(MeApiService);
    private readonly auth = inject(AuthService);
    private readonly toast = inject(ToastService);

    private readonly saving = signal(false);
    private readonly refusalMessage = signal<string | null>(null);

    readonly isSaving = this.saving.asReadonly();
    readonly refusal = this.refusalMessage.asReadonly();

    async change(request: ChangeMyPasswordRequest): Promise<boolean> {
        this.saving.set(true);
        this.refusalMessage.set(null);

        try {
            const response = await firstValueFrom(this.api.changeMyPassword(request));
            this.auth.useToken(response.accessToken);
            this.toast.success('myPassword.changed', { key: 'myPassword.changedDetail' });
            return true;
        } catch (error) {
            this.refusalMessage.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.saving.set(false);
        }
    }
}
```

The token is swapped first, inside the same `try`, so no request goes out with the dead token after a successful change (Review Focus 2).

- [ ] **Step 9: Run the client specs and see them pass**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: every spec passes, including the 1 new `AuthService` case, the 4 `passwords match` cases and the 4 `MyPasswordStore` cases. `source-text.spec.ts` is green too.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/core client/src/app/features/users
git commit -m "feat(client): change my password and keep the fresh token, with the refusal kept for the dialog (#88)"
```

End the commit message with the attribution trailer from the session's instructions.
