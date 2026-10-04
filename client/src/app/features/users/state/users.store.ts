import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { CreateUserRequest } from '../data/create-user.request';
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
    private readonly refusal = signal<string | null>(null);

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
    readonly deleteRefusal = this.refusal.asReadonly();
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

    clearDeleteRefusal(): void {
        this.refusal.set(null);
    }

    async delete(userId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusal.set(null);

        try {
            await firstValueFrom(this.api.deleteUser(userId));
            this.toast.success('users.deleted');
            this.usersResource.reload();
            return true;
        } catch (error) {
            this.refusal.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
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
}
