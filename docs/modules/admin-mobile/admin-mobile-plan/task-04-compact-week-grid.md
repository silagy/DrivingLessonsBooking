# Task 4 of 7: The week grid keeps all six days on a phone

> Part of [Admin on a phone](README.md). Work on branch `admin-mobile`. Requires task 3 committed. Step 6 needs the Browser pane tools.

**Files:**
- Modify: `client\src\app\shared\dates\window-times.ts`
- Modify: `client\src\app\shared\components\week-grid\week-grid.component.html` / `.scss` / `.ts`
- Modify: `client\src\app\shared\components\week-grid-legend\week-grid-legend.component.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Test: `client\src\app\shared\dates\window-times.spec.ts`
- Test: `client\src\app\shared\components\week-grid\week-grid.component.spec.ts`

**Interfaces:**
- Consumes: `windowTimesOf()` output, a `Partial<Record<SlotWindow, string>>` of `HH:MM-HH:MM` labels.
- Produces:
  - `compactWindowTime(time: string): string` in `window-times.ts`.
  - New grid classes: `.week-grid__day-name--long` / `--short` and `.week-grid__window-time--long` / `--compact`.
  - The grid's phone sizing: label column `3.25rem`, `gap: 5px`, cells `min-height: 48px`. Task 5 sizes its slot buttons to match.

**Why:** frame 03 and rule 5: "Week grid keeps all 6 days on screen: short day letters, cells 48px tall with no "Open" text (the legend says it). Only "חסום" is labelled. No sideways scrolling." This task is the grid's part; the cell label is task 5's. The grid is shared, so the Publications dashboard gets the same compact grid (Review Focus 5).

- [ ] **Step 1: Write the failing tests**

In `window-times.spec.ts`, add a `describe('compactWindowTime')`:

```typescript
it('drops whole-hour minutes', () => {
    //given
    const time = '07:00-12:00';

    //when
    const compact = compactWindowTime(time);

    //then
    expect(compact).toBe('07-12');
});

it('keeps minutes that are not on the hour', () => {
    //given
    const time = '07:30-12:00';

    //when
    const compact = compactWindowTime(time);

    //then
    expect(compact).toBe('07:30-12');
});
```

In `week-grid.component.spec.ts`, give the host `[windowTimes]="{ morning: '07:00-12:00' }"` (use the `SlotWindow.morning` value). Reuse the existing `TestBed` setup, then add:

```typescript
it('renders a short name for each day', async () => {
    //given
    const grid = await render();

    //when
    const names = [...grid.querySelectorAll('.week-grid__day-name--short')].map((name) => name.textContent?.trim());

    //then
    expect(names).toEqual([
        'weekGrid.daysShort.sunday', 'weekGrid.daysShort.monday', 'weekGrid.daysShort.tuesday',
        'weekGrid.daysShort.wednesday', 'weekGrid.daysShort.thursday', 'weekGrid.daysShort.friday',
    ]);
});

it('renders a compact time next to each window time', async () => {
    //given
    const grid = await render();

    //when
    const compact = grid.querySelector('.week-grid__window-time--compact')?.textContent?.trim();

    //then
    expect(compact).toBe('07-12');
});
```

Move the existing test's setup into a `render()` helper, so all three tests share it.

- [ ] **Step 2: Run the tests to see them fail**

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/shared/**/*.spec.ts"`
Expected: FAIL; `compactWindowTime` isn't exported, and there's no `--short` span.

- [ ] **Step 3: Implement**

1. `compactWindowTime(time: string): string`: split on `-`, drop a trailing `:00` from each end, join with `-`.
2. Add `weekGrid.daysShort` to both translation files:

| Day | he | en |
|-----|----|----|
| sunday | `א׳` | `Sun` |
| monday | `ב׳` | `Mon` |
| tuesday | `ג׳` | `Tue` |
| wednesday | `ד׳` | `Wed` |
| thursday | `ה׳` | `Thu` |
| friday | `ו׳` | `Fri` |

   The Hebrew `׳` is U+05F3 (geresh), as in the design. It isn't a long dash or an ellipsis, so `source-text.spec.ts` allows it.
3. In `week-grid.component.ts`, expose `protected readonly compactTime = compactWindowTime;`.
4. In the template:
   - Today's day-name span becomes `week-grid__day-name week-grid__day-name--long`.
   - Add a sibling `week-grid__day-name week-grid__day-name--short` with `'weekGrid.daysShort.' + day | transloco`.
   - Today's window-time span becomes `week-grid__window-time week-grid__window-time--long`.
   - Add a sibling `week-grid__window-time week-grid__window-time--compact` with `<bdi>{{ compactTime(time) }}</bdi>`.

- [ ] **Step 4: Run the tests to see them pass**

Run the step 2 command.
Expected: PASS, including the existing day-first date test.

- [ ] **Step 5: Style the phone grid**

In `week-grid.component.scss`:
- At every width, hide `.week-grid__day-name--short` and `.week-grid__window-time--compact`.
- Add `@media screen and (max-width: 639px)`:

| Selector | Value |
|----------|-------|
| `.week-grid` | `grid-template-columns: 3.25rem repeat(6, minmax(0, 1fr)); gap: 5px` |
| `.week-grid__day-head` | `padding-block-end: 4px; font-size: 0.8125rem` |
| `.week-grid__day-date` | `font-size: 0.6875rem` |
| `.week-grid__window-label` | `padding-inline-end: 0` |
| `.week-grid__window-name` | `font-size: 0.8125rem` |
| `.week-grid__window-time` | `font-size: 0.65625rem` |
| `.week-grid__cell` | `min-height: 48px; min-inline-size: 0` |
| `--long` variants | `display: none` |
| `--short` / `--compact` variants | shown (`display: inline`) |

In `week-grid-legend.component.scss`, add `@media screen and (max-width: 639px) { .week-grid-legend { flex-wrap: wrap; gap: 0.5rem 1rem; } }`.

- [ ] **Step 6: Measure both grids (GREEN), at 390 and 360 (Review Focus 4 and 5)**

For `/week-schedules` (choose a teacher and a week that has a Week Schedule) and `/publications` (a week with a Publication), at 390 × 844 and at 360 × 780, in Hebrew and in English, run the README's phone measurement, plus:

```javascript
const box = (element) => element.getBoundingClientRect();
const grid = document.querySelector('.week-grid');
const cells = [...grid.querySelectorAll('.week-grid__cell')].map(box);
const heads = [...grid.querySelectorAll('.week-grid__day-head')].map(box);
const sunday = heads[0];
const friday = heads[5];
({
    gridOverflows: grid.scrollWidth > grid.clientWidth,
    narrowestCell: Math.round(Math.min(...cells.map((cell) => cell.width))),
    shortestCell: Math.round(Math.min(...cells.map((cell) => cell.height))),
    shortDayShown: getComputedStyle(grid.querySelector('.week-grid__day-name--short')).display !== 'none',
    compactTime: grid.querySelector('.week-grid__window-time--compact')?.textContent?.trim(),
    sundayAtInlineStart: document.documentElement.dir === 'rtl' ? sunday.left > friday.left : sunday.left < friday.left,
    countsFit: [...grid.querySelectorAll('.slot-count')].every((cell) => cell.scrollWidth <= cell.clientWidth),
});
```

Expected:
- `pageOverflows: false`, `gridOverflows: false`, `shortDayShown: true`, `compactTime: "07-12"` (or the dev data's first window), `sundayAtInlineStart: true`, `countsFit: true` (no `.slot-count` on Weekly prep, so it's trivially true there).
- `narrowestCell` at least 40 at 390 and at least 36 at 360.
- `shortestCell` 48 on Weekly prep after task 5. Until task 5 lands, Weekly prep's own 64px button keeps the cells at 64. The dashboard's `.slot-count` keeps its own 62px.

If `countsFit` is `false` on the dashboard, shrink `.slot-count__value` inside the dashboard's `slot-count-cell.component.scss` with its own `max-width: 639px` block (`font-size: 1rem`) and measure again. Note it for the PR.

At 1280 × 900, reload both pages. Expected: full day names, `07:00-12:00`, the 118px label column. Then `resize_window {preset: "desktop"}`.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/shared client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(admin-mobile): the week grid keeps six days on a phone with short day names"
```

If step 6 changed `slot-count-cell.component.scss`, add it to the same commit. End the commit message with the attribution trailer from the session's instructions.
