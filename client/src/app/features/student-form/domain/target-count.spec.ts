import { leavesLittleRoom, MIN_TARGET_COUNT, missingPickCount, ROOM_TO_SCHEDULE_MARGIN } from './target-count';

describe('target count', () => {
    it('starts from one lesson', () => {
        expect(MIN_TARGET_COUNT).toBe(1);
    });

    it.each([
        [3, 2, 1],
        [30, 5, 25],
        [2, 0, 2],
    ])('target %i with %i picks is %i short', (targetCount, pickCount, missing) => {
        expect(missingPickCount(targetCount, pickCount)).toBe(missing);
    });

    it.each([
        [2, 2],
        [2, 5],
    ])('target %i is covered by %i picks', (targetCount, pickCount) => {
        expect(missingPickCount(targetCount, pickCount)).toBe(0);
    });

    it.each([
        [1, 1, 22],
        [1, 2, 22],
        [2, 2, 22],
        [2, 3, 22],
    ])('target %i with %i of %i open slots picked leaves little room', (targetCount, pickCount, openSlotCount) => {
        expect(leavesLittleRoom(targetCount, pickCount, openSlotCount)).toBe(true);
    });

    it.each([
        [1, 3, 22],
        [2, 4, 22],
        [2, 1, 22],
        [1, 2, 2],
        [2, 3, 3],
    ])(
        'target %i with %i of %i open slots picked leaves enough room or is short',
        (targetCount, pickCount, openSlotCount) => {
            expect(leavesLittleRoom(targetCount, pickCount, openSlotCount)).toBe(false);
        },
    );

    it('asks for two slots beyond the target', () => {
        expect(ROOM_TO_SCHEDULE_MARGIN).toBe(2);
    });
});
