import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ExtendPublicationWindowRequest } from './extend-publication-window.request';
import { GetPublicationDashboardResponse } from './get-publication-dashboard.response';
import { GetPublicationResponse } from './get-publication.response';
import { ItemForFindPublicationHistoryResponse } from './item-for-find-publication-history.response';
import { PublishPublicationRequest } from './publish-publication.request';
import { ReopenPublicationRequest } from './reopen-publication.request';

@Injectable({ providedIn: 'root' })
export class PublicationsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/publications';

    getByWeek(week: string): Observable<GetPublicationResponse> {
        const params = new HttpParams().set('week', week);

        return this.http.get<GetPublicationResponse>(`${this.baseUrl}/by-week`, { params });
    }

    publish(id: string, request: PublishPublicationRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/publish`, request);
    }

    extendWindow(id: string, request: ExtendPublicationWindowRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/extend-window`, request);
    }

    reopen(id: string, request: ReopenPublicationRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/reopen`, request);
    }

    getDashboard(id: string, teacherId: string): Observable<GetPublicationDashboardResponse> {
        const params = new HttpParams().set('teacherId', teacherId);

        return this.http.get<GetPublicationDashboardResponse>(`${this.baseUrl}/${id}/dashboard`, { params });
    }

    findHistory(): Observable<ItemForFindPublicationHistoryResponse[]> {
        return this.http.get<ItemForFindPublicationHistoryResponse[]>(`${this.baseUrl}/history`);
    }

    downloadExcel(id: string, teacherId: string): Observable<Blob> {
        const params = new HttpParams().set('teacherId', teacherId);

        return this.http.get(`${this.baseUrl}/${id}/excel`, { params, responseType: 'blob' });
    }
}
