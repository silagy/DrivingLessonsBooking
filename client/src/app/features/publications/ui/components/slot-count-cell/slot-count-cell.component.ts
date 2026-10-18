import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { DayOfWeek } from '../../../../../shared/models/day-of-week.enum';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';

@Component({
    selector: 'app-slot-count-cell',
    imports: [TranslocoPipe],
    templateUrl: './slot-count-cell.component.html',
    styleUrl: './slot-count-cell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotCountCellComponent {
    readonly requestCount = input.required<number>();
    readonly slotState = input.required<SlotState>();
    readonly day = input.required<DayOfWeek>();
    readonly window = input.required<SlotWindow>();
    readonly selected = input(false);

    readonly opened = output<HTMLElement>();

    protected readonly isUnavailable = computed(() => this.slotState() === SlotState.unavailable);
    protected readonly isEmpty = computed(() => this.requestCount() === 0);
    protected readonly isOpenable = computed(() => !this.isUnavailable() && !this.isEmpty());
}
