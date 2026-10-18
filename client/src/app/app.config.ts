import { ApplicationConfig, provideZonelessChangeDetection, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding, withNavigationErrorHandler } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { AppPreset } from './theme/app-preset';
import { TranslocoHttpLoader } from './transloco-loader';
import { authInterceptor } from './core/auth.interceptor';
import { checkForNewVersionAfterNavigationError } from './core/pwa/check-for-new-version';
import { providePwa } from './core/pwa/provide-pwa';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withNavigationErrorHandler(checkForNewVersionAfterNavigationError),
    ),
    provideHttpClient(withInterceptors([authInterceptor])),
    providePwa(),
    MessageService,
    providePrimeNG({
      theme: {
        preset: AppPreset,
        options: {
          darkModeSelector: '.app-dark',
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
