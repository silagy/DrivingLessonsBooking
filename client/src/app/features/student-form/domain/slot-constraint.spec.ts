import { SLOT_CONSTRAINT_MAX_LENGTH, toSlotConstraint } from './slot-constraint';

describe('toSlotConstraint', () => {
    it('trims what was typed', () => {
        expect(toSlotConstraint('  only after 16:00 \n')).toBe('only after 16:00');
    });

    it.each(['', '   ', '\n\t'])('sends no constraint for blank text %j', text => {
        expect(toSlotConstraint(text)).toBeNull();
    });

    it('mirrors the backend limit of 200 characters', () => {
        expect(SLOT_CONSTRAINT_MAX_LENGTH).toBe(200);
    });
});
