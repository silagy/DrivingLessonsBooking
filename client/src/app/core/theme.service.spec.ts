import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

type DeviceListener = (event: { matches: boolean }) => void;

let deviceListeners: DeviceListener[] = [];
let deviceIsDark = false;

function withSavedTheme(saved: string | null): void {
    const storage = new Map<string, string>(saved === null ? [] : [['drivecal.theme', saved]]);
    Object.defineProperty(globalThis, 'localStorage', {
        configurable: true,
        value: {
            getItem: (key: string) => storage.get(key) ?? null,
            setItem: (key: string, value: string) => void storage.set(key, value),
            removeItem: (key: string) => void storage.delete(key),
            clear: () => storage.clear(),
        } as Storage,
    });
}

function withDevice(dark: boolean): void {
    deviceIsDark = dark;
    deviceListeners = [];
    Object.defineProperty(globalThis, 'matchMedia', {
        configurable: true,
        value: (query: string) => ({
            media: query,
            get matches() {
                return deviceIsDark;
            },
            addEventListener: (_type: string, listener: DeviceListener) => void deviceListeners.push(listener),
            removeEventListener: (_type: string, listener: DeviceListener) => {
                deviceListeners = deviceListeners.filter((l) => l !== listener);
            },
        }),
    });
}

function changeDevice(dark: boolean): void {
    deviceIsDark = dark;
    deviceListeners.forEach((listener) => listener({ matches: dark }));
    TestBed.tick();
}

function startService(): ThemeService {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    const theme = TestBed.inject(ThemeService);
    TestBed.tick();

    return theme;
}

function use(theme: ThemeService, mode: 'light' | 'dark' | 'system'): void {
    theme.use(mode);
    TestBed.tick();
}

function statusBar(): string | null {
    return document.querySelector('meta[name="theme-color"]')!.getAttribute('content');
}

describe('ThemeService', () => {
    beforeEach(() => {
        document.documentElement.className = '';
        document.documentElement.style.colorScheme = '';
        document.head.querySelectorAll('meta[name="theme-color"]').forEach((meta) => meta.remove());
        const meta = document.createElement('meta');
        meta.name = 'theme-color';
        meta.content = '#ffffff';
        document.head.appendChild(meta);
    });

    it('starts in System when nothing is saved', () => {
        //given
        withSavedTheme(null);
        withDevice(false);

        //when
        const theme = startService();

        //then
        expect([theme.mode(), theme.isDark()]).toEqual(['system', false]);
    });

    it('treats an unknown saved value as System', () => {
        //given
        withSavedTheme('Dark');
        withDevice(false);

        //when
        const theme = startService();

        //then
        expect(theme.mode()).toBe('system');
    });

    it('restores the saved mode', () => {
        //given
        withSavedTheme('dark');
        withDevice(false);

        //when
        const theme = startService();

        //then
        expect([theme.mode(), theme.isDark()]).toEqual(['dark', true]);
    });

    it('resolves System from the device', () => {
        //given
        withSavedTheme(null);
        withDevice(true);

        //when
        startService();

        //then
        expect(document.documentElement.classList.contains('app-dark')).toBe(true);
        expect(document.documentElement.style.colorScheme).toBe('dark');
    });

    it('saves the picked mode and applies it', () => {
        //given
        withSavedTheme('dark');
        withDevice(false);
        const theme = startService();

        //when
        use(theme, 'light');

        //then
        expect(localStorage.getItem('drivecal.theme')).toBe('light');
        expect(document.documentElement.classList.contains('app-dark')).toBe(false);
        expect(document.documentElement.style.colorScheme).toBe('light');
    });

    it('follows the device when it changes, in System only', () => {
        //given
        withSavedTheme(null);
        withDevice(false);
        const theme = startService();

        //when
        changeDevice(true);
        const darkInSystem = document.documentElement.classList.contains('app-dark');
        changeDevice(false);
        use(theme, 'light');
        changeDevice(true);

        //then
        expect([darkInSystem, document.documentElement.classList.contains('app-dark')]).toEqual([true, false]);
    });

    it('colours the status bar like the top bar', () => {
        //given
        withSavedTheme('dark');
        withDevice(false);
        const theme = startService();
        const darkBar = statusBar();

        //when
        use(theme, 'light');

        //then
        expect([darkBar, statusBar()]).toEqual(['#1B1D22', '#ffffff']);
    });
});
