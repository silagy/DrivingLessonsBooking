import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

export const TranslationsRevision = '2026-10-17';

@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(lang: string) {
    const params = { revision: TranslationsRevision };

    return this.http.get<Translation>(`/i18n/${lang}.json`, { params });
  }
}
