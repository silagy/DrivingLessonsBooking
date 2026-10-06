import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeStudentDetailsRequest } from './change-student-details.request';
import { CreateStudentRequest } from './create-student.request';
import { CreateStudentResponse } from './create-student.response';
import { GetStudentResponse } from './get-student.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class StudentsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/students';

    findStudents(): Observable<ItemForFindStudentsResponse[]> {
        return this.http.get<ItemForFindStudentsResponse[]>(`${this.baseUrl}/find`);
    }

    getStudent(studentId: string): Observable<GetStudentResponse> {
        return this.http.get<GetStudentResponse>(`${this.baseUrl}/${studentId}`);
    }

    createStudent(request: CreateStudentRequest): Observable<CreateStudentResponse> {
        return this.http.post<CreateStudentResponse>(this.baseUrl, request);
    }

    changeStudentDetails(studentId: string, request: ChangeStudentDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${studentId}/details`, request);
    }

    deactivateStudent(studentId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/deactivate`, {});
    }

    reactivateStudent(studentId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${studentId}/reactivate`, {});
    }
}
