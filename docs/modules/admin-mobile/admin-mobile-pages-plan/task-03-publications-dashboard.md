# Task 3 of 4: The Publications dashboard on a phone

> Part of [Students, Users and Publications on a phone](README.md). Requires task 2 committed. Steps 5 to 7 need the Browser pane tools.

**Files:**
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` / `.scss` / `.spec.ts`
- Modify: `client\src\app\features\publications\ui\components\publication-stat-chip\publication-stat-chip.component.scss`
- Modify: `client\src\app\features\publications\ui\components\share-link-box\share-link-box.component.scss`

**Interfaces:**
- New in the template: `.dashboard__window-line` around the window status, holding a phone-only Extend `p-button` (`styleClass="dashboard__extend-phone"`, class `dashboard__phone-only` on the host) when the state is Open and `store.canManageLifecycle()`. The Extend in `.dashboard__actions` gets `class="dashboard__wide-only"`.
- `dashboard__card--grid` on the two grid sections.

**Why:** frames 08, 08b and 08 EN 360, rules 15 to 17.

- [ ] **Step 1: Write the failing spec**

In `publications-dashboard.page.spec.ts`, for an Open Publication with lifecycle rights:

```typescript
it('offers Extend deadline next to the window status for a phone', async () => {
    //given an Open Publication the Administrator manages
    //when
    const extend = page.querySelector('.dashboard__window-line .dashboard__extend-phone .p-button-label');
    //then
    expect(extend?.textContent?.trim()).toBe('publications.extendWindow');
});

it('extends the window from the phone button', async () => { ... });   // click → the same dialog/handler as onExtend()
it('leaves the phone Extend out for a Teacher-role User', async () => { ... }); // canManageLifecycle false → null
```

The existing action-label tests keep reading `.dashboard__actions`, so they still see one Extend.

Run the spec with `--include`. Expected: FAIL.

- [ ] **Step 2: Add the markup**

- Wrap the status part in `div.dashboard__window-line` (inside `.dashboard__subtitle`, after the week select), add the phone-only Extend there.
- Mark the actions' Extend `dashboard__wide-only`.
- Add `dashboard__card--grid` to the Open and Closed grid sections.

Run the spec. Expected: PASS.

- [ ] **Step 3: Style the header**

`.dashboard__phone-only { display: none }`. At the end of the page's SCSS, `@media screen and (max-width: 639px)`:
- `.dashboard__header` column, stretch, `gap: 1rem`; `.dashboard__heading` column, `gap: 0.5rem`.
- `.dashboard__title-row`: `display: grid; grid-template-columns: auto minmax(0, 1fr); column-gap: 0.625rem; align-items: center`. `.dashboard__title`: `display: grid; grid-column: 1 / -1; grid-row: 1 / span 2; grid-template-columns: subgrid; grid-template-rows: subgrid; align-items: center`. Teacher select `grid-column: 1 / -1; grid-row: 1; min-block-size: 2.75rem; margin-block-end: 0.375rem`, label 1.5rem. `.dashboard__week-title` `grid-column: 1; grid-row: 2; font-size: 1.0625rem; color: var(--app-text-secondary)` (frame 08's `.wn`). The state tag `grid-column: 2; grid-row: 2; justify-self: start`.
- `.dashboard__subtitle` column, `align-items: stretch; gap: 0.5rem`. The week select a pill: `align-self: flex-start; min-block-size: 2.75rem; padding-inline: 0.875rem; border: 1.5px solid var(--app-border); border-radius: 999px; background: var(--app-bg-card)`, label 0.875rem ink.
- `.dashboard__window-line` `display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 0 0.625rem; min-block-size: 2.75rem`; the status part's `::before` dot `content: none`; status 0.875rem.
- `.dashboard__wide-only` `display: none`; `.dashboard__phone-only` `display: inline-flex`; the phone Extend `block-size: 2.75rem; padding-inline: 0.25rem`.
- `.dashboard__actions` `display: grid; grid-template-columns: 1fr 1fr; gap: 0.625rem`; `:empty` `display: none`; a lone action `grid-column: 1 / -1`; buttons `inline-size: 100%; block-size: 2.75rem; white-space: nowrap`.

- [ ] **Step 4: Style the body**

In the same block:
- `.dashboard__stats` `display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.5rem`; `.dashboard__stamp` `grid-column: 1 / -1; margin-inline-start: 0; font-size: 0.8125rem`.
- `.dashboard__card` `padding: 0.875rem`; `.dashboard__card--grid` `padding: 0.75rem 0.625rem`; `:host ::ng-deep .dashboard__card--grid .week-grid` `grid-template-columns: 2.875rem repeat(6, minmax(0, 1fr)); gap: 4px`.
- `.dashboard__empty` column, stretch; its link 100% wide, 44px.

`publication-stat-chip.component.scss`, at ≤ 639px: `.stat-chip` `flex-direction: column; align-items: flex-start; gap: 2px; padding: 0.625rem 0.75rem; block-size: 100%`; value 1.375rem; label 0.8125rem, `line-height: 1.3`.

`share-link-box.component.scss`, at ≤ 639px: `.share-link` `padding: 0.875rem`; title and hint 0.8125rem; `.share-link__row` `align-items: stretch`; value `white-space: normal; overflow-wrap: anywhere; min-block-size: 2.75rem; display: flex; align-items: center; font-size: 0.875rem`; the copy button `:host ::ng-deep .share-link__row .p-button` `block-size: 2.75rem`.

- [ ] **Step 5: Measure every state (GREEN)**

At 390 × 844 in Hebrew on `/publications`, for an Open week, a Closed week, a Draft week and a week with no Publication (the dev data's week select, or the teacher select). The README's measurement passes in each, plus:

```javascript
const box = (selector) => document.querySelector(selector)?.getBoundingClientRect();
const week = box('.dashboard__week-title'), tag = box('app-publication-state-tag');
({
    tagNextToWeek: Math.abs(week.top - tag.top) < 12 && (document.documentElement.dir === 'rtl' ? tag.right <= week.left + 1 : tag.left >= week.right - 1),
    cellWidth: Math.round(box('.dashboard__card--grid .week-grid__cell')?.width ?? 0),
    windowNamesFit: [...document.querySelectorAll('.week-grid__window-name--short')].every((name) => name.scrollWidth <= name.clientWidth + 1),
    actions: [...document.querySelectorAll('.dashboard__actions .p-button')].filter((button) => button.offsetParent).map((button) => Math.round(button.getBoundingClientRect().width)),
});
```

Expected: `tagNextToWeek: true`; `cellWidth` at least 39 at 360 (rule 17); `windowNamesFit: true`; two half-width actions in Open and Closed, one full-width in Draft, none in Published.

- [ ] **Step 6: English and 360px**

Repeat step 5 in English and at 360 × 780. Expected: "Extend deadline", "Reopen window...", "Download Excel" on one line; no overflow.

- [ ] **Step 7: Actions and desktop**

1. Tap the phone Extend on an Open week. Expected: the extend dialog opens, as from the desktop button.
2. Copy the share link. Expected: the toast.
3. A Teacher-role User (if the dev data has one): no teacher select, the only-yours note, the week title and tag on one line.
4. Desktop at 1280: the header as today, one Extend, the stats in one row.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/publications
git commit -m "feat(admin-mobile): the publications dashboard fits a phone"
```

End the commit message with the attribution trailer from the session's instructions.
