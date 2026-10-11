import { Signal } from '@angular/core';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';

export interface ChangeCarDialogData {
    student: Student;
    cars: Signal<CarOption[]>;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (carId: string) => Promise<boolean>;
    refresh: () => void;
    clearRefusal: () => void;
}
