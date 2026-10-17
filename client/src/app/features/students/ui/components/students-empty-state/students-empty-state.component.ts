import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-students-empty-state',
    imports: [TranslocoPipe],
    templateUrl: './students-empty-state.component.html',
    styleUrl: './students-empty-state.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentsEmptyStateComponent {
    readonly icon = input.required<string>();
    readonly titleKey = input.required<string>();
    readonly hintKey = input.required<string>();
}
