import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    inject,
    Injector,
    input,
    output,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ReviewItem } from '../../../domain/review-item';
import { SessionType } from '../../../domain/session-type.enum';
import { PickMove } from '../../../domain/slot-pick';
import { SubmitStatus } from '../../../domain/submit-status.enum';
import { WizardStepComponent } from '../wizard-step/wizard-step.component';

const SINGLE_PREFERRED_TARGET = 1;
const SINGLE_DROPPED_PICK = 1;
const MOVE_UP_BUTTON = '.review__move-up button';
const MOVE_DOWN_BUTTON = '.review__move-down button';

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
    readonly canReorder = input.required<boolean>();
    readonly movedPickRank = input.required<number | null>();

    readonly submitted = output<void>();
    readonly addSlots = output<void>();
    readonly changeTarget = output<void>();
    readonly recheck = output<void>();
    readonly back = output<void>();
    readonly moved = output<PickMove>();

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly injector = inject(Injector);

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

    protected moveUp(item: ReviewItem): void {
        this.move(item.slotId, positionOf(item) - 1, [MOVE_UP_BUTTON, MOVE_DOWN_BUTTON]);
    }

    protected moveDown(item: ReviewItem): void {
        this.move(item.slotId, positionOf(item) + 1, [MOVE_DOWN_BUTTON, MOVE_UP_BUTTON]);
    }

    private move(slotId: string, toIndex: number, focusOrder: readonly string[]): void {
        this.moved.emit({ slotId, toIndex });
        afterNextRender(() => this.focusMoveButton(slotId, focusOrder), { injector: this.injector });
    }

    private focusMoveButton(slotId: string, focusOrder: readonly string[]): void {
        const row = this.host.nativeElement.querySelector(`[data-pick-id="${slotId}"]`);
        const buttons = focusOrder.map(selector => row?.querySelector<HTMLButtonElement>(selector));

        buttons.find(button => button && !button.disabled)?.focus();
    }
}

function positionOf(item: ReviewItem): number {
    return item.rank - 1;
}
