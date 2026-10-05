import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { InitialsPipe } from '../../../../../shared/pipes/initials.pipe';
import { Role } from '../../../../../shared/models/role.enum';
import { User } from '../../../domain/user.model';

@Component({
    selector: 'app-user-who-card',
    imports: [TranslocoPipe, InitialsPipe, TagModule],
    templateUrl: './user-who-card.component.html',
    styleUrl: './user-who-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserWhoCardComponent {
    readonly user = input.required<User>();

    protected readonly roles = Role;
}
