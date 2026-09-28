# Task 9 of 11: Client — pickable chips, rank badges, the pick bottom sheet, Target/Picked footer (US-34, US-35, US-36, US-37, US-39)

> Part of [US-33…US-41: First Submission](README.md). Requires task 8 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Modify: `ui\components\slot-day-list\slot-day-list.component.ts`, `.html`, `.scss` (chips become buttons)
- Create: `ui\components\pick-sheet\pick-sheet.component.ts`, `.html`, `.scss`
- Modify: `ui\components\slots-step\slots-step.component.ts`, `.html`; Create: `.scss`
- Modify: `state\student-form.store.ts`
- Modify: `ui\pages\student-form\student-form.page.html`, `.spec.ts`
- Modify: `client\src\styles\_tokens.scss` (+ `--app-scrim`)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 7): `SlotPick`, `PickChoice`, `upsertPick`, `removePick`, `PickSheet`, `pickSheetFor`, `SLOT_CONSTRAINT_MAX_LENGTH`, `toSlotConstraint`, `SessionType`, `SlotChip.rank`; (task 8): `WizardStepComponent` `canGoBack`/`back`, `StudentFormStore.targetCount`, `goBack()`, page-spec helpers `reachTarget`, `reachSlots`, `press`, `textOf`.
- Produces (task 10 relies on these exact names):
  - `SlotDayListComponent` output `chipSelected: string` (slot id). Chips are `<button class="slot-chip" data-slot-id="…">`; picked chips carry `.slot-chip--picked` and a `.slot-chip__rank` badge; Unavailable chips are `disabled`.
  - `PickSheetComponent` (`app-pick-sheet`): input `sheet: PickSheet`; outputs `saved: PickChoice`, `removed`, `cancelled`. Classes `.pick-sheet`, `.pick-sheet__rank`, `.pick-sheet__time`, `.pick-sheet__single`, `.pick-sheet__double`, `#pick-constraint`, `.pick-sheet__save`, `.pick-sheet__cancel`, `.pick-sheet__remove`.
  - `SlotsStepComponent`: inputs `stepNumber`, `stepCount`, `days`, `targetCount`, `pickCount`, `pickSheet: PickSheet | null` (the `teacherName` input is dropped — the caption bar names the teacher); outputs `chipSelected`, `pickSaved`, `pickRemoved`, `pickCancelled`, `reviewed`, `back`.
  - `StudentFormStore`: `pickCount`, `pickSheet`, `slotDays` (now ranked), `openPick(slotId)`, `savePick(choice)`, `removeOpenPick()`, `closePick()`; private `picks` (task 10 reads it for the review and the request body).
  - Translation keys `studentForm.slotLabel`, `studentForm.slots.{summary, backups, review, pickedRank}` (+ new `title`/`body` text), `studentForm.pick.{sessionType, single, double, doubleHint, constraintLabel, constraintHint, cancel, add, save, remove}`.

Precedents: slice 2's `slot-day-list` and `identify-step` (the `p-message` leave-animation lesson), mockup `student.jsx` → `SlotChip` (rank badge at the top inline-end corner, picked = sky border + tint), `SlotDayList`, `SSlots` (footer "Target: 2 · Picked: 3 ✓ … extra picks = backups" + "REVIEW MY LIST"), `SSlotSheet` (grip, rank + "Thursday · Evening" + time, Single / Double (2×), optional constraint with hint, Cancel + "ADD AS PICK #4").

**Interaction model** (README decisions 12, 13; roadmap decisions 5, 7):
- Tap an **Open** chip that is not picked → the sheet opens as a new pick: rank = next, **Single**, empty constraint. "Add as pick #n" appends it (US-37: selection order is rank).
- Tap a **picked** chip → the sheet opens with its own rank and choices; **Save** changes it in place (rank kept), **Remove this pick** drops it and every later pick moves up one. This is the mockup's "Tap a picked slot to edit or remove it". Reordering is slice 4.
- **Unavailable** chips are `disabled` buttons (not focusable, not tappable — US-34), striped, with a visually hidden "Unavailable".
- A slot can only ever be one pick: tapping it again edits, never adds a second request (**Review Focus 1**, client half — the backend rule is task 2's).
- The sheet is a **custom dumb component rendered with `@if`**, not a PrimeNG `Drawer` or `DialogService` dialog (README decision 12, "conventions that override the rules"): its open state is wizard state held in the store (roadmap decision 4), its choices are `linkedSignal`s from the `sheet` input (client-state "resettable local choices"), and because it is destroyed the moment the store clears `openSlotId`, no leave animation can keep a previous slot's day, time or constraint on screen (the slice-2 `p-message` `animate.leave` lesson; **Review Focus 10**). PrimeNG supplies `pFocusTrap`, `pTextarea` and `p-button`; Escape and the backdrop cancel.
- The textarea carries `maxlength="200"` (`SLOT_CONSTRAINT_MAX_LENGTH`) — immediate feedback for the backend's 200-character rule (**Review Focus 7**); the value is trimmed and blank becomes `null` on save (`toSlotConstraint`).

- [x] **Step 1: Write the failing page-spec cases**

In `ui\pages\student-form\student-form.page.spec.ts`:

1. Add these helpers directly after `textOf`:

```typescript

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
```

2. In `describe('slots step', …)`, **delete** the case `keeps the week grid read-only until picking arrives`.

3. Add this `describe` block after the `slots step` block:

```typescript
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
```

What these pin: US-37 (selection order = rank), US-34 (disabled Unavailable chip), US-35 / US-36 (Double and constraint stay on their own pick), US-39's client half (the footer's target/picked counts; a pick count above the target is still "covered"), **Review Focus 1** (a picked slot re-opens for editing — it can never become two requests), **Review Focus 7** (`maxlength`), **Review Focus 10** (a fresh sheet per open: Sunday's typed constraint never appears on Monday's sheet). The target defaults to 1, so one pick already covers it (`.slots__covered`).

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — chips are list items without `data-slot-id`, there is no `app-pick-sheet`, and the slots footer is empty.

- [x] **Step 2: Translations and the scrim token (en + he, same commit)**

In **both** i18n files, inside `"studentForm"`: add `"slotLabel"` directly after `"back"`; **replace** the `"slots"` object; add a `"pick"` object directly after `"slots"`.

`client\public\i18n\en.json`:

```json
    "slotLabel": "{{day}} · {{window}}",
```

```json
    "slots": {
      "title": "Pick your slots, best first",
      "body": "Tap a slot to add it — the order is your preference. Tap a picked slot to change or remove it. Striped slots are unavailable.",
      "shortDay": "morning & noon only",
      "unavailable": "Unavailable",
      "pickedRank": "pick {{rank}}",
      "summary": "Target: {{target}} · Picked: {{picked}}",
      "backups": "extra picks = backups",
      "review": "Review my list"
    },
    "pick": {
      "sessionType": "Session type",
      "single": "Single",
      "double": "Double",
      "doubleHint": "(2×)",
      "constraintLabel": "Constraint for this slot (optional)",
      "constraintHint": "Free text — e.g. \"only after 16:00\", \"pick me up from work\"",
      "cancel": "Cancel",
      "add": "Add as pick #{{rank}}",
      "save": "Save",
      "remove": "Remove this pick"
    },
```

`client\public\i18n\he.json`:

```json
    "slotLabel": "{{day}} · {{window}}",
```

```json
    "slots": {
      "title": "בחרו שעות, המועדפת קודם",
      "body": "הקישו על שעה כדי להוסיף אותה — הסדר הוא סדר ההעדפה שלכם. הקישו על שעה שבחרתם כדי לשנות או להסיר אותה. שעות מקווקוות אינן זמינות.",
      "shortDay": "בוקר וצהריים בלבד",
      "unavailable": "לא זמין",
      "pickedRank": "בחירה {{rank}}",
      "summary": "יעד: {{target}} · נבחרו: {{picked}}",
      "backups": "בחירות נוספות = גיבוי",
      "review": "לסקירת הרשימה"
    },
    "pick": {
      "sessionType": "סוג שיעור",
      "single": "יחיד",
      "double": "כפול",
      "doubleHint": "(2×)",
      "constraintLabel": "אילוץ לשעה הזו (לא חובה)",
      "constraintHint": "טקסט חופשי — למשל \"רק אחרי 16:00\", \"לאסוף אותי מהעבודה\"",
      "cancel": "ביטול",
      "add": "הוספה כבחירה מס׳ {{rank}}",
      "save": "שמירה",
      "remove": "הסרת הבחירה"
    },
```

The slots title no longer names the teacher: from the target step on the caption bar reads "week · teacher" (task 8), matching mockup `SSlots`. Verify parity with the task-8 `node -e …` command → `[] []`.

In `client\src\styles\_tokens.scss`, add after `--app-shadow-lift: …;`:

```scss
  --app-scrim: rgba(18, 20, 24, 0.4);
```

(The mockup's overlay tint; tokens live in this one file — client-primeng "no hardcoded colors" in components.)

- [x] **Step 3: `SlotDayListComponent` — chips become buttons**

Replace `ui\components\slot-day-list\slot-day-list.component.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotDay } from '../../../domain/slot-day';

@Component({
    selector: 'app-slot-day-list',
    imports: [TranslocoPipe],
    templateUrl: './slot-day-list.component.html',
    styleUrl: './slot-day-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotDayListComponent {
    readonly days = input.required<readonly SlotDay[]>();

    readonly chipSelected = output<string>();
}
```

Replace `ui\components\slot-day-list\slot-day-list.component.html` with:

```html
<ol class="slot-days">
    @for (slotDay of days(); track slotDay.day) {
        <li class="slot-day">
            <div class="slot-day__header">
                <span class="slot-day__name">{{ 'weekGrid.days.' + slotDay.day | transloco }}</span>
                <span class="slot-day__date">{{ slotDay.dateLabel }}</span>
                @if (slotDay.isShortDay) {
                    <span class="slot-day__note">{{ 'studentForm.slots.shortDay' | transloco }}</span>
                }
            </div>
            <ul class="slot-day__chips">
                @for (chip of slotDay.chips; track chip.id) {
                    <li class="slot-day__cell">
                        <button
                            type="button"
                            class="slot-chip"
                            [class.slot-chip--unavailable]="chip.isUnavailable"
                            [class.slot-chip--picked]="chip.rank !== null"
                            [attr.data-slot-id]="chip.id"
                            [disabled]="chip.isUnavailable"
                            (click)="chipSelected.emit(chip.id)">
                            <span class="slot-chip__window">{{ 'weekGrid.windows.' + chip.window | transloco }}</span>
                            <span class="slot-chip__time" dir="ltr">{{ chip.timeLabel }}</span>
                            @if (chip.isUnavailable) {
                                <span class="p-hidden-accessible">{{ 'studentForm.slots.unavailable' | transloco }}</span>
                            }
                            @if (chip.rank !== null) {
                                <span class="slot-chip__rank" aria-hidden="true">{{ chip.rank }}</span>
                                <span class="p-hidden-accessible">
                                    {{ 'studentForm.slots.pickedRank' | transloco: { rank: chip.rank } }}
                                </span>
                            }
                        </button>
                    </li>
                }
            </ul>
        </li>
    }
</ol>
```

Replace `ui\components\slot-day-list\slot-day-list.component.scss` with:

```scss
:host {
    display: block;
}

.slot-days {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
    margin: 0;
    padding: 0;
    list-style: none;
}

.slot-day__header {
    display: flex;
    align-items: baseline;
    gap: 0.375rem;
    margin-block-end: 0.3rem;
}

.slot-day__name {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.8rem;
    color: var(--app-ink);
}

.slot-day__date,
.slot-day__note {
    font-size: 0.66rem;
    color: var(--app-text-muted);
}

.slot-day__note {
    margin-inline-start: auto;
}

.slot-day__chips {
    display: flex;
    gap: 0.375rem;
    margin: 0;
    padding: 0.45rem 0 0;
    list-style: none;
}

.slot-day__cell {
    display: flex;
    flex: 1;
    min-width: 0;
}

.slot-chip {
    position: relative;
    display: flex;
    flex: 1;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-width: 0;
    min-height: 2.5rem;
    padding: 0.5rem 0.25rem 0.45rem;
    border: 1.5px solid var(--app-border);
    border-radius: 8px;
    background: var(--app-bg-card);
    color: var(--app-ink);
    font: inherit;
    text-align: center;
    cursor: pointer;
}

.slot-chip:focus-visible {
    outline: 2px solid var(--p-sky-500);
    outline-offset: 2px;
}

.slot-chip--picked {
    border-color: var(--p-sky-500);
    background: var(--p-sky-50);
}

.slot-chip--picked .slot-chip__window {
    color: var(--p-sky-700);
}

.slot-chip--unavailable {
    background: var(--app-slot-unavailable);
    color: var(--app-text-muted);
    cursor: default;
}

.slot-chip__window {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.72rem;
}

.slot-chip__time {
    margin-block-start: 0.125rem;
    font-size: 0.56rem;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}

.slot-chip__rank {
    position: absolute;
    inset-block-start: -0.45rem;
    inset-inline-end: -0.375rem;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.2rem;
    height: 1.2rem;
    border-radius: 999px;
    background: var(--app-grad-sky);
    color: var(--app-on-accent);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.66rem;
    box-shadow: var(--app-shadow-lift);
}
```

The badge sits at the **top inline-end** corner (mockup `insetInlineEnd:-6`), so it mirrors to the top-left in Hebrew without any per-direction CSS; the chip list gains `0.45rem` of top padding so the badge is not clipped. `font: inherit` stops the browser's default button font from overriding the chip typography.

- [x] **Step 4: `PickSheetComponent` (mockup `SSlotSheet`)**

`ui\components\pick-sheet\pick-sheet.component.ts`:

```typescript
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    input,
    linkedSignal,
    output,
    viewChild,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { FocusTrapModule } from 'primeng/focustrap';
import { TextareaModule } from 'primeng/textarea';
import { PickSheet } from '../../../domain/pick-sheet';
import { SessionType } from '../../../domain/session-type.enum';
import { SLOT_CONSTRAINT_MAX_LENGTH, toSlotConstraint } from '../../../domain/slot-constraint';
import { PickChoice } from '../../../domain/slot-pick';

@Component({
    selector: 'app-pick-sheet',
    imports: [TranslocoPipe, ButtonModule, FocusTrapModule, TextareaModule],
    templateUrl: './pick-sheet.component.html',
    styleUrl: './pick-sheet.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(keydown.escape)': 'cancelled.emit()' },
})
export class PickSheetComponent {
    readonly sheet = input.required<PickSheet>();

    readonly saved = output<PickChoice>();
    readonly removed = output<void>();
    readonly cancelled = output<void>();

    protected readonly sessionTypes = SessionType;
    protected readonly maxConstraintLength = SLOT_CONSTRAINT_MAX_LENGTH;
    protected readonly sessionType = linkedSignal(() => this.sheet().choice.sessionType);
    protected readonly constraintText = linkedSignal(() => this.sheet().choice.constraint ?? '');

    private readonly firstChoice = viewChild.required<ElementRef<HTMLInputElement>>('firstChoice');

    constructor() {
        afterNextRender(() => this.firstChoice().nativeElement.focus());
    }

    protected chooseSessionType(sessionType: SessionType): void {
        this.sessionType.set(sessionType);
    }

    protected onConstraintInput(event: Event): void {
        const field = event.target as HTMLTextAreaElement;
        this.constraintText.set(field.value);
    }

    protected save(): void {
        const choice = {
            sessionType: this.sessionType(),
            constraint: toSlotConstraint(this.constraintText()),
        };

        this.saved.emit(choice);
    }
}
```

`ui\components\pick-sheet\pick-sheet.component.html`:

```html
@let dayName = 'weekGrid.days.' + sheet().day | transloco;
@let windowName = 'weekGrid.windows.' + sheet().window | transloco;
<div class="pick-sheet__backdrop" aria-hidden="true" (click)="cancelled.emit()"></div>
<section class="pick-sheet" role="dialog" aria-modal="true" aria-labelledby="pick-sheet-title" pFocusTrap>
    <span class="pick-sheet__grip" aria-hidden="true"></span>

    <header class="pick-sheet__header">
        <span class="pick-sheet__rank" aria-hidden="true">{{ sheet().rank }}</span>
        <h2 id="pick-sheet-title" class="pick-sheet__title">
            {{ 'studentForm.slotLabel' | transloco: { day: dayName, window: windowName } }}
        </h2>
        <span class="pick-sheet__time" dir="ltr">{{ sheet().timeLabel }}</span>
    </header>

    <fieldset class="pick-sheet__types">
        <legend class="pick-sheet__label">{{ 'studentForm.pick.sessionType' | transloco }}</legend>
        <div class="pick-sheet__options">
            <label
                class="pick-sheet__option"
                [class.pick-sheet__option--selected]="sessionType() === sessionTypes.single">
                <input
                    #firstChoice
                    type="radio"
                    name="pick-session-type"
                    class="p-hidden-accessible pick-sheet__single"
                    [checked]="sessionType() === sessionTypes.single"
                    (change)="chooseSessionType(sessionTypes.single)" />
                {{ 'studentForm.pick.single' | transloco }}
            </label>
            <label
                class="pick-sheet__option"
                [class.pick-sheet__option--selected]="sessionType() === sessionTypes.double">
                <input
                    type="radio"
                    name="pick-session-type"
                    class="p-hidden-accessible pick-sheet__double"
                    [checked]="sessionType() === sessionTypes.double"
                    (change)="chooseSessionType(sessionTypes.double)" />
                {{ 'studentForm.pick.double' | transloco }}
                <span class="pick-sheet__double-hint">{{ 'studentForm.pick.doubleHint' | transloco }}</span>
            </label>
        </div>
    </fieldset>

    <div class="pick-sheet__field">
        <label class="pick-sheet__label" for="pick-constraint">{{ 'studentForm.pick.constraintLabel' | transloco }}</label>
        <textarea
            pTextarea
            id="pick-constraint"
            class="pick-sheet__constraint"
            rows="2"
            aria-describedby="pick-constraint-hint"
            [attr.maxlength]="maxConstraintLength"
            [value]="constraintText()"
            (input)="onConstraintInput($event)"></textarea>
        <small id="pick-constraint-hint" class="pick-sheet__hint">{{ 'studentForm.pick.constraintHint' | transloco }}</small>
    </div>

    @if (sheet().isEditing) {
        <p-button
            type="button"
            class="pick-sheet__remove"
            severity="danger"
            [text]="true"
            [label]="'studentForm.pick.remove' | transloco"
            (onClick)="removed.emit()" />
    }

    <div class="pick-sheet__actions">
        <p-button
            type="button"
            class="pick-sheet__cancel"
            [text]="true"
            [label]="'studentForm.pick.cancel' | transloco"
            (onClick)="cancelled.emit()" />
        <p-button
            type="button"
            class="pick-sheet__save"
            fluid
            [label]="
                sheet().isEditing
                    ? ('studentForm.pick.save' | transloco)
                    : ('studentForm.pick.add' | transloco: { rank: sheet().rank })
            "
            (onClick)="save()" />
    </div>
</section>
```

`ui\components\pick-sheet\pick-sheet.component.scss`:

```scss
:host {
    position: fixed;
    inset: 0;
    z-index: 1100;
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
}

.pick-sheet__backdrop {
    position: absolute;
    inset: 0;
    background: var(--app-scrim);
}

.pick-sheet {
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    width: 100%;
    max-width: 30rem;
    max-height: 90dvh;
    margin-inline: auto;
    padding: 0.875rem 1rem 1.125rem;
    overflow-y: auto;
    border-start-start-radius: 16px;
    border-start-end-radius: 16px;
    background: var(--app-bg-card);
    box-shadow: var(--app-shadow-lift);
}

.pick-sheet__grip {
    align-self: center;
    width: 2.25rem;
    height: 0.25rem;
    border-radius: 99px;
    background: var(--app-border);
}

.pick-sheet__header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.pick-sheet__rank {
    display: flex;
    flex: none;
    align-items: center;
    justify-content: center;
    width: 1.4rem;
    height: 1.4rem;
    border-radius: 999px;
    background: var(--app-grad-sky);
    color: var(--app-on-accent);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.72rem;
}

.pick-sheet__title {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--app-ink);
}

.pick-sheet__time {
    margin-inline-start: auto;
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}

.pick-sheet__types {
    min-width: 0;
    margin: 0;
    padding: 0;
    border: 0;
}

.pick-sheet__label {
    display: block;
    margin-block-end: 0.375rem;
    padding: 0;
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.75rem;
    color: var(--app-text-secondary);
}

.pick-sheet__options {
    display: flex;
    gap: 0.5rem;
}

.pick-sheet__option {
    display: flex;
    flex: 1;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;
    min-height: 2.75rem;
    padding: 0.625rem 0.5rem;
    border: 1.5px solid var(--app-border);
    border-radius: 8px;
    font-size: 0.8rem;
    color: var(--app-text-secondary);
    cursor: pointer;
}

.pick-sheet__option--selected {
    border: 2px solid var(--p-sky-500);
    background: var(--p-sky-50);
    color: var(--p-sky-700);
    font-weight: 500;
}

.pick-sheet__option:has(:focus-visible) {
    outline: 2px solid var(--p-sky-500);
    outline-offset: 2px;
}

.pick-sheet__double-hint {
    font-size: 0.66rem;
    color: var(--app-text-muted);
}

.pick-sheet__field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.pick-sheet__constraint {
    width: 100%;
    font-size: 1rem;
    resize: vertical;
}

.pick-sheet__hint {
    font-size: 0.72rem;
    color: var(--app-text-secondary);
}

.pick-sheet__remove {
    align-self: flex-start;
}

.pick-sheet__actions {
    display: flex;
    align-items: center;
    gap: 0.625rem;
}

.pick-sheet__save {
    flex: 1;
}
```

Notes for the implementer:
- The Single / Double options are real radio inputs (visually hidden with PrimeNG's `p-hidden-accessible`) inside big labels — one tab stop, arrow-key switching and a screen-reader "radio, 1 of 2" for free; the label is the 44px target.
- `font-size: 1rem` on the textarea stops iOS Safari from zooming the page when it gains focus.
- `max-width: 30rem` + `margin-inline: auto` matches the student shell column (`student-shell.component.scss`), so on desktop the sheet does not stretch edge to edge.
- Focus: `afterNextRender` puts focus on the Single option when the sheet opens; `pFocusTrap` keeps Tab inside the sheet; Escape (host listener) and the backdrop cancel. Closing is the store clearing `openSlotId`, which destroys the component — nothing lingers.

- [x] **Step 5: `SlotsStepComponent` — picking, footer, sheet**

Replace `ui\components\slots-step\slots-step.component.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { PickSheet } from '../../../domain/pick-sheet';
import { SlotDay } from '../../../domain/slot-day';
import { PickChoice } from '../../../domain/slot-pick';
import { PickSheetComponent } from '../pick-sheet/pick-sheet.component';
import { SlotDayListComponent } from '../slot-day-list/slot-day-list.component';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-slots-step',
    imports: [TranslocoPipe, ButtonModule, PickSheetComponent, SlotDayListComponent, WizardStepComponent],
    templateUrl: './slots-step.component.html',
    styleUrl: './slots-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly days = input.required<readonly SlotDay[]>();
    readonly targetCount = input.required<number>();
    readonly pickCount = input.required<number>();
    readonly pickSheet = input.required<PickSheet | null>();

    readonly chipSelected = output<string>();
    readonly pickSaved = output<PickChoice>();
    readonly pickRemoved = output<void>();
    readonly pickCancelled = output<void>();
    readonly reviewed = output<void>();
    readonly back = output<void>();

    protected readonly isTargetCovered = computed(() => this.pickCount() >= this.targetCount());
}
```

Replace `ui\components\slots-step\slots-step.component.html` with:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.slots.title' | transloco"
    [intro]="'studentForm.slots.body' | transloco"
    [canGoBack]="true"
    (back)="back.emit()">
    <app-slot-day-list [days]="days()" (chipSelected)="chipSelected.emit($event)" />

    <div wizardFooter>
        <p class="slots__summary" aria-live="polite">
            <strong>{{ 'studentForm.slots.summary' | transloco: { target: targetCount(), picked: pickCount() } }}</strong>
            @if (isTargetCovered()) {
                <i class="pi pi-check slots__covered" aria-hidden="true"></i>
            }
            <span class="slots__backups">{{ 'studentForm.slots.backups' | transloco }}</span>
        </p>
        <p-button
            type="button"
            fluid
            [label]="'studentForm.slots.review' | transloco"
            [disabled]="!pickCount()"
            (onClick)="reviewed.emit()" />
    </div>
</app-wizard-step>

@if (pickSheet(); as sheet) {
    <app-pick-sheet
        [sheet]="sheet"
        (saved)="pickSaved.emit($event)"
        (removed)="pickRemoved.emit()"
        (cancelled)="pickCancelled.emit()" />
}
```

`ui\components\slots-step\slots-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.slots__summary {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
    margin: 0;
    font-size: 0.8rem;
    color: var(--app-text-secondary);

    strong {
        color: var(--app-ink);
    }
}

.slots__covered {
    font-size: 0.8rem;
    color: var(--p-green-600);
}

.slots__backups {
    margin-inline-start: auto;
    font-size: 0.72rem;
    color: var(--app-text-muted);
}
```

`isTargetCovered` only decides whether the ✓ is drawn (mockup `SSlots`); whether a list is submittable stays the backend's rule, mirrored for feedback on the review step (task 10). The `p` comes before the button in the footer, so `continueButton` (first footer **button**) is still "Review my list".

- [x] **Step 6: Store — picks and the open sheet**

In `state\student-form.store.ts`:

1. Add these imports (keep them sorted by path):
   ```typescript
   import { SlotState } from '../../../shared/models/slot-state.enum';
   ```
   after the `LanguageService` import, and
   ```typescript
   import { PickSheet, pickSheetFor } from '../domain/pick-sheet';
   ```
   after the `national-id-input` import, and
   ```typescript
   import { PickChoice, removePick, SlotPick, upsertPick } from '../domain/slot-pick';
   ```
   after the `slot-day` import.
2. Add these private signals after `private readonly target = signal(MIN_TARGET_COUNT);`:
   ```typescript
       private readonly picks = signal<SlotPick[]>([]);
       private readonly openSlotId = signal<string | null>(null);
   ```
3. In `slotDays`, replace the fourth argument `[]` with `this.picks()`:
   ```typescript
               ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang(), this.picks())
   ```
4. Add these computeds directly after `readonly minTargetCount = MIN_TARGET_COUNT;`:
   ```typescript
       readonly pickCount = computed(() => this.picks().length);
       readonly pickSheet = computed<PickSheet | null>(() => {
           const slotId = this.openSlotId();
           const slot = this.student()?.slots.find(x => x.id === slotId);

           return slot ? pickSheetFor(slot, this.picks()) : null;
       });
   ```
5. Add these methods directly after `decreaseTarget()`:
   ```typescript

       openPick(slotId: string): void {
           const slot = this.student()?.slots.find(x => x.id === slotId);

           if (slot?.state !== SlotState.open) {
               return;
           }

           this.openSlotId.set(slotId);
       }

       savePick(choice: PickChoice): void {
           const slotId = this.openSlotId();

           if (!slotId) {
               return;
           }

           this.picks.update(picks => upsertPick(picks, { slotId, ...choice }));
           this.openSlotId.set(null);
       }

       removeOpenPick(): void {
           const slotId = this.openSlotId();

           if (!slotId) {
               return;
           }

           this.picks.update(picks => removePick(picks, slotId));
           this.openSlotId.set(null);
       }

       closePick(): void {
           this.openSlotId.set(null);
       }
   ```

`openPick` refusing a non-Open slot is display integrity (the chip is disabled; a stale or synthetic click still cannot open a sheet) — the rule itself is enforced by the backend (task 2). Picks live only in this per-visit store: never `localStorage`, never the URL (Global Constraints), and a refresh starts over (roadmap decision 4).

- [x] **Step 7: Wire the page**

In `ui\pages\student-form\student-form.page.html`, replace the `@case (steps.slots) { … }` block with:

```html
                @case (steps.slots) {
                    <app-slots-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [days]="store.slotDays()"
                        [targetCount]="store.targetCount()"
                        [pickCount]="store.pickCount()"
                        [pickSheet]="store.pickSheet()"
                        (chipSelected)="store.openPick($event)"
                        (pickSaved)="store.savePick($event)"
                        (pickRemoved)="store.removeOpenPick()"
                        (pickCancelled)="store.closePick()"
                        (back)="store.goBack()" />
                }
```

`(reviewed)` is wired in task 10 together with the review step it leads to — until then "Review my list" is enabled after the first pick but does nothing.

- [x] **Step 8: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — including the ten picking cases.

Then `npm run build` → success, no new warnings.

- [x] **Step 9: Commit**

```bash
git add client/src/app/features/student-form client/src/styles/_tokens.scss client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): student picks ranked slots through a bottom sheet

Open slots are tappable chips; each pick opens a sheet for Single or
Double and an optional constraint, and joins the ranked list in the
order it was tapped. Picked slots can be changed or removed; unavailable
slots cannot be tapped. The footer tracks target against picks."
```

---

**Next:** [task-10-client-review-and-confirmation.md](task-10-client-review-and-confirmation.md)
