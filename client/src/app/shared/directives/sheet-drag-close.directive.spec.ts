import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SheetDragCloseDirective } from './sheet-drag-close.directive';

@Component({
    imports: [SheetDragCloseDirective],
    template: `
        <div class="p-dialog">
            <div class="handle" appSheetDragClose (dismissed)="dismissedCount = dismissedCount + 1">
                <button type="button" class="cross">x</button>
            </div>
        </div>
    `,
})
class SheetHost {
    dismissedCount = 0;
}

async function render(): Promise<ComponentFixture<SheetHost>> {
    TestBed.configureTestingModule({
        imports: [SheetHost],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(SheetHost);
    await fixture.whenStable();
    const handle = (fixture.nativeElement as HTMLElement).querySelector('.handle') as HTMLElement;
    handle.setPointerCapture = () => undefined;

    return fixture;
}

function pointer(fixture: ComponentFixture<SheetHost>, type: string, clientY: number): void {
    const handle = (fixture.nativeElement as HTMLElement).querySelector('.handle') as HTMLElement;
    handle.dispatchEvent(new MouseEvent(type, { clientY, bubbles: true }));
}

function sheet(fixture: ComponentFixture<SheetHost>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('.p-dialog') as HTMLElement;
}

describe('SheetDragCloseDirective', () => {
    it('closes the sheet when dragged down far enough', async () => {
        //given
        const fixture = await render();

        //when
        pointer(fixture, 'pointerdown', 100);
        pointer(fixture, 'pointermove', 190);
        pointer(fixture, 'pointerup', 190);

        //then
        expect(fixture.componentInstance.dismissedCount).toBe(1);
    });

    it('springs back when let go early', async () => {
        //given
        const fixture = await render();

        //when
        pointer(fixture, 'pointerdown', 100);
        pointer(fixture, 'pointermove', 150);
        pointer(fixture, 'pointerup', 150);

        //then
        expect(fixture.componentInstance.dismissedCount).toBe(0);
        expect(sheet(fixture).style.transform).toBe('');
        expect(sheet(fixture).classList).not.toContain('sheet-dragging');
    });

    it('ignores dragging up', async () => {
        //given
        const fixture = await render();

        //when
        pointer(fixture, 'pointerdown', 300);
        pointer(fixture, 'pointermove', 100);

        //then
        expect(sheet(fixture).style.transform).toBe('');
        pointer(fixture, 'pointerup', 100);
        expect(fixture.componentInstance.dismissedCount).toBe(0);
    });

    it('moves the sheet with the finger', async () => {
        //given
        const fixture = await render();

        //when
        pointer(fixture, 'pointerdown', 100);
        pointer(fixture, 'pointermove', 140);

        //then
        expect(sheet(fixture).style.transform).toBe('translateY(40px)');
        expect(sheet(fixture).classList).toContain('sheet-dragging');
    });

    it('leaves a press on a button in the header to the button', async () => {
        //given
        const fixture = await render();
        const cross = (fixture.nativeElement as HTMLElement).querySelector('.cross') as HTMLElement;

        //when
        cross.dispatchEvent(new MouseEvent('pointerdown', { clientY: 100, bubbles: true }));
        pointer(fixture, 'pointermove', 200);
        pointer(fixture, 'pointerup', 200);

        //then
        expect(sheet(fixture).style.transform).toBe('');
        expect(fixture.componentInstance.dismissedCount).toBe(0);
    });

    it('ignores moves that are not part of a drag', async () => {
        //given
        const fixture = await render();

        //when
        pointer(fixture, 'pointermove', 400);
        pointer(fixture, 'pointerup', 400);

        //then
        expect(sheet(fixture).style.transform).toBe('');
        expect(fixture.componentInstance.dismissedCount).toBe(0);
    });
});
