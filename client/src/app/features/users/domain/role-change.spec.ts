import { Role } from '../../../shared/models/role.enum';
import { otherRole, roleChangeNoteKey } from './role-change';

describe('role change', () => {
    it('offers the other Role', () => {
        //expected
        expect(otherRole(Role.administrator)).toBe(Role.teacher);
        expect(otherRole(Role.teacher)).toBe(Role.administrator);
    });

    it('explains that the Teacher link is kept when the new Role is Administrator', () => {
        //expected
        expect(roleChangeNoteKey(Role.administrator, 'Dana Levi')).toBe('users.promoteNote');
    });

    it('explains what a linked User sees with the Teacher Role', () => {
        //expected
        expect(roleChangeNoteKey(Role.teacher, 'Ronit Avraham')).toBe('users.demoteNote');
    });

    it('has no note for a User without a linked Teacher', () => {
        //expected
        expect(roleChangeNoteKey(Role.teacher, null)).toBeNull();
        expect(roleChangeNoteKey(Role.administrator, null)).toBeNull();
    });
});
