import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/auth.service';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../shared/language-toggle/language-toggle.component';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, TranslocoPipe, ButtonModule, BrandLogoComponent, LanguageToggleComponent],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  protected readonly auth = inject(AuthService);

  protected readonly initials = computed(() => {
    const email = this.auth.email();
    return email ? email.slice(0, 2).toUpperCase() : 'AD';
  });
}
