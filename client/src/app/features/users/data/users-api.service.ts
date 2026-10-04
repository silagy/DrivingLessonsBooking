import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeUserDetailsRequest } from './change-user-details.request';
import { ChangeUserRoleRequest } from './change-user-role.request';
import { CreateUserRequest } from './create-user.request';
import { CreateUserResponse } from './create-user.response';
import { ItemForFindUsersResponse } from './item-for-find-users.response';
import { SetUserTemporaryPasswordRequest } from './set-user-temporary-password.request';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/users';

    findUsers(): Observable<ItemForFindUsersResponse[]> {
        return this.http.get<ItemForFindUsersResponse[]>(`${this.baseUrl}/find`);
    }

    createUser(request: CreateUserRequest): Observable<CreateUserResponse> {
        return this.http.post<CreateUserResponse>(this.baseUrl, request);
    }

    deleteUser(userId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${userId}`);
    }

    restoreUser(userId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${userId}/restore`, {});
    }

    changeUserDetails(userId: string, request: ChangeUserDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/details`, request);
    }

    changeUserRole(userId: string, request: ChangeUserRoleRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/role`, request);
    }

    setUserTemporaryPassword(userId: string, request: SetUserTemporaryPasswordRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${userId}/temporary-password`, request);
    }
}
