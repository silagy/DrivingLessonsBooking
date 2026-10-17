import { computed, DOCUMENT, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AppPageApiService } from './app-page-api.service';

const MAIN_BUNDLE_SELECTOR = 'script[src^="main-"]';

function mainBundleOf(page: Document): string | null {
  return page.querySelector(MAIN_BUNDLE_SELECTOR)?.getAttribute('src') ?? null;
}

@Injectable({ providedIn: 'root' })
export class NewVersionStore {
  private readonly api = inject(AppPageApiService);
  private readonly document = inject(DOCUMENT);

  private readonly ready = signal(false);
  private readonly checking = signal(false);
  private readonly dismissed = signal(false);

  readonly isReady = this.ready.asReadonly();
  readonly isShown = computed(() => this.ready() && !this.dismissed());

  async check(): Promise<void> {
    if (this.ready()) {
      this.dismissed.set(false);
      return;
    }

    if (this.checking()) {
      return;
    }

    this.checking.set(true);

    try {
      const servedPage = await firstValueFrom(this.api.getAppPage());
      const servedBundle = mainBundleOf(new DOMParser().parseFromString(servedPage, 'text/html'));
      const runningBundle = mainBundleOf(this.document);

      this.ready.set(servedBundle !== null && runningBundle !== null && servedBundle !== runningBundle);
    } catch {
      return;
    } finally {
      this.checking.set(false);
    }
  }

  dismiss(): void {
    this.dismissed.set(true);
  }

  reload(): void {
    this.document.location.reload();
  }
}
