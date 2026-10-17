import { readdirSync, readFileSync } from 'node:fs';

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47];

function publicText(path: string): string {
    return readFileSync(`public/${path}`, 'utf8');
}

function pngSize(path: string): [number, number] {
    const bytes = readFileSync(`public/${path}`);
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    expect(Array.from(bytes.subarray(0, 4))).toEqual(PNG_SIGNATURE);

    return [view.getUint32(16), view.getUint32(20)];
}

describe('pwa assets', () => {
    it('describes the installed app in hebrew', () => {
        //when
        const manifest = JSON.parse(publicText('manifest.webmanifest'));

        //then
        expect(manifest).toEqual({
            id: '/',
            name: 'דרייבקל',
            short_name: 'דרייבקל',
            description: 'ביקוש שבועי לשיעורים, בקישור אחד',
            lang: 'he',
            dir: 'rtl',
            start_url: '/',
            scope: '/',
            display: 'standalone',
            theme_color: '#ffffff',
            background_color: '#FBFBFF',
            icons: [
                { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
                { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
                { src: 'icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
            ],
        });
    });

    it('renders every icon at the size it declares', () => {
        //given
        const manifest = JSON.parse(publicText('manifest.webmanifest'));

        //when
        const sizes = manifest.icons.map((icon: { src: string }) => pngSize(icon.src).join('x'));

        //then
        expect(sizes).toEqual(manifest.icons.map((icon: { sizes: string }) => icon.sizes));
        expect(pngSize('apple-touch-icon.png')).toEqual([180, 180]);
    });

    it('links the manifest, theme color and home screen icon from the page', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<meta name="theme-color" content="#ffffff">');
        expect(page).toContain('<link rel="manifest" href="manifest.webmanifest">');
        expect(page).toContain('<link rel="apple-touch-icon" href="apple-touch-icon.png">');
        expect(page).toContain('<meta name="apple-mobile-web-app-title" content="דרייבקל">');
    });

    it('offline page stands alone, in hebrew and english', () => {
        //when
        const page = publicText('offline.html');

        //then
        expect(page).toContain('<html lang="he" dir="rtl">');
        expect(page).toContain('<title>דרייבקל</title>');
        expect(page).toContain('אין חיבור לאינטרנט');
        expect(page).toContain('דרייבקל צריך חיבור כדי להציג נתונים עדכניים. בדקו את החיבור לאינטרנט ונסו שוב.');
        expect(page).toContain('נסו שוב');
        expect(page).toContain('lang="en" dir="ltr"');
        expect(page).toContain('No internet connection');
        expect(page).toContain('DriveCal needs a connection to show current data. Check your connection and try again.');
        expect(page).toContain('Try again');
        expect(page).toContain('onclick="location.reload()"');
        expect(page).toContain('cx="20" cy="20" r="19"');
        expect(page).not.toMatch(/<script\s[^>]*src|<link|<img|url\(/);
    });

    it('writes the public text files with a plain hyphen and three dots', () => {
        //when
        const offending = readdirSync('public', { recursive: true })
            .filter(file => /\.(webmanifest|js|html)$/.test(file))
            .filter(file => /[\u2013\u2014\u2026]/.test(publicText(file)));

        //then
        expect(offending).toEqual([]);
    });
});
