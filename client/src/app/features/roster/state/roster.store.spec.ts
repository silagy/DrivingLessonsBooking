import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { RosterApiService } from '../data/roster-api.service';
import { RosterStore } from './roster.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;

function failingStore(): RosterStore {
    const serverError = () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR }));

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: RosterApiService, useValue: { getLatestImport: serverError, findStudents: serverError } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return TestBed.inject(RosterStore);
}

describe('RosterStore', () => {
    it('reports the load error instead of throwing when the roster fails to load', async () => {
        //given
        const store = failingStore();

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.latestImport()).toBeUndefined();
        expect(store.students()).toEqual([]);
        expect(store.filteredStudents()).toEqual([]);
        expect(store.teacherFilterOptions()).toEqual([]);
        expect(store.badgeByNationalId().size).toBe(0);
        expect(store.failedRows()).toEqual([]);
        expect(store.isLoading()).toBe(false);
        expect(store.loadError()).toBe('roster.loadFailed');
    });
});
