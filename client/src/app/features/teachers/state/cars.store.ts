import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { Car } from '../domain/car.model';
import { CarsApiService } from '../data/cars-api.service';
import { ChangeCarDetailsRequest } from '../data/change-car-details.request';
import { CreateCarRequest } from '../data/create-car.request';

@Injectable({ providedIn: 'root' })
export class CarsStore {
    private readonly api = inject(CarsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);

    private readonly carsResource = resource({
        loader: () => firstValueFrom(this.api.findCars()),
    });

    readonly cars = computed<Car[]>(() => {
        const cars = this.carsResource.value() ?? [];

        return [...cars].sort((a, b) => a.name.localeCompare(b.name));
    });

    readonly isLoading = computed(() => this.carsResource.isLoading() && !this.carsResource.value());
    readonly loadError = computed(() => (this.carsResource.error() ? 'teachers.carsLoadFailed' : null));
    readonly isEmpty = computed(() => !this.isLoading() && !this.cars().length);
    readonly isMutating = this.mutating.asReadonly();

    readonly carsByTeacherId = computed<Map<string, Car[]>>(() => {
        const map = new Map<string, Car[]>();

        for (const car of this.cars()) {
            for (const teacher of car.assignedTeachers) {
                const list = map.get(teacher.id) ?? [];
                list.push(car);
                map.set(teacher.id, list);
            }
        }

        return map;
    });

    reload(): void {
        this.carsResource.reload();
    }

    async create(request: CreateCarRequest): Promise<void> {
        await this.executeCommand(() => this.api.createCar(request), 'teachers.carCreated');
    }

    async changeDetails(carId: string, request: ChangeCarDetailsRequest): Promise<void> {
        await this.executeCommand(() => this.api.changeCarDetails(carId, request), 'teachers.carChanged');
    }

    async delete(carId: string): Promise<void> {
        await this.executeCommand(() => this.api.deleteCar(carId), 'teachers.carDeleted');
    }

    async applyAssignments(carId: string, selectedTeacherIds: string[], currentTeacherIds: string[]): Promise<void> {
        const toAssign = selectedTeacherIds.filter((id) => !currentTeacherIds.includes(id));
        const toUnassign = currentTeacherIds.filter((id) => !selectedTeacherIds.includes(id));

        if (!toAssign.length && !toUnassign.length) {
            return;
        }

        this.mutating.set(true);

        try {
            for (const teacherId of toAssign) {
                await firstValueFrom(this.api.assignTeacher(carId, teacherId));
            }

            for (const teacherId of toUnassign) {
                await firstValueFrom(this.api.unassignTeacher(carId, teacherId));
            }

            this.toast.success('teachers.assignmentsUpdated');
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.carsResource.reload();
            this.mutating.set(false);
        }
    }

    private async executeCommand(command: () => Observable<unknown>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.carsResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}
