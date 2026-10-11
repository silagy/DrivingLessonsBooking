const JERUSALEM_TIME_ZONE = 'Asia/Jerusalem';
const MILLIS_PER_MINUTE = 60_000;

export function jerusalemWallTimeToUtcIso(wallTime: Date): string {
    const wallAsUtc = Date.UTC(
        wallTime.getFullYear(),
        wallTime.getMonth(),
        wallTime.getDate(),
        wallTime.getHours(),
        wallTime.getMinutes(),
        0,
    );
    const offsetMinutes = jerusalemOffsetMinutes(new Date(wallAsUtc));

    return new Date(wallAsUtc - offsetMinutes * MILLIS_PER_MINUTE).toISOString();
}

export function formatInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: JERUSALEM_TIME_ZONE,
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}

export function formatWeekdayInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: JERUSALEM_TIME_ZONE,
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
    });

    return formatter.format(new Date(utcIso));
}

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

function jerusalemOffsetMinutes(instant: Date): number {
    const formatter = new Intl.DateTimeFormat('en-US', {
        timeZone: JERUSALEM_TIME_ZONE,
        hourCycle: 'h23',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
    });
    const parts = formatter.formatToParts(instant);
    const values = new Map(parts.map((part) => [part.type, part.value]));
    const wallAsUtc = Date.UTC(
        Number(values.get('year')),
        Number(values.get('month')) - 1,
        Number(values.get('day')),
        Number(values.get('hour')),
        Number(values.get('minute')),
        Number(values.get('second')),
    );

    return Math.round((wallAsUtc - instant.getTime()) / MILLIS_PER_MINUTE);
}
