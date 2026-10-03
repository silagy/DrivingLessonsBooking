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
