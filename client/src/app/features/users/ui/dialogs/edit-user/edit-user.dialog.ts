import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { ChangeUserDetailsRequest } from '../../../data/change-user-details.request';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { UserDialogData } from '../user-dialog-data';

@Component({
    selector: 'app-edit-user-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, InputTextModule, DialogRefusalComponent],
    templateUrl: './edit-user.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './edit-user.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditUserDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<UserDialogData<ChangeUserDetailsRequest>>).data as UserDialogData<ChangeUserDetailsRequest>;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;

    protected readonly form = this.fb.nonNullable.group({
        name: [this.user.name, Validators.required],
        signInEmail: [this.user.signInEmail, [Validators.required, Validators.email]],
    });

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const request: ChangeUserDetailsRequest = this.form.getRawValue();
        const saved = await this.data.confirm(request);

        if (saved) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
