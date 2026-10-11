import { fromIsoDate, optionalText, toIsoDate } from './form-values';
import { StudentDetailsChange } from './student-details-change.model';
import { StudentDetails } from './student-details.model';

export interface EditStudentFormValue {
    nationalId: string;
    name: string;
    phone: string;
    address: string;
    startDate: Date | null;
    licenseType: string;
}

export function toEditStudentFormValue(details: StudentDetails): EditStudentFormValue {
    return {
        nationalId: details.nationalId,
        name: details.name,
        phone: details.phone,
        address: details.address ?? '',
        startDate: details.startDate ? fromIsoDate(details.startDate) : null,
        licenseType: details.licenseType ?? '',
    };
}

export function toStudentDetailsChange(value: EditStudentFormValue): StudentDetailsChange {
    return {
        nationalId: value.nationalId.trim(),
        name: value.name.trim(),
        phone: value.phone.trim(),
        address: optionalText(value.address),
        startDate: value.startDate ? toIsoDate(value.startDate) : null,
        licenseType: optionalText(value.licenseType),
    };
}
