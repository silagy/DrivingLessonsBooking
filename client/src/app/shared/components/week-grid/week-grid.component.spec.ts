import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/language.service';
import { WeekGridCell } from '../../models/week-grid-cell';
import { WeekGridComponent } from './week-grid.component';

@Component({
    imports: [WeekGridComponent],
    template: `
        <app-week-grid [cells]="cells" weekStart="2026-10-04">
            <ng-template let-cell>{{ cell.window }}</ng-template>
        </app-week-grid>
    `,
})
class WeekGridHost {
    readonly cells: WeekGridCell[] = [];
}

describe('WeekGridComponent', () => {
    it('dates each day day-first in Israeli English', async () => {
        //given
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

        //when
        const fixture = TestBed.createComponent(WeekGridHost);
        await fixture.whenStable();

        //then
        const dates = [...(fixture.nativeElement as HTMLElement).querySelectorAll('.week-grid__day-date')]
            .map(date => date.textContent?.trim());
        expect(dates).toEqual(['04/10', '05/10', '06/10', '07/10', '08/10', '09/10']);
    });
});
