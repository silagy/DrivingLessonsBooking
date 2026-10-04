import { Role } from './role.enum';

export function otherRole(role: Role): Role {
    return role === Role.administrator ? Role.teacher : Role.administrator;
}

export function roleChangeNoteKey(newRole: Role, teacherName: string | null): string | null {
    if (!teacherName) {
        return null;
    }

    return newRole === Role.administrator ? 'users.promoteNote' : 'users.demoteNote';
}
