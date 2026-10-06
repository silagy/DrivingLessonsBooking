# Task 6 of 7: Edit details dialog (client)

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires task 5 committed. Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 10 to 12, 14, 16, 17 and 19 first.

**Files:**
- Create: `client\src\app\features\students\ui\dialogs\edit-student\edit-student-dialog-data.ts`
- Create: `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `StudentsStore.loadDetails(studentId): Promise<StudentDetails | null>`, `.changeDetails(studentId, change): Promise<boolean>`, `.refusal`, `.isMutating`, `.clearRefusal()` (task 4); `StudentDetails`, `StudentDetailsChange`, `toEditStudentFormValue`, `toStudentDetailsChange` (task 4); `StudentRefusal`, `StudentRefusalKind` (task 4); `StudentsPage.onRowActions`, `activeRowActions`, `inactiveRowActions`, `openDialog` (task 5); `LockedFieldComponent` (`label`, `value` inputs + projected note), `DialogRefusalComponent`, `TransmissionTagComponent`; the shared `dialog-form.scss` classes `dialog-form`, `field`, `field__hint`, `field__error`, `dialog-form__actions`; existing keys `students.add.nationalId`, `.fullName`, `.phone`, `.address`, `.startDate`, `.datePlaceholder`, `.licenseType`, `.licenseTypePlaceholder`, `.nationalIdInvalid`, `general.cancel`, `general.save`.
- Produces: `EditStudentDialog` with `EditStudentDialogData { details: StudentDetails; refusal: Signal<StudentRefusal | null>; isSaving: Signal<boolean>; confirm: (change: StudentDetailsChange) => Promise<boolean>; clearRefusal: () => void }`; keys `students.actions.editDetails`, `students.edit.title`, `.nationalIdNote`, `.nationalIdUsed` (`{{name}}`), `.teacherAndCar`, `students.detailsSaved`.

**Why:** AC "Students screen: Edit dialog". Design frames 4a to 4c. The dialog stays open on a refusal, marks the refused field and keeps Save disabled until it changes, like Add Student (#93 decision 17).

- [ ] **Step 1: Write the failing dialog spec**

Create `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.spec.ts`:

```ts
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { StudentDetailsChange } from '../../../domain/student-details-change.model';
import { StudentDetails } from '../../../domain/student-details.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Transmission } from '../../../domain/transmission.enum';
import { EditStudentDialogData } from './edit-student-dialog-data';
import { EditStudentDialog } from './edit-student.dialog';

const NOA: StudentDetails = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    address: '12 HaRimon St, Modiin',
    startDate: '2026-09-01',
    licenseType: 'B',
    isActive: true,
};

const NATIONAL_ID_IN_USE: StudentRefusal = {
    kind: StudentRefusalKind.nationalIdInUse,
    message: 'A Student with this national ID already exists: Omer Shalev.',
    existingStudentName: 'Omer Shalev',
};

interface Setup {
    fixture: ComponentFixture<EditStudentDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    confirm: ReturnType<typeof vi.fn<(change: StudentDetailsChange) => Promise<boolean>>>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult = true, details: StudentDetails = NOA): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const isSaving = signal(false);
    const confirm = vi.fn<(change: StudentDetailsChange) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: EditStudentDialogData = { details, refusal, isSaving, confirm, clearRefusal };

    TestBed.configureTestingModule({
        imports: [
            EditStudentDialog,
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

    const fixture = TestBed.createComponent(EditStudentDialog);
    await fixture.whenStable();

    return { fixture, refusal, isSaving, confirm, clearRefusal, close };
}

interface DialogInternals {
    form: EditStudentDialog['form'];
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<EditStudentDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<EditStudentDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function saveButton(fixture: ComponentFixture<EditStudentDialog>): HTMLButtonElement {
    return host(fixture).querySelector('button[type="submit"]') as HTMLButtonElement;
}

async function typeNationalId(fixture: ComponentFixture<EditStudentDialog>, value: string): Promise<void> {
    const input = host(fixture).querySelector('#edit-student-national-id') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
}

describe('EditStudentDialog', () => {
    it("opens filled with the Student's details", async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(internals(fixture).form.getRawValue()).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234567',
            address: '12 HaRimon St, Modiin',
            startDate: new Date(2026, 8, 1),
            licenseType: 'B',
        });
        expect(host(fixture).querySelector('.locked-field__value')?.textContent).toContain('Ronit Avraham');
        expect(host(fixture).querySelector('#edit-student-national-id')?.getAttribute('dir')).toBe('ltr');
    });

    it('saves the changed details and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        internals(fixture).form.patchValue({ phone: ' 050-1234568 ', address: '  ' });

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234568',
            address: null,
            startDate: '2026-09-01',
            licenseType: 'B',
        });
        expect(close).toHaveBeenCalled();
    });

    it('disables Save while a required detail is empty', async () => {
        //given
        const { fixture } = await setUp();

        //when
        internals(fixture).form.controls.name.setValue('');
        await fixture.whenStable();

        //then
        expect(saveButton(fixture).disabled).toBe(true);
    });

    it('disables Save while saving', async () => {
        //given
        const { fixture, isSaving } = await setUp();

        //when
        isSaving.set(true);
        await fixture.whenStable();

        //then
        expect(saveButton(fixture).disabled).toBe(true);
    });

    describe('after a national ID refusal', () => {
        async function refusedWith(refusal: StudentRefusal): Promise<Setup> {
            const setup = await setUp(false);
            setup.confirm.mockImplementation(() => {
                setup.refusal.set(refusal);
                return Promise.resolve(false);
            });
            await internals(setup.fixture).submit();
            await setup.fixture.whenStable();

            return setup;
        }

        it('stays open, shows the refusal first and marks the national ID', async () => {
            //given
            const { fixture, close } = await refusedWith(NATIONAL_ID_IN_USE);

            //then
            expect(close).not.toHaveBeenCalled();
            expect(host(fixture).querySelector('app-dialog-refusal')).not.toBeNull();
            expect(host(fixture).querySelectorAll('.field__error').length).toBe(1);
            expect(saveButton(fixture).disabled).toBe(true);
        });

        it('clears the refusal when the national ID is edited', async () => {
            //given
            const { fixture, clearRefusal } = await refusedWith(NATIONAL_ID_IN_USE);

            //when
            await typeNationalId(fixture, '205374184');

            //then
            expect(clearRefusal).toHaveBeenCalled();
            expect(host(fixture).querySelector('app-dialog-refusal')).toBeNull();
            expect(saveButton(fixture).disabled).toBe(false);
        });

        it('marks the national ID for an invalid one too', async () => {
            //given
            const { fixture } = await refusedWith({
                kind: StudentRefusalKind.nationalIdInvalid,
                message: "This national ID isn't valid.",
                existingStudentName: null,
            });

            //then
            expect(host(fixture).querySelectorAll('.field__error').length).toBe(1);
        });
    });

    it('opens with blank optional details for a Student who has none', async () => {
        //given
        const { fixture } = await setUp(true, { ...NOA, address: null, startDate: null, licenseType: null });

        //then
        const value = internals(fixture).form.getRawValue();
        expect([value.address, value.startDate, value.licenseType]).toEqual(['', null, '']);
    });
});
```

Run from `client\` (PowerShell): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/edit-student/edit-student.dialog.spec.ts`
Expected: FAIL (`Cannot find module './edit-student-dialog-data'`).

- [ ] **Step 2: Add the dialog**

Create `client\src\app\features\students\ui\dialogs\edit-student\edit-student-dialog-data.ts`:

```ts
import { Signal } from '@angular/core';
import { StudentDetailsChange } from '../../../domain/student-details-change.model';
import { StudentDetails } from '../../../domain/student-details.model';
import { StudentRefusal } from '../../../domain/student-refusal';

export interface EditStudentDialogData {
    details: StudentDetails;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (change: StudentDetailsChange) => Promise<boolean>;
    clearRefusal: () => void;
}
```

Create `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { LockedFieldComponent } from '../../../../../shared/components/locked-field/locked-field.component';
import { toEditStudentFormValue, toStudentDetailsChange } from '../../../domain/student-details';
import { StudentRefusalKind } from '../../../domain/student-refusal';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { EditStudentDialogData } from './edit-student-dialog-data';

const REFUSED = 'refused';
const VALID = 'VALID';
const NATIONAL_ID_REFUSALS: ReadonlySet<StudentRefusalKind> = new Set([
    StudentRefusalKind.nationalIdInUse,
    StudentRefusalKind.nationalIdInvalid,
]);

@Component({
    selector: 'app-edit-student-dialog',
    imports: [
        ReactiveFormsModule,
        TranslocoPipe,
        ButtonModule,
        DatePickerModule,
        InputTextModule,
        DialogRefusalComponent,
        LockedFieldComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './edit-student.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './edit-student.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditStudentDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<EditStudentDialogData>).data as EditStudentDialogData;

    protected readonly details = this.data.details;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = StudentRefusalKind;
    protected readonly refused = REFUSED;

    private readonly initial = toEditStudentFormValue(this.details);

    protected readonly form = this.fb.group({
        nationalId: this.fb.nonNullable.control(this.initial.nationalId, Validators.required),
        name: this.fb.nonNullable.control(this.initial.name, Validators.required),
        phone: this.fb.nonNullable.control(this.initial.phone, Validators.required),
        address: this.fb.nonNullable.control(this.initial.address),
        startDate: this.fb.control<Date | null>(this.initial.startDate),
        licenseType: this.fb.nonNullable.control(this.initial.licenseType),
    });

    private readonly formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

    protected readonly canSave = computed(() => this.formStatus() === VALID && !this.isSaving());

    protected async submit(): Promise<void> {
        if (!this.canSave()) {
            return;
        }

        const change = toStudentDetailsChange(this.form.getRawValue());
        const saved = await this.data.confirm(change);

        if (saved) {
            this.ref.close();
            return;
        }

        this.markRefusedNationalId();
    }

    protected onNationalIdInput(): void {
        const kind = this.refusal()?.kind;

        if (kind && NATIONAL_ID_REFUSALS.has(kind)) {
            this.data.clearRefusal();
        }
    }

    protected close(): void {
        this.ref.close();
    }

    private markRefusedNationalId(): void {
        const kind = this.refusal()?.kind;

        if (!kind || !NATIONAL_ID_REFUSALS.has(kind)) {
            return;
        }

        const control = this.form.controls.nationalId;
        control.setErrors({ [REFUSED]: true });
        control.markAsDirty();
    }
}
```

(Typing in the national ID re-runs the validators, which drops the `refused` error, and `clearRefusal()` removes the banner; Save comes back. `startDate` is nullable so the form value matches `EditStudentFormValue` exactly.)

Create `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.html`:

```html
@let refusedNow = refusal();
@let refusalText =
  refusedNow?.kind === refusalKinds.nationalIdInvalid
    ? ('students.add.nationalIdInvalid' | transloco)
    : refusedNow?.kind === refusalKinds.nationalIdInUse
      ? ('students.edit.nationalIdUsed' | transloco: { name: refusedNow?.existingStudentName ?? '' })
      : refusedNow?.message;

<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  <p class="edit-student__subtitle">{{ details.name }}</p>

  @if (refusedNow) {
    <app-dialog-refusal [message]="refusalText ?? ''" />
  }

  <div class="field">
    <label for="edit-student-national-id">{{ 'students.add.nationalId' | transloco }}</label>
    <input
      pInputText
      id="edit-student-national-id"
      formControlName="nationalId"
      (input)="onNationalIdInput()"
      dir="ltr"
      inputmode="numeric"
      autocomplete="off" />
    @if (form.controls.nationalId.hasError(refused)) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ refusalText }}</small>
    } @else {
      <small class="field__hint edit-student__note">
        <i class="pi pi-info-circle" aria-hidden="true"></i>
        <span>{{ 'students.edit.nationalIdNote' | transloco }}</span>
      </small>
    }
  </div>

  <div class="field">
    <label for="edit-student-name">{{ 'students.add.fullName' | transloco }}</label>
    <input pInputText id="edit-student-name" formControlName="name" autocomplete="off" />
  </div>

  <div class="field">
    <label for="edit-student-phone">{{ 'students.add.phone' | transloco }}</label>
    <input pInputText id="edit-student-phone" formControlName="phone" type="tel" dir="ltr" autocomplete="off" />
  </div>

  <div class="field">
    <label for="edit-student-address">{{ 'students.add.address' | transloco }}</label>
    <input pInputText id="edit-student-address" formControlName="address" autocomplete="off" />
  </div>

  <div class="edit-student__pair">
    <div class="field">
      <label for="edit-student-start-date">{{ 'students.add.startDate' | transloco }}</label>
      <p-datepicker
        inputId="edit-student-start-date"
        formControlName="startDate"
        dateFormat="dd/mm/yy"
        [showIcon]="true"
        appendTo="body"
        [placeholder]="'students.add.datePlaceholder' | transloco" />
    </div>
    <div class="field">
      <label for="edit-student-license-type">{{ 'students.add.licenseType' | transloco }}</label>
      <input
        pInputText
        id="edit-student-license-type"
        formControlName="licenseType"
        autocomplete="off"
        [placeholder]="'students.add.licenseTypePlaceholder' | transloco" />
    </div>
  </div>

  <app-locked-field [label]="'students.edit.teacherAndCar' | transloco" [value]="details.teacherName">
    <span class="edit-student__car">
      · {{ details.carName }}
      <app-transmission-tag [transmission]="details.carTransmission" />
    </span>
  </app-locked-field>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="close()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="!canSave()" [loading]="isSaving()" />
  </div>
</form>
```

Create `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.scss`:

```scss
.edit-student__subtitle {
    margin: -0.5rem 0 0;
    font-size: 0.875rem;
    color: var(--app-text-secondary);
}

.edit-student__note {
    display: flex;
    align-items: flex-start;
    gap: 0.375rem;

    .pi {
        margin-block-start: 0.15rem;
        font-size: 0.875rem;
    }
}

.edit-student__pair {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    gap: 0.875rem;
}

:host ::ng-deep .edit-student__pair p-datepicker,
:host ::ng-deep .edit-student__pair .p-datepicker,
:host ::ng-deep .edit-student__pair .p-datepicker-input {
    display: flex;
    inline-size: 100%;
}

.edit-student__car {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    color: var(--app-ink);
}
```

(Frame 4a: the Student's name under the title, national ID first with the info-icon note, name, phone (LTR), address, start date and license type side by side, then the locked "מורה ורכב" field (lock icon · Teacher · Car + transmission tag). The copy deck's `edit.tcHint` is left for #95, README decision 16.)

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/edit-student/edit-student.dialog.spec.ts`
Expected: PASS, 8 tests.

- [ ] **Step 3: Open the dialog from the row menu**

In `client\src\app\features\students\ui\pages\students\students.page.ts`:

1. Add the imports after the `DeactivateStudentDialog` import:

```ts
import { EditStudentDialog } from '../../dialogs/edit-student/edit-student.dialog';
import { EditStudentDialogData } from '../../dialogs/edit-student/edit-student-dialog-data';
```

2. Replace `activeRowActions` and `inactiveRowActions` with:

```ts
    private activeRowActions(student: StudentRow): MenuItem[] {
        return [
            this.editDetailsAction(student),
            { separator: true },
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
            this.editDetailsAction(student),
            {
                label: this.transloco.translate('students.actions.reactivate'),
                icon: 'pi pi-replay',
                command: () => void this.store.reactivate(student),
            },
        ];
    }

    private editDetailsAction(student: StudentRow): MenuItem {
        return {
            label: this.transloco.translate('students.actions.editDetails'),
            icon: 'pi pi-pencil',
            command: () => void this.onEditDetails(student),
        };
    }

    private async onEditDetails(student: StudentRow): Promise<void> {
        const details = await this.store.loadDetails(student.id);

        if (!details) {
            return;
        }

        const data: EditStudentDialogData = {
            details,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (change) => this.store.changeDetails(student.id, change),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(EditStudentDialog, this.transloco.translate('students.edit.title'), StudentsPage.dialogWidth, data);
    }
```

(Frames 2e / 2f: Edit details first in both menus; a separator before the danger Deactivate. The details are fetched fresh, README decision 11; a failed fetch is already an error toast from the store.)

- [ ] **Step 4: Add the translations**

In `client\public\i18n\he.json`, inside `students.actions` (task 5), add `"editDetails": "עריכת פרטים",` as the first entry, so it reads:

```json
    "actions": {
      "editDetails": "עריכת פרטים",
      "menu": "פעולות",
```

and inside `students`, right after the `"reactivatedDetail": ...` line (task 5), add:

```json
    "edit": {
      "title": "עריכת פרטים",
      "nationalIdNote": "השינוי כאן לא משנה את קובץ רשימת התלמידים. קובץ מאוחר יותר עם תעודת הזהות הישנה יוסיף תלמיד שני, ולכן צריך לתקן גם את הקובץ.",
      "nationalIdUsed": "תעודת הזהות הזו שייכת לתלמיד אחר: {{name}}.",
      "teacherAndCar": "מורה ורכב"
    },
    "detailsSaved": "הפרטים נשמרו",
```

In `client\public\i18n\en.json`, the same places:

```json
    "actions": {
      "editDetails": "Edit details",
      "menu": "Actions",
```

```json
    "edit": {
      "title": "Edit details",
      "nationalIdNote": "Changing it here doesn't change the Roster file. A later Roster file with the old ID would add a second Student, so correct the file too.",
      "nationalIdUsed": "This national ID belongs to another Student: {{name}}.",
      "teacherAndCar": "Teacher and Car"
    },
    "detailsSaved": "Details saved",
```

(Copy deck `act.edit`, `edit.title`, `edit.nidNote`, `err.nidUsed`, `edit.tc`, `toast.saved`.)

- [ ] **Step 5: Run the client suites and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS; the build succeeds.

- [ ] **Step 6: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Edit details dialog for a Student, with inline national ID refusals (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
