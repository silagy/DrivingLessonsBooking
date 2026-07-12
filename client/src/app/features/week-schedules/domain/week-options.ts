export interface WeekOption {
    weekStart: string;
    label: string;
}

const WEEK_OPTION_COUNT = 5;
const DAYS_PER_WEEK = 7;
const GRID_LAST_DAY_OFFSET = 5;

export function currentWeekStart(today: Date = new Date()): string {
    const sunday = new Date(today);
    sunday.setDate(today.getDate() - today.getDay());

    return toIsoDate(sunday);
}

export function buildWeekOptions(locale: string, today: Date = new Date()): WeekOption[] {
    const firstSunday = new Date(currentWeekStart(today));

    return Array.from({ length: WEEK_OPTION_COUNT }, (_, index) => {
        const weekStart = addDays(firstSunday, index * DAYS_PER_WEEK);

        return {
            weekStart: toIsoDate(weekStart),
            label: weekRangeLabel(weekStart, locale),
        };
    });
}

export function weekRangeLabel(weekStart: Date, locale: string): string {
    const weekEnd = addDays(weekStart, GRID_LAST_DAY_OFFSET);
    const startLabel = weekStart.toLocaleDateString(locale, { day: 'numeric', month: 'short' });
    const endLabel = weekEnd.toLocaleDateString(locale, { day: 'numeric', month: 'short', year: 'numeric' });

    return `${startLabel} – ${endLabel}`;
}

function addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);

    return result;
}

function toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}
