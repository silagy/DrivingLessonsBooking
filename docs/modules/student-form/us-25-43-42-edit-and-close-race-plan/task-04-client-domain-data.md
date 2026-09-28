# Task 4 of 7: Client domain & data: the saved submission, loading it, the window-closed problem, the `windowClosed` step

> Part of [US-25 / 43 / 42: Edit & Close Race](README.md). Requires tasks 1–3 committed. Work on branch `26-us-25-43-42-edit-and-close-race`. Client commands run from `client\`.

**Files:**
- Modify: `client\src\app\features\student-form\data\identify-student.response.ts` (`hasSubmission` → `submission`)
- Create: `client\src\app\features\student-form\data\problem-types.ts`
- Test: `client\src\app\features\student-form\data\problem-types.spec.ts`
- Create: `client\src\app\features\student-form\domain\loaded-submission.ts`
- Test: `client\src\app\features\student-form\domain\loaded-submission.spec.ts`
- Modify: `client\src\app\features\student-form\domain\student-form-step.enum.ts` (+ `windowClosed`)
- Test: `client\src\app\features\student-form\domain\student-form-step.spec.ts`
- Modify: `client\src\app\features\student-form\state\student-form.store.ts` (compile-level only)
- Modify: `client\src\app\features\student-form\state\student-form.store.spec.ts` (fixture)
- Modify: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts` (fixtures)

**Interfaces:**
- Consumes (task 2 / task 3 JSON): identify's `submission: { targetCount, lastSavedAtUtc, slotRequests: [{ slotId, sessionType, constraint }] } | null`; a 409 body with `type: "problems/submission-window-closed"`. Existing: `SlotPick` (`slot-pick.ts`), `StudentSlot` (`slot-day.ts`), `MIN_TARGET_COUNT` (`target-count.ts`), `SessionType`, `SlotState`.
- Produces:
  - DTOs `SubmissionForIdentifyStudentResponse`, `SlotRequestForIdentifyStudentResponse`, and `IdentifyStudentResponse.submission` (replaces `hasSubmission`).
  - `SUBMISSION_WINDOW_CLOSED_PROBLEM` and `isWindowClosedProblem(error: unknown): boolean` in `data\problem-types.ts`: true only for an `HttpErrorResponse` with status 409 whose body `type` is the constant. Task 6 uses it.
  - Domain shapes `SavedSubmission`, `SavedSlotRequest`, `LoadedSubmission { targetCount, picks: SlotPick[], droppedPickCount }` and `loadedSubmissionOf(saved: SavedSubmission | null, slots: readonly StudentSlot[]): LoadedSubmission`. It returns target 1 and no picks for `null`; otherwise the saved target and the saved picks in rank order, minus any whose slot is not Open in `slots`, counted in `droppedPickCount` (README decision 2). Task 5 seeds the store from it.
  - `StudentFormStep.windowClosed` (unnumbered, no way back, no caption). Task 6 renders it.
  - Test fixture `SAVED_SUBMISSION` in the page spec (target 2; `monday-noon` Double "only after 16:00", `sunday-afternoon` Single, `wednesday-evening` Single). Tasks 5 and 6 use it.

This task changes no behaviour. The store only swaps `hasSubmission` for `submission !== null` so the build stays green, and the page spec keeps passing as it is.

- [ ] **Step 1: The DTO carries the saved submission**

Replace `client\src\app\features\student-form\data\identify-student.response.ts` with:

```ts
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from '../domain/session-type.enum';
import { Transmission } from '../domain/transmission.enum';

export interface IdentifyStudentResponse {
    studentName: string;
    teacherName: string;
    carName: string;
    transmission: Transmission;
    submission: SubmissionForIdentifyStudentResponse | null;
    slots: SlotForIdentifyStudentResponse[];
}

export interface SubmissionForIdentifyStudentResponse {
    targetCount: number;
    lastSavedAtUtc: string;
    slotRequests: SlotRequestForIdentifyStudentResponse[];
}

export interface SlotRequestForIdentifyStudentResponse {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
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

- [ ] **Step 2: Failing specs for `isWindowClosedProblem` and `loadedSubmissionOf`**

Create `client\src\app\features\student-form\data\problem-types.spec.ts`:

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { isWindowClosedProblem, SUBMISSION_WINDOW_CLOSED_PROBLEM } from './problem-types';

const HTTP_CONFLICT = 409;
const HTTP_NOT_FOUND = 404;

describe('isWindowClosedProblem', () => {
    it('recognises the window-closed conflict', () => {
        const error = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { type: SUBMISSION_WINDOW_CLOSED_PROBLEM, title: 'Conflict', status: HTTP_CONFLICT },
        });

        expect(isWindowClosedProblem(error)).toBe(true);
    });

    it.each([
        {
            case: 'another conflict',
            error: new HttpErrorResponse({ status: HTTP_CONFLICT, error: { title: 'Conflict' } }),
        },
        { case: 'a conflict without a body', error: new HttpErrorResponse({ status: HTTP_CONFLICT }) },
        {
            case: 'the type on another status',
            error: new HttpErrorResponse({ status: HTTP_NOT_FOUND, error: { type: SUBMISSION_WINDOW_CLOSED_PROBLEM } }),
        },
        { case: 'a network failure', error: new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }) },
        { case: 'an error that is not HTTP', error: new Error('boom') },
    ])('ignores $case', ({ error }) => {
        expect(isWindowClosedProblem(error)).toBe(false);
    });
});
```

Create `client\src\app\features\student-form\domain\loaded-submission.spec.ts`:

```ts
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { loadedSubmissionOf, SavedSubmission } from './loaded-submission';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

function slotOf(day: DayOfWeek, window: SlotWindow, state: SlotState): StudentSlot {
    return { id: `${day}-${window}`, day, window, state, startLocal: '12:00:00', endLocal: '15:00:00' };
}

const GRID: readonly StudentSlot[] = [
    slotOf(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
    slotOf(DayOfWeek.monday, SlotWindow.noon, SlotState.open),
    slotOf(DayOfWeek.tuesday, SlotWindow.noon, SlotState.unavailable),
    slotOf(DayOfWeek.wednesday, SlotWindow.noon, SlotState.open),
];

function savedWith(targetCount: number, slotIds: readonly string[]): SavedSubmission {
    return {
        targetCount,
        lastSavedAtUtc: '2026-11-12T08:30:00Z',
        slotRequests: slotIds.map(slotId => ({ slotId, sessionType: SessionType.single, constraint: null })),
    };
}

describe('loadedSubmissionOf', () => {
    it('starts a student without a submission at one lesson with no picks', () => {
        expect(loadedSubmissionOf(null, GRID)).toEqual({ targetCount: 1, picks: [], droppedPickCount: 0 });
    });

    it('loads the saved target and picks in rank order with their session type and constraint', () => {
        const saved: SavedSubmission = {
            targetCount: 3,
            lastSavedAtUtc: '2026-11-12T08:30:00Z',
            slotRequests: [
                { slotId: 'wednesday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                { slotId: 'sunday-noon', sessionType: SessionType.single, constraint: null },
            ],
        };

        expect(loadedSubmissionOf(saved, GRID)).toEqual({
            targetCount: 3,
            picks: [
                { slotId: 'wednesday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                { slotId: 'sunday-noon', sessionType: SessionType.single, constraint: null },
            ],
            droppedPickCount: 0,
        });
    });

    it('drops a saved pick whose slot is now unavailable, keeping the others in order', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['monday-noon', 'tuesday-noon', 'sunday-noon']), GRID);

        expect(loaded.picks.map(pick => pick.slotId)).toEqual(['monday-noon', 'sunday-noon']);
        expect(loaded.droppedPickCount).toBe(1);
    });

    it('drops saved picks that are not in the current grid at all, such as a previous teacher\'s', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['other-teacher-slot', 'sunday-noon', 'another-slot']), GRID);

        expect(loaded.picks.map(pick => pick.slotId)).toEqual(['sunday-noon']);
        expect(loaded.droppedPickCount).toBe(2);
    });

    it('keeps the saved target even when fewer picks survive', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['tuesday-noon', 'sunday-noon']), GRID);

        expect(loaded.targetCount).toBe(2);
        expect(loaded.picks.length).toBe(1);
    });

    it('never shares the picks list between loads', () => {
        const first = loadedSubmissionOf(null, GRID);
        const second = loadedSubmissionOf(null, GRID);

        expect(first.picks).not.toBe(second.picks);
    });
});
```

In `client\src\app\features\student-form\domain\student-form-step.spec.ts`, replace

```ts
    it.each([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.done])(
```

with

```ts
    it.each([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.done, StudentFormStep.windowClosed])(
```

and add inside the `describe`, after that `it.each`:

```ts
    it('keeps the window-closed screen out of the numbered steps', () => {
        expect(WIZARD_STEPS).not.toContain(StudentFormStep.windowClosed);
    });
```

- [ ] **Step 3: Run the specs to see them fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. The two new spec files cannot resolve `./problem-types` / `./loaded-submission`, and `student-form-step.spec.ts` fails to compile on `StudentFormStep.windowClosed`. The store and both existing specs also fail to type-check on `hasSubmission` (fixed in Step 5).

- [ ] **Step 4: Implement**

Create `client\src\app\features\student-form\data\problem-types.ts`:

```ts
import { HttpErrorResponse } from '@angular/common/http';

const HTTP_CONFLICT = 409;

export const SUBMISSION_WINDOW_CLOSED_PROBLEM = 'problems/submission-window-closed';

export function isWindowClosedProblem(error: unknown): boolean {
    return (
        error instanceof HttpErrorResponse &&
        error.status === HTTP_CONFLICT &&
        error.error?.type === SUBMISSION_WINDOW_CLOSED_PROBLEM
    );
}
```

Create `client\src\app\features\student-form\domain\loaded-submission.ts`:

```ts
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';
import { SlotPick } from './slot-pick';
import { MIN_TARGET_COUNT } from './target-count';

export interface SavedSlotRequest {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
}

export interface SavedSubmission {
    targetCount: number;
    lastSavedAtUtc: string;
    slotRequests: readonly SavedSlotRequest[];
}

export interface LoadedSubmission {
    targetCount: number;
    picks: SlotPick[];
    droppedPickCount: number;
}

export function loadedSubmissionOf(saved: SavedSubmission | null, slots: readonly StudentSlot[]): LoadedSubmission {
    if (!saved) {
        return { targetCount: MIN_TARGET_COUNT, picks: [], droppedPickCount: 0 };
    }

    const openSlotIds = new Set(slots.filter(slot => slot.state === SlotState.open).map(slot => slot.id));
    const picks = saved.slotRequests
        .filter(request => openSlotIds.has(request.slotId))
        .map(({ slotId, sessionType, constraint }) => ({ slotId, sessionType, constraint }));

    return {
        targetCount: saved.targetCount,
        picks,
        droppedPickCount: saved.slotRequests.length - picks.length,
    };
}
```

`SubmissionForIdentifyStudentResponse` satisfies `SavedSubmission` structurally, so the store passes the DTO straight in and `domain\` never imports from `data\`.

Replace `client\src\app\features\student-form\domain\student-form-step.enum.ts` with:

```ts
export enum StudentFormStep {
    identify = 'identify',
    details = 'details',
    target = 'target',
    slots = 'slots',
    review = 'review',
    done = 'done',
    windowClosed = 'windowClosed',
}
```

`student-form-step.ts` needs no change: `windowClosed` is neither in `WIZARD_STEPS` nor in `PREVIOUS_STEP`, so it is unnumbered and has no Back.

- [ ] **Step 5: Keep the store and the fixtures compiling**

In `client\src\app\features\student-form\state\student-form.store.ts`:

1. In `CAPTION_KEY_BY_STEP`, replace
   ```ts
    [StudentFormStep.done]: null,
   };
   ```
   with
   ```ts
    [StudentFormStep.done]: null,
    [StudentFormStep.windowClosed]: null,
   };
   ```
2. Replace
   ```ts
    readonly replacesEarlierSubmission = computed(
        () => (this.student()?.hasSubmission ?? false) || this.submittedThisVisit(),
    );
   ```
   with
   ```ts
    readonly replacesEarlierSubmission = computed(
        () => Boolean(this.student()?.submission) || this.submittedThisVisit(),
    );
   ```

In `client\src\app\features\student-form\state\student-form.store.spec.ts`, replace `hasSubmission: false,` with `submission: null,`.

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Add `SubmissionForIdentifyStudentResponse` to the identify-response import:
   ```ts
   import {
       IdentifyStudentResponse,
       SlotForIdentifyStudentResponse,
       SubmissionForIdentifyStudentResponse,
   } from '../../../data/identify-student.response';
   ```
2. In `studentOf`, replace `hasSubmission: false,` with `submission: null,`.
3. Directly after `const COHEN_STUDENT = …;` add:
   ```ts
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
   ```
4. In `replaces the earlier submission of a returning student`, replace
   `const returning = { ...COHEN_STUDENT, hasSubmission: true };` with `const returning = RETURNING_STUDENT;`.
5. In `checks the form again in an open window and replaces a list sent from another tab`, replace
   `roster.student = { ...COHEN_STUDENT, hasSubmission: true };` with `roster.student = RETURNING_STUDENT;`.

(The `SessionType` import already exists in the page spec.)

- [ ] **Step 6: Run everything**

Run (in `client\`): `npm test -- --watch=false` then `npm run build`
Expected: every spec PASSES (the new `problem-types` and `loaded-submission` specs, the extra step rows, every existing page and store spec unchanged in behaviour), and the build is clean with no new warnings.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/features/student-form/data/identify-student.response.ts client/src/app/features/student-form/data/problem-types.ts client/src/app/features/student-form/data/problem-types.spec.ts client/src/app/features/student-form/domain/loaded-submission.ts client/src/app/features/student-form/domain/loaded-submission.spec.ts client/src/app/features/student-form/domain/student-form-step.enum.ts client/src/app/features/student-form/domain/student-form-step.spec.ts client/src/app/features/student-form/state/student-form.store.ts client/src/app/features/student-form/state/student-form.store.spec.ts client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts
git commit -m "feat(client): saved-submission model and window-closed problem check

The identify DTO now carries the saved submission. Loading it keeps the
saved target and ranked picks but drops picks whose slot is no longer
open, counting them. A window-closed conflict is recognised by its
problem type, and the wizard gains an unnumbered window-closed step."
```

---

**Next:** [task-05-client-returning-student.md](task-05-client-returning-student.md)
