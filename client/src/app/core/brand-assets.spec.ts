import { readFileSync } from 'node:fs';

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47];

function icoImages(icon: Uint8Array): { size: number; head: number[] }[] {
    const view = new DataView(icon.buffer, icon.byteOffset, icon.byteLength);

    return Array.from({ length: view.getUint16(4, true) }, (_, index) => {
        const entry = 6 + index * 16;
        const offset = view.getUint32(entry + 12, true);

        return { size: icon[entry] || 256, head: Array.from(icon.subarray(offset, offset + 4)) };
    });
}

describe('brand assets', () => {
    it('names the browser tab Slotline in Hebrew before the app starts', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<title>סלוטליין</title>');
    });

    it('links the svg favicon with an ico fallback', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<link rel="icon" href="favicon.ico" sizes="32x32">');
        expect(page).toContain('<link rel="icon" href="favicon.svg" type="image/svg+xml">');
    });

    it('draws the lane tile in the svg favicon', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('rx="10"');
        expect(icon).toContain('d="M20 9v5M20 17.5v5M20 26v5"');
    });

    it('ships an ico fallback that embeds png renders', () => {
        //when
        const images = icoImages(readFileSync('public/favicon.ico'));

        //then
        expect(images.map(image => image.size)).toEqual([32, 48, 256]);
        expect(images.every(image => image.head.join() === PNG_SIGNATURE.join())).toBe(true);
    });
});
