# Task 2 of 8: Client — translated "too many attempts" on 429

> Part of [#74: Production Deployment](README.md). Requires task 1 committed. Work on branch `74-production-deployment`.

**Why:** Decision 9. After task 1, a sixth sign-in within a minute (or a 31st identify from one IP) answers `429`. Today the login form would claim "Incorrect email or password" and the identify step would fall into its generic `failed` state ("check your connection") — both wrong and both make people retry immediately. Each screen gets its own translated "wait a minute" message.

**Files:**
- Create: `client/src/app/features/auth/sign-in-error.ts`
- Create: `client/src/app/features/auth/sign-in-error.spec.ts`
- Modify: `client/src/app/features/auth/login.component.ts`
- Modify: `client/src/app/features/auth/login.component.html`
- Modify: `client/src/app/features/student-form/domain/identify-status.enum.ts`
- Modify: `client/src/app/features/student-form/state/student-form.store.ts`
- Modify: `client/src/app/features/student-form/state/student-form.store.spec.ts`
- Modify: `client/src/app/features/student-form/ui/components/identify-step/identify-step.component.ts`
- Modify: `client/src/app/features/student-form/ui/components/identify-step/identify-step.component.html`
- Modify: `client/public/i18n/en.json`, `client/public/i18n/he.json`

**Interfaces:**
- Consumes: task 1 — `POST /api/auth/login` and `POST api/submissions/by-link/{token}/identify` answer HTTP `429` when limited.
- Produces: `signInErrorKeyOf(error: unknown): string`; `IdentifyStatus.tooManyAttempts = 'tooManyAttempts'`; translation keys `auth.tooManyAttempts` and `studentForm.identify.tooManyAttempts`.

**Commands** (this machine's default `npm` is broken for installs — project memory). From `client/` in bash; if `node_modules` is missing, install first with nvm's v22.6.0 npm:

```bash
cd client
"/c/Users/AdiSilagy/AppData/Roaming/nvm/v22.6.0/node.exe" "/c/Users/AdiSilagy/AppData/Roaming/nvm/v22.6.0/node_modules/npm/bin/npm-cli.js" ci   # only if node_modules is missing
NG='"/c/Program Files/nodejs/node.exe" node_modules/@angular/cli/bin/ng.js'
eval $NG test --watch=false                       # all client tests
eval $NG build --project client                   # production build
```

The client files use **4-space** indentation except `login.component.ts`/`.html`, which use **2** — keep each file's own style.

- [ ] **Step 1: Failing test — the sign-in error key**

Create `client/src/app/features/auth/sign-in-error.spec.ts`:

```ts
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signInErrorKeyOf } from './sign-in-error';

describe('signInErrorKeyOf', () => {
    it('asks the admin to wait after too many attempts', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.tooManyAttempts');
    });

    it('reports wrong credentials when the sign-in is refused', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.Unauthorized });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.invalidCredentials');
    });
});
```

- [ ] **Step 2: Failing test — the identify step maps 429**

In `client/src/app/features/student-form/state/student-form.store.spec.ts`:

1. Change the imports at the top: add `HttpErrorResponse, HttpStatusCode` from `@angular/common/http`, add `Observable, throwError` next to `of` from `rxjs`, import `IdentifyStudentResponse` next to `SlotForIdentifyStudentResponse` from `'../data/identify-student.response'`, and import `IdentifyStatus` from `'../domain/identify-status.enum'`.

2. Replace the whole `identifiedStore` function with these three functions:

```ts
const ROSTER_STUDENT: IdentifyStudentResponse = {
    studentName: 'Test Student',
    teacherName: 'Teacher Cohen',
    carName: 'Corolla White',
    transmission: Transmission.automatic,
    submission: null,
    slots: SLOTS,
};

async function storeAfterIdentify(identifyStudent: () => Observable<IdentifyStudentResponse>): Promise<StudentFormStore> {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            StudentFormStore,
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: SubmissionsApiService,
                useValue: {
                    getPublicationByLink: () =>
                        of({
                            weekStart: '2026-11-15',
                            weekNumber: 47,
                            state: PublicationState.open,
                            windowStartUtc: '2026-11-11T16:00:00Z',
                            windowEndUtc: '2026-11-13T12:00:00Z',
                        }),
                    identifyStudent,
                },
            },
        ],
    });
    const store = TestBed.inject(StudentFormStore);
    store.open(LINK_TOKEN);
    store.changeNationalId(ROSTER_NATIONAL_ID);
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

function identifiedStore(): Promise<StudentFormStore> {
    return storeAfterIdentify(() => of(ROSTER_STUDENT));
}
```

(Every existing test keeps calling `identifiedStore()` unchanged.)

3. Add a new `describe` block as the **first** child of `describe('StudentFormStore', ...)`:

```ts
    describe('identifyStatus', () => {
        it('maps a 429 to tooManyAttempts', async () => {
            //given
            const rateLimited = new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests });

            //when
            const store = await storeAfterIdentify(() => throwError(() => rateLimited));

            //then
            expect(store.identifyStatus()).toBe(IdentifyStatus.tooManyAttempts);
        });

        it('still reports a roster miss as notOnRoster', async () => {
            //given
            const missing = new HttpErrorResponse({ status: HttpStatusCode.NotFound });

            //when
            const store = await storeAfterIdentify(() => throwError(() => missing));

            //then
            expect(store.identifyStatus()).toBe(IdentifyStatus.notOnRoster);
        });
    });
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
eval $NG test --watch=false --include "src/app/features/auth/**/*.spec.ts" --include "src/app/features/student-form/state/**/*.spec.ts"
```

Expected: FAIL — `sign-in-error` cannot be resolved, and `IdentifyStatus.tooManyAttempts` does not exist (`maps a 429 to tooManyAttempts` fails; `still reports a roster miss as notOnRoster` already passes).

- [ ] **Step 4: The sign-in error key**

Create `client/src/app/features/auth/sign-in-error.ts`:

```ts
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

const TOO_MANY_ATTEMPTS_KEY = 'auth.tooManyAttempts';
const INVALID_CREDENTIALS_KEY = 'auth.invalidCredentials';

export function signInErrorKeyOf(error: unknown): string {
    const isRateLimited = error instanceof HttpErrorResponse && error.status === HttpStatusCode.TooManyRequests;

    return isRateLimited ? TOO_MANY_ATTEMPTS_KEY : INVALID_CREDENTIALS_KEY;
}
```

In `client/src/app/features/auth/login.component.ts`:
- add `import { signInErrorKeyOf } from './sign-in-error';` after the `AuthService` import;
- replace `protected readonly error = signal(false);` with `protected readonly errorKey = signal<string | null>(null);`
- in `submit()`, replace `this.error.set(false);` with `this.errorKey.set(null);` and the `error` callback with:

```ts
      error: (error: unknown) => {
        this.loading.set(false);
        this.errorKey.set(signInErrorKeyOf(error));
      },
```

In `client/src/app/features/auth/login.component.html`, replace the `@if (error()) { ... }` block with:

```html
    @if (errorKey(); as key) {
      <p-message severity="error" [textContent]="key | transloco" styleClass="login__error" />
    }
```

- [ ] **Step 5: The identify status**

In `client/src/app/features/student-form/domain/identify-status.enum.ts`, add the member after `invalidId`:

```ts
    tooManyAttempts = 'tooManyAttempts',
```

In `client/src/app/features/student-form/state/student-form.store.ts`:
- after `const HTTP_CONFLICT = 409;` add `const HTTP_TOO_MANY_REQUESTS = 429;`
- widen the result type: `| { status: IdentifyStatus.notOnRoster | IdentifyStatus.invalidId | IdentifyStatus.tooManyAttempts };`
- in `identify(...)`'s `catch`, after the `HTTP_CONFLICT` branch and before `throw error;`, add:

```ts
            if (isStatus(error, HTTP_TOO_MANY_REQUESTS)) {
                return { status: IdentifyStatus.tooManyAttempts };
            }
```

In `client/src/app/features/student-form/ui/components/identify-step/identify-step.component.ts`, let the student retry after waiting — replace the `RESUBMITTABLE_STATUSES` line with:

```ts
const RESUBMITTABLE_STATUSES: ReadonlySet<IdentifyStatus> = new Set([
    IdentifyStatus.found,
    IdentifyStatus.failed,
    IdentifyStatus.tooManyAttempts,
]);
```

(A retry with the same digits goes through `requestLookup` → `identifyResource.reload()`, already on `main`.)

In `identify-step.component.html`, add a case after the `statuses.failed` case, inside the `@switch`:

```html
                @case (statuses.tooManyAttempts) {
                    <p-message severity="warn" class="identify__too-many">
                        {{ 'studentForm.identify.tooManyAttempts' | transloco }}
                    </p-message>
                }
```

- [ ] **Step 6: Translations**

`client/public/i18n/en.json` — in `auth`, after `"invalidCredentials": "Incorrect email or password",` add:

```json
    "tooManyAttempts": "Too many sign-in attempts. Wait a minute and try again.",
```

and in `studentForm.identify`, after the `"failed": "We couldn't check your ID right now. ..."` line add:

```json
      "tooManyAttempts": "Too many attempts from this network. Wait a minute and try again.",
```

`client/public/i18n/he.json` — the same two keys in the same places:

```json
    "tooManyAttempts": "יותר מדי ניסיונות התחברות. המתינו דקה ונסו שוב.",
```

```json
      "tooManyAttempts": "יותר מדי ניסיונות מהרשת הזו. המתינו דקה ונסו שוב.",
```

Plain `-` and `.` only — `translations.spec.ts` fails on en dash, em dash or the ellipsis character. Keep both files valid JSON (watch the trailing commas).

- [ ] **Step 7: Run all client tests and the build**

```bash
eval $NG test --watch=false
eval $NG build --project client
```

Expected: every test passes (including `translations.spec.ts` and `source-text.spec.ts`); the build succeeds.

- [ ] **Step 8: See it in the browser**

From the repo root: compose Postgres up, `preview_start {name:"api"}`, `preview_start {name:"client"}`.

1. Open `http://localhost:4200/login`, sign in with a wrong password six times within a minute → the sixth shows "Too many sign-in attempts. Wait a minute and try again." Toggle to עב → the Hebrew sentence, right-aligned.
2. Wait a minute, sign in with the dev admin (`running-the-project.md`) → works.

Verify with `read_page` / `get_page_text` (screenshots are flaky on this app — project memory). Stop both servers.

- [ ] **Step 9: Commit**

```bash
cd /v/DrivingLessonsBooking
git add client/src/app/features/auth \
        client/src/app/features/student-form/domain/identify-status.enum.ts \
        client/src/app/features/student-form/state/student-form.store.ts \
        client/src/app/features/student-form/state/student-form.store.spec.ts \
        client/src/app/features/student-form/ui/components/identify-step \
        client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): tell the admin and the student to wait a minute after too many attempts

- login: a 429 shows auth.tooManyAttempts instead of wrong credentials
- identify: a 429 becomes IdentifyStatus.tooManyAttempts with a retry button

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
