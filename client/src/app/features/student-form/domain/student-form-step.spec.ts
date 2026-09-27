import { StudentFormStep } from './student-form-step.enum';
import { stepNumberOf, WIZARD_STEPS } from './student-form-step';

describe('wizard steps', () => {
    it('runs identify → details → slots', () => {
        expect(WIZARD_STEPS).toEqual([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.slots]);
    });

    it.each([
        [StudentFormStep.identify, 1],
        [StudentFormStep.details, 2],
        [StudentFormStep.slots, 3],
    ])('numbers %s as step %i', (step, number) => {
        expect(stepNumberOf(step)).toBe(number);
    });
});
