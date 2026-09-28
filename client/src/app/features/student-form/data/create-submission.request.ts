import { SlotRequestForSubmissionRequest } from './slot-request-for-submission.request';

export interface CreateSubmissionRequest {
    nationalId: string;
    targetCount: number;
    slotRequests: SlotRequestForSubmissionRequest[];
}
