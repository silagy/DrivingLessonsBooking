import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ChangeMyPasswordRequest } from '../data/change-my-password.request';
import { ChangeMyPasswordResponse } from '../data/change-my-password.response';
import { MeApiService } from '../data/me-api.service';
import { MyPasswordStore } from './my-password.store';

const HTTP_CONFLICT = 409;

const REQUEST: ChangeMyPasswordRequest = { currentPassword: 'Current#2026', newPassword: 'Fresh#2027' };

const RESPONSE: ChangeMyPasswordResponse = {
    accessToken: 'fresh-access-token',
    expiresAtUtc: '2026-10-04T22:00:00Z',
};

const WRONG_CURRENT_PASSWORD = new HttpErrorResponse({
    status: HTTP_CONFLICT,
    error: { status: HTTP_CONFLICT, title: 'Conflict', code: 'userCurrentPasswordMustBeCorrect' },
});

function createStore(changeMyPassword: () => Observable<ChangeMyPasswordResponse>): MyPasswordStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            MyPasswordStore,
            { provide: MeApiService, useValue: { changeMyPassword: vi.fn(changeMyPassword) } },
            {
                provide: ToastService,
                useValue: { success: vi.fn(), apiError: vi.fn(), messageOf: vi.fn(() => 'translated refusal') },
            },
            { provide: AuthService, useValue: { useToken: vi.fn(), logout: vi.fn() } },
        ],
    });

    return TestBed.inject(MyPasswordStore);
}

describe('MyPasswordStore', () => {
    it('changes the password, keeps the User signed in with the fresh token and confirms it', async () => {
        //given
        const store = createStore(() => of(RESPONSE));
        const api = TestBed.inject(MeApiService);
        const auth = TestBed.inject(AuthService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.change(REQUEST);

        //then
        expect(changed).toBe(true);
        expect(api.changeMyPassword).toHaveBeenCalledWith(REQUEST);
        expect(auth.useToken).toHaveBeenCalledWith('fresh-access-token');
        expect(toast.success).toHaveBeenCalledWith('myPassword.changed', { key: 'myPassword.changedDetail' });
        expect(store.refusal()).toBeNull();
        expect(store.isSaving()).toBe(false);
    });

    it('keeps the refusal and the current token when the server refuses', async () => {
        //given
        const store = createStore(() => throwError(() => WRONG_CURRENT_PASSWORD));
        const auth = TestBed.inject(AuthService);
        const toast = TestBed.inject(ToastService);

        //when
        const changed = await store.change(REQUEST);

        //then
        expect(changed).toBe(false);
        expect(store.refusal()).toBe('translated refusal');
        expect(toast.messageOf).toHaveBeenCalledWith(WRONG_CURRENT_PASSWORD);
        expect(toast.success).not.toHaveBeenCalled();
        expect(toast.apiError).not.toHaveBeenCalled();
        expect(auth.useToken).not.toHaveBeenCalled();
        expect(auth.logout).not.toHaveBeenCalled();
        expect(store.isSaving()).toBe(false);
    });

    it('is saving while the change is in flight', () => {
        //given
        const store = createStore(() => NEVER);

        //when
        void store.change(REQUEST);

        //then
        expect(store.isSaving()).toBe(true);
    });

    it('ignores a second change while one is in flight', async () => {
        //given
        const store = createStore(() => NEVER);
        const api = TestBed.inject(MeApiService);
        void store.change(REQUEST);

        //when
        const second = await store.change(REQUEST);

        //then
        expect(second).toBe(false);
        expect(api.changeMyPassword).toHaveBeenCalledTimes(1);
    });

    it('starts a new change without the previous refusal', async () => {
        //given
        let calls = 0;
        const store = createStore(() => {
            calls++;
            return calls === 1 ? throwError(() => WRONG_CURRENT_PASSWORD) : NEVER;
        });
        await store.change(REQUEST);

        //when
        void store.change(REQUEST);

        //then
        expect(store.refusal()).toBeNull();
        expect(store.isSaving()).toBe(true);
    });
});
