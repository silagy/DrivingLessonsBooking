import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationStatusApiService } from '../data/publication-status-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { WeekSchedulesApiService } from '../data/week-schedules-api.service';
import { WeekSchedulesStore } from './week-schedules.store';

const HTTP_INTERNAL_SERVER_ERROR = 500;

function failingStore(): WeekSchedulesStore {
    const serverError = () => throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR }));

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: serverError } },
            { provide: WeekSchedulesApiService, useValue: { getByTeacherAndWeek: serverError, create: serverError } },
            { provide: PublicationStatusApiService, useValue: { getByWeek: serverError } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return TestBed.inject(WeekSchedulesStore);
}

describe('WeekSchedulesStore', () => {
    it('reports the load error instead of throwing when the week fails to load', async () => {
        //given
        const store = failingStore();
        store.selectTeacher('teacher-cohen');

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.teachers()).toEqual([]);
        expect(store.publicationState()).toBeUndefined();
        expect(store.canPublish()).toBe(false);
        expect(store.weekSchedule()).toBeUndefined();
        expect(store.slots()).toEqual([]);
        expect(store.unavailableCount()).toBe(0);
        expect(store.loadError()).toBe('weekSchedules.loadFailed');
    });
});
