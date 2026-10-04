import { Role } from '../domain/role.enum';

export interface CreateUserRequest {
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    temporaryPassword: string;
}
