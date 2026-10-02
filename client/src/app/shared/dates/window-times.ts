import { SlotWindow } from '../models/slot-window.enum';

const TIME_LABEL_LENGTH = 5;

export interface TimedSlot {
    window: SlotWindow;
    startLocal: string;
    endLocal: string;
}

export function windowTimesOf(slots: readonly TimedSlot[]): Partial<Record<SlotWindow, string>> {
    const times: Partial<Record<SlotWindow, string>> = {};

    for (const slot of slots) {
        times[slot.window] ??= `${timeLabel(slot.startLocal)}-${timeLabel(slot.endLocal)}`;
    }

    return times;
}

function timeLabel(time: string): string {
    return time.slice(0, TIME_LABEL_LENGTH);
}
