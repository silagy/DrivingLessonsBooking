import { Student } from './student.model';

export interface StudentDetails extends Student {
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
}
