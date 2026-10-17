# Task 5 of 7: Weekly prep on a phone, with a sticky publish bar

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires task 4 committed. Steps 6 and 7 need the Browser pane tools.

**Files:**
- Modify: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html` / `.scss`
- Test: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.spec.ts` (new)

**Interfaces:**
- Consumes:
  - From `WeekSchedulesStore`: `publicationState()`, `canPublish()`, `selectedTeacherId()`, `selectedWeekStart()`, `slots()`, `toggleSlot(slot)`, `isMutating()`, `unavailableCount()`, `canChooseTeacher()`, `ownTeacherName()`.
  - From task 4: the grid's phone sizing (`min-height: 48px` cells).
  - From task 3: the phone page-header pattern.
- Produces: `.weekly-prep__publish-link`, `.weekly-prep__dock` / `.weekly-prep__dock-link`, and `.slot-cell__label`. Task 7 measures them.

**Why:** frame 03 and rules 5 and 6:
- "cells 48px tall with no "Open" text (the legend says it). Only "חסום" is labelled."
- "Publish moves to a sticky bottom bar, full width."

## The phone layout, from frame 03

| Piece | At ≤ 639px |
|-------|-----------|
| `.weekly-prep__header` | `flex-direction: column; align-items: stretch; gap: 0.75rem`. The state tag stays in `.weekly-prep__publish`, under the subtitle |
| `.weekly-prep__publish-link` (header) | `display: none` |
| `.weekly-prep__selectors` | `flex-direction: column; align-items: stretch; gap: 1rem`. Each `.field` and its `p-select` / `app-locked-field` is `inline-size: 100%` |
| `.weekly-prep__card` | `padding: 0.75rem 0.625rem` |
| `.slot-cell` | `min-height: 48px; border-radius: 7px; font-size: 0.65625rem; font-weight: 500` |
| Open cell's `.slot-cell__label` | visually hidden: `position: absolute; inline-size: 1px; block-size: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap` (the `students__sr-only` recipe; README decision 6) |
| `.weekly-prep__footer` | `flex-direction: column; align-items: flex-start; gap: 0.5rem` |
| `.weekly-prep__count` | `font-size: 0.8125rem` (rule 7's 13px floor; it's body text, unlike the legend) |
| `.weekly-prep__dock` | shown only here: `position: sticky; inset-block-end: 0; margin-inline: -1rem; margin-block: 1rem -1.5rem; padding: 0.75rem 1rem 1rem; background: var(--app-bg-card); border-block-start: 1px solid var(--app-border)`. The negative margins cancel `.shell__main`'s phone padding from task 1, so the bar spans the screen |
| `.weekly-prep__dock-link` | `inline-size: 100%; min-block-size: 2.75rem; justify-content: center` |

- [ ] **Step 1: Write the failing spec**

Create `weekly-prep.page.spec.ts` in the style of `publications-dashboard.page.spec.ts`. Use a `fakeStore(overrides)` that returns signals for every store member the template reads:
- `publicationState: signal(PublicationState.draft)`, `canPublish: signal(true)`
- `selectedTeacherId: signal('teacher-cohen')`, `selectedWeekStart: signal('2026-10-18')`
- `canChooseTeacher: signal(true)`, `teachers: signal([...])`, `ownTeacherName: signal(undefined)`
- `weekOptions: signal([...])`, `isLoading: signal(false)`, `loadError: signal(undefined)`
- `hasSelection: signal(true)`, `isNotCreatedYet: signal(false)`, `weekSchedule: signal({...})`
- `slots: signal([one open and one unavailable Sunday slot])`, `windowTimes: signal({})`
- `isMutating: signal(false)`, `unavailableCount: signal(1)`
- no-op `toggleSlot`, `selectTeacher`, `selectWeek`

Provide it in place of `WeekSchedulesStore`. Read the exact member names from `week-schedules.store.ts` before writing the fake.

```typescript
it('repeats the publish link in the bottom bar', async () => {
    //given
    const page = await render(fakeStore({}));

    //when
    const header = page.querySelector('a.weekly-prep__publish-link');
    const dock = page.querySelector('.weekly-prep__dock a.weekly-prep__dock-link');

    //then
    expect(dock?.getAttribute('href')).toBe('/publications?teacherId=teacher-cohen&week=2026-10-18&publish=1');
    expect(dock?.getAttribute('href')).toBe(header?.getAttribute('href'));
});

it('has no bottom bar when the week cannot be published', async () => {
    //given
    const store = fakeStore({ canPublish: signal(false) });

    //when
    const page = await render(store);

    //then
    expect(page.querySelector('.weekly-prep__dock')).toBeNull();
});

it('keeps the open label in the cell for screen readers', async () => {
    //given
    const page = await render(fakeStore({}));

    //when
    const labels = [...page.querySelectorAll('.slot-cell .slot-cell__label')].map((label) => label.textContent?.trim());

    //then
    expect(labels).toEqual(['weekSchedules.cellOpen', 'weekSchedules.cellUnavailable']);
});
```

- [ ] **Step 2: Run the spec to see it fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/week-schedules/ui/pages/weekly-prep/weekly-prep.page.spec.ts`
Expected: FAIL; there's no `.weekly-prep__publish-link`, `.weekly-prep__dock` or `.slot-cell__label`.

- [ ] **Step 3: Change the template**

- The header's publish `<a pButton>` gets `class="weekly-prep__publish-link"`.
- The slot button's content becomes `<span class="slot-cell__label">{{ ... }}</span>`, with the same translate expression as today.
- As the last child of `.weekly-prep`, add the dock:
  - `@if (store.publicationState() && store.canPublish()) { <div class="weekly-prep__dock">...</div> }`
  - Inside, `<a pButton class="weekly-prep__dock-link">` with the same `label`, `routerLink` and `queryParams` as the header link.

Both links are always in the DOM; CSS shows one (README decision 2).

- [ ] **Step 4: Run the spec to see it pass**

Run the step 2 command.
Expected: PASS, 3 tests.

- [ ] **Step 5: Style the phone layout**

In `weekly-prep.page.scss`:
- At every width, hide `.weekly-prep__dock` (`display: none`).
- At the end of the file, add a `@media screen and (max-width: 639px)` block with the table's values. The dock gets `display: block` there.
- Reach PrimeNG internals only through `:host ::ng-deep`, for the select's `inline-size`.

- [ ] **Step 6: Measure the page (GREEN)**

At 390 × 844 on `/week-schedules`, as the Administrator, in Hebrew. Pick a teacher and a week with a Draft Week Schedule. Run the README's phone measurement, plus:

```javascript
const box = (element) => element.getBoundingClientRect();
const dock = document.querySelector('.weekly-prep__dock');
const openCell = document.querySelector('.slot-cell:not(.slot-cell--unavailable)');
const blockedCell = document.querySelector('.slot-cell--unavailable');
const selects = [...document.querySelectorAll('.weekly-prep__selectors .field')].map(box);
({
    headerLinkShown: getComputedStyle(document.querySelector('.weekly-prep__publish-link')).display !== 'none',
    dockAtViewportBottom: Math.round(box(dock).bottom) === innerHeight,
    dockLinkSize: [Math.round(box(dock.querySelector('a')).width), Math.round(box(dock.querySelector('a')).height)],
    selectorsStacked: selects.length === 2 && selects[1].top > selects[0].bottom,
    selectorWidth: Math.round(selects[0].width),
    openCellHeight: Math.round(box(openCell).height),
    openLabelVisible: box(openCell.querySelector('.slot-cell__label')).width > 1,
    blockedLabelVisible: box(blockedCell.querySelector('.slot-cell__label')).width > 1,
    openCellName: openCell.textContent.trim(),
});
```

Expected:
- `pageOverflows: false`, `smallText: []`, no `weekly-prep__` entry in `shortTargets`
- `headerLinkShown: false`, `dockAtViewportBottom: true`
- `dockLinkSize` about `[358, 44]`
- `selectorsStacked: true`, `selectorWidth` about 358
- `openCellHeight: 48`, `openLabelVisible: false`, `blockedLabelVisible: true`
- `openCellName: "פתוח"` (still in the button's text for screen readers)

Then:
1. Tap an open cell. Expected: it turns "חסום" and the count in the footer goes up by one. Tap it again to restore.
2. Scroll to the bottom (`window.scrollTo(0, document.body.scrollHeight)`). Expected: the dock is still at the viewport bottom and covers no grid row. The last row's bottom is at or above the dock's top.
3. Tap "פרסום שבוע..." in the dock. Expected: `/publications` opens with the publish dialog, as the header link does today. Close it with Escape.
4. Switch to English and repeat the measurement. Expected: the same values, with "Open" as `openCellName`.

- [ ] **Step 7: A Teacher-role User (Review Focus 2)**

If the dev database has a Teacher-role User, sign in as them at 390 × 844 (otherwise skip this step and note it for the PR). Expected:
- the drawer lists three links;
- Weekly prep shows the locked teacher field full width;
- `document.querySelector('.weekly-prep__dock')` is `null` while the Teacher can't publish.

Sign back in as the Administrator.

- [ ] **Step 8: Desktop is unchanged**

At 1280 × 900, reload. Expected:
- the publish link sits in the header next to the tag, and there's no dock;
- the cells are 64px and show "פתוח" / "חסום";
- the selectors sit in a row.

Then `resize_window {preset: "desktop"}`.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/week-schedules
git commit -m "feat(admin-mobile): weekly prep stacks on a phone with a sticky publish bar"
```

End the commit message with the attribution trailer from the session's instructions.
