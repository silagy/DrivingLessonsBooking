import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { Menu, MenuModule } from 'primeng/menu';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';
import { UsersStore } from '../../../state/users.store';
import { AddUserDialog, AddUserDialogData, AddUserResult } from '../../dialogs/add-user/add-user.dialog';
import { DeleteUserDialog, DeleteUserDialogData } from '../../dialogs/delete-user/delete-user.dialog';

@Component({
    selector: 'app-users-page',
    imports: [TranslocoPipe, InitialsPipe, ButtonModule, MenuModule, ProgressSpinnerModule, TableModule, TagModule],
    templateUrl: './users.page.html',
    styleUrl: './users.page.scss',
    providers: [UsersStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
    private static readonly dialogWidth = '33.75rem';
    private static readonly deleteDialogWidth = '31.25rem';

    protected readonly store = inject(UsersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly roles = Role;
    protected readonly rowActions = signal<MenuItem[]>([]);

    private readonly rowMenu = viewChild.required<Menu>('rowMenu');

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

    protected onRowActions(event: Event, user: User): void {
        this.rowActions.set([
            {
                label: this.transloco.translate('users.delete'),
                icon: 'pi pi-trash',
                styleClass: 'users-menu__item--danger',
                command: () => this.onDeleteUser(user),
            },
        ]);
        this.rowMenu().toggle(event);
    }

    protected onRestoreUser(user: User): void {
        void this.store.restore(user);
    }

    private onDeleteUser(user: User): void {
        this.store.clearRefusal();

        const data: DeleteUserDialogData = {
            user,
            refusal: this.store.refusal,
            isDeleting: this.store.isMutating,
            confirm: () => this.store.delete(user.id),
        };

        this.dialogs.open(DeleteUserDialog, {
            header: this.transloco.translate('users.deleteTitle', { name: isolateDirection(user.name) }),
            width: UsersPage.deleteDialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
