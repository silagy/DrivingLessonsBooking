import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { LanguageService } from '../../core/language.service';
import { ThemeMode, ThemeService } from '../../core/theme.service';
import { AppearanceToggleComponent } from './appearance-toggle.component';

interface Rendered {
    fixture: ComponentFixture<AppearanceToggleComponent>;
    use: ReturnType<typeof vi.fn>;
}

async function render(mode: ThemeMode, rtl: boolean): Promise<Rendered> {
    const use = vi.fn();
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { shell: { appearance: { label: 'Appearance', light: 'Light', dark: 'Dark', system: 'System' } } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            providePrimeNG(),
            { provide: ThemeService, useValue: { mode: signal(mode), use } },
            { provide: LanguageService, useValue: { isRtl: signal(rtl) } },
        ],
    });
    const fixture = TestBed.createComponent(AppearanceToggleComponent);
    await fixture.whenStable();

    return { fixture, use };
}

function options(fixture: ComponentFixture<AppearanceToggleComponent>): HTMLElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.p-togglebutton')];
}

function press(fixture: ComponentFixture<AppearanceToggleComponent>, key: string): void {
    (document.activeElement as HTMLElement).dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
}

describe('AppearanceToggleComponent', () => {
    afterEach(() => document.body.replaceChildren());

    it('offers Light, Dark and System, in that order', async () => {
        //given
        const { fixture } = await render('system', false);

        //when
        const labels = options(fixture).map((option) => option.textContent?.trim());

        //then
        expect(labels).toEqual(['Light', 'Dark', 'System']);
    });

    it('shows the current mode as pressed', async () => {
        //given
        const { fixture } = await render('dark', false);

        //when
        const pressed = options(fixture).map((option) => option.getAttribute('aria-pressed'));

        //then
        expect(pressed).toEqual(['false', 'true', 'false']);
    });

    it('applies the picked mode', async () => {
        //given
        const { fixture, use } = await render('system', false);

        //when
        options(fixture)[0].click();
        await fixture.whenStable();

        //then
        expect(use.mock.calls).toEqual([['light']]);
    });

    it('keeps the current mode when it is picked again', async () => {
        //given
        const { fixture, use } = await render('dark', false);

        //when
        options(fixture)[1].click();
        await fixture.whenStable();

        //then
        expect(use).not.toHaveBeenCalled();
    });

    it('names the group', async () => {
        //given
        const { fixture } = await render('system', false);

        //when
        const group = (fixture.nativeElement as HTMLElement).querySelector('p-selectbutton')!;

        //then
        expect([group.getAttribute('role'), group.getAttribute('aria-label')]).toEqual(['group', 'Appearance']);
    });

    it('moves focus with the arrow keys, mirrored in Hebrew', async () => {
        //given
        const hebrew = await render('system', true);
        document.body.appendChild(hebrew.fixture.nativeElement);
        const hebrewOptions = options(hebrew.fixture);

        //when
        hebrewOptions[0].focus();
        press(hebrew.fixture, 'ArrowLeft');
        const afterLeft = document.activeElement;
        hebrewOptions[0].focus();
        press(hebrew.fixture, 'ArrowRight');
        const afterRight = document.activeElement;

        //then
        expect([afterLeft, afterRight]).toEqual([hebrewOptions[1], hebrewOptions[2]]);
    });

    it('moves focus to the right in English', async () => {
        //given
        const { fixture } = await render('system', false);
        document.body.appendChild(fixture.nativeElement);
        const englishOptions = options(fixture);

        //when
        englishOptions[0].focus();
        press(fixture, 'ArrowRight');

        //then
        expect(document.activeElement).toBe(englishOptions[1]);
    });

    it('jumps to the first and last option with Home and End', async () => {
        //given
        const { fixture } = await render('system', false);
        document.body.appendChild(fixture.nativeElement);
        const all = options(fixture);

        //when
        all[1].focus();
        press(fixture, 'End');
        const afterEnd = document.activeElement;
        press(fixture, 'Home');

        //then
        expect([afterEnd, document.activeElement]).toEqual([all[2], all[0]]);
    });

    it('moves focus without changing the mode', async () => {
        //given
        const { fixture, use } = await render('system', true);
        document.body.appendChild(fixture.nativeElement);

        //when
        options(fixture)[0].focus();
        press(fixture, 'ArrowLeft');
        press(fixture, 'ArrowRight');
        press(fixture, 'End');

        //then
        expect(use).not.toHaveBeenCalled();
    });
});
