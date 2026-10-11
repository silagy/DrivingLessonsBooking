import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { Menu, MenuModule } from 'primeng/menu';
import { AuthService } from '../../core/auth.service';
import { Role } from '../../shared/models/role.enum';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../shared/language-toggle/language-toggle.component';
import { navigationFor } from './domain/navigation';
import { ChangeMyPasswordDialog } from './ui/dialogs/change-my-password/change-my-password.dialog';

@Component({
  selector: 'app-admin-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoPipe,
    ButtonModule,
    MenuModule,
    BrandLogoComponent,
    LanguageToggleComponent,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  providers: [DialogService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private static readonly dialogWidth = '32.5rem';

  protected readonly auth = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly transloco = inject(TranslocoService);

  protected readonly roles = Role;
  protected readonly navigation = computed(() => navigationFor(this.auth.role()));
  protected readonly userMenuItems = signal<MenuItem[]>([]);
  protected readonly isUserMenuOpen = signal(false);

  private readonly userMenu = viewChild.required<Menu>('userMenu');

  protected readonly initials = computed(() => {
    const email = this.auth.email();
    return email ? email.slice(0, 2).toUpperCase() : 'AD';
  });

  protected onUserMenu(event: Event): void {
    this.userMenuItems.set([
      {
        label: this.transloco.translate('shell.changeMyPassword'),
        icon: 'pi pi-key',
        command: () => this.onChangeMyPassword(),
      },
      {
        label: this.transloco.translate('shell.logout'),
        icon: 'pi pi-sign-out',
        styleClass: 'shell-user-menu__item--sign-out',
        command: () => this.auth.logout(),
      },
    ]);
    this.userMenu().toggle(event);
  }

  private onChangeMyPassword(): void {
    this.dialogs.open(ChangeMyPasswordDialog, {
      header: this.transloco.translate('myPassword.title'),
      width: AdminShellComponent.dialogWidth,
      modal: true,
      dismissableMask: false,
    });
  }
}
