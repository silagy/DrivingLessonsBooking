import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'app-publication-stat-chip',
    templateUrl: './publication-stat-chip.component.html',
    styleUrl: './publication-stat-chip.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationStatChipComponent {
    readonly value = input.required<string>();
    readonly label = input.required<string>();
}
