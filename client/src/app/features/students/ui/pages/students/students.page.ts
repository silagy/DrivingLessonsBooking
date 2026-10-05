import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
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
    providers: [StudentsStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsPage {
    protected readonly store = inject(StudentsStore);

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
}
