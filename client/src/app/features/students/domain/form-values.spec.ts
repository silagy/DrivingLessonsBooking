import { fromIsoDate, optionalText, toIsoDate } from './form-values';

describe('optionalText', () => {
    it('trims a value', () => {
        //when
        const text = optionalText(' B ');

        //then
        expect(text).toBe('B');
    });

    it.each(['', '   '])('treats %j as absent', (value) => {
        //when
        const text = optionalText(value);

        //then
        expect(text).toBeNull();
    });
});

describe('toIsoDate', () => {
    it("keeps the picked date's calendar day, late in the evening too", () => {
        //when
        const text = toIsoDate(new Date(2026, 8, 1, 23, 30));

        //then
        expect(text).toBe('2026-09-01');
    });
});

describe('fromIsoDate', () => {
    it('reads the calendar day as local midnight', () => {
        //when
        const date = fromIsoDate('2026-09-01');

        //then
        expect([date.getFullYear(), date.getMonth(), date.getDate(), date.getHours()]).toEqual([2026, 8, 1, 0]);
    });

    it('round-trips the saved start date', () => {
        //when
        const text = toIsoDate(fromIsoDate('2026-12-31'));

        //then
        expect(text).toBe('2026-12-31');
    });
});
