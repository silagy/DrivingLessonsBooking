# Task 2 of 8: Rule-violation toasts speak the selected language (L1, client half, TDD)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires task 1 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Modify: `client\src\app\shared\models\problem-details.ts` (one optional field)
- Modify: `client\src\app\core\services\toast.service.ts` (whole file below)
- Create: `client\src\app\core\services\toast.service.spec.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (one new `errors` namespace each)

**Interfaces:**
- Consumes: task 1's `"code"` on every 404/409 body (`"carNameMustNotBeEmpty"`, `"teacherNotFound"`, …; 56 codes, one per exception class except `NotFoundException` and `AuthenticationFailedException`).
- Produces: `ProblemDetails.code?: string`. `ToastService.apiError(error: unknown): void` keeps its signature and callers (`teachers.store`, `cars.store`, `roster.store`, `week-schedules.store`, `publications.store`). It now shows, in order of preference: `errors.{code}` if that key exists; else `errors.notFound` for a 404 or `errors.conflict` for a 409; else `general.unexpectedError`. It never shows `detail`. Task 8 Step 5 checks real toasts end to end.

**Why:** README defect L1, decisions 3 and 4. Today `resolveMessage` returns `problem.detail`, which is English, developer-facing and full of GUIDs. A key lookup gives the message in the active language, and the generic fallbacks guarantee that a code without a key (a future exception) still produces a sentence in the right language.

- [ ] **Step 1: Write the failing spec**

Create `client\src\app\core\services\toast.service.spec.ts`:

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { MessageService } from 'primeng/api';
import { ToastService } from './toast.service';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_SERVER_ERROR = 500;

const EN = {
    errors: {
        carNameMustNotBeEmpty: 'Enter the car\'s name.',
        conflict: 'This change conflicts with the latest data. Refresh the page and try again.',
        notFound: 'This item no longer exists. Refresh the page.',
    },
    general: {
        unexpectedError: 'Something went wrong. Please try again.',
    },
};

function setUp() {
    const add = vi.fn();
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: EN },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection(), { provide: MessageService, useValue: { add } }],
    });

    return { toast: TestBed.inject(ToastService), add };
}

function problem(status: number, body: object): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body });
}

function shownSummary(add: ReturnType<typeof vi.fn>): string {
    return add.mock.calls[0][0].summary;
}

describe('ToastService', () => {
    describe('apiError', () => {
        it('names the broken rule in the active language', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                detail: 'Car name must not be empty.',
                code: 'carNameMustNotBeEmpty',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Enter the car\'s name.');
        });

        it('falls back to the generic message for a rule it has no key for', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                detail: 'Publication 3f2a9c1e must be draft.',
                code: 'publicationMustBeDraft',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('This change conflicts with the latest data. Refresh the page and try again.');
        });

        it('says the item is gone when a not-found problem has no code', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_NOT_FOUND, {
                status: HTTP_NOT_FOUND,
                title: 'Not Found',
                detail: 'Teacher 3f2a9c1e was not found.',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('This item no longer exists. Refresh the page.');
        });

        it('never shows the server detail of an unexpected failure', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_SERVER_ERROR, { status: HTTP_SERVER_ERROR, detail: 'Npgsql: connection reset' });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Something went wrong. Please try again.');
        });

        it('shows the unexpected error for anything that is not an HTTP failure', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.apiError(undefined);

            //then
            expect(shownSummary(add)).toBe('Something went wrong. Please try again.');
        });
    });
});
```

`EN` deliberately has no `errors.publicationMustBeDraft`, so the second spec exercises the fallback with a real code (README Review Focus 1).

- [ ] **Step 2: Run the spec to verify it fails**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. Exactly four `ToastService` specs fail, each showing the server `detail` instead of the expected message (`expected 'Car name must not be empty.' to be 'Enter the car\'s name.'`, and so on). `shows the unexpected error for anything that is not an HTTP failure` passes. Every other spec in the suite still passes.

- [ ] **Step 3: Add `code` to the client's `ProblemDetails`**

Replace the whole of `client\src\app\shared\models\problem-details.ts` with:

```ts
export interface ProblemDetails {
    status: number;
    title: string;
    detail?: string;
    code?: string;
}
```

- [ ] **Step 4: Map the code in `ToastService`**

Replace the whole of `client\src\app\core\services\toast.service.ts` with:

```ts
import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { ProblemDetails } from '../../shared/models/problem-details';

const UNEXPECTED_ERROR_KEY = 'general.unexpectedError';

const GENERIC_ERROR_KEYS: Partial<Record<number, string>> = {
    [HttpStatusCode.NotFound]: 'errors.notFound',
    [HttpStatusCode.Conflict]: 'errors.conflict',
};

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly messages = inject(MessageService);
    private readonly transloco = inject(TranslocoService);

    success(key: string): void {
        this.messages.add({ severity: 'success', summary: this.transloco.translate(key) });
    }

    apiError(error: unknown): void {
        this.messages.add({ severity: 'error', summary: this.resolveMessage(error) });
    }

    private resolveMessage(error: unknown): string {
        if (!(error instanceof HttpErrorResponse)) {
            return this.transloco.translate(UNEXPECTED_ERROR_KEY);
        }

        const problem = error.error as ProblemDetails | null;
        const ruleKey = problem?.code ? `errors.${problem.code}` : undefined;

        if (ruleKey && this.hasTranslation(ruleKey)) {
            return this.transloco.translate(ruleKey);
        }

        return this.transloco.translate(GENERIC_ERROR_KEYS[error.status] ?? UNEXPECTED_ERROR_KEY);
    }

    private hasTranslation(key: string): boolean {
        return this.transloco.translate(key) !== key;
    }
}
```

Transloco returns the key itself for a missing key (no `useFallbackTranslation` in `app.config.ts`), and in dev mode it also logs `Missing translation for 'errors.…'` to the console. That warning is the signal a new exception needs its key.

- [ ] **Step 5: Run the spec**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the five `ToastService` specs. One `Missing translation for 'errors.publicationMustBeDraft'` console warning from the fallback spec is expected.

- [ ] **Step 6: Add the `errors` namespace to both translation files**

1. In `client\public\i18n\en.json`, change

```json
    "unexpectedError": "Something went wrong. Please try again."
  },
  "formErrors": {
```

to

```json
    "unexpectedError": "Something went wrong. Please try again."
  },
  "errors": {
    "conflict": "This change conflicts with the latest data. Refresh the page and try again.",
    "notFound": "This item no longer exists. Refresh the page.",
    "addressMustNotBeEmpty": "The address can't be empty.",
    "carAlreadyDeleted": "This car was already deleted. Refresh the page.",
    "carNameMustNotBeEmpty": "Enter the car's name.",
    "carNotFound": "This car no longer exists. Refresh the page.",
    "carTypeMustNotBeEmpty": "Enter the car's type.",
    "emailMustBeValid": "Enter a valid email address.",
    "licenseTypeMustNotBeEmpty": "The license type can't be empty.",
    "nationalIdMustBeAtMostNineDigits": "A national ID has at most nine digits.",
    "nationalIdMustBeDigits": "A national ID contains digits only.",
    "nationalIdMustHaveValidCheckDigit": "This national ID isn't valid. Check the digits.",
    "phoneNumberMustBeValid": "Enter a valid phone number.",
    "publicationLinkNotFound": "No week is available for this link.",
    "publicationMustBeClosed": "This week's submission window isn't closed yet. Refresh the page.",
    "publicationMustBeDraft": "This week was already published. Refresh the page.",
    "publicationMustBeOpen": "The submission window isn't open. Refresh the page.",
    "publicationMustBePublished": "This week hasn't been published yet. Refresh the page.",
    "publicationNotFound": "This week's publication no longer exists. Refresh the page.",
    "rankMustBePositive": "Ranks start at 1.",
    "rosterFileMustContainRequiredColumns": "The file is missing required columns. Its first row must include: שם מלא, תעודת זהות, טלפון, מורה, רכב.",
    "rosterFileMustNotBeEmpty": "The file has no student rows.",
    "rosterFileNameMustNotBeEmpty": "The file has no name. Choose the file again.",
    "rosterImportFailureRowNumberMustBePositive": "A row number in the import report is invalid.",
    "rosterImportNotFound": "No roster has been uploaded yet.",
    "sessionTypeMustBeSingleOrDouble": "Choose Single or Double.",
    "shareableLinkTokenMustNotBeEmpty": "This link is incomplete.",
    "slotConstraintMustNotBeEmpty": "A constraint can't be blank.",
    "slotConstraintMustNotExceedMaxLength": "A constraint can be at most 200 characters.",
    "slotMustBeOpen": "This slot is already unavailable. Refresh the page.",
    "slotMustBeUnavailable": "This slot is already open. Refresh the page.",
    "slotNotFound": "This slot isn't in your teacher's week.",
    "slotNotInWeekSchedule": "This slot isn't part of the week. Refresh the page.",
    "studentAlreadyActive": "This student is already active.",
    "studentAlreadyDeactivated": "This student is already inactive.",
    "studentNameMustNotBeEmpty": "The student's name can't be empty.",
    "studentNotFound": "No active student on the roster has this national ID.",
    "submissionAlreadyExists": "A list was already sent for this week.",
    "submissionMustBeForPublicationAndStudent": "This list belongs to another week or student.",
    "submissionNotFound": "No list was sent for this week yet.",
    "submissionPicksMustCoverTarget": "Pick at least as many slots as the lessons you want.",
    "submissionSlotMustBeInWeekSchedule": "One of the picks isn't part of this week.",
    "submissionSlotMustBeOpen": "One of the picks is no longer available.",
    "submissionSlotMustBeRequestedOnce": "Each slot can be picked only once.",
    "submissionStudentMustBeActive": "Only students on the current roster can send a list.",
    "submissionWeekScheduleMustMatchStudentWeek": "This list doesn't match your teacher's week.",
    "submissionWindowEndMustBeAfterStart": "The window must close after it opens.",
    "submissionWindowMustBeOpen": "The submission window is closed.",
    "targetSessionCountMustBePositive": "Ask for at least one lesson.",
    "teacherAlreadyAssignedToCar": "This teacher is already assigned to the car. Refresh the page.",
    "teacherAlreadyDeleted": "This teacher was already deleted. Refresh the page.",
    "teacherNameMustNotBeEmpty": "Enter the teacher's name.",
    "teacherNotAssignedToCar": "This teacher isn't assigned to the car. Refresh the page.",
    "teacherNotFound": "This teacher no longer exists. Refresh the page.",
    "weekScheduleAlreadyExists": "This teacher's week is already prepared. Refresh the page.",
    "weekScheduleNotFound": "This teacher's week isn't prepared yet.",
    "weekStartMustBeSunday": "A week starts on Sunday.",
    "windowExtensionMustBeLater": "The new closing time must be later than the current one."
  },
  "formErrors": {
```

The column names in `rosterFileMustContainRequiredColumns` stay Hebrew in English too: they are the literal CSV headers the file must contain (`RosterCsvHeaders.Required`), not UI text.

2. In `client\public\i18n\he.json`, change

```json
    "unexpectedError": "משהו השתבש. נסו שוב."
  },
  "formErrors": {
```

to

```json
    "unexpectedError": "משהו השתבש. נסו שוב."
  },
  "errors": {
    "conflict": "השינוי מתנגש בנתונים העדכניים. רעננו את הדף ונסו שוב.",
    "notFound": "הפריט הזה כבר לא קיים. רעננו את הדף.",
    "addressMustNotBeEmpty": "הכתובת לא יכולה להיות ריקה.",
    "carAlreadyDeleted": "הרכב הזה כבר נמחק. רעננו את הדף.",
    "carNameMustNotBeEmpty": "הזינו את שם הרכב.",
    "carNotFound": "הרכב הזה כבר לא קיים. רעננו את הדף.",
    "carTypeMustNotBeEmpty": "הזינו את סוג הרכב.",
    "emailMustBeValid": "הזינו כתובת אימייל תקינה.",
    "licenseTypeMustNotBeEmpty": "סוג הרישיון לא יכול להיות ריק.",
    "nationalIdMustBeAtMostNineDigits": "תעודת זהות כוללת עד תשע ספרות.",
    "nationalIdMustBeDigits": "תעודת זהות כוללת ספרות בלבד.",
    "nationalIdMustHaveValidCheckDigit": "תעודת הזהות אינה תקינה. בדקו את הספרות.",
    "phoneNumberMustBeValid": "הזינו מספר טלפון תקין.",
    "publicationLinkNotFound": "אין שבוע זמין בקישור הזה.",
    "publicationMustBeClosed": "חלון ההגשה של השבוע עדיין לא נסגר. רעננו את הדף.",
    "publicationMustBeDraft": "השבוע הזה כבר פורסם. רעננו את הדף.",
    "publicationMustBeOpen": "חלון ההגשה אינו פתוח. רעננו את הדף.",
    "publicationMustBePublished": "השבוע הזה עדיין לא פורסם. רעננו את הדף.",
    "publicationNotFound": "הפרסום של השבוע הזה כבר לא קיים. רעננו את הדף.",
    "rankMustBePositive": "הדירוג מתחיל מ-1.",
    "rosterFileMustContainRequiredColumns": "בקובץ חסרות עמודות חובה. השורה הראשונה צריכה לכלול: שם מלא, תעודת זהות, טלפון, מורה, רכב.",
    "rosterFileMustNotBeEmpty": "בקובץ אין שורות תלמידים.",
    "rosterFileNameMustNotBeEmpty": "לקובץ אין שם. בחרו את הקובץ שוב.",
    "rosterImportFailureRowNumberMustBePositive": "מספר שורה בדוח הייבוא אינו תקין.",
    "rosterImportNotFound": "עדיין לא הועלתה רשימת תלמידים.",
    "sessionTypeMustBeSingleOrDouble": "בחרו שיעור יחיד או כפול.",
    "shareableLinkTokenMustNotBeEmpty": "הקישור אינו שלם.",
    "slotConstraintMustNotBeEmpty": "אילוץ לא יכול להיות ריק.",
    "slotConstraintMustNotExceedMaxLength": "אילוץ יכול להכיל עד 200 תווים.",
    "slotMustBeOpen": "המשבצת הזו כבר לא זמינה. רעננו את הדף.",
    "slotMustBeUnavailable": "המשבצת הזו כבר פתוחה. רעננו את הדף.",
    "slotNotFound": "השעה הזו אינה בשבוע של המורה שלכם.",
    "slotNotInWeekSchedule": "המשבצת הזו אינה חלק מהשבוע. רעננו את הדף.",
    "studentAlreadyActive": "התלמיד הזה כבר פעיל.",
    "studentAlreadyDeactivated": "התלמיד הזה כבר לא פעיל.",
    "studentNameMustNotBeEmpty": "שם התלמיד לא יכול להיות ריק.",
    "studentNotFound": "אין ברשימה תלמיד פעיל עם תעודת הזהות הזו.",
    "submissionAlreadyExists": "כבר נשלחה רשימה לשבוע הזה.",
    "submissionMustBeForPublicationAndStudent": "הרשימה הזו שייכת לשבוע אחר או לתלמיד אחר.",
    "submissionNotFound": "עדיין לא נשלחה רשימה לשבוע הזה.",
    "submissionPicksMustCoverTarget": "בחרו לפחות כמה שעות כמספר השיעורים שתרצו.",
    "submissionSlotMustBeInWeekSchedule": "אחת הבחירות אינה חלק מהשבוע הזה.",
    "submissionSlotMustBeOpen": "אחת הבחירות כבר לא זמינה.",
    "submissionSlotMustBeRequestedOnce": "אפשר לבחור כל שעה פעם אחת בלבד.",
    "submissionStudentMustBeActive": "רק תלמידים שברשימה הנוכחית יכולים לשלוח רשימה.",
    "submissionWeekScheduleMustMatchStudentWeek": "הרשימה לא תואמת את השבוע של המורה שלכם.",
    "submissionWindowEndMustBeAfterStart": "החלון צריך להיסגר אחרי שהוא נפתח.",
    "submissionWindowMustBeOpen": "חלון ההגשה סגור.",
    "targetSessionCountMustBePositive": "בקשו לפחות שיעור אחד.",
    "teacherAlreadyAssignedToCar": "המורה כבר משויך לרכב הזה. רעננו את הדף.",
    "teacherAlreadyDeleted": "המורה הזה כבר נמחק. רעננו את הדף.",
    "teacherNameMustNotBeEmpty": "הזינו את שם המורה.",
    "teacherNotAssignedToCar": "המורה לא משויך לרכב הזה. רעננו את הדף.",
    "teacherNotFound": "המורה הזה כבר לא קיים. רעננו את הדף.",
    "weekScheduleAlreadyExists": "השבוע של המורה הזה כבר הוכן. רעננו את הדף.",
    "weekScheduleNotFound": "השבוע של המורה הזה עדיין לא הוכן.",
    "weekStartMustBeSunday": "שבוע מתחיל ביום ראשון.",
    "windowExtensionMustBeLater": "מועד הסגירה החדש צריך להיות מאוחר מהמועד הנוכחי."
  },
  "formErrors": {
```

Wording follows `he.json`'s existing voice: plural imperative (`הזינו`, `בחרו`, `רעננו`), `משבצת` for an admin's grid slot and `שעה` where the student picks one (as in `studentForm.slots.*`).

- [ ] **Step 7: Check that every exception has its key, in both files**

Run from the repo root (Git Bash):

```bash
node -e "
const fs = require('fs'), path = require('path');
const dirs = ['src/DrivingLessons.Domain/Exceptions', 'src/DrivingLessons.Application/Common/Exceptions'];
const skip = new Set(['NotFoundException', 'AuthenticationFailedException']);
const codes = dirs.flatMap(d => fs.readdirSync(d)).map(f => path.basename(f, '.cs')).filter(n => !skip.has(n))
  .map(n => n.replace(/Exception$/, '')).map(n => n[0].toLowerCase() + n.slice(1));
for (const lang of ['en', 'he']) {
  const errors = require(path.resolve('client/public/i18n/' + lang + '.json')).errors ?? {};
  const missing = codes.filter(c => !(c in errors));
  const extra = Object.keys(errors).filter(k => !codes.includes(k) && k !== 'conflict' && k !== 'notFound');
  console.log(lang, 'codes:', codes.length, 'missing:', JSON.stringify(missing), 'extra:', JSON.stringify(extra));
}"
```

Expected, exactly:

```
en codes: 56 missing: [] extra: []
he codes: 56 missing: [] extra: []
```

The script lowercases the first letter, which is what `JsonNamingPolicy.CamelCase` does to these names (task 1): none of them starts with an acronym.

- [ ] **Step 8: Run the suite and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/shared/models/problem-details.ts client/src/app/core/services/toast.service.ts client/src/app/core/services/toast.service.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(i18n): show rule violations in the selected language

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
