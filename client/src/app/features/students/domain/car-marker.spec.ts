import { CarMarker } from './car-marker.enum';
import { carMarkerOf } from './car-marker';

describe('carMarkerOf', () => {
    it('shows no marker when the Car is one of the Teacher\'s current Cars', () => {
        //when
        const marker = carMarkerOf({ isCarOfTeacher: true, isTeacherDeleted: false, isCarDeleted: false });

        //then
        expect(marker).toBeNull();
    });

    it('flags a Car that is not one of the Teacher\'s Cars', () => {
        //when
        const marker = carMarkerOf({ isCarOfTeacher: false, isTeacherDeleted: false, isCarDeleted: false });

        //then
        expect(marker).toBe(CarMarker.notOfTeacher);
    });

    it.each([
        { isCarOfTeacher: true, isTeacherDeleted: false },
        { isCarOfTeacher: false, isTeacherDeleted: false },
        { isCarOfTeacher: false, isTeacherDeleted: true },
    ])('marks a deleted Car as deleted instead of flagging it (%o)', (flags) => {
        //when
        const marker = carMarkerOf({ ...flags, isCarDeleted: true });

        //then
        expect(marker).toBe(CarMarker.deleted);
    });

    it('leaves the Car unflagged when the Teacher is deleted, since the Teacher marker explains it', () => {
        //when
        const marker = carMarkerOf({ isCarOfTeacher: false, isTeacherDeleted: true, isCarDeleted: false });

        //then
        expect(marker).toBeNull();
    });
});
