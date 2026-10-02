import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';

type ToastCorner = 'top-left' | 'top-right';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: `
    <router-outlet />
    @for (corner of toastCorner(); track corner) {
      <p-toast [position]="corner" />
    }
  `,
})
export class App {
  private readonly language = inject(LanguageService);

  protected readonly toastCorner = computed<readonly ToastCorner[]>(() => [
    this.language.isRtl() ? 'top-left' : 'top-right',
  ]);
}
