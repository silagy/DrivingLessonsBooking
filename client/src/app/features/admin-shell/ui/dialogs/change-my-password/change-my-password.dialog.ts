import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { PasswordModule } from 'primeng/password';
import { ChangeMyPasswordRequest } from '../../../data/change-my-password.request';
import { passwordsMatch } from '../../../domain/passwords-match';
import { MyPasswordStore } from '../../../state/my-password.store';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';

const PASSWORDS_MISMATCH = 'passwordsMismatch';

function matchingPasswords(group: AbstractControl): ValidationErrors | null {
    const newPassword = group.get('newPassword')?.value as string;
    const confirmation = group.get('confirmation')?.value as string;

    return passwordsMatch(newPassword, confirmation) ? null : { [PASSWORDS_MISMATCH]: true };
}

@Component({
    selector: 'app-change-my-password-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, PasswordModule, DialogRefusalComponent],
    templateUrl: './change-my-password.dialog.html',
    styleUrl: '../../../../../shared/dialogs/dialog-form.scss',
    providers: [MyPasswordStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeMyPasswordDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly store = inject(MyPasswordStore);

    protected readonly refusal = this.store.refusal;
    protected readonly isSaving = this.store.isSaving;
    protected readonly passwordsMismatch = PASSWORDS_MISMATCH;

    protected readonly form = this.fb.nonNullable.group(
        {
            currentPassword: ['', Validators.required],
            newPassword: ['', Validators.required],
            confirmation: ['', Validators.required],
        },
        { validators: matchingPasswords },
    );

    protected async submit(): Promise<void> {
        if (this.form.invalid) {
            return;
        }

        const { currentPassword, newPassword } = this.form.getRawValue();
        const request: ChangeMyPasswordRequest = { currentPassword, newPassword };
        const changed = await this.store.change(request);

        if (changed) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
