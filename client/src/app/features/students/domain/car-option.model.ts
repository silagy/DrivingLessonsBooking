import { Transmission } from './transmission.enum';

export interface CarOption {
    id: string;
    name: string;
    transmission: Transmission;
    teacherIds: readonly string[];
}
