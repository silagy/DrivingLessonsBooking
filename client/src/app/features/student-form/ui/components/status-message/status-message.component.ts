import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatusMessageTone = 'neutral' | 'success' | 'danger';

@Component({
    selector: 'app-status-message',
    templateUrl: './status-message.component.html',
    styleUrl: './status-message.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusMessageComponent {
    readonly icon = input.required<string>();
    readonly heading = input.required<string>();
    readonly tone = input<StatusMessageTone>('neutral');

    protected readonly toneClass = computed(() => `status__icon--${this.tone()}`);
}
