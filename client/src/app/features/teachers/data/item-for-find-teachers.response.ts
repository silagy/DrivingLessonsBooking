import { Transmission } from '../domain/transmission.enum';

export interface CarForFindTeachersResponse {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
}

export interface ItemForFindTeachersResponse {
    id: string;
    name: string;
    contactEmail: string;
    cars: CarForFindTeachersResponse[];
}
