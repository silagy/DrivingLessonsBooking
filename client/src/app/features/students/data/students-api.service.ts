import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateStudentRequest } from './create-student.request';
import { CreateStudentResponse } from './create-student.response';
import { ItemForFindStudentsResponse } from './item-for-find-students.response';

@Injectable({ providedIn: 'root' })
export class StudentsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/students';

    findStudents(): Observable<ItemForFindStudentsResponse[]> {
        return this.http.get<ItemForFindStudentsResponse[]>(`${this.baseUrl}/find`);
    }

    createStudent(request: CreateStudentRequest): Observable<CreateStudentResponse> {
        return this.http.post<CreateStudentResponse>(this.baseUrl, request);
    }
}
