# Task 4 of 6: Client — identify step and read-only details (US-50 + US-27)

> Part of [US-50/51/27: Identity & Routing](README.md). Requires task 3 complete. Work on branch `52-us-50-51-27-identity-and-routing`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Modify: `state\student-form.store.ts`
- Create: `ui\components\wizard-step\wizard-step.component.ts`, `.html`, `.scss`
- Create: `ui\components\identify-step\identify-step.component.ts`, `.html`, `.scss`
- Create: `ui\components\details-step\details-step.component.ts`, `.html`, `.scss`
- Modify: `ui\pages\student-form\student-form.page.ts`, `.html`
- Modify (test): `ui\pages\student-form\student-form.page.spec.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 3): `IdentifyStatus`, `StudentFormStep`, `WIZARD_STEPS`, `stepNumberOf`, `toNationalIdDigits`, `isNationalIdCandidate`, `isCompleteNationalId`, `nameInitials`, `Transmission`, `IdentifyStudentRequest`, `IdentifyStudentResponse`, `SlotForIdentifyStudentResponse`, `SubmissionsApiService.identifyStudent`; existing store members `view`, `isOpen`, `weekParams`, `closesAt`, `open`, `retry`.
- Produces (task 5 relies on these exact names):
  - `StudentFormStore` readonly: `identifyStatus: Signal<IdentifyStatus>`, `student: Signal<IdentifyStudentResponse | null>`, `studentName`, `teacherName`, `teacherInitials: Signal<string>`, `hasAvailability: Signal<boolean>`, `currentStep: Signal<StudentFormStep>`, `stepNumber: Signal<number>`, `stepCount: number`, `captionKey: Signal<string | null>`, `captionParams: Signal<{ weekNumber; weekRange; teacherName }>`; methods `changeNationalId(digits: string)`, `requestLookup(digits: string)`, `continueToDetails()`.
  - `WizardStepComponent` (`app-wizard-step`): inputs `stepNumber`, `stepCount` (required), `heading`, `intro` (optional strings); default content slot + `[wizardFooter]` footer slot.
  - `DetailsStepComponent` (`app-details-step`): inputs `stepNumber`, `stepCount`, `teacherName`, `teacherInitials`, `studentName`, `carName`, `transmission`, `weekLabel`, `hasAvailability`.
  - Page spec helpers and fixtures `weekSlots(unavailable)`, `studentOf(...)`, `COHEN_STUDENT`, `identifyingAs(...)`, `provideOpenLinkIdentifying(...)`, `renderPage()`, `page(...)`, `typeNationalId(...)`, `submitIdentify(...)`, `continueButton(...)`, `clickContinue(...)`, `identifyAndContinue(...)` (task 5 adds cases using them).
  - Translation keys: `studentForm.step`, `.continue`, `.weekTeacherCaption`, `.identify.*`, `.details.*` (full list in Step 3).

Precedents to open before coding: the current `state\student-form.store.ts` (resource + 404-swallowing loader + `isStatus`), `ui\components\status-message\` (dumb component, content projection), `ui\pages\student-form\student-form.page.html` (dates card styles to mirror for the details card), `features\auth\login.component.html` (`p-message`, `fluid` button), mockup `student.jsx` → `SIdKnown`, `SIdUnknown`, `SDetails`, `shared.jsx` → `MStep`, `MFoot`, `MkField`.

- [ ] **Step 1: Write the failing page spec**

Replace `ui\pages\student-form\student-form.page.spec.ts` with the file below. The read-only table loses its `open` row (the open view now asks for an ID), and new `describe` blocks cover the identify and details steps.

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { Observable, of, throwError } from 'rxjs';
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
    return { studentName: 'Test Student', teacherName, carName: 'Corolla White', transmission, slots };
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
});
```

Why these cases: separators (**Review Focus 3**), short-ID submit (leading zero dropped), 404/409/500 each map to their own message (README decision 3), "drops the previous result" is **Review Focus 5**, no-grid teacher is **Review Focus 2**. `TranslocoTestingModule` with empty translations renders keys, so assertions use CSS hooks, directly bound values (teacher/car name), and key names — never English copy. `vi` is a Vitest global in this workspace (the existing specs use `describe`/`it`/`expect` globals).

- [ ] **Step 2: Run the spec to verify it fails**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — the open view still renders the slice-1 status panel, so `#national-id`, `.identify__*`, `.details__*` are missing (the five read-only rows still pass).

- [ ] **Step 3: Translations (en + he, same commit)**

In **both** files, replace the whole `"studentForm"` object with the one below (it keeps every slice-1 key except `open`, which the ID step replaces — README decision 13).

`client\public\i18n\en.json`:

```json
  "studentForm": {
    "loading": "Loading the form…",
    "weekCaption": "Week {{weekNumber}} · {{weekRange}}",
    "weekTeacherCaption": "Week {{weekNumber}} · {{weekRange}} · {{teacherName}}",
    "step": "Step {{current}} of {{total}}",
    "continue": "Continue",
    "window": {
      "opens": "Opens",
      "closes": "Closes",
      "closed": "Closed"
    },
    "notYetOpen": {
      "title": "Submissions aren't open yet",
      "body": "The form for week {{weekNumber}} ({{weekRange}}) isn't open yet.",
      "hint": "Come back through the same link — no need for a new one."
    },
    "closed": {
      "title": "Submissions are closed",
      "body": "The form for week {{weekNumber}} ({{weekRange}}) has closed.",
      "hint": "If the school reopens the window, this link will work again."
    },
    "invalidLink": {
      "title": "This link isn't valid",
      "body": "We couldn't find a form for this link. Ask your school for the current link."
    },
    "loadFailed": {
      "title": "Something went wrong",
      "body": "We couldn't load the form. Check your connection and try again.",
      "retry": "Try again"
    },
    "identify": {
      "title": "Enter your national ID",
      "body": "We match it to the school roster to load your details — no password needed.",
      "label": "National ID",
      "hint": "9 digits, as registered with the school",
      "checking": "Checking the roster…",
      "foundTitle": "Found you — {{studentName}}.",
      "foundBody": "Let's collect your availability for week {{weekNumber}} with {{teacherName}}.",
      "notOnRosterTitle": "We don't have you on file.",
      "notOnRosterBody": "This ID isn't on the school's roster, so there's nothing to submit. Please contact your school to be added.",
      "invalidId": "This doesn't look like a valid national ID. Check the digits and try again.",
      "failed": "We couldn't check your ID right now. Check your connection and try again."
    },
    "details": {
      "lead": "You are submitting availability for",
      "student": "Student",
      "transmission": "Transmission",
      "car": "Car",
      "week": "Week",
      "transmissions": {
        "automatic": "Automatic",
        "manual": "Manual"
      },
      "noAvailabilityTitle": "No lesson slots yet.",
      "noAvailabilityBody": "{{teacherName}} hasn't published availability for this week. Please contact your school."
    }
  },
```

`client\public\i18n\he.json`:

```json
  "studentForm": {
    "loading": "טוען את הטופס…",
    "weekCaption": "שבוע {{weekNumber}} · {{weekRange}}",
    "weekTeacherCaption": "שבוע {{weekNumber}} · {{weekRange}} · {{teacherName}}",
    "step": "שלב {{current}} מתוך {{total}}",
    "continue": "המשך",
    "window": {
      "opens": "פתיחה",
      "closes": "סגירה",
      "closed": "נסגר"
    },
    "notYetOpen": {
      "title": "ההגשה עוד לא נפתחה",
      "body": "הטופס לשבוע {{weekNumber}} ({{weekRange}}) עוד לא פתוח.",
      "hint": "חזרו דרך אותו הקישור — אין צורך בקישור חדש."
    },
    "closed": {
      "title": "ההגשה נסגרה",
      "body": "הטופס לשבוע {{weekNumber}} ({{weekRange}}) נסגר.",
      "hint": "אם בית הספר יפתח את החלון מחדש, הקישור יעבוד שוב."
    },
    "invalidLink": {
      "title": "הקישור אינו תקין",
      "body": "לא מצאנו טופס עבור הקישור הזה. בקשו מבית הספר את הקישור העדכני."
    },
    "loadFailed": {
      "title": "משהו השתבש",
      "body": "לא הצלחנו לטעון את הטופס. בדקו את החיבור ונסו שוב.",
      "retry": "נסו שוב"
    },
    "identify": {
      "title": "הזינו מספר תעודת זהות",
      "body": "נאתר אתכם ברשימת התלמידים של בית הספר כדי לטעון את הפרטים שלכם — בלי סיסמה.",
      "label": "תעודת זהות",
      "hint": "9 ספרות, כפי שרשום בבית הספר",
      "checking": "בודקים ברשימת התלמידים…",
      "foundTitle": "מצאנו אתכם — {{studentName}}.",
      "foundBody": "בואו נאסוף את הזמינות שלכם לשבוע {{weekNumber}} אצל {{teacherName}}.",
      "notOnRosterTitle": "אינכם מופיעים אצלנו.",
      "notOnRosterBody": "תעודת הזהות הזו לא מופיעה ברשימת התלמידים של בית הספר, ולכן אין מה להגיש. פנו לבית הספר כדי שיוסיפו אתכם.",
      "invalidId": "זה לא נראה כמו מספר תעודת זהות תקין. בדקו את הספרות ונסו שוב.",
      "failed": "לא הצלחנו לבדוק את תעודת הזהות כרגע. בדקו את החיבור ונסו שוב."
    },
    "details": {
      "lead": "אתם מגישים זמינות עבור",
      "student": "תלמיד/ה",
      "transmission": "תיבת הילוכים",
      "car": "רכב",
      "week": "שבוע",
      "transmissions": {
        "automatic": "אוטומטית",
        "manual": "ידנית"
      },
      "noAvailabilityTitle": "עדיין אין שעות לשבוע הזה.",
      "noAvailabilityBody": "אין עדיין זמינות שפורסמה אצל {{teacherName}} לשבוע הזה. פנו לבית הספר."
    }
  },
```

Match the file's existing indentation (the snippets above use the files' 2-space JSON indent; keep the trailing comma only if another top-level key follows `studentForm` — in both files `general` does). Then verify both files parse and mirror each other:

```bash
node -e "const f=(o,p='')=>Object.entries(o).flatMap(([k,v])=>typeof v==='object'?f(v,p+k+'.'):[p+k]);const en=f(require('./public/i18n/en.json')),he=f(require('./public/i18n/he.json'));console.log(en.filter(k=>!he.includes(k)),he.filter(k=>!en.includes(k)))"
```

Expected: `[] []`. Also confirm nothing else referenced the deleted keys: `grep -rn "studentForm.open" src` → no matches once Step 7 lands.

- [ ] **Step 4: Store — identify lookup, wizard step, captions**

Replace `state\student-form.store.ts` with:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { GetPublicationByLinkResponse } from '../data/get-publication-by-link.response';
import { IdentifyStudentResponse } from '../data/identify-student.response';
import { SubmissionsApiService } from '../data/submissions-api.service';
import { IdentifyStatus } from '../domain/identify-status.enum';
import { formatWindowInstant } from '../domain/jerusalem-time';
import { nameInitials } from '../domain/name-initials';
import { isCompleteNationalId, isNationalIdCandidate } from '../domain/national-id-input';
import { stepNumberOf, WIZARD_STEPS } from '../domain/student-form-step';
import { StudentFormStep } from '../domain/student-form-step.enum';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };

const CAPTION_KEY_BY_STEP: Record<StudentFormStep, string | null> = {
    [StudentFormStep.identify]: null,
    [StudentFormStep.details]: 'studentForm.weekCaption',
    [StudentFormStep.slots]: 'studentForm.weekTeacherCaption',
};

interface IdentifyLookup {
    token: string;
    nationalId: string;
}

type IdentifyResult =
    | { status: IdentifyStatus.found; student: IdentifyStudentResponse }
    | { status: IdentifyStatus.notOnRoster | IdentifyStatus.invalidId };

@Injectable()
export class StudentFormStore {
    private readonly api = inject(SubmissionsApiService);
    private readonly language = inject(LanguageService);

    private readonly linkToken = signal<string | null>(null);
    private readonly nationalId = signal<string | null>(null);
    private readonly step = signal(StudentFormStep.identify);

    private readonly publicationResource = resource({
        params: () => this.linkToken() ?? undefined,
        loader: ({ params: token }) => this.loadByLink(token),
    });

    private readonly identifyLookup = computed<IdentifyLookup | undefined>(() => {
        const token = this.linkToken();
        const nationalId = this.nationalId();

        return token && nationalId ? { token, nationalId } : undefined;
    });

    private readonly identifyResource = resource({
        params: () => this.identifyLookup(),
        loader: ({ params: lookup }) => this.identify(lookup),
    });

    private readonly publication = computed<GetPublicationByLinkResponse | null>(() =>
        this.publicationResource.hasValue() ? this.publicationResource.value() : null,
    );

    private readonly identifyResult = computed<IdentifyResult | null>(() =>
        this.identifyResource.hasValue() ? this.identifyResource.value() : null,
    );

    readonly view = computed<StudentFormView>(() => {
        if (!this.linkToken() || this.publicationResource.isLoading()) {
            return StudentFormView.loading;
        }

        if (this.publicationResource.error()) {
            return StudentFormView.loadFailed;
        }

        const publication = this.publication();

        return publication ? viewForPublicationState(publication.state) : StudentFormView.invalidLink;
    });

    readonly isOpen = computed(() => this.view() === StudentFormView.open);

    readonly weekParams = computed(() => {
        const publication = this.publication();

        if (!publication) {
            return EMPTY_WEEK_PARAMS;
        }

        return {
            weekNumber: publication.weekNumber,
            weekRange: weekRangeLabel(publication.weekStart, this.language.lang()),
        };
    });

    readonly opensAt = computed(() => this.formatInstant(this.publication()?.windowStartUtc));
    readonly closesAt = computed(() => this.formatInstant(this.publication()?.windowEndUtc));

    readonly identifyStatus = computed<IdentifyStatus>(() => {
        if (!this.identifyLookup()) {
            return IdentifyStatus.idle;
        }

        if (this.identifyResource.isLoading()) {
            return IdentifyStatus.checking;
        }

        if (this.identifyResource.error()) {
            return IdentifyStatus.failed;
        }

        return this.identifyResult()?.status ?? IdentifyStatus.checking;
    });

    readonly student = computed<IdentifyStudentResponse | null>(() => {
        const result = this.identifyResult();
        const isFound = this.identifyStatus() === IdentifyStatus.found;

        return isFound && result?.status === IdentifyStatus.found ? result.student : null;
    });

    readonly studentName = computed(() => this.student()?.studentName ?? '');
    readonly teacherName = computed(() => this.student()?.teacherName ?? '');
    readonly teacherInitials = computed(() => nameInitials(this.teacherName()));
    readonly hasAvailability = computed(() => (this.student()?.slots.length ?? 0) > 0);

    readonly currentStep = this.step.asReadonly();
    readonly stepNumber = computed(() => stepNumberOf(this.step()));
    readonly stepCount = WIZARD_STEPS.length;

    readonly captionKey = computed(() => (this.isOpen() ? CAPTION_KEY_BY_STEP[this.step()] : null));
    readonly captionParams = computed(() => ({ ...this.weekParams(), teacherName: this.teacherName() }));

    open(token: string): void {
        this.linkToken.set(token);
    }

    retry(): void {
        this.publicationResource.reload();
    }

    changeNationalId(digits: string): void {
        const lookup = isCompleteNationalId(digits) ? digits : null;
        this.nationalId.set(lookup);
    }

    requestLookup(digits: string): void {
        if (!isNationalIdCandidate(digits)) {
            return;
        }

        if (digits === this.nationalId()) {
            this.identifyResource.reload();
            return;
        }

        this.nationalId.set(digits);
    }

    continueToDetails(): void {
        if (!this.student()) {
            return;
        }

        this.step.set(StudentFormStep.details);
    }

    private formatInstant(utcIso: string | undefined): string {
        return utcIso ? formatWindowInstant(utcIso, this.language.lang()) : '';
    }

    private async loadByLink(token: string): Promise<GetPublicationByLinkResponse | null> {
        try {
            return await firstValueFrom(this.api.getPublicationByLink(token));
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return null;
            }

            throw error;
        }
    }

    private async identify(lookup: IdentifyLookup): Promise<IdentifyResult> {
        try {
            const request = { nationalId: lookup.nationalId };
            const student = await firstValueFrom(this.api.identifyStudent(lookup.token, request));

            return { status: IdentifyStatus.found, student };
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return { status: IdentifyStatus.notOnRoster };
            }

            if (isStatus(error, HTTP_CONFLICT)) {
                return { status: IdentifyStatus.invalidId };
            }

            throw error;
        }
    }
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
```

How it behaves (read this before touching it):
- **Reads are `resource()`** (client-state rule 7): identify changes nothing server-side, so it is a params-keyed read even though it travels as POST. A new `{ token, nationalId }` supersedes any in-flight request — a stale answer can never be shown (**Review Focus 5**).
- `changeNationalId` runs on every keystroke: a complete 9-digit ID starts a lookup; anything else clears the lookup, so an edit always drops the previous result. `requestLookup` is the explicit Continue/Enter path — it accepts shorter IDs and retries the same ID (`reload()`) after a failure or a "not on file" (e.g., the admin just re-uploaded the roster).
- 404 → `notOnRoster`, 409 → `invalidId` are **values**, not errors; any other failure is a resource error → `failed`. The backend's ProblemDetails text is never shown (README decision 3).
- `student` is gated on `identifyStatus() === found`, so nothing renders a student while a newer lookup is running.
- The national ID lives only in a private signal for the visit — never `localStorage`, never the URL (**Review Focus 4**). The store is page-provided (slice 1 decision 7), so it is gone when the student leaves.
- `captionKey`/`captionParams` feed the shell caption: none on the ID step, week on details, week · teacher on the slots step (task 5 renders that step).

- [ ] **Step 5: `WizardStepComponent` — counter, heading, intro, sticky footer**

`ui\components\wizard-step\wizard-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-wizard-step',
    imports: [TranslocoPipe],
    templateUrl: './wizard-step.component.html',
    styleUrl: './wizard-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly heading = input<string>('');
    readonly intro = input<string>('');
}
```

`ui\components\wizard-step\wizard-step.component.html`:

```html
<section class="wizard-step">
    <div class="wizard-step__body">
        <p class="wizard-step__counter">
            {{ 'studentForm.step' | transloco: { current: stepNumber(), total: stepCount() } }}
        </p>
        @if (heading()) {
            <h1 class="wizard-step__heading">{{ heading() }}</h1>
        }
        @if (intro()) {
            <p class="wizard-step__intro">{{ intro() }}</p>
        }
        <ng-content />
    </div>
    <footer class="wizard-step__footer">
        <ng-content select="[wizardFooter]" />
    </footer>
</section>
```

`ui\components\wizard-step\wizard-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.wizard-step {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.wizard-step__body {
    display: flex;
    flex-direction: column;
    gap: 0.625rem;
    padding: 1.25rem 1rem 1.5rem;
}

.wizard-step__counter {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.66rem;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--app-text-muted);
}

.wizard-step__heading {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.3rem;
    line-height: 1.25;
    letter-spacing: -0.005em;
    color: var(--app-ink);
}

.wizard-step__intro {
    margin: 0;
    font-size: 0.85rem;
    line-height: 1.55;
    color: var(--app-text-secondary);
}

.wizard-step__footer {
    position: sticky;
    inset-block-end: 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-block-start: auto;
    padding: 0.8rem 1rem;
    border-block-start: 1px solid var(--app-border);
    background: var(--app-bg-card);
}

.wizard-step__footer:empty {
    display: none;
}
```

Mockup mapping: `MStep` → counter, `stTitle`/`stBody` → heading/intro (same sizes as slice 1's `status__heading`), `MFoot` → sticky footer. A step with no footer action (details without availability; the slots step in this slice) collapses the empty footer.

- [ ] **Step 6: `IdentifyStepComponent` (mockup `SIdKnown` / `SIdUnknown`)**

`ui\components\identify-step\identify-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { IdentifyStatus } from '../../../domain/identify-status.enum';
import { isNationalIdCandidate, toNationalIdDigits } from '../../../domain/national-id-input';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const RESUBMITTABLE_STATUSES: ReadonlySet<IdentifyStatus> = new Set([IdentifyStatus.found, IdentifyStatus.failed]);
const REJECTED_STATUSES: ReadonlySet<IdentifyStatus> = new Set([IdentifyStatus.notOnRoster, IdentifyStatus.invalidId]);

@Component({
    selector: 'app-identify-step',
    imports: [TranslocoPipe, ButtonModule, InputTextModule, MessageModule, WizardStepComponent],
    templateUrl: './identify-step.component.html',
    styleUrl: './identify-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IdentifyStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly status = input.required<IdentifyStatus>();
    readonly studentName = input.required<string>();
    readonly teacherName = input.required<string>();
    readonly weekNumber = input.required<number>();

    readonly nationalIdChanged = output<string>();
    readonly lookupRequested = output<string>();
    readonly continued = output<void>();

    protected readonly statuses = IdentifyStatus;
    protected readonly digits = signal('');

    protected readonly isRejected = computed(() => REJECTED_STATUSES.has(this.status()));
    protected readonly canSubmit = computed(() => {
        const status = this.status();
        const isFreshCandidate = status === IdentifyStatus.idle && isNationalIdCandidate(this.digits());

        return RESUBMITTABLE_STATUSES.has(status) || isFreshCandidate;
    });

    protected onInput(event: Event): void {
        const field = event.target as HTMLInputElement;
        const digits = toNationalIdDigits(field.value);

        this.digits.set(digits);
        this.nationalIdChanged.emit(digits);
    }

    protected onSubmit(event: Event): void {
        event.preventDefault();

        if (!this.canSubmit()) {
            return;
        }

        if (this.status() === IdentifyStatus.found) {
            this.continued.emit();
            return;
        }

        this.lookupRequested.emit(this.digits());
    }
}
```

`ui\components\identify-step\identify-step.component.html`:

```html
<form class="identify" novalidate (submit)="onSubmit($event)">
    <app-wizard-step
        [stepNumber]="stepNumber()"
        [stepCount]="stepCount()"
        [heading]="'studentForm.identify.title' | transloco"
        [intro]="'studentForm.identify.body' | transloco">
        <div class="identify__field">
            <label class="identify__label" for="national-id">{{ 'studentForm.identify.label' | transloco }}</label>
            <input
                pInputText
                id="national-id"
                class="identify__input"
                type="text"
                inputmode="numeric"
                autocomplete="off"
                aria-describedby="national-id-hint"
                [invalid]="isRejected()"
                (input)="onInput($event)" />
            <small id="national-id-hint" class="identify__hint">{{ 'studentForm.identify.hint' | transloco }}</small>
        </div>

        <div class="identify__result" aria-live="polite">
            @switch (status()) {
                @case (statuses.checking) {
                    <p class="identify__checking">
                        <i class="pi pi-spin pi-spinner" aria-hidden="true"></i>
                        {{ 'studentForm.identify.checking' | transloco }}
                    </p>
                }
                @case (statuses.found) {
                    <p-message severity="success" class="identify__found">
                        <span>
                            <strong>{{ 'studentForm.identify.foundTitle' | transloco: { studentName: studentName() } }}</strong>
                            {{
                                'studentForm.identify.foundBody'
                                    | transloco: { weekNumber: weekNumber(), teacherName: teacherName() }
                            }}
                        </span>
                    </p-message>
                }
                @case (statuses.notOnRoster) {
                    <p-message severity="error" class="identify__not-on-roster">
                        <span>
                            <strong>{{ 'studentForm.identify.notOnRosterTitle' | transloco }}</strong>
                            {{ 'studentForm.identify.notOnRosterBody' | transloco }}
                        </span>
                    </p-message>
                }
                @case (statuses.invalidId) {
                    <p-message severity="error" class="identify__invalid">
                        {{ 'studentForm.identify.invalidId' | transloco }}
                    </p-message>
                }
                @case (statuses.failed) {
                    <p-message severity="warn" class="identify__failed">
                        {{ 'studentForm.identify.failed' | transloco }}
                    </p-message>
                }
            }
        </div>

        <div wizardFooter>
            <p-button type="submit" fluid [label]="'studentForm.continue' | transloco" [disabled]="!canSubmit()" />
        </div>
    </app-wizard-step>
</form>
```

`ui\components\identify-step\identify-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.identify {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.identify__field {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    margin-block-start: 0.5rem;
}

.identify__label {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.78rem;
    color: var(--app-ink);
}

.identify__input {
    width: 100%;
    min-height: 2.75rem;
    font-size: 1rem;
    letter-spacing: 0.04em;
}

.identify__hint {
    font-size: 0.72rem;
    color: var(--app-text-secondary);
}

.identify__result:empty {
    display: none;
}

.identify__checking {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0;
    font-size: 0.8rem;
    color: var(--app-text-secondary);
}
```

Behavior notes:
- The field is a single signal-backed input (README decision 11). Its raw text is never rewritten while typing (no `[value]` binding) so the caret never jumps; only the separator-free digits leave the component.
- `inputmode="numeric"` opens the digit keypad on phones; `autocomplete="off"` keeps the browser from storing the ID; `font-size: 1rem` prevents iOS zoom-on-focus; `min-height` keeps the 40px touch target.
- Continue is enabled when the student can act: "Found you" (→ continue), "couldn't check" (→ retry), or a fresh 1–9 digit entry (→ look up). After "not on file" / "invalid" it stays disabled until the ID is edited (mockup: "Continue stays locked until the ID matches the roster").
- A disabled submit button also blocks Enter — which is exactly the locked state; in every enabled state, Enter and the button do the same thing.
- If `[invalid]` is not an input of the installed `pInputText`, fall back to `[class.p-invalid]="isRejected()"` (checked while planning: the installed PrimeNG 21 `InputText` exposes an `invalid()` input).

- [ ] **Step 7: `DetailsStepComponent` (mockup `SDetails`, US-27)**

`ui\components\details-step\details-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { MessageModule } from 'primeng/message';
import { Transmission } from '../../../domain/transmission.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-details-step',
    imports: [TranslocoPipe, MessageModule, WizardStepComponent],
    templateUrl: './details-step.component.html',
    styleUrl: './details-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DetailsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly teacherName = input.required<string>();
    readonly teacherInitials = input.required<string>();
    readonly studentName = input.required<string>();
    readonly carName = input.required<string>();
    readonly transmission = input.required<Transmission>();
    readonly weekLabel = input.required<string>();
    readonly hasAvailability = input.required<boolean>();

    protected readonly transmissionKey = computed(() => `studentForm.details.transmissions.${this.transmission()}`);
}
```

`ui\components\details-step\details-step.component.html`:

```html
<app-wizard-step [stepNumber]="stepNumber()" [stepCount]="stepCount()">
    <p class="details__lead">{{ 'studentForm.details.lead' | transloco }}</p>
    <div class="details__teacher">
        <span class="details__avatar" aria-hidden="true">{{ teacherInitials() }}</span>
        <h1 class="details__teacher-name">{{ teacherName() }}</h1>
    </div>

    <dl class="details__card">
        <div class="details__row">
            <dt>{{ 'studentForm.details.student' | transloco }}</dt>
            <dd class="details__student">{{ studentName() }}</dd>
        </div>
        <div class="details__row">
            <dt>{{ 'studentForm.details.transmission' | transloco }}</dt>
            <dd class="details__transmission">{{ transmissionKey() | transloco }}</dd>
        </div>
        <div class="details__row">
            <dt>{{ 'studentForm.details.car' | transloco }}</dt>
            <dd class="details__car">{{ carName() }}</dd>
        </div>
        <div class="details__row">
            <dt>{{ 'studentForm.details.week' | transloco }}</dt>
            <dd>{{ weekLabel() }}</dd>
        </div>
    </dl>

    @if (!hasAvailability()) {
        <p-message severity="warn" class="details__no-availability">
            <span>
                <strong>{{ 'studentForm.details.noAvailabilityTitle' | transloco }}</strong>
                {{ 'studentForm.details.noAvailabilityBody' | transloco: { teacherName: teacherName() } }}
            </span>
        </p-message>
    }
</app-wizard-step>
```

`ui\components\details-step\details-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.details__lead {
    margin: 0;
    font-size: 0.85rem;
    color: var(--app-text-secondary);
}

.details__teacher {
    display: flex;
    align-items: center;
    gap: 0.8rem;
}

.details__avatar {
    display: flex;
    flex: none;
    align-items: center;
    justify-content: center;
    width: 3.25rem;
    height: 3.25rem;
    border-radius: 999px;
    background: var(--app-steel-light);
    color: var(--app-whale);
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.1rem;
}

.details__teacher-name {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.6rem;
    line-height: 1.2;
    color: var(--app-ink);
}

.details__card {
    margin: 0.375rem 0 0;
    padding: 0.25rem 1rem;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);

    dt {
        margin: 0;
        font-size: 0.78rem;
        color: var(--app-text-secondary);
    }

    dd {
        margin: 0;
        font-family: var(--app-font-display);
        font-weight: 700;
        font-size: 0.875rem;
        color: var(--app-ink);
        text-align: end;
    }
}

.details__row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding-block: 0.75rem;
}

.details__row + .details__row {
    border-block-start: 1px solid var(--app-bg-muted);
}

.details__card .details__transmission {
    color: var(--p-sky-700);
}
```

The two-class selector outranks `.details__card dd`, so only the transmission value takes the accent (mockup `skyDark`).

US-27 mapping: "You are submitting availability for" + the teacher as the page's `h1` (avatar initials from the roster name), then a read-only card — Student, **Transmission** (from the roster car), Car, Week. There is nothing to choose and nothing to contest: no teacher picker, no "not my teacher", no transmission question (ADR 0003). The Continue button arrives in task 5 together with the step it leads to, so this commit has no dead control.

- [ ] **Step 8: Wire the page**

Replace `ui\pages\student-form\student-form.page.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormStep } from '../../../domain/student-form-step.enum';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';
import { DetailsStepComponent } from '../../components/details-step/details-step.component';
import { IdentifyStepComponent } from '../../components/identify-step/identify-step.component';
import { StatusMessageComponent } from '../../components/status-message/status-message.component';
import { StudentShellComponent } from '../../components/student-shell/student-shell.component';

@Component({
    selector: 'app-student-form-page',
    imports: [
        TranslocoPipe,
        ButtonModule,
        ProgressSpinnerModule,
        StatusMessageComponent,
        StudentShellComponent,
        IdentifyStepComponent,
        DetailsStepComponent,
    ],
    providers: [StudentFormStore],
    templateUrl: './student-form.page.html',
    styleUrl: './student-form.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
    protected readonly store = inject(StudentFormStore);
    protected readonly views = StudentFormView;
    protected readonly steps = StudentFormStep;

    readonly token = input.required<string>();

    constructor() {
        effect(() => this.store.open(this.token()));
    }
}
```

In `ui\pages\student-form\student-form.page.html`:

1. Replace the first line (`<app-student-shell [caption]="store.isOpen() ? … : ''">`) with:

```html
@let captionKey = store.captionKey();
<app-student-shell [caption]="captionKey ? (captionKey | transloco: store.captionParams()) : ''">
```

2. Replace the whole `@case (views.open) { … }` block (the slice-1 "Submissions are open" status message) with:

```html
        @case (views.open) {
            @switch (store.currentStep()) {
                @case (steps.identify) {
                    <app-identify-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [status]="store.identifyStatus()"
                        [studentName]="store.studentName()"
                        [teacherName]="store.teacherName()"
                        [weekNumber]="store.weekParams().weekNumber"
                        (nationalIdChanged)="store.changeNationalId($event)"
                        (lookupRequested)="store.requestLookup($event)"
                        (continued)="store.continueToDetails()" />
                }
                @case (steps.details) {
                    @if (store.student(); as student) {
                        <app-details-step
                            [stepNumber]="store.stepNumber()"
                            [stepCount]="store.stepCount"
                            [teacherName]="student.teacherName"
                            [teacherInitials]="store.teacherInitials()"
                            [studentName]="student.studentName"
                            [carName]="student.carName"
                            [transmission]="student.transmission"
                            [weekLabel]="'studentForm.weekCaption' | transloco: store.weekParams()"
                            [hasAvailability]="store.hasAvailability()" />
                    }
                }
            }
        }
```

Leave every other `@case` and `student-form.page.scss` untouched. `@let` keeps the caption expression free of function calls (client-state "no function calls in templates except signal reads").

- [ ] **Step 9: Run the spec to verify it passes**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — the 5 read-only rows, 7 identify-step cases, 2 details-step cases, plus the task-3 domain/API specs and the untouched routes spec.

Then: `npm run build` → success, no new warnings; `grep -rn "studentForm.open" src` → no matches.

- [ ] **Step 10: Quick look in the browser (optional, full verification is task 6)**

With `api-smoke` (task 2) and `client` running, open `/s/{LINK}` at the mobile preset: the ID step shows "שלב 1 מתוך 3", typing `000000018` shows "מצאנו אתכם — Smoke Student A." and Continue leads to the details card for Smoke Cohen. Stop here if anything looks off and fix before committing.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/features/student-form client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): student national-ID step and read-only roster details

The open link now asks for the national ID, looks it up against the
roster (found / not on file / invalid / retry), and confirms the teacher,
car and transmission read-only before continuing."
```

---

**Next:** [task-05-client-teacher-grid.md](task-05-client-teacher-grid.md)
