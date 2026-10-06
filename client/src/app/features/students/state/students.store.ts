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
import { StudentRefusal, refusalKindOf } from '../domain/student-refusal';
import { CarOption } from '../domain/car-option.model';
import { NewStudent } from '../domain/new-student.model';
import { DEFAULT_STUDENT_FILTERS, StudentFilters } from '../domain/student-filters.model';
import { visibleStudents } from '../domain/student-list';
import { StudentDetailsChange } from '../domain/student-details-change.model';
import { StudentDetails } from '../domain/student-details.model';
import { StudentRow } from '../domain/student-row.model';
import { StudentStatusFilter } from '../domain/student-status-filter.enum';
import { Student } from '../domain/student.model';
import { TeacherOption } from '../domain/teacher-option.model';

const SINGLE_STUDENT_COUNT = 1;
const EXISTING_STUDENT_NAME_PARAM = 'name';
const ISOLATED_REFUSAL_PARAMS = [EXISTING_STUDENT_NAME_PARAM];
const STALE_TOGGLE_CODES: ReadonlySet<string> = new Set([
    'studentAlreadyDeactivated',
    'studentAlreadyActive',
    'studentNotFound',
]);

@Injectable()
export class StudentsStore {
    private readonly api = inject(StudentsApiService);
    private readonly teacherOptionsApi = inject(TeacherOptionsApiService);
    private readonly carOptionsApi = inject(CarOptionsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);
    private readonly refusalState = signal<StudentRefusal | null>(null);
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
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async loadDetails(studentId: string): Promise<StudentDetails | null> {
        this.mutating.set(true);

        try {
            return await firstValueFrom(this.api.getStudent(studentId));
        } catch (error) {
            this.toast.apiError(error);
            return null;
        } finally {
            this.mutating.set(false);
        }
    }

    async changeDetails(studentId: string, change: StudentDetailsChange): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentDetails(studentId, change));
            this.toast.success('students.detailsSaved');
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async deactivate(student: Student): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.deactivateStudent(student.id));
            this.toast.success('students.deactivated', {
                key: 'students.deactivatedDetail',
                params: { name: isolateDirection(student.name) },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            if (!isStaleToggle(error)) {
                this.refusalState.set(this.refusalOf(error));
                return false;
            }

            this.toast.apiError(error);
            this.studentsResource.reload();
            return true;
        } finally {
            this.mutating.set(false);
        }
    }

    async reactivate(student: Student): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(this.api.reactivateStudent(student.id));
            this.toast.success('students.reactivated', {
                key: 'students.reactivatedDetail',
                params: { name: isolateDirection(student.name) },
            });
            this.studentsResource.reload();
        } catch (error) {
            this.toast.apiError(error);

            if (isStaleToggle(error)) {
                this.studentsResource.reload();
            }
        } finally {
            this.mutating.set(false);
        }
    }

    async changeTeacher(student: Student, teacherId: string, carId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentTeacher(student.id, { teacherId, carId }));
            this.toast.success('students.teacherChanged', {
                key: 'students.teacherChangedDetail',
                params: {
                    name: isolateDirection(student.name),
                    teacher: isolateDirection(this.teacherNameOf(teacherId)),
                    car: isolateDirection(this.carNameOf(carId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    async changeCar(student: Student, carId: string): Promise<boolean> {
        this.mutating.set(true);
        this.refusalState.set(null);

        try {
            await firstValueFrom(this.api.changeStudentCar(student.id, { carId }));
            this.toast.success('students.carChanged', {
                key: 'students.carChangedDetail',
                params: {
                    name: isolateDirection(student.name),
                    car: isolateDirection(this.carNameOf(carId)),
                },
            });
            this.studentsResource.reload();
            return true;
        } catch (error) {
            this.refusalState.set(this.refusalOf(error));
            return false;
        } finally {
            this.mutating.set(false);
        }
    }

    private refusalOf(error: unknown): StudentRefusal {
        return {
            kind: refusalKindOf(problemCodeOf(error)),
            message: this.toast.messageOf(error, ISOLATED_REFUSAL_PARAMS),
            existingStudentName: existingStudentNameOf(error),
        };
    }

    private teacherNameOf(teacherId: string): string {
        return this.teacherOptions().find((teacher) => teacher.id === teacherId)?.name ?? '';
    }

    private carNameOf(carId: string): string {
        return this.carOptions().find((car) => car.id === carId)?.name ?? '';
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

function problemOf(error: unknown): ProblemDetails | null {
    if (!(error instanceof HttpErrorResponse)) {
        return null;
    }

    return error.error as ProblemDetails | null;
}

function problemCodeOf(error: unknown): string | undefined {
    return problemOf(error)?.code;
}

function existingStudentNameOf(error: unknown): string | null {
    const name = problemOf(error)?.params?.[EXISTING_STUDENT_NAME_PARAM];

    return name ? isolateDirection(name) : null;
}

function isStaleToggle(error: unknown): boolean {
    const code = problemCodeOf(error);

    return code !== undefined && STALE_TOGGLE_CODES.has(code);
}
