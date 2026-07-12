# Task 9 of 12: Teachers feature — domain, data, state

> Part of [US-02–04: Teachers Module](README.md) ([parent plan](../us-02-04-teachers-plan.md)). Requires task 8 complete. Work on branch `3-us-02-04-teachers-module`. **Do not commit at the end of this task** — tasks 9–10 commit together (a store with no consumer is dead code).

## Shared Context

**Goal:** The first full `domain/data/state` feature stack: client models, typed request/response interfaces named identically to the backend DTOs, `TeachersApiService` (HTTP only), and `TeachersStore` (signals only, `resource()` read, `executeCommand` writes).

**Conventions:** DTO names match the backend classes exactly · enums are camelCase string enums · components never see observables — the store consumes them via `firstValueFrom`.

---

**Files:**
- Create: `client/src/app/features/teachers/domain/transmission.enum.ts`, `car.model.ts`, `teacher.model.ts`
- Create: `client/src/app/features/teachers/data/create-teacher.request.ts`, `create-teacher.response.ts`, `add-car.request.ts`, `add-car.response.ts`, `change-teacher-details.request.ts`, `change-car-details.request.ts`, `item-for-find-teachers.response.ts`, `teachers-api.service.ts`
- Create: `client/src/app/features/teachers/state/teachers.store.ts`

- [x] **Step 1: `domain/transmission.enum.ts`**

```typescript
export enum Transmission {
    automatic = 'automatic',
    manual = 'manual',
}
```

- [x] **Step 2: `domain/car.model.ts`**

```typescript
import { Transmission } from './transmission.enum';

export interface Car {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
}
```

- [x] **Step 3: `domain/teacher.model.ts`**

```typescript
import { Car } from './car.model';

export interface Teacher {
    id: string;
    name: string;
    contactEmail: string;
    cars: Car[];
}
```

- [x] **Step 4: Request/response interfaces in `data/`**

`create-teacher.request.ts`:

```typescript
export interface CreateTeacherRequest {
    name: string;
    contactEmail: string;
}
```

`create-teacher.response.ts`:

```typescript
export interface CreateTeacherResponse {
    id: string;
}
```

`add-car.request.ts`:

```typescript
import { Transmission } from '../domain/transmission.enum';

export interface AddCarRequest {
    name: string;
    type: string;
    transmission: Transmission;
}
```

`add-car.response.ts`:

```typescript
export interface AddCarResponse {
    id: string;
}
```

`change-teacher-details.request.ts`:

```typescript
export interface ChangeTeacherDetailsRequest {
    name: string;
    contactEmail: string;
}
```

`change-car-details.request.ts`:

```typescript
import { Transmission } from '../domain/transmission.enum';

export interface ChangeCarDetailsRequest {
    name: string;
    type: string;
    transmission: Transmission;
}
```

`item-for-find-teachers.response.ts`:

```typescript
import { Transmission } from '../domain/transmission.enum';

export interface CarForFindTeachersResponse {
    id: string;
    name: string;
    type: string;
    transmission: Transmission;
}

export interface ItemForFindTeachersResponse {
    id: string;
    name: string;
    contactEmail: string;
    cars: CarForFindTeachersResponse[];
}
```

- [x] **Step 5: `data/teachers-api.service.ts`**

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AddCarRequest } from './add-car.request';
import { AddCarResponse } from './add-car.response';
import { ChangeCarDetailsRequest } from './change-car-details.request';
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

    addCar(teacherId: string, request: AddCarRequest): Observable<AddCarResponse> {
        return this.http.post<AddCarResponse>(`${this.baseUrl}/${teacherId}/cars`, request);
    }

    changeTeacherDetails(teacherId: string, request: ChangeTeacherDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${teacherId}/details`, request);
    }

    changeCarDetails(teacherId: string, carId: string, request: ChangeCarDetailsRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${teacherId}/cars/${carId}`, request);
    }
}
```

- [x] **Step 6: `state/teachers.store.ts`**

Cars are sorted client-side for stable order (owned-collection order from the DB is nondeterministic). The spinner should only show on first load — during a `reload()` the resource keeps serving the previous value.

```typescript
import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { Teacher } from '../domain/teacher.model';
import { AddCarRequest } from '../data/add-car.request';
import { ChangeCarDetailsRequest } from '../data/change-car-details.request';
import { ChangeTeacherDetailsRequest } from '../data/change-teacher-details.request';
import { CreateTeacherRequest } from '../data/create-teacher.request';
import { TeachersApiService } from '../data/teachers-api.service';

@Injectable({ providedIn: 'root' })
export class TeachersStore {
    private readonly api = inject(TeachersApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.api.findTeachers()),
    });

    readonly teachers = computed<Teacher[]>(() => {
        const teachers = this.teachersResource.value() ?? [];

        return teachers.map((teacher) => ({
            ...teacher,
            cars: [...teacher.cars].sort((a, b) => a.name.localeCompare(b.name)),
        }));
    });

    readonly isLoading = computed(() => this.teachersResource.isLoading() && !this.teachersResource.value());
    readonly loadError = computed(() => (this.teachersResource.error() ? 'teachers.loadFailed' : null));
    readonly isEmpty = computed(() => !this.isLoading() && !this.teachers().length);
    readonly isMutating = this.mutating.asReadonly();

    async create(request: CreateTeacherRequest): Promise<void> {
        await this.executeCommand(() => this.api.createTeacher(request), 'teachers.created');
    }

    async addCar(teacherId: string, request: AddCarRequest): Promise<void> {
        await this.executeCommand(() => this.api.addCar(teacherId, request), 'teachers.carAdded');
    }

    async changeDetails(teacherId: string, request: ChangeTeacherDetailsRequest): Promise<void> {
        await this.executeCommand(() => this.api.changeTeacherDetails(teacherId, request), 'teachers.detailsChanged');
    }

    async changeCarDetails(teacherId: string, carId: string, request: ChangeCarDetailsRequest): Promise<void> {
        await this.executeCommand(
            () => this.api.changeCarDetails(teacherId, carId, request),
            'teachers.carChanged',
        );
    }

    private async executeCommand(command: () => Observable<unknown>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.teachersResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}
```

- [x] **Step 7: Verify (no commit)**

Run from `client\`: `npm run build` — expected: success (the store compiles even though nothing consumes it yet; if the linter flags unused code, proceed to task 10 before committing).

---

**Next:** [task-10-teachers-feature-ui.md](task-10-teachers-feature-ui.md)
