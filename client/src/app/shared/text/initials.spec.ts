import { initials } from './initials';

describe('initials', () => {
    it('takes the first letter of the first two words', () => {
        //given
        const name = 'Dana Levi Cohen';

        //when
        const result = initials(name);

        //then
        expect(result).toBe('DL');
    });

    it('reads Hebrew names the same way', () => {
        //given
        const name = 'רונית אברהם';

        //when
        const result = initials(name);

        //then
        expect(result).toBe('רא');
    });

    it('ignores extra spaces around and between words', () => {
        //given
        const name = '  Avi   Cohen ';

        //when
        const result = initials(name);

        //then
        expect(result).toBe('AC');
    });
});
