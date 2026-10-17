import { DOCUMENT, EnvironmentProviders, inject, provideAppInitializer } from '@angular/core';
import { holdBackInstallPromptOnStudentForm } from './hold-back-install-prompt';
import { NewVersionStore } from './new-version.store';
import { registerServiceWorker } from './register-service-worker';

export function providePwa(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const document = inject(DOCUMENT);
    const view = document.defaultView;
    const newVersion = inject(NewVersionStore);

    if (!view) {
      return;
    }

    holdBackInstallPromptOnStudentForm(view);
    void registerServiceWorker('serviceWorker' in view.navigator ? view.navigator.serviceWorker : undefined);
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        void newVersion.check();
      }
    });
  });
}
