import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { WeekGridComponent } from '../../../../../shared/components/week-grid/week-grid.component';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { SlotState } from '../../../../../shared/models/slot-state.enum';
import { WeekSchedulesStore } from '../../../state/week-schedules.store';

@Component({
    selector: 'app-weekly-prep-page',
    imports: [
        FormsModule,
        TranslocoPipe,
        ProgressSpinnerModule,
        SelectModule,
        WeekGridComponent,
        ButtonModule,
        RouterLink,
        PublicationStateTagComponent,
    ],
    templateUrl: './weekly-prep.page.html',
    styleUrl: './weekly-prep.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyPrepPage {
    protected readonly store = inject(WeekSchedulesStore);
    protected readonly SlotState = SlotState;
    protected readonly appRoutes = AppRoutes;
}
