# Task 10 of 14: Client — core services, shared enums, feature domain + data

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–9 complete (the full backend API is live). Work on branch `9-us-08-21-publications-module`, commands from `client\` unless noted.

Two app-wide singletons (file download, clipboard), the cross-feature enum relocation the plan calls for (`SlotState` → `shared\models`, decision #12), a new `PublicationState` shared enum, then the `publications` feature `domain\` + `data\` layers. DTO interfaces are named **identically** to the backend DTOs; `publications` never imports from `features\week-schedules` or `features\teachers` (it copies `TeacherOption`, `week-options`, and its own `TeacherOptionsApiService`).

**Files:**
- Create: `client\src\app\core\services\file-download.service.ts`, `clipboard.service.ts`
- Create: `client\src\app\shared\models\slot-state.enum.ts` (relocated), `publication-state.enum.ts`
- Delete: `client\src\app\features\week-schedules\domain\slot-state.enum.ts`
- Modify: 4 week-schedules imports of `slot-state.enum` (Step 3)
- Create: `client\src\app\features\publications\domain\publication.model.ts`, `slot-count-cell.model.ts`, `teacher-option.model.ts`, `week-options.ts`, `jerusalem-time.ts`
- Create: `client\src\app\features\publications\data\get-publication.response.ts`, `publish-publication.request.ts`, `extend-publication-window.request.ts`, `reopen-publication.request.ts`, `get-publication-dashboard.response.ts`, `item-for-find-publication-history.response.ts`, `item-for-find-teachers.response.ts`, `publications-api.service.ts`, `teacher-options-api.service.ts`

---

- [ ] **Step 1: Core services**

`core\services\file-download.service.ts`:

```typescript
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class FileDownloadService {
    download(blob: Blob, fileName: string): void {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');

        anchor.href = url;
        anchor.download = fileName;
        anchor.click();

        URL.revokeObjectURL(url);
    }
}
```

`core\services\clipboard.service.ts`:

```typescript
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ClipboardService {
    async copy(text: string): Promise<boolean> {
        try {
            await navigator.clipboard.writeText(text);

            return true;
        } catch {
            return false;
        }
    }
}
```

- [ ] **Step 2: Relocate `SlotState` and add `PublicationState` to `shared\models`**

Create `shared\models\slot-state.enum.ts` with the exact contents of the current feature file:

```typescript
export enum SlotState {
    open = 'open',
    unavailable = 'unavailable',
}
```

Create `shared\models\publication-state.enum.ts`:

```typescript
export enum PublicationState {
    draft = 'draft',
    published = 'published',
    open = 'open',
    closed = 'closed',
}
```

Delete the old file:

```bash
git rm client/src/app/features/week-schedules/domain/slot-state.enum.ts
```

- [ ] **Step 3: Update the 4 week-schedules imports of `slot-state.enum`**

Each currently imports from the feature `domain\` folder; repoint to `shared\models`. Apply exactly these edits:

`features\week-schedules\domain\slot.model.ts`:

```typescript
import { SlotState } from './slot-state.enum';
```
→
```typescript
import { SlotState } from '../../../shared/models/slot-state.enum';
```

`features\week-schedules\data\get-week-schedule.response.ts`:

```typescript
import { SlotState } from '../domain/slot-state.enum';
```
→
```typescript
import { SlotState } from '../../../shared/models/slot-state.enum';
```

`features\week-schedules\state\week-schedules.store.ts`:

```typescript
import { SlotState } from '../domain/slot-state.enum';
```
→
```typescript
import { SlotState } from '../../../shared/models/slot-state.enum';
```

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts`:

```typescript
import { SlotState } from '../../../domain/slot-state.enum';
```
→
```typescript
import { SlotState } from '../../../../../shared/models/slot-state.enum';
```

> These four are the only references (verified by search). No other week-schedules file imports `slot-state.enum`.

- [ ] **Step 4: Feature domain**

`features\publications\domain\publication.model.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface Publication {
    id: string;
    weekStart: string;
    state: PublicationState;
    linkToken: string;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
}
```

`features\publications\domain\slot-count-cell.model.ts`:

```typescript
import { SlotState } from '../../../shared/models/slot-state.enum';
import { WeekGridCell } from '../../../shared/models/week-grid-cell';

export interface SlotCountCell extends WeekGridCell {
    slotId: string;
    state: SlotState;
    requestCount: number;
}
```

`features\publications\domain\teacher-option.model.ts` (copied — no cross-feature import):

```typescript
export interface TeacherOption {
    id: string;
    name: string;
}
```

`features\publications\domain\week-options.ts` (copied from week-schedules — pure functions, no Angular):

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

`features\publications\domain\jerusalem-time.ts` (submission windows are entered as Asia/Jerusalem wall time and sent to the API as UTC ISO; instants come back UTC and display in Asia/Jerusalem — §8.3):

```typescript
const MILLIS_PER_MINUTE = 60_000;

export function jerusalemWallTimeToUtcIso(wallTime: Date): string {
    const wallAsUtc = Date.UTC(
        wallTime.getFullYear(),
        wallTime.getMonth(),
        wallTime.getDate(),
        wallTime.getHours(),
        wallTime.getMinutes(),
        0,
    );
    const offsetMinutes = jerusalemOffsetMinutes(new Date(wallAsUtc));

    return new Date(wallAsUtc - offsetMinutes * MILLIS_PER_MINUTE).toISOString();
}

export function formatInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: 'Asia/Jerusalem',
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}

function jerusalemOffsetMinutes(instant: Date): number {
    const formatter = new Intl.DateTimeFormat('en-US', {
        timeZone: 'Asia/Jerusalem',
        hourCycle: 'h23',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
    });
    const parts = formatter.formatToParts(instant);
    const values = new Map(parts.map((part) => [part.type, part.value]));
    const wallAsUtc = Date.UTC(
        Number(values.get('year')),
        Number(values.get('month')) - 1,
        Number(values.get('day')),
        Number(values.get('hour')),
        Number(values.get('minute')),
        Number(values.get('second')),
    );

    return Math.round((wallAsUtc - instant.getTime()) / MILLIS_PER_MINUTE);
}
```

- [ ] **Step 5: Data layer — DTO interfaces**

`features\publications\data\get-publication.response.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface GetPublicationResponse {
    id: string;
    weekStart: string;
    state: PublicationState;
    linkToken: string;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
}
```

`features\publications\data\publish-publication.request.ts`:

```typescript
export interface PublishPublicationRequest {
    startUtc: string;
    endUtc: string;
}
```

`features\publications\data\extend-publication-window.request.ts`:

```typescript
export interface ExtendPublicationWindowRequest {
    newEndUtc: string;
}
```

`features\publications\data\reopen-publication.request.ts`:

```typescript
export interface ReopenPublicationRequest {
    newEndUtc: string;
}
```

`features\publications\data\get-publication-dashboard.response.ts`:

```typescript
import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';

export interface GetPublicationDashboardResponse {
    state: PublicationState;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
    linkToken: string;
    studentsSubmitted: number;
    totalPicks: number;
    lastSubmissionAtUtc: string | null;
    latestExcelVersion: number | null;
    slotCounts: SlotCountForGetPublicationDashboardResponse[];
}

export interface SlotCountForGetPublicationDashboardResponse {
    slotId: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    requestCount: number;
}
```

`features\publications\data\item-for-find-publication-history.response.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface ItemForFindPublicationHistoryResponse {
    publicationId: string;
    weekStart: string;
    teacherId: string;
    teacherName: string;
    state: PublicationState;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
    latestExcelVersion: number | null;
}
```

`features\publications\data\item-for-find-teachers.response.ts` (duplicated contract — features never import from each other; only the fields this feature needs are typed):

```typescript
export interface ItemForFindTeachersResponse {
    id: string;
    name: string;
}
```

- [ ] **Step 6: Data layer — API services**

`features\publications\data\publications-api.service.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ExtendPublicationWindowRequest } from './extend-publication-window.request';
import { GetPublicationDashboardResponse } from './get-publication-dashboard.response';
import { GetPublicationResponse } from './get-publication.response';
import { ItemForFindPublicationHistoryResponse } from './item-for-find-publication-history.response';
import { PublishPublicationRequest } from './publish-publication.request';
import { ReopenPublicationRequest } from './reopen-publication.request';

@Injectable({ providedIn: 'root' })
export class PublicationsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/publications';

    getByWeek(week: string): Observable<GetPublicationResponse> {
        const params = new HttpParams().set('week', week);

        return this.http.get<GetPublicationResponse>(`${this.baseUrl}/by-week`, { params });
    }

    publish(id: string, request: PublishPublicationRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/publish`, request);
    }

    extendWindow(id: string, request: ExtendPublicationWindowRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/extend-window`, request);
    }

    reopen(id: string, request: ReopenPublicationRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/reopen`, request);
    }

    getDashboard(id: string, teacherId: string): Observable<GetPublicationDashboardResponse> {
        const params = new HttpParams().set('teacherId', teacherId);

        return this.http.get<GetPublicationDashboardResponse>(`${this.baseUrl}/${id}/dashboard`, { params });
    }

    findHistory(): Observable<ItemForFindPublicationHistoryResponse[]> {
        return this.http.get<ItemForFindPublicationHistoryResponse[]>(`${this.baseUrl}/history`);
    }

    downloadExcel(id: string, teacherId: string): Observable<Blob> {
        const params = new HttpParams().set('teacherId', teacherId);

        return this.http.get(`${this.baseUrl}/${id}/excel`, { params, responseType: 'blob' });
    }
}
```

`features\publications\data\teacher-options-api.service.ts` (own copy — no cross-feature import):

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

- [ ] **Step 7: Verify build, commit**

Run (in `client\`): `npm run build`

Expected: success. The relocation edits (Step 3) must compile against the moved `SlotState`; the new `publications` files are not yet referenced (the store + pages arrive in tasks 11–12), so they are checked for syntax only. If your Angular config fails the build on an unreferenced feature route, defer the full build check to task 12 — but the relocation + week-schedules edits must build now regardless.

```bash
git add client/src/app/core/services client/src/app/shared/models client/src/app/features/week-schedules client/src/app/features/publications
git commit -m "feat(client): publications domain models and api services"
```

---

**Next:** [task-11-client-store-routing.md](task-11-client-store-routing.md)
