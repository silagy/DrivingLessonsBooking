import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChangeCarDetailsRequest } from './change-car-details.request';
import { CreateCarRequest } from './create-car.request';
import { CreateCarResponse } from './create-car.response';
import { ItemForFindCarsResponse } from './item-for-find-cars.response';

@Injectable({ providedIn: 'root' })
export class CarsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/cars';

    findCars(): Observable<ItemForFindCarsResponse[]> {
        return this.http.get<ItemForFindCarsResponse[]>(`${this.baseUrl}/find`);
    }

    createCar(request: CreateCarRequest): Observable<CreateCarResponse> {
        return this.http.post<CreateCarResponse>(this.baseUrl, request);
    }

    changeCarDetails(carId: string, request: ChangeCarDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${carId}/details`, request);
    }

    deleteCar(carId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${carId}`);
    }

    assignTeacher(carId: string, teacherId: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${carId}/teachers/${teacherId}`, null);
    }

    unassignTeacher(carId: string, teacherId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${carId}/teachers/${teacherId}`);
    }
}
