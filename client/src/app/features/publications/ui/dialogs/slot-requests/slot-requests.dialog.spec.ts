import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { LanguageService } from '../../../../../core/language.service';
import { DayOfWeek } from '../../../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';
import { ItemForFindSlotRequestsResponse } from '../../../data/item-for-find-slot-requests.response';
import { SlotRequestsDialog } from './slot-requests.dialog';
import { SlotRequestsDialogData } from './slot-requests-dialog-data';

const WEDNESDAY_AFTERNOON = {
    slotId: 'slot-a',
    day: DayOfWeek.wednesday,
    window: SlotWindow.afternoon,
    state: SlotState.open,
    requestCount: 6,
    startLocal: '15:00:00',
    endLocal: '18:00:00',
};

const NOA: ItemForFindSlotRequestsResponse = { studentId: 'student-noa', studentName: 'Noa Mizrahi', rank: 1, pickCount: 3 };
const OMER: ItemForFindSlotRequestsResponse = { studentId: 'student-omer', studentName: 'Omer Biton', rank: 2, pickCount: 3 };
const MAYA: ItemForFindSlotRequestsResponse = { studentId: 'student-maya', studentName: 'Maya Cohen', rank: 3, pickCount: 3 };

const TRANSLATIONS = {
    publications: {
        slotRequests: {
            title: '{{day}}|{{date}}|{{window}}',
            requestedByOne: 'one student',
            requestedByMany: '{{count}} students',
            choice: 'choice {{rank}} of {{count}}',
            none: 'nobody now',
            loadFailed: 'could not load',
        },
    },
};

interface DialogState {
    requests: ItemForFindSlotRequestsResponse[];
    isLoading: boolean;
    errorKey: string | null;
}

async function render(
    state: DialogState,
    dialogFrame?: HTMLElement,
): Promise<{ page: HTMLElement; ref: { close: ReturnType<typeof vi.fn> } }> {
    const data: SlotRequestsDialogData = {
        slot: signal(WEDNESDAY_AFTERNOON),
        requests: signal(state.requests),
        isLoading: signal(state.isLoading),
        errorKey: signal(state.errorKey),
        weekStart: signal('2026-10-18'),
        windowTimes: signal({ [SlotWindow.afternoon]: '15:00-18:00' }),
    };
    const ref = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [
            SlotRequestsDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: TRANSLATIONS },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            providePrimeNG(),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: ref },
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
        ],
    });

    const fixture: ComponentFixture<SlotRequestsDialog> = TestBed.createComponent(SlotRequestsDialog);
    dialogFrame?.appendChild(fixture.nativeElement as HTMLElement);
    await fixture.whenStable();

    return { page: fixture.nativeElement as HTMLElement, ref };
}

function text(page: HTMLElement, selector: string): string {
    return page.querySelector(selector)?.textContent?.trim() ?? '';
}

function loaded(requests: ItemForFindSlotRequestsResponse[]): DialogState {
    return { requests, isLoading: false, errorKey: null };
}

describe('SlotRequestsDialog', () => {
    it('titles the sheet with the day, date and window', async () => {
        //when
        const { page } = await render(loaded([NOA]));

        //then
        expect(text(page, 'h2.slot-requests__title')).toBe(
            'weekGrid.daysShort.wednesday|21/10|weekGrid.windows.afternoon',
        );
    });

    it('names the dialog after its title for screen readers', async () => {
        //given
        const dialogFrame = document.createElement('div');
        dialogFrame.setAttribute('role', 'dialog');
        dialogFrame.setAttribute('aria-labelledby', 'pn_id_1_header');
        document.body.appendChild(dialogFrame);

        //when
        const { page } = await render(loaded([NOA]), dialogFrame);

        //then
        const labelledBy = dialogFrame.getAttribute('aria-labelledby');
        expect(page.querySelector(`#${labelledBy}`)).toBe(page.querySelector('h2.slot-requests__title'));
        dialogFrame.remove();
    });

    it('shows the window time left to right', async () => {
        //when
        const { page } = await render(loaded([NOA]));

        //then
        const time = page.querySelector('.slot-requests__subtitle bdi');
        expect(time?.getAttribute('dir')).toBe('ltr');
        expect(time?.textContent?.trim()).toBe('15:00-18:00');
    });

    it('lists the students in the order it gets them', async () => {
        //when
        const { page } = await render(loaded([OMER, NOA, MAYA]));

        //then
        const names = Array.from(page.querySelectorAll('.slot-requests__name')).map((name) => name.textContent?.trim());
        expect(names).toEqual(['Omer Biton', 'Noa Mizrahi', 'Maya Cohen']);
        expect(text(page, '.slot-requests__avatar')).toBe('OB');
    });

    it('says which of the student\'s choices this slot was', async () => {
        //when
        const { page } = await render(loaded([OMER]));

        //then
        expect(text(page, '.slot-requests__choice')).toBe('choice 2 of 3');
    });

    it('counts the students it lists, not the cell', async () => {
        //when
        const many = await render(loaded([NOA, OMER]));

        //then
        expect(text(many.page, '.slot-requests__count')).toBe('2 students');
    });

    it('says one student requested the slot', async () => {
        //when
        const { page } = await render(loaded([NOA]));

        //then
        expect(text(page, '.slot-requests__count')).toBe('one student');
    });

    it('counts from the cell while the list loads', async () => {
        //when
        const { page } = await render({ requests: [], isLoading: true, errorKey: null });

        //then
        expect(text(page, '.slot-requests__count')).toBe('6 students');
        expect(page.querySelector('p-progressspinner')).not.toBeNull();
        expect(page.querySelector('.slot-requests__list')).toBeNull();
    });

    it('says nobody requests the slot now', async () => {
        //when
        const { page } = await render(loaded([]));

        //then
        expect(text(page, '.slot-requests__state')).toBe('nobody now');
    });

    it('says the list could not load', async () => {
        //when
        const { page } = await render({ requests: [], isLoading: false, errorKey: 'publications.slotRequests.loadFailed' });

        //then
        expect(text(page, '.slot-requests__state')).toBe('could not load');
    });

    it('closes from the cross and from the Close button', async () => {
        //given
        const { page, ref } = await render(loaded([NOA]));

        //when
        (page.querySelector('button.slot-requests__close') as HTMLButtonElement).click();
        (page.querySelector('.slot-requests__footer button') as HTMLButtonElement).click();

        //then
        expect(ref.close).toHaveBeenCalledTimes(2);
    });
});
