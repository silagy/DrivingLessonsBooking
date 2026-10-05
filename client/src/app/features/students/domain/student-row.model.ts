import { Student } from './student.model';

export interface StudentRow extends Student {
    isNew: boolean;
}
