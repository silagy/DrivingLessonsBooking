import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { GetLatestRosterImportResponse } from '../data/get-latest-roster-import.response';
import { RosterApiService } from '../data/roster-api.service';
import { RosterEntryOutcome } from '../domain/roster-entry-outcome.enum';
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

const LATEST_IMPORT: GetLatestRosterImportResponse = {
    id: 'import-1',
    fileName: 'roster.csv',
    importedAtUtc: '2026-10-05T08:00:00Z',
    added: 1,
    updated: 1,
    failed: 0,
    entries: [
        { nationalId: '123456782', outcome: RosterEntryOutcome.added },
        { nationalId: '987654324', outcome: RosterEntryOutcome.updated },
    ],
    failures: [],
};

function storeWithLatestImport(latestImport: GetLatestRosterImportResponse): RosterStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: RosterApiService, useValue: { getLatestImport: () => of(latestImport), findStudents: () => of([]) } },
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

    it('badges every Student the latest import added or updated', async () => {
        //given
        const store = storeWithLatestImport(LATEST_IMPORT);

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.badgeByNationalId()).toEqual(
            new Map([
                ['123456782', RosterEntryOutcome.added],
                ['987654324', RosterEntryOutcome.updated],
            ]),
        );
    });
});
