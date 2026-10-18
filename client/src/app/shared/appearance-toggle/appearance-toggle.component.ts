import { ChangeDetectionStrategy, Component, ElementRef, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { SelectButtonModule } from 'primeng/selectbutton';
import { LanguageService } from '../../core/language.service';
import { ThemeMode, ThemeService } from '../../core/theme.service';

const NEXT_KEY = { rtl: 'ArrowLeft', ltr: 'ArrowRight' } as const;
const PREVIOUS_KEY = { rtl: 'ArrowRight', ltr: 'ArrowLeft' } as const;

@Component({
  selector: 'app-appearance-toggle',
  imports: [FormsModule, TranslocoPipe, SelectButtonModule],
  template: `
    <p-selectbutton
      class="appearance"
      [class.appearance--fill]="fill()"
      [options]="options"
      optionLabel="labelKey"
      optionValue="mode"
      [allowEmpty]="false"
      [ngModel]="theme.mode()"
      (ngModelChange)="theme.use($event)"
      [attr.aria-label]="'shell.appearance.label' | transloco"
      (keydown)="onKeydown($event)"
    >
      <ng-template #item let-option>{{ option.labelKey | transloco }}</ng-template>
    </p-selectbutton>
  `,
  styleUrl: './appearance-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppearanceToggleComponent {
  protected readonly theme = inject(ThemeService);
  private readonly language = inject(LanguageService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly fill = input(false);

  protected readonly options: { mode: ThemeMode; labelKey: string }[] = [
    { mode: 'light', labelKey: 'shell.appearance.light' },
    { mode: 'dark', labelKey: 'shell.appearance.dark' },
    { mode: 'system', labelKey: 'shell.appearance.system' },
  ];

  protected onKeydown(event: KeyboardEvent): void {
    const buttons = [...this.host.nativeElement.querySelectorAll<HTMLElement>('[role="button"]')];
    const target = this.targetIndex(event.key, buttons.indexOf(document.activeElement as HTMLElement), buttons.length);

    if (target === null) {
      return;
    }

    event.preventDefault();
    buttons[target].focus();
  }

  private targetIndex(key: string, current: number, count: number): number | null {
    const direction = this.language.isRtl() ? 'rtl' : 'ltr';

    switch (key) {
      case NEXT_KEY[direction]:
        return (current + 1) % count;
      case PREVIOUS_KEY[direction]:
        return (current - 1 + count) % count;
      case 'Home':
        return 0;
      case 'End':
        return count - 1;
      default:
        return null;
    }
  }
}
