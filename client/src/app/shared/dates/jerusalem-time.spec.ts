import { formatWeekdayInstantInJerusalem, formatWindowInstant } from './jerusalem-time';

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

describe('formatWindowInstant', () => {
    it('renders a summer instant in Israel daylight time (UTC+3)', () => {
        expect(formatWindowInstant('2026-06-12T11:00:00Z', 'en')).toContain('14:00');
    });

    it('renders a winter instant in Israel standard time (UTC+2)', () => {
        expect(formatWindowInstant('2026-01-09T12:00:00Z', 'en')).toContain('14:00');
    });

    it('uses a 24-hour clock', () => {
        expect(formatWindowInstant('2026-06-12T16:30:00Z', 'en')).toContain('19:30');
    });
});
