import { effect, inject, Injectable, signal } from '@angular/core';
import { PrimeNG } from 'primeng/config';
import { Translation } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';

export type AppLanguage = 'he' | 'en';
const STORAGE_KEY = 'app_lang';

export const PRIMENG_HE: Partial<Translation> = {
  firstDayOfWeek: 0,
  dayNames: ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'],
  dayNamesShort: ['א׳', 'ב׳', 'ג׳', 'ד׳', 'ה׳', 'ו׳', 'ש׳'],
  dayNamesMin: ['א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ש'],
  monthNames: [
    'ינואר', 'פברואר', 'מרץ', 'אפריל', 'מאי', 'יוני',
    'יולי', 'אוגוסט', 'ספטמבר', 'אוקטובר', 'נובמבר', 'דצמבר',
  ],
  monthNamesShort: [
    'ינו', 'פבר', 'מרץ', 'אפר', 'מאי', 'יונ',
    'יול', 'אוג', 'ספט', 'אוק', 'נוב', 'דצמ',
  ],
  today: 'היום',
  clear: 'נקה',
};

export const PRIMENG_EN: Partial<Translation> = {
  firstDayOfWeek: 0,
  dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
  dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
  dayNamesMin: ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'],
  monthNames: [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ],
  monthNamesShort: [
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
  ],
  today: 'Today',
  clear: 'Clear',
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly primeng = inject(PrimeNG);

  readonly lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'he',
  );

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      this.transloco.setActiveLang(lang);
      this.primeng.setTranslation(lang === 'he' ? PRIMENG_HE : PRIMENG_EN);
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'he' ? 'rtl' : 'ltr';
    });
  }

  toggle(): void {
    this.lang.update((l) => (l === 'he' ? 'en' : 'he'));
  }

  use(lang: AppLanguage): void {
    this.lang.set(lang);
  }
}
