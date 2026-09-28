import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { Observable, of, Subject, throwError } from 'rxjs';
import { LanguageService } from '../../../../../core/language.service';
import { DayOfWeek } from '../../../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';
import { GetPublicationByLinkResponse } from '../../../data/get-publication-by-link.response';
import { IdentifyStudentRequest } from '../../../data/identify-student.request';
import { IdentifyStudentResponse, SlotForIdentifyStudentResponse } from '../../../data/identify-student.response';
import { SubmissionsApiService } from '../../../data/submissions-api.service';
import { Transmission } from '../../../domain/transmission.enum';
import { StudentFormPage } from './student-form.page';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_SERVER_ERROR = 500;
const LINK_TOKEN = 'link-token';
const ROSTER_NATIONAL_ID = '000000018';

const WINDOW_TIMES: Record<SlotWindow, [string, string]> = {
    [SlotWindow.morning]: ['07:00:00', '12:00:00'],
    [SlotWindow.noon]: ['12:00:00', '15:00:00'],
    [SlotWindow.afternoon]: ['15:00:00', '18:00:00'],
    [SlotWindow.evening]: ['18:00:00', '22:00:00'],
};

const FULL_DAYS: readonly DayOfWeek[] = [
    DayOfWeek.sunday,
    DayOfWeek.monday,
    DayOfWeek.tuesday,
    DayOfWeek.wednesday,
    DayOfWeek.thursday,
];

const SHORT_DAY_WINDOWS: readonly SlotWindow[] = [SlotWindow.morning, SlotWindow.noon];

type IdentifyStudent = (token: string, request: IdentifyStudentRequest) => Observable<IdentifyStudentResponse>;

interface FakeSubmissionsApi {
    getPublicationByLink: () => Observable<GetPublicationByLinkResponse>;
    identifyStudent: IdentifyStudent;
}

function weekSlots(unavailable: readonly string[]): SlotForIdentifyStudentResponse[] {
    const fullDays = FULL_DAYS.flatMap(day => Object.values(SlotWindow).map(window => ({ day, window })));
    const friday = SHORT_DAY_WINDOWS.map(window => ({ day: DayOfWeek.friday, window }));

    return [...fullDays, ...friday].map(({ day, window }) => {
        const id = `${day}-${window}`;
        const [startLocal, endLocal] = WINDOW_TIMES[window];
        const state = unavailable.includes(id) ? SlotState.unavailable : SlotState.open;

        return { id, day, window, state, startLocal, endLocal };
    });
}

function studentOf(
    teacherName: string,
    transmission: Transmission,
    slots: SlotForIdentifyStudentResponse[],
): IdentifyStudentResponse {
    return {
        studentName: 'Test Student',
        teacherName,
        carName: 'Corolla White',
        transmission,
        hasSubmission: false,
        slots,
    };
}

const COHEN_STUDENT = studentOf('Teacher Cohen', Transmission.automatic, weekSlots([]));

function publicationIn(state: PublicationState): () => Observable<GetPublicationByLinkResponse> {
    return () =>
        of({
            weekStart: '2026-11-15',
            weekNumber: 47,
            state,
            windowStartUtc: '2026-11-11T16:00:00Z',
            windowEndUtc: '2026-11-13T12:00:00Z',
        });
}

function failingWith(status: number): () => Observable<never> {
    return () => throwError(() => new HttpErrorResponse({ status }));
}

function identifyingAs(student: IdentifyStudentResponse): IdentifyStudent {
    return vi.fn(() => of(student));
}

function answeredByHand(pendingLookups: Subject<IdentifyStudentResponse>[]): IdentifyStudent {
    return () => {
        const lookup = new Subject<IdentifyStudentResponse>();
        pendingLookups.push(lookup);

        return lookup;
    };
}

function answer(lookup: Subject<IdentifyStudentResponse>, student: IdentifyStudentResponse): void {
    lookup.next(student);
    lookup.complete();
}

function provideApi(api: FakeSubmissionsApi): void {
    TestBed.configureTestingModule({
        imports: [TranslocoTestingModule.forRoot({ langs: { en: {} } })],
        providers: [
            provideZonelessChangeDetection(),
            { provide: SubmissionsApiService, useValue: api },
            { provide: LanguageService, useValue: { lang: signal('en') } },
        ],
    });
}

function provideOpenLinkIdentifying(identifyStudent: IdentifyStudent): void {
    provideApi({ getPublicationByLink: publicationIn(PublicationState.open), identifyStudent });
}

async function renderPage(): Promise<ComponentFixture<StudentFormPage>> {
    const fixture = TestBed.createComponent(StudentFormPage);
    fixture.componentRef.setInput('token', LINK_TOKEN);
    await fixture.whenStable();

    return fixture;
}

function page(fixture: ComponentFixture<StudentFormPage>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

async function typeNationalId(fixture: ComponentFixture<StudentFormPage>, value: string): Promise<void> {
    const field = page(fixture).querySelector('#national-id') as HTMLInputElement;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    await fixture.whenStable();
}

async function typeNationalIdWhileLookupPending(
    fixture: ComponentFixture<StudentFormPage>,
    value: string,
): Promise<void> {
    const field = page(fixture).querySelector('#national-id') as HTMLInputElement;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    await new Promise(resolve => setTimeout(resolve));
    fixture.detectChanges();
}

async function submitIdentify(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    const form = page(fixture).querySelector('form.identify') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
}

function continueButton(fixture: ComponentFixture<StudentFormPage>): HTMLButtonElement | null {
    return page(fixture).querySelector('.wizard-step__footer button');
}

async function clickContinue(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    continueButton(fixture)!.click();
    await fixture.whenStable();
}

async function identifyAndContinue(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    await typeNationalId(fixture, ROSTER_NATIONAL_ID);
    await clickContinue(fixture);
}

describe('StudentFormPage', () => {
    it.each([
        { view: 'notYetOpen', load: publicationIn(PublicationState.published), windowRows: 2 },
        { view: 'closed', load: publicationIn(PublicationState.closed), windowRows: 1 },
        { view: 'invalidLink (draft)', load: publicationIn(PublicationState.draft), windowRows: 0 },
        { view: 'invalidLink (unknown)', load: failingWith(HTTP_NOT_FOUND), windowRows: 0 },
        { view: 'loadFailed', load: failingWith(HTTP_SERVER_ERROR), windowRows: 0 },
    ])('renders the $view view read-only', async ({ load, windowRows }) => {
        //given
        provideApi({ getPublicationByLink: load, identifyStudent: identifyingAs(COHEN_STUDENT) });

        //when
        const fixture = await renderPage();

        //then
        expect(page(fixture).querySelector('.status__heading')).not.toBeNull();
        expect(page(fixture).querySelectorAll('form, input, select, textarea').length).toBe(0);
        expect(page(fixture).querySelectorAll('.student-form__window dt').length).toBe(windowRows);
        expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
    });

    describe('identify step', () => {
        it('asks an open link for the national ID, without a caption bar', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));

            //when
            const fixture = await renderPage();

            //then
            expect(page(fixture).querySelector('#national-id')).not.toBeNull();
            expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
        });

        it('looks up a complete ID as soon as it is typed, without its separators', async () => {
            //given
            const identifyStudent = identifyingAs(COHEN_STUDENT);
            provideOpenLinkIdentifying(identifyStudent);
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, '000 000-018');

            //then
            expect(identifyStudent).toHaveBeenCalledWith(LINK_TOKEN, { nationalId: ROSTER_NATIONAL_ID });
            expect(page(fixture).querySelector('.identify__found')).not.toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(false);
        });

        it('looks up a shorter ID only when it is submitted', async () => {
            //given
            const identifyStudent = identifyingAs(COHEN_STUDENT);
            provideOpenLinkIdentifying(identifyStudent);
            const fixture = await renderPage();
            await typeNationalId(fixture, '18');

            //when
            const calledBeforeSubmit = vi.mocked(identifyStudent).mock.calls.length;
            await submitIdentify(fixture);

            //then
            expect(calledBeforeSubmit).toBe(0);
            expect(identifyStudent).toHaveBeenCalledWith(LINK_TOKEN, { nationalId: '18' });
            expect(page(fixture).querySelector('.identify__found')).not.toBeNull();
        });

        it('keeps an ID that is not on the roster at the ID step with a contact-your-school message', async () => {
            //given
            provideOpenLinkIdentifying(failingWith(HTTP_NOT_FOUND));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(page(fixture).querySelector('.identify__not-on-roster')).not.toBeNull();
            expect(page(fixture).querySelector('.identify__found')).toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
            expect(page(fixture).querySelector('app-details-step')).toBeNull();
        });

        it('flags an ID the school system rejects as malformed', async () => {
            //given
            provideOpenLinkIdentifying(failingWith(HTTP_CONFLICT));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, '000000019');

            //then
            expect(page(fixture).querySelector('.identify__invalid')).not.toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
        });

        it('offers a retry when the lookup fails', async () => {
            //given
            const identifyStudent: IdentifyStudent = vi.fn(failingWith(HTTP_SERVER_ERROR));
            provideOpenLinkIdentifying(identifyStudent);
            const fixture = await renderPage();
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('.identify__failed')).not.toBeNull();
            expect(identifyStudent).toHaveBeenCalledTimes(2);
        });

        it('keeps the typed ID left-to-right so spaced digit groups stay in order on a Hebrew page', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));

            //when
            const fixture = await renderPage();

            //then
            expect(page(fixture).querySelector('#national-id')!.getAttribute('dir')).toBe('ltr');
        });

        it('drops the previous result as soon as the ID is edited', async () => {
            //given
            const identifyStudent = identifyingAs(COHEN_STUDENT);
            provideOpenLinkIdentifying(identifyStudent);
            const fixture = await renderPage();
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //when
            await typeNationalId(fixture, '00000001');

            //then
            expect(page(fixture).querySelector('.identify__found')).toBeNull();
            expect(identifyStudent).toHaveBeenCalledTimes(1);
        });
    });

    describe('identify step while a lookup is in flight', () => {
        it('hides the previous student and locks Continue while a newer ID is being checked', async () => {
            //given
            const pendingLookups: Subject<IdentifyStudentResponse>[] = [];
            provideOpenLinkIdentifying(answeredByHand(pendingLookups));
            const fixture = await renderPage();
            await typeNationalIdWhileLookupPending(fixture, ROSTER_NATIONAL_ID);
            answer(pendingLookups[0], COHEN_STUDENT);
            await fixture.whenStable();

            //when
            await typeNationalIdWhileLookupPending(fixture, '000000026');

            //then
            expect(page(fixture).querySelector('.identify__found')).toBeNull();
            expect(page(fixture).querySelector('.identify__checking')).not.toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
        });

        it('never lets a late answer for a replaced ID overwrite the newer student', async () => {
            //given
            const pendingLookups: Subject<IdentifyStudentResponse>[] = [];
            provideOpenLinkIdentifying(answeredByHand(pendingLookups));
            const fixture = await renderPage();
            await typeNationalIdWhileLookupPending(fixture, ROSTER_NATIONAL_ID);
            await typeNationalIdWhileLookupPending(fixture, '000000026');

            //when
            answer(pendingLookups[1], studentOf('Teacher Levi', Transmission.manual, weekSlots([])));
            answer(pendingLookups[0], COHEN_STUDENT);
            await fixture.whenStable();
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('.details__teacher-name')?.textContent?.trim()).toBe('Teacher Levi');
        });
    });

    describe('details step', () => {
        it('confirms the roster teacher, car and transmission read-only', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('.details__teacher-name')?.textContent?.trim()).toBe('Teacher Cohen');
            expect(page(fixture).querySelector('.details__car')?.textContent?.trim()).toBe('Corolla White');
            expect(page(fixture).querySelector('.details__transmission')?.textContent).toContain(
                'studentForm.details.transmissions.automatic',
            );
            expect(page(fixture).querySelectorAll('main input, main select, main textarea').length).toBe(0);
            expect(page(fixture).querySelector('.student-shell__caption')?.textContent).toContain(
                'studentForm.weekCaption',
            );
        });

        it('tells a student whose teacher has no grid this week to contact the school', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(studentOf('Teacher Levi', Transmission.manual, [])));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('.details__teacher-name')?.textContent?.trim()).toBe('Teacher Levi');
            expect(page(fixture).querySelector('.details__no-availability')).not.toBeNull();
        });
    });

    describe('slots step', () => {
        it.each([
            { teacherName: 'Teacher Cohen', unavailable: [] as string[] },
            { teacherName: 'Teacher Levi', unavailable: ['sunday-morning', 'friday-noon'] },
        ])("shows $teacherName's own week grid", async ({ teacherName, unavailable }) => {
            //given
            const student = studentOf(teacherName, Transmission.automatic, weekSlots(unavailable));
            provideOpenLinkIdentifying(identifyingAs(student));
            const fixture = await renderPage();
            await identifyAndContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('app-slots-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('.slot-day').length).toBe(6);
            expect(page(fixture).querySelectorAll('.slot-chip').length).toBe(22);
            expect(page(fixture).querySelectorAll('.slot-chip--unavailable').length).toBe(unavailable.length);
            expect(page(fixture).querySelector('.student-shell__caption')?.textContent).toContain(
                'studentForm.weekTeacherCaption',
            );
        });

        it('never offers the grid when the teacher has no availability this week', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(studentOf('Teacher Levi', Transmission.manual, [])));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('.details__no-availability')).not.toBeNull();
            expect(continueButton(fixture)).toBeNull();
        });

        it('keeps the week grid read-only', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await identifyAndContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelectorAll('main button, main input').length).toBe(0);
        });
    });
});
