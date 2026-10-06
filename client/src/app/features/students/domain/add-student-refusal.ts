export enum AddStudentRefusalKind {
    nationalIdInUse = 'nationalIdInUse',
    nationalIdInvalid = 'nationalIdInvalid',
    staleCar = 'staleCar',
    other = 'other',
}

export interface AddStudentRefusal {
    kind: AddStudentRefusalKind;
    message: string;
}

const KIND_BY_CODE: Partial<Record<string, AddStudentRefusalKind>> = {
    studentNationalIdAlreadyInUse: AddStudentRefusalKind.nationalIdInUse,
    nationalIdMustBeDigits: AddStudentRefusalKind.nationalIdInvalid,
    nationalIdMustBeAtMostNineDigits: AddStudentRefusalKind.nationalIdInvalid,
    nationalIdMustHaveValidCheckDigit: AddStudentRefusalKind.nationalIdInvalid,
    studentCarMustBeAssignedToTeacher: AddStudentRefusalKind.staleCar,
    carNotFound: AddStudentRefusalKind.staleCar,
};

export function refusalKindOf(code: string | undefined): AddStudentRefusalKind {
    if (!code) {
        return AddStudentRefusalKind.other;
    }

    return KIND_BY_CODE[code] ?? AddStudentRefusalKind.other;
}
