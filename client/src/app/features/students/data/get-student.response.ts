import { Transmission } from '../domain/transmission.enum';

export interface GetStudentResponse {
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
    address: string | null;
    startDate: string | null;
    licenseType: string | null;
    isActive: boolean;
}
