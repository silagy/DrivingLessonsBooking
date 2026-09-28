import { DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    inject,
    input,
    linkedSignal,
    output,
    viewChild,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { FocusTrapModule } from 'primeng/focustrap';
import { TextareaModule } from 'primeng/textarea';
import { PickSheet } from '../../../domain/pick-sheet';
import { SessionType } from '../../../domain/session-type.enum';
import { SLOT_CONSTRAINT_MAX_LENGTH, toSlotConstraint } from '../../../domain/slot-constraint';
import { PickChoice } from '../../../domain/slot-pick';

@Component({
    selector: 'app-pick-sheet',
    imports: [TranslocoPipe, ButtonModule, FocusTrapModule, TextareaModule],
    templateUrl: './pick-sheet.component.html',
    styleUrl: './pick-sheet.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(document:keydown.escape)': 'cancelled.emit()' },
})
export class PickSheetComponent {
    readonly sheet = input.required<PickSheet>();

    readonly saved = output<PickChoice>();
    readonly removed = output<void>();
    readonly cancelled = output<void>();

    protected readonly sessionTypes = SessionType;
    protected readonly maxConstraintLength = SLOT_CONSTRAINT_MAX_LENGTH;
    protected readonly sessionType = linkedSignal(() => this.sheet().choice.sessionType);
    protected readonly constraintText = linkedSignal(() => this.sheet().choice.constraint ?? '');

    private readonly singleChoice = viewChild.required<ElementRef<HTMLInputElement>>('singleChoice');
    private readonly doubleChoice = viewChild.required<ElementRef<HTMLInputElement>>('doubleChoice');
    private readonly opener = inject(DOCUMENT).activeElement as HTMLElement | null;

    constructor() {
        afterNextRender(() => this.chosenSessionTypeField().focus());
        inject(DestroyRef).onDestroy(() => this.opener?.focus({ preventScroll: true }));
    }

    protected chooseSessionType(sessionType: SessionType): void {
        this.sessionType.set(sessionType);
    }

    protected onConstraintInput(event: Event): void {
        const field = event.target as HTMLTextAreaElement;
        this.constraintText.set(field.value);
    }

    protected save(): void {
        const choice = {
            sessionType: this.sessionType(),
            constraint: toSlotConstraint(this.constraintText()),
        };

        this.saved.emit(choice);
    }

    private chosenSessionTypeField(): HTMLInputElement {
        const field = this.sessionType() === SessionType.double ? this.doubleChoice() : this.singleChoice();

        return field.nativeElement;
    }
}
