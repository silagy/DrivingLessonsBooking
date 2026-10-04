import { ChangeDetectionStrategy, Component, Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { SelectButtonModule } from 'primeng/selectbutton';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { ChangeUserRoleRequest } from '../../../data/change-user-role.request';
import { otherRole, roleChangeNoteKey } from '../../../domain/role-change';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';
import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';

export interface ChangeUserRoleDialogData {
    user: User;
    refusal: Signal<string | null>;
    isSaving: Signal<boolean>;
    confirm: (request: ChangeUserRoleRequest) => Promise<boolean>;
}

@Component({
    selector: 'app-change-user-role-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, MessageModule, SelectButtonModule, UserWhoCardComponent],
    templateUrl: './change-user-role.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeUserRoleDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly transloco = inject(TranslocoService);
    private readonly data = inject(DynamicDialogConfig<ChangeUserRoleDialogData>).data as ChangeUserRoleDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly teacher = this.user.teacherName ? isolateDirection(this.user.teacherName) : '';

    protected readonly roleOptions = Object.values(Role).map((value) => ({
        value,
        label: this.roleLabel(value),
        current: value === this.user.role,
    }));

    protected readonly form = this.fb.nonNullable.group({
        role: this.fb.nonNullable.control(otherRole(this.user.role)),
    });

    private readonly role = toSignal(this.form.controls.role.valueChanges, {
        initialValue: this.form.controls.role.value,
    });

    protected readonly noteKey = computed(() => roleChangeNoteKey(this.role(), this.user.teacherName));

    protected async submit(): Promise<void> {
        const request: ChangeUserRoleRequest = this.form.getRawValue();
        const changed = await this.data.confirm(request);

        if (changed) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }

    private roleLabel(role: Role): string {
        const label = this.transloco.translate(`users.roles.${role}`);

        if (role !== this.user.role) {
            return label;
        }

        return `${label} ${this.transloco.translate('users.currentRole')}`;
    }
}
