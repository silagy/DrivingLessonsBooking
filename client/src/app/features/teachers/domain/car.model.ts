import { Transmission } from './transmission.enum';

export interface Car {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
}
