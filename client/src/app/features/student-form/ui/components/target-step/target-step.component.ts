import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const SINGLE_LESSON = 1;

@Component({
    selector: 'app-target-step',
    imports: [TranslocoPipe, ButtonModule, WizardStepComponent],
    templateUrl: './target-step.component.html',
    styleUrl: './target-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TargetStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly targetCount = input.required<number>();
    readonly minTargetCount = input.required<number>();

    readonly increased = output<void>();
    readonly decreased = output<void>();
    readonly continued = output<void>();
    readonly back = output<void>();

    protected readonly isAtMinimum = computed(() => this.targetCount() <= this.minTargetCount());
    protected readonly unitKey = computed(() =>
        this.targetCount() === SINGLE_LESSON ? 'studentForm.target.unitOne' : 'studentForm.target.unit',
    );
}
