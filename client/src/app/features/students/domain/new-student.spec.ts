import { AddStudentFormValue, toNewStudent } from './new-student';

const FORM: AddStudentFormValue = {
    nationalId: ' 123456782 ',
    name: ' Shaked Navon ',
    phone: ' 050-3318842 ',
    teacherId: 'teacher-yael',
    carId: 'car-picanto',
    address: ' 12 HaRimon St, Modiin ',
    startDate: new Date(2026, 8, 1),
    licenseType: ' B ',
};

describe('toNewStudent', () => {
    it('trims every text', () => {
        //when
        const newStudent = toNewStudent(FORM);

        //then
        expect(newStudent).toEqual({
            nationalId: '123456782',
            name: 'Shaked Navon',
            phone: '050-3318842',
            teacherId: 'teacher-yael',
            carId: 'car-picanto',
            address: '12 HaRimon St, Modiin',
            startDate: '2026-09-01',
            licenseType: 'B',
        });
    });

    it('sends blank optional details as absent, never as empty text', () => {
        //given
        const form: AddStudentFormValue = { ...FORM, address: '   ', startDate: null, licenseType: '' };

        //when
        const newStudent = toNewStudent(form);

        //then
        expect(newStudent.address).toBeNull();
        expect(newStudent.startDate).toBeNull();
        expect(newStudent.licenseType).toBeNull();
    });

    it("keeps the picked start date's calendar day, late in the evening too", () => {
        //given
        const lateEvening: AddStudentFormValue = { ...FORM, startDate: new Date(2026, 8, 1, 23, 30) };
        const earlyJanuary: AddStudentFormValue = { ...FORM, startDate: new Date(2027, 0, 5) };

        //when
        const late = toNewStudent(lateEvening);
        const early = toNewStudent(earlyJanuary);

        //then
        expect(late.startDate).toBe('2026-09-01');
        expect(early.startDate).toBe('2027-01-05');
    });
});
