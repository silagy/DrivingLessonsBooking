import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';

export interface GetPublicationDashboardResponse {
    state: PublicationState;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
    linkToken: string;
    studentsSubmitted: number;
    totalPicks: number;
    lastSubmissionAtUtc: string | null;
    latestExcelVersion: number | null;
    slotCounts: SlotCountForGetPublicationDashboardResponse[];
}

export interface SlotCountForGetPublicationDashboardResponse {
    slotId: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    requestCount: number;
}
