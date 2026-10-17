import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-no-other-users',
    imports: [TranslocoPipe, ButtonModule],
    templateUrl: './no-other-users.component.html',
    styleUrl: './no-other-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoOtherUsersComponent {
    readonly disabled = input(false);

    readonly addRequested = output<void>();
}
