import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { WeekGridLegendComponent } from './week-grid-legend.component';

async function render(firstLabelKey?: string): Promise<ComponentFixture<WeekGridLegendComponent>> {
    TestBed.configureTestingModule({
        imports: [
            WeekGridLegendComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(WeekGridLegendComponent);
    if (firstLabelKey) {
        fixture.componentRef.setInput('firstLabelKey', firstLabelKey);
    }
    await fixture.whenStable();

    return fixture;
}

function items(fixture: ComponentFixture<WeekGridLegendComponent>): string[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll('.week-grid-legend__item')].map(
        (item) => item.textContent?.trim() ?? '',
    );
}

describe('WeekGridLegendComponent', () => {
    it('says the open label first by default', async () => {
        //when
        const fixture = await render();

        //then
        expect(items(fixture)[0]).toBe('weekGrid.legend.open');
    });

    it('says the label it is given first', async () => {
        //when
        const fixture = await render('publications.dashboard.legendRequests');

        //then
        expect(items(fixture)[0]).toBe('publications.dashboard.legendRequests');
    });

    it('names the slots that never have lessons', async () => {
        //when
        const fixture = await render();

        //then
        expect(items(fixture).at(-1)).toBe('weekGrid.legend.noLessons');
    });
});
