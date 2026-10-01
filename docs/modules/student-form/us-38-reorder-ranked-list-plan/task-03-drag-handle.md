# Task 3 of 4: Drag-reorder by a grip handle (Angular CDK)

> Part of [US-38: Reorder the Ranked List](README.md). Requires tasks 1–2 committed. Work on branch `38-us-38-reorder-ranked-list`.

**Files:**
- Modify: `client\package.json`, `client\package-lock.json` (declare `@angular/cdk`, already locked at 21.2.14)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` (CDK imports + `onDropped`)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.html` (`cdkDropList` on the list, `cdkDrag` on each row, grip handle)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss` (grip, preview, placeholder, drop animation)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (reword `studentForm.review.reorderHint`)
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts` (two imports, two helpers, a new `describe('dragging the ranked list')`)

**Interfaces:**
- Consumes (task 2): `ReviewStepComponent` inputs `items`, `canReorder`, protected `isSubmitting`, output `moved: PickMove`. Store `reorderPick(move)` ignores calls while submitting or with fewer than two picks. Spec helpers `reviewOrder(fixture, selector?)`, `move(…)`, `moveButton(…)`. Row hook `li.review__item[data-pick-id]`, `p.review__announcement`.
- Consumes (CDK): `CdkDropList`, `CdkDrag`, `CdkDragHandle`, `CdkDragDrop<T>` (`previousIndex`, `currentIndex`) from `@angular/cdk/drag-drop`.
- Produces: protected `onDropped(drop: CdkDragDrop<readonly ReviewItem[]>): void` → emits `moved` with `{ slotId: items()[previousIndex].slotId, toIndex: currentIndex }`, or nothing when the indexes are equal. DOM hook `span.review__grip` (one per row while `canReorder()`).

**Why CDK:** README decision 3. It is already installed and locked as PrimeNG's peer dependency, so declaring it adds no new code to `node_modules`. It handles touch and mouse dragging, auto-scroll near the viewport edge and the drop animation. Dragging only from the handle keeps touch scrolling working on the rest of the card (decision 4). The drop goes through the same `moved` output as the buttons, so the store, the ranks and the request do not change.

- [ ] **Step 1: Declare `@angular/cdk`**

In `client\package.json`, in `"dependencies"`, add the line directly above `"@angular/common"`:

```json
    "@angular/cdk": "^21.2.0",
```

Then sync the lockfile with the nvm v22.6.0 npm (project memory: the default `npm` is broken for installs). In PowerShell:

```powershell
Set-Location V:\DrivingLessonsBooking\client
& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" install
```

`EBADENGINE` warnings are harmless. Then check that only the declaration changed:

```bash
git diff --stat client/package.json client/package-lock.json
node -e "const l=require('./client/package-lock.json');console.log(l.packages['node_modules/@angular/cdk'].version, l.packages[''].dependencies['@angular/cdk'], l.packages['node_modules/@angular/cdk'].peer)"
```

Expected: two files with a handful of lines changed (`package.json` +1; `package-lock.json`: the root `dependencies` gains `@angular/cdk`, and the `node_modules/@angular/cdk` entry loses `"peer": true`), then `21.2.14 ^21.2.0 undefined`. If the lockfile diff touches any other package, stop: `git checkout client/package-lock.json` and investigate before going on. The version must not move.

- [ ] **Step 2: Write the failing specs**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Add two imports to the top import block (keep it alphabetical by module path: `@angular/cdk` goes first, `@angular/platform-browser` after `@angular/core/testing`):

```ts
import { CdkDropList } from '@angular/cdk/drag-drop';
```

```ts
import { By } from '@angular/platform-browser';
```

2. Add these helpers directly **after** the `move` helper that task 2 added:

```ts
function dropList(fixture: ComponentFixture<StudentFormPage>): CdkDropList {
    return fixture.debugElement.query(By.directive(CdkDropList)).injector.get(CdkDropList);
}

async function drop(
    fixture: ComponentFixture<StudentFormPage>,
    previousIndex: number,
    currentIndex: number,
): Promise<void> {
    fixture.debugElement
        .query(By.directive(CdkDropList))
        .triggerEventHandler('cdkDropListDropped', { previousIndex, currentIndex });
    await fixture.whenStable();
}
```

3. Add this `describe` block directly **after** the closing `});` of task 2's `describe('reordering the ranked list', …)`:

```ts
    describe('dragging the ranked list', () => {
        it('reorders a pick dropped at a new rank and submits the new order', async () => {
            //given
            const createSubmission = accepting();
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 2, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //when
            await drop(fixture, 2, 0);
            const order = reviewOrder(fixture);
            const announcement = textOf(fixture, '.review__announcement');
            await clickContinue(fixture);

            //then
            expect(order).toEqual(['wednesday-afternoon', 'sunday-afternoon', 'monday-evening']);
            expect(announcement).toMatch(/studentForm\.review\.movedTo$/);
            expect(createSubmission).toHaveBeenCalledWith(LINK_TOKEN, {
                nationalId: ROSTER_NATIONAL_ID,
                targetCount: 2,
                slotRequests: [
                    { slotId: 'wednesday-afternoon', sessionType: SessionType.single, constraint: null },
                    { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
                    { slotId: 'monday-evening', sessionType: SessionType.single, constraint: null },
                ],
            });
        });

        it('changes nothing when a pick is dropped where it started', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //when
            await drop(fixture, 1, 1);

            //then
            expect(reviewOrder(fixture)).toEqual(['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);
            expect(textOf(fixture, '.review__announcement')).toBe('');
        });

        it('offers a drag handle on every pick when there is more than one', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening', 'wednesday-afternoon']);

            //then
            const grips = [...page(fixture).querySelectorAll('.review__item .review__grip')];
            expect(grips.length).toBe(3);
            expect(grips.every(grip => grip.getAttribute('aria-hidden') === 'true')).toBe(true);
            expect(dropList(fixture).disabled).toBe(false);
            expect(dropList(fixture).lockAxis).toBe('y');
        });

        it('offers no drag handle for a single pick', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();

            //when
            await reachReview(fixture, 1, ['sunday-afternoon']);

            //then
            expect(page(fixture).querySelector('.review__grip')).toBeNull();
            expect(dropList(fixture).disabled).toBe(true);
        });

        it('locks dragging while the list is being sent', async () => {
            //given
            const createSubmission: SubmitCommand = vi.fn(() => new Subject<void>());
            provideOpenLinkSubmitting(identifyingAs(COHEN_STUDENT), createSubmission, accepting());
            const fixture = await renderPage();
            await reachReview(fixture, 1, ['sunday-afternoon', 'monday-evening']);

            //when
            await clickContinue(fixture);
            await drop(fixture, 1, 0);

            //then
            expect(dropList(fixture).disabled).toBe(true);
            expect(reviewOrder(fixture)).toEqual(['sunday-afternoon', 'monday-evening']);
        });
    });
```

`locks dragging while the list is being sent` fires the drop event even though the list is disabled. That proves the store guard as well as the CDK lock: a drop that slips through still changes nothing.

- [ ] **Step 3: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. The five new specs fail, the first four because `fixture.debugElement.query(By.directive(CdkDropList))` returns `null` (`Cannot read properties of null (reading 'injector')` / `(reading 'triggerEventHandler')`). The fifth fails the same way. Every other spec still passes.

- [ ] **Step 4: Make the list draggable**

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts`:

1. Add this import as the first line of the file:

```ts
import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';
```

2. Change

```ts
    imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent],
```

to

```ts
    imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent, CdkDropList, CdkDrag, CdkDragHandle],
```

3. Add this method directly **before** `protected moveUp(item: ReviewItem): void {`:

```ts
    protected onDropped(drop: CdkDragDrop<readonly ReviewItem[]>): void {
        const item = this.items()[drop.previousIndex];

        if (!item || drop.previousIndex === drop.currentIndex) {
            return;
        }

        this.moved.emit({ slotId: item.slotId, toIndex: drop.currentIndex });
    }
```

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.html`:

1. Change

```html
        <ol class="review__list">
```

to

```html
        <ol
            class="review__list"
            cdkDropList
            cdkDropListLockAxis="y"
            [cdkDropListData]="items()"
            [cdkDropListDisabled]="!canReorder() || isSubmitting()"
            (cdkDropListDropped)="onDropped($event)">
```

2. Change

```html
                <li
                    class="review__item"
                    [class.review__item--preferred]="item.isPreferred"
                    [attr.data-pick-id]="item.slotId">
                    <span class="review__rank" aria-hidden="true">{{ item.rank }}</span>
```

to

```html
                <li
                    class="review__item"
                    cdkDrag
                    cdkDragPreviewContainer="parent"
                    [class.review__item--preferred]="item.isPreferred"
                    [attr.data-pick-id]="item.slotId">
                    @if (canReorder()) {
                        <span class="review__grip" cdkDragHandle aria-hidden="true">
                            <i class="pi pi-bars"></i>
                        </span>
                    }
                    <span class="review__rank" aria-hidden="true">{{ item.rank }}</span>
```

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss`:

1. Directly **after** the `.review__item--preferred { … }` rule, add:

```scss
.review__item.cdk-drag-preview {
    border-color: var(--p-sky-500);
    box-shadow: var(--app-shadow-lift);
}

.review__item.cdk-drag-placeholder {
    opacity: 0.4;
}

@media (prefers-reduced-motion: no-preference) {
    .review__item.cdk-drag-animating,
    .review__list.cdk-drop-list-dragging .review__item:not(.cdk-drag-placeholder) {
        transition: transform 200ms cubic-bezier(0, 0, 0.2, 1);
    }
}

.review__grip {
    display: flex;
    flex: none;
    align-self: center;
    align-items: center;
    justify-content: center;
    width: 2.5rem;
    height: 2.5rem;
    margin-block: -0.4rem;
    margin-inline: -0.5rem -0.25rem;
    color: var(--app-text-muted);
    cursor: grab;
    touch-action: none;
}

.review__grip:active {
    cursor: grabbing;
}
```

Notes:
- `cdkDragPreviewContainer="parent"` keeps the floating preview inside the `<ol>`. It inherits the page's `dir`, the component's emulated-encapsulation attributes (the clone keeps them) and the tokens. Only `.cdk-drag-preview` needs extra styling.
- `cdkDropListData` is not read by `onDropped`, which uses `items()[previousIndex]`. It is bound so that CDK's own event carries the list being reordered, for debugging.
- The `@if` around the handle is safe: `CdkDragHandle` registers itself with the enclosing `CdkDrag` through DI when it is created, so a handle that appears later is picked up.

- [ ] **Step 5: Reword the hint (both files, same commit)**

In `client\public\i18n\en.json`, change

```json
      "reorderHint": "Use the arrows to move a pick up or down.",
```

to

```json
      "reorderHint": "Drag a pick by its handle, or use the arrows, to change its rank.",
```

In `client\public\i18n\he.json`, change

```json
      "reorderHint": "השתמשו בחיצים כדי להזיז בחירה למעלה או למטה.",
```

to

```json
      "reorderHint": "גררו בחירה בעזרת הידית, או השתמשו בחיצים, כדי לשנות את הדירוג שלה.",
```

- [ ] **Step 6: Run the specs and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the five in `dragging the ranked list` and all 13 from task 2.

If the `drop(…)` helper does not reach `onDropped` (the order is unchanged in the first spec), the Angular version's `triggerEventHandler` is not dispatching to directive outputs. Replace the helper body with `dropList(fixture).dropped.emit({ previousIndex, currentIndex } as CdkDragDrop<unknown>); await fixture.whenStable();` (import `CdkDragDrop` next to `CdkDropList`) and re-run.

Run (in `client\`): `npm run build`
Expected: builds clean, no new warnings, no budget warning. The student route is lazy-loaded, so the drag-drop code belongs in the student-form lazy chunk. The `Initial total` line should be within a few kB of the task-2 build. If it jumped by tens of kB, something imports `@angular/cdk/drag-drop` outside the feature, so find and fix that import.

- [ ] **Step 7: Commit**

```bash
git add client/package.json client/package-lock.json client/src/app/features/student-form/ui/components/review-step/review-step.component.ts client/src/app/features/student-form/ui/components/review-step/review-step.component.html client/src/app/features/student-form/ui/components/review-step/review-step.component.scss client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(student-form): drag picks by a handle to reorder the ranked list

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
