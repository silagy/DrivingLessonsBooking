const NATIONAL_ID_LENGTH = 9;
const SEPARATORS = /[\s\-\u200E\u200F\u202A-\u202E\u2066-\u2069]/g;
const CANDIDATE = /^\d{1,9}$/;

export function toNationalIdDigits(input: string): string {
    return input.replace(SEPARATORS, '');
}

export function isNationalIdCandidate(digits: string): boolean {
    return CANDIDATE.test(digits);
}

export function isCompleteNationalId(digits: string): boolean {
    return isNationalIdCandidate(digits) && digits.length === NATIONAL_ID_LENGTH;
}
