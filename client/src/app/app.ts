import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: '<router-outlet /><p-toast [position]="toastPosition()" />',
})
export class App {
  private readonly language = inject(LanguageService);

  protected readonly toastPosition = computed(() => (this.language.isRtl() ? 'top-left' : 'top-right'));
}
