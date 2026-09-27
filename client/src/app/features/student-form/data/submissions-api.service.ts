import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationByLinkResponse } from './get-publication-by-link.response';
import { IdentifyStudentRequest } from './identify-student.request';
import { IdentifyStudentResponse } from './identify-student.response';

@Injectable({ providedIn: 'root' })
export class SubmissionsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/submissions';

    getPublicationByLink(token: string): Observable<GetPublicationByLinkResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.get<GetPublicationByLinkResponse>(`${this.baseUrl}/by-link/${encodedToken}`);
    }

    identifyStudent(token: string, request: IdentifyStudentRequest): Observable<IdentifyStudentResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.post<IdentifyStudentResponse>(`${this.baseUrl}/by-link/${encodedToken}/identify`, request);
    }
}
