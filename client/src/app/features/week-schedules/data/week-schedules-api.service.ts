import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateWeekScheduleRequest } from './create-week-schedule.request';
import { CreateWeekScheduleResponse } from './create-week-schedule.response';
import { GetWeekScheduleResponse } from './get-week-schedule.response';

@Injectable({ providedIn: 'root' })
export class WeekSchedulesApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/week-schedules';

    getByTeacherAndWeek(teacherId: string, week: string): Observable<GetWeekScheduleResponse> {
        const params = new HttpParams().set('teacherId', teacherId).set('week', week);

        return this.http.get<GetWeekScheduleResponse>(`${this.baseUrl}/by-teacher-and-week`, { params });
    }

    create(request: CreateWeekScheduleRequest): Observable<CreateWeekScheduleResponse> {
        return this.http.post<CreateWeekScheduleResponse>(this.baseUrl, request);
    }

    markSlotUnavailable(weekScheduleId: string, slotId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${weekScheduleId}/slots/${slotId}/mark-unavailable`, null);
    }

    markSlotAvailable(weekScheduleId: string, slotId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${weekScheduleId}/slots/${slotId}/mark-available`, null);
    }
}
