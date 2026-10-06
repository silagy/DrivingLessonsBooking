# Task 5 of 7: Row actions, Deactivate dialog and one-click Reactivate (client)

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires task 4 committed. Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 10, 13 to 15, 18 and 19 first.

**Files:**
- Create: `client\src\app\features\students\ui\components\student-who-card\student-who-card.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\students\ui\dialogs\deactivate-student\deactivate-student.dialog.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`, `.html`, `.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `StudentsStore.deactivate(student: Student): Promise<boolean>`, `.reactivate(student: Student): Promise<void>`, `.refusal: Signal<StudentRefusal | null>`, `.isMutating`, `.clearRefusal()` (task 4 / #93), `StudentRow` (`Student` + `isNew`), `TransmissionTagComponent` (`transmission`, `muted` inputs), `InitialsPipe`, `DialogRefusalComponent` (`message` input), PrimeNG `Menu` / `MenuItem`.
- Produces (task 6 relies on these):
  - `StudentsPage.onRowActions(event: Event, student: StudentRow)` building `rowActions` (a `signal<MenuItem[]>`), the `#rowMenu` popup menu, and the private `openDialog<TData>(component, header, width, data)` helper.
  - `app-student-who-card` with input `student: Student`.
  - `DeactivateStudentDialog` with `DeactivateStudentDialogData { student: Student; refusal: Signal<StudentRefusal | null>; isDeactivating: Signal<boolean>; confirm: () => Promise<boolean> }`.
  - Keys `students.actions.menu`, `.menuFor`, `.deactivate`, `.reactivate`; `students.who.teacher`, `.car`; `students.deactivate.title`, `.cannotSubmit`, `.submissionsStay`, `.rosterReactivates`; `students.deactivated`, `.deactivatedDetail`, `.reactivated`, `.reactivatedDetail`.

**Why:** AC "Students screen: ... Deactivate and Reactivate actions; the active-state column updates". Design frames 2e, 2f, 5a to 5e. The Edit item joins the menu in task 6.

- [ ] **Step 1: Write the failing dialog spec**

Create `client\src\app\features\students\ui\dialogs\deactivate-student\deactivate-student.dialog.spec.ts`:

```ts
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { Transmission } from '../../../domain/transmission.enum';
import { DeactivateStudentDialog, DeactivateStudentDialogData } from './deactivate-student.dialog';

const ITAI: Student = {
    id: 'student-itai',
    nationalId: '207815432',
    name: 'Itai Peretz',
    phone: '050-6612034',
    teacherId: 'teacher-yael',
    teacherName: 'Yael Carmi',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
    isActive: true,
};

interface Setup {
    fixture: ComponentFixture<DeactivateStudentDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    isDeactivating: ReturnType<typeof signal<boolean>>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult: boolean): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const isDeactivating = signal(false);
    const close = vi.fn();
    const data: DeactivateStudentDialogData = {
        student: ITAI,
        refusal,
        isDeactivating,
        confirm: () => Promise.resolve(confirmResult),
    };

    TestBed.configureTestingModule({
        imports: [
            DeactivateStudentDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: { close } },
        ],
    });

    const fixture = TestBed.createComponent(DeactivateStudentDialog);
    await fixture.whenStable();

    return { fixture, refusal, isDeactivating, close };
}

function host(fixture: ComponentFixture<DeactivateStudentDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function confirmButton(fixture: ComponentFixture<DeactivateStudentDialog>): HTMLButtonElement {
    return host(fixture).querySelector('.deactivate-student__confirm button') as HTMLButtonElement;
}

describe('DeactivateStudentDialog', () => {
    it('shows who is being deactivated, with the national ID left-to-right', async () => {
        //given
        const { fixture } = await setUp(true);

        //then
        expect(host(fixture).querySelector('.who-card__name')?.textContent).toContain('Itai Peretz');
        expect(host(fixture).querySelector('.who-card__national-id')?.getAttribute('dir')).toBe('ltr');
        expect(host(fixture).querySelectorAll('.deactivate-student__effect').length).toBe(3);
    });

    it('closes once the Student is deactivated', async () => {
        //given
        const { fixture, close } = await setUp(true);

        //when
        confirmButton(fixture).click();
        await fixture.whenStable();

        //then
        expect(close).toHaveBeenCalled();
    });

    it('stays open with the refusal when the deactivation is refused', async () => {
        //given
        const { fixture, close, refusal } = await setUp(false);

        //when
        confirmButton(fixture).click();
        refusal.set({ kind: StudentRefusalKind.other, message: 'Something went wrong.', existingStudentName: null });
        await fixture.whenStable();

        //then
        expect(close).not.toHaveBeenCalled();
        expect(host(fixture).querySelector('app-dialog-refusal')).not.toBeNull();
    });

    it('disables the confirmation while deactivating', async () => {
        //given
        const { fixture, isDeactivating } = await setUp(true);

        //when
        isDeactivating.set(true);
        await fixture.whenStable();

        //then
        expect(confirmButton(fixture).disabled).toBe(true);
    });
});
```

Run from `client\` (PowerShell): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/deactivate-student/deactivate-student.dialog.spec.ts`
Expected: FAIL (`Cannot find module './deactivate-student.dialog'`).

- [ ] **Step 2: Add the who card**

Create `client\src\app\features\students\ui\components\student-who-card\student-who-card.component.ts`:

```ts
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Student } from '../../../domain/student.model';
import { TransmissionTagComponent } from '../transmission-tag/transmission-tag.component';

@Component({
    selector: 'app-student-who-card',
    imports: [TranslocoPipe, InitialsPipe, TransmissionTagComponent],
    templateUrl: './student-who-card.component.html',
    styleUrl: './student-who-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentWhoCardComponent {
    readonly student = input.required<Student>();
}
```

Create `client\src\app\features\students\ui\components\student-who-card\student-who-card.component.html`:

```html
<div class="who-card">
  <span class="who-card__avatar" aria-hidden="true">{{ student().name | initials }}</span>
  <span class="who-card__identity">
    <span class="who-card__name">{{ student().name }}</span>
    <bdi dir="ltr" class="who-card__national-id">{{ student().nationalId }}</bdi>
  </span>
  <span class="who-card__assignment">
    <span>
      {{ 'students.who.teacher' | transloco }}
      <span class="who-card__value">{{ student().teacherName }}</span>
    </span>
    <span class="who-card__car">
      {{ 'students.who.car' | transloco }}
      <span class="who-card__value">{{ student().carName }}</span>
      <app-transmission-tag [transmission]="student().carTransmission" />
    </span>
  </span>
</div>
```

Create `client\src\app\features\students\ui\components\student-who-card\student-who-card.component.scss`:

```scss
.who-card {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.6875rem 0.75rem;
    border-radius: 10px;
    background: var(--app-bg-page);
    border: 1px solid var(--app-border);
}

.who-card__avatar {
    flex: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    inline-size: 34px;
    block-size: 34px;
    border-radius: 999px;
    background: var(--app-steel-light);
    color: var(--app-whale);
    font-size: 0.8rem;
    font-weight: 700;
}

.who-card__identity {
    display: flex;
    flex: 1;
    flex-direction: column;
    min-inline-size: 0;
}

.who-card__name {
    font-size: 0.9rem;
    font-weight: 600;
    color: var(--app-ink);
}

.who-card__national-id {
    unicode-bidi: isolate;
    font-size: 0.8rem;
    font-variant-numeric: tabular-nums;
    color: var(--app-text-secondary);
}

.who-card__assignment {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
    font-size: 0.8rem;
    color: var(--app-text-secondary);
}

.who-card__value {
    font-weight: 600;
    color: var(--app-ink);
}

.who-card__car {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
}
```

(Frame `StWho`: 34px avatar, name 14.5px semibold, national ID 13px tabular LTR, "מורה: {t}" and "רכב: {car} [tag]" stacked at the inline end; page-background tint and border like `app-user-who-card`.)

- [ ] **Step 3: Add the Deactivate dialog**

Create `client\src\app\features\students\ui\dialogs\deactivate-student\deactivate-student.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';

export interface DeactivateStudentDialogData {
    student: Student;
    refusal: Signal<StudentRefusal | null>;
    isDeactivating: Signal<boolean>;
    confirm: () => Promise<boolean>;
}

@Component({
    selector: 'app-deactivate-student-dialog',
    imports: [TranslocoPipe, ButtonModule, DialogRefusalComponent, StudentWhoCardComponent],
    templateUrl: './deactivate-student.dialog.html',
    styleUrl: './deactivate-student.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeactivateStudentDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<DeactivateStudentDialogData>).data as DeactivateStudentDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isDeactivating = this.data.isDeactivating;

    protected async confirm(): Promise<void> {
        const done = await this.data.confirm();

        if (done) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

Create `client\src\app\features\students\ui\dialogs\deactivate-student\deactivate-student.dialog.html`:

```html
<div class="deactivate-student">
  @if (refusal(); as refused) {
    <app-dialog-refusal [message]="refused.message" />
  }

  <app-student-who-card [student]="student" />

  <ul class="deactivate-student__effects">
    <li class="deactivate-student__effect">
      <i class="pi pi-pause-circle deactivate-student__icon" aria-hidden="true"></i>
      {{ 'students.deactivate.cannotSubmit' | transloco }}
    </li>
    <li class="deactivate-student__effect">
      <i class="pi pi-check deactivate-student__icon" aria-hidden="true"></i>
      {{ 'students.deactivate.submissionsStay' | transloco }}
    </li>
    <li class="deactivate-student__effect">
      <i class="pi pi-replay deactivate-student__icon deactivate-student__icon--mirrored" aria-hidden="true"></i>
      {{ 'students.deactivate.rosterReactivates' | transloco }}
    </li>
  </ul>

  <div class="deactivate-student__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
    <p-button
      class="deactivate-student__confirm"
      [label]="'students.actions.deactivate' | transloco"
      severity="danger"
      [outlined]="true"
      type="button"
      [disabled]="isDeactivating()"
      (onClick)="confirm()" />
  </div>
</div>
```

Create `client\src\app\features\students\ui\dialogs\deactivate-student\deactivate-student.dialog.scss`:

```scss
.deactivate-student {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.deactivate-student__effects {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 0;
    padding: 0;
    list-style: none;
}

.deactivate-student__effect {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    font-size: 0.875rem;
    line-height: 1.5;
    color: var(--app-text-secondary);
}

.deactivate-student__icon {
    margin-block-start: 0.2rem;
    font-size: 1rem;
    color: var(--app-text-muted);
}

:host-context([dir='rtl']) .deactivate-student__icon--mirrored {
    transform: scaleX(-1);
}

.deactivate-student__actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.625rem;
    padding-block-start: 0.5rem;
}
```

(Frame 5a / `StDeactDlg`: who card, three 14px lines with 16px muted icons (pause, check, restore mirrored in RTL), text Cancel + the danger **outline** button: the action is reversible, so it reads quieter than Users' filled Delete. The design's design-system `danger` variant maps to PrimeNG `severity="danger"` + `outlined`.)

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/deactivate-student/deactivate-student.dialog.spec.ts`
Expected: PASS, 4 tests.

- [ ] **Step 4: Add the row actions to the page**

Replace `client\src\app\features\students\ui\pages\students\students.page.ts` with:

```ts
import { ChangeDetectionStrategy, Component, Type, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Menu, MenuModule } from 'primeng/menu';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { StudentRow } from '../../../domain/student-row.model';
import { StudentStatusFilter } from '../../../domain/student-status-filter.enum';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { StudentsStore } from '../../../state/students.store';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { AddStudentDialog } from '../../dialogs/add-student/add-student.dialog';
import { AddStudentDialogData } from '../../dialogs/add-student/add-student-dialog-data';
import {
    DeactivateStudentDialog,
    DeactivateStudentDialogData,
} from '../../dialogs/deactivate-student/deactivate-student.dialog';

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
        MenuModule,
        ProgressSpinnerModule,
        SelectModule,
        SelectButtonModule,
        TableModule,
        TagModule,
        TransmissionTagComponent,
    ],
    templateUrl: './students.page.html',
    styleUrl: './students.page.scss',
    providers: [StudentsStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsPage {
    private static readonly dialogWidth = '35rem';
    private static readonly deactivateDialogWidth = '32.5rem';

    protected readonly store = inject(StudentsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly allTeachers = ALL_TEACHERS;
    protected readonly importRosterLink = ['/', AppRoutes.students, AppRoutes.rosterImport];
    protected readonly rowActions = signal<MenuItem[]>([]);

    private readonly rowMenu = viewChild.required<Menu>('rowMenu');

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

    protected onAddStudent(): void {
        const data: AddStudentDialogData = {
            teachers: this.store.teacherOptions,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (newStudent) => this.store.create(newStudent),
            refreshCars: () => this.store.refreshCarOptions(),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(AddStudentDialog, this.transloco.translate('students.add.title'), StudentsPage.dialogWidth, data);
    }

    protected onRowActions(event: Event, student: StudentRow): void {
        this.rowActions.set(student.isActive ? this.activeRowActions(student) : this.inactiveRowActions(student));
        this.rowMenu().toggle(event);
    }

    private activeRowActions(student: StudentRow): MenuItem[] {
        return [
            {
                label: this.transloco.translate('students.actions.deactivate'),
                icon: 'pi pi-pause-circle',
                styleClass: 'students-menu__item--danger',
                command: () => this.onDeactivate(student),
            },
        ];
    }

    private inactiveRowActions(student: StudentRow): MenuItem[] {
        return [
            {
                label: this.transloco.translate('students.actions.reactivate'),
                icon: 'pi pi-replay',
                command: () => void this.store.reactivate(student),
            },
        ];
    }

    private onDeactivate(student: StudentRow): void {
        const data: DeactivateStudentDialogData = {
            student,
            refusal: this.store.refusal,
            isDeactivating: this.store.isMutating,
            confirm: () => this.store.deactivate(student),
        };

        this.openDialog(
            DeactivateStudentDialog,
            this.transloco.translate('students.deactivate.title', { name: isolateDirection(student.name) }),
            StudentsPage.deactivateDialogWidth,
            data,
        );
    }

    private openDialog<TData>(component: Type<unknown>, header: string, width: string, data: TData): void {
        this.store.clearRefusal();

        this.dialogs.open(component, {
            header,
            width,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
```

(`onAddStudent` now opens through `openDialog`, which clears the refusal first, as before. Task 6 adds the Edit item at the top of both lists, with a separator before Deactivate.)

In `client\src\app\features\students\ui\pages\students\students.page.html`:

1. In the header row, after `<th>{{ 'students.table.status' | transloco }}</th>`, add:

```html
            <th class="students__actions-header">
              <span class="students__sr-only">{{ 'students.actions.menu' | transloco }}</span>
            </th>
```

2. In the body row, after the status `<td>...</td>` (the one with `p-tag` and `students.status.*`), add:

```html
            <td class="students__actions">
              <p-button
                icon="pi pi-ellipsis-v"
                rounded
                text
                severity="secondary"
                [ariaLabel]="'students.actions.menuFor' | transloco: { name: row.name }"
                [disabled]="store.isMutating()"
                (onClick)="onRowActions($event, row)" />
            </td>
```

3. In the `emptymessage` template, change `<td colspan="6">` to `<td colspan="7">`.

4. Right after the closing `</p-table>`, add:

```html
      <p-menu #rowMenu [model]="rowActions()" [popup]="true" appendTo="body" />
```

Append to `client\src\app\features\students\ui\pages\students\students.page.scss`:

```scss
.students__actions-header {
    inline-size: 60px;
}

.students__actions {
    text-align: end;
    white-space: nowrap;
}

.students__sr-only {
    position: absolute;
    inline-size: 1px;
    block-size: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
}

::ng-deep .students-menu__item--danger .p-menu-item-link,
::ng-deep .students-menu__item--danger .p-menu-item-icon,
::ng-deep .students-menu__item--danger .p-menu-item-label {
    color: var(--p-red-600);
}
```

(Frames 2e / 2f: 60px actions column, kebab at the inline end, the danger item red like Users' Delete. The actions column is shown at every width, so the 768px layout keeps it.)

- [ ] **Step 5: Add the translations**

In `client\public\i18n\he.json`, inside `students`, right after the `"addedDetail": ...` line, add:

```json
    "actions": {
      "menu": "פעולות",
      "menuFor": "פעולות - {{name}}",
      "deactivate": "סימון כלא פעיל",
      "reactivate": "סימון כפעיל"
    },
    "who": {
      "teacher": "מורה:",
      "car": "רכב:"
    },
    "deactivate": {
      "title": "לסמן את {{name}} כלא פעיל/ה?",
      "cannotSubmit": "עד לסימון מחדש כפעיל/ה, לא תהיה אפשרות להגיש בטופס התלמידים.",
      "submissionsStay": "ההגשות הקודמות נשמרות.",
      "rosterReactivates": "קובץ רשימת תלמידים שעדיין כולל אותם יסמן אותם שוב כפעילים."
    },
    "deactivated": "התלמיד סומן כלא פעיל",
    "deactivatedDetail": "{{name}} לא יוכל/תוכל להגיש עד לסימון מחדש כפעיל/ה.",
    "reactivated": "התלמיד סומן כפעיל",
    "reactivatedDetail": "{{name}} יכול/ה להגיש שוב בטופס התלמידים.",
```

In `client\public\i18n\en.json`, inside `students`, right after the `"addedDetail": ...` line, add:

```json
    "actions": {
      "menu": "Actions",
      "menuFor": "Actions - {{name}}",
      "deactivate": "Deactivate",
      "reactivate": "Reactivate"
    },
    "who": {
      "teacher": "Teacher:",
      "car": "Car:"
    },
    "deactivate": {
      "title": "Deactivate {{name}}?",
      "cannotSubmit": "Until reactivated, they can't submit on the student form.",
      "submissionsStay": "Their past Submissions stay.",
      "rosterReactivates": "A Roster file that still lists them will reactivate them."
    },
    "deactivated": "Student deactivated",
    "deactivatedDetail": "{{name}} can't submit until reactivated.",
    "reactivated": "Student reactivated",
    "reactivatedDetail": "{{name}} can submit on the student form again.",
```

(Copy deck `act.menu`, `act.deactivate`, `act.reactivate`, `who.teacher`, `who.car`, `deact.*`, `toast.(de|re)activated*`, with `{name}` as `{{name}}`; `deact.confirm` "סימון כלא פעיל" is the same text as `act.deactivate`, so the button reuses `students.actions.deactivate`. English buttons in Title Case per #93 decision 20.)

- [ ] **Step 6: Run the client suites and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS (`translations.spec.ts` sees the same keys in both files); the build succeeds. The menu, the dialog and the toasts are checked in the browser in task 7.

- [ ] **Step 7: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Students row actions with Deactivate confirmation and one-click Reactivate (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
