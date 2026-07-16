import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-roster-page',
    imports: [TranslocoPipe],
    templateUrl: './roster.page.html',
    styleUrl: './roster.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterPage {}
