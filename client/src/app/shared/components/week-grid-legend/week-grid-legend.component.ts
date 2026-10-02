import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
    selector: 'app-week-grid-legend',
    imports: [TranslocoPipe],
    templateUrl: './week-grid-legend.component.html',
    styleUrl: './week-grid-legend.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeekGridLegendComponent {}
