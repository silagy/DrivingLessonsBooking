import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Transmission } from '../../../domain/transmission.enum';

@Component({
    selector: 'app-transmission-tag',
    imports: [TranslocoPipe],
    template: `<span
        class="transmission-tag"
        [class.transmission-tag--automatic]="isAutomatic() && !muted()"
        [class.transmission-tag--manual]="!isAutomatic() && !muted()"
        [class.transmission-tag--muted]="muted()">{{ 'students.transmissions.' + transmission() | transloco }}</span>`,
    styleUrl: './transmission-tag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransmissionTagComponent {
    readonly transmission = input.required<Transmission>();
    readonly muted = input(false);

    protected readonly isAutomatic = computed(() => this.transmission() === Transmission.automatic);
}
