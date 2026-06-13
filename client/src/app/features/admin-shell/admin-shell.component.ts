import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, TranslocoPipe, ToolbarModule, ButtonModule],
  templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly language = inject(LanguageService);
}
