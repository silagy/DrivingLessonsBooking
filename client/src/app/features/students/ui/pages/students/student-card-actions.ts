import { MenuItem } from 'primeng/api';
import { StudentRow } from '../../../domain/student-row.model';

export interface StudentCardCommands {
    editDetails(): void;
    changeTeacher(): void;
    changeCar(): void;
    deactivate(): void;
    reactivate(): void;
}

const SEPARATOR: MenuItem = { separator: true };

export function studentCardActions(
    student: StudentRow,
    translate: (key: string) => string,
    commands: StudentCardCommands,
): MenuItem[] {
    const editDetails: MenuItem = {
        label: translate('students.actions.editDetails'),
        icon: 'pi pi-pencil',
        command: () => commands.editDetails(),
    };

    if (!student.isActive) {
        return [
            editDetails,
            {
                label: translate('students.actions.reactivate'),
                icon: 'pi pi-replay',
                styleClass: 'students-menu__item--mirrored-icon',
                command: () => commands.reactivate(),
            },
        ];
    }

    const usualActions: MenuItem[] = [
        editDetails,
        {
            label: translate('students.actions.changeTeacher'),
            icon: 'pi pi-arrow-right-arrow-left',
            command: () => commands.changeTeacher(),
        },
        {
            label: translate('students.actions.changeCar'),
            icon: 'pi pi-car',
            command: () => commands.changeCar(),
        },
        SEPARATOR,
        {
            label: translate('students.actions.deactivate'),
            icon: 'pi pi-pause-circle',
            styleClass: 'card-menu__item--destructive',
            command: () => commands.deactivate(),
        },
    ];

    if (student.isCarOfTeacher) {
        return usualActions;
    }

    const fixCar: MenuItem = {
        label: translate('students.actions.fixCar'),
        icon: 'pi pi-exclamation-circle',
        styleClass: 'card-menu__item--fix',
        command: () => commands.changeCar(),
    };

    return [fixCar, SEPARATOR, ...usualActions];
}
