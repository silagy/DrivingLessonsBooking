# Task 2 of 5: Week picker in the dashboard subtitle (P2, TDD)

> Part of [#76: Publications Dashboard - Share Link While Open, Week Picker, Empty State](README.md). Sub-issue [#78](https://github.com/silagy/DrivingLessonsBooking/issues/78). Requires task 1 committed. Work on branch `76-dashboard-link-and-week`.

**Files:**
- Modify: `client\src\app\features\publications\domain\week-options.ts`
- Modify: `client\src\app\features\publications\domain\week-options.spec.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts` (after `weekOptions`)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` (subtitle)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`

**Interfaces:**
- Consumes: `buildWeekOptions(locale, today)`, `weekRangeLabel(weekStart: Date, locale)` and `WeekOption { weekStart: string; label: string }` (all in `week-options.ts`); `parseIsoDate` (already imported there); `PublicationsStore.selectWeek(weekStart: string)` (exists); task 1's `fakeStore`, `render` and `WEEK_LABEL` in the page spec.
- Produces: `withWeekOption(options: readonly WeekOption[], weekStart: string, locale: string): WeekOption[]` and `PublicationsStore.weekChoices: Signal<WeekOption[]>`.

**Why:** README P2 and decisions 2–3. The dashboard can only reach another week through a History link, so the current week, which usually has no publication, becomes a dead end.

**Run the tests** (PowerShell, from `client\`):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/publications/**/*.spec.ts"
```

- [ ] **Step 1: Write the failing domain tests**

In `week-options.spec.ts`, replace the import line with:

```typescript
import { buildWeekOptions, withWeekOption } from './week-options';
```

Append to the file:

```typescript
describe('withWeekOption', () => {
    it('keeps the options as they are when the week is already offered', () => {
        //given
        const options = buildWeekOptions('en-IL', new Date(2026, 9, 7));

        //when
        const choices = withWeekOption(options, '2026-10-11', 'en-IL');

        //then
        expect(choices).toEqual(options);
    });

    it('adds a week outside the offered range in date order', () => {
        //given
        const options = buildWeekOptions('en-IL', new Date(2026, 9, 7));

        //when
        const choices = withWeekOption(options, '2026-08-02', 'en-IL');

        //then
        expect(choices[0]).toEqual({ weekStart: '2026-08-02', label: '2 Aug - 7 Aug 2026' });
        expect(choices[1].weekStart).toBe('2026-10-04');
    });
});
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the test command above.
Expected: FAIL. `withWeekOption` is not exported.

- [ ] **Step 3: Implement `withWeekOption`**

In `week-options.ts`, add after `buildWeekOptions`:

```typescript
export function withWeekOption(options: readonly WeekOption[], weekStart: string, locale: string): WeekOption[] {
    if (options.some((option) => option.weekStart === weekStart)) {
        return [...options];
    }

    const selected = { weekStart, label: weekRangeLabel(parseIsoDate(weekStart), locale) };

    return [...options, selected].sort((first, second) => first.weekStart.localeCompare(second.weekStart));
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the test command above.
Expected: both `withWeekOption` tests PASS.

- [ ] **Step 5: Write the failing page test**

In `publications-dashboard.page.spec.ts`, add inside `describe('PublicationsDashboardPage', ...)`:

```typescript
    it('shows the selected week in the week picker', async () => {
        //given
        const store = fakeStore({});

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__week-select .p-select-label')?.textContent?.trim()).toBe(WEEK_LABEL);
    });
```

- [ ] **Step 6: Run the test to verify it fails**

Run the test command above.
Expected: `shows the selected week in the week picker` FAILS (`.dashboard__week-select` is `null`).

- [ ] **Step 7: Expose `weekChoices` and render the picker**

In `publications.store.ts`, change the week-options import to:

```typescript
import { buildWeekOptions, WeekOption, weekRangeLabel, withWeekOption } from '../domain/week-options';
```

Add directly after `readonly weekOptions = computed<WeekOption[]>(...)`:

```typescript
    readonly weekChoices = computed<WeekOption[]>(() =>
        withWeekOption(this.weekOptions(), this.selectedWeekStartState(), this.language.locale()),
    );
```

In `publications-dashboard.page.html`, replace:

```html
                <span><bdi>{{ store.weekLabel() }}</bdi></span>
```

with:

```html
                <p-select
                    styleClass="dashboard__week-select"
                    [options]="store.weekChoices()"
                    optionLabel="label"
                    optionValue="weekStart"
                    [ngModel]="store.selectedWeekStart()"
                    (ngModelChange)="store.selectWeek($event)"
                    [ariaLabel]="'publications.week' | transloco" />
```

In `publications-dashboard.page.scss`, replace the `.dashboard__subtitle` rule:

```scss
.dashboard__subtitle {
    display: flex;
    flex-wrap: wrap;
    margin-block-start: 4px;
    font-size: 0.85rem;
    color: var(--app-text-secondary);
}
```

with the rules below. The font size moves off the container: PrimeNG 21 renders the open option list inside the `p-select`, so the list would otherwise inherit the small subtitle font (the same problem as the teacher title).

```scss
.dashboard__subtitle {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    margin-block-start: 4px;
}

.dashboard__subtitle-part {
    font-size: 0.85rem;
    color: var(--app-text-secondary);
}

:host ::ng-deep .dashboard__week-select {
    border: none;
    box-shadow: none;
    background: transparent;

    .p-select-label {
        padding: 0;
        font-size: 0.85rem;
        color: var(--app-text-secondary);
    }

    .p-select-dropdown {
        width: auto;
        padding-inline-start: 4px;
        color: var(--app-text-secondary);
    }
}
```

- [ ] **Step 8: Run the tests to verify they pass**

Run the test command above.
Expected: all `withWeekOption` and `PublicationsDashboardPage` tests PASS.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/publications
git commit -m "feat(publications): pick the week on the dashboard" -m "Closes #78"
```
