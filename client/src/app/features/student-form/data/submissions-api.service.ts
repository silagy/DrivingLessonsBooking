import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationByLinkResponse } from './get-publication-by-link.response';

@Injectable({ providedIn: 'root' })
export class SubmissionsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/submissions';

    getPublicationByLink(token: string): Observable<GetPublicationByLinkResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.get<GetPublicationByLinkResponse>(`${this.baseUrl}/by-link/${encodedToken}`);
    }
}
