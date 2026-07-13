import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { Transmission } from '../../../domain/transmission.enum';
import { AssignTeachersPopoverComponent } from '../assign-teachers-popover/assign-teachers-popover.component';

@Component({
    selector: 'app-car-card',
    imports: [TranslocoPipe, ButtonModule, AssignTeachersPopoverComponent],
    templateUrl: './car-card.component.html',
    styleUrl: './car-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarCardComponent {
    readonly car = input.required<Car>();
    readonly teachers = input.required<Teacher[]>();

    readonly edit = output<void>();
    readonly remove = output<void>();
    readonly assignApplied = output<string[]>();

    protected readonly isManual = computed(() => this.car().transmission === Transmission.manual);
    protected readonly isShared = computed(() => this.car().assignedTeachers.length > 1);
    protected readonly assignedIds = computed(() => this.car().assignedTeachers.map((teacher) => teacher.id));
}
