import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { ProblemDetails } from '../../../shared/models/problem-details';
import { isolateDirection } from '../../../shared/text/isolate-direction';
import { CarOptionsApiService } from '../data/car-options-api.service';
import { ItemForFindCarsResponse } from '../data/item-for-find-cars.response';
import { StudentsApiService } from '../data/students-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { AddStudentRefusal, refusalKindOf } from '../domain/add-student-refusal';
import { CarOption } from '../domain/car-option.model';
import { NewStudent } from '../domain/new-student.model';
import { DEFAULT_STUDENT_FILTERS, StudentFilters } from '../domain/student-filters.model';
import { visibleStudents } from '../domain/student-list';
import { StudentRow } from '../domain/student-row.model';
import { StudentStatusFilter } from '../domain/student-status-filter.enum';
import { Student } from '../domain/student.model';
import { TeacherOption } from '../domain/teacher-option.model';

const SINGLE_STUDENT_COUNT = 1;
const ISOLATED_REFUSAL_PARAMS = ['name'];

@Injectable()
export class StudentsStore {
    private readonly api = inject(StudentsApiService);
    private readonly teacherOptionsApi = inject(TeacherOptionsApiService);
    private readonly carOptionsApi = inject(CarOptionsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);
    private readonly refusalState = signal<AddStudentRefusal | null>(null);
    private readonly filtersState = signal<StudentFilters>(DEFAULT_STUDENT_FILTERS);
    private readonly newStudentIds = signal<ReadonlySet<string>>(new Set<string>());

    private readonly studentsResource = resource({
        loader: () => firstValueFrom(this.api.findStudents()),
    });

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teacherOptionsApi.findTeachers()),
    });

    private readonly carsResource = resource({
        loader: () => firstValueFrom(this.carOptionsApi.findCars()),
    });

    readonly students = computed<Student[]>(() =>
        this.studentsResource.hasValue() ? this.studentsResource.value() : [],
    );

    readonly teacherOptions = computed<TeacherOption[]>(() => {
        const teachers = this.teachersResource.hasValue() ? this.teachersResource.value() : [];

        return teachers
            .map((teacher) => ({ id: teacher.id, name: teacher.name }))
            .sort((first, second) => first.name.localeCompare(second.name));
    });

    readonly carOptions = computed<CarOption[]>(() => {
        const cars = this.carsResource.hasValue() ? this.carsResource.value() : [];

        return cars.map(toCarOption);
    });

    readonly filters = this.filtersState.asReadonly();

    readonly visibleStudents = computed<StudentRow[]>(() =>
        visibleStudents(this.students(), this.filtersState(), this.newStudentIds()),
    );

    readonly isLoading = computed(() => this.studentsResource.isLoading() && !this.studentsResource.hasValue());
    readonly loadError = computed(() => (this.studentsResource.error() ? 'students.loadFailed' : null));
    readonly isEmpty = computed(() => this.studentsResource.hasValue() && !this.students().length);
    readonly hasNoMatch = computed(() => this.students().length > 0 && !this.visibleStudents().length);
    readonly footerKey = computed(() =>
        this.visibleStudents().length === SINGLE_STUDENT_COUNT ? 'students.footerOne' : 'students.footer',
    );
    readonly isMutating = this.mutating.asReadonly();
    readonly refusal = this.refusalState.asReadonly();

    selectTeacher(teacherId: string | null): void {
        this.filtersState.update((filters) => ({ ...filters, teacherId }));
    }

    selectStatus(status: StudentStatusFilter): void {
        this.filtersState.update((filters) => ({ ...filters, status }));
    }

    search(text: string): void {
        this.filtersState.update((filters) => ({ ...filters, search: text }));
    }

    clearFilters(): void {
        this.filtersState.set(DEFAULT_STUDENT_FILTERS);
    }

    reload(): void {
        this.studentsResource.reload();
        this.teachersResource.reload();
        this.carsResource.reload();
    }

    refreshCarOptions(): void {
        this.refusalState.set(null);
        this.carsResource.reload();
    }

    clearRefusal(): void {
        this.refusalState.set(null);
    }

    async create(newStudent: NewStudent): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            const created = await firstValueFrom(this.api.createStudent(newStudent));
            this.newStudentIds.update((ids) => new Set([...ids, created.id]));
            this.toast.success('students.added', {
                key: 'students.addedDetail',
                params: {
                    name: isolateDirection(newStudent.name),
                    teacher: isolateDirection(this.teacherNameOf(newStudent.teacherId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set({
                kind: refusalKindOf(problemCodeOf(error)),
                message: this.toast.messageOf(error, ISOLATED_REFUSAL_PARAMS),
            });
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    private teacherNameOf(teacherId: string): string {
        return this.teacherOptions().find((teacher) => teacher.id === teacherId)?.name ?? '';
    }
}

function toCarOption(car: ItemForFindCarsResponse): CarOption {
    return {
        id: car.id,
        name: car.name,
        transmission: car.transmission,
        teacherIds: car.assignedTeachers.map((teacher) => teacher.id),
    };
}

function problemCodeOf(error: unknown): string | undefined {
    if (!(error instanceof HttpErrorResponse)) {
        return undefined;
    }

    const problem = error.error as ProblemDetails | null;

    return problem?.code;
}
