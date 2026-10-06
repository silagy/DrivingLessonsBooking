import { CarOption } from './car-option.model';
import { pickCarFor, teacherHasCars } from './car-pick';
import { Transmission } from './transmission.enum';

const COROLLA: CarOption = {
    id: 'car-corolla',
    name: 'Corolla White',
    transmission: Transmission.automatic,
    teacherIds: ['teacher-ronit'],
};

const I20: CarOption = {
    id: 'car-i20',
    name: 'i20 Silver',
    transmission: Transmission.manual,
    teacherIds: ['teacher-ronit', 'teacher-yael'],
};

const PICANTO: CarOption = {
    id: 'car-picanto',
    name: 'Picanto Red',
    transmission: Transmission.automatic,
    teacherIds: ['teacher-yael'],
};

const MAZDA: CarOption = {
    id: 'car-mazda',
    name: 'Mazda 3 Grey',
    transmission: Transmission.manual,
    teacherIds: ['teacher-oren'],
};

const CARS = [PICANTO, MAZDA, I20, COROLLA];

describe('pickCarFor', () => {
    it('locks the Car until a Teacher is chosen', () => {
        //when
        const pick = pickCarFor(null, CARS);

        //then
        expect(pick).toEqual({ options: [], selectedCarId: null, isLocked: true, hasNoCars: false, isOnlyCar: false });
    });

    it("lists only the chosen Teacher's Cars, by name, with nothing chosen yet", () => {
        //when
        const pick = pickCarFor('teacher-yael', CARS);

        //then
        expect(pick).toEqual({
            options: [I20, PICANTO],
            selectedCarId: null,
            isLocked: false,
            hasNoCars: false,
            isOnlyCar: false,
        });
    });

    it('lists a shared Car for each of its Teachers', () => {
        //when
        const forRonit = pickCarFor('teacher-ronit', CARS);
        const forYael = pickCarFor('teacher-yael', CARS);

        //then
        expect(forRonit.options).toEqual([COROLLA, I20]);
        expect(forYael.options).toContain(I20);
    });

    it("preselects a Teacher's only Car", () => {
        //when
        const pick = pickCarFor('teacher-oren', CARS);

        //then
        expect(pick.options).toEqual([MAZDA]);
        expect(pick.selectedCarId).toBe('car-mazda');
        expect(pick.isOnlyCar).toBe(true);
    });

    it('selects nothing for a Teacher with no Cars', () => {
        //when
        const pick = pickCarFor('teacher-michal', CARS);

        //then
        expect(pick).toEqual({ options: [], selectedCarId: null, isLocked: false, hasNoCars: true, isOnlyCar: false });
    });

    it('starts empty again when the Teacher changes, even to a Teacher who shares the chosen Car', () => {
        //given
        const forRonit = pickCarFor('teacher-ronit', CARS);
        const chosen = forRonit.options.find((car) => car.id === I20.id);

        //when
        const forYael = pickCarFor('teacher-yael', CARS);

        //then
        expect(chosen).toEqual(I20);
        expect(forYael.options).toContain(I20);
        expect(forYael.selectedCarId).toBeNull();
    });
});

describe('teacherHasCars', () => {
    it('knows a Teacher with at least one Car', () => {
        //expected
        expect(teacherHasCars('teacher-yael', CARS)).toBe(true);
    });

    it('knows a Teacher with no Cars', () => {
        //expected
        expect(teacherHasCars('teacher-michal', CARS)).toBe(false);
    });
});
