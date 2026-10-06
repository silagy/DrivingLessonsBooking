import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { pickCarForNewTeacher, teacherHasCars } from '../../../domain/car-pick';
import { StudentRefusalKind } from '../../../domain/student-refusal';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { ChangeTeacherDialogData } from './change-teacher-dialog-data';

interface TeacherChoice {
    id: string;
    name: string;
    isCurrent: boolean;
    hasCars: boolean;
}

@Component({
    selector: 'app-change-teacher-dialog',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        ButtonModule,
        MessageModule,
        SelectModule,
        DialogRefusalComponent,
        StudentWhoCardComponent,
        TransmissionTagComponent,
    ],
    templateUrl: './change-teacher.dialog.html',
    styleUrls: ['../../../../../shared/dialogs/dialog-form.scss', './change-teacher.dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeTeacherDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<ChangeTeacherDialogData>).data as ChangeTeacherDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isSaving = this.data.isSaving;
    protected readonly refusalKinds = StudentRefusalKind;
    protected readonly carsAndTeachersLink = ['/', AppRoutes.teachers];

    protected readonly teacherId = signal<string | null>(null);

    protected readonly teacherChoices = computed<TeacherChoice[]>(() => {
        const cars = this.data.cars();

        return this.data.teachers().map((teacher) => ({
            ...teacher,
            isCurrent: teacher.id === this.student.teacherId,
            hasCars: teacherHasCars(teacher.id, cars),
        }));
    });

    protected readonly carPick = computed(() =>
        pickCarForNewTeacher(this.teacherId(), this.data.cars(), this.student.carId),
    );

    protected readonly carId = linkedSignal(() => this.carPick().selectedCarId);

    protected readonly teacherName = computed(() =>
        isolateDirection(this.data.teachers().find((teacher) => teacher.id === this.teacherId())?.name ?? ''),
    );

    protected readonly carHintKey = computed(() => {
        const pick = this.carPick();

        if (pick.keepsCurrentCar) {
            return 'students.changeTeacher.carKept';
        }

        return pick.isOnlyCar ? 'students.add.carOnly' : 'students.add.carScoped';
    });

    protected readonly canSave = computed(() => {
        const kind = this.refusal()?.kind;
        const isStale = kind === StudentRefusalKind.sameTeacher || kind === StudentRefusalKind.staleCar;

        return this.teacherId() !== null && this.carId() !== null && !isStale && !this.isSaving();
    });

    protected selectTeacher(teacherId: string): void {
        this.forgetRefusal();
        this.teacherId.set(teacherId);
    }

    protected selectCar(carId: string): void {
        this.forgetRefusal();
        this.carId.set(carId);
    }

    protected async submit(): Promise<void> {
        const teacherId = this.teacherId();
        const carId = this.carId();

        if (!this.canSave() || !teacherId || !carId) {
            return;
        }

        const saved = await this.data.confirm(teacherId, carId);

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

    private forgetRefusal(): void {
        if (this.refusal()) {
            this.data.clearRefusal();
        }
    }
}
