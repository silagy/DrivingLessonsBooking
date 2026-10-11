# Task 4 of 7: Students store - load and change details, deactivate, reactivate (client)

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires tasks 1 and 2 committed. Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 11 to 14 and 17 first.

**Files:**
- Rename: `client\src\app\features\students\domain\add-student-refusal.ts` → `student-refusal.ts`
- Create: `client\src\app\features\students\domain\form-values.ts`, `form-values.spec.ts`
- Modify: `client\src\app\features\students\domain\new-student.ts`
- Create: `client\src\app\features\students\domain\student-details.model.ts`
- Create: `client\src\app\features\students\domain\student-details-change.model.ts`
- Create: `client\src\app\features\students\domain\student-details.ts`, `student-details.spec.ts`
- Create: `client\src\app\features\students\data\get-student.response.ts`
- Create: `client\src\app\features\students\data\change-student-details.request.ts`
- Modify: `client\src\app\features\students\data\students-api.service.ts`
- Modify: `client\src\app\features\students\state\students.store.ts`, `students.store.spec.ts`
- Modify (rename only): `client\src\app\features\students\ui\dialogs\add-student\add-student-dialog-data.ts`, `add-student.dialog.ts`, `add-student.dialog.spec.ts`

**Interfaces:**
- Consumes: `PUT api/students/{id}/details` (task 1), `POST api/students/{id}/deactivate|reactivate` (task 2), `GET api/students/{id}` (#93: `GetStudentResponse`, JSON camelCase, `carTransmission` `"automatic"|"manual"`, `startDate` `"yyyy-MM-dd"|null`), `ToastService.success(key, detail?)`, `.apiError(error)`, `.messageOf(error, isolatedParams)`, `isolateDirection(text)`, `ProblemDetails` (`code?`, `params?: Record<string, string>`), the existing `StudentsStore` internals (`mutating`, `refusalState`, `studentsResource`, `problemCodeOf`).
- Produces (tasks 5 and 6 rely on these):
  - `domain\student-refusal.ts`: `enum StudentRefusalKind { nationalIdInUse, nationalIdInvalid, staleCar, other }`, `interface StudentRefusal { kind: StudentRefusalKind; message: string; existingStudentName: string | null }`, `refusalKindOf(code: string | undefined): StudentRefusalKind`.
  - `domain\form-values.ts`: `optionalText(value: string): string | null`, `toIsoDate(date: Date): string`, `fromIsoDate(text: string): Date`.
  - `domain\student-details.model.ts`: `interface StudentDetails extends Student { address: string | null; startDate: string | null; licenseType: string | null }`.
  - `domain\student-details-change.model.ts`: `interface StudentDetailsChange { nationalId; name; phone: string; address; startDate; licenseType: string | null }`.
  - `domain\student-details.ts`: `interface EditStudentFormValue { nationalId: string; name: string; phone: string; address: string; startDate: Date | null; licenseType: string }`, `toEditStudentFormValue(details: StudentDetails): EditStudentFormValue`, `toStudentDetailsChange(value: EditStudentFormValue): StudentDetailsChange`.
  - `StudentsApiService.getStudent(id)`, `.changeStudentDetails(id, request)`, `.deactivateStudent(id)`, `.reactivateStudent(id)`.
  - `StudentsStore.loadDetails(studentId: string): Promise<StudentDetails | null>`, `.changeDetails(studentId: string, change: StudentDetailsChange): Promise<boolean>`, `.deactivate(student: Student): Promise<boolean>` (true = close the dialog), `.reactivate(student: Student): Promise<void>`; `refusal` is now `Signal<StudentRefusal | null>`.
  - Toast keys used (added in tasks 5 and 6): `students.detailsSaved`, `students.deactivated`, `students.deactivatedDetail` (`{{name}}`), `students.reactivated`, `students.reactivatedDetail` (`{{name}}`).

**Why:** the dialogs (tasks 5, 6) stay dumb: every command, toast and reload lives in the page-scoped store, like #93's `create`. Stale toggles (README decision 13, Review Focus 5) and the date round-trip (Review Focus 3) are pinned here, where they are pure and fast to test.

- [ ] **Step 1: Rename the refusal type**

From the repository root (Bash):

```bash
git mv client/src/app/features/students/domain/add-student-refusal.ts client/src/app/features/students/domain/student-refusal.ts
grep -rl "add-student-refusal\|AddStudentRefusal" client/src/app/features/students | xargs sed -i 's/add-student-refusal/student-refusal/g; s/AddStudentRefusalKind/StudentRefusalKind/g; s/AddStudentRefusal/StudentRefusal/g'
```

Then replace `client\src\app\features\students\domain\student-refusal.ts` with:

```ts
export enum StudentRefusalKind {
    nationalIdInUse = 'nationalIdInUse',
    nationalIdInvalid = 'nationalIdInvalid',
    staleCar = 'staleCar',
    other = 'other',
}

export interface StudentRefusal {
    kind: StudentRefusalKind;
    message: string;
    existingStudentName: string | null;
}

const KIND_BY_CODE: Partial<Record<string, StudentRefusalKind>> = {
    studentNationalIdAlreadyInUse: StudentRefusalKind.nationalIdInUse,
    nationalIdMustBeDigits: StudentRefusalKind.nationalIdInvalid,
    nationalIdMustBeAtMostNineDigits: StudentRefusalKind.nationalIdInvalid,
    nationalIdMustHaveValidCheckDigit: StudentRefusalKind.nationalIdInvalid,
    studentCarMustBeAssignedToTeacher: StudentRefusalKind.staleCar,
    carNotFound: StudentRefusalKind.staleCar,
};

export function refusalKindOf(code: string | undefined): StudentRefusalKind {
    if (!code) {
        return StudentRefusalKind.other;
    }

    return KIND_BY_CODE[code] ?? StudentRefusalKind.other;
}
```

In `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.spec.ts`, give both refusal constants the new field:

```ts
const NATIONAL_ID_REFUSAL: StudentRefusal = {
    kind: StudentRefusalKind.nationalIdInUse,
    message: 'A Student with this national ID already exists.',
    existingStudentName: 'Noa Mizrahi',
};
const STALE_CAR_REFUSAL: StudentRefusal = {
    kind: StudentRefusalKind.staleCar,
    message: 'Refresh and choose again.',
    existingStudentName: null,
};
```

Check: `grep -rn "AddStudentRefusal\|add-student-refusal" client/src` prints nothing.

- [ ] **Step 2: Write the failing domain specs**

Create `client\src\app\features\students\domain\form-values.spec.ts`:

```ts
import { fromIsoDate, optionalText, toIsoDate } from './form-values';

describe('optionalText', () => {
    it('trims a value', () => {
        //when
        const text = optionalText(' B ');

        //then
        expect(text).toBe('B');
    });

    it.each(['', '   '])('treats %j as absent', (value) => {
        //when
        const text = optionalText(value);

        //then
        expect(text).toBeNull();
    });
});

describe('toIsoDate', () => {
    it("keeps the picked date's calendar day, late in the evening too", () => {
        //when
        const text = toIsoDate(new Date(2026, 8, 1, 23, 30));

        //then
        expect(text).toBe('2026-09-01');
    });
});

describe('fromIsoDate', () => {
    it('reads the calendar day as local midnight', () => {
        //when
        const date = fromIsoDate('2026-09-01');

        //then
        expect([date.getFullYear(), date.getMonth(), date.getDate(), date.getHours()]).toEqual([2026, 8, 1, 0]);
    });

    it('round-trips the saved start date', () => {
        //when
        const text = toIsoDate(fromIsoDate('2026-12-31'));

        //then
        expect(text).toBe('2026-12-31');
    });
});
```

Create `client\src\app\features\students\domain\student-details.spec.ts`:

```ts
import { StudentDetails } from './student-details.model';
import { EditStudentFormValue, toEditStudentFormValue, toStudentDetailsChange } from './student-details';
import { Transmission } from './transmission.enum';

const NOA: StudentDetails = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    address: '12 HaRimon St, Modiin',
    startDate: '2026-09-01',
    licenseType: 'B',
    isActive: true,
};

describe('toEditStudentFormValue', () => {
    it("fills the form with the Student's details and the start date as a local date", () => {
        //when
        const value = toEditStudentFormValue(NOA);

        //then
        expect(value).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234567',
            address: '12 HaRimon St, Modiin',
            startDate: new Date(2026, 8, 1),
            licenseType: 'B',
        });
    });

    it('leaves absent optional details blank', () => {
        //when
        const value = toEditStudentFormValue({ ...NOA, address: null, startDate: null, licenseType: null });

        //then
        expect([value.address, value.startDate, value.licenseType]).toEqual(['', null, '']);
    });
});

describe('toStudentDetailsChange', () => {
    it('trims every text and keeps the calendar day', () => {
        //given
        const value: EditStudentFormValue = {
            nationalId: ' 205374184 ',
            name: ' Noa Mizrahi ',
            phone: ' 050-1234568 ',
            address: ' 12 HaRimon St, Modiin ',
            startDate: new Date(2026, 8, 1),
            licenseType: ' B ',
        };

        //when
        const change = toStudentDetailsChange(value);

        //then
        expect(change).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234568',
            address: '12 HaRimon St, Modiin',
            startDate: '2026-09-01',
            licenseType: 'B',
        });
    });

    it('sends cleared optional details as absent, never as empty text', () => {
        //given
        const value: EditStudentFormValue = {
            ...toEditStudentFormValue(NOA),
            address: '   ',
            startDate: null,
            licenseType: '',
        };

        //when
        const change = toStudentDetailsChange(value);

        //then
        expect([change.address, change.startDate, change.licenseType]).toEqual([null, null, null]);
    });

    it('round-trips the saved start date', () => {
        //when
        const change = toStudentDetailsChange(toEditStudentFormValue(NOA));

        //then
        expect(change.startDate).toBe('2026-09-01');
    });
});
```

Run from `client\` (PowerShell): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/domain`
Expected: FAIL (`Cannot find module './form-values'`, `'./student-details.model'`, `'./student-details'`).

- [ ] **Step 3: Add the form helpers and the details mapping**

Create `client\src\app\features\students\domain\form-values.ts`:

```ts
const MONTH_OFFSET = 1;
const DATE_PART_LENGTH = 2;
const DATE_PART_PAD = '0';
const ISO_DATE_SEPARATOR = '-';
const DECIMAL_RADIX = 10;

export function optionalText(value: string): string | null {
    const trimmed = value.trim();

    return trimmed ? trimmed : null;
}

export function toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + MONTH_OFFSET).padStart(DATE_PART_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_LENGTH, DATE_PART_PAD);

    return `${year}-${month}-${day}`;
}

export function fromIsoDate(text: string): Date {
    const [year, month, day] = text.split(ISO_DATE_SEPARATOR).map((part) => Number.parseInt(part, DECIMAL_RADIX));

    return new Date(year, month - MONTH_OFFSET, day);
}
```

Replace `client\src\app\features\students\domain\new-student.ts` with (same behaviour, helpers now shared):

```ts
import { optionalText, toIsoDate } from './form-values';
import { NewStudent } from './new-student.model';

export interface AddStudentFormValue {
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    carId: string;
    address: string;
    startDate: Date | null;
    licenseType: string;
}

export function toNewStudent(value: AddStudentFormValue): NewStudent {
    return {
        nationalId: value.nationalId.trim(),
        name: value.name.trim(),
        phone: value.phone.trim(),
        teacherId: value.teacherId,
        carId: value.carId,
        address: optionalText(value.address),
        startDate: value.startDate ? toIsoDate(value.startDate) : null,
        licenseType: optionalText(value.licenseType),
    };
}
```

Create `client\src\app\features\students\domain\student-details.model.ts`:

```ts
import { Student } from './student.model';

export interface StudentDetails extends Student {
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
```

Create `client\src\app\features\students\domain\student-details-change.model.ts`:

```ts
export interface StudentDetailsChange {
    nationalId: string;
    name: string;
    phone: string;
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
```

Create `client\src\app\features\students\domain\student-details.ts`:

```ts
import { fromIsoDate, optionalText, toIsoDate } from './form-values';
import { StudentDetailsChange } from './student-details-change.model';
import { StudentDetails } from './student-details.model';

export interface EditStudentFormValue {
    nationalId: string;
    name: string;
    phone: string;
    address: string;
    startDate: Date | null;
    licenseType: string;
}

export function toEditStudentFormValue(details: StudentDetails): EditStudentFormValue {
    return {
        nationalId: details.nationalId,
        name: details.name,
        phone: details.phone,
        address: details.address ?? '',
        startDate: details.startDate ? fromIsoDate(details.startDate) : null,
        licenseType: details.licenseType ?? '',
    };
}

export function toStudentDetailsChange(value: EditStudentFormValue): StudentDetailsChange {
    return {
        nationalId: value.nationalId.trim(),
        name: value.name.trim(),
        phone: value.phone.trim(),
        address: optionalText(value.address),
        startDate: value.startDate ? toIsoDate(value.startDate) : null,
        licenseType: optionalText(value.licenseType),
    };
}
```

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/domain`
Expected: PASS (the new specs and the existing `new-student.spec.ts`, `car-pick.spec.ts`).

- [ ] **Step 4: Add the DTOs and the API methods**

Create `client\src\app\features\students\data\get-student.response.ts`:

```ts
import { Transmission } from '../domain/transmission.enum';

export interface GetStudentResponse {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    carTransmission: Transmission;
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
    isActive: boolean;
}
```

Create `client\src\app\features\students\data\change-student-details.request.ts`:

```ts
export interface ChangeStudentDetailsRequest {
    nationalId: string;
    name: string;
    phone: string;
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
```

Replace `client\src\app\features\students\data\students-api.service.ts` with:

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeStudentDetailsRequest } from './change-student-details.request';
import { CreateStudentRequest } from './create-student.request';
import { CreateStudentResponse } from './create-student.response';
import { GetStudentResponse } from './get-student.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class StudentsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/students';

    findStudents(): Observable<ItemForFindStudentsResponse[]> {
        return this.http.get<ItemForFindStudentsResponse[]>(`${this.baseUrl}/find`);
    }

    getStudent(studentId: string): Observable<GetStudentResponse> {
        return this.http.get<GetStudentResponse>(`${this.baseUrl}/${studentId}`);
    }

    createStudent(request: CreateStudentRequest): Observable<CreateStudentResponse> {
        return this.http.post<CreateStudentResponse>(this.baseUrl, request);
    }

    changeStudentDetails(studentId: string, request: ChangeStudentDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${studentId}/details`, request);
    }

    deactivateStudent(studentId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/deactivate`, {});
    }

    reactivateStudent(studentId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/reactivate`, {});
    }
}
```

- [ ] **Step 5: Write the failing store specs**

In `client\src\app\features\students\state\students.store.spec.ts`:

1. Add the imports (alphabetical with the existing ones):

```ts
import { GetStudentResponse } from '../data/get-student.response';
import { StudentDetailsChange } from '../domain/student-details-change.model';
```

2. Add these constants right after `NEW_STUDENT`:

```ts
const NOA_DETAILS: GetStudentResponse = {
    ...NOA,
    address: '12 HaRimon St, Modiin',
    startDate: '2026-09-01',
    licenseType: 'B',
};

const NOA_CHANGE: StudentDetailsChange = {
    nationalId: NOA.nationalId,
    name: NOA.name,
    phone: '050-1234568',
    address: null,
    startDate: null,
    licenseType: 'B',
};
```

3. Replace the `problem` function, the `ApiStubs` interface and `createStore` with:

```ts
function problem(status: number, code: string, params?: Record<string, string>): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: { status, title: 'Refused', code, params } });
}

interface ApiStubs {
    findStudents: () => Observable<ItemForFindStudentsResponse[]>;
    getStudent: () => Observable<GetStudentResponse>;
    createStudent: () => Observable<CreateStudentResponse>;
    changeStudentDetails: () => Observable<void>;
    deactivateStudent: () => Observable<void>;
    reactivateStudent: () => Observable<void>;
    findTeachers: () => Observable<ItemForFindTeachersResponse[]>;
    findCars: () => Observable<ItemForFindCarsResponse[]>;
}

function createStore(overrides: Partial<ApiStubs> = {}) {
    const stubs: ApiStubs = {
        findStudents: () => of(STUDENTS),
        getStudent: () => of(NOA_DETAILS),
        createStudent: () => of({ id: SHAKED.id }),
        changeStudentDetails: () => of(undefined),
        deactivateStudent: () => of(undefined),
        reactivateStudent: () => of(undefined),
        findTeachers: () => of(TEACHERS),
        findCars: () => of(CARS),
        ...overrides,
    };
    const toast = { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') };
    const findCars = vi.fn(stubs.findCars);
    const api = {
        findStudents: vi.fn(stubs.findStudents),
        getStudent: vi.fn(stubs.getStudent),
        createStudent: vi.fn(stubs.createStudent),
        changeStudentDetails: vi.fn(stubs.changeStudentDetails),
        deactivateStudent: vi.fn(stubs.deactivateStudent),
        reactivateStudent: vi.fn(stubs.reactivateStudent),
    };

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            StudentsStore,
            { provide: StudentsApiService, useValue: api },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(stubs.findTeachers) } },
            { provide: CarOptionsApiService, useValue: { findCars } },
            { provide: ToastService, useValue: toast },
        ],
    });

    return { store: TestBed.inject(StudentsStore), toast, findCars, api };
}
```

4. In the existing tests `keeps the refusal for the dialog when the national ID is in use` and `treats any other refusal as other`, add `existingStudentName: null` to the expected refusal objects:

```ts
        expect(store.refusal()).toEqual({
            kind: StudentRefusalKind.nationalIdInUse,
            message: 'translated refusal',
            existingStudentName: null,
        });
```

```ts
        expect(store.refusal()).toEqual({
            kind: StudentRefusalKind.other,
            message: 'translated refusal',
            existingStudentName: null,
        });
```

5. Append these tests inside `describe('StudentsStore', ...)`, after `reloads the Car options and forgets the stale refusal`:

```ts
    it("loads a Student's details for editing", async () => {
        //given
        const { store, api } = createStore();
        await stable();

        //when
        const details = await store.loadDetails(NOA.id);

        //then
        expect(api.getStudent).toHaveBeenCalledWith(NOA.id);
        expect(details).toEqual(NOA_DETAILS);
        expect(store.isMutating()).toBe(false);
    });

    it('reports a details load failure as a toast and opens nothing', async () => {
        //given
        const failure = problem(HTTP_NOT_FOUND, 'studentNotFound');
        const { store, toast } = createStore({ getStudent: () => throwError(() => failure) });
        await stable();

        //when
        const details = await store.loadDetails(NOA.id);

        //then
        expect(details).toBeNull();
        expect(toast.apiError).toHaveBeenCalledWith(failure);
    });

    it('saves the details, confirms them and reloads the list', async () => {
        //given
        const { store, toast, api } = createStore();
        await stable();

        //when
        const saved = await store.changeDetails(NOA.id, NOA_CHANGE);

        //then
        expect(saved).toBe(true);
        expect(api.changeStudentDetails).toHaveBeenCalledWith(NOA.id, NOA_CHANGE);
        expect(toast.success).toHaveBeenCalledWith('students.detailsSaved');
        expect(store.refusal()).toBeNull();
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it("keeps the other Student's name for the Edit dialog when the national ID is theirs", async () => {
        //given
        const refusal = problem(HTTP_CONFLICT, 'studentNationalIdAlreadyInUse', { name: 'Omer Shalev' });
        const { store, toast } = createStore({ changeStudentDetails: () => throwError(() => refusal) });
        await stable();

        //when
        const saved = await store.changeDetails(NOA.id, NOA_CHANGE);

        //then
        expect(saved).toBe(false);
        expect(toast.success).not.toHaveBeenCalled();
        expect(store.refusal()).toEqual({
            kind: StudentRefusalKind.nationalIdInUse,
            message: 'translated refusal',
            existingStudentName: isolateDirection('Omer Shalev'),
        });
        expect(store.isMutating()).toBe(false);
    });

    it('deactivates a Student, names them in the confirmation and reloads the list', async () => {
        //given
        const { store, toast, api } = createStore();
        await stable();

        //when
        const done = await store.deactivate(NOA);

        //then
        expect(done).toBe(true);
        expect(api.deactivateStudent).toHaveBeenCalledWith(NOA.id);
        expect(toast.success).toHaveBeenCalledWith('students.deactivated', {
            key: 'students.deactivatedDetail',
            params: { name: isolateDirection('Noa Mizrahi') },
        });
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it.each([
        [HTTP_CONFLICT, 'studentAlreadyDeactivated'],
        [HTTP_NOT_FOUND, 'studentNotFound'],
    ])('treats a %i %s on Deactivate as stale: toast, reload, close', async (status, code) => {
        //given
        const failure = problem(status, code);
        const { store, toast, api } = createStore({ deactivateStudent: () => throwError(() => failure) });
        await stable();

        //when
        const done = await store.deactivate(NOA);

        //then
        expect(done).toBe(true);
        expect(toast.apiError).toHaveBeenCalledWith(failure);
        expect(store.refusal()).toBeNull();
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it('keeps any other Deactivate failure in the dialog', async () => {
        //given
        const failure = new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR });
        const { store, toast } = createStore({ deactivateStudent: () => throwError(() => failure) });
        await stable();

        //when
        const done = await store.deactivate(NOA);

        //then
        expect(done).toBe(false);
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(store.refusal()?.message).toBe('translated refusal');
    });

    it('reactivates a Student, names them in the confirmation and reloads the list', async () => {
        //given
        const { store, toast, api } = createStore();
        await stable();

        //when
        await store.reactivate(DANA);

        //then
        expect(api.reactivateStudent).toHaveBeenCalledWith(DANA.id);
        expect(toast.success).toHaveBeenCalledWith('students.reactivated', {
            key: 'students.reactivatedDetail',
            params: { name: isolateDirection('Dana Sasson') },
        });
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('reloads when Reactivate finds the Student already active', async () => {
        //given
        const failure = problem(HTTP_CONFLICT, 'studentAlreadyActive');
        const { store, toast, api } = createStore({ reactivateStudent: () => throwError(() => failure) });
        await stable();

        //when
        await store.reactivate(DANA);

        //then
        expect(toast.apiError).toHaveBeenCalledWith(failure);
        await vi.waitFor(() => expect(api.findStudents).toHaveBeenCalledTimes(2));
    });

    it('reports any other Reactivate failure without reloading', async () => {
        //given
        const failure = new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR });
        const { store, toast, api } = createStore({ reactivateStudent: () => throwError(() => failure) });
        await stable();

        //when
        await store.reactivate(DANA);
        await stable();

        //then
        expect(toast.apiError).toHaveBeenCalledWith(failure);
        expect(api.findStudents).toHaveBeenCalledTimes(1);
    });
```

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/state/students.store.spec.ts`
Expected: FAIL (TypeScript: `Property 'loadDetails' does not exist on type 'StudentsStore'`, and the refusal objects lack `existingStudentName`).

- [ ] **Step 6: Implement the store commands**

In `client\src\app\features\students\state\students.store.ts`:

1. Replace the imports block's domain imports so it reads (keep the other imports as they are):

```ts
import { StudentRefusal, refusalKindOf } from '../domain/student-refusal';
import { CarOption } from '../domain/car-option.model';
import { NewStudent } from '../domain/new-student.model';
import { DEFAULT_STUDENT_FILTERS, StudentFilters } from '../domain/student-filters.model';
import { visibleStudents } from '../domain/student-list';
import { StudentDetailsChange } from '../domain/student-details-change.model';
import { StudentDetails } from '../domain/student-details.model';
import { StudentRow } from '../domain/student-row.model';
import { StudentStatusFilter } from '../domain/student-status-filter.enum';
import { Student } from '../domain/student.model';
import { TeacherOption } from '../domain/teacher-option.model';
```

(Step 1's `sed` already turned the first line's names into `StudentRefusal`; the two `StudentDetails*` imports are new.)

2. Add this constant after `ISOLATED_REFUSAL_PARAMS`:

```ts
const EXISTING_STUDENT_NAME_PARAM = 'name';
const STALE_TOGGLE_CODES: ReadonlySet<string> = new Set([
    'studentAlreadyDeactivated',
    'studentAlreadyActive',
    'studentNotFound',
]);
```

and change `ISOLATED_REFUSAL_PARAMS` to reuse it:

```ts
const ISOLATED_REFUSAL_PARAMS = [EXISTING_STUDENT_NAME_PARAM];
```

(declare `EXISTING_STUDENT_NAME_PARAM` above `ISOLATED_REFUSAL_PARAMS`).

3. In `create`, replace the `catch` body

```ts
            this.refusalState.set({
                kind: refusalKindOf(problemCodeOf(error)),
                message: this.toast.messageOf(error, ISOLATED_REFUSAL_PARAMS),
            });
            return false;
```

with

```ts
            this.refusalState.set(this.refusalOf(error));
            return false;
```

4. Add these methods after `create` (before the private `teacherNameOf`):

```ts
    async loadDetails(studentId: string): Promise<StudentDetails | null> {
        this.mutating.set(true);

        try {
            return await firstValueFrom(this.api.getStudent(studentId));
        } catch (error) {
            this.toast.apiError(error);
            return null;
        } finally {
            this.mutating.set(false);
        }
    }

    async changeDetails(studentId: string, change: StudentDetailsChange): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentDetails(studentId, change));
            this.toast.success('students.detailsSaved');
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async deactivate(student: Student): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.deactivateStudent(student.id));
            this.toast.success('students.deactivated', {
                key: 'students.deactivatedDetail',
                params: { name: isolateDirection(student.name) },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            if (!isStaleToggle(error)) {
                this.refusalState.set(this.refusalOf(error));
                return false;
            }

            this.toast.apiError(error);
            this.studentsResource.reload();
            return true;
        } finally {
            this.mutating.set(false);
        }
    }

    async reactivate(student: Student): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.reactivateStudent(student.id));
            this.toast.success('students.reactivated', {
                key: 'students.reactivatedDetail',
                params: { name: isolateDirection(student.name) },
            });
            this.studentsResource.reload();
        } catch (error) {
            this.toast.apiError(error);

            if (isStaleToggle(error)) {
                this.studentsResource.reload();
            }
        } finally {
            this.mutating.set(false);
        }
    }

    private refusalOf(error: unknown): StudentRefusal {
        return {
            kind: refusalKindOf(problemCodeOf(error)),
            message: this.toast.messageOf(error, ISOLATED_REFUSAL_PARAMS),
            existingStudentName: existingStudentNameOf(error),
        };
    }
```

5. Replace the module-level `problemCodeOf` function at the bottom of the file with these three functions:

```ts
function problemOf(error: unknown): ProblemDetails | null {
    if (!(error instanceof HttpErrorResponse)) {
        return null;
    }

    return error.error as ProblemDetails | null;
}

function problemCodeOf(error: unknown): string | undefined {
    return problemOf(error)?.code;
}

function existingStudentNameOf(error: unknown): string | null {
    const name = problemOf(error)?.params?.[EXISTING_STUDENT_NAME_PARAM];

    return name ? isolateDirection(name) : null;
}

function isStaleToggle(error: unknown): boolean {
    const code = problemCodeOf(error);

    return code !== undefined && STALE_TOGGLE_CODES.has(code);
}
```

(`StudentDetails` extends `Student`, and `GetStudentResponse` has the same shape, so `firstValueFrom(this.api.getStudent(...))` is returned as is, like `findStudents` feeds `Student[]`.)

- [ ] **Step 7: Run the students specs and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: PASS (store, domain and the Add Student dialog spec after the rename); the build succeeds.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/students
git commit -m "feat(students): store commands to load and change a Student's details, deactivate and reactivate (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
