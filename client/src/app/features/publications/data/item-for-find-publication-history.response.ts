import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface ItemForFindPublicationHistoryResponse {
    publicationId: string;
    weekStart: string;
    teacherId: string;
    teacherName: string;
    state: PublicationState;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
    latestExcelVersion: number | null;
}
