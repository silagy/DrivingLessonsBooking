import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { User } from '../../../domain/user.model';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { UserWhoCardComponent } from '../../components/user-who-card/user-who-card.component';

export interface DeleteUserDialogData {
    user: User;
    refusal: Signal<string | null>;
    isDeleting: Signal<boolean>;
    confirm: () => Promise<boolean>;
}

@Component({
    selector: 'app-delete-user-dialog',
    imports: [TranslocoPipe, ButtonModule, DialogRefusalComponent, UserWhoCardComponent],
    templateUrl: './delete-user.dialog.html',
    styleUrl: './delete-user.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteUserDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<DeleteUserDialogData>).data as DeleteUserDialogData;

    protected readonly user = this.data.user;
    protected readonly refusal = this.data.refusal;
    protected readonly isDeleting = this.data.isDeleting;

    protected async confirm(): Promise<void> {
        const deleted = await this.data.confirm();

        if (deleted) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
