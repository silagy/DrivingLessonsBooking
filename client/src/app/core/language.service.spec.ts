import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { PrimeNG } from 'primeng/config';
import { LanguageService } from './language.service';

const TRANSLATIONS = {
    en: { shell: { title: 'Driving Lessons Planner' } },
    he: { shell: { title: 'מערכת תכנון שיעורי נהיגה' } },
};

function withSavedLanguage(saved: string | null): void {
    const storage = new Map<string, string>(saved ? [['app_lang', saved]] : []);
    Object.defineProperty(globalThis, 'localStorage', {
        configurable: true,
        value: {
            getItem: (key: string) => storage.get(key) ?? null,
            setItem: (key: string, value: string) => void storage.set(key, value),
            removeItem: (key: string) => void storage.delete(key),
            clear: () => storage.clear(),
        } as Storage,
    });
}

function startService(): LanguageService {
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: TRANSLATIONS,
                translocoConfig: { availableLangs: ['he', 'en'], defaultLang: 'he' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });
    const language = TestBed.inject(LanguageService);
    TestBed.tick();

    return language;
}

function switchTo(language: LanguageService, lang: 'he' | 'en'): void {
    language.use(lang);
    TestBed.tick();
}

function primeNgLabels() {
    return TestBed.inject(PrimeNG).translation;
}

describe('LanguageService', () => {
    it('starts in Hebrew, right to left, when nothing is saved', () => {
        //given
        withSavedLanguage(null);

        //when
        const language = startService();

        //then
        expect([language.lang(), language.locale(), language.isRtl()]).toEqual(['he', 'he-IL', true]);
        expect([document.documentElement.lang, document.documentElement.dir]).toEqual(['he', 'rtl']);
    });

    it('starts in Hebrew when the saved language is not one it supports', () => {
        //given
        withSavedLanguage('fr');

        //when
        const language = startService();

        //then
        expect([language.lang(), language.locale(), document.documentElement.dir]).toEqual(['he', 'he-IL', 'rtl']);
        expect(primeNgLabels().aria?.close).toBe('סגירה');
    });

    it('starts in the saved language', () => {
        //given
        withSavedLanguage('en');

        //when
        const language = startService();

        //then
        expect([language.locale(), language.isRtl(), document.documentElement.dir]).toEqual(['en-IL', false, 'ltr']);
        expect(document.title).toBe('Driving Lessons Planner');
        expect(primeNgLabels().aria?.close).toBe('Close');
    });

    it('switches to Israeli English, left to right', () => {
        //given
        withSavedLanguage(null);
        const language = startService();

        //when
        switchTo(language, 'en');

        //then
        expect([language.lang(), language.locale(), language.isRtl()]).toEqual(['en', 'en-IL', false]);
        expect([document.documentElement.lang, document.documentElement.dir]).toEqual(['en', 'ltr']);
    });

    it('gives PrimeNG its labels in Hebrew', () => {
        //given
        withSavedLanguage(null);

        //when
        startService();

        //then
        const labels = primeNgLabels();
        expect([labels.chooseDate, labels.nextMonth, labels.emptyMessage, labels.aria?.close])
            .toEqual(['בחירת תאריך', 'החודש הבא', 'לא נמצאו תוצאות', 'סגירה']);
    });

    it('restores every PrimeNG label in English after Hebrew', () => {
        //given
        withSavedLanguage(null);
        const language = startService();

        //when
        switchTo(language, 'en');

        //then
        const labels = primeNgLabels();
        expect([labels.chooseDate, labels.nextMonth, labels.emptyMessage, labels.aria?.close])
            .toEqual(['Choose Date', 'Next Month', 'No results found', 'Close']);
        expect(labels.aria?.moveUp).toBe('Move Up');
    });

    it('names the browser tab in the selected language', () => {
        //given
        withSavedLanguage(null);
        const language = startService();
        const hebrewTitle = document.title;

        //when
        switchTo(language, 'en');

        //then
        expect([hebrewTitle, document.title]).toEqual(['מערכת תכנון שיעורי נהיגה', 'Driving Lessons Planner']);
    });
});
