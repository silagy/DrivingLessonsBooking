import { ChangeDetectionStrategy, Component, computed, contentChild, inject, input, TemplateRef } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguageService } from '../../../core/language.service';
import { DayOfWeek } from '../../models/day-of-week.enum';
import { SlotWindow } from '../../models/slot-window.enum';
import { WeekGridCell } from '../../models/week-grid-cell';

export interface WeekGridCellContext<T extends WeekGridCell> {
    $implicit: T;
}

const GRID_DAYS: readonly DayOfWeek[] = [
    DayOfWeek.sunday,
    DayOfWeek.monday,
    DayOfWeek.tuesday,
    DayOfWeek.wednesday,
    DayOfWeek.thursday,
    DayOfWeek.friday,
];

const GRID_WINDOWS: readonly SlotWindow[] = [
    SlotWindow.morning,
    SlotWindow.noon,
    SlotWindow.afternoon,
    SlotWindow.evening,
];

@Component({
    selector: 'app-week-grid',
    imports: [NgTemplateOutlet, TranslocoPipe],
    templateUrl: './week-grid.component.html',
    styleUrl: './week-grid.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeekGridComponent<T extends WeekGridCell> {
    private readonly language = inject(LanguageService);

    readonly cells = input.required<readonly T[]>();
    readonly weekStart = input<string>();
    readonly windowTimes = input<Partial<Record<SlotWindow, string>>>({});

    readonly cellTemplate = contentChild.required<TemplateRef<WeekGridCellContext<T>>>(TemplateRef);

    protected readonly days = GRID_DAYS;
    protected readonly windows = GRID_WINDOWS;

    protected readonly cellMap = computed(() => {
        const map = new Map<string, T>();
        for (const cell of this.cells()) {
            map.set(`${cell.day}-${cell.window}`, cell);
        }
        return map;
    });

    protected cellFor(day: DayOfWeek, window: SlotWindow): T | undefined {
        return this.cellMap().get(`${day}-${window}`);
    }

    protected dateFor(day: DayOfWeek): string | undefined {
        const weekStart = this.weekStart();
        if (!weekStart) {
            return undefined;
        }
        const date = new Date(weekStart);
        date.setDate(date.getDate() + this.days.indexOf(day));
        return date.toLocaleDateString(this.language.lang(), { day: 'numeric', month: 'numeric' });
    }
}
