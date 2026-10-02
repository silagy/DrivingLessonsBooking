import { buildWeekOptions } from './week-options';

describe('buildWeekOptions', () => {
    it('starts at the Sunday of the given day\'s week', () => {
        //given
        const wednesday = new Date(2026, 9, 7);

        //when
        const [first, second] = buildWeekOptions('en-IL', wednesday);

        //then
        expect(first).toEqual({ weekStart: '2026-10-04', label: '4 Oct – 9 Oct 2026' });
        expect(second.weekStart).toBe('2026-10-11');
    });
});
