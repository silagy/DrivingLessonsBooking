import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { slotTimeLabel, StudentSlot } from './slot-day';
import { SlotPick } from './slot-pick';

export interface ReviewItem {
    slotId: string;
    rank: number;
    day: DayOfWeek;
    window: SlotWindow;
    timeLabel: string;
    sessionType: SessionType;
    constraint: string | null;
    isPreferred: boolean;
}

export function reviewItemsOf(
    picks: readonly SlotPick[],
    slots: readonly StudentSlot[],
    targetCount: number,
): ReviewItem[] {
    const slotsById = new Map(slots.map(slot => [slot.id, slot]));

    return picks.flatMap((pick, index) => {
        const slot = slotsById.get(pick.slotId);

        if (!slot) {
            return [];
        }

        const rank = index + 1;

        return [
            {
                slotId: pick.slotId,
                rank,
                day: slot.day,
                window: slot.window,
                timeLabel: slotTimeLabel(slot),
                sessionType: pick.sessionType,
                constraint: pick.constraint,
                isPreferred: rank <= targetCount,
            },
        ];
    });
}
