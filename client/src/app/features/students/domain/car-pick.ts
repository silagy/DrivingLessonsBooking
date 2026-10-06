import { CarOption } from './car-option.model';

export interface CarPick {
    options: CarOption[];
    selectedCarId: string | null;
    isLocked: boolean;
    hasNoCars: boolean;
    isOnlyCar: boolean;
}

const SINGLE_CAR_COUNT = 1;

export function pickCarFor(teacherId: string | null, cars: readonly CarOption[]): CarPick {
    if (!teacherId) {
        return { options: [], selectedCarId: null, isLocked: true, hasNoCars: false, isOnlyCar: false };
    }

    const options = carsOf(teacherId, cars);
    const isOnlyCar = options.length === SINGLE_CAR_COUNT;

    return {
        options,
        selectedCarId: isOnlyCar ? options[0].id : null,
        isLocked: false,
        hasNoCars: !options.length,
        isOnlyCar,
    };
}

export function teacherHasCars(teacherId: string, cars: readonly CarOption[]): boolean {
    return cars.some((car) => car.teacherIds.includes(teacherId));
}

function carsOf(teacherId: string, cars: readonly CarOption[]): CarOption[] {
    return cars
        .filter((car) => car.teacherIds.includes(teacherId))
        .sort((first, second) => first.name.localeCompare(second.name));
}
