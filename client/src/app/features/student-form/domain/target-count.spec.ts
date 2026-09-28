import { MIN_TARGET_COUNT, missingPickCount } from './target-count';

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
    ])('target %i is covered by %i picks, extra picks are backups', (targetCount, pickCount) => {
        expect(missingPickCount(targetCount, pickCount)).toBe(0);
    });
});
