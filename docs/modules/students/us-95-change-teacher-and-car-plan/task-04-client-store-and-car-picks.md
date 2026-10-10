# Task 4 of 7: Car picks, refusals and store commands (client)

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires tasks 2 and 3 committed. Work on branch `95-change-teacher-and-car`. Read README decisions 12, 14, 15 and 17 first.

**Files:**
- Modify: `client\src\app\features\students\domain\car-pick.ts`, `car-pick.spec.ts`
- Create: `client\src\app\features\students\domain\car-change.ts`, `car-change.spec.ts`
- Modify: `client\src\app\features\students\domain\student-refusal.ts`
- Create: `client\src\app\features\students\data\change-student-teacher.request.ts`, `change-student-car.request.ts`
- Modify: `client\src\app\features\students\data\students-api.service.ts`
- Modify: `client\src\app\features\students\state\students.store.ts`, `students.store.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`students.teacherChanged*`, `students.carChanged*`)

**Interfaces:**
- Consumes: `POST api/students/{id}/change-teacher` body `{ teacherId, carId }` and `POST api/students/{id}/change-car` body `{ carId }` (task 2; 204, or a `ProblemDetails` with `code`), `Student.isCarOfTeacher` (task 3), `CarOption { id; name; transmission; teacherIds }`, `CarPick { options; selectedCarId; isLocked; hasNoCars; isOnlyCar }`, `pickCarFor(teacherId, cars)`, the existing `StudentsStore` internals (`mutating`, `refusalState`, `studentsResource`, `refusalOf`, `teacherNameOf`, `carOptions`), `ToastService.success(key, detail?)`, `isolateDirection(text)`.
- Produces (tasks 5 and 6 rely on these):
  - `domain\car-pick.ts`: `interface TeacherChangeCarPick extends CarPick { keepsCurrentCar: boolean }`, `pickCarForNewTeacher(teacherId: string | null, cars: readonly CarOption[], currentCarId: string): TeacherChangeCarPick`, `carsOfTeacher(teacherId: string, cars: readonly CarOption[]): CarOption[]` (now exported, sorted by name).
  - `domain\car-change.ts`: `interface CarChoice extends CarOption { isCurrent: boolean }`, `interface CarChange { choices: CarChoice[]; hasOtherCars: boolean }`, `carChangeFor(student: Student, cars: readonly CarOption[]): CarChange`.
  - `StudentRefusalKind.sameTeacher` (code `studentAlreadyWithTeacher`), `StudentRefusalKind.sameCar` (code `studentAlreadyOnCar`).
  - `StudentsApiService.changeStudentTeacher(studentId: string, request: ChangeStudentTeacherRequest): Observable<void>`, `.changeStudentCar(studentId: string, request: ChangeStudentCarRequest): Observable<void>`.
  - `StudentsStore.changeTeacher(student: Student, teacherId: string, carId: string): Promise<boolean>`, `StudentsStore.changeCar(student: Student, carId: string): Promise<boolean>`: `true` on success (toast, list reloaded), `false` with `refusal()` set otherwise.

- [ ] **Step 1: Write the failing domain specs**

Append to `car-pick.spec.ts` (change its import to `import { carsOfTeacher, pickCarFor, pickCarForNewTeacher, teacherHasCars } from './car-pick';`):

```ts
describe('pickCarForNewTeacher', () => {
    it('locks the Car until a new Teacher is chosen', () => {
        //when
        const pick = pickCarForNewTeacher(null, CARS, COROLLA.id);

        //then
        expect(pick.isLocked).toBe(true);
        expect(pick.selectedCarId).toBeNull();
        expect(pick.keepsCurrentCar).toBe(false);
    });

    it('keeps the current Car when the new Teacher teaches on it', () => {
        //when
        const pick = pickCarForNewTeacher('teacher-yael', CARS, I20.id);

        //then
        expect(pick.options).toEqual([I20, PICANTO]);
        expect(pick.selectedCarId).toBe(I20.id);
        expect(pick.keepsCurrentCar).toBe(true);
    });

    it("starts empty when the new Teacher doesn't teach on the current Car", () => {
        //when
        const pick = pickCarForNewTeacher('teacher-yael', CARS, COROLLA.id);

        //then
        expect(pick.options).toEqual([I20, PICANTO]);
        expect(pick.selectedCarId).toBeNull();
        expect(pick.keepsCurrentCar).toBe(false);
    });

    it("preselects the new Teacher's only Car", () => {
        //when
        const pick = pickCarForNewTeacher('teacher-oren', CARS, COROLLA.id);

        //then
        expect(pick.selectedCarId).toBe(MAZDA.id);
        expect(pick.isOnlyCar).toBe(true);
        expect(pick.keepsCurrentCar).toBe(false);
    });

    it('says the current Car was kept, not that it is the only one, when both are true', () => {
        //when
        const pick = pickCarForNewTeacher('teacher-oren', CARS, MAZDA.id);

        //then
        expect(pick.selectedCarId).toBe(MAZDA.id);
        expect(pick.keepsCurrentCar).toBe(true);
    });

    it('selects nothing for a new Teacher with no Cars', () => {
        //when
        const pick = pickCarForNewTeacher('teacher-michal', CARS, COROLLA.id);

        //then
        expect(pick.hasNoCars).toBe(true);
        expect(pick.selectedCarId).toBeNull();
    });
});

describe('carsOfTeacher', () => {
    it("lists a Teacher's Cars by name", () => {
        //when
        const cars = carsOfTeacher('teacher-ronit', CARS);

        //then
        expect(cars).toEqual([COROLLA, I20]);
    });
});
```

`client\src\app\features\students\domain\car-change.spec.ts`:

```ts
import { CarOption } from './car-option.model';
import { carChangeFor } from './car-change';
import { Student } from './student.model';
import { Transmission } from './transmission.enum';

const COROLLA: CarOption = { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] };
const I20: CarOption = { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] };
const MAZDA: CarOption = { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] };
const CARS = [MAZDA, I20, COROLLA];

const NOA: Student = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isActive: true,
};

describe('carChangeFor', () => {
    it("offers the Teacher's Cars by name with the current one marked", () => {
        //when
        const change = carChangeFor(NOA, CARS);

        //then
        expect(change.choices).toEqual([
            { ...COROLLA, isCurrent: true },
            { ...I20, isCurrent: false },
        ]);
        expect(change.hasOtherCars).toBe(true);
    });

    it('has no other Car when the current Car is the Teacher\'s only one', () => {
        //given
        const lia: Student = { ...NOA, teacherId: 'teacher-oren', carId: MAZDA.id };

        //when
        const change = carChangeFor(lia, CARS);

        //then
        expect(change.choices).toEqual([{ ...MAZDA, isCurrent: true }]);
        expect(change.hasOtherCars).toBe(false);
    });

    it('offers every Car of the Teacher to a flagged Student', () => {
        //given
        const roi: Student = { ...NOA, teacherId: 'teacher-oren', carId: COROLLA.id, isCarOfTeacher: false };

        //when
        const change = carChangeFor(roi, CARS);

        //then
        expect(change.choices).toEqual([{ ...MAZDA, isCurrent: false }]);
        expect(change.hasOtherCars).toBe(true);
    });

    it('has nothing to offer when the Teacher has no Cars', () => {
        //given
        const flagged: Student = { ...NOA, teacherId: 'teacher-michal', isCarOfTeacher: false };

        //when
        const change = carChangeFor(flagged, CARS);

        //then
        expect(change.choices).toEqual([]);
        expect(change.hasOtherCars).toBe(false);
    });
});
```

- [ ] **Step 2: Run the specs to verify they fail**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/domain`
Expected: FAIL, `pickCarForNewTeacher` / `carsOfTeacher` not exported, `Cannot find module './car-change'`.

- [ ] **Step 3: Implement the picks**

In `car-pick.ts`, rename the private `carsOf` to an exported `carsOfTeacher` (update its call in `pickCarFor`), and add after `teacherHasCars`:

```ts
export interface TeacherChangeCarPick extends CarPick {
    keepsCurrentCar: boolean;
}

export function pickCarForNewTeacher(
    teacherId: string | null,
    cars: readonly CarOption[],
    currentCarId: string,
): TeacherChangeCarPick {
    const pick = pickCarFor(teacherId, cars);
    const keepsCurrentCar = pick.options.some((car) => car.id === currentCarId);

    return {
        ...pick,
        selectedCarId: keepsCurrentCar ? currentCarId : pick.selectedCarId,
        keepsCurrentCar,
    };
}
```

The exported helper:

```ts
export function carsOfTeacher(teacherId: string, cars: readonly CarOption[]): CarOption[] {
    return cars
        .filter((car) => car.teacherIds.includes(teacherId))
        .sort((first, second) => first.name.localeCompare(second.name));
}
```

`client\src\app\features\students\domain\car-change.ts`:

```ts
import { CarOption } from './car-option.model';
import { carsOfTeacher } from './car-pick';
import { Student } from './student.model';

export interface CarChoice extends CarOption {
    isCurrent: boolean;
}

export interface CarChange {
    choices: CarChoice[];
    hasOtherCars: boolean;
}

export function carChangeFor(student: Student, cars: readonly CarOption[]): CarChange {
    const choices = carsOfTeacher(student.teacherId, cars).map((car) => ({
        ...car,
        isCurrent: car.id === student.carId,
    }));

    return { choices, hasOtherCars: choices.some((choice) => !choice.isCurrent) };
}
```

- [ ] **Step 4: Run the domain specs to verify they pass**

Run: the step 2 command.
Expected: PASS (the existing `pickCarFor` tests included).

- [ ] **Step 5: Write the failing store specs**

In `students.store.spec.ts` (no new imports needed; the stubs only use `Observable<void>`):
- Extend `ApiStubs` with `changeStudentTeacher: () => Observable<void>;` and `changeStudentCar: () => Observable<void>;`, `createStore`'s `stubs` defaults with `changeStudentTeacher: () => of(undefined),` and `changeStudentCar: () => of(undefined),`, and `api` with `changeStudentTeacher: vi.fn(stubs.changeStudentTeacher),` and `changeStudentCar: vi.fn(stubs.changeStudentCar),`.

Append inside `describe('StudentsStore', ...)`:

```ts
    it('changes the Teacher, names the Student, Teacher and Car in the confirmation and reloads', async () => {
        //given
        const { store, toast, api } = createStore();
        await stable();

        //when
        const saved = await store.changeTeacher(NOA, 'teacher-yael', 'car-i20');

        //then
        expect(saved).toBe(true);
        expect(api.changeStudentTeacher).toHaveBeenCalledWith(NOA.id, { teacherId: 'teacher-yael', carId: 'car-i20' });
        expect(toast.success).toHaveBeenCalledWith('students.teacherChanged', {
            key: 'students.teacherChangedDetail',
            params: {
                name: isolateDirection('Noa Mizrahi'),
                teacher: isolateDirection('Yael Carmi'),
                car: isolateDirection('i20 Silver'),
            },
        });
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it('keeps the same-Teacher refusal for the dialog', async () => {
        //given
        const refusal = problem(HTTP_CONFLICT, 'studentAlreadyWithTeacher');
        const { store, toast } = createStore({ changeStudentTeacher: () => throwError(() => refusal) });
        await stable();

        //when
        const saved = await store.changeTeacher(NOA, 'teacher-yael', 'car-i20');

        //then
        expect(saved).toBe(false);
        expect(toast.success).not.toHaveBeenCalled();
        expect(store.refusal()?.kind).toBe(StudentRefusalKind.sameTeacher);
        expect(store.isMutating()).toBe(false);
    });

    it("treats a Car that is not the new Teacher's as a stale Car", async () => {
        //given
        const refusal = problem(HTTP_CONFLICT, 'studentCarMustBeAssignedToTeacher');
        const { store } = createStore({ changeStudentTeacher: () => throwError(() => refusal) });
        await stable();

        //when
        await store.changeTeacher(NOA, 'teacher-yael', 'car-i20');

        //then
        expect(store.refusal()?.kind).toBe(StudentRefusalKind.staleCar);
    });

    it('changes the Car, names the Student and Car in the confirmation and reloads', async () => {
        //given
        const { store, toast, api } = createStore();
        await stable();

        //when
        const saved = await store.changeCar(NOA, 'car-i20');

        //then
        expect(saved).toBe(true);
        expect(api.changeStudentCar).toHaveBeenCalledWith(NOA.id, { carId: 'car-i20' });
        expect(toast.success).toHaveBeenCalledWith('students.carChanged', {
            key: 'students.carChangedDetail',
            params: { name: isolateDirection('Noa Mizrahi'), car: isolateDirection('i20 Silver') },
        });
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it('keeps the same-Car refusal for the dialog', async () => {
        //given
        const refusal = problem(HTTP_CONFLICT, 'studentAlreadyOnCar');
        const { store } = createStore({ changeStudentCar: () => throwError(() => refusal) });
        await stable();

        //when
        const saved = await store.changeCar(NOA, 'car-i20');

        //then
        expect(saved).toBe(false);
        expect(store.refusal()?.kind).toBe(StudentRefusalKind.sameCar);
    });
```

- [ ] **Step 6: Run the store spec to verify it fails**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/state/students.store.spec.ts`
Expected: FAIL, `store.changeTeacher is not a function` (and `changeCar`), `StudentRefusalKind.sameTeacher` undefined.

- [ ] **Step 7: Add the refusal kinds, requests, API methods**

In `student-refusal.ts`, add to the enum after `staleCar = 'staleCar',`:

```ts
    sameTeacher = 'sameTeacher',
    sameCar = 'sameCar',
```

and to `KIND_BY_CODE` after `carNotFound: StudentRefusalKind.staleCar,`:

```ts
    studentAlreadyWithTeacher: StudentRefusalKind.sameTeacher,
    studentAlreadyOnCar: StudentRefusalKind.sameCar,
```

`data\change-student-teacher.request.ts`:

```ts
export interface ChangeStudentTeacherRequest {
    teacherId: string;
    carId: string;
}
```

`data\change-student-car.request.ts`:

```ts
export interface ChangeStudentCarRequest {
    carId: string;
}
```

In `students-api.service.ts`, import both and add after `reactivateStudent`:

```ts
    changeStudentTeacher(studentId: string, request: ChangeStudentTeacherRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/change-teacher`, request);
    }

    changeStudentCar(studentId: string, request: ChangeStudentCarRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/change-car`, request);
    }
```

- [ ] **Step 8: Add the store commands**

In `students.store.ts`, after `reactivate(...)`:

```ts
    async changeTeacher(student: Student, teacherId: string, carId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentTeacher(student.id, { teacherId, carId }));
            this.toast.success('students.teacherChanged', {
                key: 'students.teacherChangedDetail',
                params: {
                    name: isolateDirection(student.name),
                    teacher: isolateDirection(this.teacherNameOf(teacherId)),
                    car: isolateDirection(this.carNameOf(carId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async changeCar(student: Student, carId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentCar(student.id, { carId }));
            this.toast.success('students.carChanged', {
                key: 'students.carChangedDetail',
                params: {
                    name: isolateDirection(student.name),
                    car: isolateDirection(this.carNameOf(carId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }
```

and after `teacherNameOf(...)`:

```ts
    private carNameOf(carId: string): string {
        return this.carOptions().find((car) => car.id === carId)?.name ?? '';
    }
```

- [ ] **Step 9: Add the toast translations**

`he.json`, inside `students`, after `"reactivatedDetail"`:

```json
    "teacherChanged": "המורה הוחלף",
    "teacherChangedDetail": "המורה של {{name}}: {{teacher}}, על {{car}}.",
    "carChanged": "הרכב הוחלף",
    "carChangedDetail": "{{name}} ילמד/תלמד על {{car}}.",
```

`en.json`, same place:

```json
    "teacherChanged": "Teacher changed",
    "teacherChangedDetail": "{{name}}'s Teacher is now {{teacher}}, on {{car}}.",
    "carChanged": "Car changed",
    "carChangedDetail": "{{name}} now learns on {{car}}.",
```

- [ ] **Step 10: Run the client suite and build**

Run from `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS; build succeeds.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Change Teacher and Change Car picks and store commands for Students (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
