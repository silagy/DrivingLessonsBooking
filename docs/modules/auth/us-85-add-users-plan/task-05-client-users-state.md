# Task 5 of 7: Client `users` feature - domain, data and store (specs first)

> Part of [#85: Add Users from a New Users Screen](README.md). Requires tasks 1 to 4 committed (task 3 fixes the API contract). Work on branch `85-add-users-screen`.

**Files:**
- Create: `client\src\app\features\users\domain\role.enum.ts`
- Create: `client\src\app\features\users\domain\user.model.ts`
- Create: `client\src\app\features\users\domain\teacher-option.model.ts`
- Create: `client\src\app\features\users\domain\linkable-teacher.model.ts`
- Create: `client\src\app\features\users\domain\teacher-link.ts`
- Create: `client\src\app\features\users\data\item-for-find-users.response.ts`
- Create: `client\src\app\features\users\data\item-for-find-teachers.response.ts`
- Create: `client\src\app\features\users\data\create-user.request.ts`
- Create: `client\src\app\features\users\data\create-user.response.ts`
- Create: `client\src\app\features\users\data\users-api.service.ts`
- Create: `client\src\app\features\users\data\teacher-options-api.service.ts`
- Create: `client\src\app\features\users\state\users.store.ts`
- Test: `client\src\app\features\users\domain\teacher-link.spec.ts`
- Test: `client\src\app\features\users\state\users.store.spec.ts`

**Interfaces:**
- Consumes: task 3's HTTP contract (`GET api/users/find`, `POST api/users`) and the existing `GET api/teachers/find` (`{ id, name, contactEmail }[]`). Existing `ToastService.success(key)` / `apiError(error)` in `client\src\app\core\services\toast.service.ts`.
- Produces (task 6 uses these names):
  - `enum Role { administrator = 'administrator', teacher = 'teacher' }`
  - `interface User { id: string; name: string; signInEmail: string; role: Role; teacherId: string | null; teacherName: string | null; isDeleted: boolean }`
  - `interface TeacherOption { id: string; name: string }`
  - `interface LinkableTeacher { id: string; name: string; alreadyLinked: boolean }`
  - `isTeacherLinkRequired(role: Role): boolean`
  - `toLinkableTeachers(teachers: TeacherOption[], users: User[]): LinkableTeacher[]`, ordered by name, `alreadyLinked` true for a Teacher linked to any User, deleted or not (README decisions 1 and 10)
  - `interface CreateUserRequest { name: string; signInEmail: string; role: Role; teacherId: string | null; temporaryPassword: string }`
  - `UsersStore` (root): readonly signals `users: Signal<User[]>`, `linkableTeachers: Signal<LinkableTeacher[]>`, `isLoading: Signal<boolean>`, `loadError: Signal<string | null>` (`'users.loadFailed'`), `hasOnlyOneUser: Signal<boolean>`, `isMutating: Signal<boolean>`; method `create(request: CreateUserRequest): Promise<void>` that toasts `'users.created'` and reloads on success, or calls `toast.apiError(error)` on failure

**Why:** #85 acceptance criterion 10 (lazy `users` feature, signals-only store) and the client side of criteria 1 and 2. `client-architecture.md` layers: `domain` has no Angular, `data` is HTTP only, `state` holds the signals. The `users` feature has its own `TeacherOptionsApiService` (README decision 14).

**Run the specs** (from `client\` in PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/users/**/*.spec.ts"
```

- [ ] **Step 1: Write the failing domain spec**

Create `client\src\app\features\users\domain\teacher-link.spec.ts`:

```typescript
import { Role } from './role.enum';
import { isTeacherLinkRequired, toLinkableTeachers } from './teacher-link';
import { TeacherOption } from './teacher-option.model';
import { User } from './user.model';

const DANA: TeacherOption = { id: 'teacher-levi', name: 'Dana Levi' };
const AVI: TeacherOption = { id: 'teacher-cohen', name: 'Avi Cohen' };

function userLinkedTo(teacherId: string | null, isDeleted: boolean): User {
    return {
        id: `user-for-${teacherId}`,
        name: 'Some User',
        signInEmail: 'user@school.example',
        role: Role.teacher,
        teacherId,
        teacherName: null,
        isDeleted,
    };
}

describe('isTeacherLinkRequired', () => {
    it('requires a Teacher for the Teacher Role', () => {
        //expected
        expect(isTeacherLinkRequired(Role.teacher)).toBe(true);
    });

    it('leaves the Teacher optional for an Administrator', () => {
        //expected
        expect(isTeacherLinkRequired(Role.administrator)).toBe(false);
    });
});

describe('toLinkableTeachers', () => {
    it('marks a Teacher that already has a User', () => {
        //given
        const users = [userLinkedTo(DANA.id, false)];

        //when
        const linkable = toLinkableTeachers([DANA], users);

        //then
        expect(linkable).toEqual([{ id: DANA.id, name: DANA.name, alreadyLinked: true }]);
    });

    it('still marks a Teacher whose User is deleted, because the link stays', () => {
        //given
        const users = [userLinkedTo(DANA.id, true)];

        //when
        const linkable = toLinkableTeachers([DANA], users);

        //then
        expect(linkable[0].alreadyLinked).toBe(true);
    });

    it('keeps a Teacher without a User selectable, even next to an unlinked Administrator', () => {
        //given
        const users = [userLinkedTo(null, false)];

        //when
        const linkable = toLinkableTeachers([AVI], users);

        //then
        expect(linkable).toEqual([{ id: AVI.id, name: AVI.name, alreadyLinked: false }]);
    });

    it('orders the Teachers by name', () => {
        //when
        const linkable = toLinkableTeachers([DANA, AVI], []);

        //then
        expect(linkable.map((teacher) => teacher.id)).toEqual([AVI.id, DANA.id]);
    });
});
```

- [ ] **Step 2: Run it and watch it fail**

Run the spec command above. Expected: FAIL, the imported modules don't exist.

- [ ] **Step 3: Add the domain files**

`client\src\app\features\users\domain\role.enum.ts`:

```typescript
export enum Role {
    administrator = 'administrator',
    teacher = 'teacher',
}
```

`client\src\app\features\users\domain\user.model.ts`:

```typescript
import { Role } from './role.enum';

export interface User {
    id: string;
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    teacherName: string | null;
    isDeleted: boolean;
}
```

`client\src\app\features\users\domain\teacher-option.model.ts`:

```typescript
export interface TeacherOption {
    id: string;
    name: string;
}
```

`client\src\app\features\users\domain\linkable-teacher.model.ts`:

```typescript
export interface LinkableTeacher {
    id: string;
    name: string;
    alreadyLinked: boolean;
}
```

`client\src\app\features\users\domain\teacher-link.ts`:

```typescript
import { LinkableTeacher } from './linkable-teacher.model';
import { Role } from './role.enum';
import { TeacherOption } from './teacher-option.model';
import { User } from './user.model';

export function isTeacherLinkRequired(role: Role): boolean {
    return role === Role.teacher;
}

export function toLinkableTeachers(teachers: TeacherOption[], users: User[]): LinkableTeacher[] {
    const linkedTeacherIds = new Set(users.map((user) => user.teacherId));

    return [...teachers]
        .sort((first, second) => first.name.localeCompare(second.name))
        .map((teacher) => ({
            id: teacher.id,
            name: teacher.name,
            alreadyLinked: linkedTeacherIds.has(teacher.id),
        }));
}
```

Run the spec command. Expected: `teacher-link.spec.ts` 6 PASS.

- [ ] **Step 4: Add the data layer**

`client\src\app\features\users\data\item-for-find-users.response.ts`:

```typescript
import { Role } from '../domain/role.enum';

export interface ItemForFindUsersResponse {
    id: string;
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    teacherName: string | null;
    isDeleted: boolean;
}
```

`client\src\app\features\users\data\item-for-find-teachers.response.ts`:

```typescript
export interface ItemForFindTeachersResponse {
    id: string;
    name: string;
    contactEmail: string;
}
```

`client\src\app\features\users\data\create-user.request.ts`:

```typescript
import { Role } from '../domain/role.enum';

export interface CreateUserRequest {
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    temporaryPassword: string;
}
```

`client\src\app\features\users\data\create-user.response.ts`:

```typescript
export interface CreateUserResponse {
    id: string;
}
```

`client\src\app\features\users\data\users-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserRequest } from './create-user.request';
import { CreateUserResponse } from './create-user.response';
import { ItemForFindUsersResponse } from './item-for-find-users.response';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/users';

    findUsers(): Observable<ItemForFindUsersResponse[]> {
        return this.http.get<ItemForFindUsersResponse[]>(`${this.baseUrl}/find`);
    }

    createUser(request: CreateUserRequest): Observable<CreateUserResponse> {
        return this.http.post<CreateUserResponse>(this.baseUrl, request);
    }
}
```

`client\src\app\features\users\data\teacher-options-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ItemForFindTeachersResponse } from './item-for-find-teachers.response';

@Injectable({ providedIn: 'root' })
export class TeacherOptionsApiService {
    private readonly http = inject(HttpClient);

    findTeachers(): Observable<ItemForFindTeachersResponse[]> {
        return this.http.get<ItemForFindTeachersResponse[]>('api/teachers/find');
    }
}
```

- [ ] **Step 5: Write the failing store spec**

Create `client\src\app\features\users\state\users.store.spec.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { CreateUserRequest } from '../data/create-user.request';
import { CreateUserResponse } from '../data/create-user.response';
import { ItemForFindTeachersResponse } from '../data/item-for-find-teachers.response';
import { ItemForFindUsersResponse } from '../data/item-for-find-users.response';
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

function createStore(
    findUsers: () => Observable<ItemForFindUsersResponse[]>,
    createUser: () => Observable<CreateUserResponse>,
): UsersStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: UsersApiService, useValue: { findUsers: vi.fn(findUsers), createUser: vi.fn(createUser) } },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: () => of(TEACHERS) } },
            { provide: ToastService, useValue: { success: vi.fn(), apiError: vi.fn() } },
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
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);

        //when
        await stable();

        //then
        expect(store.users()).toEqual([ADMINISTRATOR, TEACHER_USER]);
        expect(store.isLoading()).toBe(false);
    });

    it('reports the load error instead of throwing when the Users fail to load', async () => {
        //given
        const store = createStore(
            () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
            () => NEVER,
        );

        //when
        await stable();

        //then
        expect(store.users()).toEqual([]);
        expect(store.isLoading()).toBe(false);
        expect(store.loadError()).toBe('users.loadFailed');
    });

    it('marks the Teachers that already have a User, ordered by name', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);

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
        const store = createStore(() => of([ADMINISTRATOR]), () => NEVER);

        //when
        await stable();

        //then
        expect(store.hasOnlyOneUser()).toBe(true);
    });

    it('creates the User, confirms it and reloads the list', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR]), () => of({ id: 'user-new' }));
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

    it('shows the refusal and keeps the list when the server rejects the User', async () => {
        //given
        const refusal = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userSignInEmailAlreadyInUse' },
        });
        const store = createStore(() => of([ADMINISTRATOR]), () => throwError(() => refusal));
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
});
```

- [ ] **Step 6: Run it and watch it fail**

Run the spec command. Expected: FAIL, `./users.store` doesn't exist.

- [ ] **Step 7: Add the store**

Create `client\src\app\features\users\state\users.store.ts`. Read every `resource()` value through `hasValue()`: `value()` throws while the resource is in error.

```typescript
import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { CreateUserRequest } from '../data/create-user.request';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { UsersApiService } from '../data/users-api.service';
import { LinkableTeacher } from '../domain/linkable-teacher.model';
import { toLinkableTeachers } from '../domain/teacher-link';
import { TeacherOption } from '../domain/teacher-option.model';
import { User } from '../domain/user.model';

const SINGLE_USER_COUNT = 1;

@Injectable({ providedIn: 'root' })
export class UsersStore {
    private readonly api = inject(UsersApiService);
    private readonly teacherOptionsApi = inject(TeacherOptionsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);

    private readonly usersResource = resource({
        loader: () => firstValueFrom(this.api.findUsers()),
    });

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teacherOptionsApi.findTeachers()),
    });

    private readonly teachers = computed<TeacherOption[]>(() =>
        this.teachersResource.hasValue() ? this.teachersResource.value() : [],
    );

    readonly users = computed<User[]>(() => (this.usersResource.hasValue() ? this.usersResource.value() : []));

    readonly linkableTeachers = computed<LinkableTeacher[]>(() => toLinkableTeachers(this.teachers(), this.users()));

    readonly isLoading = computed(() => this.usersResource.isLoading() && !this.usersResource.hasValue());
    readonly loadError = computed(() => (this.usersResource.error() ? 'users.loadFailed' : null));
    readonly hasOnlyOneUser = computed(() => this.users().length === SINGLE_USER_COUNT);
    readonly isMutating = this.mutating.asReadonly();

    async create(request: CreateUserRequest): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.createUser(request));
            this.toast.success('users.created');
            this.usersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}
```

- [ ] **Step 8: Run the specs and watch them pass**

Run the spec command. Expected: `teacher-link.spec.ts` 6 PASS, `users.store.spec.ts` 6 PASS. Then the whole client suite:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

Expected: every spec PASS (`source-text.spec.ts` included).

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/users
git commit -m "feat(client): users feature store with Teachers that already have a User (#85)"
```

End the commit message with the attribution trailer from the session's instructions.
