import { CarOption } from './car-option.model';
import { carChangeFor } from './car-change';
import { Student } from './student.model';
import { Transmission } from './transmission.enum';

const COROLLA: CarOption = { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] };
const I20: CarOption = { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] };
const MAZDA: CarOption = { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] };
const CARS = [MAZDA, I20, COROLLA];

const NOA: Student = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isActive: true,
};

describe('carChangeFor', () => {
    it("offers the Teacher's Cars by name with the current one marked", () => {
        //when
        const change = carChangeFor(NOA, CARS);

        //then
        expect(change.choices).toEqual([
            { ...COROLLA, isCurrent: true },
            { ...I20, isCurrent: false },
        ]);
        expect(change.hasOtherCars).toBe(true);
    });

    it('has no other Car when the current Car is the Teacher\'s only one', () => {
        //given
        const lia: Student = { ...NOA, teacherId: 'teacher-oren', carId: MAZDA.id };

        //when
        const change = carChangeFor(lia, CARS);

        //then
        expect(change.choices).toEqual([{ ...MAZDA, isCurrent: true }]);
        expect(change.hasOtherCars).toBe(false);
    });

    it('offers every Car of the Teacher to a flagged Student', () => {
        //given
        const roi: Student = { ...NOA, teacherId: 'teacher-oren', carId: COROLLA.id, isCarOfTeacher: false };

        //when
        const change = carChangeFor(roi, CARS);

        //then
        expect(change.choices).toEqual([{ ...MAZDA, isCurrent: false }]);
        expect(change.hasOtherCars).toBe(true);
    });

    it('has nothing to offer when the Teacher has no Cars', () => {
        //given
        const flagged: Student = { ...NOA, teacherId: 'teacher-michal', isCarOfTeacher: false };

        //when
        const change = carChangeFor(flagged, CARS);

        //then
        expect(change.choices).toEqual([]);
        expect(change.hasOtherCars).toBe(false);
    });
});
