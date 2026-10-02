import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { PrimeNG } from 'primeng/config';
import { Translation } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { PRIMENG_EN, PRIMENG_HE } from './primeng-translations';

export type AppLanguage = 'he' | 'en';
export type AppLocale = 'he-IL' | 'en-IL';

const STORAGE_KEY = 'app_lang';
const DOCUMENT_TITLE_KEY = 'shell.title';

const LOCALES: Record<AppLanguage, AppLocale> = {
  he: 'he-IL',
  en: 'en-IL',
};

const PRIMENG_TRANSLATIONS: Record<AppLanguage, Partial<Translation>> = {
  he: PRIMENG_HE,
  en: PRIMENG_EN,
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly primeng = inject(PrimeNG);
  private readonly title = inject(Title);

  readonly lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'he',
  );

  readonly locale = computed<AppLocale>(() => LOCALES[this.lang()]);

  readonly isRtl = computed(() => this.lang() === 'he');

  private readonly documentTitle = toSignal(this.transloco.selectTranslate(DOCUMENT_TITLE_KEY), {
    initialValue: '',
  });

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      this.transloco.setActiveLang(lang);
      this.applyPrimeNgTranslation(PRIMENG_TRANSLATIONS[lang]);
      document.documentElement.lang = lang;
      document.documentElement.dir = this.isRtl() ? 'rtl' : 'ltr';
    });

    effect(() => {
      const title = this.documentTitle();

      if (title) {
        this.title.setTitle(title);
      }
    });
  }

  toggle(): void {
    this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
  }

  use(lang: AppLanguage): void {
    this.lang.set(lang);
  }

  private applyPrimeNgTranslation(translation: Partial<Translation>): void {
    this.primeng.setTranslation({
      ...translation,
      aria: { ...this.primeng.translation.aria, ...translation.aria },
    });
  }
}
