import { existsSync, readFileSync } from 'node:fs';

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47];

function icoImages(icon: Uint8Array): { size: number; head: number[] }[] {
    const view = new DataView(icon.buffer, icon.byteOffset, icon.byteLength);

    return Array.from({ length: view.getUint16(4, true) }, (_, index) => {
        const entry = 6 + index * 16;
        const offset = view.getUint32(entry + 12, true);

        return { size: icon[entry] || 256, head: Array.from(icon.subarray(offset, offset + 4)) };
    });
}

function brandKeys(lang: string): { appName: string; nameLead: string; nameAccent: string } {
    return JSON.parse(readFileSync(`public/i18n/${lang}.json`, 'utf8')).brand;
}

describe('brand assets', () => {
    it('names the browser tab DriveCal in Hebrew before the app starts', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<title>דרייבקל</title>');
    });

    it('links the svg favicon with an ico fallback', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<link rel="icon" href="favicon.ico" sizes="32x32">');
        expect(page).toContain('<link rel="icon" href="favicon.svg" type="image/svg+xml">');
    });

    it('draws the solid calendar in the svg favicon, reversed on dark tab bars', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('x="1" y="5.5" width="38" height="33.5" rx="10"');
        expect(icon).toContain('d="M12.5 1.8v7M27.5 1.8v7"');
        expect(icon).toContain('cx="12.5" cy="9.5" r="1.6"');
        expect(icon).toContain('cx="27.5" cy="9.5" r="1.6"');
        expect(icon).toContain('d="M20 15.5v4M20 23.5v4M20 31.5v3"');
        expect(icon).toContain('@media (prefers-color-scheme: dark)');
        expect(icon).not.toContain('M20 9v5');
    });

    it('ships an ico fallback that embeds png renders', () => {
        //when
        const images = icoImages(readFileSync('public/favicon.ico'));

        //then
        expect(images.map(image => image.size)).toEqual([32, 48, 256]);
        expect(images.every(image => image.head.join() === PNG_SIGNATURE.join())).toBe(true);
    });

    it('spells the wordmark halves into the product name in both languages', () => {
        //when
        const names = ['en', 'he'].map(brandKeys).map(brand => [brand.appName, brand.nameLead + brand.nameAccent]);

        //then
        expect(names).toEqual([['DriveCal', 'DriveCal'], ['דרייבקל', 'דרייבקל']]);
    });

    it('self-hosts rubik for the hebrew wordmark only', () => {
        //when
        const styles = readFileSync('src/styles/_global.scss', 'utf8');
        const face = styles.split('@font-face').find(block => block.includes("font-family: 'Rubik'")) ?? '';

        //then
        expect(face).toContain('@fontsource-variable/rubik/files/rubik-hebrew-wght-normal.woff2');
        expect(face).toContain('unicode-range: U+0307-0308, U+0590-05FF, U+200C-2010, U+20AA, U+25CC, U+FB1D-FB4F;');
        expect(existsSync('node_modules/@fontsource-variable/rubik/files/rubik-hebrew-wght-normal.woff2')).toBe(true);
    });

    it('sets the wordmark italic in english and upright in hebrew', () => {
        //when
        const [root, hebrew] = readFileSync('src/styles/_tokens.scss', 'utf8').split(":root[lang='he']");

        //then
        expect(root).toContain("--app-font-wordmark: 'Rubik',");
        expect(root).toContain('--app-wordmark-style: italic;');
        expect(hebrew).toContain('--app-wordmark-style: normal;');
    });
});
