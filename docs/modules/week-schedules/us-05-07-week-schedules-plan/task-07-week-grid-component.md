# Task 7 of 10: Shared WeekGridComponent + grid i18n keys

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–6 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from `client\` unless noted.

**Files:**
- Create: `client\src\app\shared\components\week-grid\week-grid.component.ts`, `.html`, `.scss`
- Modify: `client\src\styles\_tokens.scss` (add `--app-slot-unavailable`), `client\public\i18n\en.json`, `client\public\i18n\he.json` (add `weekGrid.*`)

Per client-primeng.md: one shared **dumb** component, fixed Sunday–Friday × window matrix, impossible cells (Fri afternoon/evening) do NOT exist in the DOM as interactive elements, cell content projected via template. CSS grid + logical properties → RTL mirrors automatically (Sunday at inline-start).

- [ ] **Step 1: Component**

`shared\components\week-grid\week-grid.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, contentChild, input, TemplateRef } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { DayOfWeek } from '../../models/day-of-week.enum';
import { SlotWindow } from '../../models/slot-window.enum';
import { WeekGridCell } from '../../models/week-grid-cell';

export interface WeekGridCellContext<T extends WeekGridCell> {
  $implicit: T;
}

const GRID_DAYS: readonly DayOfWeek[] = [
  DayOfWeek.sunday,
  DayOfWeek.monday,
  DayOfWeek.tuesday,
  DayOfWeek.wednesday,
  DayOfWeek.thursday,
  DayOfWeek.friday,
];

const GRID_WINDOWS: readonly SlotWindow[] = [
  SlotWindow.morning,
  SlotWindow.noon,
  SlotWindow.afternoon,
  SlotWindow.evening,
];

@Component({
  selector: 'app-week-grid',
  imports: [NgTemplateOutlet, TranslocoPipe],
  templateUrl: './week-grid.component.html',
  styleUrl: './week-grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeekGridComponent<T extends WeekGridCell> {
  readonly cells = input.required<readonly T[]>();
  readonly weekStart = input<string>();
  readonly windowTimes = input<Partial<Record<SlotWindow, string>>>({});

  readonly cellTemplate = contentChild.required<TemplateRef<WeekGridCellContext<T>>>(TemplateRef);

  protected readonly days = GRID_DAYS;
  protected readonly windows = GRID_WINDOWS;

  protected readonly cellMap = computed(() => {
    const map = new Map<string, T>();
    for (const cell of this.cells()) {
      map.set(`${cell.day}-${cell.window}`, cell);
    }
    return map;
  });

  protected cellFor(day: DayOfWeek, window: SlotWindow): T | undefined {
    return this.cellMap().get(`${day}-${window}`);
  }

  protected dateFor(day: DayOfWeek): Date | undefined {
    const weekStart = this.weekStart();
    if (!weekStart) {
      return undefined;
    }
    const date = new Date(weekStart);
    date.setDate(date.getDate() + this.days.indexOf(day));
    return date;
  }
}
```

`shared\components\week-grid\week-grid.component.html`:

```html
<div class="week-grid">
  <div class="week-grid__corner"></div>
  @for (day of days; track day) {
    <div class="week-grid__day-head">
      <span class="week-grid__day-name">{{ 'weekGrid.days.' + day | transloco }}</span>
      @if (dateFor(day); as date) {
        <span class="week-grid__day-date">{{ date.toLocaleDateString() }}</span>
      }
    </div>
  }
  @for (window of windows; track window) {
    <div class="week-grid__window-label">
      <span class="week-grid__window-name">{{ 'weekGrid.windows.' + window | transloco }}</span>
      @if (windowTimes()[window]; as time) {
        <span class="week-grid__window-time"><bdi>{{ time }}</bdi></span>
      }
    </div>
    @for (day of days; track day) {
      @if (cellFor(day, window); as cell) {
        <div class="week-grid__cell">
          <ng-container *ngTemplateOutlet="cellTemplate(); context: { $implicit: cell }" />
        </div>
      } @else {
        <div class="week-grid__cell week-grid__cell--void"></div>
      }
    }
  }
</div>
```

> Use the locale-aware date formatting approach the codebase already has (check `core\language.service.ts` for a locale signal); if none exists for dates, `toLocaleDateString(this.language.lang(), { day: 'numeric', month: 'numeric' })` injected via `LanguageService` is acceptable — shared may import core.

`shared\components\week-grid\week-grid.component.scss` (logical properties only, tokens only):

```scss
.week-grid {
  display: grid;
  grid-template-columns: 118px repeat(6, 1fr);
  gap: 8px;
}

.week-grid__day-head {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding-block-end: 10px;
  font-weight: 700;
  color: var(--app-ink);
}

.week-grid__day-date {
  font-size: 0.75rem;
  font-weight: 400;
  color: var(--app-muted, var(--p-surface-500));
}

.week-grid__window-label {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 2px;
  padding-inline-end: 14px;
}

.week-grid__window-name {
  font-weight: 700;
  color: var(--app-ink);
}

.week-grid__window-time {
  font-size: 0.75rem;
  color: var(--app-muted, var(--p-surface-500));
  font-variant-numeric: tabular-nums;
}

.week-grid__cell {
  min-height: 64px;
  display: flex;
}

.week-grid__cell--void {
  background: var(--p-surface-100);
  border-radius: 6px;
}
```

> Reuse the exact token names from `client\src\styles\_tokens.scss` — if `--app-muted` doesn't exist, use the closest existing token instead of inventing one.

- [ ] **Step 2: Token for unavailable slots**

In `client\src\styles\_tokens.scss` add (matching the mockup's striped `mkC.stripes` look):

```scss
--app-slot-unavailable: repeating-linear-gradient(
  45deg,
  var(--p-surface-100),
  var(--p-surface-100) 6px,
  var(--p-surface-200) 6px,
  var(--p-surface-200) 12px
);
```

- [ ] **Step 3: i18n keys (both files, mirrored)**

`client\public\i18n\en.json` — add top-level:

```json
"weekGrid": {
  "days": {
    "sunday": "Sunday",
    "monday": "Monday",
    "tuesday": "Tuesday",
    "wednesday": "Wednesday",
    "thursday": "Thursday",
    "friday": "Friday"
  },
  "windows": {
    "morning": "Morning",
    "noon": "Noon",
    "afternoon": "Afternoon",
    "evening": "Evening"
  },
  "legend": {
    "open": "Open",
    "unavailable": "Unavailable",
    "outsideGrid": "Outside grid (Fri PM, Saturday)"
  }
}
```

`client\public\i18n\he.json`:

```json
"weekGrid": {
  "days": {
    "sunday": "ראשון",
    "monday": "שני",
    "tuesday": "שלישי",
    "wednesday": "רביעי",
    "thursday": "חמישי",
    "friday": "שישי"
  },
  "windows": {
    "morning": "בוקר",
    "noon": "צהריים",
    "afternoon": "אחה״צ",
    "evening": "ערב"
  },
  "legend": {
    "open": "פתוח",
    "unavailable": "לא זמין",
    "outsideGrid": "מחוץ לרשת (שישי אחה״צ/ערב, שבת)"
  }
}
```

- [ ] **Step 4: Build, commit**

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app/shared client/src/styles client/public/i18n
git commit -m "feat(client): shared week grid component"
```

---

**Next:** [task-08-signal-store.md](task-08-signal-store.md)
