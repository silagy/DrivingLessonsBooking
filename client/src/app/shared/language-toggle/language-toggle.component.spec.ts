import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { AppLanguage, LanguageService } from '../../core/language.service';
import { LanguageToggleComponent } from './language-toggle.component';

async function renderToggle(): Promise<ComponentFixture<LanguageToggleComponent>> {
    const lang = signal<AppLanguage>('he');
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: { shell: { language: 'Language' } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang, use: (next: AppLanguage) => lang.set(next) } },
        ],
    });
    const fixture = TestBed.createComponent(LanguageToggleComponent);
    await fixture.whenStable();

    return fixture;
}

function options(fixture: ComponentFixture<LanguageToggleComponent>): HTMLButtonElement[] {
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.lang__opt')];
}

describe('LanguageToggleComponent', () => {
    it('names the toggle group in the active language', async () => {
        //given
        const fixture = await renderToggle();

        //when
        const group = (fixture.nativeElement as HTMLElement).querySelector('[role=group]');

        //then
        expect(group?.getAttribute('aria-label')).toBe('Language');
    });

    it('marks each option with its own language and name', async () => {
        //given
        const fixture = await renderToggle();

        //when
        const buttons = options(fixture);

        //then
        expect(buttons.map(button => button.getAttribute('lang'))).toEqual(['en', 'he']);
        expect(buttons.map(button => button.getAttribute('aria-label'))).toEqual(['English', 'עברית']);
    });

    it('switches to the pressed language', async () => {
        //given
        const fixture = await renderToggle();

        //when
        options(fixture)[0].click();
        await fixture.whenStable();

        //then
        expect(options(fixture).map(button => button.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
    });
});
