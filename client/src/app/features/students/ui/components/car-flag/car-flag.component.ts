import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TooltipModule } from 'primeng/tooltip';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';

@Component({
    selector: 'app-car-flag',
    imports: [TranslocoPipe, TooltipModule],
    templateUrl: './car-flag.component.html',
    styleUrl: './car-flag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarFlagComponent {
    readonly teacherName = input.required<string>();
    readonly isActive = input.required<boolean>();

    protected readonly tipKey = computed(() => (this.isActive() ? 'students.flag.tip' : 'students.flag.tipInactive'));
    protected readonly tipParams = computed(() => ({ teacher: isolateDirection(this.teacherName()) }));
}
