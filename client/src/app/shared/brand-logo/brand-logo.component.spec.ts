import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { BrandLogoComponent } from './brand-logo.component';

const BRAND = {
    appName: 'DriveCal',
    nameLead: 'Drive',
    nameAccent: 'Cal',
    tagline: 'Weekly lesson demand, in one link',
};

async function renderLogo(inputs: Partial<Record<'showName' | 'showTagline', boolean>> = {}): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { brand: BRAND } },
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

function attributes(element: Element | null | undefined, names: string[]): (string | null)[] {
    return names.map(name => element?.getAttribute(name) ?? null);
}

describe('BrandLogoComponent', () => {
    it('draws the solid calendar mark, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(attributes(mark, ['viewBox', 'aria-hidden'])).toEqual(['0 0 40 40', 'true']);
        expect(attributes(mark?.querySelector('rect.brand__page'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['1', '5.5', '38', '33.5', '10']);
        expect(attributes(mark?.querySelector('path.brand__rings'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M12.5 1.8v7M27.5 1.8v7', '3.4', 'round']);
        expect(Array.from(mark?.querySelectorAll('circle.brand__hole') ?? [], hole => attributes(hole, ['cx', 'cy', 'r'])))
            .toEqual([['12.5', '9.5', '1.6'], ['27.5', '9.5', '1.6']]);
        expect(attributes(mark?.querySelector('path.brand__lane'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M20 15.5v4M20 23.5v4M20 31.5v3', '3.2', 'round']);
    });

    it('leaves every color of the mark to the stylesheet', async () => {
        //given
        const logo = await renderLogo();

        //when
        const markup = logo.querySelector('svg.brand__mark')?.outerHTML ?? '';

        //then
        expect(markup).not.toMatch(/#[0-9a-f]{3,6}\b|\bfill="|\bstroke="/i);
    });

    it('names the product in two tones, as one word', async () => {
        //given
        const logo = await renderLogo();

        //when
        const name = logo.querySelector('.brand__name');

        //then
        expect(name?.textContent).toBe('DriveCal');
        expect(name?.querySelector('.brand__accent')?.textContent).toBe('Cal');
    });

    it('announces the two-tone name to assistive technology as the single product name', async () => {
        //given
        const logo = await renderLogo();

        //when
        const name = logo.querySelector('.brand__name');

        //then
        expect(attributes(name, ['role', 'aria-label'])).toEqual(['img', 'DriveCal']);
        expect(Array.from(name?.children ?? [], child => child.getAttribute('aria-hidden'))).toEqual(['true']);
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
