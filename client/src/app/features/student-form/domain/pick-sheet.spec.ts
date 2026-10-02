import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { pickSheetFor } from './pick-sheet';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

const THURSDAY_EVENING: StudentSlot = {
    id: 'thursday-evening',
    day: DayOfWeek.thursday,
    window: SlotWindow.evening,
    state: SlotState.open,
    startLocal: '18:00:00',
    endLocal: '22:00:00',
};

describe('pickSheetFor', () => {
    it('offers a new pick as the next rank, Single, with no constraint', () => {
        const picks = [
            { slotId: 'sunday-noon', sessionType: SessionType.double, constraint: 'late' },
            { slotId: 'monday-noon', sessionType: SessionType.single, constraint: null },
        ];

        expect(pickSheetFor(THURSDAY_EVENING, picks)).toEqual({
            slotId: 'thursday-evening',
            day: DayOfWeek.thursday,
            window: SlotWindow.evening,
            timeLabel: '18:00-22:00',
            rank: 3,
            choice: { sessionType: SessionType.single, constraint: null },
            isEditing: false,
        });
    });

    it('reopens a picked slot with its own rank and choices', () => {
        const picks = [
            { slotId: 'thursday-evening', sessionType: SessionType.double, constraint: 'only after 19:30' },
            { slotId: 'monday-noon', sessionType: SessionType.single, constraint: null },
        ];

        const sheet = pickSheetFor(THURSDAY_EVENING, picks);

        expect(sheet.rank).toBe(1);
        expect(sheet.choice).toEqual({ sessionType: SessionType.double, constraint: 'only after 19:30' });
        expect(sheet.isEditing).toBe(true);
    });
});
