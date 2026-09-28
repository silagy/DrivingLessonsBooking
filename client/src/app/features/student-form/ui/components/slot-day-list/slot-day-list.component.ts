import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotDay } from '../../../domain/slot-day';

@Component({
    selector: 'app-slot-day-list',
    imports: [TranslocoPipe],
    templateUrl: './slot-day-list.component.html',
    styleUrl: './slot-day-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotDayListComponent {
    readonly days = input.required<readonly SlotDay[]>();

    readonly chipSelected = output<string>();
}
