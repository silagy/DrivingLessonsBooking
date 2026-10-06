import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Student } from '../../../domain/student.model';
import { TransmissionTagComponent } from '../transmission-tag/transmission-tag.component';

@Component({
    selector: 'app-student-who-card',
    imports: [TranslocoPipe, InitialsPipe, TransmissionTagComponent],
    templateUrl: './student-who-card.component.html',
    styleUrl: './student-who-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentWhoCardComponent {
    readonly student = input.required<Student>();
}
