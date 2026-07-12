# Task 8 of 10: Week-schedules signal store

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–7 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from `client\` unless noted.

**Files:**
- Create: `client\src\app\features\week-schedules\state\week-schedules.store.ts`

Signals only (rule 10). Copy the `TeachersStore` structure; key differences: parameterized resource (`teacherId` + `weekStart`), create-on-404 inside the loader, silent-success toggles, and a second resource for teacher options.

- [ ] **Step 1: Implement the store**

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { LanguageService } from '../../../core/language.service';
import { SlotWindow } from '../../../shared/models/slot-window.enum';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { WeekSchedulesApiService } from '../data/week-schedules-api.service';
import { GetWeekScheduleResponse } from '../data/get-week-schedule.response';
import { SlotState } from '../domain/slot-state.enum';
import { Slot } from '../domain/slot.model';
import { TeacherOption } from '../domain/teacher-option.model';
import { WeekSchedule } from '../domain/week-schedule.model';
import { buildWeekOptions, WeekOption } from '../domain/week-options';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const TIME_LABEL_LENGTH = 5;

@Injectable({ providedIn: 'root' })
export class WeekSchedulesStore {
  private readonly api = inject(WeekSchedulesApiService);
  private readonly teachersApi = inject(TeacherOptionsApiService);
  private readonly toast = inject(ToastService);
  private readonly language = inject(LanguageService);

  private readonly selectedTeacherIdState = signal<string | null>(null);
  private readonly selectedWeekStartState = signal<string>(defaultWeekStart());
  private readonly mutating = signal(false);

  private readonly teachersResource = resource({
    loader: () => firstValueFrom(this.teachersApi.findTeachers()),
  });

  private readonly scheduleResource = resource({
    params: () => {
      const teacherId = this.selectedTeacherIdState();

      return teacherId ? { teacherId, weekStart: this.selectedWeekStartState() } : undefined;
    },
    loader: ({ params }) => this.loadOrCreate(params.teacherId, params.weekStart),
  });

  readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();
  readonly selectedWeekStart = this.selectedWeekStartState.asReadonly();
  readonly isMutating = this.mutating.asReadonly();

  readonly teachers = computed<TeacherOption[]>(() => {
    const items = this.teachersResource.value() ?? [];

    return items
      .map((item) => ({ id: item.id, name: item.name }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly weekOptions = computed<WeekOption[]>(() => buildWeekOptions(this.language.lang()));

  readonly weekSchedule = computed<WeekSchedule | undefined>(() => this.scheduleResource.value());

  readonly slots = computed<Slot[]>(() => this.weekSchedule()?.slots ?? []);

  readonly windowTimes = computed<Partial<Record<SlotWindow, string>>>(() => {
    const times: Partial<Record<SlotWindow, string>> = {};

    for (const slot of this.slots()) {
      times[slot.window] ??= `${formatTime(slot.startLocal)}–${formatTime(slot.endLocal)}`;
    }

    return times;
  });

  readonly unavailableCount = computed(
    () => this.slots().filter((slot) => slot.state === SlotState.unavailable).length,
  );

  readonly isLoading = computed(() => this.scheduleResource.isLoading() || this.teachersResource.isLoading());

  readonly loadError = computed(() => {
    const failed = this.scheduleResource.error() || this.teachersResource.error();

    return failed ? 'weekSchedules.loadFailed' : undefined;
  });

  readonly hasSelection = computed(() => this.selectedTeacherIdState() !== null);

  selectTeacher(teacherId: string): void {
    this.selectedTeacherIdState.set(teacherId);
  }

  selectWeek(weekStart: string): void {
    this.selectedWeekStartState.set(weekStart);
  }

  async toggleSlot(slot: Slot): Promise<void> {
    const schedule = this.weekSchedule();

    if (!schedule || this.mutating()) {
      return;
    }

    this.mutating.set(true);

    try {
      const command =
        slot.state === SlotState.open
          ? this.api.markSlotUnavailable(schedule.id, slot.id)
          : this.api.markSlotAvailable(schedule.id, slot.id);

      await firstValueFrom(command);
    } catch (error) {
      this.toast.apiError(error);
    } finally {
      this.mutating.set(false);
      this.scheduleResource.reload();
    }
  }

  private async loadOrCreate(teacherId: string, weekStart: string): Promise<GetWeekScheduleResponse> {
    try {
      return await firstValueFrom(this.api.getByTeacherAndWeek(teacherId, weekStart));
    } catch (error) {
      if (!isStatus(error, HTTP_NOT_FOUND)) {
        throw error;
      }
    }

    try {
      await firstValueFrom(this.api.create({ teacherId, weekStart }));
    } catch (error) {
      if (!isStatus(error, HTTP_CONFLICT)) {
        throw error;
      }
    }

    return firstValueFrom(this.api.getByTeacherAndWeek(teacherId, weekStart));
  }
}

function defaultWeekStart(): string {
  const options = buildWeekOptions('en');

  return options[1].weekStart;
}

function isStatus(error: unknown, status: number): boolean {
  return error instanceof HttpErrorResponse && error.status === status;
}

function formatTime(time: string): string {
  return time.slice(0, TIME_LABEL_LENGTH);
}
```

> Adjust import paths to the actual locations (`core\services\toast.service`, `core\language.service`) and match the exact `resource()` usage already present in `TeachersStore` (e.g. if it uses `rxResource` or a different error/loading API in this Angular version, follow the existing code). Default week = options[1] = **next week** (options[0] is the current week).

- [ ] **Step 2: Build, commit**

Run (in `client\`): `npm run build` — success.

```bash
git add client/src/app/features/week-schedules
git commit -m "feat(client): week-schedules signal store with create-on-404 flow"
```

---

**Next:** [task-09-weekly-prep-page.md](task-09-weekly-prep-page.md)
