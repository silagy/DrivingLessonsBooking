import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { GetLatestRosterImportResponse } from '../data/get-latest-roster-import.response';
import { ItemForFindStudentsResponse } from '../data/item-for-find-students.response';
import { RosterApiService } from '../data/roster-api.service';
import { RosterEntryOutcome } from '../domain/roster-entry-outcome.enum';
import { TeacherFilterOption } from '../domain/teacher-filter-option.model';

const HTTP_NOT_FOUND = 404;

@Injectable({ providedIn: 'root' })
export class RosterStore {
    private readonly api = inject(RosterApiService);
    private readonly toast = inject(ToastService);

    private readonly selectedTeacherIdState = signal<string | null>(null);
    private readonly uploadingState = signal(false);

    private readonly latestImportResource = resource({
        loader: () => this.loadLatestImport(),
    });

    private readonly studentsResource = resource({
        loader: () => firstValueFrom(this.api.findStudents()),
    });

    readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();
    readonly uploading = this.uploadingState.asReadonly();

    readonly latestImport = computed<GetLatestRosterImportResponse | undefined>(() =>
        this.latestImportResource.value(),
    );

    readonly students = computed<ItemForFindStudentsResponse[]>(() => this.studentsResource.value() ?? []);

    readonly filteredStudents = computed<ItemForFindStudentsResponse[]>(() => {
        const teacherId = this.selectedTeacherIdState();
        const students = this.students();

        return teacherId ? students.filter((student) => student.teacherId === teacherId) : students;
    });

    readonly teacherFilterOptions = computed<TeacherFilterOption[]>(() => {
        const distinct = new Map<string, string>();

        for (const student of this.students()) {
            distinct.set(student.teacherId, student.teacherName);
        }

        return [...distinct.entries()]
            .map(([id, name]) => ({ id, name }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });

    readonly badgeByNationalId = computed<Map<string, RosterEntryOutcome>>(() => {
        const badges = new Map<string, RosterEntryOutcome>();

        for (const entry of this.latestImport()?.entries ?? []) {
            if (entry.outcome !== RosterEntryOutcome.deactivated) {
                badges.set(entry.nationalId, entry.outcome);
            }
        }

        return badges;
    });

    readonly failedRows = computed(() => this.latestImport()?.failures ?? []);

    readonly isLoading = computed(
        () => this.latestImportResource.isLoading() || this.studentsResource.isLoading(),
    );

    readonly loadError = computed(() => {
        const failed = this.latestImportResource.error() || this.studentsResource.error();

        return failed ? 'roster.loadFailed' : undefined;
    });

    readonly isEmpty = computed(
        () => !this.isLoading() && !this.students().length && this.latestImport() === undefined,
    );

    selectTeacher(teacherId: string | null): void {
        this.selectedTeacherIdState.set(teacherId);
    }

    async upload(file: File): Promise<void> {
        this.uploadingState.set(true);

        try {
            await firstValueFrom(this.api.importRoster(file));
            this.toast.success('roster.imported');
            this.latestImportResource.reload();
            this.studentsResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.uploadingState.set(false);
        }
    }

    private async loadLatestImport(): Promise<GetLatestRosterImportResponse | undefined> {
        try {
            return await firstValueFrom(this.api.getLatestImport());
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return undefined;
            }

            throw error;
        }
    }
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
