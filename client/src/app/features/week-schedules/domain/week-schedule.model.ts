import { Slot } from './slot.model';

export interface WeekSchedule {
    id: string;
    teacherId: string;
    weekStart: string;
    slots: Slot[];
}
