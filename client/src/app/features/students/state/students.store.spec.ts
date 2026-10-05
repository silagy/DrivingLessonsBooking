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
