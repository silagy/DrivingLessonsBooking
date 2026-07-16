import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetLatestRosterImportResponse } from './get-latest-roster-import.response';
import { ImportRosterResponse } from './import-roster.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class RosterApiService {
    private readonly http = inject(HttpClient);
    private readonly importsUrl = 'api/roster-imports';
    private readonly studentsUrl = 'api/students';

    importRoster(file: File): Observable<ImportRosterResponse> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<ImportRosterResponse>(this.importsUrl, formData);
    }

    getLatestImport(): Observable<GetLatestRosterImportResponse> {
        return this.http.get<GetLatestRosterImportResponse>(`${this.importsUrl}/latest`);
    }

    findStudents(teacherId?: string): Observable<ItemForFindStudentsResponse[]> {
        let params = new HttpParams();

        if (teacherId) {
            params = params.set('teacherId', teacherId);
        }

        return this.http.get<ItemForFindStudentsResponse[]>(`${this.studentsUrl}/find`, { params });
    }
}
