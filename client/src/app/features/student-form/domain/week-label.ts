import { parseIsoDate } from '../../../shared/dates/parse-iso-date';

const GRID_LAST_DAY_OFFSET = 5;
const THIN_SPACE = /\u2009/g;
const EN_DASH = /\u2013/g;

export interface WeekDates {
    start: Date;
    end: Date;
}

export function dateInWeek(weekStart: string, dayOffset: number): Date {
    const date = parseIsoDate(weekStart);
    date.setDate(date.getDate() + dayOffset);

    return date;
}

export function weekDates(weekStart: string): WeekDates {
    const start = dateInWeek(weekStart, 0);
    const end = dateInWeek(weekStart, GRID_LAST_DAY_OFFSET);

    return { start, end };
}

export function weekRangeLabel(weekStart: string, locale: string): string {
    const { start, end } = weekDates(weekStart);
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });

    return formatter
               .formatRange(start, end)
               .replace(THIN_SPACE, ' ')
               .replace(EN_DASH, '-');
}
