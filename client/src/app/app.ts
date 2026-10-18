import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';
import { ThemeService } from './core/theme.service';
import { NewVersionNoticeComponent } from './shared/components/new-version-notice/new-version-notice.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, NewVersionNoticeComponent],
  template: `
    <router-outlet />
    <app-new-version-notice />
    @if (language.isRtl()) {
      <p-toast position="top-left" />
    } @else {
      <p-toast position="top-right" />
    }
  `,
})
export class App {
  protected readonly language = inject(LanguageService);
  private readonly theme = inject(ThemeService);
}
