import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { CarsApiService } from '../data/cars-api.service';
import { CarsStore } from './cars.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;

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
});
