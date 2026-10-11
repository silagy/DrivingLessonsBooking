const MONTH_OFFSET = 1;
const DATE_PART_LENGTH = 2;
const DATE_PART_PAD = '0';
const ISO_DATE_SEPARATOR = '-';
const DECIMAL_RADIX = 10;

export function optionalText(value: string): string | null {
    const trimmed = value.trim();

    return trimmed ? trimmed : null;
}

export function toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + MONTH_OFFSET).padStart(DATE_PART_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_LENGTH, DATE_PART_PAD);

    return `${year}-${month}-${day}`;
}

export function fromIsoDate(text: string): Date {
    const [year, month, day] = text.split(ISO_DATE_SEPARATOR).map((part) => Number.parseInt(part, DECIMAL_RADIX));

    return new Date(year, month - MONTH_OFFSET, day);
}
