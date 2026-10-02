import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SlotForIdentifyStudentResponse } from '../data/identify-student.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
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

async function identifiedStore(): Promise<StudentFormStore> {
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
                    identifyStudent: () =>
                        of({
                            studentName: 'Test Student',
                            teacherName: 'Teacher Cohen',
                            carName: 'Corolla White',
                            transmission: Transmission.automatic,
                            submission: null,
                            slots: SLOTS,
                        }),
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

describe('StudentFormStore', () => {
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
