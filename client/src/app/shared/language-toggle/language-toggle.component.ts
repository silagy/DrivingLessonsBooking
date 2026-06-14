import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AppLanguage, LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-language-toggle',
  imports: [],
  template: `
    <div class="lang" role="group">
      @for (option of options; track option.lang) {
        <button
          type="button"
          class="lang__opt"
          [class.lang__opt--active]="language.lang() === option.lang"
          [attr.aria-pressed]="language.lang() === option.lang"
          (click)="language.use(option.lang)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styleUrl: './language-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageToggleComponent {
  protected readonly language = inject(LanguageService);

  protected readonly options: ReadonlyArray<{ lang: AppLanguage; label: string }> = [
    { lang: 'en', label: 'EN' },
    { lang: 'he', label: 'עב' },
  ];
}
