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

    it('draws the plain wheel in the svg favicon, reversed on dark tab bars', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('cx="20" cy="20" r="19"');
        expect(icon).toContain('d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z"');
        expect(icon).toContain('d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z"');
        expect(icon).toContain('d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z"');
        expect(icon).toContain('cx="20" cy="21.5" r="4.6"');
        expect(icon).toContain('@media (prefers-color-scheme: dark)');
        expect(icon).not.toContain('<rect');
    });

    it('ships an ico fallback that embeds png renders at 16, 32, 48 and 256', () => {
        //when
        const images = icoImages(readFileSync('public/favicon.ico'));

        //then
        expect(images.map(image => image.size)).toEqual([16, 32, 48, 256]);
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
