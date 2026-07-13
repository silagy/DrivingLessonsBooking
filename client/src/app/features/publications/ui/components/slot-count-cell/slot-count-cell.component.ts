import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotState } from '../../../../../shared/models/slot-state.enum';

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

    protected readonly isUnavailable = computed(() => this.slotState() === SlotState.unavailable);
    protected readonly isEmpty = computed(() => this.requestCount() === 0);
}
