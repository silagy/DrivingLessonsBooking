# Task 5 of 6: Client — the assigned teacher's week grid (US-51)

> Part of [US-50/51/27: Identity & Routing](README.md). Requires task 4 complete. Work on branch `52-us-50-51-27-identity-and-routing`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Create: `ui\components\slot-day-list\slot-day-list.component.ts`, `.html`, `.scss`
- Create: `ui\components\slots-step\slots-step.component.ts`, `.html`
- Modify: `ui\components\details-step\details-step.component.ts`, `.html` (Continue)
- Modify: `state\student-form.store.ts` (`slotDays`, `continueToSlots`)
- Modify: `ui\pages\student-form\student-form.page.ts`, `.html`
- Modify (test): `ui\pages\student-form\student-form.page.spec.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 3): `groupSlotsByDay`, `SlotDay`, `SlotChip`; (task 4): `StudentFormStore` members `student`, `teacherName`, `hasAvailability`, `stepNumber`, `stepCount`, `currentStep`; `WizardStepComponent`; `DetailsStepComponent`; page-spec helpers `provideOpenLinkIdentifying`, `identifyingAs`, `studentOf`, `weekSlots`, `COHEN_STUDENT`, `renderPage`, `clickContinue`, `continueButton`, `identifyAndContinue`, `page`.
- Produces (slice 3 builds on these):
  - `SlotDayListComponent` (`app-slot-day-list`): input `days: readonly SlotDay[]`; read-only chips in this slice — slice 3 adds pick state and a chip output.
  - `SlotsStepComponent` (`app-slots-step`): inputs `stepNumber`, `stepCount`, `teacherName`, `days`.
  - `DetailsStepComponent.continued = output<void>()`.
  - `StudentFormStore.slotDays: Signal<SlotDay[]>`, `continueToSlots(): void`.
  - Translation keys `studentForm.slots.{title, body, shortDay, unavailable}`.

Precedents: task 4's `details-step` / `wizard-step` components, mockup `student.jsx` → `SlotDayList`, `SlotChip`, `SSlots` (layout only — picking, rank badges and the "Target / Picked" footer are slice 3).

- [ ] **Step 1: Write the failing page-spec cases**

In `ui\pages\student-form\student-form.page.spec.ts`, add this `describe` block **inside** `describe('StudentFormPage', …)`, after the `details step` block:

```typescript
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
```

The two `it.each` rows are the page-level half of US-51 ("A sees Cohen's grid, B sees Levi's"): the page renders exactly the grid the backend resolved for the ID. The routing itself is the backend's and was proven over HTTP in task 2 Step 5.

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — the details step has no Continue button yet (`clickContinue` hits `null`) and `app-slots-step` does not exist. The "no availability" case already passes.

- [ ] **Step 2: Translations (en + he, same commit)**

In **both** files, add a `"slots"` object inside `"studentForm"`, directly after `"details"` (add the comma after the closing brace of `"details"`).

`client\public\i18n\en.json`:

```json
    "slots": {
      "title": "{{teacherName}}'s week",
      "body": "These are {{teacherName}}'s slots for this week. Striped slots are unavailable.",
      "shortDay": "morning & noon only",
      "unavailable": "Unavailable"
    }
```

`client\public\i18n\he.json`:

```json
    "slots": {
      "title": "השבוע של {{teacherName}}",
      "body": "אלה השעות של {{teacherName}} לשבוע הזה. שעות מקווקוות אינן זמינות.",
      "shortDay": "בוקר וצהריים בלבד",
      "unavailable": "לא זמין"
    }
```

Day and window names reuse the shared `weekGrid.days.*` / `weekGrid.windows.*` keys (README decision 12). Verify parity:

```bash
node -e "const f=(o,p='')=>Object.entries(o).flatMap(([k,v])=>typeof v==='object'?f(v,p+k+'.'):[p+k]);const en=f(require('./public/i18n/en.json')),he=f(require('./public/i18n/he.json'));console.log(en.filter(k=>!he.includes(k)),he.filter(k=>!en.includes(k)))"
```

Expected: `[] []`.

- [ ] **Step 3: `SlotDayListComponent` (mockup `SlotDayList` / `SlotChip`)**

`ui\components\slot-day-list\slot-day-list.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
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
}
```

`ui\components\slot-day-list\slot-day-list.component.html`:

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
                    <li class="slot-chip" [class.slot-chip--unavailable]="chip.isUnavailable">
                        <span class="slot-chip__window">{{ 'weekGrid.windows.' + chip.window | transloco }}</span>
                        <span class="slot-chip__time" dir="ltr">{{ chip.timeLabel }}</span>
                        @if (chip.isUnavailable) {
                            <span class="p-hidden-accessible">{{ 'studentForm.slots.unavailable' | transloco }}</span>
                        }
                    </li>
                }
            </ul>
        </li>
    }
</ol>
```

`ui\components\slot-day-list\slot-day-list.component.scss`:

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
    padding: 0;
    list-style: none;
}

.slot-chip {
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
    text-align: center;
}

.slot-chip--unavailable {
    background: var(--app-slot-unavailable);
    color: var(--app-text-muted);
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
```

Notes:
- Sunday renders at the inline-start edge in both languages because flex follows `dir` (client-i18n "week grid keeps Sunday first"); nothing mirrors by hand.
- The time label is wall-clock text (`07:00–12:00`), isolated with `dir="ltr"` so the digits never reorder inside Hebrew.
- Unavailable chips use the existing `--app-slot-unavailable` stripes (same token as the admin grid) plus a visually hidden "Unavailable" for screen readers — the state is never color-only.
- Chips are list items, not buttons: nothing is pickable until slice 3. `min-height: 2.5rem` already reserves the 40px touch target slice 3 needs.

- [ ] **Step 4: `SlotsStepComponent`**

`ui\components\slots-step\slots-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotDay } from '../../../domain/slot-day';
import { SlotDayListComponent } from '../slot-day-list/slot-day-list.component';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-slots-step',
    imports: [TranslocoPipe, SlotDayListComponent, WizardStepComponent],
    templateUrl: './slots-step.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly teacherName = input.required<string>();
    readonly days = input.required<readonly SlotDay[]>();
}
```

`ui\components\slots-step\slots-step.component.html`:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.slots.title' | transloco: { teacherName: teacherName() }"
    [intro]="'studentForm.slots.body' | transloco: { teacherName: teacherName() }">
    <app-slot-day-list [days]="days()" />
</app-wizard-step>
```

No footer content in this slice, so the wizard footer collapses (task 4 `:empty` rule). Slice 3 adds the "Target / Picked" footer and the review action here.

- [ ] **Step 5: Details step — Continue to the grid**

In `ui\components\details-step\details-step.component.ts`:

1. Change the Angular import line to:
   ```typescript
   import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
   ```
2. Add `import { ButtonModule } from 'primeng/button';` after the `TranslocoPipe` import, and add `ButtonModule` to `imports`:
   ```typescript
       imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent],
   ```
3. Add the output directly after `readonly hasAvailability = input.required<boolean>();`:
   ```typescript

       readonly continued = output<void>();
   ```

In `ui\components\details-step\details-step.component.html`, add this block after the closing `}` of the `@if (!hasAvailability())` block, still inside `<app-wizard-step>`:

```html

    @if (hasAvailability()) {
        <div wizardFooter>
            <p-button type="button" fluid [label]="'studentForm.continue' | transloco" (onClick)="continued.emit()" />
        </div>
    }
```

A teacher with no grid this week never gets a Continue — the notice is the end of the flow (**Review Focus 2**).

- [ ] **Step 6: Store — grid days and the slots step**

In `state\student-form.store.ts`:

1. Add the import (keep the domain imports alphabetical by path — it goes after `../domain/national-id-input`):
   ```typescript
   import { groupSlotsByDay } from '../domain/slot-day';
   ```
2. Add this computed directly after `readonly hasAvailability = …;`:
   ```typescript
       readonly slotDays = computed(() => {
           const student = this.student();
           const publication = this.publication();

           return student && publication
               ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang())
               : [];
       });
   ```
3. Add this method directly after `continueToDetails()`:
   ```typescript
       continueToSlots(): void {
           if (!this.hasAvailability()) {
               return;
           }

           this.step.set(StudentFormStep.slots);
       }
   ```

`slotDays` re-computes on a language switch, so day dates re-format without leaving the step. The caption for this step (`studentForm.weekTeacherCaption`) was already mapped in task 4.

- [ ] **Step 7: Wire the page**

In `ui\pages\student-form\student-form.page.ts`, add the import after the `IdentifyStepComponent` import:

```typescript
import { SlotsStepComponent } from '../../components/slots-step/slots-step.component';
```

and add `SlotsStepComponent` at the end of the `imports` array.

In `ui\pages\student-form\student-form.page.html`:

1. On the `<app-details-step … />` element, add the output binding after `[hasAvailability]="store.hasAvailability()"`:
   ```html
                            (continued)="store.continueToSlots()"
   ```
2. Add the slots case after the closing `}` of `@case (steps.details) { … }`:
   ```html
                @case (steps.slots) {
                    <app-slots-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [teacherName]="store.teacherName()"
                        [days]="store.slotDays()" />
                }
   ```

- [ ] **Step 8: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — including the 4 new slots-step cases.

Then `npm run build` → success, no new warnings.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/student-form client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): show the student's assigned teacher's week grid

After confirming their roster details, the student sees their own
teacher's slots day by day, with unavailable slots striped; the caption
names the teacher."
```

---

**Next:** [task-06-verification-and-pr.md](task-06-verification-and-pr.md)
