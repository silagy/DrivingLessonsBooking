# Task 6 of 10: Client — shared grid enums, feature domain + data layer

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–5 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from `client\` unless noted.

**Files:**
- Create: `client\src\app\shared\models\day-of-week.enum.ts`, `slot-window.enum.ts`, `week-grid-cell.ts`
- Create: `client\src\app\features\week-schedules\domain\slot-state.enum.ts`, `slot.model.ts`, `week-schedule.model.ts`, `teacher-option.model.ts`, `week-options.ts`
- Create: `client\src\app\features\week-schedules\data\get-week-schedule.response.ts`, `create-week-schedule.request.ts`, `create-week-schedule.response.ts`, `item-for-find-teachers.response.ts`, `week-schedules-api.service.ts`, `teacher-options-api.service.ts`

`DayOfWeek`/`SlotWindow` live in **shared** because the shared `WeekGridComponent` needs them (shared never imports from features). `SlotState` is prep-surface-specific → feature domain. All enum values are camelCase strings matching the API's `JsonStringEnumConverter(CamelCase)` output.

- [ ] **Step 1: Shared models**

`shared\models\day-of-week.enum.ts`:

```typescript
export enum DayOfWeek {
  sunday = 'sunday',
  monday = 'monday',
  tuesday = 'tuesday',
  wednesday = 'wednesday',
  thursday = 'thursday',
  friday = 'friday',
}
```

`shared\models\slot-window.enum.ts`:

```typescript
export enum SlotWindow {
  morning = 'morning',
  noon = 'noon',
  afternoon = 'afternoon',
  evening = 'evening',
}
```

`shared\models\week-grid-cell.ts`:

```typescript
import { DayOfWeek } from './day-of-week.enum';
import { SlotWindow } from './slot-window.enum';

export interface WeekGridCell {
  day: DayOfWeek;
  window: SlotWindow;
}
```

- [ ] **Step 2: Feature domain**

`features\week-schedules\domain\slot-state.enum.ts`:

```typescript
export enum SlotState {
  open = 'open',
  unavailable = 'unavailable',
}
```

`features\week-schedules\domain\slot.model.ts`:

```typescript
import { WeekGridCell } from '../../../shared/models/week-grid-cell';
import { SlotState } from './slot-state.enum';

export interface Slot extends WeekGridCell {
  id: string;
  state: SlotState;
  startLocal: string;
  endLocal: string;
}
```

`features\week-schedules\domain\week-schedule.model.ts`:

```typescript
import { Slot } from './slot.model';

export interface WeekSchedule {
  id: string;
  teacherId: string;
  weekStart: string;
  slots: Slot[];
}
```

`features\week-schedules\domain\teacher-option.model.ts`:

```typescript
export interface TeacherOption {
  id: string;
  name: string;
}
```

`features\week-schedules\domain\week-options.ts` (pure functions, no Angular):

```typescript
export interface WeekOption {
  weekStart: string;
  label: string;
}

const WEEK_OPTION_COUNT = 5;
const DAYS_PER_WEEK = 7;
const GRID_LAST_DAY_OFFSET = 5;

export function currentWeekStart(today: Date = new Date()): string {
  const sunday = new Date(today);
  sunday.setDate(today.getDate() - today.getDay());

  return toIsoDate(sunday);
}

export function buildWeekOptions(locale: string, today: Date = new Date()): WeekOption[] {
  const firstSunday = new Date(currentWeekStart(today));

  return Array.from({ length: WEEK_OPTION_COUNT }, (_, index) => {
    const weekStart = addDays(firstSunday, index * DAYS_PER_WEEK);

    return {
      weekStart: toIsoDate(weekStart),
      label: weekRangeLabel(weekStart, locale),
    };
  });
}

export function weekRangeLabel(weekStart: Date, locale: string): string {
  const weekEnd = addDays(weekStart, GRID_LAST_DAY_OFFSET);
  const startLabel = weekStart.toLocaleDateString(locale, { day: 'numeric', month: 'short' });
  const endLabel = weekEnd.toLocaleDateString(locale, { day: 'numeric', month: 'short', year: 'numeric' });

  return `${startLabel} – ${endLabel}`;
}

function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);

  return result;
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}
```

- [ ] **Step 3: Data layer**

`features\week-schedules\data\get-week-schedule.response.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SlotState } from '../domain/slot-state.enum';

export interface GetWeekScheduleResponse {
  id: string;
  teacherId: string;
  weekStart: string;
  slots: SlotForGetWeekScheduleResponse[];
}

export interface SlotForGetWeekScheduleResponse {
  id: string;
  day: DayOfWeek;
  window: SlotWindow;
  state: SlotState;
  startLocal: string;
  endLocal: string;
}
```

`features\week-schedules\data\create-week-schedule.request.ts`:

```typescript
export interface CreateWeekScheduleRequest {
  teacherId: string;
  weekStart: string;
}
```

`features\week-schedules\data\create-week-schedule.response.ts`:

```typescript
export interface CreateWeekScheduleResponse {
  id: string;
}
```

`features\week-schedules\data\item-for-find-teachers.response.ts` (duplicated contract — features never import from each other):

```typescript
export interface ItemForFindTeachersResponse {
  id: string;
  name: string;
}
```

> Check the backend `ItemForFindTeachersResponse` fields and include only what this feature needs (`id`, `name`) — extra fields in the JSON are simply not typed.

`features\week-schedules\data\week-schedules-api.service.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateWeekScheduleRequest } from './create-week-schedule.request';
import { CreateWeekScheduleResponse } from './create-week-schedule.response';
import { GetWeekScheduleResponse } from './get-week-schedule.response';

@Injectable({ providedIn: 'root' })
export class WeekSchedulesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'api/week-schedules';

  getByTeacherAndWeek(teacherId: string, week: string): Observable<GetWeekScheduleResponse> {
    const params = new HttpParams().set('teacherId', teacherId).set('week', week);

    return this.http.get<GetWeekScheduleResponse>(`${this.baseUrl}/by-teacher-and-week`, { params });
  }

  create(request: CreateWeekScheduleRequest): Observable<CreateWeekScheduleResponse> {
    return this.http.post<CreateWeekScheduleResponse>(this.baseUrl, request);
  }

  markSlotUnavailable(weekScheduleId: string, slotId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${weekScheduleId}/slots/${slotId}/mark-unavailable`, null);
  }

  markSlotAvailable(weekScheduleId: string, slotId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${weekScheduleId}/slots/${slotId}/mark-available`, null);
  }
}
```

`features\week-schedules\data\teacher-options-api.service.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ItemForFindTeachersResponse } from './item-for-find-teachers.response';

@Injectable({ providedIn: 'root' })
export class TeacherOptionsApiService {
  private readonly http = inject(HttpClient);

  findTeachers(): Observable<ItemForFindTeachersResponse[]> {
    return this.http.get<ItemForFindTeachersResponse[]>('api/teachers/find');
  }
}
```

- [ ] **Step 4: Verify client build, commit**

Run (in `client\`): `npm run build`
Expected: success (files are not yet referenced, but must compile — Angular builds the whole app graph; unreferenced files are checked by the editor/tsc only, so a clean `ng build` here mostly proves no syntax errors in imported chains).

```bash
git add client/src/app/shared/models client/src/app/features/week-schedules
git commit -m "feat(client): week-schedules domain models and api services"
```

---

**Next:** [task-07-week-grid-component.md](task-07-week-grid-component.md)
