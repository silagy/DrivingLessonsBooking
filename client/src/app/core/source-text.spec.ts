import { readdirSync, readFileSync } from 'node:fs';

const SOURCE_ROOT = 'src';
const SOURCE_FILE = /\.(ts|html|scss)$/;
const TYPOGRAPHIC_PUNCTUATION = /[\u2013\u2014\u2026]/;

function linesWithTypographicPunctuation(): string[] {
    return readdirSync(SOURCE_ROOT, { recursive: true })
        .filter(file => SOURCE_FILE.test(file))
        .flatMap(file => readFileSync(`${SOURCE_ROOT}/${file}`, 'utf8')
            .split('\n')
            .flatMap((line, index) => (TYPOGRAPHIC_PUNCTUATION.test(line) ? [`${file}:${index + 1}`] : [])));
}

describe('client source text', () => {
    it('uses a plain hyphen and three dots, never an en dash, em dash or ellipsis character', () => {
        //when
        const offending = linesWithTypographicPunctuation();

        //then
        expect(offending).toEqual([]);
    });
});
