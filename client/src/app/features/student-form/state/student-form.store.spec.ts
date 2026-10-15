import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { IdentifyStudentResponse, SlotForIdentifyStudentResponse } from '../data/identify-student.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { IdentifyStatus } from '../domain/identify-status.enum';
import { SessionType } from '../domain/session-type.enum';
import { SubmitStatus } from '../domain/submit-status.enum';
import { Transmission } from '../domain/transmission.enum';
import { StudentFormStore } from './student-form.store';

const LINK_TOKEN = 'link-token';
const ROSTER_NATIONAL_ID = '000000018';

function slotOf(day: DayOfWeek, window: SlotWindow, state: SlotState): SlotForIdentifyStudentResponse {
    return { id: `${day}-${window}`, day, window, state, startLocal: '07:00:00', endLocal: '12:00:00' };
}

const SLOTS = [
    slotOf(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable),
    slotOf(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
];

const ROSTER_STUDENT: IdentifyStudentResponse = {
    studentName: 'Test Student',
    teacherName: 'Teacher Cohen',
    carName: 'Corolla White',
    transmission: Transmission.automatic,
    submission: null,
    slots: SLOTS,
};

const SUBMIT_ACCEPTED = (): Observable<void> => of(undefined);

async function storeAfterIdentify(
    identifyStudent: () => Observable<IdentifyStudentResponse>,
    createSubmission: () => Observable<void>,
): Promise<StudentFormStore> {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            StudentFormStore,
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: SubmissionsApiService,
                useValue: {
                    getPublicationByLink: () =>
                        of({
                            weekStart: '2026-11-15',
                            weekNumber: 47,
                            state: PublicationState.open,
                            windowStartUtc: '2026-11-11T16:00:00Z',
                            windowEndUtc: '2026-11-13T12:00:00Z',
                        }),
                    identifyStudent,
                    createSubmission,
                },
            },
        ],
    });
    const store = TestBed.inject(StudentFormStore);
    store.open(LINK_TOKEN);
    store.changeNationalId(ROSTER_NATIONAL_ID);
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

function identifiedStore(): Promise<StudentFormStore> {
    return storeAfterIdentify(() => of(ROSTER_STUDENT), SUBMIT_ACCEPTED);
}

async function storeRateLimitedOnSubmit(): Promise<StudentFormStore> {
    const rateLimited = new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests });
    const store = await storeAfterIdentify(() => of(ROSTER_STUDENT), () => throwError(() => rateLimited));
    store.openPick('sunday-noon');
    store.savePick({ sessionType: SessionType.single, constraint: null });

    return store;
}

describe('StudentFormStore', () => {
    describe('identifyStatus', () => {
        it('maps a 429 to tooManyAttempts', async () => {
            //given
            const rateLimited = new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests });

            //when
            const store = await storeAfterIdentify(() => throwError(() => rateLimited), SUBMIT_ACCEPTED);

            //then
            expect(store.identifyStatus()).toBe(IdentifyStatus.tooManyAttempts);
        });

        it('still reports a roster miss as notOnRoster', async () => {
            //given
            const missing = new HttpErrorResponse({ status: HttpStatusCode.NotFound });

            //when
            const store = await storeAfterIdentify(() => throwError(() => missing), SUBMIT_ACCEPTED);

            //then
            expect(store.identifyStatus()).toBe(IdentifyStatus.notOnRoster);
        });
    });

    describe('submitStatus', () => {
        it('maps a 429 on send to tooManyAttempts', async () => {
            //given
            const store = await storeRateLimitedOnSubmit();

            //when
            await store.submit();

            //then
            expect(store.submitStatus()).toBe(SubmitStatus.tooManyAttempts);
        });

        it('lets the student send again after a 429', async () => {
            //given
            const store = await storeRateLimitedOnSubmit();

            //when
            await store.submit();

            //then
            expect(store.canSubmit()).toBe(true);
        });
    });

    describe('openPick', () => {
        it('opens the sheet for an open slot', async () => {
            //given
            const store = await identifiedStore();

            //when
            store.openPick('sunday-noon');

            //then
            expect(store.pickSheet()?.slotId).toBe('sunday-noon');
        });

        it('never opens the sheet for an unavailable slot', async () => {
            //given
            const store = await identifiedStore();

            //when
            store.openPick('sunday-morning');

            //then
            expect(store.pickSheet()).toBeNull();
        });
    });

    describe('captionParams', () => {
        it('keeps the teacher\'s name in its own reading direction', async () => {
            //given
            const store = await identifiedStore();

            //when
            const params = store.captionParams();

            //then
            expect(params.teacherName).toBe('⁨Teacher Cohen⁩');
        });
    });

    describe('slotDays', () => {
        it('dates each day day-first in Israeli English', async () => {
            //given
            const store = await identifiedStore();

            //when
            const [sunday] = store.slotDays();

            //then
            expect(sunday.dateLabel).toBe('15/11');
        });
    });
});
