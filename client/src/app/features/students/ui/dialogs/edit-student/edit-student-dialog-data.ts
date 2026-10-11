import { Signal } from '@angular/core';
import { StudentDetailsChange } from '../../../domain/student-details-change.model';
import { StudentDetails } from '../../../domain/student-details.model';
import { StudentRefusal } from '../../../domain/student-refusal';

export interface EditStudentDialogData {
    details: StudentDetails;
    refusal: Signal<StudentRefusal | null>;
    isSaving: Signal<boolean>;
    confirm: (change: StudentDetailsChange) => Promise<boolean>;
    clearRefusal: () => void;
}
