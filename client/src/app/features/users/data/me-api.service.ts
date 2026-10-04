import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeMyPasswordRequest } from './change-my-password.request';
import { ChangeMyPasswordResponse } from './change-my-password.response';

@Injectable({ providedIn: 'root' })
export class MeApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/me';

    changeMyPassword(request: ChangeMyPasswordRequest): Observable<ChangeMyPasswordResponse> {
        return this.http.put<ChangeMyPasswordResponse>(`${this.baseUrl}/password`, request);
    }
}
