# Task 2 of 7: The client reads Role and linked Teacher from the token

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires task 1 committed. Work on branch `89-teacher-role-navigation`. Read README decisions 4 and 5 first.

**Files:**
- Move: `client\src\app\features\users\domain\role.enum.ts` → `client\src\app\shared\models\role.enum.ts`
- Modify (imports only): the 13 users-feature files listed in step 1
- Modify: `client\src\app\core\auth.service.ts`
- Test: `client\src\app\core\auth.service.spec.ts`

**Interfaces:**
- Consumes: the token claims from task 1's backend: `role` is `"administrator"` or `"teacher"`; `teacher_id` is a Teacher id string, present only when the User is linked to a Teacher.
- Produces (later tasks rely on these exact names):
  - `client\src\app\shared\models\role.enum.ts`: `export enum Role { administrator = 'administrator', teacher = 'teacher' }` (unchanged values)
  - On `AuthService` (all readonly `Signal`s computed from `token`):
    - `role: Signal<Role | null>`: `null` when signed out, when the token isn't a JWT, or when the claim is not a known Role
    - `teacherId: Signal<string | null>`: the linked Teacher id, `null` when absent
    - `isAdministrator: Signal<boolean>`: `role() === Role.administrator`
    - `isTeacher: Signal<boolean>`: `role() === Role.teacher`

**Why:** AC 4 ("The client auth service parses Role and linked Teacher from the token (spec covers the parsing)").

- [ ] **Step 1: Move the Role enum to `shared\` and fix the imports**

From the repository root:

```bash
git mv client/src/app/features/users/domain/role.enum.ts client/src/app/shared/models/role.enum.ts
```

Then change exactly these import lines (the imported name stays `Role`):

| File (under `client\src\app\features\users\`) | New import line |
|---|---|
| `data\change-user-role.request.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `data\create-user.request.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `data\item-for-find-users.response.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `domain\role-change.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `domain\role-change.spec.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `domain\teacher-link.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `domain\teacher-link.spec.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `domain\user.model.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `state\users.store.spec.ts` | `import { Role } from '../../../shared/models/role.enum';` |
| `ui\components\user-who-card\user-who-card.component.ts` | `import { Role } from '../../../../../shared/models/role.enum';` |
| `ui\dialogs\add-user\add-user.dialog.ts` | `import { Role } from '../../../../../shared/models/role.enum';` |
| `ui\dialogs\change-user-role\change-user-role.dialog.ts` | `import { Role } from '../../../../../shared/models/role.enum';` |
| `ui\pages\users\users.page.ts` | `import { Role } from '../../../../../shared/models/role.enum';` |

Confirm nothing still points at the old path:

```bash
grep -rn "role.enum'" client/src/app
```

Expected: every hit ends in `shared/models/role.enum'`.

- [ ] **Step 2: Write the failing parsing specs**

In `client\src\app\core\auth.service.spec.ts`, add the import and token fixtures below the existing constants (the payloads are base64url JSON, already encoded):

```typescript
import { Role } from '../shared/models/role.enum';
```

```typescript
const ADMINISTRATOR_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLWRhbmkiLCJlbWFpbCI6ImRhbmlAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoiYWRtaW5pc3RyYXRvciJ9';
const TEACHER_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLXlhZWwiLCJlbWFpbCI6InlhZWxAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoidGVhY2hlciIsInRlYWNoZXJfaWQiOiJ0ZWFjaGVyLXlhZWwifQ';
const LINKED_ADMINISTRATOR_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLXJvbml0IiwiZW1haWwiOiJyb25pdEBzY2hvb2wuZXhhbXBsZSIsInJvbGUiOiJhZG1pbmlzdHJhdG9yIiwidGVhY2hlcl9pZCI6InRlYWNoZXItcm9uaXQifQ';
const UNKNOWN_ROLE_PAYLOAD = 'eyJzdWIiOiJ1c2VyLXgiLCJlbWFpbCI6InhAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoic3R1ZGVudCJ9';
const ADMINISTRATOR_TOKEN = `header.${ADMINISTRATOR_PAYLOAD}.signature`;
const TEACHER_TOKEN = `header.${TEACHER_PAYLOAD}.signature`;
const LINKED_ADMINISTRATOR_TOKEN = `header.${LINKED_ADMINISTRATOR_PAYLOAD}.signature`;
const UNKNOWN_ROLE_TOKEN = `header.${UNKNOWN_ROLE_PAYLOAD}.signature`;
```

They decode to:
- Administrator: `{"sub":"user-dani","email":"dani@school.example","role":"administrator"}`
- Teacher: `{"sub":"user-yael","email":"yael@school.example","role":"teacher","teacher_id":"teacher-yael"}`
- Linked Administrator: `{"sub":"user-ronit","email":"ronit@school.example","role":"administrator","teacher_id":"teacher-ronit"}`
- Unknown Role: `{"sub":"user-x","email":"x@school.example","role":"student"}`

Add these specs inside `describe('AuthService', ...)`:

```typescript
    it('reads the Administrator Role from the token', () => {
        //given
        const auth = createService(ADMINISTRATOR_TOKEN);

        //expected
        expect(auth.role()).toBe(Role.administrator);
        expect(auth.isAdministrator()).toBe(true);
        expect(auth.isTeacher()).toBe(false);
        expect(auth.teacherId()).toBeNull();
    });

    it('reads the Teacher Role and the linked Teacher from the token', () => {
        //given
        const auth = createService(TEACHER_TOKEN);

        //expected
        expect(auth.role()).toBe(Role.teacher);
        expect(auth.isTeacher()).toBe(true);
        expect(auth.isAdministrator()).toBe(false);
        expect(auth.teacherId()).toBe('teacher-yael');
    });

    it('reads the linked Teacher of an Administrator who also teaches', () => {
        //given
        const auth = createService(LINKED_ADMINISTRATOR_TOKEN);

        //expected
        expect(auth.isAdministrator()).toBe(true);
        expect(auth.teacherId()).toBe('teacher-ronit');
    });

    it('grants no Role for an unknown role claim', () => {
        //given
        const auth = createService(UNKNOWN_ROLE_TOKEN);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.isAdministrator()).toBe(false);
        expect(auth.isTeacher()).toBe(false);
    });

    it('grants no Role when the token has no role claim', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.teacherId()).toBeNull();
    });

    it('grants no Role when signed out', () => {
        //given
        const auth = createService(null);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.isAdministrator()).toBe(false);
    });

    it('grants no Role when the token is not a JWT', () => {
        //given
        const auth = createService('not-a-token');

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.teacherId()).toBeNull();
    });

    it('takes the Role of a fresh token', () => {
        //given
        const auth = createService(ADMINISTRATOR_TOKEN);

        //when
        auth.useToken(TEACHER_TOKEN);

        //then
        expect(auth.role()).toBe(Role.teacher);
        expect(auth.teacherId()).toBe('teacher-yael');
    });
```

(`TOKEN` is the existing fixture, whose payload has `sub`, `email` and `name` but no `role`.)

- [ ] **Step 3: Run the specs to verify they fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/auth.service.spec.ts
```

Expected: FAIL to compile with `Property 'role' does not exist on type 'AuthService'` (and `teacherId`, `isAdministrator`, `isTeacher`).

- [ ] **Step 4: Parse the claims**

In `client\src\app\core\auth.service.ts`:

Add the import:

```typescript
import { Role } from '../shared/models/role.enum';
```

Widen the claim type:

```typescript
type TokenClaim = 'email' | 'sub' | 'role' | 'teacher_id';
```

Add this function after `readClaim`:

```typescript
function roleOf(claim: string | null): Role | null {
  return Object.values(Role).find((role) => role === claim) ?? null;
}
```

Add the signals after `userId`:

```typescript
  readonly role = computed(() => roleOf(readClaim(this.token(), 'role')));
  readonly teacherId = computed(() => readClaim(this.token(), 'teacher_id'));
  readonly isAdministrator = computed(() => this.role() === Role.administrator);
  readonly isTeacher = computed(() => this.role() === Role.teacher);
```

(This file uses 2-space indentation; keep it.)

- [ ] **Step 5: Run the specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/auth.service.spec.ts
```

Expected: PASS.

Then the whole client suite and build (the enum move touches the users feature):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS, build succeeds.

- [ ] **Step 6: Commit**

```bash
git add client/src/app/shared/models/role.enum.ts client/src/app/features/users client/src/app/core/auth.service.ts client/src/app/core/auth.service.spec.ts
git commit -m "feat(client): read the signed-in User's Role and linked Teacher from the token (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
