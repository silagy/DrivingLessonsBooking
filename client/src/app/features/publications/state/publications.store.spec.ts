import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationsApiService } from '../data/publications-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { PublicationsStore } from './publications.store';

const HTTP_NOT_FOUND = 404;

async function loadedStore(): Promise<PublicationsStore> {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: TeacherOptionsApiService,
                useValue: {
                    findTeachers: () =>
                        of([
                            { id: 'teacher-levi', name: 'Teacher Levi' },
                            { id: 'teacher-cohen', name: 'Teacher Cohen' },
                        ]),
                },
            },
            {
                provide: PublicationsApiService,
                useValue: {
                    getByWeek: () => throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
                    findHistory: () => of([]),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
            { provide: ClipboardService, useValue: { copy: async () => true } },
            { provide: FileDownloadService, useValue: { download: () => undefined } },
        ],
    });

    const store = TestBed.inject(PublicationsStore);
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
});
