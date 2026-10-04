import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Role } from '../../../domain/role.enum';
import { UsersStore } from '../../../state/users.store';
import { AddUserDialog, AddUserDialogData, AddUserResult } from '../../dialogs/add-user/add-user.dialog';

@Component({
    selector: 'app-users-page',
    imports: [TranslocoPipe, InitialsPipe, ButtonModule, ProgressSpinnerModule, TableModule, TagModule],
    templateUrl: './users.page.html',
    styleUrl: './users.page.scss',
    providers: [UsersStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
    private static readonly dialogWidth = '33.75rem';

    protected readonly store = inject(UsersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly roles = Role;

    protected onAddUser(): void {
        const data: AddUserDialogData = { teachers: this.store.linkableTeachers() };
        const ref = this.dialogs.open(AddUserDialog, {
            header: this.transloco.translate('users.addUser'),
            width: UsersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });

        ref?.onClose.subscribe((result?: AddUserResult) => {
            if (result) {
                void this.store.create(result);
            }
        });
    }
}
