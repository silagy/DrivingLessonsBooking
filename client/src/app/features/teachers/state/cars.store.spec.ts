import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { CarsApiService } from '../data/cars-api.service';
import { CarsStore } from './cars.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;
const HTTP_CONFLICT = 409;

function storeWithApi(apiError: ReturnType<typeof vi.fn>, api: Partial<Record<keyof CarsApiService, unknown>>): CarsStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: CarsApiService, useValue: { findCars: () => of([]), ...api } },
            { provide: ToastService, useValue: { success: () => undefined, apiError } },
        ],
    });

    return TestBed.inject(CarsStore);
}

function conflict(code: string, params: Record<string, string>): HttpErrorResponse {
    return new HttpErrorResponse({ status: HTTP_CONFLICT, error: { code, params } });
}

function failingStore(): CarsStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            {
                provide: CarsApiService,
                useValue: {
                    findCars: () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return TestBed.inject(CarsStore);
}

describe('CarsStore', () => {
    it('reports the load error instead of throwing when the cars fail to load', async () => {
        //given
        const store = failingStore();

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.cars()).toEqual([]);
        expect(store.carsByTeacherId().size).toBe(0);
        expect(store.isLoading()).toBe(false);
        expect(store.isEmpty()).toBe(true);
        expect(store.loadError()).toBe('teachers.carsLoadFailed');
    });

    it('isolates the Student names when deleting a Car is refused', async () => {
        //given
        const apiError = vi.fn();
        const refusal = conflict('carMustNotHaveActiveStudents', { count: '1', names: 'Noa Mizrahi' });
        const store = storeWithApi(apiError, { deleteCar: () => throwError(() => refusal) });

        //when
        await store.delete('car-1');

        //then
        expect(apiError).toHaveBeenCalledWith(refusal, ['names']);
    });

    it('isolates the Teacher and Student names when an unassign is refused, and reloads the Cars', async () => {
        //given
        const apiError = vi.fn();
        const findCars = vi.fn(() => of([]));
        const refusal = conflict('teacherAssignmentMustNotHaveActiveStudents', {
            count: '1',
            names: 'Noa Mizrahi',
            teacher: 'Ronit Avraham',
        });
        const store = storeWithApi(apiError, {
            findCars,
            unassignTeacher: () => throwError(() => refusal),
        });
        await TestBed.inject(ApplicationRef).whenStable();
        const loadsBefore = findCars.mock.calls.length;

        //when
        await store.applyAssignments('car-1', [], ['teacher-1']);
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(apiError).toHaveBeenCalledWith(refusal, ['teacher', 'names']);
        expect(findCars.mock.calls.length).toBeGreaterThan(loadsBefore);
        expect(store.isMutating()).toBe(false);
    });

    it('sends no assign once an unassign in the same Apply is refused', async () => {
        //given
        const apiError = vi.fn();
        const assignTeacher = vi.fn(() => of(undefined));
        const refusal = conflict('teacherAssignmentMustNotHaveActiveStudents', {
            count: '1',
            names: 'Noa Mizrahi',
            teacher: 'Ronit Avraham',
        });
        const store = storeWithApi(apiError, {
            assignTeacher,
            unassignTeacher: () => throwError(() => refusal),
        });

        //when
        await store.applyAssignments('car-1', ['teacher-2'], ['teacher-1']);

        //then
        expect(assignTeacher).not.toHaveBeenCalled();
        expect(apiError).toHaveBeenCalledWith(refusal, ['teacher', 'names']);
    });
});
