import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { dateInWeek } from './week-label';

const GRID_DAYS: readonly DayOfWeek[] = [
    DayOfWeek.sunday,
    DayOfWeek.monday,
    DayOfWeek.tuesday,
    DayOfWeek.wednesday,
    DayOfWeek.thursday,
    DayOfWeek.friday,
];

const GRID_WINDOWS: readonly SlotWindow[] = [
    SlotWindow.morning,
    SlotWindow.noon,
    SlotWindow.afternoon,
    SlotWindow.evening,
];

const TIME_LABEL_LENGTH = 5;

export interface StudentSlot {
    id: string;
    day: DayOfWeek;
    window: SlotWindow;
    state: SlotState;
    startLocal: string;
    endLocal: string;
}

export interface SlotChip {
    id: string;
    window: SlotWindow;
    timeLabel: string;
    isUnavailable: boolean;
}

export interface SlotDay {
    day: DayOfWeek;
    dateLabel: string;
    isShortDay: boolean;
    chips: SlotChip[];
}

export function groupSlotsByDay(slots: readonly StudentSlot[], weekStart: string, locale: string): SlotDay[] {
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'numeric' });

    return GRID_DAYS
        .map((day, dayOffset) => {
            const chips = chipsFor(slots, day);

            return {
                day,
                dateLabel: formatter.format(dateInWeek(weekStart, dayOffset)),
                isShortDay: chips.length < GRID_WINDOWS.length,
                chips,
            };
        })
        .filter(slotDay => slotDay.chips.length > 0);
}

function chipsFor(slots: readonly StudentSlot[], day: DayOfWeek): SlotChip[] {
    return slots
        .filter(slot => slot.day === day)
        .sort((first, second) => GRID_WINDOWS.indexOf(first.window) - GRID_WINDOWS.indexOf(second.window))
        .map(slot => ({
            id: slot.id,
            window: slot.window,
            timeLabel: `${timeLabel(slot.startLocal)}–${timeLabel(slot.endLocal)}`,
            isUnavailable: slot.state === SlotState.unavailable,
        }));
}

function timeLabel(localTime: string): string {
    return localTime.slice(0, TIME_LABEL_LENGTH);
}
