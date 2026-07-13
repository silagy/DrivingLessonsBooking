import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Car } from '../../../domain/car.model';
import { Transmission } from '../../../domain/transmission.enum';

export interface CarFormResult {
    name: string;
    type: string;
    transmission: Transmission;
}

@Component({
    selector: 'app-car-form-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule, SelectModule],
    templateUrl: './car-form.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarFormDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<{ car?: Car }>);
    private readonly transloco = inject(TranslocoService);

    protected readonly transmissionOptions = Object.values(Transmission).map((value) => ({
        value,
        label: this.transloco.translate(`teachers.transmissions.${value}`),
    }));

    protected readonly form = this.fb.nonNullable.group({
        name: [this.config.data?.car?.name ?? '', Validators.required],
        type: [this.config.data?.car?.type ?? '', Validators.required],
        transmission: [this.config.data?.car?.transmission ?? Transmission.automatic, Validators.required],
    });

    protected submit(): void {
        if (this.form.invalid) {
            return;
        }

        const result: CarFormResult = this.form.getRawValue();
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
