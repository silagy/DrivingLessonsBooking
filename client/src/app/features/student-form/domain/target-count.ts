export const MIN_TARGET_COUNT = 1;
export const ROOM_TO_SCHEDULE_MARGIN = 2;

export function missingPickCount(targetCount: number, pickCount: number): number {
    return Math.max(0, targetCount - pickCount);
}

export function leavesLittleRoom(targetCount: number, pickCount: number, openSlotCount: number): boolean {
    const coversTarget = pickCount >= targetCount;
    const withinMargin = pickCount < targetCount + ROOM_TO_SCHEDULE_MARGIN;
    const hasUnpickedOpenSlot = pickCount < openSlotCount;

    return coversTarget && withinMargin && hasUnpickedOpenSlot;
}
