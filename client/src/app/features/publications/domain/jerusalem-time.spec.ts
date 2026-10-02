import { formatWeekdayInstantInJerusalem } from './jerusalem-time';

describe('formatWeekdayInstantInJerusalem', () => {
    it('names the weekday and shows the Jerusalem wall time', () => {
        //given
        const closesAtUtc = '2026-10-09T11:00:00Z';

        //when
        const label = formatWeekdayInstantInJerusalem(closesAtUtc, 'en-IL');

        //then
        expect(label).toBe('Friday, 9 October at 14:00');
    });
});
