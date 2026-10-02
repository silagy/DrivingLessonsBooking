import en from '../../../public/i18n/en.json';
import he from '../../../public/i18n/he.json';

const TYPOGRAPHIC_PUNCTUATION = /[\u2013\u2014\u2026]/;

type TranslationTree = { [key: string]: string | TranslationTree };

function keysWithTypographicPunctuation(tree: TranslationTree, prefix = ''): string[] {
    return Object.entries(tree).flatMap(([key, value]) => {
        const path = `${prefix}${key}`;

        if (typeof value === 'string') {
            return TYPOGRAPHIC_PUNCTUATION.test(value) ? [path] : [];
        }

        return keysWithTypographicPunctuation(value, `${path}.`);
    });
}

describe('translations', () => {
    it('use a plain hyphen and three dots, never an en dash, em dash or ellipsis character', () => {
        //given
        const files = { en, he } as Record<string, TranslationTree>;

        //when
        const offending = Object.entries(files)
            .flatMap(([lang, tree]) => keysWithTypographicPunctuation(tree).map(key => `${lang}: ${key}`));

        //then
        expect(offending).toEqual([]);
    });
});
