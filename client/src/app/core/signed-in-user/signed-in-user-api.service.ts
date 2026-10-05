import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetUserResponse } from './get-user.response';

@Injectable({ providedIn: 'root' })
export class SignedInUserApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/me';

    getMe(): Observable<GetUserResponse> {
        return this.http.get<GetUserResponse>(this.baseUrl);
    }
}
