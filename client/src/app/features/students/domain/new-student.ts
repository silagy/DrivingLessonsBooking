import { optionalText, toIsoDate } from './form-values';
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
