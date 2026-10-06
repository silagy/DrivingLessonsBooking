import { Transmission } from './transmission.enum';

export interface Student {
    id: string;
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    teacherName: string;
    carId: string;
    carName: string;
    carTransmission: Transmission;
    isActive: boolean;
}
