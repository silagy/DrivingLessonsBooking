import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { TeachersApiService } from '../data/teachers-api.service';
import { TeachersStore } from './teachers.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;

function failingStore(): TeachersStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            {
                provide: TeachersApiService,
                useValue: {
                    findTeachers: () =>
                        throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return TestBed.inject(TeachersStore);
}

describe('TeachersStore', () => {
    it('reports the load error instead of throwing when the teachers fail to load', async () => {
        //given
        const store = failingStore();

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.teachers()).toEqual([]);
        expect(store.isLoading()).toBe(false);
        expect(store.isEmpty()).toBe(true);
        expect(store.loadError()).toBe('teachers.loadFailed');
    });
});
