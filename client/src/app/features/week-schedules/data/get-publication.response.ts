import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface GetPublicationResponse {
    id: string;
    weekStart: string;
    state: PublicationState;
    linkToken: string;
    windowStartUtc: string | null;
    windowEndUtc: string | null;
}
