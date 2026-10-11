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
