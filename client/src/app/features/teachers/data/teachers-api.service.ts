import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChangeTeacherDetailsRequest } from './change-teacher-details.request';
import { CreateTeacherRequest } from './create-teacher.request';
import { CreateTeacherResponse } from './create-teacher.response';
import { ItemForFindTeachersResponse } from './item-for-find-teachers.response';

@Injectable({ providedIn: 'root' })
export class TeachersApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/teachers';

    findTeachers(): Observable<ItemForFindTeachersResponse[]> {
        return this.http.get<ItemForFindTeachersResponse[]>(`${this.baseUrl}/find`);
    }

    createTeacher(request: CreateTeacherRequest): Observable<CreateTeacherResponse> {
        return this.http.post<CreateTeacherResponse>(this.baseUrl, request);
    }

    changeTeacherDetails(teacherId: string, request: ChangeTeacherDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${teacherId}/details`, request);
    }

    deleteTeacher(teacherId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${teacherId}`);
    }
}
