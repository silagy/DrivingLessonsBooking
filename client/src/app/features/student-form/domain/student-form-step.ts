import { StudentFormStep } from './student-form-step.enum';

export const WIZARD_STEPS: readonly StudentFormStep[] = [
    StudentFormStep.identify,
    StudentFormStep.details,
    StudentFormStep.slots,
];

export function stepNumberOf(step: StudentFormStep): number {
    return WIZARD_STEPS.indexOf(step) + 1;
}
