# Task 8 of 11: Client — five-step wizard, target stepper (US-33), Back, all-unavailable notice

> Part of [US-33…US-41: First Submission](README.md). Requires task 7 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`; client commands run from `client\`.

**Files** (under `client\src\app\features\student-form\` unless noted):
- Modify: `domain\student-form-step.enum.ts`, `domain\student-form-step.ts`, `domain\student-form-step.spec.ts`
- Modify: `ui\components\wizard-step\wizard-step.component.ts`, `.html`, `.scss` (+ Back)
- Create: `ui\components\target-step\target-step.component.ts`, `.html`, `.scss`
- Modify: `ui\components\slots-step\slots-step.component.ts`, `.html` (+ Back)
- Modify: `state\student-form.store.ts`
- Modify: `ui\pages\student-form\student-form.page.ts`, `.html`, `.spec.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

**Interfaces:**
- Consumes (task 7): `MIN_TARGET_COUNT`, `hasOpenSlot`, `groupSlotsByDay(…, picks)`. (Slice 2): `WizardStepComponent`, `DetailsStepComponent.continued`, `SlotsStepComponent`, page-spec helpers `provideOpenLinkIdentifying`, `identifyingAs`, `studentOf`, `weekSlots`, `COHEN_STUDENT`, `renderPage`, `page`, `clickContinue`, `continueButton`, `identifyAndContinue`.
- Produces (tasks 9–10 rely on these exact names):
  - `enum StudentFormStep { identify, details, target, slots, review, done }`; `WIZARD_STEPS = [identify, details, target, slots, review]` (so "Step n of **5**", roadmap/mockup numbering); `previousStepOf(step): StudentFormStep | null` (target → details, slots → target, review → slots, otherwise `null`).
  - `WizardStepComponent` inputs `canGoBack` (default `false`), output `back`.
  - `TargetStepComponent` (`app-target-step`): inputs `stepNumber`, `stepCount`, `targetCount`, `minTargetCount`; outputs `increased`, `decreased`, `continued`, `back`. Classes `.target__count`, `.target__increase`, `.target__decrease`.
  - `SlotsStepComponent` output `back`.
  - `StudentFormStore`: `targetCount`, `minTargetCount`, `hasAvailability` (now: at least one **Open** slot), `continueToTarget()`, `continueToSlots()` (now from the target step), `increaseTarget()`, `decreaseTarget()`, `goBack()`.
  - Translation keys `studentForm.back`, `studentForm.target.{title, body, unit, fewer, more, pickSlots}`.
  - Page-spec helpers `reachTarget`, `reachSlots`, `press`, `textOf`.

Precedents: slice 2's `details-step` / `slots-step` components and their page-spec cases; mockup `student.jsx` → `STarget` (stepper, "lessons · minimum 1", footer "PICK SLOTS"), `shared.jsx` → `MStep`.

**Decisions made here** (README decisions 11, 15):
- The mockup numbers the flow 1–5 (ID, details, target, slots, review); the confirmation has no step number. `done` joins the enum now so the caption map stays total; task 10 uses it.
- **Back** appears on target, slots and review only. It is not in the mockup, but with one route (roadmap decision 4) the browser Back button would leave the form and lose the picks; an in-page Back is the way to change the target after picking. Details has no Back: returning to the ID step would re-render an empty ID field next to a stale "Found you" result.
- The stepper uses two native `<button>`s around an `<output>` — the mockup's 48px circles, no PrimeNG `InputNumber` (which would open the numeric keyboard for a two-tap choice). `−` is disabled at `MIN_TARGET_COUNT` (immediate feedback for the backend's "target ≥ 1"); `+` has **no upper limit** (requirements §5.6).
- `hasAvailability` becomes "has at least one **Open** slot": a grid that is entirely Unavailable now gets slice 2's "no lesson slots — contact your school" notice instead of a grid with nothing to pick.

- [ ] **Step 1: Write the failing specs**

Replace `domain\student-form-step.spec.ts` with:

```typescript
import { StudentFormStep } from './student-form-step.enum';
import { previousStepOf, stepNumberOf, WIZARD_STEPS } from './student-form-step';

describe('wizard steps', () => {
    it('runs identify → details → target → slots → review', () => {
        expect(WIZARD_STEPS).toEqual([
            StudentFormStep.identify,
            StudentFormStep.details,
            StudentFormStep.target,
            StudentFormStep.slots,
            StudentFormStep.review,
        ]);
    });

    it.each([
        [StudentFormStep.identify, 1],
        [StudentFormStep.details, 2],
        [StudentFormStep.target, 3],
        [StudentFormStep.slots, 4],
        [StudentFormStep.review, 5],
    ])('numbers %s as step %i', (step, number) => {
        expect(stepNumberOf(step)).toBe(number);
    });

    it.each([
        [StudentFormStep.target, StudentFormStep.details],
        [StudentFormStep.slots, StudentFormStep.target],
        [StudentFormStep.review, StudentFormStep.slots],
    ])('goes back from %s to %s', (step, previous) => {
        expect(previousStepOf(step)).toBe(previous);
    });

    it.each([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.done])(
        'offers no way back from %s',
        step => {
            expect(previousStepOf(step)).toBeNull();
        },
    );
});
```

In `ui\pages\student-form\student-form.page.spec.ts`:

1. Add these helpers directly after `identifyAndContinue`:

```typescript

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
```

2. Add this `describe` block after the `details step` block:

```typescript
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
```

3. Replace the whole `describe('slots step', …)` block with:

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

        it('keeps the week grid read-only until picking arrives', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachTarget(fixture);

            //when
            await clickContinue(fixture);

            //then
            expect(page(fixture).querySelectorAll('.slot-days button, .slot-days input').length).toBe(0);
        });
    });
```

(The last case is replaced by the picking cases in task 9.)

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL — `previousStepOf` and the `target`/`review`/`done` steps do not exist, the details Continue still goes to the grid, and there is no `app-target-step` or Back button.

- [ ] **Step 2: Wizard steps**

Replace `domain\student-form-step.enum.ts` with:

```typescript
export enum StudentFormStep {
    identify = 'identify',
    details = 'details',
    target = 'target',
    slots = 'slots',
    review = 'review',
    done = 'done',
}
```

Replace `domain\student-form-step.ts` with:

```typescript
import { StudentFormStep } from './student-form-step.enum';

export const WIZARD_STEPS: readonly StudentFormStep[] = [
    StudentFormStep.identify,
    StudentFormStep.details,
    StudentFormStep.target,
    StudentFormStep.slots,
    StudentFormStep.review,
];

const PREVIOUS_STEP: Partial<Record<StudentFormStep, StudentFormStep>> = {
    [StudentFormStep.target]: StudentFormStep.details,
    [StudentFormStep.slots]: StudentFormStep.target,
    [StudentFormStep.review]: StudentFormStep.slots,
};

export function stepNumberOf(step: StudentFormStep): number {
    return WIZARD_STEPS.indexOf(step) + 1;
}

export function previousStepOf(step: StudentFormStep): StudentFormStep | null {
    return PREVIOUS_STEP[step] ?? null;
}
```

- [ ] **Step 3: Translations (en + he, same commit)**

In **both** files, inside `"studentForm"`: add `"back"` directly after `"continue"`, and add a `"target"` object directly after `"details"` (commas as JSON requires).

`client\public\i18n\en.json`:

```json
    "back": "Back",
```

```json
    "target": {
      "title": "How many lessons do you want this week?",
      "body": "You'll then pick at least that many slots, ranked by preference.",
      "unit": "lessons · minimum {{min}}",
      "fewer": "Fewer lessons",
      "more": "More lessons",
      "pickSlots": "Pick slots"
    },
```

`client\public\i18n\he.json`:

```json
    "back": "חזרה",
```

```json
    "target": {
      "title": "כמה שיעורים תרצו השבוע?",
      "body": "אחר כך תבחרו לפחות אותו מספר של שעות, לפי סדר העדפה.",
      "unit": "שיעורים · מינימום {{min}}",
      "fewer": "פחות שיעורים",
      "more": "יותר שיעורים",
      "pickSlots": "לבחירת שעות"
    },
```

Verify parity (in `client\`):

```bash
node -e "const f=(o,p='')=>Object.entries(o).flatMap(([k,v])=>typeof v==='object'?f(v,p+k+'.'):[p+k]);const en=f(require('./public/i18n/en.json')),he=f(require('./public/i18n/he.json'));console.log(en.filter(k=>!he.includes(k)),he.filter(k=>!en.includes(k)))"
```

Expected: `[] []`.

- [ ] **Step 4: `WizardStepComponent` — Back**

Replace `ui\components\wizard-step\wizard-step.component.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-wizard-step',
    imports: [TranslocoPipe, ButtonModule],
    templateUrl: './wizard-step.component.html',
    styleUrl: './wizard-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly heading = input<string>('');
    readonly intro = input<string>('');
    readonly canGoBack = input(false);

    readonly back = output<void>();
}
```

Replace `ui\components\wizard-step\wizard-step.component.html` with:

```html
<section class="wizard-step">
    <div class="wizard-step__body">
        <div class="wizard-step__top">
            <p class="wizard-step__counter">
                {{ 'studentForm.step' | transloco: { current: stepNumber(), total: stepCount() } }}
            </p>
            @if (canGoBack()) {
                <p-button
                    type="button"
                    class="wizard-step__back"
                    [text]="true"
                    [label]="'studentForm.back' | transloco"
                    (onClick)="back.emit()" />
            }
        </div>
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

In `ui\components\wizard-step\wizard-step.component.scss`, add after the `.wizard-step__body` rule:

```scss

.wizard-step__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
}

.wizard-step__back {
    margin-block: -0.5rem;
    margin-inline-end: -0.75rem;
}
```

The label is text only ("Back" / "חזרה") — no arrow glyph, which would need RTL flipping (client-i18n "no hardcoded directional glyphs"). The negative margins keep the counter row's height and align the label with the column edge; the button itself stays PrimeNG's full-size ~40px touch target. It sits in the body, never in the footer, so the page spec's `continueButton` (`.wizard-step__footer button`) never picks it up.

- [ ] **Step 5: `TargetStepComponent` (mockup `STarget`)**

`ui\components\target-step\target-step.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-target-step',
    imports: [TranslocoPipe, ButtonModule, WizardStepComponent],
    templateUrl: './target-step.component.html',
    styleUrl: './target-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TargetStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly targetCount = input.required<number>();
    readonly minTargetCount = input.required<number>();

    readonly increased = output<void>();
    readonly decreased = output<void>();
    readonly continued = output<void>();
    readonly back = output<void>();

    protected readonly isAtMinimum = computed(() => this.targetCount() <= this.minTargetCount());
}
```

`ui\components\target-step\target-step.component.html`:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.target.title' | transloco"
    [intro]="'studentForm.target.body' | transloco"
    [canGoBack]="true"
    (back)="back.emit()">
    <div class="target" role="group" [attr.aria-label]="'studentForm.target.title' | transloco">
        <button
            type="button"
            class="target__step target__decrease"
            [disabled]="isAtMinimum()"
            [attr.aria-label]="'studentForm.target.fewer' | transloco"
            (click)="decreased.emit()">
            <i class="pi pi-minus" aria-hidden="true"></i>
        </button>
        <output class="target__count" aria-live="polite">{{ targetCount() }}</output>
        <button
            type="button"
            class="target__step target__increase"
            [attr.aria-label]="'studentForm.target.more' | transloco"
            (click)="increased.emit()">
            <i class="pi pi-plus" aria-hidden="true"></i>
        </button>
    </div>
    <p class="target__unit">{{ 'studentForm.target.unit' | transloco: { min: minTargetCount() } }}</p>

    <div wizardFooter>
        <p-button type="button" fluid [label]="'studentForm.target.pickSlots' | transloco" (onClick)="continued.emit()" />
    </div>
</app-wizard-step>
```

`ui\components\target-step\target-step.component.scss`:

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.target {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 1.375rem;
    margin-block-start: 1.6rem;
}

.target__step {
    display: flex;
    flex: none;
    align-items: center;
    justify-content: center;
    width: 3rem;
    height: 3rem;
    padding: 0;
    border: 2px solid var(--app-ink);
    border-radius: 999px;
    background: var(--app-bg-card);
    color: var(--app-ink);
    font-size: 1.1rem;
    cursor: pointer;
}

.target__step:disabled {
    border-color: var(--app-border);
    color: var(--app-text-muted);
    cursor: default;
}

.target__step:focus-visible {
    outline: 2px solid var(--p-sky-500);
    outline-offset: 2px;
}

.target__count {
    min-width: 5rem;
    font-family: var(--app-font-display);
    font-weight: 500;
    font-size: 4rem;
    line-height: 1;
    text-align: center;
    font-variant-numeric: tabular-nums;
    color: var(--app-ink);
}

.target__unit {
    margin: 0;
    text-align: center;
    font-size: 0.75rem;
    color: var(--app-text-muted);
}
```

Flex follows `dir`, so in Hebrew `−` sits at the inline start (right) and `+` at the inline end (left) — mirror-correct with no per-direction CSS. `min-width` (not `width`) lets a three-digit target grow instead of overflowing.

- [ ] **Step 6: Slots step — Back**

In `ui\components\slots-step\slots-step.component.ts`, change the Angular import to `import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';` and add after `readonly days = …;`:

```typescript

    readonly back = output<void>();
```

Replace `ui\components\slots-step\slots-step.component.html` with:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.slots.title' | transloco: { teacherName: teacherName() }"
    [intro]="'studentForm.slots.body' | transloco: { teacherName: teacherName() }"
    [canGoBack]="true"
    (back)="back.emit()">
    <app-slot-day-list [days]="days()" />
</app-wizard-step>
```

- [ ] **Step 7: Store — target count, the new steps, Back, Open-slot availability**

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
import { groupSlotsByDay, hasOpenSlot } from '../domain/slot-day';
import { previousStepOf, stepNumberOf, WIZARD_STEPS } from '../domain/student-form-step';
import { StudentFormStep } from '../domain/student-form-step.enum';
import { viewForPublicationState } from '../domain/student-form-view';
import { StudentFormView } from '../domain/student-form-view.enum';
import { MIN_TARGET_COUNT } from '../domain/target-count';
import { weekRangeLabel } from '../domain/week-label';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const EMPTY_WEEK_PARAMS = { weekNumber: 0, weekRange: '' };

const CAPTION_KEY_BY_STEP: Record<StudentFormStep, string | null> = {
    [StudentFormStep.identify]: null,
    [StudentFormStep.details]: 'studentForm.weekCaption',
    [StudentFormStep.target]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.slots]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.review]: 'studentForm.weekTeacherCaption',
    [StudentFormStep.done]: null,
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
    private readonly target = signal(MIN_TARGET_COUNT);

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
    readonly hasAvailability = computed(() => hasOpenSlot(this.student()?.slots ?? []));
    readonly slotDays = computed(() => {
        const student = this.student();
        const publication = this.publication();

        return student && publication
            ? groupSlotsByDay(student.slots, publication.weekStart, this.language.lang(), [])
            : [];
    });

    readonly targetCount = this.target.asReadonly();
    readonly minTargetCount = MIN_TARGET_COUNT;

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

    continueToTarget(): void {
        if (!this.hasAvailability()) {
            return;
        }

        this.step.set(StudentFormStep.target);
    }

    continueToSlots(): void {
        if (!this.hasAvailability()) {
            return;
        }

        this.step.set(StudentFormStep.slots);
    }

    increaseTarget(): void {
        this.target.update(count => count + 1);
    }

    decreaseTarget(): void {
        this.target.update(count => Math.max(MIN_TARGET_COUNT, count - 1));
    }

    goBack(): void {
        const previous = previousStepOf(this.step());

        if (!previous) {
            return;
        }

        this.step.set(previous);
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

What changed from slice 2: the `target`/`review`/`done` captions (the teacher stays in the caption from the target step on — mockup `PhoneShell` default sub-bar; the confirmation has none, mockup `SDone noSub`), the `target` signal with `increaseTarget` / `decreaseTarget` (clamped at `MIN_TARGET_COUNT` — the button is disabled there too), `hasAvailability` via `hasOpenSlot`, `continueToTarget`, and `goBack` via `previousStepOf`.

- [ ] **Step 8: Wire the page**

In `ui\pages\student-form\student-form.page.ts`, add after the `StatusMessageComponent` import:

```typescript
import { TargetStepComponent } from '../../components/target-step/target-step.component';
```

and add `TargetStepComponent` to the `imports` array after `DetailsStepComponent`.

In `ui\pages\student-form\student-form.page.html`, replace the `@case (views.open) { … }` block with:

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
                            [hasAvailability]="store.hasAvailability()"
                            (continued)="store.continueToTarget()" />
                    }
                }
                @case (steps.target) {
                    <app-target-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [targetCount]="store.targetCount()"
                        [minTargetCount]="store.minTargetCount"
                        (increased)="store.increaseTarget()"
                        (decreased)="store.decreaseTarget()"
                        (continued)="store.continueToSlots()"
                        (back)="store.goBack()" />
                }
                @case (steps.slots) {
                    <app-slots-step
                        [stepNumber]="store.stepNumber()"
                        [stepCount]="store.stepCount"
                        [teacherName]="store.teacherName()"
                        [days]="store.slotDays()"
                        (back)="store.goBack()" />
                }
            }
        }
```

(The other `@case (views…)` blocks are unchanged.)

- [ ] **Step 9: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: every spec PASSES — including the new wizard-step cases, the four target-step cases and the updated slots-step cases.

Then `npm run build` → success, no new warnings.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/student-form client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(client): student declares a weekly target count

The student form is now five steps: after confirming their details the
student sets how many lessons they want (one or more, no upper limit)
before the week grid, and can step back from the target, grid and
review. A week with no open slot shows the contact-your-school notice."
```

---

**Next:** [task-09-client-slot-picking.md](task-09-client-slot-picking.md)
