import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DayOfWeek } from '../../../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';
import { SlotCountCellComponent } from './slot-count-cell.component';

interface CellOptions {
    requestCount: number;
    slotState: SlotState;
    selected: boolean;
}

async function render(options: CellOptions): Promise<ComponentFixture<SlotCountCellComponent>> {
    TestBed.configureTestingModule({
        imports: [
            SlotCountCellComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(SlotCountCellComponent);
    fixture.componentRef.setInput('requestCount', options.requestCount);
    fixture.componentRef.setInput('slotState', options.slotState);
    fixture.componentRef.setInput('day', DayOfWeek.wednesday);
    fixture.componentRef.setInput('window', SlotWindow.afternoon);
    fixture.componentRef.setInput('selected', options.selected);
    await fixture.whenStable();

    return fixture;
}

function host(fixture: ComponentFixture<SlotCountCellComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function button(fixture: ComponentFixture<SlotCountCellComponent>): HTMLButtonElement | null {
    return host(fixture).querySelector('button.slot-count');
}

describe('SlotCountCellComponent', () => {
    it('opens the students of a slot with requests', async () => {
        //given
        const fixture = await render({ requestCount: 3, slotState: SlotState.open, selected: false });
        const opened: HTMLElement[] = [];
        fixture.componentInstance.opened.subscribe((element) => opened.push(element));

        //when
        button(fixture)?.click();

        //then
        expect(button(fixture)).not.toBeNull();
        expect(opened).toEqual([button(fixture)]);
    });

    it('does nothing for a slot nobody requested', async () => {
        //when
        const fixture = await render({ requestCount: 0, slotState: SlotState.open, selected: false });

        //then
        expect(host(fixture).querySelector('button')).toBeNull();
        expect(host(fixture).querySelector('.slot-count__value')?.textContent?.trim()).toBe('0');
    });

    it('does nothing for a blocked slot, even with requests', async () => {
        //when
        const fixture = await render({ requestCount: 3, slotState: SlotState.unavailable, selected: false });

        //then
        expect(host(fixture).querySelector('button')).toBeNull();
        expect(host(fixture).querySelector('.slot-count--blocked')?.textContent).toContain(
            'weekGrid.legend.unavailable',
        );
    });

    it('names the slot for screen readers', async () => {
        //when
        const fixture = await render({ requestCount: 3, slotState: SlotState.open, selected: false });

        //then
        expect(button(fixture)?.getAttribute('aria-label')).toBe('publications.slotRequests.openAria');
        expect(button(fixture)?.getAttribute('aria-haspopup')).toBe('dialog');
    });

    it('names a slot with one request in the singular', async () => {
        //when
        const fixture = await render({ requestCount: 1, slotState: SlotState.open, selected: false });

        //then
        expect(button(fixture)?.getAttribute('aria-label')).toBe('publications.slotRequests.openAriaOne');
    });

    it('marks the slot whose students are showing', async () => {
        //when
        const selected = await render({ requestCount: 3, slotState: SlotState.open, selected: true });

        //then
        expect(button(selected)?.classList).toContain('slot-count--selected');
        expect(button(selected)?.getAttribute('aria-expanded')).toBe('true');
    });

    it('leaves the other slots unmarked', async () => {
        //when
        const fixture = await render({ requestCount: 3, slotState: SlotState.open, selected: false });

        //then
        expect(button(fixture)?.classList).not.toContain('slot-count--selected');
        expect(button(fixture)?.getAttribute('aria-expanded')).toBe('false');
    });
});
