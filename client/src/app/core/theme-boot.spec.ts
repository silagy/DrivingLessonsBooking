import { readFileSync } from 'node:fs';

function page(): string {
    return readFileSync('src/index.html', 'utf8');
}

function bootScript(): string {
    const head = page().split('</head>')[0];
    const script = head.match(/<script>([\s\S]*?)<\/script>/);

    if (!script) {
        throw new Error('index.html has no inline <script> in <head>');
    }

    return script[1];
}

function storage(saved: string | null): Storage {
    return { getItem: () => saved } as unknown as Storage;
}

function throwingStorage(): Storage {
    return {
        getItem: () => {
            throw new Error('storage is unavailable');
        },
    } as unknown as Storage;
}

function device(dark: boolean): (query: string) => { matches: boolean } {
    return () => ({ matches: dark });
}

function boot(localStorage: Storage, matchMedia: (query: string) => { matches: boolean }): void {
    new Function('localStorage', 'matchMedia', bootScript())(localStorage, matchMedia);
}

function isDark(): boolean {
    return document.documentElement.classList.contains('app-dark');
}

function statusBar(): string | null {
    return document.querySelector('meta[name="theme-color"]')!.getAttribute('content');
}

describe('theme boot script', () => {
    beforeEach(() => {
        document.documentElement.className = '';
        document.documentElement.style.colorScheme = '';
        document.head.querySelectorAll('meta[name="theme-color"]').forEach((meta) => meta.remove());
        const meta = document.createElement('meta');
        meta.name = 'theme-color';
        meta.content = '#ffffff';
        document.head.appendChild(meta);
    });

    it('turns the page dark before the app boots', () => {
        //when
        boot(storage('dark'), device(false));

        //then
        expect([isDark(), document.documentElement.style.colorScheme, statusBar()]).toEqual([true, 'dark', '#1B1D22']);
    });

    it('follows the device when System or nothing is saved', () => {
        //when
        boot(storage('system'), device(true));
        const darkInSystem = isDark();
        document.documentElement.className = '';
        boot(storage(null), device(true));

        //then
        expect([darkInSystem, isDark()]).toEqual([true, true]);
    });

    it('stays light when Light is saved, even on a dark device', () => {
        //when
        boot(storage('light'), device(true));

        //then
        expect([isDark(), document.documentElement.style.colorScheme, statusBar()]).toEqual([false, 'light', '#ffffff']);
    });

    it('starts in System when storage throws', () => {
        //when
        const run = () => boot(throwingStorage(), device(true));

        //then
        expect(run).not.toThrow();
        expect(isDark()).toBe(true);
    });

    it('runs in the head, after the theme-color meta', () => {
        //when
        const html = page();

        //then
        const meta = html.indexOf('name="theme-color"');
        const script = html.indexOf('<script>');
        expect(meta).toBeGreaterThan(-1);
        expect(script).toBeGreaterThan(meta);
        expect(html.indexOf('</head>')).toBeGreaterThan(script);
    });

    it('darkens the page background before the styles load', () => {
        //when
        const head = page().split('</head>')[0];

        //then
        expect(head).toMatch(/<style>\s*html\.app-dark\s*\{[^}]*color-scheme:\s*dark/);
    });
});
