import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { BrandLogoComponent } from '../../../../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../../../../shared/language-toggle/language-toggle.component';
import { NavigationItem } from '../../../domain/navigation';

@Component({
    selector: 'app-shell-drawer',
    imports: [
        RouterLink,
        RouterLinkActive,
        TranslocoPipe,
        ButtonModule,
        DrawerModule,
        BrandLogoComponent,
        LanguageToggleComponent,
    ],
    templateUrl: './shell-drawer.component.html',
    styleUrl: './shell-drawer.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellDrawerComponent {
    readonly open = model(false);
    readonly navigation = input.required<readonly NavigationItem[]>();
    readonly position = input.required<'left' | 'right'>();
    readonly email = input<string | null>(null);
    readonly initials = input('');

    readonly signOut = output<void>();

    protected close(): void {
        this.open.set(false);
    }

    protected onSignOut(): void {
        this.close();
        this.signOut.emit();
    }
}
