# Task 6 of 7: Change Car dialog, Fix badge and Edit details hint (client)

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires task 5 committed. Work on branch `95-change-teacher-and-car`. Read README decisions 14 to 18, 20, 21 and design frames 2d, 2e, 7a to 7f first.

**Files:**
- Create: `client\src\app\features\students\ui\dialogs\change-car\change-car-dialog-data.ts`
- Create: `client\src\app\features\students\ui\dialogs\change-car\change-car.dialog.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`, `.scss`
- Modify: `client\src\app\features\students\ui\dialogs\edit-student\edit-student.dialog.html`, `edit-student.dialog.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`general.close`, `students.actions.changeCar`, `.fix`, `students.changeCar`, `students.edit.teacherAndCarHint`)

**Interfaces:**
- Consumes: task 4 (`carChangeFor(student, cars): CarChange { choices: CarChoice[]; hasOtherCars }`, `CarChoice extends CarOption { isCurrent }`, `StudentRefusalKind.sameCar` / `.staleCar`, `StudentsStore.changeCar(student, carId)`, `.reload()`, `.clearRefusal()`, `.carOptions`, `.refusal`, `.isMutating`), task 3 (`Student.isCarOfTeacher`), task 5 (`students.changeTeacher.current` "(נוכחי)", the page's `onChangeTeacher`, `activeRowActions` shape), `StudentWhoCardComponent`, `TransmissionTagComponent`, `DialogRefusalComponent`, PrimeNG `RadioButtonModule` (`primeng/radiobutton`, `p-radiobutton` with `inputId`, `name`, `value`, `ngModel`, `disabled`), PrimeNG Menu's `MenuItem.badge` / `badgeStyleClass` (rendered as `p-badge`, verified in `primeng-menu.mjs`).
- Produces (task 7 relies on these):
  - `ChangeCarDialogData { student: Student; cars: Signal<CarOption[]>; refusal: Signal<StudentRefusal | null>; isSaving: Signal<boolean>; confirm: (carId: string) => Promise<boolean>; refresh: () => void; clearRefusal: () => void }`.
  - `ChangeCarDialog` (`app-change-car-dialog`).
  - Final row menu of an active Student: Edit details · Change Teacher · Change Car (Fix badge when flagged) · separator · Deactivate.

Layout (design 7a, 560px): refusal Message (when refused) · who card · flag Message (flagged Student only) · either the "no other Cars" info Message, or the label "רכב חדש" + radio cards + hint "מוצגים רק הרכבים של {t}." · footer: text "ביטול" + primary "החלפת רכב", or only a secondary "סגירה" when no Car can be chosen.

Radio card (design `StCarCard`): min block size 48px, padding 12px 14px, radius 10px, 1.5px border `--app-border`; selected: border `--p-sky-500`, background `#F2F7FF`; current (disabled): background `--app-bg-page`, text `--app-text-muted`, "(נוכחי)" at the inline end in 12.5px muted. Content order: radio, `pi pi-car`, name (600 when selected), transmission tag (muted when disabled).

- [ ] **Step 1: Write the failing spec**

`change-car.dialog.spec.ts`:

```ts
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { Transmission } from '../../../domain/transmission.enum';
import { ChangeCarDialogData } from './change-car-dialog-data';
import { ChangeCarDialog } from './change-car.dialog';

const CARS: CarOption[] = [
    { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] },
    { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] },
    { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] },
];

const NOA: Student = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isActive: true,
};

const ROI: Student = {
    ...NOA,
    id: 'student-roi',
    name: 'Roi Almog',
    teacherId: 'teacher-oren',
    teacherName: 'Oren Levi',
    isCarOfTeacher: false,
};

const LIA: Student = {
    ...NOA,
    id: 'student-lia',
    name: 'Lia Hadad',
    teacherId: 'teacher-oren',
    teacherName: 'Oren Levi',
    carId: 'car-mazda',
    carName: 'Mazda 3 Grey',
    carTransmission: Transmission.manual,
};

const SAME_CAR: StudentRefusal = {
    kind: StudentRefusalKind.sameCar,
    message: "This is already this Student's Car. Refresh the screen.",
    existingStudentName: null,
};

interface Setup {
    fixture: ComponentFixture<ChangeCarDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    confirm: ReturnType<typeof vi.fn<(carId: string) => Promise<boolean>>>;
    refresh: ReturnType<typeof vi.fn>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(student: Student = NOA, confirmResult = true): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const confirm = vi.fn<(carId: string) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const refresh = vi.fn();
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: ChangeCarDialogData = {
        student,
        cars: signal(CARS),
        refusal,
        isSaving: signal(false),
        confirm,
        refresh,
        clearRefusal,
    };

    TestBed.configureTestingModule({
        imports: [
            ChangeCarDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: { close } },
        ],
    });

    const fixture = TestBed.createComponent(ChangeCarDialog);
    await fixture.whenStable();

    return { fixture, refusal, confirm, refresh, clearRefusal, close };
}

interface DialogInternals {
    selectedCarId: () => string | null;
    canSave: () => boolean;
    selectCar: (carId: string) => void;
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<ChangeCarDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<ChangeCarDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function cards(fixture: ComponentFixture<ChangeCarDialog>): HTMLElement[] {
    return Array.from(host(fixture).querySelectorAll<HTMLElement>('.change-car__card'));
}

describe('ChangeCarDialog', () => {
    it("lists the Teacher's Cars with the current one disabled, and waits for a choice", async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(cards(fixture).map((card) => card.textContent?.trim().split(/\s+/)[0])).toEqual(['Corolla', 'i20']);
        expect(cards(fixture)[0].classList).toContain('change-car__card--current');
        expect(cards(fixture)[1].classList).not.toContain('change-car__card--current');
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-car__flag')).toBeNull();
    });

    it('changes the Car and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        internals(fixture).selectCar('car-i20');
        await fixture.whenStable();

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith('car-i20');
        expect(close).toHaveBeenCalled();
    });

    it('enables every card for a flagged Student and says why', async () => {
        //given
        const { fixture } = await setUp(ROI);

        //then
        expect(cards(fixture).length).toBe(1);
        expect(cards(fixture)[0].classList).not.toContain('change-car__card--current');
        expect(host(fixture).querySelector('.change-car__flag')?.textContent).toContain('students.changeCar.flagTitle');
    });

    it('shows Close only when the Teacher has no other Cars', async () => {
        //given
        const { fixture } = await setUp(LIA);

        //then
        expect(host(fixture).querySelector('.change-car__none')?.textContent).toContain('students.changeCar.none');
        expect(cards(fixture).length).toBe(0);
        expect(host(fixture).querySelector('button[type="submit"]')).toBeNull();
        expect(host(fixture).querySelector('.dialog-form__actions')?.textContent).toContain('general.close');
    });

    it('shows a stale refusal with Refresh, and Refresh closes the dialog and reloads', async () => {
        //given
        const { fixture, refusal, refresh, close } = await setUp(NOA, false);
        internals(fixture).selectCar('car-i20');
        refusal.set(SAME_CAR);
        await fixture.whenStable();

        //when
        (host(fixture).querySelector('.change-car__refresh') as HTMLButtonElement).click();

        //then
        expect(host(fixture).querySelector('app-dialog-refusal')?.textContent).toContain('students.changeCar.sameCar');
        expect(refresh).toHaveBeenCalled();
        expect(close).toHaveBeenCalled();
    });

    it('forgets the refusal when another Car is chosen', async () => {
        //given
        const { fixture, refusal, clearRefusal } = await setUp(NOA, false);
        refusal.set(SAME_CAR);
        await fixture.whenStable();

        //when
        internals(fixture).selectCar('car-i20');
        await fixture.whenStable();

        //then
        expect(clearRefusal).toHaveBeenCalled();
        expect(internals(fixture).canSave()).toBe(true);
    });
});
```

- [ ] **Step 2: Run the spec to verify it fails**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/change-car/change-car.dialog.spec.ts`
Expected: FAIL, `Cannot find module './change-car-dialog-data'`.

- [ ] **Step 3: Write the dialog**

`change-car-dialog-data.ts`:

```ts
import { Signal } from '@angular/core';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';

export interface ChangeCarDialogData {
    student: Student;
    cars: Signal<CarOption[]>;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (carId: string) => Promise<boolean>;
    refresh: () => void;
    clearRefusal: () => void;
}
```

`change-car.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { RadioButtonModule } from 'primeng/radiobutton';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { carChangeFor } from '../../../domain/car-change';
import { StudentRefusalKind } from '../../../domain/student-refusal';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { ChangeCarDialogData } from './change-car-dialog-data';

@Component({
    selector: 'app-change-car-dialog',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        ButtonModule,
        MessageModule,
        RadioButtonModule,
        DialogRefusalComponent,
        StudentWhoCardComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './change-car.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './change-car.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeCarDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<ChangeCarDialogData>).data as ChangeCarDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = StudentRefusalKind;
    protected readonly carsAndTeachersLink = ['/', AppRoutes.teachers];
    protected readonly teacherName = isolateDirection(this.student.teacherName);
    protected readonly currentCarName = isolateDirection(this.student.carName);

    protected readonly carChange = computed(() => carChangeFor(this.student, this.data.cars()));

    protected readonly selectedCarId = signal<string | null>(null);

    protected readonly canSave = computed(
        () => this.selectedCarId() !== null && !this.refusal() && !this.isSaving(),
    );

    protected selectCar(carId: string): void {
        if (this.refusal()) {
            this.data.clearRefusal();
        }

        this.selectedCarId.set(carId);
    }

    protected async submit(): Promise<void> {
        const carId = this.selectedCarId();

        if (!this.canSave() || !carId) {
            return;
        }

        const saved = await this.data.confirm(carId);

        if (saved) {
            this.ref.close();
        }
    }

    protected onRefresh(): void {
        this.data.refresh();
        this.ref.close();
    }

    protected close(): void {
        this.ref.close();
    }
}
```

`change-car.dialog.html`:

```html
@let refusedNow = refusal();
@let isSameCar = refusedNow?.kind === refusalKinds.sameCar;
@let isStaleCar = refusedNow?.kind === refusalKinds.staleCar;
@let refusalText = isSameCar
  ? ('students.changeCar.sameCar' | transloco)
  : isStaleCar
    ? ('students.changeCar.carGone' | transloco: { teacher: teacherName })
    : refusedNow?.message;
@let canChoose = carChange().hasOtherCars;

<form (ngSubmit)="submit()" class="dialog-form">
  @if (refusedNow) {
    <app-dialog-refusal [message]="refusalText ?? ''">
      @if (isSameCar || isStaleCar) {
        <button type="button" class="change-car__refresh" (click)="onRefresh()">
          <i class="pi pi-refresh" aria-hidden="true"></i>
          {{ 'general.refresh' | transloco }}
        </button>
      }
    </app-dialog-refusal>
  }

  <app-student-who-card [student]="student" />

  @if (!student.isCarOfTeacher) {
    <p-message severity="error">
      <span class="change-car__message change-car__flag">
        <strong>{{ 'students.changeCar.flagTitle' | transloco: { teacher: teacherName } }}</strong>
        <span>{{ 'students.changeCar.flagBody' | transloco: { car: currentCarName, teacher: teacherName } }}</span>
      </span>
    </p-message>
  }

  @if (canChoose) {
    <div class="change-car__group" role="radiogroup" aria-labelledby="change-car-label">
      <span class="change-car__label" id="change-car-label">{{ 'students.changeCar.newCar' | transloco }}</span>
      @for (choice of carChange().choices; track choice.id) {
        <label
          class="change-car__card"
          [for]="'change-car-' + choice.id"
          [class.change-car__card--selected]="selectedCarId() === choice.id"
          [class.change-car__card--current]="choice.isCurrent">
          <p-radiobutton
            name="carId"
            [inputId]="'change-car-' + choice.id"
            [value]="choice.id"
            [ngModel]="selectedCarId()"
            (ngModelChange)="selectCar($event)"
            [disabled]="choice.isCurrent" />
          <i class="pi pi-car change-car__icon" aria-hidden="true"></i>
          <span class="change-car__name">{{ choice.name }}</span>
          <app-transmission-tag [transmission]="choice.transmission" [muted]="choice.isCurrent" />
          @if (choice.isCurrent) {
            <small class="change-car__current">{{ 'students.changeTeacher.current' | transloco }}</small>
          }
        </label>
      }
      <small class="field__hint">{{ 'students.changeCar.hint' | transloco: { teacher: teacherName } }}</small>
    </div>
  } @else {
    <p-message severity="info">
      <span class="change-car__message change-car__none">
        <strong>{{ 'students.changeCar.none' | transloco: { teacher: teacherName } }}</strong>
        <span>{{ 'students.changeCar.noneHint' | transloco }}</span>
        <a class="change-car__link" [routerLink]="carsAndTeachersLink" (click)="close()">{{ 'students.add.noCarsLink' | transloco }}</a>
      </span>
    </p-message>
  }

  <div class="dialog-form__actions">
    @if (canChoose) {
      <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="close()" />
      <p-button
        [label]="'students.changeCar.save' | transloco"
        type="submit"
        [disabled]="!canSave()"
        [loading]="isSaving()" />
    } @else {
      <p-button [label]="'general.close' | transloco" severity="secondary" type="button" [outlined]="true" (onClick)="close()" />
    }
  </div>
</form>
```

`change-car.dialog.scss`:

```scss
.change-car__group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.change-car__label {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--app-text-secondary);
}

.change-car__card {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    min-block-size: 48px;
    padding: 0.75rem 0.875rem;
    box-sizing: border-box;
    border: 1.5px solid var(--app-border);
    border-radius: 10px;
    background: var(--app-bg-card);
    cursor: pointer;
}

.change-car__card--selected {
    border-color: var(--p-sky-500);
    background: #f2f7ff;
}

.change-car__card--selected .change-car__name {
    font-weight: 600;
}

.change-car__card--current {
    background: var(--app-bg-page);
    color: var(--app-text-muted);
    cursor: default;
}

.change-car__icon {
    color: var(--app-text-secondary);
}

.change-car__card--current .change-car__icon {
    color: var(--app-text-muted);
}

.change-car__name {
    font-size: 0.9rem;
    color: var(--app-ink);
}

.change-car__card--current .change-car__name {
    color: var(--app-text-muted);
}

.change-car__current {
    margin-inline-start: auto;
    font-size: 0.78rem;
    color: var(--app-text-muted);
}

.change-car__message {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.change-car__link {
    font-weight: 600;
    color: var(--p-sky-500);
}

.change-car__refresh {
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

If `--app-bg-card` is not defined in `client\src\styles.scss`, use `#fff`, as the Students page cards do.

- [ ] **Step 4: Add the translations**

`he.json`: inside `general`, after `"cancel"`: `"close": "סגירה",`. Inside `students.actions`, after `"changeTeacher"`:

```json
      "changeCar": "החלפת רכב",
      "fix": "תיקון",
```

Inside `students.edit`, after `"teacherAndCar"`:

```json
      "teacherAndCarHint": "לשינוי: \"החלפת מורה\" או \"החלפת רכב\" בתפריט השורה.",
```

Inside `students`, after the `changeTeacher` object:

```json
    "changeCar": {
      "title": "החלפת רכב",
      "newCar": "רכב חדש",
      "hint": "מוצגים רק הרכבים של {{teacher}}.",
      "none": "ל{{teacher}} אין רכבים אחרים.",
      "noneHint": "אפשר לשייך עוד רכב למורה במסך רכבים ומורים.",
      "flagTitle": "הרכב הנוכחי אינו אחד מהרכבים של {{teacher}}",
      "flagBody": "{{car}} נקבע לפני שהכלל נכנס לתוקף. יש לבחור אחד מהרכבים של {{teacher}}.",
      "save": "החלפת רכב",
      "sameCar": "זה כבר הרכב של התלמיד הזה. יש לרענן את המסך.",
      "carGone": "הרכב הזה כבר לא משויך ל{{teacher}}. יש לרענן ולבחור שוב."
    },
```

`en.json`, same places: `"close": "Close",`;

```json
      "changeCar": "Change Car",
      "fix": "Fix",
```

```json
      "teacherAndCarHint": "To change them, use Change Teacher or Change Car in the row menu.",
```

```json
    "changeCar": {
      "title": "Change Car",
      "newCar": "New Car",
      "hint": "Only {{teacher}}'s Cars are listed.",
      "none": "{{teacher}} has no other Cars.",
      "noneHint": "You can assign another Car to this Teacher in Cars & teachers.",
      "flagTitle": "The current Car isn't one of {{teacher}}'s Cars",
      "flagBody": "{{car}} was set before the rule existed. Choose one of {{teacher}}'s Cars.",
      "save": "Change Car",
      "sameCar": "This is already this Student's Car. Refresh the screen.",
      "carGone": "This Car is no longer assigned to {{teacher}}. Refresh and choose again."
    },
```

("Cars & teachers" matches the existing `students.add.noCars` wording in `en.json`.)

- [ ] **Step 5: Run the spec to verify it passes**

Run: the step 2 command.
Expected: PASS, 6 tests.

- [ ] **Step 6: Wire the row action with the Fix badge**

In `students.page.ts`, import:

```ts
import { ChangeCarDialog } from '../../dialogs/change-car/change-car.dialog';
import { ChangeCarDialogData } from '../../dialogs/change-car/change-car-dialog-data';
```

In `activeRowActions`, insert `this.changeCarAction(student),` right after the Change Teacher item (before `{ separator: true }`), and add after `editDetailsAction`:

```ts
    private changeCarAction(student: StudentRow): MenuItem {
        const action: MenuItem = {
            label: this.transloco.translate('students.actions.changeCar'),
            icon: 'pi pi-car',
            command: () => this.onChangeCar(student),
        };

        if (student.isCarOfTeacher) {
            return action;
        }

        return {
            ...action,
            icon: 'pi pi-exclamation-circle',
            styleClass: 'students-menu__item--fix',
            badge: this.transloco.translate('students.actions.fix'),
            badgeStyleClass: 'students-menu__fix-badge',
        };
    }
```

and after `onChangeTeacher`:

```ts
    private onChangeCar(student: StudentRow): void {
        const data: ChangeCarDialogData = {
            student,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (carId) => this.store.changeCar(student, carId),
            refresh: () => this.store.reload(),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(ChangeCarDialog, this.transloco.translate('students.changeCar.title'), StudentsPage.dialogWidth, data);
    }
```

In `students.page.scss`, after the `.students-menu__item--danger` rule (design 2d: muted row, plum icon, plum outlined pill at the inline end):

```scss
::ng-deep .students-menu__item--fix .p-menu-item-content {
    background: var(--app-bg-muted);
}

::ng-deep .students-menu__item--fix .p-menu-item-icon {
    color: var(--p-red-600);
}

::ng-deep .students-menu__fix-badge.p-badge {
    margin-inline-start: auto;
    min-inline-size: 0;
    block-size: auto;
    padding: 1px 8px;
    border: 1px solid var(--p-red-200);
    border-radius: 999px;
    background: transparent;
    color: var(--p-red-600);
    font-size: 0.72rem;
    font-weight: 700;
    line-height: 1.4;
}
```

- [ ] **Step 7: Point Edit details to the new actions**

In `edit-student.dialog.html`, right after `</app-locked-field>`:

```html
  <small class="field__hint edit-student__teacher-and-car-hint">{{ 'students.edit.teacherAndCarHint' | transloco }}</small>
```

In `edit-student.dialog.spec.ts`, inside the first test (`opens filled with the Student's details`), add to `//then`:

```ts
        expect(host(fixture).querySelector('.edit-student__teacher-and-car-hint')?.textContent).toContain('students.edit.teacherAndCarHint');
```

- [ ] **Step 8: Run the client suite and build**

Run from `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS; build succeeds.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Change Car dialog with radio cards, and a Fix badge for a Car that is not the Teacher's (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
