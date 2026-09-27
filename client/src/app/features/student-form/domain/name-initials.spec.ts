import { nameInitials } from './name-initials';

describe('nameInitials', () => {
    it.each([
        ['Teacher Cohen', 'TC'],
        ['avi ben cohen', 'AC'],
        ['Cohen', 'C'],
        ['  Avi   Cohen  ', 'AC'],
        ['אבי כהן', 'אכ'],
        ['', ''],
    ])('%j → %j', (name, initials) => {
        expect(nameInitials(name)).toBe(initials);
    });
});
