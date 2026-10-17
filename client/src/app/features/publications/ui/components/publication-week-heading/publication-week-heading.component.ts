import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { WeekOption } from '../../../domain/week-options';
import { WindowStatus } from '../../../domain/window-status.model';

@Component({
    selector: 'app-publication-week-heading',
    imports: [FormsModule, TranslocoPipe, ButtonModule, SelectModule, PublicationStateTagComponent],
    templateUrl: './publication-week-heading.component.html',
    styleUrl: './publication-week-heading.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationWeekHeadingComponent {
    readonly teachers = input.required<TeacherOption[]>();
    readonly selectedTeacherId = input<string | null>(null);
    readonly canChooseTeacher = input(false);
    readonly weekNumber = input<number>();
    readonly state = input<PublicationState>();
    readonly weekChoices = input.required<WeekOption[]>();
    readonly selectedWeekStart = input.required<string>();
    readonly windowStatus = input<WindowStatus>();
    readonly canExtend = input(false);

    readonly teacherSelected = output<string>();
    readonly weekPicked = output<string>();
    readonly extendRequested = output<void>();
}
