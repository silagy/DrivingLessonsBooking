import { Transmission } from '../domain/transmission.enum';

export interface CreateCarRequest {
    name: string;
    type: string;
    transmission: Transmission;
}
