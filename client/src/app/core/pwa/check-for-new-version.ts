import { inject } from '@angular/core';
import { NewVersionStore } from './new-version.store';

export function checkForNewVersionAfterNavigationError(): void {
  void inject(NewVersionStore).check();
}
