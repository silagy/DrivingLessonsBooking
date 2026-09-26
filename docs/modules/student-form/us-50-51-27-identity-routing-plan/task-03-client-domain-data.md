# Task 3 of 6: Client — domain helpers, DTOs, identify API call

> Part of [US-50/51/27: Identity & Routing](README.md). Requires task 2 complete (the JSON shape below is what task 2 Step 5 printed). Work on branch `52-us-50-51-27-identity-and-routing`; client commands run from `client\`.

**Files** (all under `client\src\app\features\student-form\`):
- Create: `domain\transmission.enum.ts`
- Create: `domain\identify-status.enum.ts`
- Create: `domain\student-form-step.enum.ts`
- Create: `domain\student-form-step.ts` + `domain\student-form-step.spec.ts`
- Create: `domain\national-id-input.ts` + `domain\national-id-input.spec.ts`
- Create: `domain\name-initials.ts` + `domain\name-initials.spec.ts`
- Create: `domain\slot-day.ts` + `domain\slot-day.spec.ts`
- Modify: `domain\week-label.ts` (extract `dateInWeek`)
- Create: `data\identify-student.request.ts`
- Create: `data\identify-student.response.ts`
- Modify: `data\submissions-api.service.ts` + Create: `data\submissions-api.service.spec.ts`

**Interfaces:**
- Consumes: task 2's `POST api/submissions/by-link/{token}/identify` JSON; existing shared enums `DayOfWeek`, `SlotWindow`, `SlotState` (`client\src\app\shared\models\`); existing `weekDates` / `weekRangeLabel` in `domain\week-label.ts`.
- Produces (tasks 4–5 rely on these exact names):
  - `enum Transmission { automatic, manual }` (string values)
  - `enum IdentifyStatus { idle, checking, found, notOnRoster, invalidId, failed }`
  - `enum StudentFormStep { identify, details, slots }`, `WIZARD_STEPS: readonly StudentFormStep[]`, `stepNumberOf(step: StudentFormStep): number`
  - `toNationalIdDigits(input: string): string`, `isNationalIdCandidate(digits: string): boolean` (1–9 digits), `isCompleteNationalId(digits: string): boolean` (exactly 9)
  - `nameInitials(name: string): string`
  - `interface StudentSlot { id; day: DayOfWeek; window: SlotWindow; state: SlotState; startLocal: string; endLocal: string }`, `interface SlotChip { id; window; timeLabel; isUnavailable }`, `interface SlotDay { day; dateLabel; isShortDay; chips: SlotChip[] }`, `groupSlotsByDay(slots: readonly StudentSlot[], weekStart: string, locale: string): SlotDay[]`
  - `dateInWeek(weekStart: string, dayOffset: number): Date`
  - `interface IdentifyStudentRequest { nationalId: string }`, `interface IdentifyStudentResponse { studentName; teacherName; carName; transmission: Transmission; slots: SlotForIdentifyStudentResponse[] }`, `interface SlotForIdentifyStudentResponse` (same fields as `StudentSlot`)
  - `SubmissionsApiService.identifyStudent(token: string, request: IdentifyStudentRequest): Observable<IdentifyStudentResponse>`

`domain\` stays free of Angular and of `data\` imports (layer direction `ui → state → data → domain`): `StudentSlot` is the domain's own shape, and the `SlotForIdentifyStudentResponse` DTO is structurally identical, so the store can pass DTO slots straight in. Precedents: `domain\student-form-view.ts` + `.spec.ts` (pure function + `it.each`), `domain\week-label.ts`, `features\week-schedules\domain\slot.model.ts` (domain slot vs DTO slot), `data\submissions-api.service.ts`.

- [ ] **Step 1: Write the failing domain specs**

`domain\national-id-input.spec.ts`:

```typescript
import { isCompleteNationalId, isNationalIdCandidate, toNationalIdDigits } from './national-id-input';

describe('toNationalIdDigits', () => {
    it.each([
        ['000 000 018', '000000018'],
        ['000-000-018', '000000018'],
        ['‎000000018‏', '000000018'],
        [' 000000018 ', '000000018'],
        ['18', '18'],
    ])('reduces %j to %j', (input, digits) => {
        expect(toNationalIdDigits(input)).toBe(digits);
    });

    it('keeps characters that are not separators so the ID is not silently changed', () => {
        expect(toNationalIdDigits('00000001a')).toBe('00000001a');
    });
});

describe('isNationalIdCandidate', () => {
    it.each([
        ['1', true],
        ['18', true],
        ['000000018', true],
        ['', false],
        ['0000000181', false],
        ['00000001a', false],
    ])('%j → %s', (digits, expected) => {
        expect(isNationalIdCandidate(digits)).toBe(expected);
    });
});

describe('isCompleteNationalId', () => {
    it.each([
        ['000000018', true],
        ['00000018', false],
        ['0000000181', false],
        ['00000001a', false],
    ])('%j → %s', (digits, expected) => {
        expect(isCompleteNationalId(digits)).toBe(expected);
    });
});
```

The bidi-mark and NBSP rows are **Review Focus 3** (WhatsApp pastes carry U+200E/U+200F). No check-digit case: validity is the backend's rule (README decision 6).

`domain\name-initials.spec.ts`:

```typescript
import { nameInitials } from './name-initials';

describe('nameInitials', () => {
    it.each([
        ['Teacher Cohen', 'TC'],
        ['avi ben cohen', 'AC'],
        ['Cohen', 'C'],
        ['  Avi   Cohen  ', 'AC'],
        ['אבי כהן', 'אכ'],
        ['', ''],
    ])('%j → %j', (name, initials) => {
        expect(nameInitials(name)).toBe(initials);
    });
});
```

`domain\student-form-step.spec.ts`:

```typescript
import { StudentFormStep } from './student-form-step.enum';
import { stepNumberOf, WIZARD_STEPS } from './student-form-step';

describe('wizard steps', () => {
    it('runs identify → details → slots', () => {
        expect(WIZARD_STEPS).toEqual([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.slots]);
    });

    it.each([
        [StudentFormStep.identify, 1],
        [StudentFormStep.details, 2],
        [StudentFormStep.slots, 3],
    ])('numbers %s as step %i', (step, number) => {
        expect(stepNumberOf(step)).toBe(number);
    });
});
```

`domain\slot-day.spec.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { groupSlotsByDay, StudentSlot } from './slot-day';

const WEEK_START = '2026-10-04';

function slot(day: DayOfWeek, window: SlotWindow, state: SlotState): StudentSlot {
    return { id: `${day}-${window}`, day, window, state, startLocal: '07:00:00', endLocal: '12:00:00' };
}

describe('groupSlotsByDay', () => {
    it('groups Sunday through Friday in grid order whatever order the slots arrive in', () => {
        const slots = [
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.tuesday, SlotWindow.morning, SlotState.open),
        ];

        const days = groupSlotsByDay(slots, WEEK_START, 'en');

        expect(days.map(x => x.day)).toEqual([DayOfWeek.sunday, DayOfWeek.tuesday, DayOfWeek.friday]);
    });

    it('orders windows morning to evening within a day', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.evening, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.afternoon, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
        ];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en');

        expect(sunday.chips.map(x => x.window)).toEqual([
            SlotWindow.morning,
            SlotWindow.noon,
            SlotWindow.afternoon,
            SlotWindow.evening,
        ]);
    });

    it('marks unavailable slots and labels the wall-clock window', () => {
        const slots = [slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable)];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en');

        expect(sunday.chips[0]).toEqual({
            id: 'sunday-morning',
            window: SlotWindow.morning,
            timeLabel: '07:00–12:00',
            isUnavailable: true,
        });
    });

    it('labels each day with its date in the week', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
        ];

        const [sunday, friday] = groupSlotsByDay(slots, WEEK_START, 'en');

        expect(sunday.dateLabel).toBe('10/4');
        expect(friday.dateLabel).toBe('10/9');
    });

    it('flags a day offering fewer windows than a full day', () => {
        const slots = [
            ...Object.values(SlotWindow).map(window => slot(DayOfWeek.thursday, window, SlotState.open)),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.noon, SlotState.open),
        ];

        const [thursday, friday] = groupSlotsByDay(slots, WEEK_START, 'en');

        expect(thursday.isShortDay).toBe(false);
        expect(friday.isShortDay).toBe(true);
    });

    it('returns no days when the teacher has no grid this week', () => {
        expect(groupSlotsByDay([], WEEK_START, 'en')).toEqual([]);
    });
});
```

`isShortDay` is derived from the data (fewer windows than a full day), not from "is it Friday" — the grid shape stays the backend's (requirements §5.3); the client only labels what it receives. The time label is a wall-clock string slice, never a `Date` (client-i18n rule 6).

- [ ] **Step 2: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — the four new spec files cannot resolve `./national-id-input`, `./name-initials`, `./student-form-step(.enum)`, `./slot-day`.

- [ ] **Step 3: Enums**

`domain\transmission.enum.ts`:

```typescript
export enum Transmission {
    automatic = 'automatic',
    manual = 'manual',
}
```

`domain\identify-status.enum.ts`:

```typescript
export enum IdentifyStatus {
    idle = 'idle',
    checking = 'checking',
    found = 'found',
    notOnRoster = 'notOnRoster',
    invalidId = 'invalidId',
    failed = 'failed',
}
```

`domain\student-form-step.enum.ts`:

```typescript
export enum StudentFormStep {
    identify = 'identify',
    details = 'details',
    slots = 'slots',
}
```

`Transmission` duplicates `features\teachers\domain\transmission.enum.ts` on purpose — features never import each other (client-architecture anti-patterns).

- [ ] **Step 4: Wizard steps**

`domain\student-form-step.ts`:

```typescript
import { StudentFormStep } from './student-form-step.enum';

export const WIZARD_STEPS: readonly StudentFormStep[] = [
    StudentFormStep.identify,
    StudentFormStep.details,
    StudentFormStep.slots,
];

export function stepNumberOf(step: StudentFormStep): number {
    return WIZARD_STEPS.indexOf(step) + 1;
}
```

Slice 3 inserts `target` and `review` here; the counter follows automatically (README decision 8).

- [ ] **Step 5: National ID input mask**

`domain\national-id-input.ts`:

```typescript
const NATIONAL_ID_LENGTH = 9;
const SEPARATORS = /[\s\-‎‏‪-‮⁦-⁩]/g;
const CANDIDATE = /^\d{1,9}$/;

export function toNationalIdDigits(input: string): string {
    return input.replace(SEPARATORS, '');
}

export function isNationalIdCandidate(digits: string): boolean {
    return CANDIDATE.test(digits);
}

export function isCompleteNationalId(digits: string): boolean {
    return isNationalIdCandidate(digits) && digits.length === NATIONAL_ID_LENGTH;
}
```

`\s` covers regular and non-breaking spaces; the bidi marks mirror the set `NationalId.Of` strips on the server. Anything else (a letter) is left in place so the student sees why Continue stays disabled instead of having their input silently rewritten.

- [ ] **Step 6: Name initials**

`domain\name-initials.ts`:

```typescript
const MAX_INITIALS = 2;
const WHITESPACE = /\s+/;

export function nameInitials(name: string): string {
    const words = name
        .trim()
        .split(WHITESPACE)
        .filter(word => word.length > 0);
    const picked = words.length > MAX_INITIALS ? [words[0], words[words.length - 1]] : words;

    return picked.map(word => Array.from(word)[0].toLocaleUpperCase()).join('');
}
```

Display formatting for the mockup's avatar (`SDetails` → "AC"); `Array.from` keeps a surrogate pair intact.

- [ ] **Step 7: Week date helper**

Replace `domain\week-label.ts` with (behavior of the existing exports is unchanged — `week-label.spec.ts` keeps passing):

```typescript
const GRID_LAST_DAY_OFFSET = 5;

export interface WeekDates {
    start: Date;
    end: Date;
}

export function dateInWeek(weekStart: string, dayOffset: number): Date {
    const [year, month, day] = weekStart.split('-').map(Number);

    return new Date(year, month - 1, day + dayOffset);
}

export function weekDates(weekStart: string): WeekDates {
    const start = dateInWeek(weekStart, 0);
    const end = dateInWeek(weekStart, GRID_LAST_DAY_OFFSET);

    return { start, end };
}

export function weekRangeLabel(weekStart: string, locale: string): string {
    const { start, end } = weekDates(weekStart);
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });

    return formatter.formatRange(start, end);
}
```

`weekStart` is a calendar date (`yyyy-MM-dd`), not an instant — building a local `Date` from its parts is correct in any browser zone (same reasoning as slice 1).

- [ ] **Step 8: Slot days**

`domain\slot-day.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { dateInWeek } from './week-label';

const GRID_DAYS: readonly DayOfWeek[] = [
    DayOfWeek.sunday,
    DayOfWeek.monday,
    DayOfWeek.tuesday,
    DayOfWeek.wednesday,
    DayOfWeek.thursday,
    DayOfWeek.friday,
];

const GRID_WINDOWS: readonly SlotWindow[] = [
    SlotWindow.morning,
    SlotWindow.noon,
    SlotWindow.afternoon,
    SlotWindow.evening,
];

const TIME_LABEL_LENGTH = 5;

export interface StudentSlot {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}

export interface SlotChip {
    id: string;
    window: SlotWindow;
    timeLabel: string;
    isUnavailable: boolean;
}

export interface SlotDay {
    day: DayOfWeek;
    dateLabel: string;
    isShortDay: boolean;
    chips: SlotChip[];
}

export function groupSlotsByDay(slots: readonly StudentSlot[], weekStart: string, locale: string): SlotDay[] {
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'numeric' });

    return GRID_DAYS
        .map((day, dayOffset) => {
            const chips = chipsFor(slots, day);

            return {
                day,
                dateLabel: formatter.format(dateInWeek(weekStart, dayOffset)),
                isShortDay: chips.length < GRID_WINDOWS.length,
                chips,
            };
        })
        .filter(slotDay => slotDay.chips.length > 0);
}

function chipsFor(slots: readonly StudentSlot[], day: DayOfWeek): SlotChip[] {
    return slots
        .filter(slot => slot.day === day)
        .sort((first, second) => GRID_WINDOWS.indexOf(first.window) - GRID_WINDOWS.indexOf(second.window))
        .map(slot => ({
            id: slot.id,
            window: slot.window,
            timeLabel: `${timeLabel(slot.startLocal)}–${timeLabel(slot.endLocal)}`,
            isUnavailable: slot.state === SlotState.unavailable,
        }));
}

function timeLabel(localTime: string): string {
    return localTime.slice(0, TIME_LABEL_LENGTH);
}
```

`filter` returns a new array before `sort`, so the input is never mutated. Day names are **not** formatted here — they come from `weekGrid.days.*` translation keys in the template (client-i18n: "day names come from translation keys, not from `Date` formatting").

- [ ] **Step 9: Run the domain specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES, including the untouched `week-label.spec.ts`, `jerusalem-time.spec.ts`, `student-form-view.spec.ts`, page and routes specs.

- [ ] **Step 10: DTOs**

`data\identify-student.request.ts`:

```typescript
export interface IdentifyStudentRequest {
    nationalId: string;
}
```

`data\identify-student.response.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { Transmission } from '../domain/transmission.enum';

export interface IdentifyStudentResponse {
    studentName: string;
    teacherName: string;
    carName: string;
    transmission: Transmission;
    slots: SlotForIdentifyStudentResponse[];
}

export interface SlotForIdentifyStudentResponse {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}
```

Names match the backend classes exactly (client-architecture "DTO Interfaces").

- [ ] **Step 11: Write the failing API-service spec**

`data\submissions-api.service.spec.ts`:

```typescript
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SubmissionsApiService } from './submissions-api.service';

const NATIONAL_ID = '000000018';

describe('SubmissionsApiService', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
        });
    });

    it('posts the national ID in the body and never in the URL', () => {
        //given
        const api = TestBed.inject(SubmissionsApiService);
        const http = TestBed.inject(HttpTestingController);

        //when
        api.identifyStudent('link/token', { nationalId: NATIONAL_ID }).subscribe();

        //then
        const request = http.expectOne('api/submissions/by-link/link%2Ftoken/identify');
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ nationalId: NATIONAL_ID });
        expect(request.request.urlWithParams).not.toContain(NATIONAL_ID);
        http.verify();
    });
});
```

This pins **Review Focus 4** on the client side. (A `subscribe()` in a spec is fine — the "no subscribe" rule is about components.)

Run: `npm test -- --watch=false` → FAIL: `identifyStudent` does not exist on `SubmissionsApiService`.

- [ ] **Step 12: API method**

Replace `data\submissions-api.service.ts` with:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationByLinkResponse } from './get-publication-by-link.response';
import { IdentifyStudentRequest } from './identify-student.request';
import { IdentifyStudentResponse } from './identify-student.response';

@Injectable({ providedIn: 'root' })
export class SubmissionsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/submissions';

    getPublicationByLink(token: string): Observable<GetPublicationByLinkResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.get<GetPublicationByLinkResponse>(`${this.baseUrl}/by-link/${encodedToken}`);
    }

    identifyStudent(token: string, request: IdentifyStudentRequest): Observable<IdentifyStudentResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.post<IdentifyStudentResponse>(`${this.baseUrl}/by-link/${encodedToken}/identify`, request);
    }
}
```

- [ ] **Step 13: Test, build, commit**

Run (in `client\`): `npm test -- --watch=false` → all PASS; `npm run build` → success, no new warnings (the new domain files are not imported by any component yet; that is expected — task 4 consumes them).

```bash
git add client/src/app/features/student-form/domain client/src/app/features/student-form/data
git commit -m "feat(client): student identify DTOs, ID input mask and slot-day grouping

Separator- and bidi-tolerant national ID input, wizard step numbering,
teacher initials, and a Sunday-to-Friday slot grouping for the
student's week grid."
```

---

**Next:** [task-04-client-identify-and-details.md](task-04-client-identify-and-details.md)
