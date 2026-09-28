import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { Transmission } from '../domain/transmission.enum';

export interface IdentifyStudentResponse {
    studentName: string;
    teacherName: string;
    carName: string;
    transmission: Transmission;
    hasSubmission: boolean;
    slots: SlotForIdentifyStudentResponse[];
}

export interface SlotForIdentifyStudentResponse {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}
