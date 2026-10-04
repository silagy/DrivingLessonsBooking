import { passwordsMatch } from './passwords-match';

describe('passwords match', () => {
    it('matches the same password typed twice', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2027')).toBe(true);
    });

    it('does not match a different confirmation', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2026')).toBe(false);
    });

    it('does not match when only the case differs', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'fresh#2027')).toBe(false);
    });

    it('does not match when only surrounding spaces differ, because the password is stored as typed', () => {
        //expected
        expect(passwordsMatch('Fresh#2027', 'Fresh#2027 ')).toBe(false);
    });
});
