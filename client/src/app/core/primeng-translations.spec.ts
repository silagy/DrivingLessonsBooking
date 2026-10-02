import { PRIMENG_EN, PRIMENG_HE } from './primeng-translations';

function keyPaths(value: object, prefix = ''): string[] {
    return Object.entries(value).flatMap(([key, child]) =>
        child && typeof child === 'object' && !Array.isArray(child)
            ? keyPaths(child, `${prefix}${key}.`)
            : [`${prefix}${key}`],
    );
}

function textValues(value: object): string[] {
    return Object.values(value).flatMap(child => {
        if (typeof child === 'string') {
            return [child];
        }

        return child && typeof child === 'object' ? textValues(child) : [];
    });
}

describe('PrimeNG translations', () => {
    it('gives Hebrew and English the same labels', () => {
        expect(keyPaths(PRIMENG_HE).sort()).toEqual(keyPaths(PRIMENG_EN).sort());
    });

    it('writes every Hebrew label in Hebrew', () => {
        expect(textValues(PRIMENG_HE).filter(text => /[A-Za-z]/.test(text))).toEqual([]);
    });

    it('covers the labels the date picker, select and dialogs show', () => {
        expect(keyPaths(PRIMENG_HE)).toEqual(expect.arrayContaining([
            'chooseDate',
            'prevMonth',
            'nextMonth',
            'nextHour',
            'prevMinute',
            'emptyMessage',
            'aria.close',
            'aria.listLabel',
        ]));
    });
});
