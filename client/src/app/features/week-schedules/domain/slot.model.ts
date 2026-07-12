import { WeekGridCell } from '../../../shared/models/week-grid-cell';
import { SlotState } from './slot-state.enum';

export interface Slot extends WeekGridCell {
    id: string;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}
