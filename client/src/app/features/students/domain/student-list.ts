import { StudentFilters } from './student-filters.model';
import { StudentRow } from './student-row.model';
import { StudentStatusFilter } from './student-status-filter.enum';
import { Student } from './student.model';

export function visibleStudents(
    students: readonly Student[],
    filters: StudentFilters,
    newStudentIds: ReadonlySet<string>,
): StudentRow[] {
    const rows = students
        .filter((student) => matchesFilters(student, filters))
        .map((student) => ({ ...student, isNew: newStudentIds.has(student.id) }));

    return [
        ...rows.filter((row) => row.isNew),
        ...rows.filter((row) => !row.isNew && row.isActive),
        ...rows.filter((row) => !row.isNew && !row.isActive),
    ];
}

function matchesFilters(student: Student, filters: StudentFilters): boolean {
    return matchesTeacher(student, filters.teacherId)
        && matchesStatus(student, filters.status)
        && matchesSearch(student, filters.search);
}

function matchesTeacher(student: Student, teacherId: string | null): boolean {
    return teacherId === null || student.teacherId === teacherId;
}

function matchesStatus(student: Student, status: StudentStatusFilter): boolean {
    switch (status) {
        case StudentStatusFilter.active:
            return student.isActive;
        case StudentStatusFilter.inactive:
            return !student.isActive;
        default:
            return true;
    }
}

function matchesSearch(student: Student, search: string): boolean {
    const query = search.trim().toLocaleLowerCase();

    if (!query) {
        return true;
    }

    return student.nationalId.includes(query) || student.name.toLocaleLowerCase().includes(query);
}
