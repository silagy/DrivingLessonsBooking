import { DayOfWeek } from '../models/day-of-week.enum';
import { parseIsoDate } from './parse-iso-date';

const DAYS_FROM_SUNDAY: Readonly<Record<DayOfWeek, number>> = {
    [DayOfWeek.sunday]: 0,
    [DayOfWeek.monday]: 1,
    [DayOfWeek.tuesday]: 2,
    [DayOfWeek.wednesday]: 3,
    [DayOfWeek.thursday]: 4,
    [DayOfWeek.friday]: 5,
};

export function gridDayDate(weekStart: string, day: DayOfWeek, locale: string): string {
    const date = parseIsoDate(weekStart);
    date.setDate(date.getDate() + DAYS_FROM_SUNDAY[day]);

    return date.toLocaleDateString(locale, { day: 'numeric', month: 'numeric' });
}
