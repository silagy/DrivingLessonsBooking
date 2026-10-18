import { Directive, ElementRef, inject, output } from '@angular/core';

export const SHEET_DISMISS_DISTANCE_PX = 80;

const SHEET_SELECTOR = '.p-dialog';
const DRAGGING_CLASS = 'sheet-dragging';
const CONTROL_SELECTOR = 'button, a[href]';

export function shouldDismissSheet(distance: number): boolean {
    return distance >= SHEET_DISMISS_DISTANCE_PX;
}

@Directive({
    selector: '[appSheetDragClose]',
    host: {
        '[style.touch-action]': '"none"',
        '(pointerdown)': 'onPointerDown($event)',
        '(pointermove)': 'onPointerMove($event)',
        '(pointerup)': 'onPointerUp($event)',
        '(pointercancel)': 'reset()',
    },
})
export class SheetDragCloseDirective {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    readonly dismissed = output<void>();

    private startY: number | null = null;
    private sheet: HTMLElement | null = null;

    protected onPointerDown(event: PointerEvent): void {
        const startsOnControl = (event.target as Element).closest(CONTROL_SELECTOR) !== null;

        if (startsOnControl) {
            return;
        }

        this.startY = event.clientY;
        this.sheet = this.host.nativeElement.closest<HTMLElement>(SHEET_SELECTOR);
        this.host.nativeElement.setPointerCapture(event.pointerId);
    }

    protected onPointerMove(event: PointerEvent): void {
        const distance = this.distanceTo(event);

        if (!this.sheet || distance <= 0) {
            return;
        }

        this.sheet.classList.add(DRAGGING_CLASS);
        this.sheet.style.transform = `translateY(${distance}px)`;
    }

    protected onPointerUp(event: PointerEvent): void {
        const distance = this.distanceTo(event);

        if (!shouldDismissSheet(distance)) {
            this.reset();

            return;
        }

        this.sheet?.classList.remove(DRAGGING_CLASS);
        this.startY = null;
        this.sheet = null;
        this.dismissed.emit();
    }

    protected reset(): void {
        this.sheet?.classList.remove(DRAGGING_CLASS);
        this.sheet?.style.removeProperty('transform');
        this.startY = null;
        this.sheet = null;
    }

    private distanceTo(event: PointerEvent): number {
        return this.startY === null ? 0 : event.clientY - this.startY;
    }
}
