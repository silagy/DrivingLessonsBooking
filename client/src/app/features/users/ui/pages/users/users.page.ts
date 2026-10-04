import { ChangeDetectionStrategy, Component, Type, inject, signal, viewChild } from '@angular/core';
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
import { ChangeUserDetailsRequest } from '../../../data/change-user-details.request';
import { ChangeUserRoleRequest } from '../../../data/change-user-role.request';
import { SetUserTemporaryPasswordRequest } from '../../../data/set-user-temporary-password.request';
import { Role } from '../../../domain/role.enum';
import { User } from '../../../domain/user.model';
import { UsersStore } from '../../../state/users.store';
import { AddUserDialog, AddUserDialogData, AddUserResult } from '../../dialogs/add-user/add-user.dialog';
import { ChangeUserRoleDialog } from '../../dialogs/change-user-role/change-user-role.dialog';
import { DeleteUserDialog, DeleteUserDialogData } from '../../dialogs/delete-user/delete-user.dialog';
import { EditUserDialog } from '../../dialogs/edit-user/edit-user.dialog';
import { SetTemporaryPasswordDialog } from '../../dialogs/set-temporary-password/set-temporary-password.dialog';
import { UserDialogData } from '../../dialogs/user-dialog-data';

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
    private static readonly userDialogWidth = '32.5rem';
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
                label: this.transloco.translate('users.editDetails'),
                icon: 'pi pi-pencil',
                command: () => this.onEditUser(user),
            },
            {
                label: this.transloco.translate('users.changeRole'),
                icon: 'pi pi-shield',
                command: () => this.onChangeRole(user),
            },
            {
                label: this.transloco.translate('users.setTemporaryPassword'),
                icon: 'pi pi-key',
                command: () => this.onSetTemporaryPassword(user),
            },
            { separator: true },
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

    private onEditUser(user: User): void {
        const data: UserDialogData<ChangeUserDetailsRequest> = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.changeDetails(user.id, request),
        };

        this.openUserDialog(
            EditUserDialog,
            this.transloco.translate('users.editDetails'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onChangeRole(user: User): void {
        const data: UserDialogData<ChangeUserRoleRequest> = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.changeRole(user.id, request),
        };

        this.openUserDialog(
            ChangeUserRoleDialog,
            this.transloco.translate('users.changeRole'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onSetTemporaryPassword(user: User): void {
        const data: UserDialogData<SetUserTemporaryPasswordRequest> = {
            user,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (request) => this.store.setTemporaryPassword(user.id, request),
        };

        this.openUserDialog(
            SetTemporaryPasswordDialog,
            this.transloco.translate('users.setTemporaryPassword'),
            UsersPage.userDialogWidth,
            data,
        );
    }

    private onDeleteUser(user: User): void {
        const data: DeleteUserDialogData = {
            user,
            refusal: this.store.refusal,
            isDeleting: this.store.isMutating,
            confirm: () => this.store.delete(user.id),
        };

        this.openUserDialog(
            DeleteUserDialog,
            this.transloco.translate('users.deleteTitle', { name: isolateDirection(user.name) }),
            UsersPage.deleteDialogWidth,
            data,
        );
    }

    private openUserDialog<TData>(component: Type<unknown>, header: string, width: string, data: TData): void {
        this.store.clearRefusal();

        this.dialogs.open(component, {
            header,
            width,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
