import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { ReminderRow } from '../../../domain/reminder-row.model';
import { TeacherOption } from '../../../domain/teacher-option.model';

const ALL_TEACHERS = 'all-teachers';

interface TeacherFilterOption {
    id: string;
    name: string;
}

@Component({
    selector: 'app-students-without-submission',
    imports: [FormsModule, TranslocoPipe, ButtonModule, ProgressSpinnerModule, SelectModule, InitialsPipe],
    templateUrl: './students-without-submission.component.html',
    styleUrl: './students-without-submission.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsWithoutSubmissionComponent {
    readonly rows = input.required<ReminderRow[]>();
    readonly isLoading = input.required<boolean>();
    readonly errorKey = input.required<string | null>();
    readonly showTeacher = input.required<boolean>();
    readonly teachers = input.required<TeacherOption[]>();
    readonly selectedTeacherId = input.required<string | null>();
    readonly dataAsOf = input.required<string>();

    readonly teacherFiltered = output<string | null>();

    protected readonly allTeachers = ALL_TEACHERS;

    protected readonly filterOptions = computed<TeacherFilterOption[]>(() => [
        { id: ALL_TEACHERS, name: '' },
        ...this.teachers().map((teacher) => ({ id: teacher.id, name: teacher.name })),
    ]);

    protected readonly filterValue = computed(() => this.selectedTeacherId() ?? ALL_TEACHERS);

    protected onTeacherFiltered(value: string): void {
        this.teacherFiltered.emit(value === ALL_TEACHERS ? null : value);
    }
}
