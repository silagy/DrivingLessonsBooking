import { DayOfWeek } from '../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { loadedSubmissionOf, SavedSubmission } from './loaded-submission';
import { SessionType } from './session-type.enum';
import { StudentSlot } from './slot-day';

function slotOf(day: DayOfWeek, window: SlotWindow, state: SlotState): StudentSlot {
    return { id: `${day}-${window}`, day, window, state, startLocal: '12:00:00', endLocal: '15:00:00' };
}

const GRID: readonly StudentSlot[] = [
    slotOf(DayOfWeek.sunday, SlotWindow.noon, SlotState.open),
    slotOf(DayOfWeek.monday, SlotWindow.noon, SlotState.open),
    slotOf(DayOfWeek.tuesday, SlotWindow.noon, SlotState.unavailable),
    slotOf(DayOfWeek.wednesday, SlotWindow.noon, SlotState.open),
];

function savedWith(targetCount: number, slotIds: readonly string[]): SavedSubmission {
    return {
        targetCount,
        lastSavedAtUtc: '2026-11-12T08:30:00Z',
        slotRequests: slotIds.map(slotId => ({ slotId, sessionType: SessionType.single, constraint: null })),
    };
}

describe('loadedSubmissionOf', () => {
    it('starts a student without a submission at one lesson with no picks', () => {
        expect(loadedSubmissionOf(null, GRID)).toEqual({ targetCount: 1, picks: [], droppedPickCount: 0 });
    });

    it('loads the saved target and picks in rank order with their session type and constraint', () => {
        const saved: SavedSubmission = {
            targetCount: 3,
            lastSavedAtUtc: '2026-11-12T08:30:00Z',
            slotRequests: [
                { slotId: 'wednesday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                { slotId: 'sunday-noon', sessionType: SessionType.single, constraint: null },
            ],
        };

        expect(loadedSubmissionOf(saved, GRID)).toEqual({
            targetCount: 3,
            picks: [
                { slotId: 'wednesday-noon', sessionType: SessionType.double, constraint: 'only after 16:00' },
                { slotId: 'sunday-noon', sessionType: SessionType.single, constraint: null },
            ],
            droppedPickCount: 0,
        });
    });

    it('drops a saved pick whose slot is now unavailable, keeping the others in order', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['monday-noon', 'tuesday-noon', 'sunday-noon']), GRID);

        expect(loaded.picks.map(pick => pick.slotId)).toEqual(['monday-noon', 'sunday-noon']);
        expect(loaded.droppedPickCount).toBe(1);
    });

    it('drops saved picks that are not in the current grid at all, such as a previous teacher\'s', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['other-teacher-slot', 'sunday-noon', 'another-slot']), GRID);

        expect(loaded.picks.map(pick => pick.slotId)).toEqual(['sunday-noon']);
        expect(loaded.droppedPickCount).toBe(2);
    });

    it('keeps the saved target even when fewer picks survive', () => {
        const loaded = loadedSubmissionOf(savedWith(2, ['tuesday-noon', 'sunday-noon']), GRID);

        expect(loaded.targetCount).toBe(2);
        expect(loaded.picks.length).toBe(1);
    });

    it('never shares the picks list between loads', () => {
        const first = loadedSubmissionOf(null, GRID);
        const second = loadedSubmissionOf(null, GRID);

        expect(first.picks).not.toBe(second.picks);
    });
});
