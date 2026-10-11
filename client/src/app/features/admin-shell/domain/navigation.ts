import { AppRoutes } from '../../../shared/config/app-routes';
import { Role } from '../../../shared/models/role.enum';

export interface NavigationItem {
    labelKey: string;
    commands: readonly string[];
    exact: boolean;
}

const DASHBOARD: NavigationItem = { labelKey: 'shell.nav.dashboard', commands: ['/'], exact: true };
const TEACHERS: NavigationItem = { labelKey: 'shell.nav.teachers', commands: ['/', AppRoutes.teachers], exact: false };
const STUDENTS: NavigationItem = {
    labelKey: 'shell.nav.students',
    commands: ['/', AppRoutes.students],
    exact: false,
};
const WEEKLY_PREP: NavigationItem = {
    labelKey: 'shell.nav.weeklyPrep',
    commands: ['/', AppRoutes.weekSchedules],
    exact: false,
};
const PUBLICATIONS: NavigationItem = {
    labelKey: 'shell.nav.publications',
    commands: ['/', AppRoutes.publications],
    exact: true,
};
const HISTORY: NavigationItem = {
    labelKey: 'shell.nav.history',
    commands: ['/', AppRoutes.publications, AppRoutes.publicationsHistory],
    exact: false,
};
const USERS: NavigationItem = { labelKey: 'shell.nav.users', commands: ['/', AppRoutes.users], exact: false };

const ADMINISTRATOR_NAVIGATION: readonly NavigationItem[] = [
    DASHBOARD,
    TEACHERS,
    STUDENTS,
    WEEKLY_PREP,
    PUBLICATIONS,
    HISTORY,
    USERS,
];
const TEACHER_NAVIGATION: readonly NavigationItem[] = [WEEKLY_PREP, PUBLICATIONS, HISTORY];

export function navigationFor(role: Role | null): readonly NavigationItem[] {
    switch (role) {
        case Role.administrator:
            return ADMINISTRATOR_NAVIGATION;
        case Role.teacher:
            return TEACHER_NAVIGATION;
        default:
            return [];
    }
}
