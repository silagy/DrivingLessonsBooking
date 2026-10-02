import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { AppLanguage, LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-language-toggle',
  imports: [TranslocoPipe],
  template: `
    <div
      class="lang"
      [class.lang--touch]="touch()"
      role="group"
      [attr.aria-label]="'shell.language' | transloco"
    >
      @for (option of options; track option.lang) {
        <button
          type="button"
          class="lang__opt"
          [class.lang__opt--active]="language.lang() === option.lang"
          [attr.aria-pressed]="language.lang() === option.lang"
          [attr.lang]="option.lang"
          [attr.aria-label]="option.name"
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

  readonly touch = input(false);

  protected readonly options: ReadonlyArray<{ lang: AppLanguage; label: string; name: string }> = [
    { lang: 'en', label: 'EN', name: 'English' },
    { lang: 'he', label: 'עב', name: 'עברית' },
  ];
}
