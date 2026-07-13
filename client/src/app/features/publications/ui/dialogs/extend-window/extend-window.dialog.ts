import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { jerusalemWallTimeToUtcIso } from '../../../domain/jerusalem-time';

export interface WindowEndResult {
    newEndUtc: string;
}

@Component({
    selector: 'app-extend-window-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, DatePickerModule],
    templateUrl: './extend-window.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtendWindowDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);

    protected readonly form = this.fb.nonNullable.group({
        end: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    });

    protected submit(): void {
        const { end } = this.form.getRawValue();

        if (this.form.invalid || !end) {
            return;
        }

        const result: WindowEndResult = { newEndUtc: jerusalemWallTimeToUtcIso(end) };
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
