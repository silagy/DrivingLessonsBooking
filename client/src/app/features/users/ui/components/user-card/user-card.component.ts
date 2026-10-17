import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { Role } from '../../../../../shared/models/role.enum';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { User } from '../../../domain/user.model';

@Component({
    selector: 'app-user-card',
    imports: [TranslocoPipe, InitialsPipe, ButtonModule, TagModule],
    templateUrl: './user-card.component.html',
    styleUrl: './user-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserCardComponent {
    readonly user = input.required<User>();
    readonly isYou = input(false);
    readonly disabled = input(false);

    readonly actionsRequested = output<Event>();
    readonly restoreRequested = output<void>();

    protected readonly roles = Role;
}
