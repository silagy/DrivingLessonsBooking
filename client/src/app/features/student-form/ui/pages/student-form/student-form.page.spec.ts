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

async function reachTarget(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    await identifyAndContinue(fixture);
    await clickContinue(fixture);
}

async function reachSlots(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    await reachTarget(fixture);
    await clickContinue(fixture);
}

async function press(fixture: ComponentFixture<StudentFormPage>, selector: string): Promise<void> {
    const element = page(fixture).querySelector(selector) as HTMLElement;
    element.click();
    await fixture.whenStable();
}

function textOf(fixture: ComponentFixture<StudentFormPage>, selector: string): string | undefined {
    return page(fixture).querySelector(selector)?.textContent?.trim();
}

function chip(fixture: ComponentFixture<StudentFormPage>, slotId: string): HTMLButtonElement {
    return page(fixture).querySelector(`[data-slot-id="${slotId}"]`) as HTMLButtonElement;
}

function rankOn(fixture: ComponentFixture<StudentFormPage>, slotId: string): string | undefined {
    return chip(fixture, slotId).querySelector('.slot-chip__rank')?.textContent?.trim();
}

function pickSheet(fixture: ComponentFixture<StudentFormPage>): HTMLElement | null {
    return page(fixture).querySelector('.pick-sheet');
}

async function tapChip(fixture: ComponentFixture<StudentFormPage>, slotId: string): Promise<void> {
    chip(fixture, slotId).click();
    await fixture.whenStable();
}

async function addPick(fixture: ComponentFixture<StudentFormPage>, slotId: string): Promise<void> {
    await tapChip(fixture, slotId);
    await press(fixture, '.pick-sheet__save button');
}

async function chooseDouble(fixture: ComponentFixture<StudentFormPage>): Promise<void> {
    await press(fixture, '.pick-sheet__double');
}

async function typeConstraint(fixture: ComponentFixture<StudentFormPage>, text: string): Promise<void> {
    const field = page(fixture).querySelector('#pick-constraint') as HTMLTextAreaElement;
    field.value = text;
    field.dispatchEvent(new Event('input'));
    await fixture.whenStable();
}

function isChecked(fixture: ComponentFixture<StudentFormPage>, selector: string): boolean {
    return (page(fixture).querySelector(selector) as HTMLInputElement).checked;
}

function constraintValue(fixture: ComponentFixture<StudentFormPage>): string {
    return (page(fixture).querySelector('#pick-constraint') as HTMLTextAreaElement).value;
}

function accessibleNameOf(fixture: ComponentFixture<StudentFormPage>, selector: string): string {
    const labelIds = page(fixture).querySelector(selector)!.getAttribute('aria-labelledby') ?? '';

    return labelIds
        .split(' ')
        .map(id => document.getElementById(id)?.textContent?.trim())
        .join(' ');
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

    describe('target step', () => {
        it('asks for a weekly target after the details, starting at one lesson', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await identifyAndContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('app-target-step')).not.toBeNull();
            expect(textOf(fixture, '.target__count')).toBe('1');
            expect(page(fixture).querySelector<HTMLButtonElement>('.target__decrease')!.disabled).toBe(true);
            expect(textOf(fixture, '.student-shell__caption')).toContain('studentForm.weekTeacherCaption');
        });

        it('raises the target without an upper limit and never lowers it below one', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachTarget(fixture);

            //when
            for (let count = 1; count <= 30; count++) {
                await press(fixture, '.target__increase');
            }
            const raised = textOf(fixture, '.target__count');
            for (let count = 1; count <= 40; count++) {
                await press(fixture, '.target__decrease');
            }

            //then
            expect(raised).toBe('31');
            expect(textOf(fixture, '.target__count')).toBe('1');
        });

        it('names the unit in the singular for one lesson and in the plural for more', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachTarget(fixture);
            const single = textOf(fixture, '.target__unit');

            //when
            await press(fixture, '.target__increase');

            //then
            expect(single).toMatch(/studentForm\.target\.unitOne$/);
            expect(textOf(fixture, '.target__unit')).toMatch(/studentForm\.target\.unit$/);
        });

        it('goes back to the details and keeps the chosen target', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await press(fixture, '.target__increase');

            //when
            await press(fixture, '.wizard-step__back button');
            const wentBackToDetails = page(fixture).querySelector('app-details-step') !== null;
            await clickContinue(fixture);

            //then
            expect(wentBackToDetails).toBe(true);
            expect(textOf(fixture, '.target__count')).toBe('3');
        });

        it('stops at the details when every slot of the week is unavailable', async () => {
            //given
            const everySlot = weekSlots([]).map(slot => slot.id);
            const blockedWeek = studentOf('Teacher Levi', Transmission.manual, weekSlots(everySlot));
            provideOpenLinkIdentifying(identifyingAs(blockedWeek));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('.details__no-availability')).not.toBeNull();
            expect(continueButton(fixture)).toBeNull();
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
            await reachTarget(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('app-slots-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('.slot-day').length).toBe(6);
            expect(page(fixture).querySelectorAll('.slot-chip').length).toBe(22);
            expect(page(fixture).querySelectorAll('.slot-chip--unavailable').length).toBe(unavailable.length);
            expect(textOf(fixture, '.student-shell__caption')).toContain('studentForm.weekTeacherCaption');
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

        it('goes back from the grid to the target', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await press(fixture, '.wizard-step__back button');

            //then
            expect(page(fixture).querySelector('app-target-step')).not.toBeNull();
        });
    });

    describe('picking slots', () => {
        it('numbers picks in the order they are tapped', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await addPick(fixture, 'sunday-afternoon');
            await addPick(fixture, 'monday-evening');
            await addPick(fixture, 'wednesday-afternoon');

            //then
            expect(rankOn(fixture, 'sunday-afternoon')).toBe('1');
            expect(rankOn(fixture, 'monday-evening')).toBe('2');
            expect(rankOn(fixture, 'wednesday-afternoon')).toBe('3');
            expect(rankOn(fixture, 'tuesday-morning')).toBeUndefined();
            expect(chip(fixture, 'monday-evening').classList).toContain('slot-chip--picked');
            expect(pickSheet(fixture)).toBeNull();
        });

        it('never opens an unavailable slot', async () => {
            //given
            const levi = studentOf('Teacher Levi', Transmission.manual, weekSlots(['sunday-morning']));
            provideOpenLinkIdentifying(identifyingAs(levi));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'sunday-morning');

            //then
            expect(chip(fixture, 'sunday-morning').disabled).toBe(true);
            expect(pickSheet(fixture)).toBeNull();
            expect(chip(fixture, 'sunday-noon').disabled).toBe(false);
        });

        it('offers a new pick as the next rank, Single, with no constraint', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await addPick(fixture, 'sunday-afternoon');

            //when
            await tapChip(fixture, 'thursday-evening');

            //then
            expect(textOf(fixture, '.pick-sheet__rank')).toBe('2');
            expect(textOf(fixture, '.pick-sheet__time')).toBe('18:00–22:00');
            expect(isChecked(fixture, '.pick-sheet__single')).toBe(true);
            expect(constraintValue(fixture)).toBe('');
            expect(page(fixture).querySelector('.pick-sheet__remove')).toBeNull();
        });

        it('keeps Double and the constraint on that pick only', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await typeConstraint(fixture, 'only after 16:00');
            await press(fixture, '.pick-sheet__save button');
            await addPick(fixture, 'monday-evening');

            //when
            await tapChip(fixture, 'sunday-afternoon');
            const doubleChecked = isChecked(fixture, '.pick-sheet__double');
            const firstConstraint = constraintValue(fixture);
            await press(fixture, '.pick-sheet__cancel button');
            await tapChip(fixture, 'monday-evening');

            //then
            expect(doubleChecked).toBe(true);
            expect(firstConstraint).toBe('only after 16:00');
            expect(isChecked(fixture, '.pick-sheet__single')).toBe(true);
            expect(constraintValue(fixture)).toBe('');
        });

        it('removes a pick and moves every later pick up one rank', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await addPick(fixture, 'sunday-afternoon');
            await addPick(fixture, 'monday-evening');
            await addPick(fixture, 'wednesday-afternoon');

            //when
            await tapChip(fixture, 'sunday-afternoon');
            await press(fixture, '.pick-sheet__remove button');

            //then
            expect(rankOn(fixture, 'sunday-afternoon')).toBeUndefined();
            expect(rankOn(fixture, 'monday-evening')).toBe('1');
            expect(rankOn(fixture, 'wednesday-afternoon')).toBe('2');
        });

        it('changes a pick in place without moving it', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await addPick(fixture, 'sunday-afternoon');
            await addPick(fixture, 'monday-evening');

            //when
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await press(fixture, '.pick-sheet__save button');
            await tapChip(fixture, 'sunday-afternoon');

            //then
            expect(textOf(fixture, '.pick-sheet__rank')).toBe('1');
            expect(isChecked(fixture, '.pick-sheet__double')).toBe(true);
            expect(rankOn(fixture, 'monday-evening')).toBe('2');
        });

        it('adds nothing when a new pick is cancelled', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');

            //when
            await press(fixture, '.pick-sheet__cancel button');

            //then
            expect(pickSheet(fixture)).toBeNull();
            expect(rankOn(fixture, 'sunday-afternoon')).toBeUndefined();
            expect(continueButton(fixture)!.disabled).toBe(true);
        });

        it('closes on Escape and never shows the previous slot in the next sheet', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-morning');
            await typeConstraint(fixture, 'typed for Sunday');

            //when
            page(fixture)
                .querySelector('app-pick-sheet')!
                .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            await fixture.whenStable();
            const closed = pickSheet(fixture) === null;
            await tapChip(fixture, 'monday-evening');

            //then
            expect(closed).toBe(true);
            expect(page(fixture).querySelectorAll('.pick-sheet').length).toBe(1);
            expect(textOf(fixture, '.pick-sheet__time')).toBe('18:00–22:00');
            expect(constraintValue(fixture)).toBe('');
        });

        it('returns focus to the chip that opened the sheet', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            chip(fixture, 'monday-evening').focus();
            await tapChip(fixture, 'monday-evening');
            const focusedInSheet = pickSheet(fixture)!.contains(document.activeElement);

            //when
            await press(fixture, '.pick-sheet__cancel button');

            //then
            expect(focusedInSheet).toBe(true);
            expect(document.activeElement).toBe(chip(fixture, 'monday-evening'));
        });

        it('closes on Escape wherever the focus has gone', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');

            //when
            document.body.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            await fixture.whenStable();

            //then
            expect(pickSheet(fixture)).toBeNull();
        });

        it('lets the sheet itself hold focus so a click on its text keeps focus inside', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');

            //when
            pickSheet(fixture)!.focus();

            //then
            expect(document.activeElement).toBe(pickSheet(fixture));
        });

        it('focuses the chosen session type when a pick is reopened', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await press(fixture, '.pick-sheet__save button');

            //when
            await tapChip(fixture, 'sunday-afternoon');

            //then
            expect(document.activeElement).toBe(page(fixture).querySelector('.pick-sheet__double'));
        });

        it('names the sheet by the rank, day, window and time of its slot', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'thursday-evening');

            //then
            expect(accessibleNameOf(fixture, '.pick-sheet')).toMatch(
                /studentForm\.slots\.pickedRank \S*studentForm\.slotLabel 18:00–22:00$/,
            );
        });

        it('marks only open chips as opening a dialog', async () => {
            //given
            const levi = studentOf('Teacher Levi', Transmission.manual, weekSlots(['sunday-morning']));
            provideOpenLinkIdentifying(identifyingAs(levi));
            const fixture = await renderPage();

            //when
            await reachSlots(fixture);

            //then
            expect(chip(fixture, 'sunday-noon').getAttribute('aria-haspopup')).toBe('dialog');
            expect(chip(fixture, 'sunday-morning').hasAttribute('aria-haspopup')).toBe(false);
        });

        it('caps the constraint at 200 characters', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'sunday-afternoon');

            //then
            expect((page(fixture).querySelector('#pick-constraint') as HTMLTextAreaElement).maxLength).toBe(200);
        });

        it('keeps Review locked until the first pick, then shows target and picked counts', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            const lockedBeforePicking = continueButton(fixture)!.disabled;

            //when
            await addPick(fixture, 'sunday-afternoon');

            //then
            expect(lockedBeforePicking).toBe(true);
            expect(continueButton(fixture)!.disabled).toBe(false);
            expect(textOf(fixture, '.slots__summary')).toContain('studentForm.slots.summary');
            expect(page(fixture).querySelector('.slots__covered')).not.toBeNull();
        });
    });
});
