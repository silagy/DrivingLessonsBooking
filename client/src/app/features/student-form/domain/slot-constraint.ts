export const SLOT_CONSTRAINT_MAX_LENGTH = 200;

export function toSlotConstraint(text: string): string | null {
    const trimmed = text.trim();

    return trimmed ? trimmed : null;
}
