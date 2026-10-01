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
import { CreateSubmissionRequest } from '../../../data/create-submission.request';
import { GetPublicationByLinkResponse } from '../../../data/get-publication-by-link.response';
import { IdentifyStudentRequest } from '../../../data/identify-student.request';
import {
    IdentifyStudentResponse,
    SlotForIdentifyStudentResponse,
    SubmissionForIdentifyStudentResponse,
} from '../../../data/identify-student.response';
import { SUBMISSION_WINDOW_CLOSED_PROBLEM } from '../../../data/problem-types';
import { SubmissionsApiService } from '../../../data/submissions-api.service';
import { SessionType } from '../../../domain/session-type.enum';
import { Transmission } from '../../../domain/transmission.enum';
import { StudentFormPage } from './student-form.page';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_SERVER_ERROR = 500;
const LINK_TOKEN = 'link-token';
const ROSTER_NATIONAL_ID = '000000018';
const FIRST_TIMER_NATIONAL_ID = '000000026';

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
type SubmitCommand = (token: string, request: CreateSubmissionRequest) => Observable<void>;

interface FakeSubmissionsApi {
    getPublicationByLink: () => Observable<GetPublicationByLinkResponse>;
    identifyStudent: IdentifyStudent;
    createSubmission: SubmitCommand;
    reviseSubmission: SubmitCommand;
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
        submission: null,
        slots,
    };
}

const COHEN_STUDENT = studentOf('Teacher Cohen', Transmission.automatic, weekSlots([]));

const SAVED_SUBMISSION: SubmissionForIdentifyStudentResponse = {
    targetCount: 2,
    lastSavedAtUtc: '2026-11-12T08:30:00Z',
    slotRequests: [
        { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
        { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
        { slotId: 'wednesday-evening', sessionType: SessionType.single, constraint: null },
    ],
};

const RETURNING_STUDENT: IdentifyStudentResponse = { ...COHEN_STUDENT, submission: SAVED_SUBMISSION };

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

function closingWindow(): () => Observable<never> {
    return () =>
        throwError(
            () =>
                new HttpErrorResponse({
                    status: HTTP_CONFLICT,
                    error: {
                        type: SUBMISSION_WINDOW_CLOSED_PROBLEM,
                        title: 'Conflict',
                        status: HTTP_CONFLICT,
                        detail: "Submissions are accepted only while the week's submission window is open.",
                    },
                }),
        );
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

function accepting(): SubmitCommand {
    return vi.fn(() => of(undefined));
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
    provideOpenLinkSubmitting(identifyStudent, accepting(), accepting());
}

function provideOpenLinkSubmitting(
    identifyStudent: IdentifyStudent,
    createSubmission: SubmitCommand,
    reviseSubmission: SubmitCommand,
): void {
    provideApi({
        getPublicationByLink: publicationIn(PublicationState.open),
        identifyStudent,
        createSubmission,
        reviseSubmission,
    });
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

async function reachReview(
    fixture: ComponentFixture<StudentFormPage>,
    targetCount: number,
    slotIds: readonly string[],
): Promise<void> {
    await reachTarget(fixture);

    for (let count = 1; count < targetCount; count++) {
        await press(fixture, '.target__increase');
    }

    await clickContinue(fixture);

    for (const slotId of slotIds) {
        await addPick(fixture, slotId);
    }

    await clickContinue(fixture);
}

function accessibleNameOf(fixture: ComponentFixture<StudentFormPage>, selector: string): string {
    const labelIds = page(fixture).querySelector(selector)!.getAttribute('aria-labelledby') ?? '';

    return labelIds
        .split(' ')
        .map(id => document.getElementById(id)?.textContent?.trim())
        .join(' ');
}

type MoveDirection = 'up' | 'down';

function reviewOrder(fixture: ComponentFixture<StudentFormPage>, selector = '.review__item'): string[] {
    return [...page(fixture).querySelectorAll<HTMLElement>(selector)].map(item => item.dataset['pickId'] ?? '');
}

function moveButton(
    fixture: ComponentFixture<StudentFormPage>,
    slotId: string,
    direction: MoveDirection,
): HTMLButtonElement {
    return page(fixture).querySelector(`[data-pick-id="${slotId}"] .review__move-${direction} button`) as HTMLButtonElement;
}

async function move(
    fixture: ComponentFixture<StudentFormPage>,
    slotId: string,
    direction: MoveDirection,
): Promise<void> {
    moveButton(fixture, slotId, direction).click();
    await fixture.whenStable();
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
        provideApi({
            getPublicationByLink: load,
            identifyStudent: identifyingAs(COHEN_STUDENT),
            createSubmission: accepting(),
            reviseSubmission: accepting(),
        });

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

    describe('review and submit', () => {
        it('lists the picks in rank order, the ones inside the target as preferred', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //then
            expect(page(fixture).querySelector('app-review-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(3);
            expect(page(fixture).querySelectorAll('.review__item--preferred').length).toBe(2);
            expect(page(fixture).querySelector('.review__not-enough')).toBeNull();
            expect(page(fixture).querySelector('.review__replaces')).toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(false);
        });

        it('submits the ranked list with each pick\'s session type and constraint', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await clickContinue(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await typeConstraint(fixture, '  only after 16:00 ');
            await press(fixture, '.pick-sheet__save button');
            await addPick(fixture, 'monday-evening');
            await addPick(fixture, 'wednesday-afternoon');
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'sunday-afternoon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'monday-evening', sessionType: SessionType.single, constraint: null },
                    { slotId: 'wednesday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(reviseSubmission).not.toHaveBeenCalled();
        });

        it('blocks a list shorter than the target with a clear message', async () => {
            //given
            const createSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon', 'monday-evening']);

            //when
            continueButton(fixture)!.click();
            await fixture.whenStable();

            //then
            expect(page(fixture).querySelector('.review__not-enough')).not.toBeNull();
            expect(page(fixture).querySelector('.review__unlock')).not.toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
            expect(createSubmission).not.toHaveBeenCalled();
        });

        it('lets the student lower the target from the not-enough message', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon']);

            //when
            await press(fixture, '.review__change-target button');

            //then
            expect(page(fixture).querySelector('app-target-step')).not.toBeNull();
            expect(textOf(fixture, '.target__count')).toBe('3');
        });

        it('sends the student back to the grid for more slots, keeping the picks', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 3, ['sunday-afternoon']);

            //when
            await press(fixture, '.review__add-slots button');

            //then
            expect(page(fixture).querySelector('app-slots-step')).not.toBeNull();
            expect(rankOn(fixture, 'sunday-afternoon')).toBe('1');
        });

        it('confirms the submission and explains self-service editing', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.submitted\.title$/);
            expect(textOf(fixture, '.student-form__submitted')).toContain('studentForm.submitted.bodyMany');
            expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
        });

        it('edits from the confirmation by replacing the submission', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);

            //when
            await press(fixture, '.student-form__edit button');
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(reviseSubmission).toHaveBeenCalledTimes(1);
        });

        it.each([
            { status: HTTP_CONFLICT, message: '.review__rejected', canRetry: false },
            { status: HTTP_NOT_FOUND, message: '.review__not-found', canRetry: false },
            { status: HTTP_SERVER_ERROR, message: '.review__failed', canRetry: true },
        ])('keeps the list on screen when the submit answers $status', async ({ status, message, canRetry }) => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), failingWith(status), accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector(message)).not.toBeNull();
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(1);
            expect(continueButton(fixture)!.disabled).toBe(!canRetry);
        });

        it('checks the form again after a rejection and shows the window has closed', async () => {
            //given
            const link = { state: PublicationState.open };
            provideApi({
                getPublicationByLink: () => publicationIn(link.state)(),
                identifyStudent: identifyingAs(COHEN_STUDENT),
                createSubmission: failingWith(HTTP_CONFLICT),
                reviseSubmission: accepting(),
            });
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);
            link.state = PublicationState.closed;

            //when
            await press(fixture, '.review__recheck button');

            //then
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.closed\.title$/);
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
        });

        it('checks the form again in an open window and replaces a list sent from another tab', async () => {
            //given
            const roster = { student: COHEN_STUDENT };
            const createSubmission: SubmitCommand = vi.fn(failingWith(HTTP_CONFLICT));
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(() => of(roster.student), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);
            roster.student = RETURNING_STUDENT;

            //when
            await press(fixture, '.review__recheck button');
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 1,
                slotRequests: [{ slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null }],
            });
        });

        it('checks the form again in an open window and drops a pick whose slot is no longer open', async () => {
            //given
            const roster = { student: COHEN_STUDENT };
            const createSubmission: SubmitCommand = vi
                .fn(accepting())
                .mockImplementationOnce(failingWith(HTTP_CONFLICT));
            provideOpenLinkSubmitting(() => of(roster.student), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);
            await clickContinue(fixture);
            roster.student = studentOf('Teacher Cohen', Transmission.automatic, weekSlots(['monday-evening']));

            //when
            await press(fixture, '.review__recheck button');
            const reviewItemCount = page(fixture).querySelectorAll('.review__item').length;
            await clickContinue(fixture);

            //then
            expect(reviewItemCount).toBe(1);
            expect(createSubmission).toHaveBeenLastCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 1,
                slotRequests: [{ slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null }],
            });
        });

        it('never submits twice while a submission is in flight', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(() => new Subject<void>());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(continueButton(fixture)!.disabled).toBe(true);
        });
    });

    describe('reordering the ranked list', () => {
        it('promotes a later pick and submits the new order', async () => {
            //given
            const createSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await clickContinue(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await chooseDouble(fixture);
            await typeConstraint(fixture, 'only after 16:00');
            await press(fixture, '.pick-sheet__save button');
            await addPick(fixture, 'monday-evening');
            await addPick(fixture, 'wednesday-afternoon');
            await clickContinue(fixture);

            //when
            await move(fixture, 'wednesday-afternoon', 'up');
            await move(fixture, 'wednesday-afternoon', 'up');
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'wednesday-afternoon', sessionType: SessionType.single, constraint: null },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'monday-evening', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('renumbers the ranks to the new order', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //when
            await move(fixture, 'wednesday-afternoon', 'up');
            await move(fixture, 'wednesday-afternoon', 'up');

            //then
            const ranks = [...page(fixture).querySelectorAll('.review__rank')].map(x => x.textContent?.trim());
            expect(reviewOrder(fixture)).toEqual(['wednesday-afternoon', 'sunday-afternoon', 'monday-evening']);
            expect(ranks).toEqual(['1', '2', '3']);
        });

        it('moves a pick down below the next one', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //when
            await move(fixture, 'sunday-afternoon', 'down');

            //then
            expect(reviewOrder(fixture)).toEqual(['monday-evening', 'sunday-afternoon', 'wednesday-afternoon']);
        });

        it('moves the preferred boundary with the order, so a promoted backup becomes preferred', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //when
            await move(fixture, 'wednesday-afternoon', 'up');
            await move(fixture, 'wednesday-afternoon', 'up');

            //then
            expect(reviewOrder(fixture, '.review__item--preferred')).toEqual(['wednesday-afternoon', 'sunday-afternoon']);
        });

        it('never moves the first pick up or the last pick down', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //then
            expect(moveButton(fixture, 'sunday-afternoon', 'up').disabled).toBe(true);
            expect(moveButton(fixture, 'sunday-afternoon', 'down').disabled).toBe(false);
            expect(moveButton(fixture, 'monday-evening', 'up').disabled).toBe(false);
            expect(moveButton(fixture, 'monday-evening', 'down').disabled).toBe(false);
            expect(moveButton(fixture, 'wednesday-afternoon', 'up').disabled).toBe(false);
            expect(moveButton(fixture, 'wednesday-afternoon', 'down').disabled).toBe(true);
        });

        it('names each move button by its rank and slot', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //then
            expect(moveButton(fixture, 'monday-evening', 'up').getAttribute('aria-label')).toMatch(
                /studentForm\.review\.moveUp$/,
            );
            expect(moveButton(fixture, 'sunday-afternoon', 'down').getAttribute('aria-label')).toMatch(
                /studentForm\.review\.moveDown$/,
            );
        });

        it('offers no reordering for a single pick', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //then
            expect(page(fixture).querySelector('.review__moves')).toBeNull();
            expect(page(fixture).querySelector('.review__reorder-hint')).toBeNull();
        });

        it('explains the controls while there is something to reorder', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //then
            expect(textOf(fixture, '.review__reorder-hint')).toMatch(/studentForm\.review\.reorderHint$/);
        });

        it('keeps focus on a pick that reaches the top', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);
            moveButton(fixture, 'wednesday-afternoon', 'up').focus();

            //when
            await move(fixture, 'wednesday-afternoon', 'up');
            const focusedAfterFirstMove = document.activeElement;
            await move(fixture, 'wednesday-afternoon', 'up');

            //then
            expect(focusedAfterFirstMove).toBe(moveButton(fixture, 'wednesday-afternoon', 'up'));
            expect(document.activeElement).toBe(moveButton(fixture, 'wednesday-afternoon', 'down'));
        });

        it('announces the new rank of a moved pick, and starts silent on every visit to the review', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);
            const before = textOf(fixture, '.review__announcement');

            //when
            await move(fixture, 'monday-evening', 'up');
            const afterMove = textOf(fixture, '.review__announcement');
            await press(fixture, '.wizard-step__back button');
            await clickContinue(fixture);

            //then
            expect(before).toBe('');
            expect(afterMove).toMatch(/studentForm\.review\.movedTo$/);
            expect(page(fixture).querySelector('.review__announcement')!.getAttribute('aria-live')).toBe('polite');
            expect(textOf(fixture, '.review__announcement')).toBe('');
        });

        it('shows the new ranks on the grid and appends a new pick after them', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);
            await move(fixture, 'wednesday-afternoon', 'up');
            await move(fixture, 'wednesday-afternoon', 'up');

            //when
            await press(fixture, '.wizard-step__back button');
            const badges = ['wednesday-afternoon', 'sunday-afternoon', 'monday-evening'].map(x => rankOn(fixture, x));
            await addPick(fixture, 'thursday-evening');
            await clickContinue(fixture);

            //then
            expect(badges).toEqual(['1', '2', '3']);
            expect(reviewOrder(fixture)).toEqual([
                'wednesday-afternoon',
                'sunday-afternoon',
                'monday-evening',
                'thursday-evening',
            ]);
        });

        it('locks reordering while the list is being sent', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(() => new Subject<void>());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //when
            await clickContinue(fixture);

            //then
            expect(moveButton(fixture, 'sunday-afternoon', 'down').disabled).toBe(true);
            expect(moveButton(fixture, 'monday-evening', 'up').disabled).toBe(true);
        });

        it('reorders a saved list and replaces it in the new order', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            await clickContinue(fixture);

            //when
            await move(fixture, 'wednesday-evening', 'up');
            await move(fixture, 'wednesday-evening', 'up');
            await clickContinue(fixture);

            //then
            expect(createSubmission).not.toHaveBeenCalled();
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'wednesday-evening', sessionType: SessionType.single, constraint: null },
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
        });
    });

    describe('returning student', () => {
        it('welcomes a returning student back and offers to edit the saved submission', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(textOf(fixture, '.identify__welcome-back')).toContain('studentForm.identify.welcomeBackTitle');
            expect(textOf(fixture, '.identify__welcome-back')).toContain('studentForm.identify.welcomeBackBodyMany');
            expect(page(fixture).querySelector('.identify__found')).toBeNull();
            expect(continueButton(fixture)!.textContent).toContain('studentForm.identify.editSubmission');
        });

        it('greets a first-time student without a welcome back', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(page(fixture).querySelector('.identify__welcome-back')).toBeNull();
            expect(page(fixture).querySelector('.identify__found')).not.toBeNull();
            expect(continueButton(fixture)!.textContent).toContain('studentForm.continue');
        });

        it('shows the roster details read-only before the saved list', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await identifyAndContinue(fixture);

            //then
            expect(page(fixture).querySelector('app-details-step')).not.toBeNull();
            expect(page(fixture).querySelectorAll('app-details-step input, app-details-step select').length).toBe(0);
        });

        it('starts the target at the saved count', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();

            //when
            await reachTarget(fixture);

            //then
            expect(textOf(fixture, '.target__count')).toBe('2');
        });

        it('shows the saved picks ranked on the grid with their session type and constraint', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'monday-noon');

            //then
            expect(rankOn(fixture, 'monday-noon')).toBe('1');
            expect(rankOn(fixture, 'sunday-afternoon')).toBe('2');
            expect(rankOn(fixture, 'wednesday-evening')).toBe('3');
            expect(isChecked(fixture, '.pick-sheet__double')).toBe(true);
            expect(constraintValue(fixture)).toBe('only after 16:00');
        });

        it('replaces the saved submission with the edited list and says the changes are saved', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachTarget(fixture);
            await press(fixture, '.target__increase');
            await clickContinue(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await press(fixture, '.pick-sheet__remove button');
            await addPick(fixture, 'thursday-morning');
            await addPick(fixture, 'friday-noon');
            await clickContinue(fixture);
            const replacesNoticeShown = page(fixture).querySelector('.review__replaces') !== null;

            //when
            await clickContinue(fixture);

            //then
            expect(replacesNoticeShown).toBe(true);
            expect(createSubmission).not.toHaveBeenCalled();
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 3,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'wednesday-evening', sessionType: SessionType.single, constraint: null },
                    { slotId: 'thursday-morning', sessionType: SessionType.single, constraint: null },
                    { slotId: 'friday-noon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.submitted\.revisedTitle$/);
        });

        it('tells the student when saved picks are no longer offered and never sends them back', async () => {
            //given
            const reviseSubmission = accepting();
            const regridded = { ...RETURNING_STUDENT, slots: weekSlots(['monday-noon', 'wednesday-evening']) };
            provideOpenLinkSubmitting(identifyingAs(regridded), accepting(), reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            const survivingRank = rankOn(fixture, 'sunday-afternoon');
            await clickContinue(fixture);
            const droppedNotice = textOf(fixture, '.review__dropped');
            const notEnoughShown = page(fixture).querySelector('.review__not-enough') !== null;
            await press(fixture, '.review__add-slots button');
            await addPick(fixture, 'tuesday-evening');
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(survivingRank).toBe('1');
            expect(droppedNotice).toContain('studentForm.review.droppedMany');
            expect(notEnoughShown).toBe(true);
            expect(reviseSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                    { slotId: 'tuesday-evening', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('tells the student on the slots step when none of the saved picks is offered any more', async () => {
            //given
            const regridded = {
                ...RETURNING_STUDENT,
                slots: weekSlots(['monday-noon', 'sunday-afternoon', 'wednesday-evening']),
            };
            provideOpenLinkIdentifying(identifyingAs(regridded));
            const fixture = await renderPage();

            //when
            await reachSlots(fixture);

            //then
            expect(page(fixture).querySelectorAll('.slot-chip__rank').length).toBe(0);
            expect(textOf(fixture, '.slots__dropped')).toContain('studentForm.review.droppedMany');
        });

        it('says nothing about dropped picks when every saved pick is still offered', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(RETURNING_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelectorAll('.review__item').length).toBe(3);
            expect(page(fixture).querySelector('.review__dropped')).toBeNull();
        });

        it('edits again from the confirmation any number of times, always replacing', async () => {
            //given
            const createSubmission = accepting();
            const reviseSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), createSubmission, reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            await clickContinue(fixture);
            await clickContinue(fixture);

            //when
            await press(fixture, '.student-form__edit button');
            await press(fixture, '.wizard-step__back button');
            await tapChip(fixture, 'wednesday-evening');
            await press(fixture, '.pick-sheet__remove button');
            await clickContinue(fixture);
            await clickContinue(fixture);
            await press(fixture, '.student-form__edit button');
            await clickContinue(fixture);

            //then
            expect(createSubmission).not.toHaveBeenCalled();
            expect(reviseSubmission).toHaveBeenCalledTimes(3);
            expect(reviseSubmission).toHaveBeenLastCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.submitted\.revisedTitle$/);
        });

        it('keeps the edits in progress when the form is checked again', async () => {
            //given
            const reviseSubmission: SubmitCommand = vi
                .fn(accepting())
                .mockImplementationOnce(failingWith(HTTP_CONFLICT));
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), accepting(), reviseSubmission);
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'wednesday-evening');
            await press(fixture, '.pick-sheet__remove button');
            await clickContinue(fixture);
            await clickContinue(fixture);

            //when
            await press(fixture, '.review__recheck button');
            const reviewItemCount = page(fixture).querySelectorAll('.review__item').length;
            await clickContinue(fixture);

            //then
            expect(reviewItemCount).toBe(2);
            expect(reviseSubmission).toHaveBeenLastCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'monday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('starts fresh when a returning ID is replaced by a first-timer\'s', async () => {
            //given
            provideOpenLinkIdentifying(
                vi.fn((_token: string, request: IdentifyStudentRequest) =>
                    of(request.nationalId === ROSTER_NATIONAL_ID ? RETURNING_STUDENT : COHEN_STUDENT),
                ),
            );
            const fixture = await renderPage();
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //when
            await typeNationalId(fixture, FIRST_TIMER_NATIONAL_ID);
            await clickContinue(fixture);
            await clickContinue(fixture);
            const target = textOf(fixture, '.target__count');
            await clickContinue(fixture);

            //then
            expect(target).toBe('1');
            expect(page(fixture).querySelectorAll('.slot-chip__rank').length).toBe(0);
        });
    });

    describe('window closed mid-submit', () => {
        it('tells a first-time student the window just closed and that their list was not saved', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(closingWindow());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(createSubmission).toHaveBeenCalledTimes(1);
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.closedMidSubmit\.title$/);
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain('studentForm.closedMidSubmit.bodyNew');
            expect(page(fixture).querySelector('app-review-step')).toBeNull();
            expect(page(fixture).querySelector('.student-shell__caption')).toBeNull();
            expect(page(fixture).querySelectorAll('app-status-message button').length).toBe(0);
        });

        it('tells a returning student their changes were not saved and the earlier list stands', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(RETURNING_STUDENT), accepting(), closingWindow());
            const fixture = await renderPage();
            await reachSlots(fixture);
            await clickContinue(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.status__heading')).toMatch(/studentForm\.closedMidSubmit\.title$/);
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain(
                'studentForm.closedMidSubmit.bodyChanges',
            );
        });

        it('says the edit was not saved when the window closes during an edit from the confirmation', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), accepting(), closingWindow());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);
            await clickContinue(fixture);
            await press(fixture, '.student-form__edit button');

            //when
            await clickContinue(fixture);

            //then
            expect(textOf(fixture, '.student-form__closed-mid-submit')).toContain(
                'studentForm.closedMidSubmit.bodyChanges',
            );
        });

        it('still offers a recheck for any other rejection', async () => {
            //given
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), failingWith(HTTP_CONFLICT), accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelector('.review__rejected')).not.toBeNull();
            expect(page(fixture).querySelector('.review__recheck')).not.toBeNull();
            expect(page(fixture).querySelector('.student-form__closed-mid-submit')).toBeNull();
        });
    });
});
