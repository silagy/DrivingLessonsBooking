import { DayOfWeek } from './day-of-week.enum';
import { SlotWindow } from './slot-window.enum';

export interface WeekGridCell {
    day: DayOfWeek;
    window: SlotWindow;
}
