import { DOCUMENT, EnvironmentProviders, inject, provideAppInitializer } from '@angular/core';
import { holdBackInstallPromptOnStudentForm } from './hold-back-install-prompt';
import { registerServiceWorker } from './register-service-worker';

export function providePwa(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const view = inject(DOCUMENT).defaultView;

    if (!view) {
      return;
    }

    holdBackInstallPromptOnStudentForm(view);
    void registerServiceWorker('serviceWorker' in view.navigator ? view.navigator.serviceWorker : undefined);
  });
}
