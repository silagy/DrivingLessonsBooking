# Task 2 of 3: The review step nudges for more slots when the list leaves little room

> Part of [More Slots, Better Chance](README.md). Read its Decisions and Global Constraints first. Requires task 1 committed. Branch `more-slots-better-chance`.

**Files:**
- Modify: `client\src\app\features\student-form\domain\target-count.ts`
- Test: `client\src\app\features\student-form\domain\target-count.spec.ts`
- Modify: `client\src\app\features\student-form\state\student-form.store.ts`
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` / `.html` / `.scss`
- Modify: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (`studentForm.review`)
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`
- Docs: `docs\requirements.md` §7 step 8 and §11

**Interfaces:**
- Consumes: task 1's `studentForm.review.addSlots` value; `ReviewStepComponent.addSlots` output; the store's private `target`, `pickCount` and `openSlotIds`; spec helpers `reachTarget`, `reachReview`, `clickContinue`, `continueButton`, `press`, `rankOn`, `textOf`, `page`, `studentOf`, `weekSlots`, fixtures `COHEN_STUDENT` (22 Open slots) and `RETURNING_STUDENT` (target 2, three picks).
- Produces: `ROOM_TO_SCHEDULE_MARGIN = 2` and `leavesLittleRoom(targetCount: number, pickCount: number, openSlotCount: number): boolean` in `target-count.ts`; `StudentFormStore.suggestsMoreSlots` (readonly computed `boolean`); `ReviewStepComponent.suggestsMoreSlots` (`input.required<boolean>()`); keys `studentForm.review.moreRoomTitle`, `studentForm.review.moreRoomBody`; classes `.review__more-room`, `.review__more-room-add`.

- [ ] **Step 1: Write the failing domain spec**

In `target-count.spec.ts`:

```typescript
it.each([
    [1, 1, 22],
    [1, 2, 22],
    [2, 2, 22],
    [2, 3, 22],
])('target %i with %i of %i open slots picked leaves little room', (targetCount, pickCount, openSlotCount) => {
    expect(leavesLittleRoom(targetCount, pickCount, openSlotCount)).toBe(true);
});

it.each([
    [1, 3, 22],
    [2, 4, 22],
    [2, 1, 22],
    [1, 2, 2],
    [2, 3, 3],
])('target %i with %i of %i open slots picked leaves enough room or is short', (targetCount, pickCount, openSlotCount) => {
    expect(leavesLittleRoom(targetCount, pickCount, openSlotCount)).toBe(false);
});

it('asks for two slots beyond the target', () => {
    expect(ROOM_TO_SCHEDULE_MARGIN).toBe(2);
});
```

Also rename the existing `'target %i is covered by %i picks, extra picks are backups'` to `'target %i is covered by %i picks'`.

- [ ] **Step 2: Run it to verify it fails**

Run (from `client\`): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/student-form/domain/target-count.spec.ts`
Expected: FAIL, `leavesLittleRoom` and `ROOM_TO_SCHEDULE_MARGIN` are not exported.

- [ ] **Step 3: Implement `leavesLittleRoom` in `target-count.ts`**

True when the target is met (`pickCount >= targetCount`), `pickCount < targetCount + ROOM_TO_SCHEDULE_MARGIN`, and `pickCount < openSlotCount` (picks are always Open slots, so this means an unpicked Open slot exists).

- [ ] **Step 4: Run it to verify it passes**

Run Step 2's command. Expected: PASS.

- [ ] **Step 5: Write the failing page specs**

In `student-form.page.spec.ts`, next to `COHEN_STUDENT`, add a fixture with only two Open slots:

```typescript
const TWO_OPEN_SLOTS = ['sunday-afternoon', 'monday-evening'];
const TWO_SLOT_STUDENT = studentOf(
    'Teacher Cohen',
    Transmission.automatic,
    weekSlots([]).map(slot => (TWO_OPEN_SLOTS.includes(slot.id) ? slot : { ...slot, state: SlotState.unavailable })),
);
```

Add `describe('room to schedule', ...)` after `describe('review and submit', ...)`. Each spec is `//given` provide + render, `//when` the reach, `//then` the asserts:

| Spec | Student, reach | Then |
|------|----------------|------|
| `suggests more slots when one slot covers a target of one` | `COHEN_STUDENT`, `reachReview(fixture, 1, ['sunday-afternoon'])` | `.review__more-room` text matches `/studentForm\.review\.moreRoomTitle/` and `/studentForm\.review\.moreRoomBody/`; `continueButton(fixture)!.disabled` is `false` |
| `keeps suggesting until the list holds two slots beyond the target` | `COHEN_STUDENT`, `reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon'])` | `.review__more-room` not null |
| `stops suggesting once the list holds two slots beyond the target` | `COHEN_STUDENT`, `reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon', 'thursday-evening'])` | `.review__more-room` null |
| `never suggests more slots when every open slot is picked` | `TWO_SLOT_STUDENT`, `reachReview(fixture, 1, TWO_OPEN_SLOTS)` | `.review__more-room` null |
| `never shows the suggestion together with the not-enough message` | `COHEN_STUDENT`, `reachReview(fixture, 3, ['sunday-afternoon'])` | `.review__not-enough` not null; `.review__more-room` null |
| `suggests more slots to a returning student whose saved list leaves little room` | `RETURNING_STUDENT`, `reachTarget(fixture)`, then `clickContinue` twice | `app-review-step` not null; `.review__more-room` not null |
| `sends the student back to the grid from the suggestion, keeping the picks` | `COHEN_STUDENT`, `reachReview(fixture, 1, ['sunday-afternoon'])`, then `press(fixture, '.review__more-room-add button')` | `app-slots-step` not null; `rankOn(fixture, 'sunday-afternoon')` is `'1'` |

- [ ] **Step 6: Run them to verify they fail**

Run `... ng.js test --watch=false --include src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts`
Expected: the specs that expect `.review__more-room` or `.review__more-room-add` FAIL; the three that expect it null pass already.

- [ ] **Step 7: Implement the nudge**

- Store: `readonly suggestsMoreSlots = computed(() => leavesLittleRoom(this.target(), this.pickCount(), this.openSlotIds().size));`, next to `missingPicks`.
- Review step: `readonly suggestsMoreSlots = input.required<boolean>();` after `missingPicks`.
- Page HTML: `[suggestsMoreSlots]="store.suggestsMoreSlots()"` after `[missingPicks]`.
- Review step HTML, inside the `@else` branch after `.review__summary`:

```html
@if (suggestsMoreSlots()) {
    <p-message severity="info" class="review__more-room">
        <span>
            <strong>{{ 'studentForm.review.moreRoomTitle' | transloco }}</strong>
            {{ 'studentForm.review.moreRoomBody' | transloco }}
        </span>
    </p-message>
    <p-button
        type="button"
        class="review__more-room-add"
        severity="secondary"
        fluid
        [outlined]="true"
        [label]="'studentForm.review.addSlots' | transloco"
        (onClick)="addSlots.emit()" />
}
```

- Review step SCSS: give `.review__more-room` and `.review__more-room-add` the same block spacing the not-enough message and `.review__fixes` use (reuse their declarations; no new values).
- Both JSON files, under `studentForm.review` after `preferred`:

| Key | he | en |
|-----|----|----|
| `moreRoomTitle` | רוצים להגדיל את הסיכוי? | Want a better chance? |
| `moreRoomBody` | הרשימה שלכם משאירה לנו מעט גמישות בשיבוץ. ככל שתיתנו יותר שיעורים אפשריים, אתם מגדילים את הסיכוי לקבל את מספר השיעורים שביקשתם. | Your list leaves us little room to schedule you. The more slots you give, the better your chance of getting all the lessons you asked for. |

- [ ] **Step 8: Run the specs**

Run Step 6's command, then `--include src/app/core/translations.spec.ts`, then `--include src/app/features/student-form`. Expected: PASS.

- [ ] **Step 9: The docs**

- `docs\requirements.md` §7 step 8: append "The form encourages picking more slots than the target, because more slots give the school more room to schedule: the target step says the two numbers don't have to match, and the review step suggests adding slots while the list holds fewer than the target plus two and Open slots remain. The minimum stays the target."
- `docs\requirements.md` §11: a new row (26, or the next free number): "**More slots, better chance**: the student form says the number of slots doesn't have to match the number of lessons, calls a slot שיעור אפשרי in Hebrew, and suggests adding slots while a list holds fewer than the target plus two" | rationale "Customer feedback (October 2026): students sent one slot for one lesson, or two for two, which left no room to schedule them. The minimum was deliberately not raised: it would block students who truly have little time, instead of persuading the rest".
- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/student-form client/public/i18n/he.json client/public/i18n/en.json \
  docs/requirements.md
git commit -m "feat(student-form): suggest more slots on review while the list leaves the school little room to schedule"
```

---

**Next:** [task-03-verification-and-pr.md](task-03-verification-and-pr.md)
