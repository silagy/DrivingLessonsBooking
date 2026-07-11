import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { Teacher } from '../domain/teacher.model';
import { AddCarRequest } from '../data/add-car.request';
import { ChangeCarDetailsRequest } from '../data/change-car-details.request';
import { ChangeTeacherDetailsRequest } from '../data/change-teacher-details.request';
import { CreateTeacherRequest } from '../data/create-teacher.request';
import { TeachersApiService } from '../data/teachers-api.service';

@Injectable({ providedIn: 'root' })
export class TeachersStore {
    private readonly api = inject(TeachersApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.api.findTeachers()),
    });

    readonly teachers = computed<Teacher[]>(() => {
        const teachers = this.teachersResource.value() ?? [];

        return teachers.map((teacher) => ({
            ...teacher,
            cars: [...teacher.cars].sort((a, b) => a.name.localeCompare(b.name)),
        }));
    });

    readonly isLoading = computed(() => this.teachersResource.isLoading() && !this.teachersResource.value());
    readonly loadError = computed(() => (this.teachersResource.error() ? 'teachers.loadFailed' : null));
    readonly isEmpty = computed(() => !this.isLoading() && !this.teachers().length);
    readonly isMutating = this.mutating.asReadonly();

    async create(request: CreateTeacherRequest): Promise<void> {
        await this.executeCommand(() => this.api.createTeacher(request), 'teachers.created');
    }

    async addCar(teacherId: string, request: AddCarRequest): Promise<void> {
        await this.executeCommand(() => this.api.addCar(teacherId, request), 'teachers.carAdded');
    }

    async changeDetails(teacherId: string, request: ChangeTeacherDetailsRequest): Promise<void> {
        await this.executeCommand(() => this.api.changeTeacherDetails(teacherId, request), 'teachers.detailsChanged');
    }

    async changeCarDetails(teacherId: string, carId: string, request: ChangeCarDetailsRequest): Promise<void> {
        await this.executeCommand(
            () => this.api.changeCarDetails(teacherId, carId, request),
            'teachers.carChanged',
        );
    }

    private async executeCommand(command: () => Observable<unknown>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.teachersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}
