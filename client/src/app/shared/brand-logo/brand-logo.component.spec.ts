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
    it('draws the calendar with a checked day, hidden from assistive technology', async () => {
        //given
        const logo = await renderLogo();

        //when
        const mark = logo.querySelector('svg.brand__mark');

        //then
        expect(attributes(mark, ['viewBox', 'aria-hidden'])).toEqual(['0 0 40 40', 'true']);
        expect(attributes(mark?.querySelector('rect.brand__calendar'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['13', '5', '25', '24', '5']);
        expect(attributes(mark?.querySelector('rect.brand__page'), ['x', 'y', 'width', 'height', 'rx']))
            .toEqual(['15.5', '11', '20', '15.5', '2.5']);
        expect(attributes(mark?.querySelector('path.brand__rings'), ['d', 'stroke-width', 'stroke-linecap']))
            .toEqual(['M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8', '2.4', 'round']);
        expect(attributes(mark?.querySelector('path.brand__check'), ['d', 'stroke-width', 'stroke-linecap', 'stroke-linejoin']))
            .toEqual(['M26.6 17.6l2.4 2.4 4.2-4.6', '2.3', 'round', 'round']);
        expect(attributes(mark?.querySelector('circle.brand__halo'), ['cx', 'cy', 'r'])).toEqual(['15', '25.5', '14.2']);
    });

    it('draws the steering wheel in its own 40-unit box', async () => {
        //given
        const logo = await renderLogo();

        //when
        const wheel = logo.querySelector('svg.brand__mark svg.brand__wheel');

        //then
        expect(attributes(wheel, ['x', 'y', 'width', 'height', 'viewBox'])).toEqual(['2', '12.5', '26', '26', '0 0 40 40']);
        expect(attributes(wheel?.querySelector('circle.brand__disc'), ['cx', 'cy', 'r'])).toEqual(['20', '20', '19']);
        expect(Array.from(wheel?.querySelectorAll('path.brand__window') ?? [], window => window.getAttribute('d'))).toEqual([
            'M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z',
            'M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z',
            'M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z',
        ]);
        expect(attributes(wheel?.querySelector('circle.brand__hub'), ['cx', 'cy', 'r', 'stroke-width']))
            .toEqual(['20', '21.5', '4.6', '2.2']);
    });

    it('paints the halo between the calendar and the wheel', async () => {
        //given
        const logo = await renderLogo();

        //when
        const parts = Array.from(logo.querySelector('svg.brand__mark')?.children ?? [], part => part.getAttribute('class'));

        //then
        expect(parts).toEqual(['brand__calendar', 'brand__page', 'brand__rings', 'brand__check', 'brand__halo', 'brand__wheel']);
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
