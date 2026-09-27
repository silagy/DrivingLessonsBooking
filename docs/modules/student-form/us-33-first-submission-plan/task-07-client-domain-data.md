# Task 7 of 11: Client — pick, constraint, target, pick-sheet and review models (TDD, Vitest), DTOs, API methods

> Part of [US-33…US-41: First Submission](README.md). Requires task 6 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Create: `domain\session-type.enum.ts`, `domain\submit-status.enum.ts`
- Create: `domain\slot-pick.ts` (+ `.spec.ts`), `domain\slot-constraint.ts` (+ `.spec.ts`), `domain\target-count.ts` (+ `.spec.ts`), `domain\pick-sheet.ts` (+ `.spec.ts`), `domain\review-item.ts` (+ `.spec.ts`)
- Modify: `domain\slot-day.ts` (+ `rank` on chips, `picks` parameter, `slotTimeLabel`, `hasOpenSlot`), `domain\slot-day.spec.ts`
- Create: `data\slot-request-for-submission.request.ts`, `data\create-submission.request.ts`, `data\revise-submission.request.ts`
- Modify: `data\identify-student.response.ts` (+ `hasSubmission`), `data\submissions-api.service.ts` (+ `createSubmission`, `reviseSubmission`), `data\submissions-api.service.spec.ts`
- Modify: `state\student-form.store.ts` (one call site), `ui\pages\student-form\student-form.page.spec.ts` (one fixture field)

**Interfaces:**
- Consumes (task 5/6 API): `POST`/`PUT api/submissions/by-link/{token}` → 204; identify's `hasSubmission`. (Slice 2): `StudentSlot`, `SlotChip`, `SlotDay`, `groupSlotsByDay`, `dateInWeek`, shared `DayOfWeek` / `SlotWindow` / `SlotState` enums.
- Produces (tasks 8–10 rely on these exact names):
  - `enum SessionType { single = 'single', double = 'double' }` (camelCase strings, mirrors the backend enum).
  - `enum SubmitStatus { idle, submitting, rejected, notFound, failed }`.
  - `interface SlotPick { slotId: string; sessionType: SessionType; constraint: string | null }`, `interface PickChoice { sessionType; constraint }`, `upsertPick(picks, pick): SlotPick[]`, `removePick(picks, slotId): SlotPick[]`, `rankOf(picks, slotId): number | null`.
  - `SLOT_CONSTRAINT_MAX_LENGTH = 200`, `toSlotConstraint(text): string | null`.
  - `MIN_TARGET_COUNT = 1`, `missingPickCount(targetCount, pickCount): number`.
  - `interface PickSheet { slotId; day; window; timeLabel; rank; choice: PickChoice; isEditing }`, `pickSheetFor(slot, picks): PickSheet`.
  - `interface ReviewItem { slotId; rank; day; window; timeLabel; sessionType; constraint; isPreferred }`, `reviewItemsOf(picks, slots, targetCount): ReviewItem[]`.
  - `SlotChip.rank: number | null`; `groupSlotsByDay(slots, weekStart, locale, picks)`; `slotTimeLabel(slot)`; `hasOpenSlot(slots)`.
  - `IdentifyStudentResponse.hasSubmission: boolean`; `CreateSubmissionRequest` / `ReviseSubmissionRequest` `{ nationalId; targetCount; slotRequests: SlotRequestForSubmissionRequest[] }`; `SubmissionsApiService.createSubmission(token, request): Observable<void>` (POST) and `.reviseSubmission(token, request): Observable<void>` (PUT).

Everything here is a pure function or a type — no Angular in `domain\` (client-architecture layer table). None of it decides a business rule: `upsertPick` keeps one chip = one pick (UI state), the constraint helper only trims before sending (`SlotConstraint.Of` stays the validator), and `MIN_TARGET_COUNT` / `missingPickCount` / `SLOT_CONSTRAINT_MAX_LENGTH` exist **only** for immediate feedback that duplicates the backend's rules (client-architecture "client-side validation always duplicates — never replaces — a backend rule"; README decision 10).

Precedents: slice 2's `domain\slot-day.ts` + `.spec.ts`, `domain\national-id-input.ts` + `.spec.ts`, `data\submissions-api.service.ts` + `.spec.ts`. Specs in `domain\` use plain Vitest `describe`/`it` without TestBed (slice-2 style: no section markers in these small specs).

- [ ] **Step 1: Write the failing domain specs**

`domain\slot-pick.spec.ts`:

```typescript
import { SessionType } from './session-type.enum';
import { rankOf, removePick, SlotPick, upsertPick } from './slot-pick';

function pick(slotId: string, sessionType: SessionType, constraint: string | null): SlotPick {
    return { slotId, sessionType, constraint };
}

describe('slot picks', () => {
    it('appends new picks in selection order', () => {
        const picks = [pick('sunday-noon', SessionType.single, null)];

        const next = upsertPick(picks, pick('monday-evening', SessionType.double, null));

        expect(next.map(x => x.slotId)).toEqual(['sunday-noon', 'monday-evening']);
    });

    it('changes a picked slot in place, keeping its rank', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
        ];

        const next = upsertPick(picks, pick('sunday-noon', SessionType.double, 'only after 16:00'));

        expect(next).toEqual([
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
            pick('monday-evening', SessionType.single, null),
        ]);
    });

    it('removes a pick and closes the gap in the ranking', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = removePick(picks, 'sunday-noon');

        expect(rankOf(next, 'monday-evening')).toBe(1);
        expect(rankOf(next, 'friday-morning')).toBe(2);
        expect(rankOf(next, 'sunday-noon')).toBeNull();
    });

    it('never mutates the list it was given', () => {
        const picks = [pick('sunday-noon', SessionType.single, null)];

        upsertPick(picks, pick('monday-evening', SessionType.single, null));
        removePick(picks, 'sunday-noon');

        expect(picks).toEqual([pick('sunday-noon', SessionType.single, null)]);
    });
});
```

`domain\slot-constraint.spec.ts`:

```typescript
import { SLOT_CONSTRAINT_MAX_LENGTH, toSlotConstraint } from './slot-constraint';

describe('toSlotConstraint', () => {
    it('trims what was typed', () => {
        expect(toSlotConstraint('  only after 16:00 \n')).toBe('only after 16:00');
    });

    it.each(['', '   ', '\n\t'])('sends no constraint for blank text %j', text => {
        expect(toSlotConstraint(text)).toBeNull();
    });

    it('mirrors the backend limit of 200 characters', () => {
        expect(SLOT_CONSTRAINT_MAX_LENGTH).toBe(200);
    });
});
```

`domain\target-count.spec.ts`:

```typescript
import { MIN_TARGET_COUNT, missingPickCount } from './target-count';

describe('target count', () => {
    it('starts from one lesson', () => {
        expect(MIN_TARGET_COUNT).toBe(1);
    });

    it.each([
        [3, 2, 1],
        [30, 5, 25],
        [2, 0, 2],
    ])('target %i with %i picks is %i short', (targetCount, pickCount, missing) => {
        expect(missingPickCount(targetCount, pickCount)).toBe(missing);
    });

    it.each([
        [2, 2],
        [2, 5],
    ])('target %i is covered by %i picks, extra picks are backups', (targetCount, pickCount) => {
        expect(missingPickCount(targetCount, pickCount)).toBe(0);
    });
});
```

`domain\pick-sheet.spec.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { pickSheetFor } from './pick-sheet';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

const THURSDAY_EVENING: StudentSlot = {
    id: 'thursday-evening',
    day: DayOfWeek.thursday,
    window: SlotWindow.evening,
    state: SlotState.open,
    startLocal: '18:00:00',
    endLocal: '22:00:00',
};

describe('pickSheetFor', () => {
    it('offers a new pick as the next rank, Single, with no constraint', () => {
        const picks = [
            { slotId: 'sunday-noon', sessionType: SessionType.double, constraint: 'late' },
            { slotId: 'monday-noon', sessionType: SessionType.single, constraint: null },
        ];

        expect(pickSheetFor(THURSDAY_EVENING, picks)).toEqual({
            slotId: 'thursday-evening',
            day: DayOfWeek.thursday,
            window: SlotWindow.evening,
            timeLabel: '18:00–22:00',
            rank: 3,
            choice: { sessionType: SessionType.single, constraint: null },
            isEditing: false,
        });
    });

    it('reopens a picked slot with its own rank and choices', () => {
        const picks = [
            { slotId: 'thursday-evening', sessionType: SessionType.double, constraint: 'only after 19:30' },
            { slotId: 'monday-noon', sessionType: SessionType.single, constraint: null },
        ];

        const sheet = pickSheetFor(THURSDAY_EVENING, picks);

        expect(sheet.rank).toBe(1);
        expect(sheet.choice).toEqual({ sessionType: SessionType.double, constraint: 'only after 19:30' });
        expect(sheet.isEditing).toBe(true);
    });
});
```

`domain\review-item.spec.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { reviewItemsOf } from './review-item';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

function slot(id: string, day: DayOfWeek, window: SlotWindow, startLocal: string, endLocal: string): StudentSlot {
    return { id, day, window, state: SlotState.open, startLocal, endLocal };
}

const SLOTS = [
    slot('sunday-afternoon', DayOfWeek.sunday, SlotWindow.afternoon, '15:00:00', '18:00:00'),
    slot('monday-evening', DayOfWeek.monday, SlotWindow.evening, '18:00:00', '22:00:00'),
    slot('friday-morning', DayOfWeek.friday, SlotWindow.morning, '07:00:00', '12:00:00'),
];

describe('reviewItemsOf', () => {
    it('lists picks in rank order with their slot, session type and constraint', () => {
        const picks = [
            { slotId: 'monday-evening', sessionType: SessionType.double, constraint: 'only after 19:30' },
            { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
        ];

        expect(reviewItemsOf(picks, SLOTS, 2)).toEqual([
            {
                slotId: 'monday-evening',
                rank: 1,
                day: DayOfWeek.monday,
                window: SlotWindow.evening,
                timeLabel: '18:00–22:00',
                sessionType: SessionType.double,
                constraint: 'only after 19:30',
                isPreferred: true,
            },
            {
                slotId: 'sunday-afternoon',
                rank: 2,
                day: DayOfWeek.sunday,
                window: SlotWindow.afternoon,
                timeLabel: '15:00–18:00',
                sessionType: SessionType.single,
                constraint: null,
                isPreferred: true,
            },
        ]);
    });

    it('marks the ranks past the target as backups', () => {
        const picks = SLOTS.map(x => ({ slotId: x.id, sessionType: SessionType.single, constraint: null }));

        const items = reviewItemsOf(picks, SLOTS, 1);

        expect(items.map(x => x.isPreferred)).toEqual([true, false, false]);
    });
});
```

Replace `domain\slot-day.spec.ts` with (the six slice-2 cases are unchanged except for the new fourth argument `[]` and `rank: null` in the chip they compare; two cases are new, plus `hasOpenSlot`):

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { groupSlotsByDay, hasOpenSlot, StudentSlot } from './slot-day';

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

        const days = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(days.map(x => x.day)).toEqual([DayOfWeek.sunday, DayOfWeek.tuesday, DayOfWeek.friday]);
    });

    it('orders windows morning to evening within a day', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.evening, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.afternoon, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
        ];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.chips.map(x => x.window)).toEqual([
            SlotWindow.morning,
            SlotWindow.noon,
            SlotWindow.afternoon,
            SlotWindow.evening,
        ]);
    });

    it('marks unavailable slots and labels the wall-clock window', () => {
        const slots = [slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable)];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.chips[0]).toEqual({
            id: 'sunday-morning',
            window: SlotWindow.morning,
            timeLabel: '07:00–12:00',
            isUnavailable: true,
            rank: null,
        });
    });

    it('labels each day with its date in the week', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
        ];

        const [sunday, friday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.dateLabel).toBe('10/4');
        expect(friday.dateLabel).toBe('10/9');
    });

    it('flags a day offering fewer windows than a full day', () => {
        const slots = [
            ...Object.values(SlotWindow).map(window => slot(DayOfWeek.thursday, window, SlotState.open)),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.noon, SlotState.open),
        ];

        const [thursday, friday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(thursday.isShortDay).toBe(false);
        expect(friday.isShortDay).toBe(true);
    });

    it('returns no days when the teacher has no grid this week', () => {
        expect(groupSlotsByDay([], WEEK_START, 'en', [])).toEqual([]);
    });

    it('numbers picked chips by their rank and leaves the rest unnumbered', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.afternoon, SlotState.open),
        ];
        const picks = [
            { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
            { slotId: 'sunday-morning', sessionType: SessionType.double, constraint: null },
        ];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', picks);

        expect(sunday.chips.map(x => x.rank)).toEqual([2, null, 1]);
    });
});

describe('hasOpenSlot', () => {
    it('is true when at least one slot is open', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
        ];

        expect(hasOpenSlot(slots)).toBe(true);
    });

    it('is false for a grid that is entirely unavailable, or empty', () => {
        const slots = [slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable)];

        expect(hasOpenSlot(slots)).toBe(false);
        expect(hasOpenSlot([])).toBe(false);
    });
});
```

- [ ] **Step 2: Write the failing API-service spec**

Replace `data\submissions-api.service.spec.ts` with:

```typescript
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionType } from '../domain/session-type.enum';
import { CreateSubmissionRequest } from './create-submission.request';
import { SubmissionsApiService } from './submissions-api.service';

const NATIONAL_ID = '000000018';

const SUBMISSION: CreateSubmissionRequest = {
    nationalId: NATIONAL_ID,
    targetCount: 2,
    slotRequests: [
        { slotId: 'slot-1', sessionType: SessionType.double, constraint: 'only after 16:00' },
        { slotId: 'slot-2', sessionType: SessionType.single, constraint: null },
    ],
};

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

    it.each([
        { method: 'POST', send: (api: SubmissionsApiService) => api.createSubmission('link/token', SUBMISSION) },
        { method: 'PUT', send: (api: SubmissionsApiService) => api.reviseSubmission('link/token', SUBMISSION) },
    ])('sends a submission with $method, the national ID in the body only', ({ method, send }) => {
        //given
        const api = TestBed.inject(SubmissionsApiService);
        const http = TestBed.inject(HttpTestingController);

        //when
        send(api).subscribe();

        //then
        const request = http.expectOne('api/submissions/by-link/link%2Ftoken');
        expect(request.request.method).toBe(method);
        expect(request.request.body).toEqual(SUBMISSION);
        expect(request.request.urlWithParams).not.toContain(NATIONAL_ID);
        request.flush(null, { status: 204, statusText: 'No Content' });
        http.verify();
    });
});
```

- [ ] **Step 3: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — the new domain files, `SessionType`, `CreateSubmissionRequest`, `createSubmission`/`reviseSubmission` and `hasOpenSlot` do not exist, and `groupSlotsByDay` does not take a fourth argument.

- [ ] **Step 4: Enums and small domain helpers**

`domain\session-type.enum.ts`:

```typescript
export enum SessionType {
    single = 'single',
    double = 'double',
}
```

`domain\submit-status.enum.ts`:

```typescript
export enum SubmitStatus {
    idle = 'idle',
    submitting = 'submitting',
    rejected = 'rejected',
    notFound = 'notFound',
    failed = 'failed',
}
```

`domain\slot-pick.ts`:

```typescript
import { SessionType } from './session-type.enum';

export interface PickChoice {
    sessionType: SessionType;
    constraint: string | null;
}

export interface SlotPick extends PickChoice {
    slotId: string;
}

export function upsertPick(picks: readonly SlotPick[], pick: SlotPick): SlotPick[] {
    const isPicked = picks.some(existing => existing.slotId === pick.slotId);

    return isPicked
        ? picks.map(existing => (existing.slotId === pick.slotId ? pick : existing))
        : [...picks, pick];
}

export function removePick(picks: readonly SlotPick[], slotId: string): SlotPick[] {
    return picks.filter(pick => pick.slotId !== slotId);
}

export function rankOf(picks: readonly SlotPick[], slotId: string): number | null {
    const index = picks.findIndex(pick => pick.slotId === slotId);

    return index < 0 ? null : index + 1;
}
```

Rank is the position in the list (US-37) — never stored, so removing a pick can never leave a gap.

`domain\slot-constraint.ts`:

```typescript
export const SLOT_CONSTRAINT_MAX_LENGTH = 200;

export function toSlotConstraint(text: string): string | null {
    const trimmed = text.trim();

    return trimmed ? trimmed : null;
}
```

`domain\target-count.ts`:

```typescript
export const MIN_TARGET_COUNT = 1;

export function missingPickCount(targetCount: number, pickCount: number): number {
    return Math.max(0, targetCount - pickCount);
}
```

- [ ] **Step 5: `slot-day.ts` — rank on chips, time label, open-slot check**

Replace `domain\slot-day.ts` with:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { rankOf, SlotPick } from './slot-pick';
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
    rank: number | null;
}

export interface SlotDay {
    day: DayOfWeek;
    dateLabel: string;
    isShortDay: boolean;
    chips: SlotChip[];
}

export function groupSlotsByDay(
    slots: readonly StudentSlot[],
    weekStart: string,
    locale: string,
    picks: readonly SlotPick[],
): SlotDay[] {
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'numeric' });

    return GRID_DAYS
        .map((day, dayOffset) => {
            const chips = chipsFor(slots, day, picks);

            return {
                day,
                dateLabel: formatter.format(dateInWeek(weekStart, dayOffset)),
                isShortDay: chips.length < GRID_WINDOWS.length,
                chips,
            };
        })
        .filter(slotDay => slotDay.chips.length > 0);
}

export function slotTimeLabel(slot: StudentSlot): string {
    return `${timeLabel(slot.startLocal)}–${timeLabel(slot.endLocal)}`;
}

export function hasOpenSlot(slots: readonly StudentSlot[]): boolean {
    return slots.some(slot => slot.state === SlotState.open);
}

function chipsFor(slots: readonly StudentSlot[], day: DayOfWeek, picks: readonly SlotPick[]): SlotChip[] {
    return slots
        .filter(slot => slot.day === day)
        .sort((first, second) => GRID_WINDOWS.indexOf(first.window) - GRID_WINDOWS.indexOf(second.window))
        .map(slot => ({
            id: slot.id,
            window: slot.window,
            timeLabel: slotTimeLabel(slot),
            isUnavailable: slot.state === SlotState.unavailable,
            rank: rankOf(picks, slot.id),
        }));
}

function timeLabel(localTime: string): string {
    return localTime.slice(0, TIME_LABEL_LENGTH);
}
```

`hasOpenSlot` fixes the slice-2 hidden detail (README decision 15): the store's `hasAvailability` counted *all* slots, so a teacher whose whole week is Unavailable skipped the "contact your school" notice and led to a grid with nothing pickable. Task 8 switches the store to it.

- [ ] **Step 6: `pick-sheet.ts` and `review-item.ts`**

`domain\pick-sheet.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { slotTimeLabel, StudentSlot } from './slot-day';
import { PickChoice, rankOf, SlotPick } from './slot-pick';

const NEW_PICK_CHOICE: PickChoice = { sessionType: SessionType.single, constraint: null };

export interface PickSheet {
    slotId: string;
    day: DayOfWeek;
    window: SlotWindow;
    timeLabel: string;
    rank: number;
    choice: PickChoice;
    isEditing: boolean;
}

export function pickSheetFor(slot: StudentSlot, picks: readonly SlotPick[]): PickSheet {
    const existing = picks.find(pick => pick.slotId === slot.id);
    const nextRank = picks.length + 1;

    return {
        slotId: slot.id,
        day: slot.day,
        window: slot.window,
        timeLabel: slotTimeLabel(slot),
        rank: rankOf(picks, slot.id) ?? nextRank,
        choice: existing ? { sessionType: existing.sessionType, constraint: existing.constraint } : NEW_PICK_CHOICE,
        isEditing: existing !== undefined,
    };
}
```

A new pick defaults to **Single** with no constraint (mockup `SSlotSheet`); the sheet's header shows the rank it will get ("Add as pick #4").

`domain\review-item.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { slotTimeLabel, StudentSlot } from './slot-day';
import { SlotPick } from './slot-pick';

export interface ReviewItem {
    slotId: string;
    rank: number;
    day: DayOfWeek;
    window: SlotWindow;
    timeLabel: string;
    sessionType: SessionType;
    constraint: string | null;
    isPreferred: boolean;
}

export function reviewItemsOf(
    picks: readonly SlotPick[],
    slots: readonly StudentSlot[],
    targetCount: number,
): ReviewItem[] {
    const slotsById = new Map(slots.map(slot => [slot.id, slot]));

    return picks.flatMap((pick, index) => {
        const slot = slotsById.get(pick.slotId);

        if (!slot) {
            return [];
        }

        const rank = index + 1;

        return [
            {
                slotId: pick.slotId,
                rank,
                day: slot.day,
                window: slot.window,
                timeLabel: slotTimeLabel(slot),
                sessionType: pick.sessionType,
                constraint: pick.constraint,
                isPreferred: rank <= targetCount,
            },
        ];
    });
}
```

`isPreferred` is presentation only — the mockup's review ("your top 2 are your preferred slots, the rest are backups") highlights the ranks inside the target; the teacher still interprets rank against target (requirements §5.7).

- [ ] **Step 7: DTOs and API methods**

Replace `data\identify-student.response.ts` with:

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
    hasSubmission: boolean;
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

`data\slot-request-for-submission.request.ts`:

```typescript
import { SessionType } from '../domain/session-type.enum';

export interface SlotRequestForSubmissionRequest {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
}
```

`data\create-submission.request.ts`:

```typescript
import { SlotRequestForSubmissionRequest } from './slot-request-for-submission.request';

export interface CreateSubmissionRequest {
    nationalId: string;
    targetCount: number;
    slotRequests: SlotRequestForSubmissionRequest[];
}
```

`data\revise-submission.request.ts`:

```typescript
import { SlotRequestForSubmissionRequest } from './slot-request-for-submission.request';

export interface ReviseSubmissionRequest {
    nationalId: string;
    targetCount: number;
    slotRequests: SlotRequestForSubmissionRequest[];
}
```

Interfaces are named identically to the backend DTOs (client-architecture "never name a client DTO differently from its backend counterpart"), so both halves are found by name.

Replace `data\submissions-api.service.ts` with:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateSubmissionRequest } from './create-submission.request';
import { GetPublicationByLinkResponse } from './get-publication-by-link.response';
import { IdentifyStudentRequest } from './identify-student.request';
import { IdentifyStudentResponse } from './identify-student.response';
import { ReviseSubmissionRequest } from './revise-submission.request';

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

    createSubmission(token: string, request: CreateSubmissionRequest): Observable<void> {
        const encodedToken = encodeURIComponent(token);

        return this.http.post<void>(`${this.baseUrl}/by-link/${encodedToken}`, request);
    }

    reviseSubmission(token: string, request: ReviseSubmissionRequest): Observable<void> {
        const encodedToken = encodeURIComponent(token);

        return this.http.put<void>(`${this.baseUrl}/by-link/${encodedToken}`, request);
    }
}
```

- [ ] **Step 8: Keep the store and page spec compiling**

In `state\student-form.store.ts`, the `slotDays` computed passes the new fourth argument — replace its `groupSlotsByDay(…)` call with:

```typescript
            ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang(), [])
```

(Task 9 swaps `[]` for the store's picks, the same line.)

In `ui\pages\student-form\student-form.page.spec.ts`, the `studentOf` fixture must satisfy the widened response — replace its `return` line with:

```typescript
    return {
        studentName: 'Test Student',
        teacherName,
        carName: 'Corolla White',
        transmission,
        hasSubmission: false,
        slots,
    };
```

- [ ] **Step 9: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — the five new domain specs, the two new `slot-day` cases + `hasOpenSlot`, and the two new API cases; the page spec is unchanged in behavior.

Then `npm run build` → success, no new warnings.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/student-form
git commit -m "feat(client): submission models and API methods for the student form

Ranked slot picks, the pick sheet and review list models, constraint
trimming and the target-count feedback helpers, plus create/revise
submission calls that keep the national ID in the body."
```

---

**Next:** [task-08-client-target-step.md](task-08-client-target-step.md)
