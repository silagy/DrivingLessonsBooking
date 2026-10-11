import { AppRoutes } from '../../../shared/config/app-routes';
import { Role } from '../../../shared/models/role.enum';
import { navigationFor } from './navigation';

function labelsOf(role: Role | null): string[] {
    return navigationFor(role).map((item) => item.labelKey);
}

describe('navigationFor', () => {
    it('gives an Administrator every screen, with Users last', () => {
        //expected
        expect(labelsOf(Role.administrator)).toEqual([
            'shell.nav.dashboard',
            'shell.nav.teachers',
            'shell.nav.students',
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
            'shell.nav.users',
        ]);
    });

    it('gives a Teacher only weekly prep, publications and history', () => {
        //expected
        expect(labelsOf(Role.teacher)).toEqual([
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
        ]);
    });

    it('shows no navigation without a known Role', () => {
        //expected
        expect(navigationFor(null)).toEqual([]);
    });

    it('links each screen to its route', () => {
        //given
        const commandsByLabel = new Map(
            navigationFor(Role.administrator).map((item) => [item.labelKey, item.commands]),
        );

        //expected
        expect(commandsByLabel.get('shell.nav.dashboard')).toEqual(['/']);
        expect(commandsByLabel.get('shell.nav.teachers')).toEqual(['/', 'teachers']);
        expect(commandsByLabel.get('shell.nav.students')).toEqual(['/', 'students']);
        expect(commandsByLabel.get('shell.nav.weeklyPrep')).toEqual(['/', 'week-schedules']);
        expect(commandsByLabel.get('shell.nav.publications')).toEqual(['/', 'publications']);
        expect(commandsByLabel.get('shell.nav.history')).toEqual(['/', 'publications', 'history']);
        expect(commandsByLabel.get('shell.nav.users')).toEqual(['/', 'users']);
    });

    it('highlights the dashboard and publications only on their exact route', () => {
        //given
        const exactLabels = navigationFor(Role.administrator)
            .filter((item) => item.exact)
            .map((item) => item.labelKey);

        //expected
        expect(exactLabels).toEqual(['shell.nav.dashboard', 'shell.nav.publications']);
    });

    it('keeps Students highlighted on the Roster import page below it', () => {
        //given
        const students = navigationFor(Role.administrator).find((item) => item.labelKey === 'shell.nav.students');
        const rosterImport = ['/', AppRoutes.students, AppRoutes.rosterImport];

        //expected
        expect(students?.exact).toBe(false);
        expect(rosterImport.slice(0, students?.commands.length)).toEqual(students?.commands);
    });
});
