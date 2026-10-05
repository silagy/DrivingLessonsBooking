import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ItemForFindCarsResponse } from './item-for-find-cars.response';

@Injectable({ providedIn: 'root' })
export class CarOptionsApiService {
    private readonly http = inject(HttpClient);

    findCars(): Observable<ItemForFindCarsResponse[]> {
        return this.http.get<ItemForFindCarsResponse[]>('api/cars/find');
    }
}
