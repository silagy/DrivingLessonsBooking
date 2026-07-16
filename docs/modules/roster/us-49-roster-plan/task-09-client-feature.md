# Task 9 of 10: Client — roster feature (routes, i18n, data, store)

> Part of [US-49: Roster Module](README.md). Work on branch `51-us-49-roster-module`, commands from the repo root.

**Files:**
- Create: `client\src\app\features\roster\roster.routes.ts`
- Create: `client\src\app\features\roster\ui\pages\roster\roster.page.ts`, `.html`, `.scss`
- Create: `client\src\app\features\roster\domain\roster-entry-outcome.enum.ts`, `roster-row-failure-reason.enum.ts`, `teacher-filter-option.model.ts`
- Create: `client\src\app\features\roster\data\roster-api.service.ts`, `import-roster.response.ts`, `get-latest-roster-import.response.ts`, `item-for-find-students.response.ts`
- Create: `client\src\app\features\roster\state\roster.store.ts`
- Modify: `client\src\app\shared\config\app-routes.ts`, `client\src\app\features\admin-shell\admin.routes.ts`, `client\src\app\features\admin-shell\admin-shell.component.html`, `client\public\i18n\en.json`, `client\public\i18n\he.json`

Three commits: skeleton/nav, data layer, store — same split the prior modules used for client plumbing. Precedents to open before coding: `features\teachers\teachers.routes.ts` (routes shape), `features\publications\data\publications-api.service.ts` (API service), `features\publications\state\publications.store.ts` (`resource()` + 404-swallowing loader + `executeCommand` shape), `shared\models\publication-state.enum.ts` (camelCase string enums mirroring `JsonStringEnumConverter`).

- [ ] **Step 1: Routes, nav, page skeleton, i18n**

`shared\config\app-routes.ts` — add after `teachers`:

```typescript
roster: 'roster',
```

`features\roster\roster.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { RosterPage } from './ui/pages/roster/roster.page';

export default [{ path: '', component: RosterPage }] satisfies Routes;
```

`features\admin-shell\admin.routes.ts` — add a child between `teachers` and `weekSchedules`:

```typescript
{
    path: AppRoutes.roster,
    loadChildren: () => import('../roster/roster.routes'),
},
```

`features\admin-shell\admin-shell.component.html` — add a nav link between the Teachers and Weekly prep links (exact existing markup):

```html
<a
  [routerLink]="['/', appRoutes.roster]"
  routerLinkActive="shell__nav-link--active"
  class="shell__nav-link">
  {{ 'shell.nav.roster' | transloco }}
</a>
```

`features\roster\ui\pages\roster\roster.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-roster-page',
    imports: [TranslocoPipe],
    templateUrl: './roster.page.html',
    styleUrl: './roster.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterPage {}
```

`features\roster\ui\pages\roster\roster.page.html`:

```html
<div class="roster">
    <header class="roster__header">
        <div>
            <h2 class="roster__title">{{ 'roster.title' | transloco }}</h2>
            <p class="roster__subtitle">{{ 'roster.subtitle' | transloco }}</p>
        </div>
    </header>
</div>
```

`features\roster\ui\pages\roster\roster.page.scss` (matches `cars-and-teachers.page.scss` header conventions; task 10 extends this file):

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
```

i18n — both files in this commit (rule: `en.json` and `he.json` ship together). Namespaces in these files are camelCase (`publications`, `weekSchedules`) — follow that, not the PascalCase examples in the rules doc.

`client\public\i18n\en.json` — inside `shell.nav`, between `"teachers"` and `"weeklyPrep"`:

```json
"roster": "Roster",
```

and a full `roster` namespace after the `teachers` block:

```json
"roster": {
    "title": "Student roster",
    "subtitle": "Upload the school's CSV to sync students, their teachers, and cars.",
    "uploadCsv": "Upload CSV",
    "processed": "Processed",
    "rowsSummary": "{{rows}} rows · {{time}}",
    "stats": {
        "added": "Added",
        "updated": "Updated",
        "deactivated": "Deactivated (missing from file)",
        "failed": "Failed rows"
    },
    "table": {
        "name": "Name",
        "nationalId": "National ID",
        "phone": "Phone",
        "car": "Car",
        "teacher": "Teacher",
        "result": "Result"
    },
    "badge": {
        "added": "Added",
        "updated": "Updated"
    },
    "inactive": "Inactive",
    "filter": {
        "all": "All"
    },
    "failedRows": {
        "title": "Failed rows",
        "row": "Row {{number}} · {{name}}"
    },
    "failureReasons": {
        "invalidNationalId": "Invalid national ID",
        "duplicateNationalId": "Duplicate national ID in the file",
        "missingName": "Missing name",
        "missingPhone": "Missing phone",
        "missingTeacher": "Missing teacher",
        "missingCar": "Missing car",
        "unknownTeacher": "Teacher not found in the system",
        "unknownCar": "Car not found in the system",
        "invalidStartDate": "Invalid start date"
    },
    "imported": "Roster imported",
    "loadFailed": "Failed to load the roster.",
    "empty": "No students yet. Upload the school's CSV to build the roster."
}
```

`client\public\i18n\he.json` — inside `shell.nav`, between `"teachers"` and `"weeklyPrep"`:

```json
"roster": "תלמידים",
```

and the mirrored namespace after the `teachers` block:

```json
"roster": {
    "title": "רשימת תלמידים",
    "subtitle": "העלו את קובץ ה-CSV של בית הספר כדי לסנכרן תלמידים, מורים ורכבים.",
    "uploadCsv": "העלאת קובץ CSV",
    "processed": "נקלט",
    "rowsSummary": "{{rows}} שורות · {{time}}",
    "stats": {
        "added": "נוספו",
        "updated": "עודכנו",
        "deactivated": "הושבתו (חסרים בקובץ)",
        "failed": "שורות שנכשלו"
    },
    "table": {
        "name": "שם",
        "nationalId": "תעודת זהות",
        "phone": "טלפון",
        "car": "רכב",
        "teacher": "מורה",
        "result": "תוצאה"
    },
    "badge": {
        "added": "נוסף",
        "updated": "עודכן"
    },
    "inactive": "לא פעיל",
    "filter": {
        "all": "הכול"
    },
    "failedRows": {
        "title": "שורות שנכשלו",
        "row": "שורה {{number}} · {{name}}"
    },
    "failureReasons": {
        "invalidNationalId": "תעודת זהות לא תקינה",
        "duplicateNationalId": "תעודת זהות כפולה בקובץ",
        "missingName": "שם חסר",
        "missingPhone": "טלפון חסר",
        "missingTeacher": "מורה חסר",
        "missingCar": "רכב חסר",
        "unknownTeacher": "מורה לא קיים במערכת",
        "unknownCar": "רכב לא קיים במערכת",
        "invalidStartDate": "תאריך התחלה לא תקין"
    },
    "imported": "הרשימה יובאה",
    "loadFailed": "טעינת רשימת התלמידים נכשלה.",
    "empty": "אין תלמידים עדיין. העלו את קובץ ה-CSV של בית הספר כדי לבנות את הרשימה."
}
```

Run (in `client\`): `npm run build` — success. Nav shows the new link; `/roster` renders the title.

```bash
git add client/src/app client/public/i18n
git commit -m "feat(client): add roster feature skeleton and navigation"
```

- [ ] **Step 2: Domain enums + data layer**

Enum values are camelCase strings — the backend serializes enums with `JsonStringEnumConverter` (same convention as `shared\models\publication-state.enum.ts`).

`features\roster\domain\roster-entry-outcome.enum.ts`:

```typescript
export enum RosterEntryOutcome {
    added = 'added',
    updated = 'updated',
    deactivated = 'deactivated',
}
```

`features\roster\domain\roster-row-failure-reason.enum.ts`:

```typescript
export enum RosterRowFailureReason {
    invalidNationalId = 'invalidNationalId',
    duplicateNationalId = 'duplicateNationalId',
    missingName = 'missingName',
    missingPhone = 'missingPhone',
    missingTeacher = 'missingTeacher',
    missingCar = 'missingCar',
    unknownTeacher = 'unknownTeacher',
    unknownCar = 'unknownCar',
    invalidStartDate = 'invalidStartDate',
}
```

`features\roster\data\import-roster.response.ts`:

```typescript
export interface ImportRosterResponse {
    rosterImportId: string;
    added: number;
    updated: number;
    deactivated: number;
    failed: number;
}
```

`features\roster\data\get-latest-roster-import.response.ts`:

```typescript
import { RosterEntryOutcome } from '../domain/roster-entry-outcome.enum';
import { RosterRowFailureReason } from '../domain/roster-row-failure-reason.enum';

export interface GetLatestRosterImportResponse {
    id: string;
    fileName: string;
    importedAtUtc: string;
    added: number;
    updated: number;
    deactivated: number;
    failed: number;
    entries: EntryForGetLatestRosterImportResponse[];
    failures: FailureForGetLatestRosterImportResponse[];
}

export interface EntryForGetLatestRosterImportResponse {
    nationalId: string;
    outcome: RosterEntryOutcome;
}

export interface FailureForGetLatestRosterImportResponse {
    rowNumber: number;
    studentName: string;
    reason: RosterRowFailureReason;
}
```

`features\roster\data\item-for-find-students.response.ts`:

```typescript
export interface ItemForFindStudentsResponse {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    isActive: boolean;
}
```

> Cross-check the interface names and property lists against the backend response DTOs from tasks 5–8 (`ImportRosterResponse`, `GetLatestRosterImportResponse`, `ItemForFindStudentsResponse`) — client DTOs are named identically to their backend counterparts, and the backend classes win on any drift.

`features\roster\data\roster-api.service.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetLatestRosterImportResponse } from './get-latest-roster-import.response';
import { ImportRosterResponse } from './import-roster.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class RosterApiService {
    private readonly http = inject(HttpClient);
    private readonly importsUrl = 'api/roster-imports';
    private readonly studentsUrl = 'api/students';

    importRoster(file: File): Observable<ImportRosterResponse> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<ImportRosterResponse>(this.importsUrl, formData);
    }

    getLatestImport(): Observable<GetLatestRosterImportResponse> {
        return this.http.get<GetLatestRosterImportResponse>(`${this.importsUrl}/latest`);
    }

    findStudents(teacherId?: string): Observable<ItemForFindStudentsResponse[]> {
        let params = new HttpParams();

        if (teacherId) {
            params = params.set('teacherId', teacherId);
        }

        return this.http.get<ItemForFindStudentsResponse[]>(`${this.studentsUrl}/find`, { params });
    }
}
```

> Do not set a `Content-Type` header on `importRoster` — `HttpClient` derives the multipart boundary from the `FormData` body automatically; setting it manually breaks the upload.

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app/features/roster
git commit -m "feat(client): add roster api service and dtos"
```

- [ ] **Step 3: Signal store**

`features\roster\domain\teacher-filter-option.model.ts` (same shape as `publications\domain\teacher-option.model.ts`):

```typescript
export interface TeacherFilterOption {
    id: string;
    name: string;
}
```

`features\roster\state\roster.store.ts` — modeled on `PublicationsStore`: a `resource()` per query, the latest-import loader swallows 404 into `undefined` (never imported), `upload` follows the `executeCommand` shape but reloads both resources:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { GetLatestRosterImportResponse } from '../data/get-latest-roster-import.response';
import { ItemForFindStudentsResponse } from '../data/item-for-find-students.response';
import { RosterApiService } from '../data/roster-api.service';
import { RosterEntryOutcome } from '../domain/roster-entry-outcome.enum';
import { TeacherFilterOption } from '../domain/teacher-filter-option.model';

const HTTP_NOT_FOUND = 404;

@Injectable({ providedIn: 'root' })
export class RosterStore {
    private readonly api = inject(RosterApiService);
    private readonly toast = inject(ToastService);

    private readonly selectedTeacherIdState = signal<string | null>(null);
    private readonly uploadingState = signal(false);

    private readonly latestImportResource = resource({
        loader: () => this.loadLatestImport(),
    });

    private readonly studentsResource = resource({
        loader: () => firstValueFrom(this.api.findStudents()),
    });

    readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();
    readonly uploading = this.uploadingState.asReadonly();

    readonly latestImport = computed<GetLatestRosterImportResponse | undefined>(() =>
        this.latestImportResource.value(),
    );

    readonly students = computed<ItemForFindStudentsResponse[]>(() => this.studentsResource.value() ?? []);

    readonly filteredStudents = computed<ItemForFindStudentsResponse[]>(() => {
        const teacherId = this.selectedTeacherIdState();
        const students = this.students();

        return teacherId ? students.filter((student) => student.teacherId === teacherId) : students;
    });

    readonly teacherFilterOptions = computed<TeacherFilterOption[]>(() => {
        const distinct = new Map<string, string>();

        for (const student of this.students()) {
            distinct.set(student.teacherId, student.teacherName);
        }

        return [...distinct.entries()]
            .map(([id, name]) => ({ id, name }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });

    readonly badgeByNationalId = computed<Map<string, RosterEntryOutcome>>(() => {
        const badges = new Map<string, RosterEntryOutcome>();

        for (const entry of this.latestImport()?.entries ?? []) {
            if (entry.outcome !== RosterEntryOutcome.deactivated) {
                badges.set(entry.nationalId, entry.outcome);
            }
        }

        return badges;
    });

    readonly failedRows = computed(() => this.latestImport()?.failures ?? []);

    readonly isLoading = computed(
        () => this.latestImportResource.isLoading() || this.studentsResource.isLoading(),
    );

    readonly loadError = computed(() => {
        const failed = this.latestImportResource.error() || this.studentsResource.error();

        return failed ? 'roster.loadFailed' : undefined;
    });

    readonly isEmpty = computed(
        () => !this.isLoading() && !this.students().length && this.latestImport() === undefined,
    );

    selectTeacher(teacherId: string | null): void {
        this.selectedTeacherIdState.set(teacherId);
    }

    async upload(file: File): Promise<void> {
        this.uploadingState.set(true);

        try {
            await firstValueFrom(this.api.importRoster(file));
            this.toast.success('roster.imported');
            this.latestImportResource.reload();
            this.studentsResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.uploadingState.set(false);
        }
    }

    private async loadLatestImport(): Promise<GetLatestRosterImportResponse | undefined> {
        try {
            return await firstValueFrom(this.api.getLatestImport());
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return undefined;
            }

            throw error;
        }
    }
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
```

> The teacher filter is client-side only (`filteredStudents`); `findStudents(teacherId?)` keeps the query-string capability for parity with the backend contract, but the store loads the full roster once and filters in memory — a school roster is small.

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app/features/roster
git commit -m "feat(client): add roster signals store"
```

---

**Next:** [task-10-client-ui-and-verification.md](task-10-client-ui-and-verification.md)
