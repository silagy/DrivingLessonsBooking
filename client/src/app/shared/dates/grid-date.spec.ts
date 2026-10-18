import { DayOfWeek } from '../models/day-of-week.enum';
import { gridDayDate } from './grid-date';

const SUNDAY_18_OCTOBER = '2026-10-18';

describe('gridDayDate', () => {
    it('dates a weekday of the week in Hebrew', () => {
        expect(gridDayDate(SUNDAY_18_OCTOBER, DayOfWeek.wednesday, 'he-IL')).toBe('21.10');
    });

    it('dates a weekday of the week in English', () => {
        expect(gridDayDate(SUNDAY_18_OCTOBER, DayOfWeek.wednesday, 'en-IL')).toBe('21/10');
    });

    it('dates the Sunday the week starts on', () => {
        expect(gridDayDate(SUNDAY_18_OCTOBER, DayOfWeek.sunday, 'he-IL')).toBe('18.10');
    });

    it('dates the Friday the week ends on', () => {
        expect(gridDayDate(SUNDAY_18_OCTOBER, DayOfWeek.friday, 'he-IL')).toBe('23.10');
    });
});
