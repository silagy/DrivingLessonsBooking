# Task 3 of 5: Empty state for a week without a publication (P2, TDD)

> Part of [#76: Publications Dashboard - Share Link While Open, Week Picker, Empty State](README.md). Sub-issue [#79](https://github.com/silagy/DrivingLessonsBooking/issues/79). Requires tasks 1–2 committed. Work on branch `76-dashboard-link-and-week`.

**Files:**
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` (the `@if (store.isLoading())` chain)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (+ `publications.dashboard.prepareWeek`)

**Interfaces:**
- Consumes: `PublicationsStore.hasPublication(): boolean` and `isLoading(): boolean` (both exist); `AppRoutes.weekSchedules` (`'week-schedules'`, in `shared\config\app-routes.ts`); the existing, unused key `publications.noPublication` ("השבוע הזה עדיין לא הוכן." / "This week has not been prepared yet."); task 1's `fakeStore` and `render`.
- Produces: the translation key `publications.dashboard.prepareWeek` in both files.

**Why:** README P2 and decision 5. When the selected week has no publication, the dashboard currently renders nothing below the header.

**Run the tests** (PowerShell, from `client\`):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/publications/**/*.spec.ts"
```

- [ ] **Step 1: Write the failing tests**

In `publications-dashboard.page.spec.ts`, add inside `describe('PublicationsDashboardPage', ...)`:

```typescript
    it('explains that the week is not prepared and links to weekly prep', async () => {
        //given
        const store = fakeStore({
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            weekNumber: signal(undefined),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__empty')).not.toBeNull();
        expect(page.querySelector('.dashboard__empty a')?.getAttribute('href')).toBe('/week-schedules');
    });

    it('shows the spinner rather than the empty state while loading', async () => {
        //given
        const store = fakeStore({
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            isLoading: signal(true),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__empty')).toBeNull();
        expect(page.querySelector('p-progressspinner')).not.toBeNull();
    });
```

- [ ] **Step 2: Run the tests to verify the first one fails**

Run the test command above.
Expected: `explains that the week is not prepared and links to weekly prep` FAILS (`.dashboard__empty` is `null`). The spinner test already PASSES.

- [ ] **Step 3: Implement the empty state**

In `publications-dashboard.page.html`, replace:

```html
    } @else if (store.loadError(); as errorKey) {
        <div class="dashboard__state">{{ errorKey | transloco }}</div>
    } @else {
```

with:

```html
    } @else if (store.loadError(); as errorKey) {
        <div class="dashboard__state">{{ errorKey | transloco }}</div>
    } @else if (!store.hasPublication()) {
        <section class="dashboard__card dashboard__empty">
            <p class="dashboard__notice">{{ 'publications.noPublication' | transloco }}</p>
            <a
                pButton
                [label]="'publications.dashboard.prepareWeek' | transloco"
                [routerLink]="['/', appRoutes.weekSchedules]"></a>
        </section>
    } @else {
```

In `publications-dashboard.page.ts`:
- add `import { RouterLink } from '@angular/router';`
- add `import { AppRoutes } from '../../../../../shared/config/app-routes';`
- add `RouterLink` to the component `imports` array, after `FormsModule`
- add this field directly after `protected readonly PublicationState = PublicationState;`:

```typescript
    protected readonly appRoutes = AppRoutes;
```

In `publications-dashboard.page.scss`, add after the `.dashboard__notice` rule:

```scss
.dashboard__empty {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;

    .dashboard__notice {
        margin: 0;
    }
}
```

In `client\public\i18n\he.json`, inside `publications.dashboard`, change the last entry:

```json
      "closedAt": "ההגשה נסגרה {{moment}}"
```

to:

```json
      "closedAt": "ההגשה נסגרה {{moment}}",
      "prepareWeek": "להכנת השבוע"
```

In `client\public\i18n\en.json`, inside `publications.dashboard`, change:

```json
      "closedAt": "Submissions closed {{moment}}"
```

to:

```json
      "closedAt": "Submissions closed {{moment}}",
      "prepareWeek": "Prepare this week"
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the test command above.
Expected: all `PublicationsDashboardPage` tests PASS.

- [ ] **Step 5: Commit**

```bash
git add client/src/app/features/publications client/public/i18n
git commit -m "feat(publications): explain weeks without a publication on the dashboard" -m "Closes #79"
```
