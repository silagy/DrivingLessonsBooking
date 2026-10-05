import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Role } from '../../shared/models/role.enum';
import { AuthService } from '../auth.service';
import { SignedInUserApiService } from './signed-in-user-api.service';
import { GetUserResponse } from './get-user.response';
import { SignedInUserStore } from './signed-in-user.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;
const YAEL_TOKEN = 'token-yael';
const DANI_TOKEN = 'token-dani';

const YAEL: GetUserResponse = {
    id: 'user-yael',
    name: 'Yael Carmi',
    signInEmail: 'yael@school.example',
    role: Role.teacher,
    teacherId: 'teacher-yael',
    teacherName: 'Teacher Yael',
};

const DANI: GetUserResponse = {
    id: 'user-dani',
    name: 'Dani Levi',
    signInEmail: 'dani@school.example',
    role: Role.administrator,
    teacherId: null,
    teacherName: null,
};

function storeSignedInWith(
    token: string | null,
    getMe: ReturnType<typeof vi.fn> = vi.fn(() => of(YAEL)),
) {
    const auth = { token: signal<string | null>(token) };

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: AuthService, useValue: auth },
            { provide: SignedInUserApiService, useValue: { getMe } },
        ],
    });

    return { store: TestBed.inject(SignedInUserStore), auth, getMe };
}

async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('SignedInUserStore', () => {
    it('loads the signed-in User once signed in', async () => {
        //given
        const { store, getMe } = storeSignedInWith(YAEL_TOKEN);

        //when
        await settle();

        //then
        expect(store.teacherName()).toBe('Teacher Yael');
        expect(store.teacherName()).toBe('Teacher Yael');
        expect(getMe).toHaveBeenCalledTimes(1);
    });

    it('has no signed-in User when signed out', async () => {
        //given
        const { store, getMe } = storeSignedInWith(null);

        //when
        await settle();

        //then
        expect(getMe).not.toHaveBeenCalled();
        expect(store.teacherName()).toBeNull();
    });

    it('has no linked Teacher name for a User not linked to a Teacher', async () => {
        //given
        const { store } = storeSignedInWith(DANI_TOKEN, vi.fn(() => of(DANI)));

        //when
        await settle();

        //then
        expect(store.teacherName()).toBeNull();
    });

    it('loads the other User when signed in as another User', async () => {
        //given
        const getMe = vi.fn().mockReturnValueOnce(of(YAEL)).mockReturnValue(of(DANI));
        const { store, auth } = storeSignedInWith(YAEL_TOKEN, getMe);
        await settle();

        //when
        auth.token.set(DANI_TOKEN);
        await settle();

        //then
        expect(getMe).toHaveBeenCalledTimes(2);
        expect(store.teacherName()).toBeNull();
    });

    it('forgets the signed-in User on sign out', async () => {
        //given
        const { store, auth, getMe } = storeSignedInWith(YAEL_TOKEN);
        await settle();

        //when
        auth.token.set(null);
        await settle();

        //then
        expect(getMe).toHaveBeenCalledTimes(1);
        expect(store.teacherName()).toBeNull();
    });

    it('has no signed-in User instead of throwing when the load fails', async () => {
        //given
        const failingGetMe = vi.fn(() =>
            throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        );
        const { store } = storeSignedInWith(YAEL_TOKEN, failingGetMe);

        //when
        await settle();

        //then
        expect(store.teacherName()).toBeNull();
    });
});
