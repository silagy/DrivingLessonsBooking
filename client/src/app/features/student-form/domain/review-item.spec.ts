import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { reviewItemsOf } from './review-item';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

function slot(id: string, day: DayOfWeek, window: SlotWindow, startLocal: string, endLocal: string): StudentSlot {
    return { id, day, window, state: SlotState.open, startLocal, endLocal };
}

const SLOTS = [
    slot('sunday-afternoon', DayOfWeek.sunday, SlotWindow.afternoon, '15:00:00', '18:00:00'),
    slot('monday-evening', DayOfWeek.monday, SlotWindow.evening, '18:00:00', '22:00:00'),
    slot('friday-morning', DayOfWeek.friday, SlotWindow.morning, '07:00:00', '12:00:00'),
];

describe('reviewItemsOf', () => {
    it('lists picks in rank order with their slot, session type and constraint', () => {
        const picks = [
            { slotId: 'monday-evening', sessionType: SessionType.double, constraint: 'only after 19:30' },
            { slotId: 'sunday-afternoon', sessionType: SessionType.single, constraint: null },
        ];

        expect(reviewItemsOf(picks, SLOTS, 2)).toEqual([
            {
                slotId: 'monday-evening',
                rank: 1,
                day: DayOfWeek.monday,
                window: SlotWindow.evening,
                timeLabel: '18:00-22:00',
                sessionType: SessionType.double,
                constraint: 'only after 19:30',
                isPreferred: true,
            },
            {
                slotId: 'sunday-afternoon',
                rank: 2,
                day: DayOfWeek.sunday,
                window: SlotWindow.afternoon,
                timeLabel: '15:00-18:00',
                sessionType: SessionType.single,
                constraint: null,
                isPreferred: true,
            },
        ]);
    });

    it('marks the ranks past the target as backups', () => {
        const picks = SLOTS.map(x => ({ slotId: x.id, sessionType: SessionType.single, constraint: null }));

        const items = reviewItemsOf(picks, SLOTS, 1);

        expect(items.map(x => x.isPreferred)).toEqual([true, false, false]);
    });
});
