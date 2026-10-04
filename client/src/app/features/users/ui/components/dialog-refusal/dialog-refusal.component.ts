import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { MessageModule } from 'primeng/message';

@Component({
    selector: 'app-dialog-refusal',
    imports: [TranslocoPipe, MessageModule],
    templateUrl: './dialog-refusal.component.html',
    styleUrl: './dialog-refusal.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DialogRefusalComponent {
    readonly message = input.required<string>();
}
