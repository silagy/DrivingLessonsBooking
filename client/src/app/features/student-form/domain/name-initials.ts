const MAX_INITIALS = 2;
const WHITESPACE = /\s+/;

export function nameInitials(name: string): string {
    const words = name
        .trim()
        .split(WHITESPACE)
        .filter(word => word.length > 0);
    const picked = words.length > MAX_INITIALS ? [words[0], words[words.length - 1]] : words;

    return picked.map(word => Array.from(word)[0].toLocaleUpperCase()).join('');
}
