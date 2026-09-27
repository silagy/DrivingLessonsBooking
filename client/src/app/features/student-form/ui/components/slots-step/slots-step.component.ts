import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotDay } from '../../../domain/slot-day';
import { SlotDayListComponent } from '../slot-day-list/slot-day-list.component';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-slots-step',
    imports: [TranslocoPipe, SlotDayListComponent, WizardStepComponent],
    templateUrl: './slots-step.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly teacherName = input.required<string>();
    readonly days = input.required<readonly SlotDay[]>();
}
