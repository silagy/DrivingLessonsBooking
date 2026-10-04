import { Role } from './role.enum';

export interface User {
    id: string;
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    teacherName: string | null;
    isDeleted: boolean;
}
