import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { TeachersApiService } from '../data/teachers-api.service';
import { TeachersStore } from './teachers.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;
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
});
