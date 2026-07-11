import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogService } from 'primeng/dynamicdialog';
import { ConfirmDialog, ConfirmDialogData } from '../../../../../shared/dialogs/confirm/confirm.dialog';
import { TeachersStore } from '../../../state/teachers.store';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { TeacherCardComponent } from '../../components/teacher-card/teacher-card.component';
import { CarFormDialog, CarFormOutcome, CarFormResult } from '../../dialogs/car-form/car-form.dialog';
import { TeacherFormDialog, TeacherFormOutcome, TeacherFormResult } from '../../dialogs/teacher-form/teacher-form.dialog';

@Component({
    selector: 'app-teachers-list-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, TeacherCardComponent],
    templateUrl: './teachers-list.page.html',
    styleUrl: './teachers-list.page.scss',
    providers: [DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeachersListPage {
    private static readonly dialogWidth = '28rem';

    protected readonly store = inject(TeachersStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected onAddTeacher(): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.addTeacher'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: TeacherFormResult) => {
            if (result) {
                void this.store.create(result);
            }
        });
    }

    protected onEditTeacher(teacher: Teacher): void {
        const ref = this.dialogs.open(TeacherFormDialog, {
            header: this.transloco.translate('teachers.editTeacher'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { teacher },
        });

        ref?.onClose.subscribe((result?: TeacherFormOutcome) => {
            if (result === 'delete') {
                this.confirmDeleteTeacher(teacher);
                return;
            }

            if (result) {
                void this.store.changeDetails(teacher.id, result);
            }
        });
    }

    protected onAddCar(teacher: Teacher): void {
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.addCar'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.store.addCar(teacher.id, result);
            }
        });
    }

    protected onEditCar(teacher: Teacher, car: Car): void {
        const isLastCar = teacher.cars.length === 1;
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.editCar'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { car, isLastCar },
        });

        ref?.onClose.subscribe((result?: CarFormOutcome) => {
            if (result === 'delete') {
                this.confirmRemoveCar(teacher, car);
                return;
            }

            if (result) {
                void this.store.changeCarDetails(teacher.id, car.id, result);
            }
        });
    }

    private confirmDeleteTeacher(teacher: Teacher): void {
        const data: ConfirmDialogData = {
            messageKey: 'teachers.confirmDeleteTeacher',
            messageParams: { name: teacher.name },
        };

        this.openConfirm('teachers.deleteTeacher', data, () => void this.store.delete(teacher.id));
    }

    private confirmRemoveCar(teacher: Teacher, car: Car): void {
        const data: ConfirmDialogData = {
            messageKey: 'teachers.confirmRemoveCar',
            messageParams: { name: car.name },
        };

        this.openConfirm('teachers.removeCar', data, () => void this.store.removeCar(teacher.id, car.id));
    }

    private openConfirm(headerKey: string, data: ConfirmDialogData, onConfirm: () => void): void {
        const ref = this.dialogs.open(ConfirmDialog, {
            header: this.transloco.translate(headerKey),
            width: TeachersListPage.dialogWidth,
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
