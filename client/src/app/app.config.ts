import { ApplicationConfig, provideZonelessChangeDetection, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { AppPreset } from './theme/app-preset';
import { TranslocoHttpLoader } from './transloco-loader';
import { authInterceptor } from './core/auth.interceptor';
import { PRIMENG_HE } from './core/language.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    MessageService,
    providePrimeNG({
      translation: PRIMENG_HE,
      theme: {
        preset: AppPreset,
        options: {
          darkModeSelector: false,
          cssLayer: { name: 'primeng', order: 'primeng, app' },
        },
      },
    }),
    provideTransloco({
      config: {
        availableLangs: ['he', 'en'],
        defaultLang: 'he',
        fallbackLang: 'en',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
