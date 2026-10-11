import { CarMarker } from './car-marker.enum';
import { Student } from './student.model';

export type CarMarkerFlags = Pick<Student, 'isCarOfTeacher' | 'isTeacherDeleted' | 'isCarDeleted'>;

export function carMarkerOf(flags: CarMarkerFlags): CarMarker | null {
    if (flags.isCarDeleted) {
        return CarMarker.deleted;
    }

    if (!flags.isCarOfTeacher && !flags.isTeacherDeleted) {
        return CarMarker.notOfTeacher;
    }

    return null;
}
