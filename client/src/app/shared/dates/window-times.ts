import { SlotWindow } from '../models/slot-window.enum';

const TIME_LABEL_LENGTH = 5;
const RANGE_SEPARATOR = '-';
const WHOLE_HOUR_SUFFIX = ':00';

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

export function compactWindowTime(time: string): string {
    return time
        .split(RANGE_SEPARATOR)
        .map((end) => (end.endsWith(WHOLE_HOUR_SUFFIX) ? end.slice(0, -WHOLE_HOUR_SUFFIX.length) : end))
        .join(RANGE_SEPARATOR);
}

function timeLabel(time: string): string {
    return time.slice(0, TIME_LABEL_LENGTH);
}
