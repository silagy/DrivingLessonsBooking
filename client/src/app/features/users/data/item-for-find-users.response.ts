import { Role } from '../domain/role.enum';

export interface ItemForFindUsersResponse {
    id: string;
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    teacherName: string | null;
    isDeleted: boolean;
}
