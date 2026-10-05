# Task 5 of 6: Add Student dialog with the Teacher → Car pick (client)

> Part of [#93: Students screen: list, filter and add a Student by hand](README.md). Requires task 4 committed. Work on branch `93-students-screen`. Read README decisions 6, 12, 17 to 19 and 22, and the README "Design" section (frames 3a to 3i) first.

**Files:**
- Create: `client\src\app\features\students\domain\car-pick.ts`
- Test: `client\src\app\features\students\domain\car-pick.spec.ts`
- Create: `client\src\app\features\students\domain\new-student.ts`
- Test: `client\src\app\features\students\domain\new-student.spec.ts`
- Create: `client\src\app\features\students\ui\dialogs\add-student\add-student-dialog-data.ts`
- Create: `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.ts`, `.html`, `.scss`
- Modify: `client\src\app\shared\components\dialog-refusal\dialog-refusal.component.html`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`, `.html`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: from task 3 `CarOption`, `TeacherOption`, `NewStudent`, `Transmission`, `AddStudentRefusal`, `AddStudentRefusalKind`, `StudentsStore` (`teacherOptions`, `carOptions`, `refusal`, `isMutating`, `create(newStudent): Promise<boolean>`, `refreshCarOptions()`, `clearRefusal()`, `loadError`); from task 4 `TransmissionTagComponent`, `StudentsPage` and its `importRosterLink`; `DialogRefusalComponent` (`shared\components\dialog-refusal\`, input `message`); `shared\dialogs\dialog-form.scss` (`.dialog-form`, `.field`, `.field__hint`, `.field__error`, `.dialog-form__actions`); `AppRoutes.teachers`.
- Produces:
  - `pickCarFor(teacherId: string | null, cars: readonly CarOption[]): CarPick` with `CarPick { options: CarOption[]; selectedCarId: string | null; isLocked: boolean; hasNoCars: boolean; isOnlyCar: boolean }`, and `teacherHasCars(teacherId: string, cars: readonly CarOption[]): boolean` (the AC's "Teacher → Car pick logic").
  - `toNewStudent(value: AddStudentFormValue): NewStudent` with `AddStudentFormValue { nationalId; name; phone; teacherId: string; carId: string; address: string; startDate: Date | null; licenseType: string }`.
  - `AddStudentDialog` (`app-add-student-dialog`) fed by `AddStudentDialogData { teachers; cars; refusal; isSaving; confirm; refreshCars }`.
  - `DialogRefusalComponent` projects content after the message (used for the Refresh link; every existing use projects nothing and looks the same).
  - Translation keys `students.addStudent`, `students.added`, `students.addedDetail`, `students.add.*`.

**Why:** AC "the Car picker is populated from the chosen Teacher's Cars and resets when the Teacher changes", "client spec for the Teacher → Car pick logic", "national ID already exists is rejected (409, translated)" and design section 3. Review Focus 3 (a start date one day early), 4 (a shared Car silently kept across a Teacher change) and 5 (blank optional fields sent as `""` and refused) are pinned by the specs in steps 1 and 4.

**Design (frames 3a to 3i; spelled out so this task needs no MCP):**
- `DynamicDialog`, 560px (`35rem`), header "הוספת תלמיד". Fields stacked, 16px apart, in this order: תעודת זהות (LTR, numeric keyboard, hint "9 ספרות, כולל ספרת ביקורת") · שם מלא · טלפון (LTR) · מורה (Select, placeholder "בחירת מורה"; a Teacher without Cars shows the muted note "אין רכבים" at the option's inline end) · רכב (Select). Then a sub-heading "אופציונלי" (12px / 700, secondary, a top border) and כתובת, then two equal columns: תאריך התחלה (DatePicker `dd/mm/yy`, calendar icon, placeholder "dd/mm/yyyy") and סוג רישיון (placeholder "לדוגמה B"). Footer: text "ביטול", primary "שמירה", disabled until the form is valid and a Car is chosen.
- Car (3a): disabled with placeholder "יש לבחור מורה קודם" until a Teacher is chosen. 3b: only that Teacher's Cars, each option "name + transmission tag", hint "מוצגים רק הרכבים של {Teacher}." 3c: a Teacher with exactly one Car has it preselected, hint "זה הרכב היחיד של {Teacher}, ולכן הוא נבחר." The Car empties whenever the Teacher changes.
- 3d: a Teacher with no Cars: the Car stays disabled ("בחירת רכב") and an info message sits under it: "למורה הזה עדיין אין רכבים. יש לשייך רכב במסך רכבים ומורים." with the link "מעבר לרכבים ומורים"; Save stays disabled.
- 3e / 3f / 3g (server refusals): an error message at the top of the dialog, title "הפעולה לא בוצעה", then the text; the related field turns invalid with the same text under it. 3e: "כבר קיים תלמיד עם תעודת הזהות הזו: {name}." on תעודת זהות. 3f: "תעודת הזהות לא תקינה. יש לבדוק שיש 9 ספרות ושספרת הביקורת נכונה." on תעודת זהות (for every national-ID rule). 3g: "הרכב הזה לא משויך למורה הזה. יש לרענן ולבחור שוב." on רכב, plus a "רענון" link (refresh icon) in the message that reloads the Car options.
- 3h: the dialog closes, toast "התלמיד נוסף" / "{name} נוסף/ה לרשימה של {Teacher}.", the new row first in the list, tinted, with the "חדש" tag (task 3 and 4).
- 3i: the same dialog in English, LTR.

- [ ] **Step 1: Write the failing Teacher → Car pick specs**

Create `client\src\app\features\students\domain\car-pick.spec.ts`:

```typescript
import { CarOption } from './car-option.model';
import { pickCarFor, teacherHasCars } from './car-pick';
import { Transmission } from './transmission.enum';

const COROLLA: CarOption = {
    id: 'car-corolla',
    name: 'Corolla White',
    transmission: Transmission.automatic,
    teacherIds: ['teacher-ronit'],
};

const I20: CarOption = {
    id: 'car-i20',
    name: 'i20 Silver',
    transmission: Transmission.manual,
    teacherIds: ['teacher-ronit', 'teacher-yael'],
};

const PICANTO: CarOption = {
    id: 'car-picanto',
    name: 'Picanto Red',
    transmission: Transmission.automatic,
    teacherIds: ['teacher-yael'],
};

const MAZDA: CarOption = {
    id: 'car-mazda',
    name: 'Mazda 3 Grey',
    transmission: Transmission.manual,
    teacherIds: ['teacher-oren'],
};

const CARS = [PICANTO, MAZDA, I20, COROLLA];

describe('pickCarFor', () => {
    it('locks the Car until a Teacher is chosen', () => {
        //when
        const pick = pickCarFor(null, CARS);

        //then
        expect(pick).toEqual({ options: [], selectedCarId: null, isLocked: true, hasNoCars: false, isOnlyCar: false });
    });

    it("lists only the chosen Teacher's Cars, by name, with nothing chosen yet", () => {
        //when
        const pick = pickCarFor('teacher-yael', CARS);

        //then
        expect(pick).toEqual({
            options: [I20, PICANTO],
            selectedCarId: null,
            isLocked: false,
            hasNoCars: false,
            isOnlyCar: false,
        });
    });

    it('lists a shared Car for each of its Teachers', () => {
        //when
        const forRonit = pickCarFor('teacher-ronit', CARS);
        const forYael = pickCarFor('teacher-yael', CARS);

        //then
        expect(forRonit.options).toEqual([COROLLA, I20]);
        expect(forYael.options).toContain(I20);
    });

    it("preselects a Teacher's only Car", () => {
        //when
        const pick = pickCarFor('teacher-oren', CARS);

        //then
        expect(pick.options).toEqual([MAZDA]);
        expect(pick.selectedCarId).toBe('car-mazda');
        expect(pick.isOnlyCar).toBe(true);
    });

    it('selects nothing for a Teacher with no Cars', () => {
        //when
        const pick = pickCarFor('teacher-michal', CARS);

        //then
        expect(pick).toEqual({ options: [], selectedCarId: null, isLocked: false, hasNoCars: true, isOnlyCar: false });
    });

    it('starts empty again when the Teacher changes, even to a Teacher who shares the chosen Car', () => {
        //given
        const forRonit = pickCarFor('teacher-ronit', CARS);
        const chosen = forRonit.options.find((car) => car.id === I20.id);

        //when
        const forYael = pickCarFor('teacher-yael', CARS);

        //then
        expect(chosen).toEqual(I20);
        expect(forYael.options).toContain(I20);
        expect(forYael.selectedCarId).toBeNull();
    });
});

describe('teacherHasCars', () => {
    it('knows a Teacher with at least one Car', () => {
        //expected
        expect(teacherHasCars('teacher-yael', CARS)).toBe(true);
    });

    it('knows a Teacher with no Cars', () => {
        //expected
        expect(teacherHasCars('teacher-michal', CARS)).toBe(false);
    });
});
```

- [ ] **Step 2: Run them to see them fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/domain/car-pick.spec.ts
```

Expected: FAIL: the spec can't resolve `./car-pick`.

- [ ] **Step 3: Write the pick logic**

Create `client\src\app\features\students\domain\car-pick.ts`:

```typescript
import { CarOption } from './car-option.model';

export interface CarPick {
    options: CarOption[];
    selectedCarId: string | null;
    isLocked: boolean;
    hasNoCars: boolean;
    isOnlyCar: boolean;
}

const SINGLE_CAR_COUNT = 1;

export function pickCarFor(teacherId: string | null, cars: readonly CarOption[]): CarPick {
    if (!teacherId) {
        return { options: [], selectedCarId: null, isLocked: true, hasNoCars: false, isOnlyCar: false };
    }

    const options = carsOf(teacherId, cars);
    const isOnlyCar = options.length === SINGLE_CAR_COUNT;

    return {
        options,
        selectedCarId: isOnlyCar ? options[0].id : null,
        isLocked: false,
        hasNoCars: !options.length,
        isOnlyCar,
    };
}

export function teacherHasCars(teacherId: string, cars: readonly CarOption[]): boolean {
    return cars.some((car) => car.teacherIds.includes(teacherId));
}

function carsOf(teacherId: string, cars: readonly CarOption[]): CarOption[] {
    return cars
        .filter((car) => car.teacherIds.includes(teacherId))
        .sort((first, second) => first.name.localeCompare(second.name));
}
```

The pick never looks at the previous choice, so applying it on every Teacher change is the reset (README decision 18). Whether a Car may serve a Teacher stays the server's rule; this only narrows the list, and the server refuses a stale choice (frame 3g).

- [ ] **Step 4: Write the failing form-to-request specs**

Create `client\src\app\features\students\domain\new-student.spec.ts`:

```typescript
import { AddStudentFormValue, toNewStudent } from './new-student';

const FORM: AddStudentFormValue = {
    nationalId: ' 123456782 ',
    name: ' Shaked Navon ',
    phone: ' 050-3318842 ',
    teacherId: 'teacher-yael',
    carId: 'car-picanto',
    address: ' 12 HaRimon St, Modiin ',
    startDate: new Date(2026, 8, 1),
    licenseType: ' B ',
};

describe('toNewStudent', () => {
    it('trims every text', () => {
        //when
        const newStudent = toNewStudent(FORM);

        //then
        expect(newStudent).toEqual({
            nationalId: '123456782',
            name: 'Shaked Navon',
            phone: '050-3318842',
            teacherId: 'teacher-yael',
            carId: 'car-picanto',
            address: '12 HaRimon St, Modiin',
            startDate: '2026-09-01',
            licenseType: 'B',
        });
    });

    it('sends blank optional details as absent, never as empty text', () => {
        //given
        const form: AddStudentFormValue = { ...FORM, address: '   ', startDate: null, licenseType: '' };

        //when
        const newStudent = toNewStudent(form);

        //then
        expect(newStudent.address).toBeNull();
        expect(newStudent.startDate).toBeNull();
        expect(newStudent.licenseType).toBeNull();
    });

    it("keeps the picked start date's calendar day, late in the evening too", () => {
        //given
        const lateEvening: AddStudentFormValue = { ...FORM, startDate: new Date(2026, 8, 1, 23, 30) };
        const earlyJanuary: AddStudentFormValue = { ...FORM, startDate: new Date(2027, 0, 5) };

        //when
        const late = toNewStudent(lateEvening);
        const early = toNewStudent(earlyJanuary);

        //then
        expect(late.startDate).toBe('2026-09-01');
        expect(early.startDate).toBe('2027-01-05');
    });
});
```

(A `Date` from the DatePicker is local midnight; `toISOString()` would give the previous day anywhere east of UTC, Israel included.)

- [ ] **Step 5: Run them to see them fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/domain/new-student.spec.ts
```

Expected: FAIL: the spec can't resolve `./new-student`.

- [ ] **Step 6: Write the form-to-request mapping**

Create `client\src\app\features\students\domain\new-student.ts`:

```typescript
import { NewStudent } from './new-student.model';

export interface AddStudentFormValue {
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    carId: string;
    address: string;
    startDate: Date | null;
    licenseType: string;
}

const MONTH_OFFSET = 1;
const DATE_PART_LENGTH = 2;
const DATE_PART_PAD = '0';

export function toNewStudent(value: AddStudentFormValue): NewStudent {
    return {
        nationalId: value.nationalId.trim(),
        name: value.name.trim(),
        phone: value.phone.trim(),
        teacherId: value.teacherId,
        carId: value.carId,
        address: optionalText(value.address),
        startDate: value.startDate ? toIsoDate(value.startDate) : null,
        licenseType: optionalText(value.licenseType),
    };
}

function optionalText(value: string): string | null {
    const trimmed = value.trim();

    return trimmed ? trimmed : null;
}

function toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + MONTH_OFFSET).padStart(DATE_PART_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_LENGTH, DATE_PART_PAD);

    return `${year}-${month}-${day}`;
}
```

- [ ] **Step 7: Run both domain specs**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/students/domain/*.spec.ts"
```

Expected: PASS, 11 specs.

- [ ] **Step 8: Let the refusal message carry a link**

Replace `client\src\app\shared\components\dialog-refusal\dialog-refusal.component.html` with:

```html
<p-message severity="error">
  <span class="dialog-refusal__text">
    <strong>{{ 'general.refusedTitle' | transloco }}</strong>
    <span>{{ message() }}</span>
    <ng-content />
  </span>
</p-message>
```

Every existing `<app-dialog-refusal [message]="..." />` projects nothing, so they render as before.

- [ ] **Step 9: The dialog**

Create `client\src\app\features\students\ui\dialogs\add-student\add-student-dialog-data.ts`:

```typescript
import { Signal } from '@angular/core';
import { AddStudentRefusal } from '../../../domain/add-student-refusal';
import { CarOption } from '../../../domain/car-option.model';
import { NewStudent } from '../../../domain/new-student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';

export interface AddStudentDialogData {
    teachers: Signal<TeacherOption[]>;
    cars: Signal<CarOption[]>;
    refusal: Signal<AddStudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (newStudent: NewStudent) => Promise<boolean>;
    refreshCars: () => void;
}
```

Create `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { AddStudentRefusalKind } from '../../../domain/add-student-refusal';
import { pickCarFor, teacherHasCars } from '../../../domain/car-pick';
import { toNewStudent } from '../../../domain/new-student';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { AddStudentDialogData } from './add-student-dialog-data';

const REFUSED = 'refused';
const VALID = 'VALID';

interface TeacherChoice {
    id: string;
    name: string;
    hasCars: boolean;
}

@Component({
    selector: 'app-add-student-dialog',
    imports: [
        FormsModule,
        ReactiveFormsModule,
        RouterLink,
        TranslocoPipe,
        ButtonModule,
        DatePickerModule,
        InputTextModule,
        MessageModule,
        SelectModule,
        DialogRefusalComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './add-student.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './add-student.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddStudentDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<AddStudentDialogData>).data as AddStudentDialogData;

    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = AddStudentRefusalKind;
    protected readonly refused = REFUSED;
    protected readonly carsAndTeachersLink = ['/', AppRoutes.teachers];

    protected readonly form = this.fb.group({
        nationalId: this.fb.nonNullable.control('', Validators.required),
        name: this.fb.nonNullable.control('', Validators.required),
        phone: this.fb.nonNullable.control('', Validators.required),
        teacherId: this.fb.control<string | null>(null, Validators.required),
        address: this.fb.nonNullable.control(''),
        startDate: this.fb.control<Date | null>(null),
        licenseType: this.fb.nonNullable.control(''),
    });

    private readonly teacherId = toSignal(this.form.controls.teacherId.valueChanges, {
        initialValue: this.form.controls.teacherId.value,
    });

    private readonly formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

    protected readonly teacherOptions = computed<TeacherChoice[]>(() => {
        const cars = this.data.cars();

        return this.data.teachers().map((teacher) => ({ ...teacher, hasCars: teacherHasCars(teacher.id, cars) }));
    });

    protected readonly carPick = computed(() => pickCarFor(this.teacherId(), this.data.cars()));

    protected readonly carId = linkedSignal(() => this.carPick().selectedCarId);

    protected readonly isCarRefused = linkedSignal({
        source: () => ({ carId: this.carId(), cars: this.data.cars() }),
        computation: () => false,
    });

    protected readonly teacherName = computed(
        () => this.data.teachers().find((teacher) => teacher.id === this.teacherId())?.name ?? '',
    );

    protected readonly canSave = computed(
        () => this.formStatus() === VALID && this.carId() !== null && !this.isCarRefused() && !this.isSaving(),
    );

    protected selectCar(carId: string | null): void {
        this.carId.set(carId);
    }

    protected async submit(): Promise<void> {
        const value = this.form.getRawValue();
        const carId = this.carId();

        if (!this.canSave() || !value.teacherId || !carId) {
            return;
        }

        const newStudent = toNewStudent({ ...value, teacherId: value.teacherId, carId });
        const saved = await this.data.confirm(newStudent);

        if (saved) {
            this.ref.close();
            return;
        }

        this.markRefusedField();
    }

    protected onRefreshCars(): void {
        this.data.refreshCars();
    }

    protected close(): void {
        this.ref.close();
    }

    private markRefusedField(): void {
        const kind = this.refusal()?.kind;

        if (kind === AddStudentRefusalKind.staleCar) {
            this.isCarRefused.set(true);
            return;
        }

        const control = this.refusedControl(kind);

        if (!control) {
            return;
        }

        control.setErrors({ [REFUSED]: true });
        control.markAsDirty();
    }

    private refusedControl(kind: AddStudentRefusalKind | undefined): AbstractControl | null {
        switch (kind) {
            case AddStudentRefusalKind.nationalIdInUse:
            case AddStudentRefusalKind.nationalIdInvalid:
                return this.form.controls.nationalId;
            default:
                return null;
        }
    }
}
```

The Car is not a form control: `carId` is a `linkedSignal` derived from `carPick()` (README decision 18, `client-state.md`: "derived, but the user can override it" is `linkedSignal`, never an `effect()`). It resets to the pick's selection, empty or a Teacher's only Car, on every Teacher change, and again when the Car options reload after "רענון" or arrive after the Teacher was chosen. The Administrator's choice overrides it through `selectCar`. So the Car list, the selection and the disabled state never disagree, and `canSave` requires a Car. A refused national ID gets a `refused` error on its control and turns invalid until the Administrator edits it. A stale Car sets `isCarRefused`, which resets itself when the Car or the Car options change. Either way, Save can't resend the same refusal.

Create `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.html`:

```html
@let refusedNow = refusal();
@let refusalText = refusedNow?.kind === refusalKinds.nationalIdInvalid ? ('students.add.nationalIdInvalid' | transloco) : refusedNow?.message;

<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
  @if (refusedNow) {
    <app-dialog-refusal [message]="refusalText ?? ''">
      @if (refusedNow.kind === refusalKinds.staleCar) {
        <button type="button" class="add-student__refresh" (click)="onRefreshCars()">
          <i class="pi pi-refresh" aria-hidden="true"></i>
          {{ 'general.refresh' | transloco }}
        </button>
      }
    </app-dialog-refusal>
  }

  <div class="field">
    <label for="student-national-id">{{ 'students.add.nationalId' | transloco }}</label>
    <input
      pInputText
      id="student-national-id"
      formControlName="nationalId"
      dir="ltr"
      inputmode="numeric"
      autocomplete="off" />
    @if (form.controls.nationalId.hasError(refused)) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ refusalText }}</small>
    } @else {
      <small class="field__hint">{{ 'students.add.nationalIdHint' | transloco }}</small>
    }
  </div>

  <div class="field">
    <label for="student-name">{{ 'students.add.fullName' | transloco }}</label>
    <input pInputText id="student-name" formControlName="name" autocomplete="off" />
  </div>

  <div class="field">
    <label for="student-phone">{{ 'students.add.phone' | transloco }}</label>
    <input pInputText id="student-phone" formControlName="phone" type="tel" dir="ltr" autocomplete="off" />
  </div>

  <div class="field">
    <label for="student-teacher">{{ 'students.add.teacher' | transloco }}</label>
    <p-select
      inputId="student-teacher"
      formControlName="teacherId"
      [options]="teacherOptions()"
      optionLabel="name"
      optionValue="id"
      [placeholder]="'students.add.teacherPlaceholder' | transloco"
      appendTo="body"
      fluid>
      <ng-template pTemplate="item" let-teacher>
        <span class="add-student__option">
          <span>{{ teacher.name }}</span>
          @if (!teacher.hasCars) {
            <small class="add-student__option-note">{{ 'students.add.noCarsOption' | transloco }}</small>
          }
        </span>
      </ng-template>
    </p-select>
  </div>

  <div class="field">
    <label for="student-car">{{ 'students.add.car' | transloco }}</label>
    <p-select
      inputId="student-car"
      [ngModel]="carId()"
      (ngModelChange)="selectCar($event)"
      [ngModelOptions]="{ standalone: true }"
      [disabled]="!carPick().options.length"
      [class.ng-invalid]="isCarRefused()"
      [class.ng-dirty]="isCarRefused()"
      [options]="carPick().options"
      optionLabel="name"
      optionValue="id"
      [placeholder]="(carPick().isLocked ? 'students.add.carPlaceholderLocked' : 'students.add.carPlaceholder') | transloco"
      appendTo="body"
      fluid>
      <ng-template pTemplate="selectedItem" let-car>
        <span class="add-student__option">
          <span>{{ car.name }}</span>
          <app-transmission-tag [transmission]="car.transmission" />
        </span>
      </ng-template>
      <ng-template pTemplate="item" let-car>
        <span class="add-student__option">
          <span>{{ car.name }}</span>
          <app-transmission-tag [transmission]="car.transmission" />
        </span>
      </ng-template>
    </p-select>
    @if (carPick().hasNoCars) {
      <p-message severity="info" styleClass="add-student__no-cars">
        <span class="add-student__no-cars-text">
          <span>{{ 'students.add.noCars' | transloco }}</span>
          <a class="add-student__link" [routerLink]="carsAndTeachersLink" (click)="close()">{{ 'students.add.noCarsLink' | transloco }}</a>
        </span>
      </p-message>
    } @else if (isCarRefused()) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ refusalText }}</small>
    } @else if (!carPick().isLocked) {
      <small class="field__hint">
        {{ (carPick().isOnlyCar ? 'students.add.carOnly' : 'students.add.carScoped') | transloco: { teacher: teacherName() } }}
      </small>
    }
  </div>

  <p class="add-student__group">{{ 'students.add.optionalGroup' | transloco }}</p>

  <div class="field">
    <label for="student-address">{{ 'students.add.address' | transloco }}</label>
    <input pInputText id="student-address" formControlName="address" autocomplete="off" />
  </div>

  <div class="add-student__pair">
    <div class="field">
      <label for="student-start-date">{{ 'students.add.startDate' | transloco }}</label>
      <p-datepicker
        inputId="student-start-date"
        formControlName="startDate"
        dateFormat="dd/mm/yy"
        [showIcon]="true"
        appendTo="body"
        [placeholder]="'students.add.datePlaceholder' | transloco" />
    </div>
    <div class="field">
      <label for="student-license-type">{{ 'students.add.licenseType' | transloco }}</label>
      <input
        pInputText
        id="student-license-type"
        formControlName="licenseType"
        autocomplete="off"
        [placeholder]="'students.add.licenseTypePlaceholder' | transloco" />
    </div>
  </div>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="close()" />
    <p-button [label]="'general.save' | transloco" type="submit" [disabled]="!canSave()" [loading]="isSaving()" />
  </div>
</form>
```

Create `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.scss`:

```scss
.add-student__option {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    inline-size: 100%;
}

.add-student__option-note {
    margin-inline-start: auto;
    font-size: 0.78rem;
    color: var(--app-text-muted);
}

.add-student__group {
    margin: 0.25rem 0 0;
    padding-block-start: 0.375rem;
    border-block-start: 1px solid var(--app-border);
    font-size: 0.75rem;
    font-weight: 700;
    color: var(--app-text-secondary);
}

.add-student__pair {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    gap: 0.875rem;
}

:host ::ng-deep .add-student__pair p-datepicker,
:host ::ng-deep .add-student__pair .p-datepicker,
:host ::ng-deep .add-student__pair .p-datepicker-input {
    display: flex;
    inline-size: 100%;
}

.add-student__no-cars-text {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.add-student__link {
    font-weight: 600;
    color: var(--p-sky-500);
}

.add-student__refresh {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    margin-block-start: 0.375rem;
    padding: 0;
    border: 0;
    background: none;
    font: inherit;
    font-weight: 600;
    color: var(--p-sky-500);
    cursor: pointer;
}
```

- [ ] **Step 10: Open the dialog from the page**

Replace `client\src\app\features\students\ui\pages\students\students.page.ts` with:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
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
import { AddStudentDialog } from '../../dialogs/add-student/add-student.dialog';
import { AddStudentDialogData } from '../../dialogs/add-student/add-student-dialog-data';

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
    providers: [StudentsStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsPage {
    private static readonly dialogWidth = '35rem';

    protected readonly store = inject(StudentsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

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

    protected onAddStudent(): void {
        this.store.clearRefusal();

        const data: AddStudentDialogData = {
            teachers: this.store.teacherOptions,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (newStudent) => this.store.create(newStudent),
            refreshCars: () => this.store.refreshCarOptions(),
        };

        this.dialogs.open(AddStudentDialog, {
            header: this.transloco.translate('students.add.title'),
            width: StudentsPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
```

In `client\src\app\features\students\ui\pages\students\students.page.html`, replace the header actions

```html
    <div class="students__actions">
      <a
        pButton
        severity="secondary"
        [outlined]="true"
        icon="pi pi-upload"
        [label]="'students.importRoster' | transloco"
        [routerLink]="importRosterLink"></a>
    </div>
```

with

```html
    <div class="students__actions">
      <a
        pButton
        severity="secondary"
        [outlined]="true"
        icon="pi pi-upload"
        [label]="'students.importRoster' | transloco"
        [routerLink]="importRosterLink"></a>
      <p-button
        [label]="'students.addStudent' | transloco"
        [disabled]="store.isMutating() || !!store.loadError()"
        (onClick)="onAddStudent()" />
    </div>
```

and the empty-state actions

```html
        <div class="students__empty-actions">
          <a
            pButton
            severity="secondary"
            [outlined]="true"
            size="small"
            [label]="'students.importRoster' | transloco"
            [routerLink]="importRosterLink"></a>
        </div>
```

with

```html
        <div class="students__empty-actions">
          <a
            pButton
            severity="secondary"
            [outlined]="true"
            size="small"
            [label]="'students.importRoster' | transloco"
            [routerLink]="importRosterLink"></a>
          <p-button
            [label]="'students.addStudent' | transloco"
            size="small"
            [disabled]="store.isMutating()"
            (onClick)="onAddStudent()" />
        </div>
```

- [ ] **Step 11: Translations, Hebrew first**

In `client\public\i18n\he.json`, replace the line

```json
    "clearFilters": "ניקוי הסינון"
```

with

```json
    "clearFilters": "ניקוי הסינון",
    "addStudent": "הוספת תלמיד",
    "added": "התלמיד נוסף",
    "addedDetail": "{{name}} נוסף/ה לרשימה של {{teacher}}.",
    "add": {
      "title": "הוספת תלמיד",
      "nationalId": "תעודת זהות",
      "nationalIdHint": "9 ספרות, כולל ספרת ביקורת",
      "nationalIdInvalid": "תעודת הזהות לא תקינה. יש לבדוק שיש 9 ספרות ושספרת הביקורת נכונה.",
      "fullName": "שם מלא",
      "phone": "טלפון",
      "teacher": "מורה",
      "teacherPlaceholder": "בחירת מורה",
      "noCarsOption": "אין רכבים",
      "car": "רכב",
      "carPlaceholderLocked": "יש לבחור מורה קודם",
      "carPlaceholder": "בחירת רכב",
      "carScoped": "מוצגים רק הרכבים של {{teacher}}.",
      "carOnly": "זה הרכב היחיד של {{teacher}}, ולכן הוא נבחר.",
      "noCars": "למורה הזה עדיין אין רכבים. יש לשייך רכב במסך רכבים ומורים.",
      "noCarsLink": "מעבר לרכבים ומורים",
      "optionalGroup": "אופציונלי",
      "address": "כתובת",
      "startDate": "תאריך התחלה",
      "datePlaceholder": "dd/mm/yyyy",
      "licenseType": "סוג רישיון",
      "licenseTypePlaceholder": "לדוגמה B"
    }
```

In `client\public\i18n\en.json`, replace the line

```json
    "clearFilters": "Clear filters"
```

with

```json
    "clearFilters": "Clear filters",
    "addStudent": "Add Student",
    "added": "Student added",
    "addedDetail": "{{name}} is now on {{teacher}}'s list.",
    "add": {
      "title": "Add Student",
      "nationalId": "National ID",
      "nationalIdHint": "9 digits, including the check digit",
      "nationalIdInvalid": "This national ID isn't valid. Check it has 9 digits and the check digit is right.",
      "fullName": "Full name",
      "phone": "Phone",
      "teacher": "Teacher",
      "teacherPlaceholder": "Choose a Teacher",
      "noCarsOption": "No Cars",
      "car": "Car",
      "carPlaceholderLocked": "Choose a Teacher first",
      "carPlaceholder": "Choose a Car",
      "carScoped": "Only {{teacher}}'s Cars are listed.",
      "carOnly": "{{teacher}}'s only Car, so it's selected.",
      "noCars": "This Teacher has no Cars yet. Assign one in Cars & Teachers.",
      "noCarsLink": "Go to Cars & Teachers",
      "optionalGroup": "Optional",
      "address": "Address",
      "startDate": "Start date",
      "datePlaceholder": "dd/mm/yyyy",
      "licenseType": "License type",
      "licenseTypePlaceholder": "e.g. B"
    }
```

(Copy deck group "Add Student" verbatim with `{t}` as `{{teacher}}` and `{name}` as `{{name}}`; `noCarsOption` is `opt.noCars`, which the design's Teacher picker also shows in Add Student; `stu.add` is Title Case. The refusals reuse `general.refusedTitle` and `general.refresh`, and the 409 texts are the `errors.*` keys from task 2.)

- [ ] **Step 12: Run the whole client suite and the build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (`translations.spec.ts`, `source-text.spec.ts`, the Users specs untouched by the refusal change), build succeeds.

- [ ] **Step 13: Commit**

```bash
git add client/src/app/features/students client/src/app/shared/components/dialog-refusal/dialog-refusal.component.html client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Add Student dialog with the Car scoped to the chosen Teacher (#93)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
