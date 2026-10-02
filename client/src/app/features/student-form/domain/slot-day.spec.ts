import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { SessionType } from './session-type.enum';
import { groupSlotsByDay, hasOpenSlot, StudentSlot } from './slot-day';

const WEEK_START = '2026-10-04';

function slot(day: DayOfWeek, window: SlotWindow, state: SlotState): StudentSlot {
    return { id: `${day}-${window}`, day, window, state, startLocal: '07:00:00', endLocal: '12:00:00' };
}

describe('groupSlotsByDay', () => {
    it('groups Sunday through Friday in grid order whatever order the slots arrive in', () => {
        const slots = [
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.tuesday, SlotWindow.morning, SlotState.open),
        ];

        const days = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(days.map(x => x.day)).toEqual([DayOfWeek.sunday, DayOfWeek.tuesday, DayOfWeek.friday]);
    });

    it('orders windows morning to evening within a day', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.evening, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.afternoon, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
        ];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.chips.map(x => x.window)).toEqual([
            SlotWindow.morning,
            SlotWindow.noon,
            SlotWindow.afternoon,
            SlotWindow.evening,
        ]);
    });

    it('marks unavailable slots and labels the wall-clock window', () => {
        const slots = [slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable)];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.chips[0]).toEqual({
            id: 'sunday-morning',
            window: SlotWindow.morning,
            timeLabel: '07:00-12:00',
            isUnavailable: true,
            rank: null,
        });
    });

    it('labels each day with its date in the week', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
        ];

        const [sunday, friday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(sunday.dateLabel).toBe('10/4');
        expect(friday.dateLabel).toBe('10/9');
    });

    it('flags a day offering fewer windows than a full day', () => {
        const slots = [
            ...Object.values(SlotWindow).map(window => slot(DayOfWeek.thursday, window, SlotState.open)),
            slot(DayOfWeek.friday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.friday, SlotWindow.noon, SlotState.open),
        ];

        const [thursday, friday] = groupSlotsByDay(slots, WEEK_START, 'en', []);

        expect(thursday.isShortDay).toBe(false);
        expect(friday.isShortDay).toBe(true);
    });

    it('returns no days when the teacher has no grid this week', () => {
        expect(groupSlotsByDay([], WEEK_START, 'en', [])).toEqual([]);
    });

    it('numbers picked chips by their rank and leaves the rest unnumbered', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
            slot(DayOfWeek.sunday, SlotWindow.afternoon, SlotState.open),
        ];
        const picks = [
            { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
            { slotId: 'sunday-morning', sessionType: SessionType.double, constraint: null },
        ];

        const [sunday] = groupSlotsByDay(slots, WEEK_START, 'en', picks);

        expect(sunday.chips.map(x => x.rank)).toEqual([2, null, 1]);
    });
});

describe('hasOpenSlot', () => {
    it('is true when at least one slot is open', () => {
        const slots = [
            slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable),
            slot(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
        ];

        expect(hasOpenSlot(slots)).toBe(true);
    });

    it('is false for a grid that is entirely unavailable, or empty', () => {
        const slots = [slot(DayOfWeek.sunday, SlotWindow.morning, SlotState.unavailable)];

        expect(hasOpenSlot(slots)).toBe(false);
        expect(hasOpenSlot([])).toBe(false);
    });
});
