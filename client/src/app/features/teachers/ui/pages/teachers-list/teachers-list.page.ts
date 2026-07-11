import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogService } from 'primeng/dynamicdialog';
import { TeachersStore } from '../../../state/teachers.store';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { TeacherCardComponent } from '../../components/teacher-card/teacher-card.component';
import { CarFormDialog, CarFormResult } from '../../dialogs/car-form/car-form.dialog';
import { TeacherFormDialog, TeacherFormResult } from '../../dialogs/teacher-form/teacher-form.dialog';

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

        ref?.onClose.subscribe((result?: TeacherFormResult) => {
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
        const ref = this.dialogs.open(CarFormDialog, {
            header: this.transloco.translate('teachers.editCar'),
            width: TeachersListPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data: { car },
        });

        ref?.onClose.subscribe((result?: CarFormResult) => {
            if (result) {
                void this.store.changeCarDetails(teacher.id, car.id, result);
            }
        });
    }
}
