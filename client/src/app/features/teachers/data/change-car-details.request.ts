import { Transmission } from '../domain/transmission.enum';

export interface ChangeCarDetailsRequest {
    name: string;
    type: string;
    transmission: Transmission;
}
