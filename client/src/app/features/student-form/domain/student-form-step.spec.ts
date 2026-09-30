import { StudentFormStep } from './student-form-step.enum';
import { previousStepOf, stepNumberOf, WIZARD_STEPS } from './student-form-step';

describe('wizard steps', () => {
    it('runs identify → details → target → slots → review', () => {
        expect(WIZARD_STEPS).toEqual([
            StudentFormStep.identify,
            StudentFormStep.details,
            StudentFormStep.target,
            StudentFormStep.slots,
            StudentFormStep.review,
        ]);
    });

    it.each([
        [StudentFormStep.identify, 1],
        [StudentFormStep.details, 2],
        [StudentFormStep.target, 3],
        [StudentFormStep.slots, 4],
        [StudentFormStep.review, 5],
    ])('numbers %s as step %i', (step, number) => {
        expect(stepNumberOf(step)).toBe(number);
    });

    it.each([
        [StudentFormStep.target, StudentFormStep.details],
        [StudentFormStep.slots, StudentFormStep.target],
        [StudentFormStep.review, StudentFormStep.slots],
    ])('goes back from %s to %s', (step, previous) => {
        expect(previousStepOf(step)).toBe(previous);
    });

    it.each([StudentFormStep.identify, StudentFormStep.details, StudentFormStep.done, StudentFormStep.windowClosed])(
        'offers no way back from %s',
        step => {
            expect(previousStepOf(step)).toBeNull();
        },
    );

    it('keeps the window-closed screen out of the numbered steps', () => {
        expect(WIZARD_STEPS).not.toContain(StudentFormStep.windowClosed);
    });
});
