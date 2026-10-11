import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { BrandLogoComponent } from './brand-logo.component';

async function renderLogo(inputs: Partial<Record<'showName' | 'showTagline', boolean>> = {}): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { brand: { appName: 'Slotline', tagline: 'Weekly lesson demand, in one link' } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });
    const fixture = TestBed.createComponent(BrandLogoComponent);
    Object.entries(inputs).forEach(([name, value]) => fixture.componentRef.setInput(name, value));
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('BrandLogoComponent', () => {
    it('draws the lane tile mark, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(mark?.getAttribute('viewBox')).toBe('0 0 40 40');
        expect(mark?.getAttribute('aria-hidden')).toBe('true');
        expect(mark?.querySelector('rect')?.getAttribute('rx')).toBe('10');
        expect(mark?.querySelector('path')?.getAttribute('d')).toBe('M20 9v5M20 17.5v5M20 26v5');
    });

    it('names the product in the active language', async () => {
        //given
        const logo = await renderLogo();

        //when
        const name = logo.querySelector('.brand__name');

        //then
        expect(name?.textContent?.trim()).toBe('Slotline');
    });

    it('shows no tagline unless asked', async () => {
        //given
        const logo = await renderLogo();

        //when
        const tagline = logo.querySelector('.brand__tagline');

        //then
        expect(tagline).toBeNull();
        expect(logo.querySelector('.brand--lockup')).toBeNull();
    });

    it('shows the tagline under the name in the lockup', async () => {
        //given
        const logo = await renderLogo({ showTagline: true });

        //when
        const tagline = logo.querySelector('.brand--lockup .brand__tagline');

        //then
        expect(tagline?.textContent?.trim()).toBe('Weekly lesson demand, in one link');
    });

    it('keeps the mark when the name is hidden', async () => {
        //given
        const logo = await renderLogo({ showName: false, showTagline: true });

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(mark).not.toBeNull();
        expect([logo.querySelector('.brand__name'), logo.querySelector('.brand__tagline')]).toEqual([null, null]);
    });
});
