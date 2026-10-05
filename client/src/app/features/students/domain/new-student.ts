import { NewStudent } from './new-student.model';

export interface AddStudentFormValue {
    nationalId: string;
    name: string;
    phone: string;
    teacherId: string;
    carId: string;
    address: string;
    startDate: Date | null;
    licenseType: string;
}

const MONTH_OFFSET = 1;
const DATE_PART_LENGTH = 2;
const DATE_PART_PAD = '0';

export function toNewStudent(value: AddStudentFormValue): NewStudent {
    return {
        nationalId: value.nationalId.trim(),
        name: value.name.trim(),
        phone: value.phone.trim(),
        teacherId: value.teacherId,
        carId: value.carId,
        address: optionalText(value.address),
        startDate: value.startDate ? toIsoDate(value.startDate) : null,
        licenseType: optionalText(value.licenseType),
    };
}

function optionalText(value: string): string | null {
    const trimmed = value.trim();

    return trimmed ? trimmed : null;
}

function toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + MONTH_OFFSET).padStart(DATE_PART_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_LENGTH, DATE_PART_PAD);

    return `${year}-${month}-${day}`;
}
