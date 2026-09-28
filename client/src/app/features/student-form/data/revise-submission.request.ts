import { SlotRequestForSubmissionRequest } from './slot-request-for-submission.request';

export interface ReviseSubmissionRequest {
    nationalId: string;
    targetCount: number;
    slotRequests: SlotRequestForSubmissionRequest[];
}
