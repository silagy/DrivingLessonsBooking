import { Transmission } from '../domain/transmission.enum';

export interface ItemForFindStudentsResponse {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    carTransmission: Transmission;
    isCarOfTeacher: boolean;
    isActive: boolean;
}
