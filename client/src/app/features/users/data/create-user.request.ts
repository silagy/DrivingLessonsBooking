import { Role } from '../../../shared/models/role.enum';

export interface CreateUserRequest {
    name: string;
    signInEmail: string;
    role: Role;
    teacherId: string | null;
    temporaryPassword: string;
}
