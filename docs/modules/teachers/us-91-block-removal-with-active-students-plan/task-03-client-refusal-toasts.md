# Task 3 of 4: Refusal toasts on the Cars & Teachers screen

> Part of [#91: Block deleting or unassigning Teachers and Cars while active Students depend on them](README.md). Requires task 2 committed. Work on branch `91-block-removal-with-active-students`. Read README decisions 9 and 10 first.

**Files:**
- Modify: `client\src\app\core\services\toast.service.ts` (`apiError`), `toast.service.spec.ts`
- Modify: `client\src\app\features\teachers\state\teachers.store.ts`, `teachers.store.spec.ts`
- Modify: `client\src\app\features\teachers\state\cars.store.ts`, `cars.store.spec.ts`

**Interfaces:**
- Consumes: task 2's 409 problems (`code`, `params.count`, `params.names`, `params.teacher`) and `errors.*` translations; `ToastService.messageOf(error, isolatedParams)` (exists); `TeachersApiService.findTeachers()` / `deleteTeacher(id)`; `CarsApiService.findCars()` / `deleteCar(id)` / `assignTeacher(carId, teacherId)` / `unassignTeacher(carId, teacherId)`.
- Produces: `ToastService.apiError(error: unknown, isolatedParams: readonly string[] = []): void`. Task 4 checks the toasts in the browser.

The screen itself doesn't change: `CarsAndTeachersPage` already routes Delete Teacher, Delete Car and the assign popover's Apply through these stores, and the stores already toast every refusal. This task makes the names inside those toasts direction-safe.

- [ ] **Step 1: Write the failing `ToastService` spec**

In `toast.service.spec.ts`, add to `EN.errors` (alphabetical position):

```ts
        teacherAssignmentMustNotHaveActiveStudents: 'Active Students of {{teacher}} learn on this Car ({{count}}): {{names}}.',
```

and add a new block inside `describe('ToastService', ...)`, after the `messageOf` block:

```ts
    describe('apiError', () => {
        it('isolates the names in a refusal toast', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'teacherAssignmentMustNotHaveActiveStudents',
                params: { teacher: 'רונית אברהם', count: '2', names: 'נועה מזרחי, עומר שלו' },
            });

            //when
            toast.apiError(error, ['teacher', 'names']);

            //then
            expect(add.mock.calls[0][0].severity).toBe('error');
            expect(shownSummary(add)).toBe(
                `Active Students of ${isolateDirection('רונית אברהם')} learn on this Car (2): ${isolateDirection('נועה מזרחי, עומר שלו')}.`,
            );
        });

        it('leaves the params as they are when none are named', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'teacherAssignmentMustNotHaveActiveStudents',
                params: { teacher: 'Ronit', count: '1', names: 'Noa' },
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Active Students of Ronit learn on this Car (1): Noa.');
        });
    });
```

- [ ] **Step 2: Write the failing store specs**

In `teachers.store.spec.ts`, change the imports to:

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { TeachersApiService } from '../data/teachers-api.service';
import { TeachersStore } from './teachers.store';
```

add after `HTTP_INTERNAL_SERVER_ERROR`:

```ts
const HTTP_CONFLICT = 409;

function storeRefusingDelete(apiError: ReturnType<typeof vi.fn>, refusal: HttpErrorResponse): TeachersStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            {
                provide: TeachersApiService,
                useValue: {
                    findTeachers: () => of([]),
                    deleteTeacher: () => throwError(() => refusal),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError } },
        ],
    });

    return TestBed.inject(TeachersStore);
}
```

and inside `describe('TeachersStore', ...)`:

```ts
    it('isolates the Student names when deleting a Teacher is refused', async () => {
        //given
        const apiError = vi.fn();
        const refusal = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { code: 'teacherMustNotHaveActiveStudents', params: { count: '1', names: 'Noa Mizrahi' } },
        });
        const store = storeRefusingDelete(apiError, refusal);

        //when
        await store.delete('teacher-1');

        //then
        expect(apiError).toHaveBeenCalledWith(refusal, ['names']);
        expect(store.isMutating()).toBe(false);
    });
```

In `cars.store.spec.ts`, change the `rxjs` import to `import { of, throwError } from 'rxjs';`, add after `HTTP_INTERNAL_SERVER_ERROR`:

```ts
const HTTP_CONFLICT = 409;

function storeWithApi(apiError: ReturnType<typeof vi.fn>, api: Partial<Record<keyof CarsApiService, unknown>>): CarsStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: CarsApiService, useValue: { findCars: () => of([]), ...api } },
            { provide: ToastService, useValue: { success: () => undefined, apiError } },
        ],
    });

    return TestBed.inject(CarsStore);
}

function conflict(code: string, params: Record<string, string>): HttpErrorResponse {
    return new HttpErrorResponse({ status: HTTP_CONFLICT, error: { code, params } });
}
```

and inside `describe('CarsStore', ...)`:

```ts
    it('isolates the Student names when deleting a Car is refused', async () => {
        //given
        const apiError = vi.fn();
        const refusal = conflict('carMustNotHaveActiveStudents', { count: '1', names: 'Noa Mizrahi' });
        const store = storeWithApi(apiError, { deleteCar: () => throwError(() => refusal) });

        //when
        await store.delete('car-1');

        //then
        expect(apiError).toHaveBeenCalledWith(refusal, ['names']);
    });

    it('isolates the Teacher and Student names when an unassign is refused, and reloads the Cars', async () => {
        //given
        const apiError = vi.fn();
        const findCars = vi.fn(() => of([]));
        const refusal = conflict('teacherAssignmentMustNotHaveActiveStudents', {
            count: '1',
            names: 'Noa Mizrahi',
            teacher: 'Ronit Avraham',
        });
        const store = storeWithApi(apiError, {
            findCars,
            unassignTeacher: () => throwError(() => refusal),
        });
        await TestBed.inject(ApplicationRef).whenStable();
        const loadsBefore = findCars.mock.calls.length;

        //when
        await store.applyAssignments('car-1', [], ['teacher-1']);
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(apiError).toHaveBeenCalledWith(refusal, ['teacher', 'names']);
        expect(findCars.mock.calls.length).toBeGreaterThan(loadsBefore);
        expect(store.isMutating()).toBe(false);
    });

    it('sends no assign once an unassign in the same Apply is refused', async () => {
        //given
        const apiError = vi.fn();
        const assignTeacher = vi.fn(() => of(undefined));
        const refusal = conflict('teacherAssignmentMustNotHaveActiveStudents', {
            count: '1',
            names: 'Noa Mizrahi',
            teacher: 'Ronit Avraham',
        });
        const store = storeWithApi(apiError, {
            assignTeacher,
            unassignTeacher: () => throwError(() => refusal),
        });

        //when
        await store.applyAssignments('car-1', ['teacher-2'], ['teacher-1']);

        //then
        expect(assignTeacher).not.toHaveBeenCalled();
        expect(apiError).toHaveBeenCalledWith(refusal, ['teacher', 'names']);
    });
```

`CarsApiService` is already imported by the spec.

- [ ] **Step 3: Run the specs to verify they fail**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/services/toast.service.spec.ts --include src/app/features/teachers/state/teachers.store.spec.ts --include src/app/features/teachers/state/cars.store.spec.ts`
Expected: FAIL. `isolates the names in a refusal toast` shows the names without isolation marks (and the TypeScript check rejects the second `apiError` argument); the store specs see `apiError` called with one argument; `sends no assign once an unassign in the same Apply is refused` sees `assignTeacher` called (assigns go first today).

- [ ] **Step 4: Forward the isolated params in `ToastService`**

In `toast.service.ts`, replace `apiError`:

```ts
    apiError(error: unknown, isolatedParams: readonly string[] = []): void {
        this.messages.add({ severity: 'error', summary: this.messageOf(error, isolatedParams) });
    }
```

- [ ] **Step 5: Pass the params from the stores**

In `teachers.store.ts`, add after the imports:

```ts
const ACTIVE_STUDENTS_PARAMS: readonly string[] = ['names'];
```

change `delete`:

```ts
    async delete(teacherId: string): Promise<void> {
        await this.executeCommand(() => this.api.deleteTeacher(teacherId), 'teachers.deleted', ACTIVE_STUDENTS_PARAMS);
    }
```

and `executeCommand`:

```ts
    private async executeCommand(
        command: () => Observable<unknown>,
        successKey: string,
        isolatedParams: readonly string[] = [],
    ): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.teachersResource.reload();
        } catch (error) {
            this.toast.apiError(error, isolatedParams);
        } finally {
            this.mutating.set(false);
        }
    }
```

In `cars.store.ts`, add after the imports:

```ts
const ACTIVE_STUDENTS_PARAMS: readonly string[] = ['names'];
const ASSIGNMENT_PARAMS: readonly string[] = ['teacher', 'names'];
```

change `delete` to pass `ACTIVE_STUDENTS_PARAMS` as the third argument of `executeCommand`, and give `executeCommand` the same `isolatedParams: readonly string[] = []` parameter and `this.toast.apiError(error, isolatedParams)` as in `TeachersStore`. In `applyAssignments`, send the unassigns first (README decision 10) and name the params in the catch:

```ts
        try {
            for (const teacherId of toUnassign) {
                await firstValueFrom(this.api.unassignTeacher(carId, teacherId));
            }

            for (const teacherId of toAssign) {
                await firstValueFrom(this.api.assignTeacher(carId, teacherId));
            }

            this.toast.success('teachers.assignmentsUpdated');
        } catch (error) {
            this.toast.apiError(error, ASSIGNMENT_PARAMS);
        } finally {
```

(`assignTeacher` refusals carry no `teacher` / `names` params, so naming them is harmless there.)

- [ ] **Step 6: Run the client suite**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false`
Expected: PASS, every spec.

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client`
Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/core/services/toast.service.ts client/src/app/core/services/toast.service.spec.ts client/src/app/features/teachers/state/teachers.store.ts client/src/app/features/teachers/state/teachers.store.spec.ts client/src/app/features/teachers/state/cars.store.ts client/src/app/features/teachers/state/cars.store.spec.ts
git commit -m "feat(teachers): keep Teacher and Student names direction-safe in the refusal toasts (#91)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
