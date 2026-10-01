# Task 2 of 4: Move up / Move down on the review step (US-38 acceptance criterion)

> Part of [US-38: Reorder the Ranked List](README.md). Requires task 1 committed. Work on branch `38-us-38-reorder-ranked-list`.

**Files:**
- Modify: `client\src\app\features\student-form\state\student-form.store.ts` (import, one signal, two computeds, one method, two resets)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` (full replacement below)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.html` (full replacement below)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss` (full replacement below)
- Modify: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html` (three bindings on `<app-review-step>`)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (four keys under `studentForm.review`)
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts` (four helpers + a new `describe('reordering the ranked list')`)

**Interfaces:**
- Consumes (task 1): `PickMove { slotId: string; toIndex: number }`, `movePick(picks, move): SlotPick[]`, plus the existing `rankOf(picks, slotId): number | null` from `domain\slot-pick.ts`.
- Produces (task 3 relies on these exact names):
  - Store: `readonly canReorder: Signal<boolean>` (true when there are at least two picks), `readonly movedPickRank: Signal<number | null>`, `reorderPick(move: PickMove): void` (ignored when `!canReorder()` or while `submitStatus() === SubmitStatus.submitting`).
  - `ReviewStepComponent`: inputs `canReorder: boolean`, `movedPickRank: number | null`; output `moved: PickMove`; protected `isSubmitting` (already exists).
  - DOM hooks used by specs and by task 3: each row `li.review__item[data-pick-id="{slotId}"]`; buttons `p-button.review__move-up` / `p-button.review__move-down` inside `.review__moves`; `p.review__reorder-hint`; `p.review__announcement` (always rendered, `aria-live="polite"`).
  - Translation keys `studentForm.review.moveUp`, `moveDown`, `movedTo`, `reorderHint`. Task 3 rewrites the text of `reorderHint`.

**Why this shape:** the store already derives ranks (`reviewItems`), chip badges (`slotDays`) and the request (`submit`) from the order of `picks`. One mutation that reorders `chosenPicks` is enough (README Context). The buttons are the guaranteed, accessible path (README decision 2), so they ship first and satisfy the acceptance criterion on their own. Dragging is added in task 3.

- [ ] **Step 1: Write the failing specs**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Add these helpers directly **after** the existing `accessibleNameOf` function (just before `describe('StudentFormPage', () => {`):

```ts
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
```

2. Add this `describe` block directly **after** the closing `});` of `describe('review and submit', …)` and before `describe('returning student', …)`:

```ts
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
```

Fixture facts the specs rely on (already in the file): `COHEN_STUDENT` has every slot of the week open, with ids `{day}-{window}`. `RETURNING_STUDENT`'s saved list is `monday-noon` (Double, "only after 16:00"), `sunday-afternoon`, `wednesday-evening`, target 2. `reachReview(fixture, target, slotIds)` adds the picks in that order. `TranslocoTestingModule` has no translations, so text and labels render as their key paths (matched with `/…key$/`, the file's precedent).

- [ ] **Step 2: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. Every new spec that calls `moveButton(…)` fails with `Cannot read properties of null (reading 'click')` or `(reading 'disabled')`, and `offers no reordering for a single pick` **passes** (nothing renders yet). Every pre-existing spec still passes.

- [ ] **Step 3: Store — `canReorder`, `movedPickRank`, `reorderPick`**

In `client\src\app\features\student-form\state\student-form.store.ts`:

1. Replace the import line

```ts
import { PickChoice, removePick, SlotPick, upsertPick } from '../domain/slot-pick';
```

with

```ts
import { movePick, PickChoice, PickMove, rankOf, removePick, SlotPick, upsertPick } from '../domain/slot-pick';
```

2. After `private readonly sentAsRevision = signal(false);` add:

```ts
    private readonly movedSlotId = signal<string | null>(null);
```

3. After `readonly pickCount = computed(() => this.picks().length);` add:

```ts
    readonly canReorder = computed(() => this.pickCount() > SINGLE_PICK);
    readonly movedPickRank = computed(() => {
        const slotId = this.movedSlotId();

        return slotId ? rankOf(this.picks(), slotId) : null;
    });
```

(`SINGLE_PICK = 1` is already declared at the top of the file.)

4. After the `closePick()` method add:

```ts
    reorderPick(move: PickMove): void {
        if (!this.canReorder() || this.submitState() === SubmitStatus.submitting) {
            return;
        }

        this.chosenPicks.set(movePick(this.picks(), move));
        this.movedSlotId.set(move.slotId);
    }
```

5. In `continueToReview()`, change

```ts
        this.submitState.set(SubmitStatus.idle);
        this.step.set(StudentFormStep.review);
```

to

```ts
        this.submitState.set(SubmitStatus.idle);
        this.movedSlotId.set(null);
        this.step.set(StudentFormStep.review);
```

6. Replace `editSubmission()` with:

```ts
    editSubmission(): void {
        this.movedSlotId.set(null);
        this.step.set(StudentFormStep.review);
    }
```

`reorderPick` writes `movePick(this.picks(), …)`, not `chosenPicks()`, the same as `savePick` and `removeOpenPick`. Picks whose slot is no longer open are dropped at that moment, so ranks and the request never count a hidden pick.

- [ ] **Step 4: Review step — buttons, focus, live region, hint**

Replace `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` with:

```ts
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    inject,
    Injector,
    input,
    output,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ReviewItem } from '../../../domain/review-item';
import { SessionType } from '../../../domain/session-type.enum';
import { PickMove } from '../../../domain/slot-pick';
import { SubmitStatus } from '../../../domain/submit-status.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const SINGLE_PREFERRED_TARGET = 1;
const SINGLE_DROPPED_PICK = 1;
const MOVE_UP_BUTTON = '.review__move-up button';
const MOVE_DOWN_BUTTON = '.review__move-down button';

@Component({
    selector: 'app-review-step',
    imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent],
    templateUrl: './review-step.component.html',
    styleUrl: './review-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly targetCount = input.required<number>();
    readonly pickCount = input.required<number>();
    readonly items = input.required<readonly ReviewItem[]>();
    readonly missingPicks = input.required<number>();
    readonly replacesEarlierSubmission = input.required<boolean>();
    readonly droppedPickCount = input.required<number>();
    readonly submitStatus = input.required<SubmitStatus>();
    readonly canSubmit = input.required<boolean>();
    readonly canReorder = input.required<boolean>();
    readonly movedPickRank = input.required<number | null>();

    readonly submitted = output<void>();
    readonly addSlots = output<void>();
    readonly changeTarget = output<void>();
    readonly recheck = output<void>();
    readonly back = output<void>();
    readonly moved = output<PickMove>();

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly injector = inject(Injector);

    protected readonly statuses = SubmitStatus;
    protected readonly sessionTypes = SessionType;
    protected readonly isSubmitting = computed(() => this.submitStatus() === SubmitStatus.submitting);
    protected readonly preferredKey = computed(() =>
        this.targetCount() === SINGLE_PREFERRED_TARGET
            ? 'studentForm.review.preferredOne'
            : 'studentForm.review.preferredMany',
    );
    protected readonly droppedKey = computed(() =>
        this.droppedPickCount() === SINGLE_DROPPED_PICK ? 'studentForm.review.droppedOne' : 'studentForm.review.droppedMany',
    );

    protected moveUp(item: ReviewItem): void {
        this.move(item.slotId, positionOf(item) - 1, [MOVE_UP_BUTTON, MOVE_DOWN_BUTTON]);
    }

    protected moveDown(item: ReviewItem): void {
        this.move(item.slotId, positionOf(item) + 1, [MOVE_DOWN_BUTTON, MOVE_UP_BUTTON]);
    }

    private move(slotId: string, toIndex: number, focusOrder: readonly string[]): void {
        this.moved.emit({ slotId, toIndex });
        afterNextRender(() => this.focusMoveButton(slotId, focusOrder), { injector: this.injector });
    }

    private focusMoveButton(slotId: string, focusOrder: readonly string[]): void {
        const row = this.host.nativeElement.querySelector(`[data-pick-id="${slotId}"]`);
        const buttons = focusOrder.map(selector => row?.querySelector<HTMLButtonElement>(selector));

        buttons.find(button => button && !button.disabled)?.focus();
    }
}

function positionOf(item: ReviewItem): number {
    return item.rank - 1;
}
```

Replace `client\src\app\features\student-form\ui\components\review-step\review-step.component.html` with:

```html
<app-wizard-step
    [stepNumber]="stepNumber()"
    [stepCount]="stepCount()"
    [heading]="'studentForm.review.title' | transloco"
    [canGoBack]="true"
    (back)="back.emit()">
    <div class="review">
        @if (missingPicks() > 0) {
            <p-message severity="error" class="review__not-enough">
                <span>
                    <strong>{{ 'studentForm.review.notEnoughTitle' | transloco }}</strong>
                    {{
                        'studentForm.review.notEnoughBody'
                            | transloco: { target: targetCount(), picked: pickCount(), missing: missingPicks() }
                    }}
                </span>
            </p-message>
            <div class="review__fixes">
                <p-button
                    type="button"
                    class="review__add-slots"
                    severity="secondary"
                    fluid
                    [outlined]="true"
                    [label]="'studentForm.review.addSlots' | transloco"
                    (onClick)="addSlots.emit()" />
                <p-button
                    type="button"
                    class="review__change-target"
                    [text]="true"
                    [label]="'studentForm.review.changeTarget' | transloco"
                    (onClick)="changeTarget.emit()" />
            </div>
        } @else {
            <p class="review__summary">
                <strong>{{ 'studentForm.slots.summary' | transloco: { target: targetCount(), picked: pickCount() } }}</strong>
                {{ preferredKey() | transloco: { target: targetCount() } }}
            </p>
        }

        @if (replacesEarlierSubmission()) {
            <p-message severity="info" class="review__replaces">
                {{ 'studentForm.review.replaces' | transloco }}
            </p-message>
        }

        @if (droppedPickCount() > 0) {
            <p-message severity="warn" class="review__dropped">
                {{ droppedKey() | transloco: { count: droppedPickCount() } }}
            </p-message>
        }

        @if (canReorder()) {
            <p class="review__reorder-hint">{{ 'studentForm.review.reorderHint' | transloco }}</p>
        }

        <ol class="review__list">
            @for (item of items(); track item.slotId; let first = $first, last = $last) {
                @let dayName = 'weekGrid.days.' + item.day | transloco;
                @let windowName = 'weekGrid.windows.' + item.window | transloco;
                @let slotName = 'studentForm.slotLabel' | transloco: { day: dayName, window: windowName };
                <li
                    class="review__item"
                    [class.review__item--preferred]="item.isPreferred"
                    [attr.data-pick-id]="item.slotId">
                    <span class="review__rank" aria-hidden="true">{{ item.rank }}</span>
                    <div class="review__details">
                        <p class="review__slot">
                            {{ slotName }}
                            <span class="review__time" dir="ltr">{{ item.timeLabel }}</span>
                        </p>
                        <p class="review__meta">
                            <span
                                class="review__type"
                                [class.review__type--double]="item.sessionType === sessionTypes.double">
                                {{ 'studentForm.pick.' + item.sessionType | transloco }}
                            </span>
                            @if (item.constraint) {
                                <q class="review__constraint">{{ item.constraint }}</q>
                            }
                        </p>
                    </div>
                    @if (canReorder()) {
                        <div class="review__moves">
                            <p-button
                                type="button"
                                class="review__move-up"
                                icon="pi pi-arrow-up"
                                severity="secondary"
                                [text]="true"
                                [rounded]="true"
                                [disabled]="first || isSubmitting()"
                                [ariaLabel]="'studentForm.review.moveUp' | transloco: { rank: item.rank, slot: slotName }"
                                (onClick)="moveUp(item)" />
                            <p-button
                                type="button"
                                class="review__move-down"
                                icon="pi pi-arrow-down"
                                severity="secondary"
                                [text]="true"
                                [rounded]="true"
                                [disabled]="last || isSubmitting()"
                                [ariaLabel]="'studentForm.review.moveDown' | transloco: { rank: item.rank, slot: slotName }"
                                (onClick)="moveDown(item)" />
                        </div>
                    }
                </li>
            }
        </ol>

        <p class="review__announcement" aria-live="polite">
            @if (movedPickRank(); as rank) {
                {{ 'studentForm.review.movedTo' | transloco: { rank } }}
            }
        </p>

        @switch (submitStatus()) {
            @case (statuses.rejected) {
                <p-message severity="error" class="review__rejected">
                    {{ 'studentForm.review.errors.rejected' | transloco }}
                </p-message>
                <p-button
                    type="button"
                    class="review__recheck"
                    severity="secondary"
                    [outlined]="true"
                    [label]="'studentForm.review.recheck' | transloco"
                    (onClick)="recheck.emit()" />
            }
            @case (statuses.notFound) {
                <p-message severity="error" class="review__not-found">
                    {{ 'studentForm.review.errors.notFound' | transloco }}
                </p-message>
            }
            @case (statuses.failed) {
                <p-message severity="warn" class="review__failed">
                    {{ 'studentForm.review.errors.failed' | transloco }}
                </p-message>
            }
        }
    </div>

    <div wizardFooter>
        <p-button
            type="button"
            fluid
            [label]="'studentForm.review.submit' | transloco"
            [disabled]="!canSubmit()"
            [loading]="isSubmitting()"
            (onClick)="submitted.emit()" />
        @if (missingPicks() > 0) {
            <small class="review__unlock">{{ 'studentForm.review.unlockHint' | transloco: { target: targetCount() } }}</small>
        }
    </div>
</app-wizard-step>
```

Replace `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss` with (changes from the current file: `.review__details` gets `flex: 1`; `.review__reorder-hint`, `.review__moves` and `.review__announcement` are new):

```scss
:host {
    display: flex;
    flex: 1;
    flex-direction: column;
}

.review {
    display: flex;
    flex-direction: column;
    gap: 0.625rem;
}

.review .p-message-leave-active {
    animation: none;
}

.review__summary {
    margin: 0;
    font-size: 0.8rem;
    line-height: 1.5;
    color: var(--app-text-secondary);

    strong {
        color: var(--app-ink);
    }
}

.review__fixes {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.25rem;
}

.review__reorder-hint {
    margin: 0;
    font-size: 0.72rem;
    color: var(--app-text-muted);
}

.review__list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 0;
    padding: 0;
    list-style: none;
}

.review__item {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    padding: 0.7rem 0.75rem;
    border: 1.5px solid var(--app-border);
    border-radius: 10px;
    background: var(--app-bg-card);
    box-shadow: var(--app-shadow-card);
}

.review__item--preferred {
    border-color: var(--p-sky-500);
}

.review__rank {
    display: flex;
    flex: none;
    align-items: center;
    justify-content: center;
    width: 1.3rem;
    height: 1.3rem;
    border-radius: 999px;
    background: var(--app-border);
    color: var(--app-text-secondary);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.7rem;
}

.review__item--preferred .review__rank {
    background: var(--app-grad-sky);
    color: var(--app-on-accent);
}

.review__details {
    flex: 1;
    min-width: 0;
}

.review__slot {
    margin: 0;
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--app-ink);
}

.review__time {
    margin-inline-start: 0.25rem;
    font-size: 0.72rem;
    font-weight: 400;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}

.review__meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.375rem;
    margin: 0.25rem 0 0;
}

.review__type {
    padding: 0.125rem 0.5rem;
    border: 1px solid var(--app-border);
    border-radius: 999px;
    background: var(--app-bg-muted);
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.62rem;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    color: var(--app-text-secondary);
}

.review__type--double {
    background: var(--p-sky-50);
    color: var(--p-sky-700);
}

.review__constraint {
    font-size: 0.7rem;
    font-style: italic;
    color: var(--app-text-secondary);
    overflow-wrap: anywhere;
}

.review__moves {
    display: flex;
    flex: none;
    align-self: center;
    gap: 0.125rem;
    margin-block: -0.4rem;
    margin-inline-end: -0.4rem;
}

.review__announcement {
    position: absolute;
    width: 1px;
    height: 1px;
    margin: -1px;
    padding: 0;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
    border: 0;
}

.review__unlock {
    display: block;
    text-align: center;
    font-size: 0.7rem;
    color: var(--app-text-muted);
}
```

PrimeNG's default icon-only button is `2.5rem` (40px) square, which meets the 40px touch target. Do not add `size="small"`. The negative block/inline-end margins stop the 40px buttons from making each card taller than its two text lines.

- [ ] **Step 5: Bind the store on the page**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.html`, inside `<app-review-step …>`, change

```html
                        [canSubmit]="store.canSubmit()"
                        (submitted)="store.submit()"
```

to

```html
                        [canSubmit]="store.canSubmit()"
                        [canReorder]="store.canReorder()"
                        [movedPickRank]="store.movedPickRank()"
                        (submitted)="store.submit()"
                        (moved)="store.reorderPick($event)"
```

- [ ] **Step 6: Translations (both files, same commit)**

In `client\public\i18n\en.json`, under `studentForm.review`, change

```json
      "recheck": "Check the form again",
```

to

```json
      "recheck": "Check the form again",
      "reorderHint": "Use the arrows to move a pick up or down.",
      "moveUp": "Move pick {{rank}} ({{slot}}) up",
      "moveDown": "Move pick {{rank}} ({{slot}}) down",
      "movedTo": "Moved to rank {{rank}}.",
```

In `client\public\i18n\he.json`, under `studentForm.review`, change

```json
      "recheck": "בדיקת הטופס מחדש",
```

to

```json
      "recheck": "בדיקת הטופס מחדש",
      "reorderHint": "השתמשו בחיצים כדי להזיז בחירה למעלה או למטה.",
      "moveUp": "העברת בחירה {{rank}} ({{slot}}) למעלה",
      "moveDown": "העברת בחירה {{rank}} ({{slot}}) למטה",
      "movedTo": "הבחירה עברה לדירוג {{rank}}.",
```

Both anchors are unique (line 348 in each file at planning time). `{{slot}}` is the already-translated `studentForm.slotLabel` ("Wednesday · Afternoon" / "רביעי · אחה״צ").

- [ ] **Step 7: Run the specs and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including all 13 in `reordering the ranked list`.

Run (in `client\`): `npm run build`
Expected: builds clean, no new warnings (in particular no `NG8107`/`NG8109` template warnings, and no budget warning). Write down the `Initial total` size from the output, because task 3 compares against it.

If `keeps focus on a pick that reaches the top` fails because `document.activeElement` is `<body>`, check that `move()` passes `{ injector: this.injector }` to `afterNextRender`. Without it, the hook is not tied to this component's render and never runs.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/student-form/state/student-form.store.ts client/src/app/features/student-form/ui/components/review-step/review-step.component.ts client/src/app/features/student-form/ui/components/review-step/review-step.component.html client/src/app/features/student-form/ui/components/review-step/review-step.component.scss client/src/app/features/student-form/ui/pages/student-form/student-form.page.html client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(student-form): move picks up and down the ranked list before submitting

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
