import { AssignedTeacher } from './assigned-teacher.model';
import { Transmission } from './transmission.enum';

export interface Car {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
    assignedTeachers: AssignedTeacher[];
}
