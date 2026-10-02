import { TestBed } from '@angular/core/testing';
import { TRANSLOCO_LOADER } from '@jsverse/transloco';
import { PrimeNG } from 'primeng/config';
import { of } from 'rxjs';
import { appConfig } from './app.config';
import { LanguageService } from './core/language.service';

describe('appConfig', () => {
    it('keeps PrimeNG\'s own accessibility labels next to the translated ones', () => {
        //given
        const storage = new Map<string, string>();
        Object.defineProperty(globalThis, 'localStorage', {
            configurable: true,
            value: {
                getItem: (key: string) => storage.get(key) ?? null,
                setItem: (key: string, value: string) => void storage.set(key, value),
                removeItem: (key: string) => void storage.delete(key),
                clear: () => storage.clear(),
            } as Storage,
        });
        TestBed.configureTestingModule({
            providers: [...appConfig.providers, { provide: TRANSLOCO_LOADER, useValue: { getTranslation: () => of({}) } }],
        });

        //when
        TestBed.inject(LanguageService);
        TestBed.tick();

        //then
        const aria = TestBed.inject(PrimeNG).translation.aria;
        expect([aria?.close, aria?.moveUp]).toEqual(['סגירה', 'Move Up']);
    });
});
