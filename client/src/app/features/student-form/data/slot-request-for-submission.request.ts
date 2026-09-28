import { SessionType } from '../domain/session-type.enum';

export interface SlotRequestForSubmissionRequest {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
}
