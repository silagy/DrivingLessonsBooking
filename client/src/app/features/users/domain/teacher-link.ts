import { LinkableTeacher } from './linkable-teacher.model';
import { Role } from './role.enum';
import { TeacherOption } from './teacher-option.model';
import { User } from './user.model';

export function isTeacherLinkRequired(role: Role): boolean {
    return role === Role.teacher;
}

export function toLinkableTeachers(teachers: TeacherOption[], users: User[]): LinkableTeacher[] {
    const linkedTeacherIds = new Set(users.map((user) => user.teacherId));

    return [...teachers]
        .sort((first, second) => first.name.localeCompare(second.name))
        .map((teacher) => ({
            id: teacher.id,
            name: teacher.name,
            alreadyLinked: linkedTeacherIds.has(teacher.id),
        }));
}
