import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { CarRowComponent } from '../car-row/car-row.component';

@Component({
    selector: 'app-teacher-card',
    imports: [TranslocoPipe, ButtonModule, CarRowComponent],
    templateUrl: './teacher-card.component.html',
    styleUrl: './teacher-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherCardComponent {
    readonly teacher = input.required<Teacher>();

    readonly edit = output<void>();
    readonly addCar = output<void>();
    readonly editCar = output<Car>();

    protected readonly initials = computed(() => {
        const parts = this.teacher().name.split(' ');
        const first = parts[0]?.charAt(0) ?? '';
        const second = parts[1]?.charAt(0) ?? '';
        const combined = `${first}${second}` || this.teacher().name.slice(0, 2);

        return combined.toUpperCase();
    });
}
