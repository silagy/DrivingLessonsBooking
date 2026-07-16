import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatTileTone = 'success' | 'info' | 'neutral' | 'danger';

@Component({
    selector: 'app-roster-stat-tile',
    templateUrl: './roster-stat-tile.component.html',
    styleUrl: './roster-stat-tile.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterStatTileComponent {
    readonly value = input.required<number>();
    readonly label = input.required<string>();
    readonly tone = input.required<StatTileTone>();

    protected readonly rootClass = computed(() => `stat-tile stat-tile--${this.tone()}`);
}
