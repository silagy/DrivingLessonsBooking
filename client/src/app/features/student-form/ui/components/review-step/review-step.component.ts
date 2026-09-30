import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ReviewItem } from '../../../domain/review-item';
import { SessionType } from '../../../domain/session-type.enum';
import { SubmitStatus } from '../../../domain/submit-status.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const SINGLE_PREFERRED_TARGET = 1;
const SINGLE_DROPPED_PICK = 1;

@Component({
    selector: 'app-review-step',
    imports: [TranslocoPipe, ButtonModule, MessageModule, WizardStepComponent],
    templateUrl: './review-step.component.html',
    styleUrl: './review-step.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewStepComponent {
    readonly stepNumber = input.required<number>();
    readonly stepCount = input.required<number>();
    readonly targetCount = input.required<number>();
    readonly pickCount = input.required<number>();
    readonly items = input.required<readonly ReviewItem[]>();
    readonly missingPicks = input.required<number>();
    readonly replacesEarlierSubmission = input.required<boolean>();
    readonly droppedPickCount = input.required<number>();
    readonly submitStatus = input.required<SubmitStatus>();
    readonly canSubmit = input.required<boolean>();

    readonly submitted = output<void>();
    readonly addSlots = output<void>();
    readonly changeTarget = output<void>();
    readonly recheck = output<void>();
    readonly back = output<void>();

    protected readonly statuses = SubmitStatus;
    protected readonly sessionTypes = SessionType;
    protected readonly isSubmitting = computed(() => this.submitStatus() === SubmitStatus.submitting);
    protected readonly preferredKey = computed(() =>
        this.targetCount() === SINGLE_PREFERRED_TARGET
            ? 'studentForm.review.preferredOne'
            : 'studentForm.review.preferredMany',
    );
    protected readonly droppedKey = computed(() =>
        this.droppedPickCount() === SINGLE_DROPPED_PICK ? 'studentForm.review.droppedOne' : 'studentForm.review.droppedMany',
    );
}
