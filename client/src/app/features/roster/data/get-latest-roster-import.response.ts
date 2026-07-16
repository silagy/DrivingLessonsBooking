import { RosterEntryOutcome } from '../domain/roster-entry-outcome.enum';
import { RosterRowFailureReason } from '../domain/roster-row-failure-reason.enum';

export interface GetLatestRosterImportResponse {
    id: string;
    fileName: string;
    importedAtUtc: string;
    added: number;
    updated: number;
    deactivated: number;
    failed: number;
    entries: EntryForGetLatestRosterImportResponse[];
    failures: FailureForGetLatestRosterImportResponse[];
}

export interface EntryForGetLatestRosterImportResponse {
    nationalId: string;
    outcome: RosterEntryOutcome;
}

export interface FailureForGetLatestRosterImportResponse {
    rowNumber: number;
    studentName: string;
    reason: RosterRowFailureReason;
}
