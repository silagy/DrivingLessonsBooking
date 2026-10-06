export enum StudentRefusalKind {
    nationalIdInUse = 'nationalIdInUse',
    nationalIdInvalid = 'nationalIdInvalid',
    staleCar = 'staleCar',
    sameTeacher = 'sameTeacher',
    sameCar = 'sameCar',
    other = 'other',
}

export interface StudentRefusal {
    kind: StudentRefusalKind;
    message: string;
    existingStudentName: string | null;
}

const KIND_BY_CODE: Partial<Record<string, StudentRefusalKind>> = {
    studentNationalIdAlreadyInUse: StudentRefusalKind.nationalIdInUse,
    nationalIdMustBeDigits: StudentRefusalKind.nationalIdInvalid,
    nationalIdMustBeAtMostNineDigits: StudentRefusalKind.nationalIdInvalid,
    nationalIdMustHaveValidCheckDigit: StudentRefusalKind.nationalIdInvalid,
    studentCarMustBeAssignedToTeacher: StudentRefusalKind.staleCar,
    carNotFound: StudentRefusalKind.staleCar,
    studentAlreadyWithTeacher: StudentRefusalKind.sameTeacher,
    studentAlreadyOnCar: StudentRefusalKind.sameCar,
};

export function refusalKindOf(code: string | undefined): StudentRefusalKind {
    if (!code) {
        return StudentRefusalKind.other;
    }

    return KIND_BY_CODE[code] ?? StudentRefusalKind.other;
}
