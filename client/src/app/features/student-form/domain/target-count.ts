export const MIN_TARGET_COUNT = 1;

export function missingPickCount(targetCount: number, pickCount: number): number {
    return Math.max(0, targetCount - pickCount);
}
