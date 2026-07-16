export function formatInstantInJerusalem(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: 'Asia/Jerusalem',
        dateStyle: 'medium',
        timeStyle: 'short',
    });

    return formatter.format(new Date(utcIso));
}
