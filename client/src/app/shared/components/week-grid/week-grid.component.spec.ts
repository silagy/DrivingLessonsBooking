import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/language.service';
import { SlotWindow } from '../../models/slot-window.enum';
import { WeekGridCell } from '../../models/week-grid-cell';
import { WeekGridComponent } from './week-grid.component';

@Component({
    imports: [WeekGridComponent],
    template: `
        <app-week-grid [cells]="cells" weekStart="2026-10-04" [windowTimes]="windowTimes">
            <ng-template let-cell>{{ cell.window }}</ng-template>
        </app-week-grid>
    `,
})
class WeekGridHost {
    readonly cells: WeekGridCell[] = [];
    readonly windowTimes = { [SlotWindow.morning]: '07:00-12:00' };
}

async function render(): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            WeekGridHost,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
        ],
    });

    const fixture = TestBed.createComponent(WeekGridHost);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('WeekGridComponent', () => {
    it('dates each day day-first in Israeli English', async () => {
        //given
        const grid = await render();

        //when
        const dates = [...grid.querySelectorAll('.week-grid__day-date')].map((date) => date.textContent?.trim());

        //then
        expect(dates).toEqual(['04/10', '05/10', '06/10', '07/10', '08/10', '09/10']);
    });

    it('renders a short name for each day', async () => {
        //given
        const grid = await render();

        //when
        const names = [...grid.querySelectorAll('.week-grid__day-name--short')].map((name) => name.textContent?.trim());

        //then
        expect(names).toEqual([
            'weekGrid.daysShort.sunday',
            'weekGrid.daysShort.monday',
            'weekGrid.daysShort.tuesday',
            'weekGrid.daysShort.wednesday',
            'weekGrid.daysShort.thursday',
            'weekGrid.daysShort.friday',
        ]);
    });

    it('renders a compact time next to each window time', async () => {
        //given
        const grid = await render();

        //when
        const compact = grid.querySelector('.week-grid__window-time--compact')?.textContent?.trim();

        //then
        expect(compact).toBe('07-12');
    });
});
