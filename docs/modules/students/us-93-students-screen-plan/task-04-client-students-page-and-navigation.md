# Task 4 of 6: Students page, navigation and the Roster behind "Import Roster" (client)

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires task 3 committed. Work on branch `93-students-screen`. Read README decisions 11 to 16 and 20 to 23, and the README "Design" section (frames 1a, 2a, 2b, 2g to 2n) first.

**Files:**
- Create: `client\src\app\features\students\students.routes.ts`
- Create: `client\src\app\features\students\ui\components\transmission-tag\transmission-tag.component.ts`, `.scss`
- Create: `client\src\app\features\students\ui\pages\students\students.page.ts`, `.html`, `.scss`
- Modify: `client\src\app\shared\config\app-routes.ts`
- Modify: `client\src\app\features\admin-shell\admin.routes.ts`
- Modify: `client\src\app\features\admin-shell\domain\navigation.ts`
- Test: `client\src\app\features\admin-shell\domain\navigation.spec.ts`
- Modify: `client\src\app\features\roster\ui\pages\roster\roster.page.html`, `.ts`, `.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `StudentsStore` and the domain types from task 3 (`StudentRow`, `StudentStatusFilter`, `TeacherOption`, `Transmission`), `administratorGuard` (`core\role.guards.ts`), `InitialsPipe` (`shared\pipes\initials.pipe.ts`), `AppRoutes`, `navigationFor(role)` / `NavigationItem` (`features\admin-shell\domain\navigation.ts`).
- Produces (task 5 and 6 rely on these):
  - `AppRoutes.students = 'students'`, `AppRoutes.rosterImport = 'import'`; `AppRoutes.roster = 'roster'` stays for the redirect.
  - Routes: `/students` → `StudentsPage` (lazy `features\students\students.routes.ts`), `/students/import` → the Roster page (lazy `features\roster\roster.routes.ts`), `/roster` → redirect to `/students/import`. All Administrator-only (`canMatch: [administratorGuard]`).
  - Navigation item `shell.nav.students` → `['/', 'students']`, `exact: false`, in the Roster item's old place.
  - `StudentsPage` (`app-students-page`) providing `StudentsStore`; protected members `store`, `importRosterLink`, `statusOptions`, `teacherFilterOptions`, `selectedTeacher`, `allTeachers`; methods `onTeacherFilter`, `onStatusFilter`, `onSearch`. The header and empty state each hold a `students__actions` / `students__empty-actions` container that task 5 adds "Add Student" to.
  - `TransmissionTagComponent` (`app-transmission-tag`): `transmission = input.required<Transmission>()`, `muted = input(false)`.
  - Translation keys: `shell.nav.students`, `roster.back`, new values for `roster.title` / `roster.subtitle`, and the `students` block listed in step 8.

**Why:** AC "Students list with national ID, name, phone, Teacher, Car and active state; filters by Teacher and by active state" and "Students entry in the admin shell navigation, Administrator-only", design section 1 (recommended variant 1a) and section 2.

**Design (frames 2a, 2b, 2g to 2n; layout spelled out so this task needs no MCP):**
- Header (`AuHeader`): title "תלמידים" 26px / 600, subtitle below 14px secondary text, line height 1.5. Actions at the inline end: secondary "ייבוא רשימת תלמידים" with an upload icon; task 5 adds the primary "הוספת תלמיד" after it.
- Filters row, 20px below the header, aligned to the bottom: Teacher `Select` (label "מורה", 220px, "כל המורים" first, then every Teacher by name); status `SelectButton` (label "מצב", 300px, pill segments "פעילים" / "לא פעילים" / "הכול", default "פעילים", never empty); search field pushed to the inline end (label "חיפוש", 300px, search icon at the inline start, placeholder "שם או תעודת זהות"). Hidden in the empty state (2k).
- Card (radius 14px, border, card shadow), 16px below the filters. Table header cells 12px / 700 secondary text on the page background. Columns: Name (30px avatar with initials, name 600, "חדש" tag for a new row) · National ID (LTR, tabular digits, secondary color) · Phone (LTR) · Teacher · Car (name, then the transmission tag) · Status (Tag "פעיל" green, "לא פעיל" grey). No row-actions column in #93.
- Inactive rows: page-background tint, every text and the avatar in the muted color, transmission tag muted, listed last (2b).
- New row (3h): `--p-sky-50` tint, listed first, "חדש" tag in the Administrator tag tones.
- Footer under the table: "{n} תלמידים" (12.5px, muted).
- Loading (2i): spinner + "טוען תלמידים..." centered, ~320px high. Error (2j): red alert icon + "לא הצלחנו לטעון את התלמידים." + text button "ניסיון חוזר". Empty (2k): 44px round icon (user), title "עדיין אין תלמידים", hint "הוסיפו תלמיד ידנית, או ייבאו את רשימת התלמידים מקובץ.", actions "ייבוא רשימת תלמידים" (secondary, small) and, from task 5, "הוספת תלמיד" (small). Empty after filter (2l): the table header, then a search icon, "אין תלמידים שמתאימים לסינון", "נסו מורה אחר או מצב אחר.", secondary small "ניקוי הסינון".
- 768px (2n): filters on two rows (Teacher and status side by side, then search full width); the table's columns become Student (avatar, name, and under it national ID and phone, LTR, 12.5px) · Teacher and Car (Teacher name, under it the Car and its tag) · Status.
- Roster page (1a): a back link "חזרה לתלמידים" (sky, 600, back chevron that points to the inline start) above the header; title "ייבוא רשימת תלמידים"; subtitle "קובץ CSV מוסיף תלמידים חדשים ומעדכן תלמידים קיימים לפי תעודת זהות. הוא אף פעם לא משבית תלמיד."; the "תלמידים" nav item stays highlighted.

- [ ] **Step 1: Write the failing navigation specs**

Replace `client\src\app\features\admin-shell\domain\navigation.spec.ts` with:

```typescript
import { AppRoutes } from '../../../shared/config/app-routes';
import { Role } from '../../../shared/models/role.enum';
import { navigationFor } from './navigation';

function labelsOf(role: Role | null): string[] {
    return navigationFor(role).map((item) => item.labelKey);
}

describe('navigationFor', () => {
    it('gives an Administrator every screen, with Users last', () => {
        //expected
        expect(labelsOf(Role.administrator)).toEqual([
            'shell.nav.dashboard',
            'shell.nav.teachers',
            'shell.nav.students',
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
            'shell.nav.users',
        ]);
    });

    it('gives a Teacher only weekly prep, publications and history', () => {
        //expected
        expect(labelsOf(Role.teacher)).toEqual([
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
        ]);
    });

    it('shows no navigation without a known Role', () => {
        //expected
        expect(navigationFor(null)).toEqual([]);
    });

    it('links each screen to its route', () => {
        //given
        const commandsByLabel = new Map(
            navigationFor(Role.administrator).map((item) => [item.labelKey, item.commands]),
        );

        //expected
        expect(commandsByLabel.get('shell.nav.dashboard')).toEqual(['/']);
        expect(commandsByLabel.get('shell.nav.teachers')).toEqual(['/', 'teachers']);
        expect(commandsByLabel.get('shell.nav.students')).toEqual(['/', 'students']);
        expect(commandsByLabel.get('shell.nav.weeklyPrep')).toEqual(['/', 'week-schedules']);
        expect(commandsByLabel.get('shell.nav.publications')).toEqual(['/', 'publications']);
        expect(commandsByLabel.get('shell.nav.history')).toEqual(['/', 'publications', 'history']);
        expect(commandsByLabel.get('shell.nav.users')).toEqual(['/', 'users']);
    });

    it('highlights the dashboard and publications only on their exact route', () => {
        //given
        const exactLabels = navigationFor(Role.administrator)
            .filter((item) => item.exact)
            .map((item) => item.labelKey);

        //expected
        expect(exactLabels).toEqual(['shell.nav.dashboard', 'shell.nav.publications']);
    });

    it('keeps Students highlighted on the Roster import page below it', () => {
        //given
        const students = navigationFor(Role.administrator).find((item) => item.labelKey === 'shell.nav.students');
        const rosterImport = ['/', AppRoutes.students, AppRoutes.rosterImport];

        //expected
        expect(students?.exact).toBe(false);
        expect(rosterImport.slice(0, students?.commands.length)).toEqual(students?.commands);
    });
});
```

- [ ] **Step 2: Run them to see them fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/admin-shell/domain/navigation.spec.ts
```

Expected: FAIL: TypeScript reports `Property 'students' does not exist` on `AppRoutes` (and `rosterImport`).

- [ ] **Step 3: Routes and navigation**

Replace `client\src\app\shared\config\app-routes.ts` with:

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
    students: 'students',
    rosterImport: 'import',
    roster: 'roster',
    weekSchedules: 'week-schedules',
    publications: 'publications',
    publicationsHistory: 'history',
    users: 'users',
    studentForm: 's',
} as const;
```

In `client\src\app\features\admin-shell\domain\navigation.ts`, replace

```typescript
const ROSTER: NavigationItem = { labelKey: 'shell.nav.roster', commands: ['/', AppRoutes.roster], exact: false };
```

with

```typescript
const STUDENTS: NavigationItem = {
    labelKey: 'shell.nav.students',
    commands: ['/', AppRoutes.students],
    exact: false,
};
```

and in `ADMINISTRATOR_NAVIGATION` replace `ROSTER,` with `STUDENTS,`.

In `client\src\app\features\admin-shell\admin.routes.ts`, replace the Roster route

```typescript
      {
        path: AppRoutes.roster,
        canMatch: [administratorGuard],
        loadChildren: () => import('../roster/roster.routes'),
      },
```

with

```typescript
      {
        path: `${AppRoutes.students}/${AppRoutes.rosterImport}`,
        canMatch: [administratorGuard],
        loadChildren: () => import('../roster/roster.routes'),
      },
      {
        path: AppRoutes.students,
        canMatch: [administratorGuard],
        loadChildren: () => import('../students/students.routes'),
      },
      {
        path: AppRoutes.roster,
        pathMatch: 'full',
        redirectTo: `${AppRoutes.students}/${AppRoutes.rosterImport}`,
      },
```

The two-segment Roster route comes first so `/students/import` never reaches the Students routes; `/students` has one segment and skips it. The admin-shell routes file is the one place that composes features, so no feature imports another.

Create `client\src\app\features\students\students.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { StudentsPage } from './ui/pages/students/students.page';

export default [{ path: '', component: StudentsPage }] satisfies Routes;
```

- [ ] **Step 4: The transmission tag**

Create `client\src\app\features\students\ui\components\transmission-tag\transmission-tag.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Transmission } from '../../../domain/transmission.enum';

@Component({
    selector: 'app-transmission-tag',
    imports: [TranslocoPipe],
    template: `<span
        class="transmission-tag"
        [class.transmission-tag--automatic]="isAutomatic() && !muted()"
        [class.transmission-tag--manual]="!isAutomatic() && !muted()"
        [class.transmission-tag--muted]="muted()">{{ 'students.transmissions.' + transmission() | transloco }}</span>`,
    styleUrl: './transmission-tag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransmissionTagComponent {
    readonly transmission = input.required<Transmission>();
    readonly muted = input(false);

    protected readonly isAutomatic = computed(() => this.transmission() === Transmission.automatic);
}
```

Create `client\src\app\features\students\ui\components\transmission-tag\transmission-tag.component.scss`:

```scss
.transmission-tag {
    display: inline-flex;
    align-items: center;
    padding: 2px 6px;
    border: 1px solid transparent;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 600;
    line-height: 1.2;
    white-space: nowrap;
}

.transmission-tag--automatic {
    background: var(--p-sky-50);
    color: var(--p-sky-700);
    border-color: var(--p-sky-100);
}

.transmission-tag--manual {
    background: var(--app-bg-muted);
    color: var(--app-text-secondary);
    border-color: var(--app-border);
}

.transmission-tag--muted {
    background: var(--app-bg-muted);
    color: var(--app-text-muted);
    border-color: var(--app-border);
}
```

(Design tokens `tag.tx-auto` = sky 50 / 700 / 100 and `tag.tx-manual` = muted surface / secondary text / border, radius 4px.)

- [ ] **Step 5: The Students page**

Create `client\src\app\features\students\ui\pages\students\students.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { StudentStatusFilter } from '../../../domain/student-status-filter.enum';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { StudentsStore } from '../../../state/students.store';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';

const ALL_TEACHERS = 'all';

interface StatusOption {
    value: StudentStatusFilter;
    labelKey: string;
}

@Component({
    selector: 'app-students-page',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        InitialsPipe,
        ButtonModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule,
        ProgressSpinnerModule,
        SelectModule,
        SelectButtonModule,
        TableModule,
        TagModule,
        TransmissionTagComponent,
    ],
    templateUrl: './students.page.html',
    styleUrl: './students.page.scss',
    providers: [StudentsStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsPage {
    protected readonly store = inject(StudentsStore);

    protected readonly allTeachers = ALL_TEACHERS;
    protected readonly importRosterLink = ['/', AppRoutes.students, AppRoutes.rosterImport];

    protected readonly statusOptions: StatusOption[] = [
        { value: StudentStatusFilter.active, labelKey: 'students.filters.active' },
        { value: StudentStatusFilter.inactive, labelKey: 'students.filters.inactive' },
        { value: StudentStatusFilter.all, labelKey: 'students.filters.all' },
    ];

    protected readonly teacherFilterOptions = computed<TeacherOption[]>(() => [
        { id: ALL_TEACHERS, name: '' },
        ...this.store.teacherOptions(),
    ]);

    protected readonly selectedTeacher = computed(() => this.store.filters().teacherId ?? ALL_TEACHERS);

    protected onTeacherFilter(value: string): void {
        this.store.selectTeacher(value === ALL_TEACHERS ? null : value);
    }

    protected onStatusFilter(value: StudentStatusFilter): void {
        this.store.selectStatus(value);
    }

    protected onSearch(text: string): void {
        this.store.search(text);
    }
}
```

Create `client\src\app\features\students\ui\pages\students\students.page.html`:

```html
<div class="students">
  <header class="students__header">
    <div>
      <h2 class="students__title">{{ 'students.title' | transloco }}</h2>
      <p class="students__subtitle">{{ 'students.subtitle' | transloco }}</p>
    </div>
    <div class="students__actions">
      <a
        pButton
        severity="secondary"
        [outlined]="true"
        icon="pi pi-upload"
        [label]="'students.importRoster' | transloco"
        [routerLink]="importRosterLink"></a>
    </div>
  </header>

  @if (!store.isEmpty()) {
    <div class="students__filters">
      <div class="students__filter students__filter--teacher">
        <label class="students__filter-label" for="students-teacher">{{ 'students.filters.teacher' | transloco }}</label>
        <p-select
          inputId="students-teacher"
          [options]="teacherFilterOptions()"
          optionLabel="name"
          optionValue="id"
          [ngModel]="selectedTeacher()"
          (ngModelChange)="onTeacherFilter($event)"
          appendTo="body"
          fluid>
          <ng-template pTemplate="selectedItem" let-option>
            @if (option.id === allTeachers) {
              {{ 'students.filters.allTeachers' | transloco }}
            } @else {
              {{ option.name }}
            }
          </ng-template>
          <ng-template pTemplate="item" let-option>
            @if (option.id === allTeachers) {
              {{ 'students.filters.allTeachers' | transloco }}
            } @else {
              {{ option.name }}
            }
          </ng-template>
        </p-select>
      </div>

      <div class="students__filter students__filter--status">
        <span class="students__filter-label" id="students-status-label">{{ 'students.filters.status' | transloco }}</span>
        <p-selectbutton
          class="students__status"
          [options]="statusOptions"
          optionValue="value"
          [allowEmpty]="false"
          [ngModel]="store.filters().status"
          (ngModelChange)="onStatusFilter($event)"
          ariaLabelledBy="students-status-label">
          <ng-template pTemplate="item" let-option>{{ option.labelKey | transloco }}</ng-template>
        </p-selectbutton>
      </div>

      <div class="students__filter students__filter--search">
        <label class="students__filter-label" for="students-search">{{ 'students.filters.search' | transloco }}</label>
        <p-iconfield>
          <p-inputicon styleClass="pi pi-search" />
          <input
            pInputText
            id="students-search"
            class="students__search"
            type="search"
            autocomplete="off"
            [placeholder]="'students.filters.searchPlaceholder' | transloco"
            [ngModel]="store.filters().search"
            (ngModelChange)="onSearch($event)" />
        </p-iconfield>
      </div>
    </div>
  }

  <section class="students__card">
    @if (store.isLoading()) {
      <div class="students__state">
        <p-progressSpinner />
        <span class="students__state-text">{{ 'students.loading' | transloco }}</span>
      </div>
    } @else if (store.loadError(); as errorKey) {
      <div class="students__state" role="alert">
        <span class="students__error">
          <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
          {{ errorKey | transloco }}
        </span>
        <p-button [label]="'students.retry' | transloco" text size="small" (onClick)="store.reload()" />
      </div>
    } @else if (store.isEmpty()) {
      <div class="students__empty">
        <span class="students__empty-icon" aria-hidden="true"><i class="pi pi-user"></i></span>
        <p class="students__empty-title">{{ 'students.emptyTitle' | transloco }}</p>
        <p class="students__empty-hint">{{ 'students.emptyHint' | transloco }}</p>
        <div class="students__empty-actions">
          <a
            pButton
            severity="secondary"
            [outlined]="true"
            size="small"
            [label]="'students.importRoster' | transloco"
            [routerLink]="importRosterLink"></a>
        </div>
      </div>
    } @else {
      <p-table [value]="store.visibleStudents()" dataKey="id">
        <ng-template pTemplate="header">
          <tr>
            <th>
              <span class="students__wide-only">{{ 'students.table.name' | transloco }}</span>
              <span class="students__compact-only">{{ 'students.table.student' | transloco }}</span>
            </th>
            <th class="students__wide-only">{{ 'students.table.nationalId' | transloco }}</th>
            <th class="students__wide-only">{{ 'students.table.phone' | transloco }}</th>
            <th>
              <span class="students__wide-only">{{ 'students.table.teacher' | transloco }}</span>
              <span class="students__compact-only">{{ 'students.table.teacherAndCar' | transloco }}</span>
            </th>
            <th class="students__wide-only">{{ 'students.table.car' | transloco }}</th>
            <th>{{ 'students.table.status' | transloco }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-row>
          <tr [class.students__row--inactive]="!row.isActive" [class.students__row--new]="row.isNew">
            <td>
              <div class="students__name">
                <span class="students__avatar" aria-hidden="true">{{ row.name | initials }}</span>
                <div class="students__name-block">
                  <div class="students__name-line">
                    <span class="students__name-text">{{ row.name }}</span>
                    @if (row.isNew) {
                      <p-tag class="students__tag students__tag--new" [value]="'students.new' | transloco" />
                    }
                  </div>
                  <div class="students__ids students__compact-only">
                    <bdi dir="ltr" class="students__ltr">{{ row.nationalId }}</bdi>
                    <bdi dir="ltr" class="students__ltr">{{ row.phone }}</bdi>
                  </div>
                </div>
              </div>
            </td>
            <td class="students__wide-only"><bdi dir="ltr" class="students__ltr">{{ row.nationalId }}</bdi></td>
            <td class="students__wide-only"><bdi dir="ltr" class="students__ltr">{{ row.phone }}</bdi></td>
            <td class="students__teacher">
              <span>{{ row.teacherName }}</span>
              <div class="students__car-line students__compact-only">
                <span class="students__car">
                  {{ row.carName }}
                  <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
                </span>
              </div>
            </td>
            <td class="students__wide-only">
              <span class="students__car">
                {{ row.carName }}
                <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
              </span>
            </td>
            <td>
              <p-tag
                class="students__tag"
                [severity]="row.isActive ? 'success' : 'secondary'"
                [value]="(row.isActive ? 'students.status.active' : 'students.status.inactive') | transloco" />
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="6">
              <div class="students__empty">
                <span class="students__empty-icon" aria-hidden="true"><i class="pi pi-search"></i></span>
                <p class="students__empty-title">{{ 'students.noMatchTitle' | transloco }}</p>
                <p class="students__empty-hint">{{ 'students.noMatchHint' | transloco }}</p>
                <div class="students__empty-actions">
                  <p-button
                    [label]="'students.clearFilters' | transloco"
                    severity="secondary"
                    [outlined]="true"
                    size="small"
                    (onClick)="store.clearFilters()" />
                </div>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>

      @if (store.visibleStudents().length) {
        <p class="students__footer">{{ store.footerKey() | transloco: { count: store.visibleStudents().length } }}</p>
      }
    }
  </section>
</div>
```

Create `client\src\app\features\students\ui\pages\students\students.page.scss`:

```scss
.students__header {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1.5rem;
}

.students__title {
    margin: 0;
    font-family: var(--app-font-display);
    font-weight: 600;
    font-size: 1.625rem;
    color: var(--app-ink);
    letter-spacing: -0.01em;
}

.students__subtitle {
    margin: 0.25rem 0 0;
    max-inline-size: 46rem;
    font-size: 0.875rem;
    line-height: 1.5;
    color: var(--app-text-secondary);
}

.students__actions {
    display: flex;
    flex: none;
    align-items: center;
    gap: 0.625rem;
}

.students__filters {
    display: flex;
    align-items: flex-end;
    gap: 1rem;
    margin-block-start: 1.25rem;
}

.students__filter {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    min-inline-size: 0;
}

.students__filter--teacher {
    inline-size: 13.75rem;
}

.students__filter--status {
    inline-size: 18.75rem;
}

.students__filter--search {
    inline-size: 18.75rem;
    margin-inline-start: auto;
}

.students__filter-label {
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--app-text-secondary);
}

.students__search {
    inline-size: 100%;
}

:host ::ng-deep .students__status.p-selectbutton {
    display: flex;
    gap: 2px;
    padding: 3px;
    background: var(--app-bg-muted);
    border: 1px solid var(--app-border);
    border-radius: 999px;

    .p-togglebutton {
        flex: 1;
        justify-content: center;
        border-radius: 999px;
        border: 1px solid transparent;
        background: transparent;
        color: var(--app-text-secondary);
    }

    .p-togglebutton-content {
        background: transparent;
        box-shadow: none;
    }

    .p-togglebutton-checked {
        background: var(--app-bg-card);
        border-color: var(--app-border);
        color: var(--app-ink);
        font-weight: 600;
        box-shadow: var(--app-shadow-card);
    }
}

.students__card {
    margin-block-start: 1rem;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);
    overflow: hidden;
}

:host ::ng-deep .students__card .p-datatable {
    overflow-x: auto;

    .p-datatable-thead > tr > th {
        padding: 0.75rem 1rem;
        background: var(--app-bg-page);
        border-block-end: 1px solid var(--app-border);
        font-size: 0.75rem;
        font-weight: 700;
        color: var(--app-text-secondary);
        text-align: start;
        white-space: nowrap;
    }

    .p-datatable-tbody > tr > td {
        padding: 0.75rem 1rem;
        border-block-end: 1px solid var(--app-bg-muted);
        font-size: 0.875rem;
        color: var(--app-ink);
        vertical-align: middle;
    }

    .p-datatable-tbody > tr.students__row--inactive {
        background: var(--app-bg-page);
    }

    .p-datatable-tbody > tr.students__row--inactive > td {
        color: var(--app-text-muted);
    }

    .p-datatable-tbody > tr.students__row--new {
        background: var(--p-sky-50);
    }
}

.students__name {
    display: flex;
    align-items: center;
    gap: 0.625rem;
}

.students__name-block {
    min-inline-size: 0;
}

.students__name-line {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.students__name-text {
    font-weight: 600;
    white-space: nowrap;
}

.students__avatar {
    flex: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    inline-size: 30px;
    block-size: 30px;
    border-radius: 999px;
    background: var(--app-steel-light);
    color: var(--app-whale);
    font-size: 0.72rem;
    font-weight: 700;
}

.students__row--inactive .students__avatar {
    background: var(--app-bg-muted);
    color: var(--app-text-muted);
}

.students__ltr {
    unicode-bidi: isolate;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-secondary);
    white-space: nowrap;
}

.students__row--inactive .students__ltr {
    color: var(--app-text-muted);
}

.students__ids {
    gap: 0.625rem;
    margin-block-start: 0.125rem;
    font-size: 0.78rem;
}

.students__teacher {
    white-space: nowrap;
}

.students__car {
    display: inline-flex;
    align-items: center;
    gap: 0.4375rem;
    white-space: nowrap;
}

.students__car-line {
    margin-block-start: 0.3125rem;
}

.students__tag {
    border: 1px solid transparent;
    border-radius: 999px;
    padding: 3px 10px;
    font-size: 0.75rem;
    font-weight: 600;
    white-space: nowrap;
}

.students__tag--new {
    background: var(--p-sky-50);
    color: var(--p-sky-700);
    border-color: var(--p-sky-100);
}

.students__state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    min-block-size: 320px;
}

.students__state-text {
    font-size: 0.85rem;
    color: var(--app-text-secondary);
}

.students__error {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.9rem;
    color: var(--p-red-600);
}

.students__footer {
    margin: 0;
    padding: 0.75rem 1rem;
    font-size: 0.78rem;
    color: var(--app-text-muted);
}

.students__empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    padding: 2.75rem 1.5rem 3rem;
    text-align: center;
}

.students__empty-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    inline-size: 44px;
    block-size: 44px;
    border-radius: 999px;
    background: var(--app-bg-muted);
    border: 1px solid var(--app-border);
    color: var(--p-sky-500);
    font-size: 1.25rem;
}

.students__empty-title {
    margin: 0.25rem 0 0;
    font-size: 1.06rem;
    font-weight: 600;
    color: var(--app-ink);
}

.students__empty-hint {
    margin: 0;
    max-inline-size: 460px;
    font-size: 0.875rem;
    line-height: 1.5;
    color: var(--app-text-secondary);
}

.students__empty-actions {
    display: flex;
    gap: 0.625rem;
    margin-block-start: 0.625rem;
}

.students__compact-only {
    display: none;
}

@media screen and (max-width: 992px) {
    .students__header {
        flex-wrap: wrap;
    }

    .students__filters {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1.2fr);
        gap: 0.75rem;
        margin-block-start: 1.125rem;
    }

    .students__filter--teacher,
    .students__filter--status {
        inline-size: auto;
    }

    .students__filter--search {
        grid-column: 1 / -1;
        inline-size: auto;
        margin-inline-start: 0;
    }

    .students__wide-only {
        display: none;
    }

    .students__compact-only {
        display: inline;
    }

    .students__ids.students__compact-only,
    .students__car-line.students__compact-only {
        display: flex;
    }
}
```

(Same table, tag, avatar and state styles as the Users page, so the two lists read as one system. The `992px` breakpoint is the project's single desktop/mobile line; the 768px frame falls under it.)

- [ ] **Step 6: The Roster page links back to Students**

In `client\src\app\features\roster\ui\pages\roster\roster.page.html`, replace the first two lines

```html
<div class="roster">
    <header class="roster__header">
```

with

```html
<div class="roster">
    <a class="roster__back" [routerLink]="studentsLink">
        <i class="pi pi-chevron-left roster__back-icon" aria-hidden="true"></i>
        {{ 'roster.back' | transloco }}
    </a>
    <header class="roster__header">
```

In `client\src\app\features\roster\ui\pages\roster\roster.page.ts`:
1. Add `import { RouterLink } from '@angular/router';` after `import { FormsModule } from '@angular/forms';`, and `import { AppRoutes } from '../../../../../shared/config/app-routes';` after `import { LanguageService } from '../../../../../core/language.service';`.
2. Add `RouterLink,` to `imports` after `FormsModule,`.
3. Add after `protected readonly allTeachers = ALL_TEACHERS;`:

```typescript
    protected readonly studentsLink = ['/', AppRoutes.students];
```

In `client\src\app\features\roster\ui\pages\roster\roster.page.scss`, add at the top:

```scss
.roster__back {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    min-block-size: 24px;
    margin-block-end: 0.75rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--p-sky-500);
    text-decoration: none;

    &:hover {
        text-decoration: underline;
    }
}

.roster__back-icon {
    font-size: 0.875rem;
}

:host-context([dir='rtl']) .roster__back-icon {
    transform: scaleX(-1);
}

```

- [ ] **Step 7: Run the navigation specs**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/admin-shell/domain/navigation.spec.ts
```

Expected: PASS, 6 specs.

- [ ] **Step 8: Translations, Hebrew first**

In `client\public\i18n\he.json`:

1. In `shell.nav`, replace `"roster": "תלמידים",` with `"students": "תלמידים",`.
2. Replace

```json
    "title": "רשימת תלמידים",
    "subtitle": "העלו את קובץ ה-CSV של בית הספר כדי להוסיף תלמידים ולעדכן את המורים והרכבים שלהם.",
```

with

```json
    "title": "ייבוא רשימת תלמידים",
    "subtitle": "קובץ CSV מוסיף תלמידים חדשים ומעדכן תלמידים קיימים לפי תעודת זהות. הוא אף פעם לא משבית תלמיד.",
    "back": "חזרה לתלמידים",
```

3. Replace

```json
    "empty": "אין תלמידים עדיין. העלו את קובץ ה-CSV של בית הספר כדי לבנות את הרשימה."
  },
  "dashboard": {
```

with

```json
    "empty": "אין תלמידים עדיין. העלו את קובץ ה-CSV של בית הספר כדי לבנות את הרשימה."
  },
  "students": {
    "title": "תלמידים",
    "subtitle": "תלמידים הם מי שמגישים בקשות שבועיות. הרשימה כוללת תלמידים מרשימת התלמידים ותלמידים שנוספו ידנית.",
    "importRoster": "ייבוא רשימת תלמידים",
    "filters": {
      "teacher": "מורה",
      "allTeachers": "כל המורים",
      "status": "מצב",
      "active": "פעילים",
      "inactive": "לא פעילים",
      "all": "הכול",
      "search": "חיפוש",
      "searchPlaceholder": "שם או תעודת זהות"
    },
    "table": {
      "name": "שם",
      "nationalId": "תעודת זהות",
      "phone": "טלפון",
      "teacher": "מורה",
      "car": "רכב",
      "status": "סטטוס",
      "student": "תלמיד",
      "teacherAndCar": "מורה ורכב"
    },
    "status": {
      "active": "פעיל",
      "inactive": "לא פעיל"
    },
    "transmissions": {
      "automatic": "אוטומט",
      "manual": "ידני"
    },
    "new": "חדש",
    "footer": "{{count}} תלמידים",
    "footerOne": "תלמיד אחד",
    "loading": "טוען תלמידים...",
    "loadFailed": "לא הצלחנו לטעון את התלמידים.",
    "retry": "ניסיון חוזר",
    "emptyTitle": "עדיין אין תלמידים",
    "emptyHint": "הוסיפו תלמיד ידנית, או ייבאו את רשימת התלמידים מקובץ.",
    "noMatchTitle": "אין תלמידים שמתאימים לסינון",
    "noMatchHint": "נסו מורה אחר או מצב אחר.",
    "clearFilters": "ניקוי הסינון"
  },
  "dashboard": {
```

In `client\public\i18n\en.json`:

1. In `shell.nav`, replace `"roster": "Roster",` with `"students": "Students",`.
2. Replace

```json
    "title": "Student roster",
    "subtitle": "Upload the school's CSV to add students and update their teachers and cars.",
```

with

```json
    "title": "Import Roster",
    "subtitle": "A CSV file adds new Students and updates existing ones by national ID. It never deactivates anyone.",
    "back": "Back to Students",
```

3. Replace

```json
    "empty": "No students yet. Upload the school's CSV to build the roster."
  },
  "dashboard": {
```

with

```json
    "empty": "No students yet. Upload the school's CSV to build the roster."
  },
  "students": {
    "title": "Students",
    "subtitle": "Students are the people who submit weekly requests. This list includes Students from the Roster and Students added by hand.",
    "importRoster": "Import Roster",
    "filters": {
      "teacher": "Teacher",
      "allTeachers": "All Teachers",
      "status": "Status",
      "active": "Active",
      "inactive": "Inactive",
      "all": "All",
      "search": "Search",
      "searchPlaceholder": "Name or national ID"
    },
    "table": {
      "name": "Name",
      "nationalId": "National ID",
      "phone": "Phone",
      "teacher": "Teacher",
      "car": "Car",
      "status": "Status",
      "student": "Student",
      "teacherAndCar": "Teacher and Car"
    },
    "status": {
      "active": "Active",
      "inactive": "Inactive"
    },
    "transmissions": {
      "automatic": "Automatic",
      "manual": "Manual"
    },
    "new": "New",
    "footer": "{{count}} Students",
    "footerOne": "1 Student",
    "loading": "Loading Students...",
    "loadFailed": "We couldn't load the Students.",
    "retry": "Try again",
    "emptyTitle": "No Students yet",
    "emptyHint": "Add a Student by hand, or import the Roster file.",
    "noMatchTitle": "No Students match these filters",
    "noMatchHint": "Try another Teacher or status.",
    "clearFilters": "Clear filters"
  },
  "dashboard": {
```

(Copy deck keys `nav.students`, `roster.*`, `stu.*`, `flt.*`, `col.*`, `status.*`, `tx.*` verbatim, except: "טוען תלמידים" / "Loading Students" end in three dots instead of the ellipsis character, `{n}` is `{{count}}`, `stu.import` is Title Case like the other English buttons, and `footerOne` is new (README decision 13).)

Confirm nothing still uses the old navigation key:

```bash
grep -rn "shell.nav.roster" client/src client/public
```

Expected: no output.

- [ ] **Step 9: Run the whole client suite and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (`translations.spec.ts` and `source-text.spec.ts` included), build succeeds with a new lazy chunk for `students-routes`.

- [ ] **Step 10: Look at it once**

Start the API on any local database that has Teachers, Cars and a few Students (the compose Postgres; memory note) and the client (`preview_start {name: "client"}`), sign in as `admin@local.dev` / `DevAdmin#2026` and open "תלמידים". Check quickly: the list loads with the transmission tags, the filters work, "ייבוא רשימת תלמידים" opens `/students/import` with the back link and "תלמידים" still highlighted, and `/roster` lands on `/students/import`. Task 6 does the full check; this is only to catch a broken page before the commit. Stop the preview.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/features/students client/src/app/shared/config/app-routes.ts client/src/app/features/admin-shell/admin.routes.ts client/src/app/features/admin-shell/domain/navigation.ts client/src/app/features/admin-shell/domain/navigation.spec.ts client/src/app/features/roster/ui/pages/roster client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Students screen with filters and search; Roster moves behind Import Roster (#93)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
