import { formatWindowInstant } from './jerusalem-time';

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
