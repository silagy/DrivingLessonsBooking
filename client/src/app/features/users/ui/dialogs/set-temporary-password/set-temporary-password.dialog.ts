import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { SetUserTemporaryPasswordRequest } from '../../../data/set-user-temporary-password.request';
import { DialogRefusalComponent } from '../../components/dialog-refusal/dialog-refusal.component';
import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';
import { UserDialogData } from '../user-dialog-data';

export interface SetTemporaryPasswordDialogData extends UserDialogData<SetUserTemporaryPasswordRequest> {
    isSelf: boolean;
}

@Component({
    selector: 'app-set-temporary-password-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, MessageModule, PasswordModule, DialogRefusalComponent, UserWhoCardComponent],
    templateUrl: './set-temporary-password.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetTemporaryPasswordDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<SetTemporaryPasswordDialogData>).data as SetTemporaryPasswordDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly isSelf = this.data.isSelf;

    protected readonly form = this.fb.nonNullable.group({
        temporaryPassword: ['', Validators.required],
    });

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const request: SetUserTemporaryPasswordRequest = this.form.getRawValue();
        const set = await this.data.confirm(request);

        if (set) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
