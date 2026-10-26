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

function leavesOf(tree: TranslationTree, prefix = ''): [string, string][] {
    return Object.entries(tree).flatMap(([key, value]): [string, string][] => {
        const path = `${prefix}${key}`;

        return typeof value === 'string' ? [[path, value]] : leavesOf(value, `${path}.`);
    });
}

function studentFormOf(tree: unknown): TranslationTree {
    return (tree as TranslationTree)['studentForm'] as TranslationTree;
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

    it('call a slot שיעור אפשרי on the Hebrew student form, never שעה', () => {
        //given
        const clockTimeKeys = ['studentForm.pick.constraintHint'];

        //when
        const offending = leavesOf(studentFormOf(he), 'studentForm.')
            .filter(([key, value]) => /שע(ה|ות)/.test(value) && !clockTimeKeys.includes(key))
            .map(([key]) => key);

        //then
        expect(offending).toEqual([]);
    });

    it('never call extra picks a backup on the student form', () => {
        //when
        const offending = [
            ...leavesOf(studentFormOf(he), 'he: studentForm.').filter(([, value]) => value.includes('גיבוי')),
            ...leavesOf(studentFormOf(en), 'en: studentForm.').filter(([, value]) => /backup/i.test(value)),
        ].map(([key]) => key);

        //then
        expect(offending).toEqual([]);
    });
});
