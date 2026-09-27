import { isCompleteNationalId, isNationalIdCandidate, toNationalIdDigits } from './national-id-input';

describe('toNationalIdDigits', () => {
    it.each([
        ['000 000 018', '000000018'],
        ['000-000-018', '000000018'],
        ['\u200E000000018\u200F', '000000018'],
        ['\u00A0000000018 ', '000000018'],
        ['18', '18'],
    ])('reduces %j to %j', (input, digits) => {
        expect(toNationalIdDigits(input)).toBe(digits);
    });

    it('keeps characters that are not separators so the ID is not silently changed', () => {
        expect(toNationalIdDigits('00000001a')).toBe('00000001a');
    });
});

describe('isNationalIdCandidate', () => {
    it.each([
        ['1', true],
        ['18', true],
        ['000000018', true],
        ['', false],
        ['0000000181', false],
        ['00000001a', false],
    ])('%j → %s', (digits, expected) => {
        expect(isNationalIdCandidate(digits)).toBe(expected);
    });
});

describe('isCompleteNationalId', () => {
    it.each([
        ['000000018', true],
        ['00000018', false],
        ['0000000181', false],
        ['00000001a', false],
    ])('%j → %s', (digits, expected) => {
        expect(isCompleteNationalId(digits)).toBe(expected);
    });
});
