import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { RadioButtonModule } from 'primeng/radiobutton';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { carChangeFor } from '../../../domain/car-change';
import { StudentRefusalKind } from '../../../domain/student-refusal';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { ChangeCarDialogData } from './change-car-dialog-data';

@Component({
    selector: 'app-change-car-dialog',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        ButtonModule,
        MessageModule,
        RadioButtonModule,
        DialogRefusalComponent,
        StudentWhoCardComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './change-car.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './change-car.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeCarDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<ChangeCarDialogData>).data as ChangeCarDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = StudentRefusalKind;
    protected readonly carsAndTeachersLink = ['/', AppRoutes.teachers];
    protected readonly teacherName = isolateDirection(this.student.teacherName);
    protected readonly currentCarName = isolateDirection(this.student.carName);

    protected readonly carChange = computed(() => carChangeFor(this.student, this.data.cars()));

    protected readonly selectedCarId = signal<string | null>(null);

    protected readonly canSave = computed(
        () => this.selectedCarId() !== null && !this.refusal() && !this.isSaving(),
    );

    protected selectCar(carId: string): void {
        if (this.refusal()) {
            this.data.clearRefusal();
        }

        this.selectedCarId.set(carId);
    }

    protected async submit(): Promise<void> {
        const carId = this.selectedCarId();

        if (!this.canSave() || !carId) {
            return;
        }

        const saved = await this.data.confirm(carId);

        if (saved) {
            this.ref.close();
        }
    }

    protected onRefresh(): void {
        this.data.refresh();
        this.ref.close();
    }

    protected close(): void {
        this.ref.close();
    }
}
