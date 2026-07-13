import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogService } from 'primeng/dynamicdialog';
import { ConfirmDialog, ConfirmDialogData } from '../../../../../shared/dialogs/confirm/confirm.dialog';
import { CarsStore } from '../../../state/cars.store';
import { TeachersStore } from '../../../state/teachers.store';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { CarCardComponent } from '../../components/car-card/car-card.component';
import { TeacherCardComponent } from '../../components/teacher-card/teacher-card.component';
import { CarFormDialog, CarFormResult } from '../../dialogs/car-form/car-form.dialog';
import { TeacherFormDialog, TeacherFormResult } from '../../dialogs/teacher-form/teacher-form.dialog';

@Component({
    selector: 'app-cars-and-teachers-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, CarCardComponent, TeacherCardComponent],
    templateUrl: './cars-and-teachers.page.html',
    styleUrl: './cars-and-teachers.page.scss',
    providers: [DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarsAndTeachersPage {
    private static readonly dialogWidth = '28rem';

    protected readonly teachersStore = inject(TeachersStore);
    protected readonly carsStore = inject(CarsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected carsOf(teacher: Teacher): Car[] {
        return this.carsStore.carsByTeacherId().get(teacher.id) ?? [];
    }

    protected onAddTeacher(): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.addTeacher'),
            width: CarsAndTeachersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: TeacherFormResult) => {
            if (result) {
                void this.teachersStore.create(result);
            }
        });
    }

    protected onEditTeacher(teacher: Teacher): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.editTeacher'),
            width: CarsAndTeachersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { teacher },
        });

        ref?.onClose.subscribe((result?: TeacherFormResult) => {
            if (result) {
                void this.teachersStore.changeDetails(teacher.id, result);
            }
        });
    }

    protected onDeleteTeacher(teacher: Teacher): void {
        const data: ConfirmDialogData = {
            messageKey: 'teachers.confirmDeleteTeacher',
            messageParams: { name: teacher.name },
        };

        this.openConfirm('teachers.deleteTeacher', data, async () => {
            await this.teachersStore.delete(teacher.id);
            this.carsStore.reload();
        });
    }

    protected onAddCar(): void {
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.addCar'),
            width: CarsAndTeachersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.carsStore.create(result);
            }
        });
    }

    protected onEditCar(car: Car): void {
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.editCar'),
            width: CarsAndTeachersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { car },
        });

        ref?.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.carsStore.changeDetails(car.id, result);
            }
        });
    }

    protected onDeleteCar(car: Car): void {
        const data: ConfirmDialogData = {
            messageKey: 'teachers.confirmDeleteCar',
            messageParams: { name: car.name },
        };

        this.openConfirm('teachers.deleteCar', data, () => void this.carsStore.delete(car.id));
    }

    protected onApplyAssignments(car: Car, selectedTeacherIds: string[]): void {
        const currentTeacherIds = car.assignedTeachers.map((teacher) => teacher.id);
        void this.carsStore.applyAssignments(car.id, selectedTeacherIds, currentTeacherIds);
    }

    private openConfirm(headerKey: string, data: ConfirmDialogData, onConfirm: () => void): void {
        const ref = this.dialogs.open(ConfirmDialog, {
            header: this.transloco.translate(headerKey),
            width: CarsAndTeachersPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });

        ref?.onClose.subscribe((confirmed?: boolean) => {
            if (confirmed) {
                onConfirm();
            }
        });
    }
}
