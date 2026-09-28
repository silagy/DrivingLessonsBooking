import { SessionType } from './session-type.enum';

export interface PickChoice {
    sessionType: SessionType;
    constraint: string | null;
}

export interface SlotPick extends PickChoice {
    slotId: string;
}

export function upsertPick(picks: readonly SlotPick[], pick: SlotPick): SlotPick[] {
    const isPicked = picks.some(existing => existing.slotId === pick.slotId);

    return isPicked
        ? picks.map(existing => (existing.slotId === pick.slotId ? pick : existing))
        : [...picks, pick];
}

export function removePick(picks: readonly SlotPick[], slotId: string): SlotPick[] {
    return picks.filter(pick => pick.slotId !== slotId);
}

export function rankOf(picks: readonly SlotPick[], slotId: string): number | null {
    const index = picks.findIndex(pick => pick.slotId === slotId);

    return index < 0 ? null : index + 1;
}
