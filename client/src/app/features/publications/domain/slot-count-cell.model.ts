import { SlotState } from '../../../shared/models/slot-state.enum';
import { WeekGridCell } from '../../../shared/models/week-grid-cell';

export interface SlotCountCell extends WeekGridCell {
    slotId: string;
    state: SlotState;
    requestCount: number;
}
