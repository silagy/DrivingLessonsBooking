import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { IdentifyStatus } from '../../../domain/identify-status.enum';
import { isNationalIdCandidate, toNationalIdDigits } from '../../../domain/national-id-input';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const RESUBMITTABLE_STATUSES: ReadonlySet<IdentifyStatus> = new Set([IdentifyStatus.found, IdentifyStatus.failed]);
const REJECTED_STATUSES: ReadonlySet<IdentifyStatus> = new Set([IdentifyStatus.notOnRoster, IdentifyStatus.invalidId]);

@Component({
    selector: 'app-identify-step',
    imports: [TranslocoPipe, ButtonModule, InputTextModule, MessageModule, WizardStepComponent],
    templateUrl: './identify-step.component.html',
    styleUrl: './identify-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IdentifyStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly status = input.required<IdentifyStatus>();
    readonly studentName = input.required<string>();
    readonly teacherName = input.required<string>();
    readonly weekNumber = input.required<number>();

    readonly nationalIdChanged = output<string>();
    readonly lookupRequested = output<string>();
    readonly continued = output<void>();

    protected readonly statuses = IdentifyStatus;
    protected readonly digits = signal('');

    protected readonly isRejected = computed(() => REJECTED_STATUSES.has(this.status()));
    protected readonly canSubmit = computed(() => {
        const status = this.status();
        const isFreshCandidate = status === IdentifyStatus.idle && isNationalIdCandidate(this.digits());

        return RESUBMITTABLE_STATUSES.has(status) || isFreshCandidate;
    });

    protected onInput(event: Event): void {
        const field = event.target as HTMLInputElement;
        const digits = toNationalIdDigits(field.value);

        this.digits.set(digits);
        this.nationalIdChanged.emit(digits);
    }

    protected onSubmit(event: Event): void {
        event.preventDefault();

        if (!this.canSubmit()) {
            return;
        }

        if (this.status() === IdentifyStatus.found) {
            this.continued.emit();
            return;
        }

        this.lookupRequested.emit(this.digits());
    }
}
