import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserRequest } from './create-user.request';
import { CreateUserResponse } from './create-user.response';
import { ItemForFindUsersResponse } from './item-for-find-users.response';

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
}
