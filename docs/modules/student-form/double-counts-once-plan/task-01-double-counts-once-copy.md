# Task 1 of 2: "A double counts once" on the target step and in the not-enough message

> Part of [Double Counts Once](README.md). Read its Decisions and Global Constraints first. Branch `customer-feedback-excel-and-target`.

**Files:**
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (`studentForm.target`, `studentForm.review`)
- Modify: `client\src\app\features\student-form\ui\components\target-step\target-step.component.html` / `.scss`
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` / `.html`
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`
- Docs: `docs\requirements.md` §7 step 5 and §8.1, `docs\modules\student-form\README.md` (slice 7 row)

**Interfaces:**
- Consumes: `ReviewItem.sessionType`, `SessionType.double`, `ReviewStepComponent.items()` and `missingPicks()`; spec helpers `reachTarget`, `clickContinue`, `tapChip`, `chooseDouble`, `press`, `reachReview`, `textOf`, `page`.
- Produces: keys `studentForm.target.doubleCountsOnce`, `studentForm.review.doubleCountsOnce`; `ReviewStepComponent.hasDoublePick` (protected computed `boolean`); classes `.target__note`, `.review__double-note`.

- [ ] **Step 1: Write the failing specs**

The page spec renders translation keys as text. In `describe('target step')`:

```typescript
it('tells the student a double lesson counts once toward the target', async () => {
    // provideOpenLinkIdentifying(identifyingAs(COHEN_STUDENT)); renderPage; reachTarget
    // expect(textOf(fixture, '.target__note')).toMatch(/studentForm\.target\.doubleCountsOnce$/)
});
```

In `describe('review and submit')`, add a local helper `reachReviewWithOneDouble(fixture, targetCount)`. It goes through `reachTarget`, presses `.target__increase` up to `targetCount`, calls `clickContinue`, then `tapChip('sunday-afternoon')`, `chooseDouble`, presses `.pick-sheet__save button`, and calls `clickContinue`. Then:

```typescript
it('reminds that a double counts as one pick when a short list holds one', async () => {
    // reachReviewWithOneDouble(fixture, 2)
    // .review__not-enough present; .review__double-note text matches /studentForm\.review\.doubleCountsOnce$/
});

it('leaves the double reminder out when a short list holds only single lessons', async () => {
    // reachReview(fixture, 3, ['sunday-afternoon'])
    // .review__not-enough present; .review__double-note is null
});

it('shows no not-enough message or double reminder when a double covers a target of one', async () => {
    // reachReviewWithOneDouble(fixture, 1)
    // .review__not-enough and .review__double-note are both null
});
```

- [ ] **Step 2: Run them to verify they fail**

Run (from `client\`): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts`
Expected: the first two new specs FAIL (`.target__note` and `.review__double-note` not found); the last two pass already.

- [ ] **Step 3: Implement**

- Both JSON files: add `doubleCountsOnce` under `studentForm.target` (after `body`) and under `studentForm.review` (after `notEnoughBody`), with the README Decision 2 and 3 wording.
- Target step: `<p class="target__note">{{ 'studentForm.target.doubleCountsOnce' | transloco }}</p>` after `.target__unit`. In the SCSS, `.target__note` uses `margin-block: 0.75rem 0`, `text-align: center`, `font-size: 0.8125rem`, `color: var(--app-text-muted)`.
- Review step: `protected readonly hasDoublePick = computed(() => this.items().some(item => item.sessionType === SessionType.double));`. Inside the not-enough `<span>`, after the body interpolation, add `@if (hasDoublePick()) { <span class="review__double-note">{{ 'studentForm.review.doubleCountsOnce' | transloco }}</span> }`.

- [ ] **Step 4: Run the spec, then the translations spec**

Run Step 2's command. Expected: PASS (all specs in the file).
Run `... ng.js test --watch=false --include src/app/core/translations.spec.ts`. Expected: PASS.

- [ ] **Step 5: The docs**

- `docs\requirements.md` §7 step 5 and §8.1: README Decision 5.
- `docs\modules\student-form\README.md` slices table: add `7. Double counts once ([plan](double-counts-once-plan/README.md))`, content "target-step note and review reminder that a Double counts as one lesson toward the target (customer feedback, October 2026)", no issue.

- [ ] **Step 6: Commit**

```bash
git add client/public/i18n/he.json client/public/i18n/en.json \
  client/src/app/features/student-form/ui/components/target-step \
  client/src/app/features/student-form/ui/components/review-step \
  client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts \
  docs/requirements.md docs/modules/student-form
git commit -m "feat(student-form): say a double lesson counts once toward the target, on the target step and when the list is short"
```

---

**Next:** [task-02-verification-and-pr.md](task-02-verification-and-pr.md)
