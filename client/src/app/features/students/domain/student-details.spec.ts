import { StudentDetails } from './student-details.model';
import { EditStudentFormValue, toEditStudentFormValue, toStudentDetailsChange } from './student-details';
import { Transmission } from './transmission.enum';

const NOA: StudentDetails = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    address: '12 HaRimon St, Modiin',
    startDate: '2026-09-01',
    licenseType: 'B',
    isActive: true,
};

describe('toEditStudentFormValue', () => {
    it("fills the form with the Student's details and the start date as a local date", () => {
        //when
        const value = toEditStudentFormValue(NOA);

        //then
        expect(value).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234567',
            address: '12 HaRimon St, Modiin',
            startDate: new Date(2026, 8, 1),
            licenseType: 'B',
        });
    });

    it('leaves absent optional details blank', () => {
        //when
        const value = toEditStudentFormValue({ ...NOA, address: null, startDate: null, licenseType: null });

        //then
        expect([value.address, value.startDate, value.licenseType]).toEqual(['', null, '']);
    });
});

describe('toStudentDetailsChange', () => {
    it('trims every text and keeps the calendar day', () => {
        //given
        const value: EditStudentFormValue = {
            nationalId: ' 205374184 ',
            name: ' Noa Mizrahi ',
            phone: ' 050-1234568 ',
            address: ' 12 HaRimon St, Modiin ',
            startDate: new Date(2026, 8, 1),
            licenseType: ' B ',
        };

        //when
        const change = toStudentDetailsChange(value);

        //then
        expect(change).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234568',
            address: '12 HaRimon St, Modiin',
            startDate: '2026-09-01',
            licenseType: 'B',
        });
    });

    it('sends cleared optional details as absent, never as empty text', () => {
        //given
        const value: EditStudentFormValue = {
            ...toEditStudentFormValue(NOA),
            address: '   ',
            startDate: null,
            licenseType: '',
        };

        //when
        const change = toStudentDetailsChange(value);

        //then
        expect([change.address, change.startDate, change.licenseType]).toEqual([null, null, null]);
    });

    it('round-trips the saved start date', () => {
        //when
        const change = toStudentDetailsChange(toEditStudentFormValue(NOA));

        //then
        expect(change.startDate).toBe('2026-09-01');
    });
});
