# Task 3 of 6: The `students` client feature: data services and the signals store

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires task 2 committed. Work on branch `93-students-screen`. Read README decisions 10 to 13 and 17 first.

**Files:**
- Create: `client\src\app\features\students\domain\transmission.enum.ts`
- Create: `client\src\app\features\students\domain\student.model.ts`
- Create: `client\src\app\features\students\domain\student-row.model.ts`
- Create: `client\src\app\features\students\domain\student-status-filter.enum.ts`
- Create: `client\src\app\features\students\domain\student-filters.model.ts`
- Create: `client\src\app\features\students\domain\student-list.ts`
- Create: `client\src\app\features\students\domain\teacher-option.model.ts`
- Create: `client\src\app\features\students\domain\car-option.model.ts`
- Create: `client\src\app\features\students\domain\new-student.model.ts`
- Create: `client\src\app\features\students\domain\add-student-refusal.ts`
- Create: `client\src\app\features\students\data\item-for-find-students.response.ts`
- Create: `client\src\app\features\students\data\create-student.request.ts`
- Create: `client\src\app\features\students\data\create-student.response.ts`
- Create: `client\src\app\features\students\data\students-api.service.ts`
- Create: `client\src\app\features\students\data\item-for-find-teachers.response.ts`
- Create: `client\src\app\features\students\data\teacher-options-api.service.ts`
- Create: `client\src\app\features\students\data\item-for-find-cars.response.ts`
- Create: `client\src\app\features\students\data\car-options-api.service.ts`
- Create: `client\src\app\features\students\state\students.store.ts`
- Test: `client\src\app\features\students\state\students.store.spec.ts`

**Interfaces:**
- Consumes: `GET api/students/find` (items carry `carTransmission` since task 1), `POST api/students` (task 2: body `CreateStudentRequest`, 201 `{ id }`, 404/409 ProblemDetails with `code` and, for `studentNationalIdAlreadyInUse`, `params.name`), `GET api/teachers/find` (`{ id, name, contactEmail }[]`), `GET api/cars/find` (`{ id, name, type, transmission, assignedTeachers: { id, name }[] }[]`), `ToastService.success(key, detail?)` / `messageOf(error)`, `ProblemDetails` (`shared\models\problem-details.ts`), `isolateDirection(text)` (`shared\text\isolate-direction.ts`).
- Produces (tasks 4 and 5 rely on these exact names):
  - `Transmission` enum (`automatic`, `manual`); `Student`; `StudentRow extends Student { isNew: boolean }`; `StudentStatusFilter` enum (`active`, `inactive`, `all`); `StudentFilters { teacherId: string | null; status: StudentStatusFilter; search: string }` and `DEFAULT_STUDENT_FILTERS`; `TeacherOption { id; name }`; `CarOption { id; name; transmission; teacherIds: readonly string[] }`; `NewStudent { nationalId; name; phone; teacherId; carId; address: string | null; startDate: string | null; licenseType: string | null }`.
  - `visibleStudents(students, filters, newStudentIds): StudentRow[]`.
  - `AddStudentRefusalKind` enum (`nationalIdInUse`, `nationalIdInvalid`, `staleCar`, `other`), `AddStudentRefusal { kind; message }`, `refusalKindOf(code: string | undefined)`.
  - `StudentsStore` (`@Injectable()`, provided by the page in task 4), readonly signals: `students`, `teacherOptions` (sorted by name), `carOptions`, `filters`, `visibleStudents`, `isLoading`, `loadError` (`'students.loadFailed'` or null), `isEmpty`, `hasNoMatch`, `footerKey` (`'students.footer'` or `'students.footerOne'`), `isMutating`, `refusal`; methods `selectTeacher(teacherId: string | null)`, `selectStatus(status)`, `search(text)`, `clearFilters()`, `reload()`, `refreshCarOptions()`, `clearRefusal()`, `create(newStudent: NewStudent): Promise<boolean>`.
  - Toast keys used by `create` (translations land in task 5): `students.added`, `students.addedDetail` with `{ name, teacher }`.

**Why:** AC "New `students` lazy client feature (domain · data · state · ui) with a signals-only store" and the list's behavior (design frames 2a, 2b, 2g, 2h, 2k, 2l, 3h). Filtering and search run on the loaded list (README decision 11), so the store owns them as `computed()`; the rules live in a pure `domain` function. The feature has its own Teacher and Car data services because features never import each other.

- [ ] **Step 1: Add the domain models**

Create `client\src\app\features\students\domain\transmission.enum.ts`:

```typescript
export enum Transmission {
    automatic = 'automatic',
    manual = 'manual',
}
```

Create `client\src\app\features\students\domain\student.model.ts`:

```typescript
import { Transmission } from './transmission.enum';

export interface Student {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    carTransmission: Transmission;
    isActive: boolean;
}
```

Create `client\src\app\features\students\domain\student-row.model.ts`:

```typescript
import { Student } from './student.model';

export interface StudentRow extends Student {
    isNew: boolean;
}
```

Create `client\src\app\features\students\domain\student-status-filter.enum.ts`:

```typescript
export enum StudentStatusFilter {
    active = 'active',
    inactive = 'inactive',
    all = 'all',
}
```

Create `client\src\app\features\students\domain\student-filters.model.ts`:

```typescript
import { StudentStatusFilter } from './student-status-filter.enum';

export interface StudentFilters {
    teacherId: string | null;
    status: StudentStatusFilter;
    search: string;
}

export const DEFAULT_STUDENT_FILTERS: StudentFilters = {
    teacherId: null,
    status: StudentStatusFilter.active,
    search: '',
};
```

Create `client\src\app\features\students\domain\teacher-option.model.ts`:

```typescript
export interface TeacherOption {
    id: string;
    name: string;
}
```

Create `client\src\app\features\students\domain\car-option.model.ts`:

```typescript
import { Transmission } from './transmission.enum';

export interface CarOption {
    id: string;
    name: string;
    transmission: Transmission;
    teacherIds: readonly string[];
}
```

Create `client\src\app\features\students\domain\new-student.model.ts`:

```typescript
export interface NewStudent {
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    carId: string;
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
```

- [ ] **Step 2: Add the list rules and the refusal kinds**

Create `client\src\app\features\students\domain\student-list.ts`:

```typescript
import { StudentFilters } from './student-filters.model';
import { StudentRow } from './student-row.model';
import { StudentStatusFilter } from './student-status-filter.enum';
import { Student } from './student.model';

export function visibleStudents(
    students: readonly Student[],
    filters: StudentFilters,
    newStudentIds: ReadonlySet<string>,
): StudentRow[] {
    const rows = students
        .filter((student) => matchesFilters(student, filters))
        .map((student) => ({ ...student, isNew: newStudentIds.has(student.id) }));

    return [
        ...rows.filter((row) => row.isNew),
        ...rows.filter((row) => !row.isNew && row.isActive),
        ...rows.filter((row) => !row.isNew && !row.isActive),
    ];
}

function matchesFilters(student: Student, filters: StudentFilters): boolean {
    return matchesTeacher(student, filters.teacherId)
        && matchesStatus(student, filters.status)
        && matchesSearch(student, filters.search);
}

function matchesTeacher(student: Student, teacherId: string | null): boolean {
    return teacherId === null || student.teacherId === teacherId;
}

function matchesStatus(student: Student, status: StudentStatusFilter): boolean {
    switch (status) {
        case StudentStatusFilter.active:
            return student.isActive;
        case StudentStatusFilter.inactive:
            return !student.isActive;
        default:
            return true;
    }
}

function matchesSearch(student: Student, search: string): boolean {
    const query = search.trim().toLocaleLowerCase();

    if (!query) {
        return true;
    }

    return student.nationalId.includes(query) || student.name.toLocaleLowerCase().includes(query);
}
```

(The server already orders by Teacher name, then Student name; the stable `filter` keeps that order inside each group.)

Create `client\src\app\features\students\domain\add-student-refusal.ts`:

```typescript
export enum AddStudentRefusalKind {
    nationalIdInUse = 'nationalIdInUse',
    nationalIdInvalid = 'nationalIdInvalid',
    staleCar = 'staleCar',
    other = 'other',
}

export interface AddStudentRefusal {
    kind: AddStudentRefusalKind;
    message: string;
}

const KIND_BY_CODE: Partial<Record<string, AddStudentRefusalKind>> = {
    studentNationalIdAlreadyInUse: AddStudentRefusalKind.nationalIdInUse,
    nationalIdMustBeDigits: AddStudentRefusalKind.nationalIdInvalid,
    nationalIdMustBeAtMostNineDigits: AddStudentRefusalKind.nationalIdInvalid,
    nationalIdMustHaveValidCheckDigit: AddStudentRefusalKind.nationalIdInvalid,
    studentCarMustBeAssignedToTeacher: AddStudentRefusalKind.staleCar,
    carNotFound: AddStudentRefusalKind.staleCar,
};

export function refusalKindOf(code: string | undefined): AddStudentRefusalKind {
    if (!code) {
        return AddStudentRefusalKind.other;
    }

    return KIND_BY_CODE[code] ?? AddStudentRefusalKind.other;
}
```

- [ ] **Step 3: Add the data layer**

Create `client\src\app\features\students\data\item-for-find-students.response.ts`:

```typescript
import { Transmission } from '../domain/transmission.enum';

export interface ItemForFindStudentsResponse {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    carTransmission: Transmission;
    isActive: boolean;
}
```

Create `client\src\app\features\students\data\create-student.request.ts`:

```typescript
export interface CreateStudentRequest {
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    carId: string;
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
```

Create `client\src\app\features\students\data\create-student.response.ts`:

```typescript
export interface CreateStudentResponse {
    id: string;
}
```

Create `client\src\app\features\students\data\students-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateStudentRequest } from './create-student.request';
import { CreateStudentResponse } from './create-student.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class StudentsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/students';

    findStudents(): Observable<ItemForFindStudentsResponse[]> {
        return this.http.get<ItemForFindStudentsResponse[]>(`${this.baseUrl}/find`);
    }

    createStudent(request: CreateStudentRequest): Observable<CreateStudentResponse> {
        return this.http.post<CreateStudentResponse>(this.baseUrl, request);
    }
}
```

Create `client\src\app\features\students\data\item-for-find-teachers.response.ts`:

```typescript
export interface ItemForFindTeachersResponse {
    id: string;
    name: string;
    contactEmail: string;
}
```

Create `client\src\app\features\students\data\teacher-options-api.service.ts`:

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

Create `client\src\app\features\students\data\item-for-find-cars.response.ts`:

```typescript
import { Transmission } from '../domain/transmission.enum';

export interface TeacherForFindCarsResponse {
    id: string;
    name: string;
}

export interface ItemForFindCarsResponse {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
    assignedTeachers: TeacherForFindCarsResponse[];
}
```

Create `client\src\app\features\students\data\car-options-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ItemForFindCarsResponse } from './item-for-find-cars.response';

@Injectable({ providedIn: 'root' })
export class CarOptionsApiService {
    private readonly http = inject(HttpClient);

    findCars(): Observable<ItemForFindCarsResponse[]> {
        return this.http.get<ItemForFindCarsResponse[]>('api/cars/find');
    }
}
```

- [ ] **Step 4: Write the failing store specs**

Create `client\src\app\features\students\state\students.store.spec.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { CarOptionsApiService } from '../data/car-options-api.service';
import { CreateStudentResponse } from '../data/create-student.response';
import { ItemForFindCarsResponse } from '../data/item-for-find-cars.response';
import { ItemForFindStudentsResponse } from '../data/item-for-find-students.response';
import { ItemForFindTeachersResponse } from '../data/item-for-find-teachers.response';
import { StudentsApiService } from '../data/students-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { AddStudentRefusalKind } from '../domain/add-student-refusal';
import { NewStudent } from '../domain/new-student.model';
import { DEFAULT_STUDENT_FILTERS } from '../domain/student-filters.model';
import { StudentStatusFilter } from '../domain/student-status-filter.enum';
import { Transmission } from '../domain/transmission.enum';
import { StudentsStore } from './students.store';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_INTERNAL_SERVER_ERROR = 500;

const DANA: ItemForFindStudentsResponse = {
    id: 'student-dana',
    nationalId: '311078542',
    name: 'Dana Sasson',
    phone: '052-9038816',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
    isActive: false,
};

const NOA: ItemForFindStudentsResponse = {
    id: 'student-noa',
    nationalId: '205374188',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    isActive: true,
};

const TAMAR: ItemForFindStudentsResponse = {
    id: 'student-tamar',
    nationalId: '318204576',
    name: 'Tamar Sagi',
    phone: '054-3321908',
    teacherId: 'teacher-yael',
    teacherName: 'Yael Carmi',
    carId: 'car-picanto',
    carName: 'Picanto Red',
    carTransmission: Transmission.automatic,
    isActive: true,
};

const YONI: ItemForFindStudentsResponse = {
    id: 'student-yoni',
    nationalId: '204667129',
    name: 'Yonatan Kedem',
    phone: '054-7710352',
    teacherId: 'teacher-yael',
    teacherName: 'Yael Carmi',
    carId: 'car-picanto',
    carName: 'Picanto Red',
    carTransmission: Transmission.automatic,
    isActive: false,
};

const SHAKED: ItemForFindStudentsResponse = {
    id: 'student-shaked',
    nationalId: '214503986',
    name: 'Shaked Navon',
    phone: '050-3318842',
    teacherId: 'teacher-yael',
    teacherName: 'Yael Carmi',
    carId: 'car-picanto',
    carName: 'Picanto Red',
    carTransmission: Transmission.automatic,
    isActive: true,
};

const STUDENTS = [DANA, NOA, TAMAR, YONI];

const TEACHERS: ItemForFindTeachersResponse[] = [
    { id: 'teacher-yael', name: 'Yael Carmi', contactEmail: 'yael@school.example' },
    { id: 'teacher-ronit', name: 'Ronit Avraham', contactEmail: 'ronit@school.example' },
];

const CARS: ItemForFindCarsResponse[] = [
    {
        id: 'car-i20',
        name: 'i20 Silver',
        type: 'i20',
        transmission: Transmission.manual,
        assignedTeachers: [
            { id: 'teacher-ronit', name: 'Ronit Avraham' },
            { id: 'teacher-yael', name: 'Yael Carmi' },
        ],
    },
];

const NEW_STUDENT: NewStudent = {
    nationalId: '214503986',
    name: 'Shaked Navon',
    phone: '050-3318842',
    teacherId: 'teacher-yael',
    carId: 'car-picanto',
    address: null,
    startDate: null,
    licenseType: null,
};

function problem(status: number, code: string): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: { status, title: 'Refused', code } });
}

interface ApiStubs {
    findStudents: () => Observable<ItemForFindStudentsResponse[]>;
    createStudent: () => Observable<CreateStudentResponse>;
    findTeachers: () => Observable<ItemForFindTeachersResponse[]>;
    findCars: () => Observable<ItemForFindCarsResponse[]>;
}

function createStore(overrides: Partial<ApiStubs> = {}) {
    const stubs: ApiStubs = {
        findStudents: () => of(STUDENTS),
        createStudent: () => of({ id: SHAKED.id }),
        findTeachers: () => of(TEACHERS),
        findCars: () => of(CARS),
        ...overrides,
    };
    const toast = { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') };
    const findCars = vi.fn(stubs.findCars);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            StudentsStore,
            {
                provide: StudentsApiService,
                useValue: { findStudents: vi.fn(stubs.findStudents), createStudent: vi.fn(stubs.createStudent) },
            },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(stubs.findTeachers) } },
            { provide: CarOptionsApiService, useValue: { findCars } },
            { provide: ToastService, useValue: toast },
        ],
    });

    return { store: TestBed.inject(StudentsStore), toast, findCars };
}

async function stable(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

function idsOf(rows: { id: string }[]): string[] {
    return rows.map((row) => row.id);
}

describe('StudentsStore', () => {
    it('shows only the active Students by default, in the server order', async () => {
        //given
        const { store } = createStore();

        //when
        await stable();

        //then
        expect(store.filters()).toEqual(DEFAULT_STUDENT_FILTERS);
        expect(idsOf(store.visibleStudents())).toEqual(['student-noa', 'student-tamar']);
        expect(store.isLoading()).toBe(false);
    });

    it('lists the Inactive Students last when showing all', async () => {
        //given
        const { store } = createStore();
        await stable();

        //when
        store.selectStatus(StudentStatusFilter.all);

        //then
        expect(idsOf(store.visibleStudents())).toEqual(['student-noa', 'student-tamar', 'student-dana', 'student-yoni']);
    });

    it('shows only the Inactive Students', async () => {
        //given
        const { store } = createStore();
        await stable();

        //when
        store.selectStatus(StudentStatusFilter.inactive);

        //then
        expect(idsOf(store.visibleStudents())).toEqual(['student-dana', 'student-yoni']);
    });

    it('filters by Teacher', async () => {
        //given
        const { store } = createStore();
        await stable();

        //when
        store.selectStatus(StudentStatusFilter.all);
        store.selectTeacher('teacher-yael');

        //then
        expect(idsOf(store.visibleStudents())).toEqual(['student-tamar', 'student-yoni']);
    });

    it('searches by part of a national ID', async () => {
        //given
        const { store } = createStore();
        await stable();
        store.selectStatus(StudentStatusFilter.all);

        //when
        store.search('7418');

        //then
        expect(idsOf(store.visibleStudents())).toEqual(['student-noa']);
    });

    it('searches by name, ignoring case and surrounding spaces', async () => {
        //given
        const { store } = createStore();
        await stable();
        store.selectStatus(StudentStatusFilter.all);

        //when
        store.search('  tamar ');

        //then
        expect(idsOf(store.visibleStudents())).toEqual(['student-tamar']);
    });

    it('has no match when the filters hide every Student, until the filters are cleared', async () => {
        //given
        const { store } = createStore();
        await stable();
        store.selectStatus(StudentStatusFilter.all);
        store.selectTeacher('teacher-oren');
        expect(store.hasNoMatch()).toBe(true);
        expect(store.visibleStudents()).toEqual([]);

        //when
        store.clearFilters();

        //then
        expect(store.filters()).toEqual(DEFAULT_STUDENT_FILTERS);
        expect(store.hasNoMatch()).toBe(false);
        expect(idsOf(store.visibleStudents())).toEqual(['student-noa', 'student-tamar']);
    });

    it('is empty, not unmatched, when there are no Students at all', async () => {
        //given
        const { store } = createStore({ findStudents: () => of([]) });

        //when
        await stable();

        //then
        expect(store.isEmpty()).toBe(true);
        expect(store.hasNoMatch()).toBe(false);
    });

    it('reports the load error instead of throwing when the Students fail to load', async () => {
        //given
        const { store } = createStore({
            findStudents: () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        });

        //when
        await stable();

        //then
        expect(store.students()).toEqual([]);
        expect(store.isLoading()).toBe(false);
        expect(store.isEmpty()).toBe(false);
        expect(store.loadError()).toBe('students.loadFailed');
    });

    it('counts a single Student with the singular footer', async () => {
        //given
        const { store } = createStore();
        await stable();
        expect(store.footerKey()).toBe('students.footer');

        //when
        store.search('noa');

        //then
        expect(store.footerKey()).toBe('students.footerOne');
    });

    it('orders the Teacher options by name and gives each Car its Teachers', async () => {
        //given
        const { store } = createStore();

        //when
        await stable();

        //then
        expect(store.teacherOptions()).toEqual([
            { id: 'teacher-ronit', name: 'Ronit Avraham' },
            { id: 'teacher-yael', name: 'Yael Carmi' },
        ]);
        expect(store.carOptions()).toEqual([
            {
                id: 'car-i20',
                name: 'i20 Silver',
                transmission: Transmission.manual,
                teacherIds: ['teacher-ronit', 'teacher-yael'],
            },
        ]);
    });

    it("adds the Student, confirms it with the Teacher's name and lists it first as new", async () => {
        //given
        let created = false;
        const { store, toast } = createStore({
            findStudents: () => of(created ? [...STUDENTS, SHAKED] : STUDENTS),
            createStudent: () => {
                created = true;
                return of({ id: SHAKED.id });
            },
        });
        await stable();

        //when
        const saved = await store.create(NEW_STUDENT);
        await stable();

        //then
        expect(saved).toBe(true);
        expect(toast.success).toHaveBeenCalledWith('students.added', {
            key: 'students.addedDetail',
            params: { name: isolateDirection('Shaked Navon'), teacher: isolateDirection('Yael Carmi') },
        });
        expect(idsOf(store.visibleStudents())).toEqual(['student-shaked', 'student-noa', 'student-tamar']);
        expect(store.visibleStudents().map((row) => row.isNew)).toEqual([true, false, false]);
        expect(store.refusal()).toBeNull();
        expect(store.isMutating()).toBe(false);
    });

    it('keeps the refusal for the dialog when the national ID is in use', async () => {
        //given
        const refusal = problem(HTTP_CONFLICT, 'studentNationalIdAlreadyInUse');
        const { store, toast } = createStore({ createStudent: () => throwError(() => refusal) });
        await stable();

        //when
        const saved = await store.create(NEW_STUDENT);

        //then
        expect(saved).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(refusal);
        expect(toast.success).not.toHaveBeenCalled();
        expect(store.refusal()).toEqual({
            kind: AddStudentRefusalKind.nationalIdInUse,
            message: 'translated refusal',
        });
        expect(store.isMutating()).toBe(false);
    });

    it.each(['nationalIdMustBeDigits', 'nationalIdMustBeAtMostNineDigits', 'nationalIdMustHaveValidCheckDigit'])(
        'treats %s as an invalid national ID',
        async (code) => {
            //given
            const { store } = createStore({ createStudent: () => throwError(() => problem(HTTP_CONFLICT, code)) });
            await stable();

            //when
            await store.create(NEW_STUDENT);

            //then
            expect(store.refusal()?.kind).toBe(AddStudentRefusalKind.nationalIdInvalid);
        },
    );

    it.each([
        [HTTP_CONFLICT, 'studentCarMustBeAssignedToTeacher'],
        [HTTP_NOT_FOUND, 'carNotFound'],
    ])('treats a %i %s as a stale Car', async (status, code) => {
        //given
        const { store } = createStore({ createStudent: () => throwError(() => problem(status, code)) });
        await stable();

        //when
        await store.create(NEW_STUDENT);

        //then
        expect(store.refusal()?.kind).toBe(AddStudentRefusalKind.staleCar);
    });

    it('treats any other refusal as other', async () => {
        //given
        const { store } = createStore({
            createStudent: () => throwError(() => problem(HTTP_CONFLICT, 'phoneNumberMustBeValid')),
        });
        await stable();

        //when
        await store.create(NEW_STUDENT);

        //then
        expect(store.refusal()).toEqual({ kind: AddStudentRefusalKind.other, message: 'translated refusal' });
    });

    it('reloads the Car options and forgets the stale refusal', async () => {
        //given
        const { store, findCars } = createStore({
            createStudent: () => throwError(() => problem(HTTP_CONFLICT, 'studentCarMustBeAssignedToTeacher')),
        });
        await stable();
        await store.create(NEW_STUDENT);

        //when
        store.refreshCarOptions();

        //then
        expect(store.refusal()).toBeNull();
        await vi.waitFor(() => expect(findCars).toHaveBeenCalledTimes(2));
    });
});
```

- [ ] **Step 5: Run the specs to see them fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/state/students.store.spec.ts
```

Expected: FAIL: the spec can't resolve `./students.store`.

- [ ] **Step 6: Write the store**

Create `client\src\app\features\students\state\students.store.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { ProblemDetails } from '../../../shared/models/problem-details';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { CarOptionsApiService } from '../data/car-options-api.service';
import { ItemForFindCarsResponse } from '../data/item-for-find-cars.response';
import { StudentsApiService } from '../data/students-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { AddStudentRefusal, refusalKindOf } from '../domain/add-student-refusal';
import { CarOption } from '../domain/car-option.model';
import { NewStudent } from '../domain/new-student.model';
import { DEFAULT_STUDENT_FILTERS, StudentFilters } from '../domain/student-filters.model';
import { visibleStudents } from '../domain/student-list';
import { StudentRow } from '../domain/student-row.model';
import { StudentStatusFilter } from '../domain/student-status-filter.enum';
import { Student } from '../domain/student.model';
import { TeacherOption } from '../domain/teacher-option.model';

const SINGLE_STUDENT_COUNT = 1;

@Injectable()
export class StudentsStore {
    private readonly api = inject(StudentsApiService);
    private readonly teacherOptionsApi = inject(TeacherOptionsApiService);
    private readonly carOptionsApi = inject(CarOptionsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);
    private readonly refusalState = signal<AddStudentRefusal | null>(null);
    private readonly filtersState = signal<StudentFilters>(DEFAULT_STUDENT_FILTERS);
    private readonly newStudentIds = signal<ReadonlySet<string>>(new Set<string>());

    private readonly studentsResource = resource({
        loader: () => firstValueFrom(this.api.findStudents()),
    });

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teacherOptionsApi.findTeachers()),
    });

    private readonly carsResource = resource({
        loader: () => firstValueFrom(this.carOptionsApi.findCars()),
    });

    readonly students = computed<Student[]>(() =>
        this.studentsResource.hasValue() ? this.studentsResource.value() : [],
    );

    readonly teacherOptions = computed<TeacherOption[]>(() => {
        const teachers = this.teachersResource.hasValue() ? this.teachersResource.value() : [];

        return teachers
            .map((teacher) => ({ id: teacher.id, name: teacher.name }))
            .sort((first, second) => first.name.localeCompare(second.name));
    });

    readonly carOptions = computed<CarOption[]>(() => {
        const cars = this.carsResource.hasValue() ? this.carsResource.value() : [];

        return cars.map(toCarOption);
    });

    readonly filters = this.filtersState.asReadonly();

    readonly visibleStudents = computed<StudentRow[]>(() =>
        visibleStudents(this.students(), this.filtersState(), this.newStudentIds()),
    );

    readonly isLoading = computed(() => this.studentsResource.isLoading() && !this.studentsResource.hasValue());
    readonly loadError = computed(() => (this.studentsResource.error() ? 'students.loadFailed' : null));
    readonly isEmpty = computed(() => this.studentsResource.hasValue() && !this.students().length);
    readonly hasNoMatch = computed(() => this.students().length > 0 && !this.visibleStudents().length);
    readonly footerKey = computed(() =>
        this.visibleStudents().length === SINGLE_STUDENT_COUNT ? 'students.footerOne' : 'students.footer',
    );
    readonly isMutating = this.mutating.asReadonly();
    readonly refusal = this.refusalState.asReadonly();

    selectTeacher(teacherId: string | null): void {
        this.filtersState.update((filters) => ({ ...filters, teacherId }));
    }

    selectStatus(status: StudentStatusFilter): void {
        this.filtersState.update((filters) => ({ ...filters, status }));
    }

    search(text: string): void {
        this.filtersState.update((filters) => ({ ...filters, search: text }));
    }

    clearFilters(): void {
        this.filtersState.set(DEFAULT_STUDENT_FILTERS);
    }

    reload(): void {
        this.studentsResource.reload();
        this.teachersResource.reload();
        this.carsResource.reload();
    }

    refreshCarOptions(): void {
        this.refusalState.set(null);
        this.carsResource.reload();
    }

    clearRefusal(): void {
        this.refusalState.set(null);
    }

    async create(newStudent: NewStudent): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            const created = await firstValueFrom(this.api.createStudent(newStudent));
            this.newStudentIds.update((ids) => new Set([...ids, created.id]));
            this.toast.success('students.added', {
                key: 'students.addedDetail',
                params: {
                    name: isolateDirection(newStudent.name),
                    teacher: isolateDirection(this.teacherNameOf(newStudent.teacherId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set({
                kind: refusalKindOf(problemCodeOf(error)),
                message: this.toast.messageOf(error),
            });
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    private teacherNameOf(teacherId: string): string {
        return this.teacherOptions().find((teacher) => teacher.id === teacherId)?.name ?? '';
    }
}

function toCarOption(car: ItemForFindCarsResponse): CarOption {
    return {
        id: car.id,
        name: car.name,
        transmission: car.transmission,
        teacherIds: car.assignedTeachers.map((teacher) => teacher.id),
    };
}

function problemCodeOf(error: unknown): string | undefined {
    if (!(error instanceof HttpErrorResponse)) {
        return undefined;
    }

    const problem = error.error as ProblemDetails | null;

    return problem?.code;
}
```

`refusal.message` is the translated rule from `ToastService.messageOf` (the same source the Users dialogs use), so `errors.studentNationalIdAlreadyInUse` arrives with the existing Student's name already interpolated from `params`. The dialog shows its own design message for `nationalIdInvalid` (task 5).

- [ ] **Step 7: Run the specs to see them pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/state/students.store.spec.ts
```

Expected: PASS, 20 specs (the two `it.each` blocks count once per case).

- [ ] **Step 8: Run the whole client suite and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (including `source-text.spec.ts`), build succeeds. Nothing routes to the feature yet; task 4 does.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/students
git commit -m "feat(client): students feature store with filters, search and Add Student refusals (#93)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
