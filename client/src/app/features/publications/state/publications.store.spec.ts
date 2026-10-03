import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { GetPublicationResponse } from '../data/get-publication.response';
import { PublicationsApiService } from '../data/publications-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { TeacherOption } from '../domain/teacher-option.model';
import { PublicationsStore } from './publications.store';

const HTTP_NOT_FOUND = 404;

const TEACHERS: TeacherOption[] = [
    { id: 'teacher-levi', name: 'Teacher Levi' },
    { id: 'teacher-cohen', name: 'Teacher Cohen' },
];

const OPEN_PUBLICATION: GetPublicationResponse = {
    id: 'publication-1',
    weekStart: '2026-10-04',
    weekNumber: 41,
    state: PublicationState.open,
    linkToken: 'link-token',
    windowStartUtc: '2026-10-01T16:00:00Z',
    windowEndUtc: '2026-10-09T11:00:00Z',
};

function createStore(
    teachers: Observable<TeacherOption[]>,
    publication: Observable<GetPublicationResponse>,
): PublicationsStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: () => teachers } },
            {
                provide: PublicationsApiService,
                useValue: {
                    getByWeek: () => publication,
                    getDashboard: () => NEVER,
                    findHistory: () => of([]),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
            { provide: ClipboardService, useValue: { copy: async () => true } },
            { provide: FileDownloadService, useValue: { download: () => undefined } },
        ],
    });

    return TestBed.inject(PublicationsStore);
}

async function loadedStore(): Promise<PublicationsStore> {
    const store = createStore(
        of(TEACHERS),
        throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
    );
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

describe('PublicationsStore', () => {
    it('selects the first teacher by name once the teachers load', async () => {
        //given
        const store = await loadedStore();

        //expected
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
    });

    it('keeps an explicitly chosen teacher', async () => {
        //given
        const store = await loadedStore();

        //when
        store.selectTeacher('teacher-levi');

        //then
        expect(store.selectedTeacherId()).toBe('teacher-levi');
    });

    it('stays loading until the teachers arrive, so an open week shows no placeholder counts', async () => {
        //given
        const store = createStore(NEVER, of(OPEN_PUBLICATION));

        //when
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //then
        expect(store.isLoading()).toBe(true);
    });
});
