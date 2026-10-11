import { CarMarker } from './car-marker.enum';
import { Student } from './student.model';

export interface StudentRow extends Student {
    isNew: boolean;
    carMarker: CarMarker | null;
}
