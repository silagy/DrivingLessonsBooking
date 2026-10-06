import { CarOption } from './car-option.model';
import { carsOfTeacher } from './car-pick';
import { Student } from './student.model';

export interface CarChoice extends CarOption {
    isCurrent: boolean;
}

export interface CarChange {
    choices: CarChoice[];
    hasOtherCars: boolean;
}

export function carChangeFor(student: Student, cars: readonly CarOption[]): CarChange {
    const choices = carsOfTeacher(student.teacherId, cars).map((car) => ({
        ...car,
        isCurrent: car.id === student.carId,
    }));

    return { choices, hasOtherCars: choices.some((choice) => !choice.isCurrent) };
}
