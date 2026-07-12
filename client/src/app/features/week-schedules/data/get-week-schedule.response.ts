import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SlotState } from '../domain/slot-state.enum';

export interface GetWeekScheduleResponse {
    id: string;
    teacherId: string;
    weekStart: string;
    slots: SlotForGetWeekScheduleResponse[];
}

export interface SlotForGetWeekScheduleResponse {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}
