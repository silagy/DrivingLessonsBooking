const JERUSALEM_TIME_ZONE = 'Asia/Jerusalem';

export function formatWindowInstant(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: JERUSALEM_TIME_ZONE,
        weekday: 'long',
        day: 'numeric',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
    });

    return formatter.format(new Date(utcIso));
}
