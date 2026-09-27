const GRID_LAST_DAY_OFFSET = 5;

export interface WeekDates {
    start: Date;
    end: Date;
}

export function dateInWeek(weekStart: string, dayOffset: number): Date {
    const [year, month, day] = weekStart.split('-').map(Number);

    return new Date(year, month - 1, day + dayOffset);
}

export function weekDates(weekStart: string): WeekDates {
    const start = dateInWeek(weekStart, 0);
    const end = dateInWeek(weekStart, GRID_LAST_DAY_OFFSET);

    return { start, end };
}

export function weekRangeLabel(weekStart: string, locale: string): string {
    const { start, end } = weekDates(weekStart);
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });

    return formatter.formatRange(start, end);
}
