import { Transmission } from '../domain/transmission.enum';

export interface AddCarRequest {
    name: string;
    type: string;
    transmission: Transmission;
}
