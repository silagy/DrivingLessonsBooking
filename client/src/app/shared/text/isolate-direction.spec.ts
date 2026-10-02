import { isolateDirection } from './isolate-direction';

const FIRST_STRONG_ISOLATE = '⁨';
const POP_DIRECTIONAL_ISOLATE = '⁩';

describe('isolateDirection', () => {
    it('lets a name keep its own reading direction inside a sentence', () => {
        //given
        const carName = '3 Series';

        //when
        const isolated = isolateDirection(carName);

        //then
        expect(isolated).toBe(`${FIRST_STRONG_ISOLATE}3 Series${POP_DIRECTIONAL_ISOLATE}`);
    });

    it('leaves an empty value empty', () => {
        //given
        const missingName = '';

        //when
        const isolated = isolateDirection(missingName);

        //then
        expect(isolated).toBe('');
    });
});
