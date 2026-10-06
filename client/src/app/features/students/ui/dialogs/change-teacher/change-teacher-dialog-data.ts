import { Signal } from '@angular/core';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';

export interface ChangeTeacherDialogData {
    student: Student;
    teachers: Signal<TeacherOption[]>;
    cars: Signal<CarOption[]>;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (teacherId: string, carId: string) => Promise<boolean>;
    refresh: () => void;
    clearRefusal: () => void;
}
