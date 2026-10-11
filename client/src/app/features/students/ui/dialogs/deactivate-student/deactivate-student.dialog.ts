import { ChangeDetectionStrategy, Component, Signal, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { DialogRefusalComponent } from '../../../../../shared/components/dialog-refusal/dialog-refusal.component';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { StudentWhoCardComponent } from '../../components/student-who-card/student-who-card.component';

export interface DeactivateStudentDialogData {
    student: Student;
    refusal: Signal<StudentRefusal | null>;
    isDeactivating: Signal<boolean>;
    confirm: () => Promise<boolean>;
}

@Component({
    selector: 'app-deactivate-student-dialog',
    imports: [TranslocoPipe, ButtonModule, DialogRefusalComponent, StudentWhoCardComponent],
    templateUrl: './deactivate-student.dialog.html',
    styleUrl: './deactivate-student.dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeactivateStudentDialog {
    private readonly ref = inject(DynamicDialogRef);
    private readonly data = inject(DynamicDialogConfig<DeactivateStudentDialogData>).data as DeactivateStudentDialogData;

    protected readonly student = this.data.student;
    protected readonly refusal = this.data.refusal;
    protected readonly isDeactivating = this.data.isDeactivating;

    protected async confirm(): Promise<void> {
        const done = await this.data.confirm();

        if (done) {
            this.ref.close();
        }
    }

    protected cancel(): void {
        this.ref.close();
    }
}
