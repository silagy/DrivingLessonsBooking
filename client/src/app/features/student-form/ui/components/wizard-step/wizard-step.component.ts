import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-wizard-step',
    imports: [TranslocoPipe, ButtonModule],
    templateUrl: './wizard-step.component.html',
    styleUrl: './wizard-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly heading = input<string>('');
    readonly intro = input<string>('');
    readonly canGoBack = input(false);

    readonly back = output<void>();
}
