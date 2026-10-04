import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { ChangeUserDetailsRequest } from '../data/change-user-details.request';
import { ChangeUserRoleRequest } from '../data/change-user-role.request';
import { CreateUserRequest } from '../data/create-user.request';
import { SetUserTemporaryPasswordRequest } from '../data/set-user-temporary-password.request';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { UsersApiService } from '../data/users-api.service';
import { LinkableTeacher } from '../domain/linkable-teacher.model';
import { toLinkableTeachers } from '../domain/teacher-link';
import { TeacherOption } from '../domain/teacher-option.model';
import { User } from '../domain/user.model';

const SINGLE_USER_COUNT = 1;

@Injectable()
export class UsersStore {
    private readonly api = inject(UsersApiService);
    private readonly teacherOptionsApi = inject(TeacherOptionsApiService);
    private readonly toast = inject(ToastService);
    private readonly auth = inject(AuthService);

    private readonly mutating = signal(false);
    private readonly refusalMessage = signal<string | null>(null);

    private readonly usersResource = resource({
        loader: () => firstValueFrom(this.api.findUsers()),
    });

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teacherOptionsApi.findTeachers()),
    });

    private readonly teachers = computed<TeacherOption[]>(() =>
        this.teachersResource.hasValue() ? this.teachersResource.value() : [],
    );

    readonly users = computed<User[]>(() => (this.usersResource.hasValue() ? this.usersResource.value() : []));

    readonly linkableTeachers = computed<LinkableTeacher[]>(() => toLinkableTeachers(this.teachers(), this.users()));

    readonly isLoading = computed(() => this.usersResource.isLoading() && !this.usersResource.hasValue());
    readonly loadError = computed(() => (this.usersResource.error() ? 'users.loadFailed' : null));
    readonly hasOnlyOneUser = computed(() => this.users().length === SINGLE_USER_COUNT);
    readonly isMutating = this.mutating.asReadonly();
    readonly refusal = this.refusalMessage.asReadonly();
    readonly currentUserId = computed(() => this.auth.userId());

    reload(): void {
        this.usersResource.reload();
    }

    async create(request: CreateUserRequest): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.createUser(request));
            this.toast.success('users.created');
            this.usersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }

    clearRefusal(): void {
        this.refusalMessage.set(null);
    }

    delete(userId: string): Promise<boolean> {
        return this.runDialogCommand(() => this.api.deleteUser(userId), 'users.deleted');
    }

    changeDetails(userId: string, request: ChangeUserDetailsRequest): Promise<boolean> {
        return this.runDialogCommand(() => this.api.changeUserDetails(userId, request), 'users.detailsChanged');
    }

    changeRole(userId: string, request: ChangeUserRoleRequest): Promise<boolean> {
        return this.runDialogCommand(() => this.api.changeUserRole(userId, request), 'users.roleChanged');
    }

    setTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Promise<boolean> {
        return this.runDialogCommand(
            () => this.api.setUserTemporaryPassword(userId, request),
            'users.temporaryPasswordSet',
        );
    }

    async restore(user: User): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.restoreUser(user.id));
            this.toast.success('users.restored', { key: 'users.restoredDetail', params: { name: isolateDirection(user.name) } });
            this.usersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }

    private async runDialogCommand(command: () => Observable<void>, successKey: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalMessage.set(null);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.usersResource.reload();
            return true;
        } catch (error) {
            this.refusalMessage.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }
}
