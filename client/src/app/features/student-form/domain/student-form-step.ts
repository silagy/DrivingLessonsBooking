import { StudentFormStep } from './student-form-step.enum';

export const WIZARD_STEPS: readonly StudentFormStep[] = [
    StudentFormStep.identify,
    StudentFormStep.details,
    StudentFormStep.target,
    StudentFormStep.slots,
    StudentFormStep.review,
];

const PREVIOUS_STEP: Partial<Record<StudentFormStep, StudentFormStep>> = {
    [StudentFormStep.target]: StudentFormStep.details,
    [StudentFormStep.slots]: StudentFormStep.target,
    [StudentFormStep.review]: StudentFormStep.slots,
};

export function stepNumberOf(step: StudentFormStep): number {
    return WIZARD_STEPS.indexOf(step) + 1;
}

export function previousStepOf(step: StudentFormStep): StudentFormStep | null {
    return PREVIOUS_STEP[step] ?? null;
}
