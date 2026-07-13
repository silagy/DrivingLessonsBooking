import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationResponse } from './get-publication.response';

@Injectable({ providedIn: 'root' })
export class PublicationStatusApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/publications';

    getByWeek(week: string): Observable<GetPublicationResponse> {
        const params = new HttpParams().set('week', week);

        return this.http.get<GetPublicationResponse>(`${this.baseUrl}/by-week`, { params });
    }
}
