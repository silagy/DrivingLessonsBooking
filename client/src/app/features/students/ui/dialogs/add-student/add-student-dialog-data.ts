import { Signal } from '@angular/core';
import { StudentRefusal } from '../../../domain/student-refusal';
import { CarOption } from '../../../domain/car-option.model';
import { NewStudent } from '../../../domain/new-student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';

export interface AddStudentDialogData {
    teachers: Signal<TeacherOption[]>;
    cars: Signal<CarOption[]>;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (newStudent: NewStudent) => Promise<boolean>;
    refreshCars: () => void;
    clearRefusal: () => void;
}
