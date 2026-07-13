import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { Car } from '../../../domain/car.model';
import { Teacher } from '../../../domain/teacher.model';
import { Transmission } from '../../../domain/transmission.enum';

interface CarChip {
    id: string;
    name: string;
    transmissionKey: string;
}

@Component({
    selector: 'app-teacher-card',
    imports: [TranslocoPipe, ButtonModule],
    templateUrl: './teacher-card.component.html',
    styleUrl: './teacher-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherCardComponent {
    readonly teacher = input.required<Teacher>();
    readonly cars = input.required<Car[]>();

    readonly edit = output<void>();
    readonly remove = output<void>();

    protected readonly initials = computed(() => {
        const parts = this.teacher().name.split(' ');
        const first = parts[0]?.charAt(0) ?? '';
        const second = parts[1]?.charAt(0) ?? '';
        const combined = `${first}${second}` || this.teacher().name.slice(0, 2);

        return combined.toUpperCase();
    });

    protected readonly carChips = computed<CarChip[]>(() =>
        this.cars().map((car) => ({
            id: car.id,
            name: car.name,
            transmissionKey:
                car.transmission === Transmission.manual
                    ? 'teachers.transmissionsShort.manual'
                    : 'teachers.transmissionsShort.automatic',
        })),
    );
}
