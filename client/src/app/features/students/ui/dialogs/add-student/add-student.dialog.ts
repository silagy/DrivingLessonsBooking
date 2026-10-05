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
