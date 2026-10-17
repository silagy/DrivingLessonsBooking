import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { LanguageService } from '../../../../../core/language.service';
import { DayOfWeek } from '../../../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';
import { Slot } from '../../../domain/slot.model';
import { WeekSchedulesStore } from '../../../state/week-schedules.store';
import { WeeklyPrepPage } from './weekly-prep.page';

const WEEK_START = '2026-10-18';

const SLOTS: Slot[] = [
    {
        id: 'slot-open',
        day: DayOfWeek.sunday,
        window: SlotWindow.morning,
        state: SlotState.open,
        startLocal: '07:00:00',
        endLocal: '12:00:00',
    },
    {
        id: 'slot-unavailable',
        day: DayOfWeek.sunday,
        window: SlotWindow.noon,
        state: SlotState.unavailable,
        startLocal: '12:00:00',
        endLocal: '15:00:00',
    },
];

function fakeStore(overrides: Record<string, unknown>) {
    return {
        publicationState: signal<PublicationState | undefined>(PublicationState.draft),
        canPublish: signal(true),
        selectedTeacherId: signal<string | null>('teacher-cohen'),
        selectedWeekStart: signal(WEEK_START),
        canChooseTeacher: signal(true),
        teachers: signal([{ id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false }]),
        ownTeacherName: signal<string | undefined>(undefined),
        weekOptions: signal([{ weekStart: WEEK_START, label: '18 Oct - 23 Oct 2026' }]),
        isLoading: signal(false),
        loadError: signal<string | undefined>(undefined),
        hasSelection: signal(true),
        isNotCreatedYet: signal(false),
        weekSchedule: signal({ id: 'week-1', teacherId: 'teacher-cohen', weekStart: WEEK_START, slots: SLOTS }),
        slots: signal(SLOTS),
        windowTimes: signal({}),
        isMutating: signal(false),
        unavailableCount: signal(1),
        toggleSlot: async () => undefined,
        selectTeacher: () => undefined,
        selectWeek: () => undefined,
        ...overrides,
    };
}

async function render(store: ReturnType<typeof fakeStore>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            WeeklyPrepPage,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            providePrimeNG(),
            { provide: WeekSchedulesStore, useValue: store },
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
        ],
    });

    const fixture = TestBed.createComponent(WeeklyPrepPage);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('WeeklyPrepPage', () => {
    it('repeats the publish link in the bottom bar', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const header = page.querySelector('a.weekly-prep__publish-link');
        const dock = page.querySelector('.weekly-prep__dock a.weekly-prep__dock-link');

        //then
        expect(dock?.getAttribute('href')).toBe('/publications?teacherId=teacher-cohen&week=2026-10-18&publish=1');
        expect(dock?.getAttribute('href')).toBe(header?.getAttribute('href'));
    });

    it('has no bottom bar when the week cannot be published', async () => {
        //given
        const store = fakeStore({ canPublish: signal(false) });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.weekly-prep__dock')).toBeNull();
    });

    it('keeps the open label in the cell for screen readers', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const labels = [...page.querySelectorAll('.slot-cell .slot-cell__label')].map((label) =>
            label.textContent?.trim(),
        );

        //then
        expect(labels).toEqual(['weekSchedules.cellOpen', 'weekSchedules.cellUnavailable']);
    });

    it('gives an unavailable cell a short label for the phone', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const shortLabels = [...page.querySelectorAll('.slot-cell .slot-cell__short')].map((label) =>
            label.textContent?.trim(),
        );

        //then
        expect(shortLabels).toEqual(['weekSchedules.cellUnavailableShort']);
    });
});
