import { Role } from '../../../shared/models/role.enum';
import { isTeacherLinkRequired, toLinkableTeachers } from './teacher-link';
import { TeacherOption } from './teacher-option.model';
import { User } from './user.model';

const DANA: TeacherOption = { id: 'teacher-levi', name: 'Dana Levi' };
const AVI: TeacherOption = { id: 'teacher-cohen', name: 'Avi Cohen' };

function userLinkedTo(teacherId: string | null, isDeleted: boolean): User {
    return {
        id: `user-for-${teacherId}`,
        name: 'Some User',
        signInEmail: 'user@school.example',
        role: Role.teacher,
        teacherId,
        teacherName: null,
        isDeleted,
    };
}

describe('isTeacherLinkRequired', () => {
    it('requires a Teacher for the Teacher Role', () => {
        //expected
        expect(isTeacherLinkRequired(Role.teacher)).toBe(true);
    });

    it('leaves the Teacher optional for an Administrator', () => {
        //expected
        expect(isTeacherLinkRequired(Role.administrator)).toBe(false);
    });
});

describe('toLinkableTeachers', () => {
    it('marks a Teacher that already has a User', () => {
        //given
        const users = [userLinkedTo(DANA.id, false)];

        //when
        const linkable = toLinkableTeachers([DANA], users);

        //then
        expect(linkable).toEqual([{ id: DANA.id, name: DANA.name, alreadyLinked: true }]);
    });

    it('still marks a Teacher whose User is deleted, because the link stays', () => {
        //given
        const users = [userLinkedTo(DANA.id, true)];

        //when
        const linkable = toLinkableTeachers([DANA], users);

        //then
        expect(linkable[0].alreadyLinked).toBe(true);
    });

    it('keeps a Teacher without a User selectable, even next to an unlinked Administrator', () => {
        //given
        const users = [userLinkedTo(null, false)];

        //when
        const linkable = toLinkableTeachers([AVI], users);

        //then
        expect(linkable).toEqual([{ id: AVI.id, name: AVI.name, alreadyLinked: false }]);
    });

    it('orders the Teachers by name', () => {
        //when
        const linkable = toLinkableTeachers([DANA, AVI], []);

        //then
        expect(linkable.map((teacher) => teacher.id)).toEqual([AVI.id, DANA.id]);
    });
});
