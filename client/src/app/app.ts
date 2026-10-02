import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: `
    <router-outlet />
    @if (language.isRtl()) {
      <p-toast position="top-left" />
    } @else {
      <p-toast position="top-right" />
    }
  `,
})
export class App {
  protected readonly language = inject(LanguageService);
}
