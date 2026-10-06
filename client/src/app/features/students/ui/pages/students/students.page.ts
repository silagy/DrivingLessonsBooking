import { ChangeDetectionStrategy, Component, Type, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Menu, MenuModule } from 'primeng/menu';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';
import { StudentRow } from '../../../domain/student-row.model';
import { StudentStatusFilter } from '../../../domain/student-status-filter.enum';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { StudentsStore } from '../../../state/students.store';
import { TransmissionTagComponent } from '../../components/transmission-tag/transmission-tag.component';
import { AddStudentDialog } from '../../dialogs/add-student/add-student.dialog';
import { AddStudentDialogData } from '../../dialogs/add-student/add-student-dialog-data';
import {
    DeactivateStudentDialog,
    DeactivateStudentDialogData,
} from '../../dialogs/deactivate-student/deactivate-student.dialog';
import { EditStudentDialog } from '../../dialogs/edit-student/edit-student.dialog';
import { EditStudentDialogData } from '../../dialogs/edit-student/edit-student-dialog-data';

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
        MenuModule,
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
    private static readonly deactivateDialogWidth = '32.5rem';

    protected readonly store = inject(StudentsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);

    protected readonly allTeachers = ALL_TEACHERS;
    protected readonly importRosterLink = ['/', AppRoutes.students, AppRoutes.rosterImport];
    protected readonly rowActions = signal<MenuItem[]>([]);

    private readonly rowMenu = viewChild.required<Menu>('rowMenu');

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
        const data: AddStudentDialogData = {
            teachers: this.store.teacherOptions,
            cars: this.store.carOptions,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (newStudent) => this.store.create(newStudent),
            refreshCars: () => this.store.refreshCarOptions(),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(AddStudentDialog, this.transloco.translate('students.add.title'), StudentsPage.dialogWidth, data);
    }

    protected onRowActions(event: Event, student: StudentRow): void {
        this.rowActions.set(student.isActive ? this.activeRowActions(student) : this.inactiveRowActions(student));
        this.rowMenu().toggle(event);
    }

    private activeRowActions(student: StudentRow): MenuItem[] {
        return [
            this.editDetailsAction(student),
            { separator: true },
            {
                label: this.transloco.translate('students.actions.deactivate'),
                icon: 'pi pi-pause-circle',
                styleClass: 'students-menu__item--danger',
                command: () => this.onDeactivate(student),
            },
        ];
    }

    private inactiveRowActions(student: StudentRow): MenuItem[] {
        return [
            this.editDetailsAction(student),
            {
                label: this.transloco.translate('students.actions.reactivate'),
                icon: 'pi pi-replay',
                styleClass: 'students-menu__item--mirrored-icon',
                command: () => void this.store.reactivate(student),
            },
        ];
    }

    private editDetailsAction(student: StudentRow): MenuItem {
        return {
            label: this.transloco.translate('students.actions.editDetails'),
            icon: 'pi pi-pencil',
            command: () => void this.onEditDetails(student),
        };
    }

    private async onEditDetails(student: StudentRow): Promise<void> {
        const details = await this.store.loadDetails(student.id);

        if (!details) {
            return;
        }

        const data: EditStudentDialogData = {
            details,
            refusal: this.store.refusal,
            isSaving: this.store.isMutating,
            confirm: (change) => this.store.changeDetails(student.id, change),
            clearRefusal: () => this.store.clearRefusal(),
        };

        this.openDialog(EditStudentDialog, this.transloco.translate('students.edit.title'), StudentsPage.dialogWidth, data);
    }

    private onDeactivate(student: StudentRow): void {
        const data: DeactivateStudentDialogData = {
            student,
            refusal: this.store.refusal,
            isDeactivating: this.store.isMutating,
            confirm: () => this.store.deactivate(student),
        };

        this.openDialog(
            DeactivateStudentDialog,
            this.transloco.translate('students.deactivate.title', { name: isolateDirection(student.name) }),
            StudentsPage.deactivateDialogWidth,
            data,
        );
    }

    private openDialog<TData>(component: Type<unknown>, header: string, width: string, data: TData): void {
        this.store.clearRefusal();

        this.dialogs.open(component, {
            header,
            width,
            modal: true,
            dismissableMask: true,
            data,
        });
    }
}
