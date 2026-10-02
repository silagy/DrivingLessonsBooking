# Task 1 of 4: A typed constraint keeps its own reading direction (D1, TDD)

> Part of [US-22: Student Form Fully Usable on a Mobile Browser](README.md). Work on branch `23-us-22-mobile-browser`.

**Files:**
- Modify: `client\src\app\features\student-form\ui\components\pick-sheet\pick-sheet.component.html` (one attribute on the `<textarea>`)
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.html` (one attribute on the `<q>`)
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts` (two specs)

**Interfaces:**
- Consumes: the existing page-spec helpers `provideOpenLinkIdentifying`, `identifyingAs`, `COHEN_STUDENT`, `renderPage`, `reachSlots`, `tapChip`, `typeConstraint`, `press`, `clickContinue`, `page`. The existing DOM hooks `#pick-constraint` (pick-sheet textarea) and `.review__constraint` (review `<q>`).
- Produces: `dir="auto"` on `#pick-constraint` and on every `.review__constraint`. Task 4 Steps 4.3–4.4 (English on the Hebrew page) and 5.2, 5.5 (Hebrew on the English page) check the rendered result in the browser.

**Why:** README defect D1 and decision 2. Students type constraints in Hebrew or English whatever the page language ("רק אחרי 16:00", "pick me up from work"). Without its own direction, the `<q>` takes the page's direction, so the quote marks and word order come out scrambled when the scripts differ. `dir="auto"` gives each element the direction of its first strong character, and the UA stylesheet isolates any element with a `dir` attribute from the sentence around it. Nothing else about the constraint changes: the stored text, `maxlength`, trimming (`toSlotConstraint`) and the request are untouched.

- [ ] **Step 1: Write the failing specs**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`:

1. Inside `describe('picking slots', …)`, directly **after** the closing `});` of `it('caps the constraint at 200 characters', …)`, add:

```ts
        it('lets the constraint follow the direction it is typed in', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);

            //when
            await tapChip(fixture, 'sunday-afternoon');

            //then
            expect(page(fixture).querySelector('#pick-constraint')!.getAttribute('dir')).toBe('auto');
        });
```

2. Inside `describe('review and submit', …)`, directly **after** the closing `});` of `it('submits the ranked list with each pick\'s session type and constraint', …)`, add:

```ts
        it('keeps a typed constraint in its own reading direction on the review', async () => {
            //given
            provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT));
            const fixture = await renderPage();
            await reachSlots(fixture);
            await tapChip(fixture, 'sunday-afternoon');
            await typeConstraint(fixture, 'only after 16:00');
            await press(fixture, '.pick-sheet__save button');

            //when
            await clickContinue(fixture);

            //then
            const constraint = page(fixture).querySelector('.review__constraint')!;
            expect(constraint.textContent?.trim()).toBe('only after 16:00');
            expect(constraint.getAttribute('dir')).toBe('auto');
        });
```

Fixture facts the specs rely on (already in the file): `COHEN_STUDENT` has every slot of the week open, with ids `{day}-{window}`. `reachSlots` identifies with `ROSTER_NATIONAL_ID`, continues through details and target (target 1) and lands on the slots step. One pick meets target 1, so `clickContinue` on the slots step opens the review.

- [ ] **Step 2: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. Exactly the two new specs fail on the `dir` assertion, with `expected null to be 'auto'`. The review spec's `textContent` assertion passes. Every other spec still passes.

- [ ] **Step 3: Add `dir="auto"` to the textarea**

In `client\src\app\features\student-form\ui\components\pick-sheet\pick-sheet.component.html`, change

```html
        <textarea
            pTextarea
            id="pick-constraint"
            class="pick-sheet__constraint"
            rows="2"
```

to

```html
        <textarea
            pTextarea
            id="pick-constraint"
            class="pick-sheet__constraint"
            dir="auto"
            rows="2"
```

- [ ] **Step 4: Add `dir="auto"` to the review's constraint**

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.html`, change

```html
                                <q class="review__constraint">{{ item.constraint }}</q>
```

to

```html
                                <q class="review__constraint" dir="auto">{{ item.constraint }}</q>
```

- [ ] **Step 5: Run the specs and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the two new ones.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 6: Commit**

```bash
git add client/src/app/features/student-form/ui/components/pick-sheet/pick-sheet.component.html client/src/app/features/student-form/ui/components/review-step/review-step.component.html client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts
git commit -m "fix(student-form): show a typed constraint in its own reading direction

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
