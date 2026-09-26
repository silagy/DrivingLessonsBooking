import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface GetPublicationByLinkResponse {
    weekStart: string;
    weekNumber: number;
    state: PublicationState;
    windowStartUtc: string;
    windowEndUtc: string;
}
