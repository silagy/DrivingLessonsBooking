import { formatWindowInstant } from '../../../shared/dates/jerusalem-time';
import { weekRangeLabel } from '../../../shared/dates/week-label';
import { reminderMessageParams, whatsAppChatUrl } from './whatsapp-reminder';

describe('whatsAppChatUrl', () => {
    it('builds a wa.me link with the message encoded', () => {
        //given
        const message = 'שלום נועה, היי & ביי?\nשורה';

        //when
        const url = whatsAppChatUrl('972501234567', message);

        //then
        expect(url).toBe('https://wa.me/972501234567?text=' + encodeURIComponent(message));
    });
});

describe('reminderMessageParams', () => {
    it('formats the week and the close time in Hebrew, in Jerusalem time', () => {
        //given
        const input = {
            studentName: 'נועה לוי',
            teacherName: 'משה',
            weekNumber: 46,
            weekStart: '2026-11-08',
            windowEndUtc: '2026-11-05T18:00:00Z',
            link: 'https://drivecal.co.il/s/abc',
        };

        //when
        const params = reminderMessageParams(input);

        //then
        expect(params).toEqual({
            name: 'נועה לוי',
            teacherName: 'משה',
            weekNumber: 46,
            weekRange: weekRangeLabel('2026-11-08', 'he-IL'),
            closesAt: formatWindowInstant('2026-11-05T18:00:00Z', 'he-IL'),
            link: 'https://drivecal.co.il/s/abc',
        });
        expect(params.closesAt).toContain('20:00');
    });
});
