import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from '../domain/session-type.enum';
import { Transmission } from '../domain/transmission.enum';

export interface IdentifyStudentResponse {
    studentName: string;
    teacherName: string;
    carName: string;
    transmission: Transmission;
    submission: SubmissionForIdentifyStudentResponse | null;
    slots: SlotForIdentifyStudentResponse[];
}

export interface SubmissionForIdentifyStudentResponse {
    targetCount: number;
    lastSavedAtUtc: string;
    slotRequests: SlotRequestForIdentifyStudentResponse[];
}

export interface SlotRequestForIdentifyStudentResponse {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
}

export interface SlotForIdentifyStudentResponse {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}
