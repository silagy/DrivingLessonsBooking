# Task 10 of 10: Client — roster page UI + end-to-end verification

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `client\src\app\features\roster\domain\jerusalem-time.ts`
- Create: `client\src\app\features\roster\ui\components\import-result-badge\import-result-badge.component.ts`
- Create: `client\src\app\features\roster\ui\components\roster-stat-tile\roster-stat-tile.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\roster\ui\components\last-import-card\last-import-card.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\roster\ui\components\failed-rows-panel\failed-rows-panel.component.ts`, `.html`, `.scss`
- Modify: `client\src\app\features\roster\ui\pages\roster\roster.page.ts`, `.html`, `.scss`

Precedents: `shared\components\publication-state-tag\publication-state-tag.component.ts` (single-file tag wrapper), `features\publications\ui\components\publication-stat-chip\` (stat tile shape), `features\publications\ui\pages\publications-history\` (`p-table` markup with `pTemplate` header/body), `features\teachers\ui\components\teacher-card\` (dumb component with `input()`/`computed()`). Implement with the app tokens from `client\src\styles\_tokens.scss` — not the mockup's palette.

- [ ] **Step 1: Import-result badge (dumb, single file)**

`features\roster\ui\components\import-result-badge\import-result-badge.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { RosterEntryOutcome } from '../../../domain/roster-entry-outcome.enum';

type BadgeSeverity = 'success' | 'info';

const OUTCOME_SEVERITY: Partial<Record<RosterEntryOutcome, BadgeSeverity>> = {
    [RosterEntryOutcome.added]: 'success',
    [RosterEntryOutcome.updated]: 'info',
};

@Component({
    selector: 'app-import-result-badge',
    imports: [TagModule, TranslocoPipe],
    template: `<p-tag [severity]="severity()" [value]="'roster.badge.' + outcome() | transloco" />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportResultBadgeComponent {
    readonly outcome = input.required<RosterEntryOutcome>();

    protected readonly severity = computed<BadgeSeverity>(() => OUTCOME_SEVERITY[this.outcome()] ?? 'info');
}
```

- [ ] **Step 2: Stat tile (dumb)**

`features\roster\ui\components\roster-stat-tile\roster-stat-tile.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatTileTone = 'success' | 'info' | 'neutral' | 'danger';

@Component({
    selector: 'app-roster-stat-tile',
    templateUrl: './roster-stat-tile.component.html',
    styleUrl: './roster-stat-tile.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterStatTileComponent {
    readonly value = input.required<number>();
    readonly label = input.required<string>();
    readonly tone = input.required<StatTileTone>();

    protected readonly rootClass = computed(() => `stat-tile stat-tile--${this.tone()}`);
}
```

`roster-stat-tile.component.html`:

```html
<div [class]="rootClass()">
    <span class="stat-tile__value"><bdi>{{ value() }}</bdi></span>
    <span class="stat-tile__label">{{ label() }}</span>
</div>
```

`roster-stat-tile.component.scss`:

```scss
.stat-tile {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 14px 18px;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: 10px;
    box-shadow: var(--app-shadow-card);
}

.stat-tile__value {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.5rem;
    color: var(--app-ink);
    font-variant-numeric: tabular-nums;
}

.stat-tile__label {
    font-size: 0.78rem;
    color: var(--app-text-secondary);
}

.stat-tile--success .stat-tile__value {
    color: var(--p-green-600);
}

.stat-tile--info .stat-tile__value {
    color: var(--p-blue-600);
}

.stat-tile--neutral .stat-tile__value {
    color: var(--app-text-secondary);
}

.stat-tile--danger .stat-tile__value {
    color: var(--p-red-600);
}
```

> `--p-green-*`, `--p-blue-*`, `--p-red-*` come from the Aura base primitives that `AppPreset` extends (`--p-red-500` is already used in `cars-and-teachers.page.scss`). Verify the shades render in the browser; if a primitive is missing, fall back to the nearest severity token PrimeNG exposes (e.g. `--p-tag-success-color`).

- [ ] **Step 3: Last-import card (dumb)**

`features\roster\ui\components\last-import-card\last-import-card.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';

@Component({
    selector: 'app-last-import-card',
    imports: [TranslocoPipe, TagModule],
    templateUrl: './last-import-card.component.html',
    styleUrl: './last-import-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LastImportCardComponent {
    readonly fileName = input.required<string>();
    readonly rows = input.required<number>();
    readonly time = input.required<string>();
}
```

`last-import-card.component.html`:

```html
<div class="import-card">
    <i class="pi pi-file import-card__icon" aria-hidden="true"></i>
    <div class="import-card__details">
        <span class="import-card__name"><bdi>{{ fileName() }}</bdi></span>
        <span class="import-card__summary">
            {{ 'roster.rowsSummary' | transloco: { rows: rows(), time: time() } }}
        </span>
    </div>
    <p-tag severity="success" [value]="'roster.processed' | transloco" />
</div>
```

`last-import-card.component.scss`:

```scss
.import-card {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 14px 18px;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);
}

.import-card__icon {
    font-size: 1.25rem;
    color: var(--p-primary-color);
}

.import-card__details {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    min-width: 0;
}

.import-card__name {
    font-weight: 600;
    color: var(--app-ink);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.import-card__summary {
    font-size: 0.78rem;
    color: var(--app-text-secondary);
}
```

- [ ] **Step 4: Failed-rows panel (dumb)**

`features\roster\ui\components\failed-rows-panel\failed-rows-panel.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { FailureForGetLatestRosterImportResponse } from '../../../data/get-latest-roster-import.response';

@Component({
    selector: 'app-failed-rows-panel',
    imports: [TranslocoPipe],
    templateUrl: './failed-rows-panel.component.html',
    styleUrl: './failed-rows-panel.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FailedRowsPanelComponent {
    readonly failures = input.required<FailureForGetLatestRosterImportResponse[]>();
}
```

`failed-rows-panel.component.html`:

```html
<aside class="failed-panel">
    <header class="failed-panel__header">
        <span>{{ 'roster.failedRows.title' | transloco }}</span>
        <span class="failed-panel__count"><bdi>{{ failures().length }}</bdi></span>
    </header>
    <ul class="failed-panel__list">
        @for (failure of failures(); track failure.rowNumber) {
            <li class="failed-panel__row">
                <span class="failed-panel__label">
                    {{ 'roster.failedRows.row' | transloco: { number: failure.rowNumber, name: failure.studentName } }}
                </span>
                <span class="failed-panel__reason">{{ 'roster.failureReasons.' + failure.reason | transloco }}</span>
            </li>
        }
    </ul>
</aside>
```

`failed-rows-panel.component.scss`:

```scss
.failed-panel {
    background: var(--app-bg-card);
    border: 1px solid var(--p-red-200);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);
    overflow: hidden;
    align-self: start;
}

.failed-panel__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    padding: 12px 18px;
    background: var(--p-red-50);
    color: var(--p-red-700);
    font-weight: 600;
    font-size: 0.84rem;
    border-block-end: 1px solid var(--p-red-100);
}

.failed-panel__count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 22px;
    height: 22px;
    padding-inline: 6px;
    border-radius: 999px;
    background: var(--p-red-100);
    font-variant-numeric: tabular-nums;
}

.failed-panel__list {
    list-style: none;
    margin: 0;
    padding: 6px 0;
}

.failed-panel__row {
    display: flex;
    flex-direction: column;
    gap: 2px;
    padding: 10px 18px;

    &:not(:last-child) {
        border-block-end: 1px solid var(--app-border);
    }
}

.failed-panel__label {
    font-size: 0.84rem;
    font-weight: 500;
    color: var(--app-ink);
}

.failed-panel__reason {
    font-size: 0.78rem;
    color: var(--p-red-600);
}
```

- [ ] **Step 5: Roster page (smart) — full rewrite of the task-9 skeleton**

`features\roster\domain\jerusalem-time.ts` — display helper duplicated from `features\publications\domain\jerusalem-time.ts` (features never import from each other; only the format half is needed):

```typescript
export function formatInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: 'Asia/Jerusalem',
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}
```

`features\roster\ui\pages\roster\roster.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { LanguageService } from '../../../../../core/language.service';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { TeacherFilterOption } from '../../../domain/teacher-filter-option.model';
import { RosterStore } from '../../../state/roster.store';
import { FailedRowsPanelComponent } from '../../components/failed-rows-panel/failed-rows-panel.component';
import { ImportResultBadgeComponent } from '../../components/import-result-badge/import-result-badge.component';
import { LastImportCardComponent } from '../../components/last-import-card/last-import-card.component';
import { RosterStatTileComponent } from '../../components/roster-stat-tile/roster-stat-tile.component';

const ALL_TEACHERS = 'all';

@Component({
    selector: 'app-roster-page',
    imports: [
        FormsModule,
        TranslocoPipe,
        ButtonModule,
        ProgressSpinnerModule,
        SelectButtonModule,
        TableModule,
        TagModule,
        FailedRowsPanelComponent,
        ImportResultBadgeComponent,
        LastImportCardComponent,
        RosterStatTileComponent,
    ],
    templateUrl: './roster.page.html',
    styleUrl: './roster.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterPage {
    protected readonly store = inject(RosterStore);
    private readonly language = inject(LanguageService);

    protected readonly allTeachers = ALL_TEACHERS;

    protected readonly filterOptions = computed<TeacherFilterOption[]>(() => [
        { id: ALL_TEACHERS, name: '' },
        ...this.store.teacherFilterOptions(),
    ]);

    protected readonly selectedFilter = computed(() => this.store.selectedTeacherId() ?? ALL_TEACHERS);

    protected readonly processedRows = computed(() => {
        const latest = this.store.latestImport();

        return latest ? latest.added + latest.updated + latest.deactivated + latest.failed : 0;
    });

    protected readonly importedAtLabel = computed(() => {
        const latest = this.store.latestImport();

        return latest ? formatInstantInJerusalem(latest.importedAtUtc, this.language.lang()) : '';
    });

    protected onFilterChange(value: string): void {
        this.store.selectTeacher(value === ALL_TEACHERS ? null : value);
    }

    protected async onFileSelected(fileInput: HTMLInputElement): Promise<void> {
        const file = fileInput.files?.[0];

        if (!file) {
            return;
        }

        await this.store.upload(file);
        fileInput.value = '';
    }
}
```

`features\roster\ui\pages\roster\roster.page.html`:

```html
<div class="roster">
    <header class="roster__header">
        <div>
            <h2 class="roster__title">{{ 'roster.title' | transloco }}</h2>
            <p class="roster__subtitle">{{ 'roster.subtitle' | transloco }}</p>
        </div>
        <div class="roster__actions">
            <input
                #fileInput
                type="file"
                accept=".csv,text/csv"
                hidden
                (change)="onFileSelected(fileInput)" />
            <p-button
                [label]="'roster.uploadCsv' | transloco"
                icon="pi pi-upload"
                [loading]="store.uploading()"
                (onClick)="fileInput.click()" />
        </div>
    </header>

    @if (store.isLoading()) {
        <div class="roster__state"><p-progressSpinner /></div>
    } @else if (store.loadError(); as errorKey) {
        <div class="roster__state">{{ errorKey | transloco }}</div>
    } @else if (store.isEmpty()) {
        <div class="roster__empty">
            <p>{{ 'roster.empty' | transloco }}</p>
        </div>
    } @else {
        @if (store.latestImport(); as latestImport) {
            <app-last-import-card
                class="roster__import-card"
                [fileName]="latestImport.fileName"
                [rows]="processedRows()"
                [time]="importedAtLabel()" />

            <div class="roster__stats">
                <app-roster-stat-tile
                    [value]="latestImport.added"
                    [label]="'roster.stats.added' | transloco"
                    tone="success" />
                <app-roster-stat-tile
                    [value]="latestImport.updated"
                    [label]="'roster.stats.updated' | transloco"
                    tone="info" />
                <app-roster-stat-tile
                    [value]="latestImport.deactivated"
                    [label]="'roster.stats.deactivated' | transloco"
                    tone="neutral" />
                <app-roster-stat-tile
                    [value]="latestImport.failed"
                    [label]="'roster.stats.failed' | transloco"
                    tone="danger" />
            </div>
        }

        <div class="roster__content" [class.roster__content--single]="!store.failedRows().length">
            <section class="roster__table-card">
                <p-selectbutton
                    class="roster__filter"
                    [options]="filterOptions()"
                    optionValue="id"
                    [allowEmpty]="false"
                    [ngModel]="selectedFilter()"
                    (ngModelChange)="onFilterChange($event)">
                    <ng-template pTemplate="item" let-option>
                        @if (option.id === allTeachers) {
                            {{ 'roster.filter.all' | transloco }}
                        } @else {
                            {{ option.name }}
                        }
                    </ng-template>
                </p-selectbutton>

                <p-table [value]="store.filteredStudents()" styleClass="roster__table">
                    <ng-template pTemplate="header">
                        <tr>
                            <th>{{ 'roster.table.name' | transloco }}</th>
                            <th>{{ 'roster.table.nationalId' | transloco }}</th>
                            <th>{{ 'roster.table.phone' | transloco }}</th>
                            <th>{{ 'roster.table.car' | transloco }}</th>
                            <th>{{ 'roster.table.teacher' | transloco }}</th>
                            <th>{{ 'roster.table.result' | transloco }}</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-row>
                        <tr [class.roster__row--inactive]="!row.isActive">
                            <td>
                                <span class="roster__name">
                                    {{ row.name }}
                                    @if (!row.isActive) {
                                        <p-tag severity="secondary" [value]="'roster.inactive' | transloco" />
                                    }
                                </span>
                            </td>
                            <td class="roster__ltr-cell">{{ row.nationalId }}</td>
                            <td class="roster__ltr-cell">{{ row.phone }}</td>
                            <td>{{ row.carName }}</td>
                            <td>{{ row.teacherName }}</td>
                            <td>
                                @if (store.badgeByNationalId().get(row.nationalId); as outcome) {
                                    <app-import-result-badge [outcome]="outcome" />
                                }
                            </td>
                        </tr>
                    </ng-template>
                </p-table>
            </section>

            @if (store.failedRows().length) {
                <app-failed-rows-panel [failures]="store.failedRows()" />
            }
        </div>
    }
</div>
```

> If `pTemplate="item"` does not render inside `p-selectbutton` on the installed PrimeNG version, switch to the template-reference form the current docs use: `<ng-template #item let-option>`. `p-table` keeps `pTemplate` — that is what `publications-history.page.html` uses today.

`features\roster\ui\pages\roster\roster.page.scss`:

```scss
.roster__header {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1.5rem;
}

.roster__title {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 500;
    font-size: 1.625rem;
    color: var(--app-ink);
    letter-spacing: -0.01em;
}

.roster__subtitle {
    margin: 0.25rem 0 0;
    font-size: 0.84rem;
    color: var(--app-text-secondary);
}

.roster__actions {
    display: flex;
    gap: 0.625rem;
}

.roster__import-card {
    display: block;
    margin-block-start: 1.125rem;
}

.roster__stats {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.875rem;
    margin-block-start: 0.875rem;
}

.roster__content {
    display: grid;
    grid-template-columns: 1.55fr 1fr;
    gap: 0.875rem;
    align-items: start;
    margin-block-start: 0.875rem;
}

.roster__content--single {
    grid-template-columns: 1fr;
}

.roster__table-card {
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);
    padding: 1rem 1.25rem;
}

.roster__filter {
    display: inline-flex;
    margin-block-end: 0.75rem;
}

.roster__name {
    display: inline-flex;
    align-items: center;
    gap: 8px;
}

.roster__ltr-cell {
    direction: ltr;
    unicode-bidi: isolate;
    font-variant-numeric: tabular-nums;
}

.roster__row--inactive td {
    color: var(--app-text-muted);
}

.roster__state {
    display: flex;
    justify-content: center;
    padding-block: 3rem;
}

.roster__empty {
    margin-block-start: 1.375rem;
    padding: 2.5rem;
    text-align: center;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);

    p {
        margin: 0;
        color: var(--app-text-secondary);
        font-size: 0.85rem;
    }
}

@media screen and (max-width: 992px) {
    .roster__stats {
        grid-template-columns: repeat(2, 1fr);
    }

    .roster__content {
        grid-template-columns: 1fr;
    }
}
```

- [ ] **Step 6: Build, commit**

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app/features/roster
git commit -m "feat(client): add roster admin page"
```

- [ ] **Step 7: Full backend check**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```

Expected: build clean, all tests PASS.

- [ ] **Step 8: Migration on a fresh database**

```bash
docker compose down -v
docker compose up -d postgres
dotnet ef database update --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: all migrations apply, including the roster migration from task 6 (they also auto-apply on app startup — this step proves a fresh database builds from zero).

- [ ] **Step 9: Scalar smoke test**

Run the API in Development, open `/scalar/v1`, authorize with a bearer token from `POST /api/auth/login`, then:

1. `POST api/roster-imports` (multipart/form-data, field name `file`) with the sample Hebrew CSV from task 8 → **201** with `{ rosterImportId, added, updated, deactivated, failed }` and non-zero `added`.
2. `GET api/roster-imports/latest` → **200** with `fileName`, `importedAtUtc`, the same counters, `entries` (camelCase `outcome` strings), and `failures` (camelCase `reason` strings).
3. `GET api/students/find` → **200** array with `nationalId`, `name`, `phone`, `teacherId`, `teacherName`, `carId`, `carName`, `isActive: true`; `GET api/students/find?teacherId={guid}` → only that teacher's students.
4. Re-upload the same CSV minus one row → **201** with `deactivated: 1`; `GET api/students/find` shows that student with `isActive: false`.

- [ ] **Step 10: Run the app and verify in the browser** (use the Browser pane / launch config, PrimeNG overlay rAF caveat noted in project memory)

Run (in `client\`): `npm run build` — success. With Postgres up and the app running, sign in as admin and verify:

1. Nav shows "Roster" between "Cars & teachers" and "Weekly prep" → open it → on a fresh database the empty state (`roster.empty`) renders — no import card, no tiles, no table.
2. Upload the sample Hebrew CSV via the Upload CSV button → success toast, last-import card appears (file icon, filename, rows summary with Asia/Jerusalem time, "Processed" tag), four stat tiles show the counters (Added green, Updated blue, Deactivated neutral, Failed red).
3. Student table lists every row with Name, National ID, Phone, Car, Teacher; every imported student shows an Added badge; national-ID and phone cells stay LTR.
4. Failed-rows panel appears on the inline-end side only when the CSV has bad rows — each row shows "Row {n} · {name}" plus the translated reason.
5. Teacher filter: select-button shows All + distinct teacher names; picking a teacher filters the table client-side; All restores the full list.
6. Re-select the same file in the file picker → the upload fires again (input value reset); re-uploading the CSV minus one row updates the tiles (`deactivated: 1`) and the removed student shows dimmed with an Inactive tag.
7. Refresh the page → the last-import card and table persist (server-backed).
8. **Hebrew/RTL**: toggle language → `dir="rtl"`, nav shows "תלמידים", title "רשימת תלמידים", upload button at the inline-end side, stats/table/failed-panel mirror correctly, national IDs and phones render LTR-isolated inside RTL rows. Repeat the checks in English. Layout mirror-correct — this is part of acceptance (client-i18n rule).
9. Take a screenshot of the Hebrew page after a successful upload as proof.

- [ ] **Step 11: Push and open the PR**

```bash
git push -u origin 51-us-49-roster-module
gh pr create --title "US-49: Roster module" --body "Closes #51

## Summary
- Roster import pipeline (CSV upload, per-row outcomes, deactivation of missing students, failure reasons) through domain/application/infrastructure/API
- Client roster feature: upload flow, last-import card, stat tiles, student table with import badges and teacher filter, failed-rows panel (EN + HE RTL)

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done)

- **Spec coverage**: US-49 client surface → Task 9 (routes/nav/i18n skeleton, data layer, signal store) + Task 10 (page UI, dumb components, verification). Upload → `POST api/roster-imports`; latest-import card/tiles/badges/failures → `GET api/roster-imports/latest` (404 = never imported = `undefined`); table → `GET api/students/find`; teacher filter is client-side per the approved plan.
- **Type consistency**: client DTO names mirror the backend classes (`ImportRosterResponse`, `GetLatestRosterImportResponse` with `EntryFor…`/`FailureFor…` nested interfaces, `ItemForFindStudentsResponse`); `RosterEntryOutcome`/`RosterRowFailureReason` are camelCase string enums matching `JsonStringEnumConverter` output, same convention as `PublicationState`.
- **Known judgment calls** (flagged inline with ">"): `p-selectbutton` is new to this app (no prior usage to copy) — its item-template syntax (`pTemplate="item"` vs `#item`) must be checked against the installed PrimeNG version; `--p-green-*`/`--p-blue-*` shades assume the Aura base primitives survive the `AppPreset` override (red demonstrably does); `formatInstantInJerusalem` is duplicated into `roster\domain` because features must not import from each other — existing code always wins over this plan's snippets.
