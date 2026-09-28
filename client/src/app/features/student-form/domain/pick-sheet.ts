import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { slotTimeLabel, StudentSlot } from './slot-day';
import { PickChoice, rankOf, SlotPick } from './slot-pick';

const NEW_PICK_CHOICE: PickChoice = { sessionType: SessionType.single, constraint: null };

export interface PickSheet {
    slotId: string;
    day: DayOfWeek;
    window: SlotWindow;
    timeLabel: string;
    rank: number;
    choice: PickChoice;
    isEditing: boolean;
}

export function pickSheetFor(slot: StudentSlot, picks: readonly SlotPick[]): PickSheet {
    const existing = picks.find(pick => pick.slotId === slot.id);
    const nextRank = picks.length + 1;

    return {
        slotId: slot.id,
        day: slot.day,
        window: slot.window,
        timeLabel: slotTimeLabel(slot),
        rank: rankOf(picks, slot.id) ?? nextRank,
        choice: existing ? { sessionType: existing.sessionType, constraint: existing.constraint } : NEW_PICK_CHOICE,
        isEditing: existing !== undefined,
    };
}
