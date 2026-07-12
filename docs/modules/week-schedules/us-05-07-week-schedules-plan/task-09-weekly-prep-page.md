# Task 9 of 10: Weekly Prep page, routes, nav, feature i18n

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–8 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from `client\` unless noted.

**Files:**
- Create: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts`, `.html`, `.scss`
- Create: `client\src\app\features\week-schedules\week-schedules.routes.ts`
- Modify: `client\src\app\shared\config\app-routes.ts`, `client\src\app\features\admin-shell\admin.routes.ts`, `client\src\app\features\admin-shell\admin-shell.component.html`, `client\public\i18n\en.json`, `client\public\i18n\he.json`

- [ ] **Step 1: Routes + nav**

`shared\config\app-routes.ts` — add:

```typescript
weekSchedules: 'week-schedules',
```

`features\week-schedules\week-schedules.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { WeeklyPrepPage } from './ui/pages/weekly-prep/weekly-prep.page';

export default [{ path: '', component: WeeklyPrepPage }] satisfies Routes;
```

`features\admin-shell\admin.routes.ts` — add child (next to `teachers`):

```typescript
{
  path: AppRoutes.weekSchedules,
  loadChildren: () => import('../week-schedules/week-schedules.routes'),
},
```

`features\admin-shell\admin-shell.component.html` — add a nav link next to the Teachers link (copy its exact markup/RouterLinkActive usage):

```html
<a [routerLink]="['/', AppRoutes.weekSchedules]" routerLinkActive="active">
  {{ 'shell.nav.weeklyPrep' | transloco }}
</a>
```

- [ ] **Step 2: Page**

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { WeekGridComponent } from '../../../../../shared/components/week-grid/week-grid.component';
import { SlotState } from '../../../domain/slot-state.enum';
import { WeekSchedulesStore } from '../../../state/week-schedules.store';

@Component({
  selector: 'app-weekly-prep-page',
  imports: [FormsModule, TranslocoPipe, ProgressSpinnerModule, SelectModule, WeekGridComponent],
  templateUrl: './weekly-prep.page.html',
  styleUrl: './weekly-prep.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyPrepPage {
  protected readonly store = inject(WeekSchedulesStore);
  protected readonly SlotState = SlotState;
}
```

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`:

```html
<div class="weekly-prep">
  <header class="weekly-prep__header">
    <div>
      <h2>{{ 'weekSchedules.title' | transloco }}</h2>
      <p class="weekly-prep__subtitle">{{ 'weekSchedules.subtitle' | transloco }}</p>
    </div>
  </header>

  <div class="weekly-prep__selectors">
    <div class="field">
      <label for="teacher">{{ 'weekSchedules.teacher' | transloco }}</label>
      <p-select
        inputId="teacher"
        [options]="store.teachers()"
        optionLabel="name"
        optionValue="id"
        [ngModel]="store.selectedTeacherId()"
        (ngModelChange)="store.selectTeacher($event)"
        [placeholder]="'weekSchedules.selectTeacher' | transloco" />
    </div>
    <div class="field">
      <label for="week">{{ 'weekSchedules.week' | transloco }}</label>
      <p-select
        inputId="week"
        [options]="store.weekOptions()"
        optionLabel="label"
        optionValue="weekStart"
        [ngModel]="store.selectedWeekStart()"
        (ngModelChange)="store.selectWeek($event)" />
    </div>
  </div>

  @if (store.isLoading()) {
    <div class="weekly-prep__state"><p-progressSpinner /></div>
  } @else if (store.loadError(); as errorKey) {
    <div class="weekly-prep__state">{{ errorKey | transloco }}</div>
  } @else if (!store.hasSelection()) {
    <div class="weekly-prep__state">{{ 'weekSchedules.selectTeacherPrompt' | transloco }}</div>
  } @else if (store.weekSchedule()) {
    <section class="weekly-prep__card">
      <app-week-grid
        [cells]="store.slots()"
        [weekStart]="store.selectedWeekStart()"
        [windowTimes]="store.windowTimes()">
        <ng-template let-slot>
          <button
            type="button"
            class="slot-cell"
            [class.slot-cell--unavailable]="slot.state === SlotState.unavailable"
            [disabled]="store.isMutating()"
            (click)="store.toggleSlot(slot)">
            {{ (slot.state === SlotState.open ? 'weekSchedules.cellOpen' : 'weekSchedules.cellUnavailable') | transloco }}
          </button>
        </ng-template>
      </app-week-grid>

      <footer class="weekly-prep__footer">
        <div class="weekly-prep__legend">
          <span class="legend-item"><span class="legend-swatch legend-swatch--open"></span>{{ 'weekGrid.legend.open' | transloco }}</span>
          <span class="legend-item"><span class="legend-swatch legend-swatch--unavailable"></span>{{ 'weekGrid.legend.unavailable' | transloco }}</span>
          <span class="legend-item"><span class="legend-swatch legend-swatch--void"></span>{{ 'weekGrid.legend.outsideGrid' | transloco }}</span>
        </div>
        <span class="weekly-prep__count">
          {{ 'weekSchedules.unavailableCount' | transloco: { count: store.unavailableCount() } }}
        </span>
      </footer>
    </section>
  }
</div>
```

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.scss` (logical properties, tokens only — match `teachers-list.page.scss` conventions):

```scss
.weekly-prep__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
}

.weekly-prep__subtitle {
  color: var(--p-surface-500);
  margin-block-start: 4px;
}

.weekly-prep__selectors {
  display: flex;
  gap: 16px;
  margin-block-start: 20px;
  align-items: flex-end;

  .field {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
}

.weekly-prep__card {
  background: var(--p-surface-0);
  border: 1px solid var(--p-surface-200);
  border-radius: var(--app-radius-lg, 14px);
  padding: 22px 24px;
  margin-block-start: 18px;
}

.weekly-prep__state {
  display: flex;
  justify-content: center;
  padding-block: 48px;
}

.weekly-prep__footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-block-start: 18px;
}

.weekly-prep__legend {
  display: flex;
  gap: 22px;
}

.legend-item {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-size: 0.75rem;
  color: var(--p-surface-600);
}

.legend-swatch {
  width: 14px;
  height: 14px;
  border-radius: 4px;
  border: 1px solid var(--p-surface-200);

  &--open {
    background: var(--p-surface-0);
  }

  &--unavailable {
    background: var(--app-slot-unavailable);
  }

  &--void {
    background: var(--p-surface-100);
    border: none;
  }
}

.weekly-prep__count {
  font-size: 0.75rem;
  color: var(--p-surface-500);
}

.slot-cell {
  flex: 1;
  min-height: 64px;
  border-radius: 6px;
  border: 1px solid var(--p-surface-200);
  background: var(--p-surface-0);
  color: var(--p-surface-500);
  font-size: 0.75rem;
  cursor: pointer;

  &--unavailable {
    background: var(--app-slot-unavailable);
    color: var(--p-surface-500);
  }

  &:disabled {
    cursor: default;
  }
}
```

> Swap any `--p-*` var above for the corresponding `--app-*` alias if one exists in `_tokens.scss` (e.g. `--app-ink`) — tokens file wins.

- [ ] **Step 3: Feature i18n keys (both files)**

`en.json` — add `weekSchedules` namespace and `shell.nav.weeklyPrep`:

```json
"weekSchedules": {
  "title": "Prepare a week",
  "subtitle": "All slots start Open — click a slot to mark it Unavailable.",
  "teacher": "Teacher",
  "week": "Week",
  "selectTeacher": "Select a teacher",
  "selectTeacherPrompt": "Select a teacher and week to prepare the grid.",
  "cellOpen": "Open",
  "cellUnavailable": "Unavailable",
  "unavailableCount": "{{count}} slots marked unavailable",
  "loadFailed": "Failed to load the week schedule."
}
```
and inside the existing `shell.nav`: `"weeklyPrep": "Weekly prep"`.

`he.json`:

```json
"weekSchedules": {
  "title": "הכנת שבוע",
  "subtitle": "כל המשבצות מתחילות פתוחות — לחצו על משבצת כדי לסמן אותה כלא זמינה.",
  "teacher": "מורה",
  "week": "שבוע",
  "selectTeacher": "בחרו מורה",
  "selectTeacherPrompt": "בחרו מורה ושבוע כדי להכין את הרשת.",
  "cellOpen": "פתוח",
  "cellUnavailable": "חסום",
  "unavailableCount": "{{count}} משבצות סומנו כלא זמינות",
  "loadFailed": "טעינת שבוע העבודה נכשלה."
}
```
and inside `shell.nav`: `"weeklyPrep": "הכנת שבוע"`.

- [ ] **Step 4: Build, commit**

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app client/public/i18n
git commit -m "feat(client): weekly prep page with slot toggle grid (US-05..07)"
```

---

**Next:** [task-10-verification.md](task-10-verification.md)
