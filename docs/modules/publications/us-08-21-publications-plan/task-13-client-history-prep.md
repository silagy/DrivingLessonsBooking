# Task 13 of 14: Client — history page + weekly-prep publish entry point

> Part of [US-08–21: Publications Module](README.md) ([parent plan](../us-08-21-publications-plan.md)). Requires tasks 1–12 complete. Work on branch `9-us-08-21-publications-module`, commands from `client\` unless noted.

Two deliverables: the **publications history** page (mockup `AdminHistory`) driven by a new resource on `PublicationsStore`, and the **weekly-prep** publish entry point — a Draft state chip plus a "Publish week…" button that routes to the dashboard with `queryParams { teacherId, week, publish: 1 }`. The cross-feature rule (decision #12) forbids `week-schedules` importing from `publications`, so the prep page gets its **own** publication-status data service and a local `GetPublicationResponse` copy; it links to the dashboard via the shared `AppRoutes` constant, never by importing a publications symbol.

**Files:**
- Create: `client\src\app\features\publications\ui\pages\publications-history\publications-history.page.ts`, `.html`, `.scss`
- Create: `client\src\app\features\week-schedules\data\get-publication.response.ts`
- Create: `client\src\app\features\week-schedules\data\publication-status-api.service.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts` (add the history resource + reload)
- Modify: `client\src\app\features\publications\publications.routes.ts` (add the `history` child)
- Modify: `client\src\app\features\week-schedules\state\week-schedules.store.ts` (add `publicationState` computed keyed on the selected week)
- Modify: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts`, `.html` (Draft chip + Publish button)
- Modify: `client\src\app\shared\config\app-routes.ts` (add `publicationsHistory` if not already present from task 11)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (history keys + prep publish keys)

---

- [ ] **Step 1: History resource on `PublicationsStore`**

Add a self-contained history read + a reload, alongside the by-week / dashboard resources from task 11. `findHistory()` maps to `GET api/publications/history`.

In `features\publications\state\publications.store.ts` add:

```typescript
    private readonly historyResource = resource({
        loader: () => firstValueFrom(this.api.findHistory()),
    });

    readonly history = computed<ItemForFindPublicationHistoryResponse[]>(() => this.historyResource.value() ?? []);
    readonly historyIsLoading = this.historyResource.isLoading;
    readonly historyError = computed(() => (this.historyResource.error() ? 'publications.history.loadFailed' : null));
    readonly historyIsEmpty = computed(() => !this.historyIsLoading() && this.history().length === 0);

    reloadHistory(): void {
        this.historyResource.reload();
    }
```

Add the import to the store's existing import block:

```typescript
import { ItemForFindPublicationHistoryResponse } from '../data/item-for-find-publication-history.response';
```

> `ItemForFindPublicationHistoryResponse` and `PublicationsApiService.findHistory()` (+ `downloadExcel(publicationId, teacherId)`) are created in tasks 10–11. Confirm the DTO fields against `data\item-for-find-publication-history.response.ts`: `publicationId`, `weekStart`, `teacherId`, `teacherName`, `state`, `windowStartUtc`, `windowEndUtc`, `latestExcelVersion`. `downloadExcel` for a history row must accept both `publicationId` and `teacherId` — if the task-11 store `downloadExcel()` only used the selected pub, add an overload `downloadExcel(publicationId?, teacherId?)` that falls back to the current selection.

- [ ] **Step 2: History route**

In `features\publications\publications.routes.ts` add the `history` child next to the default dashboard route:

```typescript
import { Routes } from '@angular/router';
import { AppRoutes } from '../../shared/config/app-routes';
import { PublicationsDashboardPage } from './ui/pages/publications-dashboard/publications-dashboard.page';
import { PublicationsHistoryPage } from './ui/pages/publications-history/publications-history.page';

export default [
    { path: '', component: PublicationsDashboardPage },
    { path: AppRoutes.publicationsHistory, component: PublicationsHistoryPage },
] satisfies Routes;
```

Ensure `shared\config\app-routes.ts` has both entries (add if task 11 did not):

```typescript
publications: 'publications',
publicationsHistory: 'history',
```

- [ ] **Step 3: History page (smart)**

A `p-table` per `AdminHistory`: Week, Teacher, Submission window (Jerusalem time), Status via `app-publication-state-tag`, Last Excel (version pill or `-`), and a row action — **Re-download** for a closed row with a version (calls `store.downloadExcel(row.publicationId, row.teacherId)`), else **View dashboard** (navigates to the dashboard with `teacherId` + `week` query params). Empty state rendered explicitly.

`features\publications\ui\pages\publications-history\publications-history.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationsStore } from '../../../state/publications.store';
import { ItemForFindPublicationHistoryResponse } from '../../../data/item-for-find-publication-history.response';

@Component({
    selector: 'app-publications-history-page',
    imports: [
        TranslocoPipe,
        ButtonModule,
        TableModule,
        ProgressSpinnerModule,
        PublicationStateTagComponent,
    ],
    templateUrl: './publications-history.page.html',
    styleUrl: './publications-history.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsHistoryPage {
    protected readonly store = inject(PublicationsStore);
    private readonly router = inject(Router);
    private readonly language = inject(LanguageService);

    protected readonly PublicationState = PublicationState;

    protected windowLabel(row: ItemForFindPublicationHistoryResponse): string {
        if (!row.windowStartUtc || !row.windowEndUtc) {
            return '—';
        }

        return `${this.formatInstant(row.windowStartUtc)} → ${this.formatInstant(row.windowEndUtc)}`;
    }

    protected canRedownload(row: ItemForFindPublicationHistoryResponse): boolean {
        return row.state === PublicationState.closed && row.latestExcelVersion !== null;
    }

    protected onRedownload(row: ItemForFindPublicationHistoryResponse): void {
        void this.store.downloadExcel(row.publicationId, row.teacherId);
    }

    protected onViewDashboard(row: ItemForFindPublicationHistoryResponse): void {
        void this.router.navigate(['/', AppRoutes.publications], {
            queryParams: { teacherId: row.teacherId, week: row.weekStart },
        });
    }

    private formatInstant(utcIso: string): string {
        return formatInstantInJerusalem(utcIso, this.language.lang());
    }
}
```

> `AppRoutes.publications` is the dashboard (empty child), so navigating `['/', AppRoutes.publications]` with query params lands on the dashboard; its `input()` bindings pick up `teacherId`/`week` (task 12). Confirm `PublicationState.closed` and the `latestExcelVersion` nullability against the DTO.

`features\publications\ui\pages\publications-history\publications-history.page.html`:

```html
<div class="history">
    <header class="history__header">
        <h2>{{ 'publications.history.title' | transloco }}</h2>
        <p class="history__subtitle">{{ 'publications.history.subtitle' | transloco }}</p>
    </header>

    @if (store.historyIsLoading()) {
        <div class="history__state"><p-progressSpinner /></div>
    } @else if (store.historyError(); as errorKey) {
        <div class="history__state">{{ errorKey | transloco }}</div>
    } @else if (store.historyIsEmpty()) {
        <div class="history__state">{{ 'publications.history.empty' | transloco }}</div>
    } @else {
        <div class="history__card">
            <p-table [value]="store.history()" [rows]="20" styleClass="history__table">
                <ng-template pTemplate="header">
                    <tr>
                        <th>{{ 'publications.history.week' | transloco }}</th>
                        <th>{{ 'publications.history.teacher' | transloco }}</th>
                        <th>{{ 'publications.history.window' | transloco }}</th>
                        <th>{{ 'publications.history.status' | transloco }}</th>
                        <th>{{ 'publications.history.lastExcel' | transloco }}</th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-row>
                    <tr>
                        <td><bdi>{{ row.weekStart }}</bdi></td>
                        <td>{{ row.teacherName }}</td>
                        <td class="history__window"><bdi>{{ windowLabel(row) }}</bdi></td>
                        <td><app-publication-state-tag [state]="row.state" /></td>
                        <td>
                            @if (row.latestExcelVersion !== null) {
                                <span class="history__version-pill"><bdi>v{{ row.latestExcelVersion }}</bdi></span>
                            } @else {
                                <span class="history__dash">—</span>
                            }
                        </td>
                        <td class="history__action">
                            @if (canRedownload(row)) {
                                <p-button
                                    [label]="'publications.history.redownload' | transloco"
                                    severity="secondary"
                                    text
                                    size="small"
                                    (onClick)="onRedownload(row)" />
                            } @else {
                                <p-button
                                    [label]="'publications.history.viewDashboard' | transloco"
                                    severity="secondary"
                                    text
                                    size="small"
                                    (onClick)="onViewDashboard(row)" />
                            }
                        </td>
                    </tr>
                </ng-template>
            </p-table>
        </div>
    }
</div>
```

`features\publications\ui\pages\publications-history\publications-history.page.scss` (logical properties, tokens only):

```scss
.history__subtitle {
    color: var(--app-text-secondary);
    margin-block-start: 4px;
}

.history__card {
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    overflow: hidden;
    margin-block-start: 20px;
}

.history__state {
    display: flex;
    justify-content: center;
    padding-block: 48px;
    color: var(--app-text-secondary);
}

.history__window {
    color: var(--app-text-secondary);
    font-size: 0.82rem;
}

.history__version-pill {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.72rem;
    background: var(--app-bg-muted);
    border: 1px solid var(--app-border);
    border-radius: 999px;
    padding: 3px 10px;
    font-variant-numeric: tabular-nums;
}

.history__dash {
    color: var(--app-text-muted);
}

.history__action {
    text-align: end;
}
```

> Confirm the `p-table` template syntax against the installed PrimeNG version. This repo uses the `*Module` barrels, so `TableModule` and the `pTemplate="header"`/`"body"` structural-directive API apply. If the project has adopted the newer `@if`/`@for`-only table content style, swap the `ng-template pTemplate` blocks accordingly — but `pTemplate` is the standard PrimeNG table API and is fine here.

- [ ] **Step 4: Weekly-prep — local publication-status data service (no cross-feature import)**

The prep page must show whether the selected week is already a Draft publication. It cannot import from `features/publications` (decision #12), so `week-schedules` gets its own DTO copy + API service hitting the same `GET api/publications/by-week` endpoint.

`features\week-schedules\data\get-publication.response.ts`:

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

`features\week-schedules\data\publication-status-api.service.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationResponse } from './get-publication.response';

@Injectable({ providedIn: 'root' })
export class PublicationStatusApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/publications';

    getByWeek(week: string): Observable<GetPublicationResponse> {
        const params = new HttpParams().set('week', week);

        return this.http.get<GetPublicationResponse>(`${this.baseUrl}/by-week`, { params });
    }
}
```

> `PublicationState` is a shared enum (`shared/models`), so importing it is not a cross-feature import — only `features/publications` symbols are off-limits. The DTO shape is copied intentionally; this is the accepted duplication cost of decision #12.

- [ ] **Step 5: `publicationState` computed on `WeekSchedulesStore`**

Add a resource keyed on the selected week that resolves the week's publication state (404 → `undefined`), so the prep page can show the Draft chip and gate the Publish button.

In `features\week-schedules\state\week-schedules.store.ts`:

```typescript
import { PublicationStatusApiService } from '../data/publication-status-api.service';
import { PublicationState } from '../../../shared/models/publication-state.enum';
```

Inject the service and add the resource + computed:

```typescript
    private readonly publicationApi = inject(PublicationStatusApiService);

    private readonly publicationResource = resource({
        params: () => this.selectedWeekStartState(),
        loader: ({ params: week }) => this.loadPublicationState(week),
    });

    readonly publicationState = computed<PublicationState | undefined>(() => this.publicationResource.value());
    readonly canPublish = computed(() => this.publicationState() === PublicationState.draft);
```

Add the loader at the bottom of the class (next to `loadOrCreate`):

```typescript
    private async loadPublicationState(week: string): Promise<PublicationState | undefined> {
        try {
            const publication = await firstValueFrom(this.publicationApi.getByWeek(week));

            return publication.state;
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return undefined;
            }

            throw error;
        }
    }
```

> `isStatus` and `HTTP_NOT_FOUND` already exist in this store. Reload of this resource after a toggle is unnecessary — publishing happens on the dashboard, not here; the chip refreshes on the next week switch / page revisit.

- [ ] **Step 6: Weekly-prep page — Draft chip + Publish button**

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts` — add imports and expose `AppRoutes` for the router link:

```typescript
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
```

Add them to `imports`:

```typescript
imports: [FormsModule, TranslocoPipe, ProgressSpinnerModule, SelectModule, WeekGridComponent, ButtonModule, RouterLink, PublicationStateTagComponent],
```

Expose the route constant on the class:

```typescript
    protected readonly appRoutes = AppRoutes;
```

`features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html` — in the header, next to the title `<div>`, add the chip + publish button. Replace the `<header>` block's closing so it reads:

```html
  <header class="weekly-prep__header">
    <div>
      <h2>{{ 'weekSchedules.title' | transloco }}</h2>
      <p class="weekly-prep__subtitle">{{ 'weekSchedules.subtitle' | transloco }}</p>
    </div>
    @if (store.publicationState(); as state) {
      <div class="weekly-prep__publish">
        <app-publication-state-tag [state]="state" />
        @if (store.canPublish()) {
          <a
            pButton
            [label]="'weekSchedules.publishWeek' | transloco"
            [routerLink]="['/', appRoutes.publications]"
            [queryParams]="{ teacherId: store.selectedTeacherId(), week: store.selectedWeekStart(), publish: 1 }">
          </a>
        }
      </div>
    }
  </header>
```

> The link uses `AppRoutes.publications` + `queryParams` — no import from `features/publications`. `pButton` on an `<a>` renders a PrimeNG-styled link (matches how the mockup's "PUBLISH WEEK…" reads as a primary action); if the project standard is `<p-button>`, wrap navigation in a click handler calling `router.navigate` instead. The `publish: 1` flag triggers the dashboard's publish-dialog effect (task 12).

- [ ] **Step 7: i18n keys (both files, mirrored)**

`en.json` — add `publications.history.*`, `weekSchedules.publishWeek`, and the two nav links inside `shell.nav`:

```json
"publications": {
    "history": {
        "title": "Publications history",
        "subtitle": "Every published week, per teacher — with the last emailed Excel version.",
        "week": "Week",
        "teacher": "Teacher",
        "window": "Submission window",
        "status": "Status",
        "lastExcel": "Last Excel",
        "redownload": "Re-download",
        "viewDashboard": "View dashboard",
        "empty": "No publications yet.",
        "loadFailed": "Failed to load history."
    }
}
```
Add inside `weekSchedules`: `"publishWeek": "Publish week…"`; add inside `shell.nav`: `"publications": "Publications"`, `"history": "History"`.

`he.json` (mirrored):

```json
"publications": {
    "history": {
        "title": "היסטוריית פרסומים",
        "subtitle": "כל שבוע שפורסם, לפי מורה — עם גרסת האקסל האחרונה שנשלחה.",
        "week": "שבוע",
        "teacher": "מורה",
        "window": "חלון הגשה",
        "status": "סטטוס",
        "lastExcel": "אקסל אחרון",
        "redownload": "הורדה חוזרת",
        "viewDashboard": "צפייה בלוח",
        "empty": "אין פרסומים עדיין.",
        "loadFailed": "טעינת ההיסטוריה נכשלה."
    }
}
```
Add inside `weekSchedules`: `"publishWeek": "פרסום שבוע…"`; add inside `shell.nav`: `"publications": "פרסומים"`, `"history": "היסטוריה"`.

> Merge these into the `publications` object created in task 12 rather than duplicating it. Confirm the nav links were added to `admin-shell.component.html` in task 11; if not, add `<a [routerLink]>` entries mirroring the existing Teachers/Weekly-prep links.

- [ ] **Step 8: Build**

Run (in `client\`): `npm run build`

Expected: build succeeds, no template type errors.

- [ ] **Step 9: Browser walk** (Browser pane / launch config)

1. Weekly-prep → select a teacher + a week that has a Draft publication → **Draft** chip appears + **Publish week…** button.
2. Click **Publish week…** → routes to the dashboard with `publish=1` → the publish dialog opens automatically (task 12 effect).
3. Nav → **History** → table lists per-teacher rows with window (Jerusalem time), status tag, version pill / `-`.
4. A **Closed** row with a version → **Re-download** downloads the `.xlsx`; an **Open/Published** row → **View dashboard** navigates with `teacherId`+`week` and the dashboard shows that selection.
5. Empty DB → history shows the empty state.

- [ ] **Step 10: Commit**

```bash
git add client/src/app client/public/i18n
git commit -m "feat(client): publications history and weekly-prep publish entry point"
```

---

**Next:** [task-14-verification.md](task-14-verification.md)
