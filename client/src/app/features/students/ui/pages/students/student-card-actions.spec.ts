import { MenuItem } from 'primeng/api';
import { StudentRow } from '../../../domain/student-row.model';
import { Transmission } from '../../../domain/transmission.enum';
import { StudentCardCommands, studentCardActions } from './student-card-actions';

const STUDENT: StudentRow = {
    id: 'student-noa',
    nationalId: '205374188',
    name: 'Noa Mizrahi',
    phone: '054-2218830',
    teacherId: 'teacher-peer',
    teacherName: 'Ziv Peer',
    carId: 'car-tesla',
    carName: 'Tesla Ziv',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isTeacherDeleted: false,
    isCarDeleted: false,
    isActive: true,
    isNew: false,
    carMarker: null,
};

const SEPARATOR = '---';

function commands(): StudentCardCommands {
    return {
        editDetails: vi.fn(),
        changeTeacher: vi.fn(),
        changeCar: vi.fn(),
        deactivate: vi.fn(),
        reactivate: vi.fn(),
    };
}

function labels(items: MenuItem[]): (string | undefined)[] {
    return items.map((item) => (item.separator ? SEPARATOR : item.label));
}

function itemLabelled(items: MenuItem[], label: string): MenuItem {
    return items.find((item) => item.label === label)!;
}

function run(item: MenuItem): void {
    item.command!({});
}

describe('studentCardActions', () => {
    it('puts fixing the car first when the car is not the Teacher\'s', () => {
        //given
        const student = { ...STUDENT, isCarOfTeacher: false };
        const studentCommands = commands();

        //when
        const items = studentCardActions(student, (key) => key, studentCommands);
        run(items[0]);

        //then
        expect(labels(items)).toEqual([
            'students.actions.fixCar',
            SEPARATOR,
            'students.actions.editDetails',
            'students.actions.changeTeacher',
            'students.actions.changeCar',
            SEPARATOR,
            'students.actions.deactivate',
        ]);
        expect(items[0].styleClass).toBe('card-menu__item--fix');
        expect(studentCommands.changeCar).toHaveBeenCalledTimes(1);
    });

    it('keeps Change car plain under the fix', () => {
        //when
        const items = studentCardActions({ ...STUDENT, isCarOfTeacher: false }, (key) => key, commands());

        //then
        const changeCar = itemLabelled(items, 'students.actions.changeCar');
        expect(changeCar.styleClass).toBeUndefined();
        expect(changeCar.badge).toBeUndefined();
    });

    it('offers no fix when the car is the Teacher\'s', () => {
        //when
        const items = studentCardActions(STUDENT, (key) => key, commands());

        //then
        expect(labels(items)).toEqual([
            'students.actions.editDetails',
            'students.actions.changeTeacher',
            'students.actions.changeCar',
            SEPARATOR,
            'students.actions.deactivate',
        ]);
    });

    it('shows deactivating as destructive', () => {
        //given
        const studentCommands = commands();

        //when
        const items = studentCardActions(STUDENT, (key) => key, studentCommands);
        const deactivate = itemLabelled(items, 'students.actions.deactivate');
        run(deactivate);

        //then
        expect(deactivate.styleClass).toBe('card-menu__item--destructive');
        expect(studentCommands.deactivate).toHaveBeenCalledTimes(1);
    });

    it('offers editing and reactivating an Inactive Student', () => {
        //given
        const studentCommands = commands();

        //when
        const items = studentCardActions({ ...STUDENT, isActive: false, isCarOfTeacher: false }, (key) => key, studentCommands);
        run(itemLabelled(items, 'students.actions.reactivate'));
        run(itemLabelled(items, 'students.actions.editDetails'));

        //then
        expect(labels(items)).toEqual(['students.actions.editDetails', 'students.actions.reactivate']);
        expect(itemLabelled(items, 'students.actions.reactivate').styleClass).toBe(
            'students-menu__item--mirrored-icon',
        );
        expect(studentCommands.reactivate).toHaveBeenCalledTimes(1);
        expect(studentCommands.editDetails).toHaveBeenCalledTimes(1);
    });

    it('runs Change teacher from its item', () => {
        //given
        const studentCommands = commands();

        //when
        const items = studentCardActions(STUDENT, (key) => key, studentCommands);
        run(itemLabelled(items, 'students.actions.changeTeacher'));

        //then
        expect(studentCommands.changeTeacher).toHaveBeenCalledTimes(1);
    });
});
