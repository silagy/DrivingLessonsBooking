import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Car } from '../../../domain/car.model';
import { Transmission } from '../../../domain/transmission.enum';

@Component({
    selector: 'app-car-row',
    imports: [TranslocoPipe],
    templateUrl: './car-row.component.html',
    styleUrl: './car-row.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarRowComponent {
    readonly car = input.required<Car>();

    readonly edit = output<void>();

    protected readonly isManual = computed(() => this.car().transmission === Transmission.manual);
}
