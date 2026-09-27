import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { MessageModule } from 'primeng/message';
import { Transmission } from '../../../domain/transmission.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

@Component({
    selector: 'app-details-step',
    imports: [TranslocoPipe, MessageModule, WizardStepComponent],
    templateUrl: './details-step.component.html',
    styleUrl: './details-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DetailsStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly teacherName = input.required<string>();
    readonly teacherInitials = input.required<string>();
    readonly studentName = input.required<string>();
    readonly carName = input.required<string>();
    readonly transmission = input.required<Transmission>();
    readonly weekLabel = input.required<string>();
    readonly hasAvailability = input.required<boolean>();

    protected readonly transmissionKey = computed(() => `studentForm.details.transmissions.${this.transmission()}`);
}
