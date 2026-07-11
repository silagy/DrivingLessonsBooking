import { Car } from './car.model';

export interface Teacher {
    id: string;
    name: string;
    contactEmail: string;
    cars: Car[];
}
