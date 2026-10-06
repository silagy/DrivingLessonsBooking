import { StudentStatusFilter } from './student-status-filter.enum';

export interface StudentFilters {
    teacherId: string | null;
    status: StudentStatusFilter;
    search: string;
}

export const DEFAULT_STUDENT_FILTERS: StudentFilters = {
    teacherId: null,
    status: StudentStatusFilter.active,
    search: '',
};
