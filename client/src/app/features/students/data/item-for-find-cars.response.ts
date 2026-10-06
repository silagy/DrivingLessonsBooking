import { Transmission } from '../domain/transmission.enum';

export interface TeacherForFindCarsResponse {
    id: string;
    name: string;
}

export interface ItemForFindCarsResponse {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
    assignedTeachers: TeacherForFindCarsResponse[];
}
