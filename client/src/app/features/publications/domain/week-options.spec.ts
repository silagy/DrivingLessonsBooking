import { buildWeekOptions, withWeekOption } from './week-options';

describe('buildWeekOptions', () => {
    it('starts at the Sunday of the given day\'s week', () => {
        //given
        const wednesday = new Date(2026, 9, 7);

        //when
        const [first, second] = buildWeekOptions('en-IL', wednesday);

        //then
        expect(first).toEqual({ weekStart: '2026-10-04', label: '4 Oct - 9 Oct 2026' });
        expect(second.weekStart).toBe('2026-10-11');
    });
});

describe('withWeekOption', () => {
    it('keeps the options as they are when the week is already offered', () => {
        //given
        const options = buildWeekOptions('en-IL', new Date(2026, 9, 7));

        //when
        const choices = withWeekOption(options, '2026-10-11', 'en-IL');

        //then
        expect(choices).toEqual(options);
    });

    it('adds a week outside the offered range in date order', () => {
        //given
        const options = buildWeekOptions('en-IL', new Date(2026, 9, 7));

        //when
        const choices = withWeekOption(options, '2026-08-02', 'en-IL');

        //then
        expect(choices[0]).toEqual({ weekStart: '2026-08-02', label: '2 Aug - 7 Aug 2026' });
        expect(choices[1].weekStart).toBe('2026-10-04');
    });
});
