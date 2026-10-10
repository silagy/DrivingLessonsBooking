import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TooltipModule } from 'primeng/tooltip';
import { RemovedPart } from '../../../domain/removed-part.enum';

@Component({
    selector: 'app-deleted-flag',
    imports: [TranslocoPipe, TooltipModule],
    templateUrl: './deleted-flag.component.html',
    styleUrl: './deleted-flag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeletedFlagComponent {
    readonly part = input.required<RemovedPart>();

    protected readonly labelKey = computed(() => `students.deletedFlag.${this.part()}`);
}
