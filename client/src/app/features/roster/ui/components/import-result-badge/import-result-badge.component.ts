import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { RosterEntryOutcome } from '../../../domain/roster-entry-outcome.enum';

type BadgeSeverity = 'success' | 'info';

const OUTCOME_SEVERITY: Partial<Record<RosterEntryOutcome, BadgeSeverity>> = {
    [RosterEntryOutcome.added]: 'success',
    [RosterEntryOutcome.updated]: 'info',
};

@Component({
    selector: 'app-import-result-badge',
    imports: [TagModule, TranslocoPipe],
    template: `<p-tag [severity]="severity()" [value]="'roster.badge.' + outcome() | transloco" />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportResultBadgeComponent {
    readonly outcome = input.required<RosterEntryOutcome>();

    protected readonly severity = computed<BadgeSeverity>(() => OUTCOME_SEVERITY[this.outcome()] ?? 'info');
}
