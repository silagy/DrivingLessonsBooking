import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';

@Component({
    selector: 'app-last-import-card',
    imports: [TranslocoPipe, TagModule],
    templateUrl: './last-import-card.component.html',
    styleUrl: './last-import-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LastImportCardComponent {
    readonly fileName = input.required<string>();
    readonly rows = input.required<number>();
    readonly time = input.required<string>();
}
