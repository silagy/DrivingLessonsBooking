import { weekDates, weekRangeLabel } from './week-label';

describe('weekDates', () => {
    it('spans Sunday through Friday of the week', () => {
        const { start, end } = weekDates('2026-06-14');

        expect([start.getFullYear(), start.getMonth(), start.getDate()]).toEqual([2026, 5, 14]);
        expect([end.getFullYear(), end.getMonth(), end.getDate()]).toEqual([2026, 5, 19]);
    });

    it('crosses a month boundary', () => {
        const { end } = weekDates('2026-05-31');

        expect([end.getMonth(), end.getDate()]).toEqual([5, 5]);
    });

    it('crosses a year boundary', () => {
        const { end } = weekDates('2026-12-27');

        expect([end.getFullYear(), end.getMonth(), end.getDate()]).toEqual([2027, 0, 1]);
    });
});

describe('weekRangeLabel', () => {
    it('labels the first and last grid day', () => {
        const label = weekRangeLabel('2026-06-14', 'en');

        expect(label).toContain('14');
        expect(label).toContain('19');
    });

    it('joins the two days with a plain hyphen', () => {
        //given
        const weekStart = '2026-06-14';

        //when
        const label = weekRangeLabel(weekStart, 'en-IL');

        //then
        expect(label).toBe('14-19 Jun');
    });
});
