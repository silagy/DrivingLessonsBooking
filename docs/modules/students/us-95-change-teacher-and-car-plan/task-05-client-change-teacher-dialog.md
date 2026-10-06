# Task 5 of 7: Change Teacher dialog and row action (client)

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires task 4 committed. Work on branch `95-change-teacher-and-car`. Read README decisions 12, 13, 15 to 18 and design frames 6a to 6h first.

**Files:**
- Create: `client\src\app\features\students\ui\dialogs\change-teacher\change-teacher-dialog-data.ts`
- Create: `client\src\app\features\students\ui\dialogs\change-teacher\change-teacher.dialog.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`students.actions.changeTeacher`, `students.changeTeacher`)

**Interfaces:**
- Consumes: task 4 (`pickCarForNewTeacher(teacherId, cars, currentCarId): TeacherChangeCarPick`, `teacherHasCars(teacherId, cars)`, `StudentRefusalKind.sameTeacher` / `.staleCar`, `StudentsStore.changeTeacher(student, teacherId, carId)`, `StudentsStore.reload()`, `.clearRefusal()`, `.teacherOptions`, `.carOptions`, `.refusal`, `.isMutating`), task 3 (`Student.isCarOfTeacher`), `StudentWhoCardComponent` (`app-student-who-card`, input `student`), `TransmissionTagComponent` (`app-transmission-tag`, inputs `transmission`, `muted`), `DialogRefusalComponent` (`app-dialog-refusal`, input `message`, projected content), `shared\dialogs\dialog-form.scss` (`.dialog-form`, `.field`, `.field__hint`, `.field__error`, `.dialog-form__actions`), `AppRoutes.teachers`, `isolateDirection`.
- Produces (task 6 and 7 rely on these):
  - `ChangeTeacherDialogData { student: Student; teachers: Signal<TeacherOption[]>; cars: Signal<CarOption[]>; refusal: Signal<StudentRefusal | null>; isSaving: Signal<boolean>; confirm: (teacherId: string, carId: string) => Promise<boolean>; refresh: () => void; clearRefusal: () => void }`.
  - `ChangeTeacherDialog` (`app-change-teacher-dialog`).
  - Row menu of an active Student: Edit details · Change Teacher · (Change Car comes in task 6) · separator · Deactivate.

Layout (design 6a, 560px): refusal Message (when refused) · who card · "מורה חדש" Select · "רכב" Select (+ hint / info / error under it) · info line with `pi pi-info-circle` "הגשות קיימות נשארות..." in `--app-text-secondary`, 13px · footer: text "ביטול" + primary "החלפת מורה".

- [ ] **Step 1: Write the failing spec**

`change-teacher.dialog.spec.ts`:

```ts
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { Transmission } from '../../../domain/transmission.enum';
import { ChangeTeacherDialogData } from './change-teacher-dialog-data';
import { ChangeTeacherDialog } from './change-teacher.dialog';

const TEACHERS: TeacherOption[] = [
    { id: 'teacher-michal', name: 'Michal Ben-David' },
    { id: 'teacher-oren', name: 'Oren Levi' },
    { id: 'teacher-ronit', name: 'Ronit Avraham' },
    { id: 'teacher-yael', name: 'Yael Carmi' },
];

const CARS: CarOption[] = [
    { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] },
    { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] },
    { id: 'car-picanto', name: 'Picanto Red', transmission: Transmission.automatic, teacherIds: ['teacher-yael'] },
    { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] },
    { id: 'car-kia', name: 'Kia Rio Blue', transmission: Transmission.automatic, teacherIds: ['teacher-oren'] },
];

const OMER: Student = {
    id: 'student-omer',
    nationalId: '312456783',
    name: 'Omer Shalev',
    phone: '052-7654321',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
    isCarOfTeacher: true,
    isActive: true,
};

const SAME_TEACHER: StudentRefusal = {
    kind: StudentRefusalKind.sameTeacher,
    message: "This is already this Student's Teacher. Refresh the screen.",
    existingStudentName: null,
};

interface Setup {
    fixture: ComponentFixture<ChangeTeacherDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    confirm: ReturnType<typeof vi.fn<(teacherId: string, carId: string) => Promise<boolean>>>;
    refresh: ReturnType<typeof vi.fn>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult = true): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const confirm = vi.fn<(teacherId: string, carId: string) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const refresh = vi.fn();
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: ChangeTeacherDialogData = {
        student: OMER,
        teachers: signal(TEACHERS),
        cars: signal(CARS),
        refusal,
        isSaving: signal(false),
        confirm,
        refresh,
        clearRefusal,
    };

    TestBed.configureTestingModule({
        imports: [
            ChangeTeacherDialog,
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

    const fixture = TestBed.createComponent(ChangeTeacherDialog);
    await fixture.whenStable();

    return { fixture, refusal, confirm, refresh, clearRefusal, close };
}

interface TeacherChoiceView {
    id: string;
    isCurrent: boolean;
    hasCars: boolean;
}

interface DialogInternals {
    carId: () => string | null;
    teacherChoices: () => TeacherChoiceView[];
    canSave: () => boolean;
    selectTeacher: (teacherId: string) => void;
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<ChangeTeacherDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<ChangeTeacherDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

async function chooseTeacher(fixture: ComponentFixture<ChangeTeacherDialog>, teacherId: string): Promise<void> {
    internals(fixture).selectTeacher(teacherId);
    await fixture.whenStable();
}

describe('ChangeTeacherDialog', () => {
    it('shows who is changing and keeps Save disabled until a Teacher and Car are chosen', async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(host(fixture).querySelector('.who-card__name')?.textContent).toContain('Omer Shalev');
        expect(internals(fixture).carId()).toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-teacher__note')?.textContent).toContain('students.changeTeacher.note');
    });

    it('offers every Teacher, the current one disabled and those without Cars noted', async () => {
        //given
        const { fixture } = await setUp();

        //when
        const choices = internals(fixture).teacherChoices();

        //then
        expect(choices.find((choice) => choice.id === 'teacher-ronit')?.isCurrent).toBe(true);
        expect(choices.filter((choice) => choice.isCurrent).length).toBe(1);
        expect(choices.find((choice) => choice.id === 'teacher-michal')?.hasCars).toBe(false);
    });

    it('keeps the current Car when the new Teacher also teaches on it', async () => {
        //given
        const { fixture } = await setUp();

        //when
        await chooseTeacher(fixture, 'teacher-yael');

        //then
        expect(internals(fixture).carId()).toBe('car-i20');
        expect(internals(fixture).canSave()).toBe(true);
        expect(host(fixture).querySelector('.change-teacher__car-hint')?.textContent).toContain('students.changeTeacher.carKept');
    });

    it('clears the Car when the Teacher changes to one without the current Car', async () => {
        //given
        const { fixture } = await setUp();
        await chooseTeacher(fixture, 'teacher-yael');

        //when
        await chooseTeacher(fixture, 'teacher-oren');

        //then
        expect(internals(fixture).carId()).toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-teacher__car-hint')?.textContent).toContain('students.add.carScoped');
    });

    it('keeps Save disabled when the new Teacher has no Cars', async () => {
        //given
        const { fixture } = await setUp();

        //when
        await chooseTeacher(fixture, 'teacher-michal');

        //then
        expect(host(fixture).querySelector('.change-teacher__no-cars')).not.toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
    });

    it('changes the Teacher and Car and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        await chooseTeacher(fixture, 'teacher-yael');

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith('teacher-yael', 'car-i20');
        expect(close).toHaveBeenCalled();
    });

    it('shows a stale refusal with Refresh, and Refresh closes the dialog and reloads', async () => {
        //given
        const { fixture, refusal, refresh, close } = await setUp(false);
        await chooseTeacher(fixture, 'teacher-yael');
        refusal.set(SAME_TEACHER);
        await fixture.whenStable();

        //when
        (host(fixture).querySelector('.change-teacher__refresh') as HTMLButtonElement).click();

        //then
        expect(host(fixture).querySelector('app-dialog-refusal')?.textContent).toContain('students.changeTeacher.sameTeacher');
        expect(internals(fixture).canSave()).toBe(false);
        expect(refresh).toHaveBeenCalled();
        expect(close).toHaveBeenCalled();
    });

    it('forgets the refusal when another Teacher is chosen', async () => {
        //given
        const { fixture, refusal, clearRefusal } = await setUp(false);
        refusal.set(SAME_TEACHER);
        await fixture.whenStable();

        //when
        await chooseTeacher(fixture, 'teacher-oren');

        //then
        expect(clearRefusal).toHaveBeenCalled();
        expect(host(fixture).querySelector('app-dialog-refusal')).toBeNull();
    });
});
```

- [ ] **Step 2: Run the spec to verify it fails**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/dialogs/change-teacher/change-teacher.dialog.spec.ts`
Expected: FAIL, `Cannot find module './change-teacher-dialog-data'`.

- [ ] **Step 3: Write the dialog**

`change-teacher-dialog-data.ts`:

```ts
import { Signal } from '@angular/core';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';

export interface ChangeTeacherDialogData {
    student: Student;
    teachers: Signal<TeacherOption[]>;
    cars: Signal<CarOption[]>;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (teacherId: string, carId: string) => Promise<boolean>;
    refresh: () => void;
    clearRefusal: () => void;
}
```

`change-teacher.dialog.ts`:

```ts
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { pickCarForNewTeacher, teacherHasCars } from '../../../domain/car-pick';
import { StudentRefusalKind } from '../../../domain/student-refusal';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { ChangeTeacherDialogData } from './change-teacher-dialog-data';

interface TeacherChoice {
    id: string;
    name: string;
    isCurrent: boolean;
    hasCars: boolean;
}

@Component({
    selector: 'app-change-teacher-dialog',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        ButtonModule,
        MessageModule,
        SelectModule,
        DialogRefusalComponent,
        StudentWhoCardComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './change-teacher.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './change-teacher.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeTeacherDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<ChangeTeacherDialogData>).data as ChangeTeacherDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = StudentRefusalKind;
    protected readonly carsAndTeachersLink = ['/', AppRoutes.teachers];

    protected readonly teacherId = signal<string | null>(null);

    protected readonly teacherChoices = computed<TeacherChoice[]>(() => {
        const cars = this.data.cars();

        return this.data.teachers().map((teacher) => ({
            ...teacher,
            isCurrent: teacher.id === this.student.teacherId,
            hasCars: teacherHasCars(teacher.id, cars),
        }));
    });

    protected readonly carPick = computed(() =>
        pickCarForNewTeacher(this.teacherId(), this.data.cars(), this.student.carId),
    );

    protected readonly carId = linkedSignal(() => this.carPick().selectedCarId);

    protected readonly teacherName = computed(() =>
        isolateDirection(this.data.teachers().find((teacher) => teacher.id === this.teacherId())?.name ?? ''),
    );

    protected readonly carHintKey = computed(() => {
        const pick = this.carPick();

        if (pick.keepsCurrentCar) {
            return 'students.changeTeacher.carKept';
        }

        return pick.isOnlyCar ? 'students.add.carOnly' : 'students.add.carScoped';
    });

    protected readonly canSave = computed(
        () => this.teacherId() !== null && this.carId() !== null && !this.refusal() && !this.isSaving(),
    );

    protected selectTeacher(teacherId: string): void {
        this.forgetRefusal();
        this.teacherId.set(teacherId);
    }

    protected selectCar(carId: string): void {
        this.forgetRefusal();
        this.carId.set(carId);
    }

    protected async submit(): Promise<void> {
        const teacherId = this.teacherId();
        const carId = this.carId();

        if (!this.canSave() || !teacherId || !carId) {
            return;
        }

        const saved = await this.data.confirm(teacherId, carId);

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

    private forgetRefusal(): void {
        if (this.refusal()) {
            this.data.clearRefusal();
        }
    }
}
```

`change-teacher.dialog.html`:

```html
@let refusedNow = refusal();
@let isSameTeacher = refusedNow?.kind === refusalKinds.sameTeacher;
@let isStaleCar = refusedNow?.kind === refusalKinds.staleCar;
@let refusalText = isSameTeacher
  ? ('students.changeTeacher.sameTeacher' | transloco: { teacher: teacherName() })
  : isStaleCar
    ? ('students.changeTeacher.carNotOfTeacher' | transloco: { teacher: teacherName() })
    : refusedNow?.message;

<form (ngSubmit)="submit()" class="dialog-form">
  @if (refusedNow) {
    <app-dialog-refusal [message]="refusalText ?? ''">
      @if (isSameTeacher || isStaleCar) {
        <button type="button" class="change-teacher__refresh" (click)="onRefresh()">
          <i class="pi pi-refresh" aria-hidden="true"></i>
          {{ 'general.refresh' | transloco }}
        </button>
      }
    </app-dialog-refusal>
  }

  <app-student-who-card [student]="student" />

  <div class="field">
    <label for="change-teacher-teacher">{{ 'students.changeTeacher.newTeacher' | transloco }}</label>
    <p-select
      inputId="change-teacher-teacher"
      name="teacherId"
      [ngModel]="teacherId()"
      (ngModelChange)="selectTeacher($event)"
      [options]="teacherChoices()"
      optionLabel="name"
      optionValue="id"
      optionDisabled="isCurrent"
      [class.ng-invalid]="isSameTeacher"
      [class.ng-dirty]="isSameTeacher"
      [placeholder]="'students.add.teacherPlaceholder' | transloco"
      appendTo="body"
      fluid>
      <ng-template pTemplate="item" let-teacher>
        <span class="change-teacher__option">
          <span>{{ teacher.name }}</span>
          @if (teacher.isCurrent) {
            <small class="change-teacher__option-note">{{ 'students.changeTeacher.current' | transloco }}</small>
          } @else if (!teacher.hasCars) {
            <small class="change-teacher__option-note">{{ 'students.add.noCarsOption' | transloco }}</small>
          }
        </span>
      </ng-template>
    </p-select>
    @if (isSameTeacher) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ refusalText }}</small>
    }
  </div>

  <div class="field">
    <label for="change-teacher-car">{{ 'students.add.car' | transloco }}</label>
    <p-select
      inputId="change-teacher-car"
      name="carId"
      [ngModel]="carId()"
      (ngModelChange)="selectCar($event)"
      [disabled]="!carPick().options.length"
      [class.ng-invalid]="isStaleCar"
      [class.ng-dirty]="isStaleCar"
      [options]="carPick().options"
      optionLabel="name"
      optionValue="id"
      [placeholder]="(carPick().isLocked ? 'students.add.carPlaceholderLocked' : 'students.add.carPlaceholder') | transloco"
      appendTo="body"
      fluid>
      <ng-template pTemplate="selectedItem" let-car>
        <span class="change-teacher__option">
          <span>{{ car.name }}</span>
          <app-transmission-tag [transmission]="car.transmission" />
          @if (car.id === student.carId) {
            <small class="change-teacher__current-car">{{ 'students.changeTeacher.currentCar' | transloco }}</small>
          }
        </span>
      </ng-template>
      <ng-template pTemplate="item" let-car>
        <span class="change-teacher__option">
          <span>{{ car.name }}</span>
          <app-transmission-tag [transmission]="car.transmission" />
          @if (car.id === student.carId) {
            <small class="change-teacher__option-note">{{ 'students.changeTeacher.currentCar' | transloco }}</small>
          }
        </span>
      </ng-template>
    </p-select>
    @if (carPick().hasNoCars) {
      <p-message severity="info" styleClass="change-teacher__no-cars">
        <span class="change-teacher__no-cars-text">
          <span>{{ 'students.add.noCars' | transloco }}</span>
          <a class="change-teacher__link" [routerLink]="carsAndTeachersLink" (click)="close()">{{ 'students.add.noCarsLink' | transloco }}</a>
        </span>
      </p-message>
    } @else if (isStaleCar) {
      <small class="field__error"><i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ refusalText }}</small>
    } @else if (!carPick().isLocked) {
      <small class="field__hint change-teacher__car-hint">{{ carHintKey() | transloco: { teacher: teacherName() } }}</small>
    }
  </div>

  <p class="change-teacher__note">
    <i class="pi pi-info-circle" aria-hidden="true"></i>
    <span>{{ 'students.changeTeacher.note' | transloco }}</span>
  </p>

  <div class="dialog-form__actions">
    <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="close()" />
    <p-button
      [label]="'students.changeTeacher.save' | transloco"
      type="submit"
      [disabled]="!canSave()"
      [loading]="isSaving()" />
  </div>
</form>
```

The `p-message` with `styleClass="change-teacher__no-cars"` must render an element with that class for the spec (`.change-teacher__no-cars`); if PrimeNG 21 puts `styleClass` on an inner element only, the query still finds it. If it doesn't render the class at all, wrap the message in `<div class="change-teacher__no-cars">` instead.

`change-teacher.dialog.scss`:

```scss
.change-teacher__option {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    inline-size: 100%;
}

.change-teacher__option-note {
    margin-inline-start: auto;
    font-size: 0.78rem;
    color: var(--app-text-muted);
}

.change-teacher__current-car {
    font-size: 0.8rem;
    color: var(--app-text-secondary);
}

.change-teacher__no-cars-text {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.change-teacher__link {
    font-weight: 600;
    color: var(--p-sky-500);
}

.change-teacher__refresh {
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

.change-teacher__note {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    margin: 0;
    font-size: 0.8125rem;
    line-height: 1.5;
    color: var(--app-text-secondary);
}

.change-teacher__note .pi {
    margin-block-start: 0.2rem;
}
```

- [ ] **Step 4: Add the translations**

`he.json`, inside `students.actions`, after `"editDetails"`:

```json
      "changeTeacher": "החלפת מורה",
```

and inside `students`, after the `edit` object:

```json
    "changeTeacher": {
      "title": "החלפת מורה",
      "newTeacher": "מורה חדש",
      "current": "(נוכחי)",
      "currentCar": "(הרכב הנוכחי)",
      "carKept": "גם {{teacher}} מלמד/ת על הרכב הנוכחי, ולכן הוא נבחר.",
      "note": "הגשות קיימות נשארות בשבוע שבו נעשו. מההזדהות הבאה, התלמיד יראה את השבוע של המורה החדש.",
      "save": "החלפת מורה",
      "sameTeacher": "{{teacher}} כבר המורה של התלמיד הזה. יש לרענן את המסך.",
      "carNotOfTeacher": "הרכב הזה לא משויך ל{{teacher}}. יש לרענן ולבחור שוב."
    },
```

`en.json`, same places:

```json
      "changeTeacher": "Change Teacher",
```

```json
    "changeTeacher": {
      "title": "Change Teacher",
      "newTeacher": "New Teacher",
      "current": "(current)",
      "currentCar": "(current Car)",
      "carKept": "{{teacher}} also teaches on the current Car, so it's selected.",
      "note": "Existing Submissions stay with the week they were made in. From their next identification, the Student sees the new Teacher's week.",
      "save": "Change Teacher",
      "sameTeacher": "{{teacher}} is already this Student's Teacher. Refresh the screen.",
      "carNotOfTeacher": "This Car isn't assigned to {{teacher}}. Refresh and choose again."
    },
```

(The copy deck's button labels are capitalised for the mock only; the app's buttons use sentence case like `students.addStudent`.)

- [ ] **Step 5: Run the spec to verify it passes**

Run: the step 2 command.
Expected: PASS, 8 tests.

- [ ] **Step 6: Wire the row action**

In `students.page.ts`, import:

```ts
import { ChangeTeacherDialog } from '../../dialogs/change-teacher/change-teacher.dialog';
import { ChangeTeacherDialogData } from '../../dialogs/change-teacher/change-teacher-dialog-data';
```

Change `activeRowActions` to:

```ts
    private activeRowActions(student: StudentRow): MenuItem[] {
        return [
            this.editDetailsAction(student),
            {
                label: this.transloco.translate('students.actions.changeTeacher'),
                icon: 'pi pi-arrow-right-arrow-left',
                command: () => this.onChangeTeacher(student),
            },
            { separator: true },
            {
                label: this.transloco.translate('students.actions.deactivate'),
                icon: 'pi pi-pause-circle',
                styleClass: 'students-menu__item--danger',
                command: () => this.onDeactivate(student),
            },
        ];
    }
```

and add after `onEditDetails`:

```ts
    private onChangeTeacher(student: StudentRow): void {
        const data: ChangeTeacherDialogData = {
            student,
            teachers: this.store.teacherOptions,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (teacherId, carId) => this.store.changeTeacher(student, teacherId, carId),
            refresh: () => this.store.reload(),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(
            ChangeTeacherDialog,
            this.transloco.translate('students.changeTeacher.title'),
            StudentsPage.dialogWidth,
            data,
        );
    }
```

- [ ] **Step 7: Run the client suite and build**

Run from `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS; build succeeds.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Change Teacher dialog keeps the current Car when the new Teacher shares it (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
