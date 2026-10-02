export interface RtlPopoverPlacement {
    insetInlineStart: number;
    arrowInset: number;
}

export function rtlPopoverPlacement(
    triggerRight: number,
    panelWidth: number,
    containerRight: number,
): RtlPopoverPlacement {
    const flushWithTrigger = containerRight - triggerRight;
    const insideContainer = containerRight - panelWidth;
    const insetInlineStart = Math.max(0, Math.min(flushWithTrigger, insideContainer));
    const panelRight = containerRight - insetInlineStart;

    return { insetInlineStart, arrowInset: Math.max(0, panelRight - triggerRight) };
}
