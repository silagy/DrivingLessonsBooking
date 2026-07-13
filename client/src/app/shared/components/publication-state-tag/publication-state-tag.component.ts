import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { PublicationState } from '../../models/publication-state.enum';

type TagSeverity = 'secondary' | 'info' | 'success' | 'danger';

const STATE_SEVERITY: Record<PublicationState, TagSeverity> = {
    [PublicationState.draft]: 'secondary',
    [PublicationState.published]: 'info',
    [PublicationState.open]: 'success',
    [PublicationState.closed]: 'danger',
};

@Component({
    selector: 'app-publication-state-tag',
    imports: [TagModule, TranslocoPipe],
    template: `<p-tag [severity]="severity()" [value]="'publications.state.' + state() | transloco" />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationStateTagComponent {
    readonly state = input.required<PublicationState>();

    protected readonly severity = computed<TagSeverity>(() => STATE_SEVERITY[this.state()]);
}
