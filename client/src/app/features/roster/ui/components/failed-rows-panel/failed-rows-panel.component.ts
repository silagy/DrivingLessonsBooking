import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { FailureForGetLatestRosterImportResponse } from '../../../data/get-latest-roster-import.response';

@Component({
    selector: 'app-failed-rows-panel',
    imports: [TranslocoPipe],
    templateUrl: './failed-rows-panel.component.html',
    styleUrl: './failed-rows-panel.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FailedRowsPanelComponent {
    readonly failures = input.required<FailureForGetLatestRosterImportResponse[]>();
}
