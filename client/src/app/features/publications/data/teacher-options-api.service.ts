import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ItemForFindTeachersResponse } from './item-for-find-teachers.response';

@Injectable({ providedIn: 'root' })
export class TeacherOptionsApiService {
    private readonly http = inject(HttpClient);

    findTeachers(): Observable<ItemForFindTeachersResponse[]> {
        return this.http.get<ItemForFindTeachersResponse[]>('api/teachers/find');
    }
}
