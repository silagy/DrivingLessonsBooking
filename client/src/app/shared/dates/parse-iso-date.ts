export function parseIsoDate(isoDate: string): Date {
    const [year, month, day] = isoDate.split('-').map(Number);

    return new Date(year, month - 1, day);
}
