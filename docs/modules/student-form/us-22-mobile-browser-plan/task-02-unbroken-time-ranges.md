# Task 2 of 4: Time ranges stay on one line (D2)

> Part of [US-22: Student Form Fully Usable on a Mobile Browser](README.md). Requires task 1 committed. Work on branch `23-us-22-mobile-browser`.

**Files:**
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss` (`.review__time`)
- Modify: `client\src\app\features\student-form\ui\components\pick-sheet\pick-sheet.component.scss` (`.pick-sheet__time`)

**Interfaces:**
- Consumes: the existing `<span class="review__time" dir="ltr">` (review row) and `<span class="pick-sheet__time" dir="ltr">` (pick-sheet header). Neither template changes.
- Produces: both spans render as a single line box. Task 4 Step 5.4 asserts `getClientRects().length === 1` for every `.review__time` at 320px in English (Step 4.5 repeats it in Hebrew), the case where the planning audit measured 2.

**Why:** README defect D2 and decision 3. At 320px in English, a review row with the grip, the rank, the two move arrows and a Double + constraint leaves the details column narrower than the slot name plus the range. `Sunday · Afternoon 15:00–18:00` then wraps at the en dash, so `15:00–` ends one line and `18:00` starts the next. A range is one unit. With `nowrap` the row wraps at the spaces in the slot name, and the whole range moves to the next line when it does not fit. The pick-sheet header did not break in the audit (the time sits in its own flex item), but it is the same label. One rule on both keeps them consistent if the title grows. `.slot-chip__time` is deliberately left alone (decision 3).

**Test approach:** jsdom does no layout, so a page spec cannot see a line break. There is no failing spec in this task. The red state is the planning-time measurement in the README (D2), and the green state is pinned in task 4 Step 5.4 against the real browser. Step 2 below lets you check it immediately if a smoke stack happens to be running.

- [ ] **Step 1: Add `white-space: nowrap` to both time labels**

In `client\src\app\features\student-form\ui\components\review-step\review-step.component.scss`, change

```scss
.review__time {
    margin-inline-start: 0.25rem;
    font-size: 0.72rem;
    font-weight: 400;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}
```

to

```scss
.review__time {
    margin-inline-start: 0.25rem;
    font-size: 0.72rem;
    font-weight: 400;
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
    color: var(--app-text-muted);
}
```

In `client\src\app\features\student-form\ui\components\pick-sheet\pick-sheet.component.scss`, change

```scss
.pick-sheet__time {
    margin-inline-start: auto;
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-muted);
}
```

to

```scss
.pick-sheet__time {
    margin-inline-start: auto;
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
    color: var(--app-text-muted);
}
```

- [ ] **Step 2: Run the specs and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec. Nothing in the specs depends on these styles.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings and no budget warning (two declarations add a few bytes to the lazy student-form chunk).

Optional, only if a smoke stack from an earlier session is still running (otherwise task 4 Step 5.4 covers it): in the browser pane at 320×568 in English, on a review whose first pick is `Sunday · Afternoon` as a Double with a long constraint:

```js
[...document.querySelectorAll('.review__time')].map(time => time.getClientRects().length)
```

→ every entry `1`.

- [ ] **Step 3: Commit**

```bash
git add client/src/app/features/student-form/ui/components/review-step/review-step.component.scss client/src/app/features/student-form/ui/components/pick-sheet/pick-sheet.component.scss
git commit -m "fix(student-form): keep a slot's time range on one line on narrow phones

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
