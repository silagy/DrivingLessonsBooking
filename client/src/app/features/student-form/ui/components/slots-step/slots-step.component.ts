import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { PickSheet } from '../../../domain/pick-sheet';
import { SlotDay } from '../../../domain/slot-day';
import { PickChoice } from '../../../domain/slot-pick';
import { PickSheetComponent } from '../pick-sheet/pick-sheet.component';
import { SlotDayListComponent } from '../slot-day-list/slot-day-list.component';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-slots-step',
    imports: [TranslocoPipe, ButtonModule, PickSheetComponent, SlotDayListComponent, WizardStepComponent],
    templateUrl: './slots-step.component.html',
    styleUrl: './slots-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly days = input.required<readonly SlotDay[]>();
    readonly targetCount = input.required<number>();
    readonly pickCount = input.required<number>();
    readonly pickSheet = input.required<PickSheet | null>();

    readonly chipSelected = output<string>();
    readonly pickSaved = output<PickChoice>();
    readonly pickRemoved = output<void>();
    readonly pickCancelled = output<void>();
    readonly reviewed = output<void>();
    readonly back = output<void>();

    protected readonly isTargetCovered = computed(() => this.pickCount() >= this.targetCount());
}
