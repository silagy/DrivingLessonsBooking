import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Teacher } from '../../../domain/teacher.model';

export interface TeacherFormResult {
    name: string;
    contactEmail: string;
}

@Component({
    selector: 'app-teacher-form-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule],
    templateUrl: './teacher-form.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherFormDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<{ teacher?: Teacher }>);

    protected readonly isEdit = !!this.config.data?.teacher;

    protected readonly form = this.fb.nonNullable.group({
        name: [this.config.data?.teacher?.name ?? '', Validators.required],
        contactEmail: [this.config.data?.teacher?.contactEmail ?? '', [Validators.required, Validators.email]],
    });

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: TeacherFormResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
