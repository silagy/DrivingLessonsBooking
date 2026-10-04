const MAX_INITIALS = 2;

export function initials(name: string): string {
    return name
        .split(/\s+/)
        .filter((word) => word.length)
        .slice(0, MAX_INITIALS)
        .map((word) => word[0])
        .join('');
}
