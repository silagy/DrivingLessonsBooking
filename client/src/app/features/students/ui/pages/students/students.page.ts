import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { StudentStatusFilter } from '../../../domain/student-status-filter.enum';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { StudentsStore } from '../../../state/students.store';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { AddStudentDialog } from '../../dialogs/add-student/add-student.dialog';
import { AddStudentDialogData } from '../../dialogs/add-student/add-student-dialog-data';

const ALL_TEACHERS = 'all';

interface StatusOption {
    value: StudentStatusFilter;
    labelKey: string;
}

@Component({
    selector: 'app-students-page',
    imports: [
        FormsModule,
        RouterLink,
        TranslocoPipe,
        InitialsPipe,
        ButtonModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule,
        ProgressSpinnerModule,
        SelectModule,
        SelectButtonModule,
        TableModule,
        TagModule,
        TransmissionTagComponent,
    ],
    templateUrl: './students.page.html',
    styleUrl: './students.page.scss',
    providers: [StudentsStore, DialogService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsPage {
    private static readonly dialogWidth = '35rem';

    protected readonly store = inject(StudentsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly allTeachers = ALL_TEACHERS;
    protected readonly importRosterLink = ['/', AppRoutes.students, AppRoutes.rosterImport];

    protected readonly statusOptions: StatusOption[] = [
        { value: StudentStatusFilter.active, labelKey: 'students.filters.active' },
        { value: StudentStatusFilter.inactive, labelKey: 'students.filters.inactive' },
        { value: StudentStatusFilter.all, labelKey: 'students.filters.all' },
    ];

    protected readonly teacherFilterOptions = computed<TeacherOption[]>(() => [
        { id: ALL_TEACHERS, name: '' },
        ...this.store.teacherOptions(),
    ]);

    protected readonly selectedTeacher = computed(() => this.store.filters().teacherId ?? ALL_TEACHERS);

    protected onTeacherFilter(value: string): void {
        this.store.selectTeacher(value === ALL_TEACHERS ? null : value);
    }

    protected onStatusFilter(value: StudentStatusFilter): void {
        this.store.selectStatus(value);
    }

    protected onSearch(text: string): void {
        this.store.search(text);
    }

    protected onAddStudent(): void {
        this.store.clearRefusal();

        const data: AddStudentDialogData = {
            teachers: this.store.teacherOptions,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (newStudent) => this.store.create(newStudent),
            refreshCars: () => this.store.refreshCarOptions(),
        };

        this.dialogs.open(AddStudentDialog, {
            header: this.transloco.translate('students.add.title'),
            width: StudentsPage.dialogWidth,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
