import { SlotState } from '../../../shared/models/slot-state.enum';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';
import { SlotPick } from './slot-pick';
import { MIN_TARGET_COUNT } from './target-count';

export interface SavedSlotRequest {
    slotId: string;
    sessionType: SessionType;
    constraint: string | null;
}

export interface SavedSubmission {
    targetCount: number;
    lastSavedAtUtc: string;
    slotRequests: readonly SavedSlotRequest[];
}

export interface LoadedSubmission {
    targetCount: number;
    picks: SlotPick[];
    droppedPickCount: number;
}

export function loadedSubmissionOf(saved: SavedSubmission | null, slots: readonly StudentSlot[]): LoadedSubmission {
    if (!saved) {
        return { targetCount: MIN_TARGET_COUNT, picks: [], droppedPickCount: 0 };
    }

    const openSlotIds = new Set(slots.filter(slot => slot.state === SlotState.open).map(slot => slot.id));
    const picks = saved.slotRequests
        .filter(request => openSlotIds.has(request.slotId))
        .map(({ slotId, sessionType, constraint }) => ({ slotId, sessionType, constraint }));

    return {
        targetCount: saved.targetCount,
        picks,
        droppedPickCount: saved.slotRequests.length - picks.length,
    };
}
