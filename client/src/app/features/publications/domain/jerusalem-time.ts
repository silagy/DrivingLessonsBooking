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
        timeZone: 'Asia/Jerusalem',
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}

function jerusalemOffsetMinutes(instant: Date): number {
    const formatter = new Intl.DateTimeFormat('en-US', {
        timeZone: 'Asia/Jerusalem',
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
