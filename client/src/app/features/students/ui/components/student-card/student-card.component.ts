import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { CarMarker } from '../../../domain/car-marker.enum';
import { RemovedPart } from '../../../domain/removed-part.enum';
import { StudentRow } from '../../../domain/student-row.model';
import { CarFlagComponent } from '../car-flag/car-flag.component';
import { DeletedFlagComponent } from '../deleted-flag/deleted-flag.component';
import { TransmissionTagComponent } from '../transmission-tag/transmission-tag.component';

@Component({
    selector: 'app-student-card',
    imports: [
        TranslocoPipe,
        InitialsPipe,
        ButtonModule,
        TagModule,
        TransmissionTagComponent,
        CarFlagComponent,
        DeletedFlagComponent,
    ],
    templateUrl: './student-card.component.html',
    styleUrl: './student-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentCardComponent {
    readonly student = input.required<StudentRow>();
    readonly disabled = input(false);

    readonly actionsRequested = output<Event>();

    protected readonly carMarkers = CarMarker;
    protected readonly removedParts = RemovedPart;
}
