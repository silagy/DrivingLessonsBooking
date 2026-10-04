import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { isolateDirection } from '../../../shared/text/isolate-direction';
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

const DELETED_USER: ItemForFindUsersResponse = {
    id: 'user-nahum',
    name: 'Gil Nahum',
    signInEmail: 'gil@school.example',
    role: Role.teacher,
    teacherId: 'teacher-nahum',
    teacherName: 'Gil Nahum',
    isDeleted: true,
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

const SELF_REFUSAL = new HttpErrorResponse({
    status: HTTP_CONFLICT,
    error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userMustNotDeleteSelf' },
});

function createStore(
    findUsers: () => Observable<ItemForFindUsersResponse[]>,
    createUser: () => Observable<CreateUserResponse>,
    deleteUser: () => Observable<void> = () => of(undefined),
    restoreUser: () => Observable<void> = () => of(undefined),
): UsersStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            UsersStore,
            {
                provide: UsersApiService,
                useValue: {
                    findUsers: vi.fn(findUsers),
                    createUser: vi.fn(createUser),
                    deleteUser: vi.fn(deleteUser),
                    restoreUser: vi.fn(restoreUser),
                },
            },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(() => of(TEACHERS)) } },
            {
                provide: ToastService,
                useValue: { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') },
            },
            { provide: AuthService, useValue: { userId: signal('user-owner') } },
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

    it('loads the Users again when asked to retry', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR]), () => NEVER);
        await stable();
        const api = TestBed.inject(UsersApiService);

        //when
        store.reload();

        //then
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
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

    it('loads the Teachers again for a new store instance', async () => {
        //given
        createStore(() => of([ADMINISTRATOR]), () => NEVER);
        await stable();
        const teacherOptionsApi = TestBed.inject(TeacherOptionsApiService);

        //when
        TestBed.runInInjectionContext(() => new UsersStore());
        await stable();

        //then
        expect(teacherOptionsApi.findTeachers).toHaveBeenCalledTimes(2);
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

    it('knows which User is signed in', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);

        //expected
        expect(store.currentUserId()).toBe('user-owner');
    });

    it('deletes the User, confirms it and reloads the list', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, TEACHER_USER]), () => NEVER);
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(TEACHER_USER.id);

        //then
        expect(deleted).toBe(true);
        expect(api.deleteUser).toHaveBeenCalledWith('user-levi');
        expect(toast.success).toHaveBeenCalledWith('users.deleted');
        expect(store.deleteRefusal()).toBeNull();
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('keeps the refusal for the Delete dialog instead of a toast', async () => {
        //given
        const store = createStore(
            () => of([ADMINISTRATOR]),
            () => NEVER,
            () => throwError(() => SELF_REFUSAL),
        );
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        const deleted = await store.delete(ADMINISTRATOR.id);

        //then
        expect(deleted).toBe(false);
        expect(toast.messageOf).toHaveBeenCalledWith(SELF_REFUSAL);
        expect(store.deleteRefusal()).toBe('translated refusal');
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });

    it('clears the refusal so a reopened Delete dialog starts clean', async () => {
        //given
        const store = createStore(
            () => of([ADMINISTRATOR]),
            () => NEVER,
            () => throwError(() => SELF_REFUSAL),
        );
        await stable();
        await store.delete(ADMINISTRATOR.id);

        //when
        store.clearDeleteRefusal();

        //then
        expect(store.deleteRefusal()).toBeNull();
    });

    it('restores the User, names them in the confirmation and reloads the list', async () => {
        //given
        const store = createStore(() => of([ADMINISTRATOR, DELETED_USER]), () => NEVER);
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.restore(DELETED_USER);

        //then
        expect(api.restoreUser).toHaveBeenCalledWith('user-nahum');
        expect(toast.success).toHaveBeenCalledWith('users.restored', {
            key: 'users.restoredDetail',
            params: { name: isolateDirection('Gil Nahum') },
        });
        await vi.waitFor(() => expect(api.findUsers).toHaveBeenCalledTimes(2));
        expect(store.isMutating()).toBe(false);
    });

    it('shows a Restore refusal as a toast and keeps the list', async () => {
        //given
        const refusal = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userAlreadyActive' },
        });
        const store = createStore(
            () => of([ADMINISTRATOR, DELETED_USER]),
            () => NEVER,
            () => of(undefined),
            () => throwError(() => refusal),
        );
        await stable();
        const api = TestBed.inject(UsersApiService);
        const toast = TestBed.inject(ToastService);

        //when
        await store.restore(DELETED_USER);

        //then
        expect(toast.apiError).toHaveBeenCalledWith(refusal);
        expect(toast.success).not.toHaveBeenCalled();
        expect(api.findUsers).toHaveBeenCalledTimes(1);
        expect(store.isMutating()).toBe(false);
    });
});
