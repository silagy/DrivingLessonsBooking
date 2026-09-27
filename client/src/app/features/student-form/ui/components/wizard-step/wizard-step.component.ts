import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-wizard-step',
    imports: [TranslocoPipe],
    templateUrl: './wizard-step.component.html',
    styleUrl: './wizard-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly heading = input<string>('');
    readonly intro = input<string>('');
}
