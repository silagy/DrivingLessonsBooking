import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'drivecal.theme';
const DARK_CLASS = 'app-dark';
const DEVICE_QUERY = '(prefers-color-scheme: dark)';
const STATUS_BAR = { light: '#ffffff', dark: '#1B1D22' } as const;
const THEME_MODES: readonly ThemeMode[] = ['light', 'dark', 'system'];

function savedMode(): ThemeMode {
  const saved = localStorage.getItem(STORAGE_KEY);

  return THEME_MODES.find((mode) => mode === saved) ?? 'system';
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly deviceQuery = matchMedia(DEVICE_QUERY);
  private readonly prefersDark = signal(this.deviceQuery.matches);

  readonly mode = signal<ThemeMode>(savedMode());

  readonly isDark = computed(() => this.mode() === 'dark' || (this.mode() === 'system' && this.prefersDark()));

  constructor() {
    const onDeviceChange = (event: { matches: boolean }) => this.prefersDark.set(event.matches);
    this.deviceQuery.addEventListener('change', onDeviceChange);
    inject(DestroyRef).onDestroy(() => this.deviceQuery.removeEventListener('change', onDeviceChange));

    effect(() => {
      const isDark = this.isDark();
      const root = document.documentElement;
      localStorage.setItem(STORAGE_KEY, this.mode());
      root.classList.toggle(DARK_CLASS, isDark);
      root.style.colorScheme = isDark ? 'dark' : 'light';
      document
        .querySelector('meta[name="theme-color"]')
        ?.setAttribute('content', isDark ? STATUS_BAR.dark : STATUS_BAR.light);
    });
  }

  use(mode: ThemeMode): void {
    this.mode.set(mode);
  }
}
