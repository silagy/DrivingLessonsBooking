import { Transmission } from '../domain/transmission.enum';

export interface ItemForFindCarsResponse {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
    assignedTeachers: TeacherForFindCarsResponse[];
}

export interface TeacherForFindCarsResponse {
    id: string;
    name: string;
}
